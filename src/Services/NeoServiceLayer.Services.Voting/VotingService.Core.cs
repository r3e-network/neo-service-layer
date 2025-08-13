using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Exception thrown when voting operations fail.
/// </summary>
public class VotingException : Exception
{
    public VotingException(string message) : base(message) { }
    public VotingException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Core implementation of the Voting Service that provides Neo N3 council member voting assistance capabilities.
/// </summary>
public partial class VotingService : EnclaveBlockchainServiceBase, IVotingService, IDisposable
{
    private readonly IStorageService _storageService;
    private readonly SGXPersistence _sgxPersistence;
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
    /// <param name="enclaveStorage">The enclave storage service.</param>
    /// <param name="configuration">The service configuration.</param>
    public VotingService(
        ILogger<VotingService> logger, 
        IEnclaveManager enclaveManager, 
        IStorageService storageService, 
        IEnclaveStorageService? enclaveStorage = null,
        IServiceConfiguration? configuration = null)
        : base("VotingService", "Neo N3 council member voting assistance service", "1.0.0", logger, new[] { BlockchainType.NeoN3 }, enclaveManager)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _sgxPersistence = new SGXPersistence("VotingService", enclaveStorage, logger);
        Configuration = configuration;
        _rpcEndpoint = configuration?.GetValue<string>("NeoN3RpcEndpoint") ?? "http://localhost:20332";

        // Initialize timers
        _strategyExecutionTimer = new Timer(ExecuteAutoStrategies, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        _candidateUpdateTimer = new Timer(UpdateCandidateInfo, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));

        AddCapability<IVotingService>();
        AddDependency(new ServiceDependency("HealthService", false, "1.0.0"));
        AddDependency(new ServiceDependency("OracleService", false, "1.0.0"));
        AddDependency(new ServiceDependency("StorageService", true, "1.0.0"));
        AddDependency(new ServiceDependency("EnclaveStorageService", false, "1.0.0"));

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
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation during Voting Service initialization");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(ex, "Access denied during Voting Service initialization");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during Voting Service initialization");
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
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation during Voting Service enclave initialization");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(ex, "Access denied during Voting Service enclave initialization");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during Voting Service enclave initialization");
            return false;
        }
    }

    /// <summary>
    /// Disposes the voting service resources.
    /// </summary>
    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the voting service resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected override void Dispose(bool disposing)
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
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation during enclave voting algorithms initialization");
            throw new VotingException("Voting algorithms initialization failed due to invalid operation", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(ex, "Access denied during enclave voting algorithms initialization");
            throw new VotingException("Voting algorithms initialization failed due to access restrictions", ex);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during enclave voting algorithms initialization");
            throw new VotingException("Voting algorithms initialization failed", ex);
        }
    }

    /// <summary>
    /// Loads persisted data from storage.
    /// </summary>
    private async Task LoadPersistedDataAsync()
    {
        try
        {
            // Try to load from SGX storage first
            var sgxStrategies = await _sgxPersistence.GetVotingStrategiesAsync(BlockchainType.NeoN3);
            if (sgxStrategies != null)
            {
                lock (_strategiesLock)
                {
                    foreach (var kvp in sgxStrategies)
                    {
                        _votingStrategies[kvp.Key] = kvp.Value;
                    }
                }
                Logger.LogDebug("Loaded {Count} voting strategies from SGX storage", sgxStrategies.Count);
            }
            else
            {
                // Fallback to regular storage
                try
                {
                    var strategiesData = await _storageService.GetDataAsync(StrategiesStorageKey, BlockchainType.NeoN3);
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
                        // Migrate to SGX storage
                        await _sgxPersistence.StoreVotingStrategiesAsync(_votingStrategies, BlockchainType.NeoN3);
                    }
                }
                catch (Exception)
                {
                    // Data doesn't exist yet, which is fine for first run
                }
            }

            // Load voting results from SGX storage
            var sgxResults = await _sgxPersistence.GetVotingResultsAsync(BlockchainType.NeoN3);
            if (sgxResults != null)
            {
                lock (_resultsLock)
                {
                    foreach (var kvp in sgxResults)
                    {
                        _votingResults[kvp.Key] = kvp.Value;
                    }
                }
                Logger.LogDebug("Loaded {Count} voting results from SGX storage", sgxResults.Count);
            }
            else
            {
                // Fallback to regular storage
                try
                {
                    var resultsData = await _storageService.GetDataAsync(ResultsStorageKey, BlockchainType.NeoN3);
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
                        // Migrate to SGX storage
                        await _sgxPersistence.StoreVotingResultsAsync(_votingResults, BlockchainType.NeoN3);
                    }
                }
                catch (Exception)
                {
                    // Data doesn't exist yet, which is fine for first run
                }
            }

            // Load candidates from SGX storage
            var sgxCandidates = await _sgxPersistence.GetCandidatesAsync(BlockchainType.NeoN3);
            if (sgxCandidates != null)
            {
                lock (_candidatesLock)
                {
                    foreach (var kvp in sgxCandidates)
                    {
                        _candidates[kvp.Key] = kvp.Value;
                    }
                }
                Logger.LogDebug("Loaded {Count} candidates from SGX storage", sgxCandidates.Count);
            }
            else
            {
                // Fallback to regular storage
                try
                {
                    var candidatesData = await _storageService.GetDataAsync(CandidatesStorageKey, BlockchainType.NeoN3);
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
                        // Migrate to SGX storage
                        await _sgxPersistence.StoreCandidatesAsync(_candidates, BlockchainType.NeoN3);
                    }
                }
                catch (Exception)
                {
                    // Data doesn't exist yet, which is fine for first run
                }
            }

            Logger.LogInformation("Loaded persisted voting data: {StrategiesCount} strategies, {ResultsCount} results, {CandidatesCount} candidates",
                _votingStrategies.Count, _votingResults.Count, _candidates.Count);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation while loading persisted voting data");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(ex, "Access denied while loading persisted voting data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while loading persisted voting data");
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

            // Store in SGX storage
            await _sgxPersistence.StoreVotingStrategiesAsync(strategiesToPersist, BlockchainType.NeoN3);

            // Also store in regular storage for backwards compatibility
            var json = JsonSerializer.Serialize(strategiesToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            await _storageService.StoreDataAsync(StrategiesStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation while persisting voting strategies");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(ex, "Access denied while persisting voting strategies");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while persisting voting strategies");
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

            // Store in SGX storage
            await _sgxPersistence.StoreVotingResultsAsync(resultsToPersist, BlockchainType.NeoN3);

            // Also store in regular storage for backwards compatibility
            var json = JsonSerializer.Serialize(resultsToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            await _storageService.StoreDataAsync(ResultsStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation while persisting voting results");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(ex, "Access denied while persisting voting results");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while persisting voting results");
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

            // Store in SGX storage
            await _sgxPersistence.StoreCandidatesAsync(candidatesToPersist, BlockchainType.NeoN3);

            // Also store in regular storage for backwards compatibility
            var json = JsonSerializer.Serialize(candidatesToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            await _storageService.StoreDataAsync(CandidatesStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "Invalid operation while persisting candidates");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogError(ex, "Access denied while persisting candidates");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while persisting candidates");
        }
    }

    /// <summary>
    /// Inner class for SGX persistence operations.
    /// </summary>
    private class SGXPersistence : SGXPersistenceBase
    {
        public SGXPersistence(string serviceName, IEnclaveStorageService? enclaveStorage, ILogger logger) 
            : base(serviceName, enclaveStorage, logger)
        {
        }

        public async Task<bool> StoreVotingStrategiesAsync(Dictionary<string, VotingStrategy> strategies, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync("strategies", strategies, 
                new Dictionary<string, object> 
                { 
                    ["type"] = "voting_strategies",
                    ["count"] = strategies.Count 
                }, 
                blockchainType);
        }

        public async Task<Dictionary<string, VotingStrategy>?> GetVotingStrategiesAsync(BlockchainType blockchainType)
        {
            return await RetrieveSecurelyAsync<Dictionary<string, VotingStrategy>>("strategies", blockchainType);
        }

        public async Task<bool> StoreVotingResultsAsync(Dictionary<string, VotingResult> results, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync("results", results,
                new Dictionary<string, object> 
                { 
                    ["type"] = "voting_results",
                    ["count"] = results.Count 
                }, 
                blockchainType);
        }

        public async Task<Dictionary<string, VotingResult>?> GetVotingResultsAsync(BlockchainType blockchainType)
        {
            return await RetrieveSecurelyAsync<Dictionary<string, VotingResult>>("results", blockchainType);
        }

        public async Task<bool> StoreCandidatesAsync(Dictionary<string, CandidateInfo> candidates, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync("candidates", candidates,
                new Dictionary<string, object> 
                { 
                    ["type"] = "candidates",
                    ["count"] = candidates.Count 
                }, 
                blockchainType);
        }

        public async Task<Dictionary<string, CandidateInfo>?> GetCandidatesAsync(BlockchainType blockchainType)
        {
            return await RetrieveSecurelyAsync<Dictionary<string, CandidateInfo>>("candidates", blockchainType);
        }
    }
}
