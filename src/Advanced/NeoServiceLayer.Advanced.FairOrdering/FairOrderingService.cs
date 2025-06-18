using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using CoreModels = NeoServiceLayer.Core.Models;
using NeoServiceLayer.Advanced.FairOrdering.Models;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Concurrent;
using FairOrderingModels = NeoServiceLayer.Advanced.FairOrdering.Models;

namespace NeoServiceLayer.Advanced.FairOrdering;

/// <summary>
/// Interface for the Fair Ordering Service.
/// </summary>
public interface IFairOrderingService : IEnclaveService, IBlockchainService
{
    Task<string> CreateOrderingPoolAsync(FairOrderingModels.OrderingPoolConfig config, BlockchainType blockchainType);
    Task<string> SubmitFairTransactionAsync(FairTransactionRequest request, BlockchainType blockchainType);
    Task<FairnessAnalysisResult> AnalyzeFairnessRiskAsync(TransactionAnalysisRequest request, BlockchainType blockchainType);
    Task<string> SubmitTransactionAsync(FairOrderingModels.TransactionSubmission submission, BlockchainType blockchainType);
    Task<FairOrderingModels.FairnessMetrics> GetFairnessMetricsAsync(string poolId, BlockchainType blockchainType);
    Task<IEnumerable<FairOrderingModels.OrderingPool>> GetOrderingPoolsAsync(BlockchainType blockchainType);
    Task<bool> UpdatePoolConfigAsync(string poolId, FairOrderingModels.OrderingPoolConfig config, BlockchainType blockchainType);
}

/// <summary>
/// Implementation of the Fair Ordering Service that provides transaction fairness and MEV protection capabilities.
/// </summary>
public partial class FairOrderingService : EnclaveBlockchainServiceBase, IFairOrderingService
{
    private readonly ConcurrentDictionary<string, FairOrderingModels.OrderingPool> _orderingPools = new();
    private readonly ConcurrentQueue<FairOrderingModels.FairOrderingResult> _recentResults = new();
    private readonly object _poolsLock = new();
    private readonly Timer _processingTimer;
    private readonly IPersistentStorageProvider? _storageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FairOrderingService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="storageProvider">The storage provider.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    public FairOrderingService(ILogger<FairOrderingService> logger, IServiceConfiguration? configuration = null, IPersistentStorageProvider? storageProvider = null, IEnclaveManager? enclaveManager = null)
        : base("FairOrdering", "Advanced fair transaction ordering service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _storageProvider = storageProvider;

        AddCapability<IFairOrderingService>();
        
        // Initialize processing timer
        _processingTimer = new Timer(ProcessOrderingPools, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        AddDependency(new ServiceDependency("RandomnessService", true, "1.0.0"));
        AddDependency(new ServiceDependency("KeyManagementService", true, "1.0.0"));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected IServiceConfiguration? Configuration { get; }

    /// <summary>
    /// Gets the storage provider.
    /// </summary>
    protected IPersistentStorageProvider? StorageProvider => _storageProvider;

    /// <inheritdoc/>
    public async Task<string> CreateOrderingPoolAsync(FairOrderingModels.OrderingPoolConfig config, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await CreateOrderingPoolWithResilienceAsync(config, blockchainType);
    }

    /// <inheritdoc/>
    public async Task<string> SubmitFairTransactionAsync(FairTransactionRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await SubmitFairTransactionWithResilienceAsync(request, blockchainType);
    }

    /// <inheritdoc/>
    public async Task<FairnessAnalysisResult> AnalyzeFairnessRiskAsync(TransactionAnalysisRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await AnalyzeFairnessRiskWithResilienceAsync(request, blockchainType);
    }

    /// <inheritdoc/>
    public async Task<string> SubmitTransactionAsync(FairOrderingModels.TransactionSubmission submission, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var submissionId = Guid.NewGuid().ToString();
            
            // Use the first available pool or create a default one
            var pool = _orderingPools.Values.FirstOrDefault(p => p.Status == PoolStatus.Active);
            if (pool == null)
            {
                var defaultPoolId = await CreateOrderingPoolAsync(new FairOrderingModels.OrderingPoolConfig
                {
                    Name = "Default Pool",
                    Description = "Default ordering pool"
                }, blockchainType);
                pool = _orderingPools[defaultPoolId];
            }

            var pendingTransaction = new FairOrderingModels.PendingTransaction
            {
                Id = submissionId,
                Hash = ComputeTransactionHash(submission.TransactionData),
                From = submission.From,
                To = submission.To,
                Value = submission.Value,
                GasPrice = submission.GasPrice,
                GasLimit = submission.GasLimit,
                PriorityFee = submission.PriorityFee,
                Data = System.Text.Encoding.UTF8.GetBytes(submission.TransactionData),
                SubmittedAt = DateTime.UtcNow,
                Priority = 1,
                Status = TransactionStatus.Pending
            };

            // Add to ordering pool
            lock (_poolsLock)
            {
                pool.PendingTransactions.Add(pendingTransaction);
            }

            Logger.LogInformation("Submitted transaction {TransactionId} to pool {PoolId} on {Blockchain}",
                submissionId, pool.Id, blockchainType);

            return submissionId;
        });
    }

    /// <inheritdoc/>
    public async Task<FairOrderingModels.FairOrderingResult> GetOrderingResultAsync(string transactionId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(transactionId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_poolsLock)
            {
                foreach (var result in _recentResults)
                {
                    if (result.TransactionId == transactionId)
                    {
                        return result;
                    }
                }
            }

            throw new ArgumentException($"Transaction {transactionId} not found", nameof(transactionId));
        });
    }

    /// <inheritdoc/>
    public async Task<FairOrderingModels.MevProtectionResult> AnalyzeMevRiskAsync(FairOrderingModels.MevAnalysisRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var analysisId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Analyzing MEV risk {AnalysisId} for transaction {TransactionHash}",
                    analysisId, request.TransactionHash);

                // Analyze MEV risk within the enclave
                var mevRisk = await AnalyzeMevRiskInEnclaveAsync(request);
                var protectionStrategies = await GenerateProtectionStrategiesAsync(mevRisk);

                var result = new FairOrderingModels.MevProtectionResult
                {
                    AnalysisId = analysisId,
                    TransactionHash = request.TransactionHash,
                    MevRiskScore = mevRisk.RiskScore,
                    RiskLevel = CalculateRiskLevel(mevRisk.RiskScore),
                    DetectedThreats = mevRisk.DetectedThreats,
                    ProtectionStrategies = protectionStrategies,
                    AnalyzedAt = DateTime.UtcNow,
                    Success = true
                };

                Logger.LogInformation("MEV analysis {AnalysisId}: Risk {RiskScore:F3}, Level {RiskLevel} on {Blockchain}",
                    analysisId, mevRisk.RiskScore, result.RiskLevel, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to analyze MEV risk {AnalysisId}", analysisId);

                return new FairOrderingModels.MevProtectionResult
                {
                    AnalysisId = analysisId,
                    TransactionHash = request.TransactionHash,
                    Success = false,
                    ErrorMessage = ex.Message,
                    AnalyzedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<FairOrderingModels.FairnessMetrics> GetFairnessMetricsAsync(string poolId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(poolId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            var pool = GetOrderingPool(poolId);

            return new FairOrderingModels.FairnessMetrics
            {
                PoolId = poolId,
                TotalTransactionsProcessed = pool.ProcessedBatches.Sum(b => b.TransactionCount),
                AverageProcessingTime = CalculateAverageProcessingTime(pool),
                FairnessScore = CalculateFairnessScore(pool),
                MevProtectionEffectiveness = CalculateMevProtectionEffectiveness(pool),
                OrderingAlgorithmEfficiency = CalculateOrderingEfficiency(pool),
                MetricsGeneratedAt = DateTime.UtcNow
            };
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FairOrderingModels.OrderingPool>> GetOrderingPoolsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_poolsLock)
            {
                return _orderingPools.Values.Where(p => p.Status == PoolStatus.Active).ToList();
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> UpdatePoolConfigAsync(string poolId, FairOrderingModels.OrderingPoolConfig config, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(poolId);
        ArgumentNullException.ThrowIfNull(config);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_poolsLock)
            {
                if (_orderingPools.TryGetValue(poolId, out var pool))
                {
                    pool.Name = config.Name;
                    pool.Configuration = config;
                    pool.OrderingAlgorithm = config.OrderingAlgorithm;
                    pool.BatchSize = config.BatchSize;
                    pool.MevProtectionEnabled = config.MevProtectionEnabled;
                    pool.FairnessLevel = config.FairnessLevel;
                    pool.UpdatedAt = DateTime.UtcNow;

                    Logger.LogInformation("Updated ordering pool {PoolId} configuration on {Blockchain}", poolId, blockchainType);
                    return true;
                }
            }

            Logger.LogWarning("Ordering pool {PoolId} not found for update on {Blockchain}", poolId, blockchainType);
            return false;
        });
    }

    /// <summary>
    /// Processes ordering pools periodically.
    /// </summary>
    /// <param name="state">Timer state.</param>
    private async void ProcessOrderingPools(object? state)
    {
        try
        {
            var poolsToProcess = new List<FairOrderingModels.OrderingPool>();

            lock (_poolsLock)
            {
                poolsToProcess.AddRange(_orderingPools.Values.Where(p =>
                    p.Status == PoolStatus.Active &&
                    (p.PendingTransactions.Count >= p.BatchSize ||
                     (p.PendingTransactions.Count > 0 && DateTime.UtcNow - p.PendingTransactions.First().SubmittedAt > p.Configuration.BatchTimeout))));
            }

            foreach (var pool in poolsToProcess)
            {
                await ProcessPoolBatchAsync(pool);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing ordering pools");
        }
    }

    /// <summary>
    /// Processes a batch of transactions for an ordering pool.
    /// </summary>
    /// <param name="pool">The ordering pool.</param>
    private async Task ProcessPoolBatchAsync(FairOrderingModels.OrderingPool pool)
    {
        try
        {
            Logger.LogDebug("Processing batch for pool {PoolId} with {TransactionCount} transactions",
                pool.Id, pool.PendingTransactions.Count);

            var batchId = Guid.NewGuid().ToString();
            var transactionsToProcess = new List<FairOrderingModels.PendingTransaction>();

            lock (_poolsLock)
            {
                var batchSize = Math.Min(pool.BatchSize, pool.PendingTransactions.Count);
                transactionsToProcess.AddRange(pool.PendingTransactions.Take(batchSize));
                pool.PendingTransactions.RemoveRange(0, batchSize);
            }

            // Process batch within the enclave
            var orderedTransactions = await OrderTransactionsInEnclaveAsync(pool, transactionsToProcess);

            // Create processed batch
            var processedBatch = new FairOrderingModels.ProcessedBatch
            {
                BatchId = batchId,
                PoolId = pool.Id,
                TransactionCount = orderedTransactions.Count,
                ProcessedAt = DateTime.UtcNow,
                OrderingAlgorithm = pool.OrderingAlgorithm,
                FairnessScore = CalculateBatchFairnessScore(orderedTransactions)
            };

            // Record ordering results
            foreach (var transaction in orderedTransactions)
            {
                var result = new FairOrderingModels.FairOrderingResult
                {
                    TransactionId = transaction.Id,
                    PoolId = pool.Id,
                    BatchId = batchId,
                    OriginalPosition = transactionsToProcess.FindIndex(t => t.Id == transaction.Id),
                    FinalPosition = orderedTransactions.FindIndex(t => t.Id == transaction.Id),
                    OrderingAlgorithm = pool.OrderingAlgorithm,
                    FairnessScore = transaction.FairnessScore,
                    ProcessedAt = DateTime.UtcNow,
                    Success = true
                };

                lock (_poolsLock)
                {
                    _recentResults.Enqueue(result);

                    // Keep only last 10000 results
                    if (_recentResults.Count > 10000)
                    {
                        _recentResults.TryDequeue(out _);
                    }
                }
            }

            lock (_poolsLock)
            {
                pool.ProcessedBatches.Add(processedBatch);

                // Keep only last 1000 batches
                if (pool.ProcessedBatches.Count > 1000)
                {
                    pool.ProcessedBatches.RemoveAt(0);
                }
            }

            Logger.LogInformation("Processed batch {BatchId} for pool {PoolId}: {TransactionCount} transactions ordered",
                batchId, pool.Id, orderedTransactions.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process batch for pool {PoolId}", pool.Id);
        }
    }

    /// <summary>
    /// Gets an ordering pool by ID.
    /// </summary>
    /// <param name="poolId">The pool ID.</param>
    /// <returns>The ordering pool.</returns>
    private FairOrderingModels.OrderingPool GetOrderingPool(string poolId)
    {
        lock (_poolsLock)
        {
            if (_orderingPools.TryGetValue(poolId, out var pool) && pool.Status == PoolStatus.Active)
            {
                return pool;
            }
        }

        throw new ArgumentException($"Ordering pool {poolId} not found", nameof(poolId));
    }

    /// <summary>
    /// Calculates the risk level based on a score.
    /// </summary>
    /// <param name="score">The risk score (0-1).</param>
    /// <returns>The risk level.</returns>
    private FairOrderingModels.RiskLevel CalculateRiskLevel(double score)
    {
        return score switch
        {
            >= 0.8 => FairOrderingModels.RiskLevel.Critical,
            >= 0.6 => FairOrderingModels.RiskLevel.High,
            >= 0.4 => FairOrderingModels.RiskLevel.Medium,
            >= 0.2 => FairOrderingModels.RiskLevel.Low,
            _ => FairOrderingModels.RiskLevel.Minimal
        };
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Fair Ordering Service");

        // Initialize default ordering pools
        await InitializeDefaultPoolsAsync();

        Logger.LogInformation("Fair Ordering Service initialized successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Fair Ordering Service enclave...");

            // Initialize fair ordering specific enclave components
            await InitializeFairOrderingEnclaveAsync();

            Logger.LogInformation("Fair Ordering Service enclave initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Fair Ordering Service enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Fair Ordering Service");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Fair Ordering Service");

        // Dispose timer
        _processingTimer?.Dispose();

        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check fair ordering specific health
        var activePoolCount = _orderingPools.Values.Count(p => p.Status == PoolStatus.Active);
        var totalPendingTransactions = _orderingPools.Values.Sum(p => p.PendingTransactions.Count);

        if (totalPendingTransactions > 10000)
        {
            Logger.LogWarning("High number of pending transactions: {PendingCount}", totalPendingTransactions);
            return Task.FromResult(ServiceHealth.Degraded);
        }

        Logger.LogDebug("Fair Ordering Service health check: {ActivePools} pools, {PendingTransactions} pending",
            activePoolCount, totalPendingTransactions);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _processingTimer?.Dispose();
        base.Dispose();
    }
}
