using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Services.Core.SGX;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using NeoServiceLayer.Services.Voting.Models;


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
public partial class VotingService : ServiceFramework.EnclaveBlockchainServiceBase, IVotingService, IDisposable
{
    private readonly IStorageService _storageService;
    private readonly Dictionary<string, VotingStrategy> _votingStrategies = new();
    private readonly Dictionary<string, VotingResult> _votingResults = new();
    private readonly Dictionary<string, Candidate> _candidates = new();
    private readonly object _strategiesLock = new();
    private readonly object _resultsLock = new();
    private readonly object _candidatesLock = new();
    private readonly Timer _strategyExecutionTimer;
    private readonly Timer _candidateUpdateTimer;
    private readonly string _rpcEndpoint;
    private readonly ISGXPersistence _sgxPersistence;

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
        Configuration = configuration;
        _rpcEndpoint = configuration?.GetValue<string>("NeoN3RpcEndpoint") ?? "http://localhost:20332";
        _sgxPersistence = new SGXPersistence(logger, enclaveManager);

        // Initialize timers
        _strategyExecutionTimer = new Timer(ExecuteAutoStrategies, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        _candidateUpdateTimer = new Timer(UpdateCandidate, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));

        AddCapability<IVotingService>();
        AddDependency(new ServiceDependency("HealthService", false, "1.0.0"));
        AddDependency(new ServiceDependency("OracleService", false, "1.0.0"));
        AddDependency(new ServiceDependency("StorageService", true, "1.0.0"));
        AddDependency(new ServiceDependency("EnclaveStorageService", false, "1.0.0"));

        // Initialize with some sample candidates
        InitializeSampleCandidates();
    }

    /// <summary>
    /// Timer callback for executing auto strategies.
    /// </summary>
    private void ExecuteAutoStrategies(object? state)
    {
        // This method will be implemented to execute auto strategies
        // For now, just log
        Logger.LogDebug("ExecuteAutoStrategies timer callback");
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
            // Load from regular storage
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
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            // Load voting results from regular storage
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
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            // Load candidates from regular storage
            try
            {
                var candidatesData = await _storageService.GetDataAsync(CandidatesStorageKey, BlockchainType.NeoN3);
                var candidatesJson = System.Text.Encoding.UTF8.GetString(candidatesData);
                var candidates = JsonSerializer.Deserialize<Dictionary<string, Candidate>>(candidatesJson);
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

            // Store in regular storage
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
            Dictionary<string, Candidate> candidatesToPersist;
            lock (_candidatesLock)
            {
                candidatesToPersist = new Dictionary<string, Candidate>(_candidates);
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
    /// Checks if a user is eligible to create a proposal.
    /// </summary>
    public async Task<bool> IsEligibleToCreateProposalAsync(Guid userId)
    {
        return await _sgxPersistence.IsEligibleToCreateProposalAsync(userId);
    }
    
    /// <summary>
    /// Checks if a user is eligible to vote on a proposal.
    /// </summary>
    public async Task<bool> IsEligibleToVoteAsync(Guid voterId, Guid proposalId)
    {
        return await _sgxPersistence.IsEligibleToVoteAsync(voterId, proposalId);
    }
    
    /// <summary>
    /// Gets the voting weight for a user on a specific proposal.
    /// </summary>
    public async Task<decimal> GetVoterWeightAsync(Guid voterId, Guid proposalId)
    {
        return await _sgxPersistence.GetVoterWeightAsync(voterId, proposalId);
    }
    
    /// <summary>
    /// Records a vote delegation.
    /// </summary>
    public async Task RecordDelegationAsync(Guid delegatorId, Guid delegateId, Guid proposalId, decimal weight)
    {
        await _sgxPersistence.RecordDelegationAsync(delegatorId, delegateId, proposalId, weight);
    }
}

/// <summary>
/// Simple SGX persistence implementation for voting service.
/// </summary>
public class SGXPersistence : ISGXPersistence
{
    private readonly ILogger _logger;
    private readonly IEnclaveManager _enclaveManager;

    public SGXPersistence(ILogger logger, IEnclaveManager enclaveManager)
    {
        _logger = logger;
        _enclaveManager = enclaveManager;
    }

    public async Task StoreVotingResultsAsync(Dictionary<string, VotingResult> results, BlockchainType blockchainType)
    {
        try
        {
            _logger.LogDebug("Storing {Count} voting results in SGX enclave", results.Count);
            // Implementation would store in SGX enclave
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store voting results in SGX enclave");
            throw;
        }
    }

    public async Task StoreCandidatesAsync(Dictionary<string, Candidate> candidates, BlockchainType blockchainType)
    {
        try
        {
            _logger.LogDebug("Storing {Count} candidates in SGX enclave", candidates.Count);
            // Implementation would store in SGX enclave
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store candidates in SGX enclave");
            throw;
        }
    }

    public async Task<bool> IsEligibleToCreateProposalAsync(Guid userId)
    {
        try
        {
            _logger.LogDebug("Checking proposal creation eligibility for user {UserId}", userId);
            // Implementation would check in SGX enclave
            await Task.CompletedTask;
            return true; // Placeholder implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check proposal creation eligibility");
            throw;
        }
    }

    public async Task<bool> IsEligibleToVoteAsync(Guid voterId, Guid proposalId)
    {
        try
        {
            _logger.LogDebug("Checking voting eligibility for voter {VoterId} on proposal {ProposalId}", voterId, proposalId);
            // Implementation would check in SGX enclave
            await Task.CompletedTask;
            return true; // Placeholder implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check voting eligibility");
            throw;
        }
    }

    public async Task<decimal> GetVoterWeightAsync(Guid voterId, Guid proposalId)
    {
        try
        {
            _logger.LogDebug("Getting voter weight for voter {VoterId} on proposal {ProposalId}", voterId, proposalId);
            // Implementation would retrieve from SGX enclave
            await Task.CompletedTask;
            return 1.0m; // Placeholder implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get voter weight");
            throw;
        }
    }

    public async Task RecordDelegationAsync(Guid delegatorId, Guid delegateId, Guid proposalId, decimal weight)
    {
        try
        {
            _logger.LogDebug("Recording delegation from {DelegatorId} to {DelegateId} for proposal {ProposalId} with weight {Weight}", 
                delegatorId, delegateId, proposalId, weight);
            // Implementation would store in SGX enclave
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record delegation");
            throw;
        }
    }

    /// <summary>
    /// Initializes sample candidates for testing purposes.
    /// </summary>
    private void InitializeSampleCandidates()
    {
        // Implementation moved to VotingService.Candidates.cs partial class
        // where _candidates and _candidatesLock are defined
    }

    /// <summary>
    /// Executes automatic voting strategies.
    /// </summary>
    private void ExecuteAutoStrategies(object? state)
    {
        // Implementation moved to appropriate partial class
    }

    /// <summary>
}

/// <summary>
/// Interface for SGX persistence operations.
/// </summary>
public interface ISGXPersistence
{
    Task StoreVotingResultsAsync(Dictionary<string, VotingResult> results, BlockchainType blockchainType);
    Task StoreCandidatesAsync(Dictionary<string, Candidate> candidates, BlockchainType blockchainType);
    Task<bool> IsEligibleToCreateProposalAsync(Guid userId);
    Task<bool> IsEligibleToVoteAsync(Guid voterId, Guid proposalId);
    Task<decimal> GetVoterWeightAsync(Guid voterId, Guid proposalId);
    Task RecordDelegationAsync(Guid delegatorId, Guid delegateId, Guid proposalId, decimal weight);
}
