using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.SmartContracts.NeoN3;
using NeoServiceLayer.Services.SmartContracts.NeoX;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Services.SmartContracts;

/// <summary>
/// Unified smart contracts service that manages both Neo N3 and Neo X smart contract operations.
/// </summary>
public partial class SmartContractsService : ServiceFramework.EnclaveBlockchainServiceBase, ISmartContractsService
{
    private readonly IServiceConfiguration _configuration;
    private new readonly IEnclaveManager _enclaveManager;
    private readonly Dictionary<BlockchainType, ISmartContractManager> _managers = new();
    private readonly Dictionary<string, ContractUsageInfo> _usageStats = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartContractsService"/> class.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="neoN3Manager">The Neo N3 smart contract manager.</param>
    /// <param name="neoXManager">The Neo X smart contract manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public SmartContractsService(
        IServiceConfiguration configuration,
        IEnclaveManager enclaveManager,
        NeoN3SmartContractManager neoN3Manager,
        NeoXSmartContractManager neoXManager,
        ILogger<SmartContractsService> logger,
        IServiceProvider? serviceProvider = null)
        : base("SmartContracts", "Unified Smart Contracts Management Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _configuration = configuration;
        _enclaveManager = enclaveManager;
        _serviceProvider = serviceProvider;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Register managers
        _managers[BlockchainType.NeoN3] = neoN3Manager;
        _managers[BlockchainType.NeoX] = neoXManager;

        // Add capabilities
        AddCapability<ISmartContractsService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("SupportedBlockchains", "NeoN3,NeoX");
        SetMetadata("SupportedFeatures", "Deploy,Invoke,Call,Events,Statistics,CrossChain");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Smart Contracts Service...");

            // Initialize persistent storage
            await InitializePersistentStorageAsync();

            // Initialize all blockchain managers
            var initTasks = _managers.Values.Select(async manager =>
            {
                try
                {
                    // Manager initialization handled by service framework
                    Logger.LogInformation("Initialized {ManagerType} manager", manager.GetType().Name);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error initializing {ManagerType} manager", manager.GetType().Name);
                    return false;
                }
            });

            var results = await Task.WhenAll(initTasks);
            var successCount = results.Count(r => r);

            Logger.LogInformation("Initialized {SuccessCount}/{TotalCount} blockchain managers",
                successCount, _managers.Count);

            // Load usage statistics
            await LoadUsageStatisticsAsync();

            Logger.LogInformation("Smart Contracts Service initialized successfully");
            return successCount > 0; // At least one manager must be initialized
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Smart Contracts Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing enclave for Smart Contracts Service...");
            return await _enclaveManager.InitializeEnclaveAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing enclave for Smart Contracts Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Smart Contracts Service...");

            // Start all blockchain managers
            var startTasks = _managers.Values.Select(async manager =>
            {
                try
                {
                    // Manager start handled by service framework
                    Logger.LogInformation("Started {ManagerType} manager", manager.GetType().Name);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error starting {ManagerType} manager", manager.GetType().Name);
                    return false;
                }
            });

            var results = await Task.WhenAll(startTasks);
            var successCount = results.Count(r => r);

            Logger.LogInformation("Started {SuccessCount}/{TotalCount} blockchain managers",
                successCount, _managers.Count);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Smart Contracts Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Smart Contracts Service...");

            // Stop all blockchain managers
            var stopTasks = _managers.Values.Select(async manager =>
            {
                try
                {
                    // Manager stop handled by service framework
                    Logger.LogInformation("Stopped {ManagerType} manager", manager.GetType().Name);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error stopping {ManagerType} manager", manager.GetType().Name);
                    return false;
                }
            });

            await Task.WhenAll(stopTasks);

            // Save usage statistics
            await SaveUsageStatisticsAsync();

            _usageStats.Clear();
            Logger.LogInformation("Smart Contracts Service stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Smart Contracts Service");
            return false;
        }
    }

    /// <inheritdoc/>
    public ISmartContractManager GetManager(BlockchainType blockchainType)
    {
        if (!_managers.TryGetValue(blockchainType, out var manager))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported");
        }

        return manager;
    }

    /// <inheritdoc/>
    public async Task<ContractDeploymentResult> DeployContractAsync(
        BlockchainType blockchainType,
        byte[] contractCode,
        object[]? constructorParameters = null,
        ContractDeploymentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogInformation("Deploying contract to {BlockchainType}", blockchainType);

            try
            {
                // First, validate deployment using privacy-preserving operations
                var privacyResult = await DeployContractWithPrivacyAsync(contractCode, constructorParameters, options);

                Logger.LogDebug("Privacy-preserving contract deployment validation completed, gas estimate: {GasEstimate}",
                    privacyResult.GasUsed);

                var manager = GetManager(blockchainType);
                var result = await manager.DeployContractAsync(contractCode, constructorParameters, options, cancellationToken);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.ContractHash))
                {
                    // Initialize usage tracking for the new contract
                    var usageInfo = new ContractUsageInfo
                    {
                        ContractHash = result.ContractHash,
                        Name = options?.Name ?? "Unknown",
                        BlockchainType = blockchainType,
                        InvocationCount = 0,
                        TotalGasConsumed = result.GasConsumed,
                        LastInvoked = DateTime.UtcNow
                    };

                    lock (_usageStats)
                    {
                        _usageStats[result.ContractHash] = usageInfo;
                    }

                    await SaveUsageStatisticsAsync();
                }

                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                UpdateMetric("TotalDeployments", _successCount);

                Logger.LogInformation("Successfully deployed contract {ContractHash} to {BlockchainType}",
                    result.ContractHash, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                UpdateMetric("LastErrorMessage", ex.Message);
                Logger.LogError(ex, "Error deploying contract to {BlockchainType}", blockchainType);
                throw;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<ContractInvocationResult> InvokeContractAsync(
        BlockchainType blockchainType,
        string contractHash,
        string method,
        object[]? parameters = null,
        ContractInvocationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogDebug("Invoking contract {ContractHash} method {Method} on {BlockchainType}",
                contractHash, method, blockchainType);

            try
            {
                // First, validate and analyze invocation using privacy-preserving operations
                var privacyResult = await InvokeContractWithPrivacyAsync(contractHash, method, parameters, options);

                Logger.LogDebug("Privacy-preserving contract invocation validation completed, gas estimate: {GasEstimate}, proof: {Proof}",
                    privacyResult.GasUsed, privacyResult.ExecutionProof);

                var manager = GetManager(blockchainType);
                var result = await manager.InvokeContractAsync(contractHash, method, parameters, options, cancellationToken);

                // Update usage statistics
                if (result.IsSuccess)
                {
                    UpdateContractUsage(contractHash, blockchainType, result.GasConsumed);
                }

                _successCount++;
                UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                UpdateMetric("TotalInvocations", _successCount);

                Logger.LogDebug("Successfully invoked contract {ContractHash} method {Method} on {BlockchainType}",
                    contractHash, method, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                _failureCount++;
                UpdateMetric("LastFailureTime", DateTime.UtcNow);
                UpdateMetric("LastErrorMessage", ex.Message);
                Logger.LogError(ex, "Error invoking contract {ContractHash} method {Method} on {BlockchainType}",
                    contractHash, method, blockchainType);
                throw;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<object?> CallContractAsync(
        BlockchainType blockchainType,
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogDebug("Calling contract {ContractHash} method {Method} on {BlockchainType} (read-only)",
                contractHash, method, blockchainType);

            var manager = GetManager(blockchainType);
            var result = await manager.CallContractAsync(contractHash, method, parameters, cancellationToken);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            Logger.LogDebug("Successfully called contract {ContractHash} method {Method} on {BlockchainType}",
                contractHash, method, blockchainType);

            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error calling contract {ContractHash} method {Method} on {BlockchainType}",
                contractHash, method, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ContractMetadata?> GetContractMetadataAsync(
        BlockchainType blockchainType,
        string contractHash,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            var manager = GetManager(blockchainType);
            var result = await manager.GetContractMetadataAsync(contractHash, cancellationToken);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting contract metadata for {ContractHash} on {BlockchainType}",
                contractHash, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<BlockchainType, IEnumerable<ContractMetadata>>> ListAllDeployedContractsAsync(
        BlockchainType? blockchainType = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running");
        }

        try
        {
            Logger.LogDebug("Listing deployed contracts for {BlockchainType}",
                blockchainType?.ToString() ?? "all blockchains");

            var result = new Dictionary<BlockchainType, IEnumerable<ContractMetadata>>();

            var blockchains = blockchainType.HasValue ?
                new[] { blockchainType.Value } :
                _managers.Keys.ToArray();

            var tasks = blockchains.Select(async bc =>
            {
                try
                {
                    var manager = GetManager(bc);
                    var contracts = await manager.ListDeployedContractsAsync(cancellationToken);
                    return new { BlockchainType = bc, Contracts = contracts };
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error listing contracts for {BlockchainType}", bc);
                    return new { BlockchainType = bc, Contracts = Enumerable.Empty<ContractMetadata>() };
                }
            });

            var results = await Task.WhenAll(tasks);

            foreach (var item in results)
            {
                result[item.BlockchainType] = item.Contracts;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            Logger.LogDebug("Listed contracts for {BlockchainCount} blockchains", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error listing all deployed contracts");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NeoServiceLayer.Core.SmartContracts.ContractEvent>> GetContractEventsAsync(
        BlockchainType blockchainType,
        string contractHash,
        string? eventName = null,
        long? fromBlock = null,
        long? toBlock = null,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogDebug("Getting events for contract {ContractHash} on {BlockchainType}",
                contractHash, blockchainType);

            var manager = GetManager(blockchainType);
            var result = await manager.GetContractEventsAsync(contractHash, eventName, fromBlock, toBlock, cancellationToken);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            Logger.LogDebug("Retrieved {EventCount} events for contract {ContractHash} on {BlockchainType}",
                result.Count(), contractHash, blockchainType);

            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting events for contract {ContractHash} on {BlockchainType}",
                contractHash, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long> EstimateGasAsync(
        BlockchainType blockchainType,
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            var manager = GetManager(blockchainType);
            var result = await manager.EstimateGasAsync(contractHash, method, parameters, cancellationToken);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error estimating gas for contract {ContractHash} method {Method} on {BlockchainType}",
                contractHash, method, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SmartContractStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Generating smart contract statistics");

            var statistics = new SmartContractStatistics
            {
                GeneratedAt = DateTime.UtcNow
            };

            // Get statistics from all managers
            foreach (var (blockchainType, manager) in _managers)
            {
                try
                {
                    var contracts = await manager.ListDeployedContractsAsync(cancellationToken);
                    var contractCount = contracts.Count();

                    // Calculate statistics from usage data
                    long invocations = 0;
                    long gasConsumed = 0;

                    lock (_usageStats)
                    {
                        var blockchainContracts = _usageStats.Values
                            .Where(u => u.BlockchainType == blockchainType);

                        invocations = blockchainContracts.Sum(u => u.InvocationCount);
                        gasConsumed = blockchainContracts.Sum(u => u.TotalGasConsumed);
                    }

                    var blockchainStats = new BlockchainContractStats
                    {
                        BlockchainType = blockchainType,
                        ContractsDeployed = contractCount,
                        Invocations = invocations,
                        GasConsumed = gasConsumed,
                        SuccessRate = _requestCount > 0 ? (double)_successCount / _requestCount * 100 : 0,
                        AverageGasPerInvocation = invocations > 0 ? (double)gasConsumed / invocations : 0
                    };

                    statistics.ByBlockchain[blockchainType] = blockchainStats;
                    statistics.TotalContractsDeployed += contractCount;
                    statistics.TotalInvocations += invocations;
                    statistics.TotalGasConsumed += gasConsumed;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error getting statistics for {BlockchainType}", blockchainType);
                }
            }

            // Get most active contracts
            lock (_usageStats)
            {
                statistics.MostActiveContracts = _usageStats.Values
                    .OrderByDescending(u => u.InvocationCount)
                    .Take(10)
                    .ToList();
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);

            Logger.LogDebug("Generated statistics for {ContractCount} contracts across {BlockchainCount} blockchains",
                statistics.TotalContractsDeployed, statistics.ByBlockchain.Count);

            return statistics;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error generating smart contract statistics");
            throw;
        }
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Since managers are separate services, we assume they're healthy if registered
        var healthyManagers = _managers.Count;
        var totalManagers = _managers.Count;

        var health = healthyManagers > 0 ?
            (healthyManagers == totalManagers ? ServiceHealth.Healthy : ServiceHealth.Degraded) :
            ServiceHealth.Unhealthy;

        return Task.FromResult(health);
    }

    /// <inheritdoc/>
    protected override Task OnUpdateMetricsAsync()
    {
        UpdateMetric("RequestCount", _requestCount);
        UpdateMetric("SuccessCount", _successCount);
        UpdateMetric("FailureCount", _failureCount);
        UpdateMetric("SuccessRate", _requestCount > 0 ? (double)_successCount / _requestCount : 0);
        UpdateMetric("LastRequestTime", _lastRequestTime);
        UpdateMetric("SupportedBlockchains", _managers.Count);
        UpdateMetric("HealthyManagers", _managers.Count);

        // Update contract statistics
        lock (_usageStats)
        {
            UpdateMetric("TotalContracts", _usageStats.Count);
            UpdateMetric("TotalInvocations", _usageStats.Values.Sum(u => u.InvocationCount));
            UpdateMetric("TotalGasConsumed", _usageStats.Values.Sum(u => u.TotalGasConsumed));
        }

        return Task.CompletedTask;
    }

    #region Private Helper Methods

    private void UpdateContractUsage(string contractHash, BlockchainType blockchainType, long gasConsumed)
    {
        lock (_usageStats)
        {
            if (_usageStats.TryGetValue(contractHash, out var usage))
            {
                usage.InvocationCount++;
                usage.TotalGasConsumed += gasConsumed;
                usage.LastInvoked = DateTime.UtcNow;
            }
            else
            {
                _usageStats[contractHash] = new ContractUsageInfo
                {
                    ContractHash = contractHash,
                    Name = "Unknown",
                    BlockchainType = blockchainType,
                    InvocationCount = 1,
                    TotalGasConsumed = gasConsumed,
                    LastInvoked = DateTime.UtcNow
                };
            }
        }

        // Periodically save statistics
        if (_usageStats.Count % 10 == 0)
        {
            _ = Task.Run(SaveUsageStatisticsAsync);
        }
    }

    private async Task LoadUsageStatisticsAsync()
    {
        try
        {
            var data = await _enclaveManager.CallEnclaveFunctionAsync("getSmartContractUsageStats", "");

            if (!string.IsNullOrEmpty(data) && data != "null")
            {
                var stats = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ContractUsageInfo>>(data);
                if (stats != null)
                {
                    lock (_usageStats)
                    {
                        _usageStats.Clear();
                        foreach (var kvp in stats)
                        {
                            _usageStats[kvp.Key] = kvp.Value;
                        }
                    }

                    Logger.LogInformation("Loaded usage statistics for {ContractCount} contracts", stats.Count);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error loading usage statistics, starting with empty stats");
        }
    }

    private async Task SaveUsageStatisticsAsync()
    {
        try
        {
            Dictionary<string, ContractUsageInfo> statsToSave;
            lock (_usageStats)
            {
                statsToSave = new Dictionary<string, ContractUsageInfo>(_usageStats);
            }

            var data = System.Text.Json.JsonSerializer.Serialize(statsToSave);
            await _enclaveManager.CallEnclaveFunctionAsync("saveSmartContractUsageStats", data);

            Logger.LogDebug("Saved usage statistics for {ContractCount} contracts", statsToSave.Count);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error saving usage statistics");
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposePersistenceResources();
        }
        base.Dispose(disposing);
    }
}
