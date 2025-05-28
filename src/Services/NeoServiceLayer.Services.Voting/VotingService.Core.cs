using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using System.Text.Json;

namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Core implementation of the Voting Service that provides Neo N3 council member voting assistance capabilities.
/// </summary>
public partial class VotingService : EnclaveBlockchainServiceBase, IVotingService, IDisposable
{
    private readonly IStorageService _storageService;
    private readonly Dictionary<string, VotingStrategy> _votingStrategies = new();
    private readonly Dictionary<string, VotingResult> _votingResults = new();
    private readonly Dictionary<string, CandidateInfo> _candidates = new();
    private readonly object _strategiesLock = new();
    private readonly object _resultsLock = new();
    private readonly object _candidatesLock = new();
    private readonly Timer _strategyExecutionTimer;
    private readonly Timer _candidateUpdateTimer;
    private readonly string _rpcEndpoint;

    // Storage keys
    private const string StrategiesStorageKey = "voting:strategies";
    private const string ResultsStorageKey = "voting:results";
    private const string CandidatesStorageKey = "voting:candidates";

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    public IServiceConfiguration? Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VotingService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="storageService">The storage service.</param>
    /// <param name="configuration">The service configuration.</param>
    public VotingService(ILogger<VotingService> logger, IEnclaveManager enclaveManager, IStorageService storageService, IServiceConfiguration? configuration = null)
        : base("VotingService", "Neo N3 council member voting assistance service", "1.0.0", logger, new[] { BlockchainType.NeoN3 }, enclaveManager)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        Configuration = configuration;
        _rpcEndpoint = configuration?.GetValue<string>("NeoN3RpcEndpoint") ?? "http://localhost:20332";

        // Initialize timers
        _strategyExecutionTimer = new Timer(ExecuteAutoStrategies, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        _candidateUpdateTimer = new Timer(UpdateCandidateInfo, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));

        AddCapability<IVotingService>();
        AddDependency(new ServiceDependency("HealthService", false, "1.0.0"));
        AddDependency(new ServiceDependency("OracleService", false, "1.0.0"));
        AddDependency(new ServiceDependency("StorageService", true, "1.0.0"));

        // Initialize with some sample candidates
        InitializeSampleCandidates();
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Voting Service...");

        // Load persisted data
        await LoadPersistedDataAsync();

        Logger.LogInformation("Voting Service started successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Voting Service...");

        // Persist current state
        await PersistAllDataAsync();

        // Dispose timers
        _strategyExecutionTimer?.Dispose();
        _candidateUpdateTimer?.Dispose();

        Logger.LogInformation("Voting Service stopped successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var strategiesCount = _votingStrategies.Count;
        var candidatesCount = _candidates.Count;

        Logger.LogDebug("Voting service health check: {StrategiesCount} strategies, {CandidatesCount} candidates",
            strategiesCount, candidatesCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Voting Service");

        try
        {
            // Load persisted data
            await LoadPersistedDataAsync();

            Logger.LogInformation("Voting Service initialized successfully with {StrategiesCount} strategies and {CandidatesCount} candidates",
                _votingStrategies.Count, _candidates.Count);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Voting Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Voting Service enclave operations");

        try
        {
            // Initialize enclave-specific voting operations
            await InitializeEnclaveVotingAsync();

            Logger.LogInformation("Voting Service enclave operations initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Voting Service enclave operations");
            return false;
        }
    }

    /// <summary>
    /// Disposes the voting service resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the voting service resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _strategyExecutionTimer?.Dispose();
            _candidateUpdateTimer?.Dispose();
        }
    }

    /// <summary>
    /// Initializes enclave-specific voting operations.
    /// </summary>
    private async Task InitializeEnclaveVotingAsync()
    {
        try
        {
            // Initialize voting algorithms in the enclave
            var initResult = await _enclaveManager!.ExecuteJavaScriptAsync("initializeVotingAlgorithms()");

            Logger.LogDebug("Enclave voting algorithms initialized: {Result}", initResult);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize enclave voting algorithms");
            throw;
        }
    }

    /// <summary>
    /// Loads persisted data from storage.
    /// </summary>
    private async Task LoadPersistedDataAsync()
    {
        try
        {
            // Load voting strategies
            try
            {
                var strategiesData = await _storageService.RetrieveDataAsync(StrategiesStorageKey, BlockchainType.NeoN3);
                var strategiesJson = System.Text.Encoding.UTF8.GetString(strategiesData);
                var strategies = JsonSerializer.Deserialize<Dictionary<string, VotingStrategy>>(strategiesJson);
                if (strategies != null)
                {
                    lock (_strategiesLock)
                    {
                        foreach (var kvp in strategies)
                        {
                            _votingStrategies[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            // Load voting results
            try
            {
                var resultsData = await _storageService.RetrieveDataAsync(ResultsStorageKey, BlockchainType.NeoN3);
                var resultsJson = System.Text.Encoding.UTF8.GetString(resultsData);
                var results = JsonSerializer.Deserialize<Dictionary<string, VotingResult>>(resultsJson);
                if (results != null)
                {
                    lock (_resultsLock)
                    {
                        foreach (var kvp in results)
                        {
                            _votingResults[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            // Load candidates
            try
            {
                var candidatesData = await _storageService.RetrieveDataAsync(CandidatesStorageKey, BlockchainType.NeoN3);
                var candidatesJson = System.Text.Encoding.UTF8.GetString(candidatesData);
                var candidates = JsonSerializer.Deserialize<Dictionary<string, CandidateInfo>>(candidatesJson);
                if (candidates != null)
                {
                    lock (_candidatesLock)
                    {
                        foreach (var kvp in candidates)
                        {
                            _candidates[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            Logger.LogInformation("Loaded persisted voting data: {StrategiesCount} strategies, {ResultsCount} results, {CandidatesCount} candidates",
                _votingStrategies.Count, _votingResults.Count, _candidates.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load persisted voting data");
        }
    }

    /// <summary>
    /// Persists all data to storage.
    /// </summary>
    private async Task PersistAllDataAsync()
    {
        await PersistVotingStrategiesAsync();
        await PersistVotingResultsAsync();
        await PersistCandidatesAsync();
    }

    /// <summary>
    /// Persists voting strategies to storage.
    /// </summary>
    private async Task PersistVotingStrategiesAsync()
    {
        try
        {
            Dictionary<string, VotingStrategy> strategiesToPersist;
            lock (_strategiesLock)
            {
                strategiesToPersist = new Dictionary<string, VotingStrategy>(_votingStrategies);
            }

            var json = JsonSerializer.Serialize(strategiesToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            await _storageService.StoreDataAsync(StrategiesStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist voting strategies");
        }
    }

    /// <summary>
    /// Persists voting results to storage.
    /// </summary>
    private async Task PersistVotingResultsAsync()
    {
        try
        {
            Dictionary<string, VotingResult> resultsToPersist;
            lock (_resultsLock)
            {
                resultsToPersist = new Dictionary<string, VotingResult>(_votingResults);
            }

            var json = JsonSerializer.Serialize(resultsToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            await _storageService.StoreDataAsync(ResultsStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist voting results");
        }
    }

    /// <summary>
    /// Persists candidates to storage.
    /// </summary>
    private async Task PersistCandidatesAsync()
    {
        try
        {
            Dictionary<string, CandidateInfo> candidatesToPersist;
            lock (_candidatesLock)
            {
                candidatesToPersist = new Dictionary<string, CandidateInfo>(_candidates);
            }

            var json = JsonSerializer.Serialize(candidatesToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            await _storageService.StoreDataAsync(CandidatesStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist candidates");
        }
    }
}
