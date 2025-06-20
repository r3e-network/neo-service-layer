using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Advanced.FairOrdering.Models;
using System.Collections.Concurrent;

namespace NeoServiceLayer.Advanced.FairOrdering;

/// <summary>
/// Resilience patterns implementation for the Fair Ordering Service.
/// </summary>
public partial class FairOrderingService
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new();
    private readonly object _circuitBreakerLock = new();

    /// <summary>
    /// Gets or creates a circuit breaker for a specific operation.
    /// </summary>
    /// <param name="operationKey">The operation key.</param>
    /// <param name="failureThreshold">The failure threshold.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The circuit breaker instance.</returns>
    private CircuitBreaker GetOrCreateCircuitBreaker(string operationKey, int failureThreshold = 5, TimeSpan? timeout = null)
    {
        return _circuitBreakers.GetOrAdd(operationKey, _ => new CircuitBreaker(failureThreshold, timeout));
    }

    /// <summary>
    /// Creates an ordering pool with resilience patterns.
    /// </summary>
    /// <param name="config">The ordering pool configuration.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The created pool ID.</returns>
    private async Task<string> CreateOrderingPoolWithResilienceAsync(OrderingPoolConfig config, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker($"CreatePool_{blockchainType}", 3, TimeSpan.FromMinutes(2));
        
        return await ResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                return await ResilienceHelper.ExecuteWithTimeoutAsync(
                    () => CreateOrderingPoolInternalAsync(config, blockchainType),
                    TimeSpan.FromSeconds(30),
                    Logger,
                    "CreateOrderingPool");
            },
            circuitBreaker,
            Logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(200),
            operationName: $"CreateOrderingPool_{blockchainType}");
    }

    /// <summary>
    /// Internal method to create ordering pool (extracted for resilience wrapping).
    /// </summary>
    /// <param name="config">The ordering pool configuration.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The created pool ID.</returns>
    private async Task<string> CreateOrderingPoolInternalAsync(OrderingPoolConfig config, BlockchainType blockchainType)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var poolId = Guid.NewGuid().ToString();

            var pool = new OrderingPool
            {
                Id = poolId,
                Name = config.Name,
                Configuration = config,
                OrderingAlgorithm = config.OrderingAlgorithm,
                BatchSize = config.BatchSize,
                MevProtectionEnabled = config.MevProtectionEnabled,
                FairnessLevel = config.FairnessLevel,
                CreatedAt = DateTime.UtcNow,
                Status = PoolStatus.Active,
                PendingTransactions = new List<PendingTransaction>(),
                ProcessedBatches = new List<ProcessedBatch>(),
                BlockchainType = blockchainType
            };

            lock (_poolsLock)
            {
                _orderingPools[poolId] = pool;
            }

            Logger.LogInformation("Created ordering pool {PoolId} ({Name}) with algorithm {Algorithm} on {Blockchain}",
                poolId, config.Name, config.OrderingAlgorithm, blockchainType);

            return poolId;
        });
    }

    /// <summary>
    /// Submits a fair transaction with resilience patterns.
    /// </summary>
    /// <param name="request">The fair transaction request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The transaction ID.</returns>
    private async Task<string> SubmitFairTransactionWithResilienceAsync(FairTransactionRequest request, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker($"SubmitTransaction_{blockchainType}", 5, TimeSpan.FromMinutes(1));
        
        return await ResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                return await ResilienceHelper.ExecuteWithTimeoutAsync(
                    () => SubmitFairTransactionInternalAsync(request, blockchainType),
                    TimeSpan.FromSeconds(15),
                    Logger,
                    "SubmitFairTransaction");
            },
            circuitBreaker,
            Logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            operationName: $"SubmitFairTransaction_{blockchainType}");
    }

    /// <summary>
    /// Internal method to submit fair transaction (extracted for resilience wrapping).
    /// </summary>
    /// <param name="request">The fair transaction request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The transaction ID.</returns>
    private async Task<string> SubmitFairTransactionInternalAsync(FairTransactionRequest request, BlockchainType blockchainType)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var transactionId = Guid.NewGuid().ToString();

            Logger.LogInformation("Submitting fair transaction {TransactionId} from {From} to {To} on {Blockchain}",
                transactionId, request.From, request.To, blockchainType);

            // Validate transaction parameters
            if (string.IsNullOrEmpty(request.From) || string.IsNullOrEmpty(request.To))
            {
                throw new ArgumentException("From and To addresses are required");
            }

            if (request.Value < 0 || request.GasLimit <= 0)
            {
                throw new ArgumentException("Invalid transaction parameters");
            }

            // Create fair transaction entry
            var fairTransaction = new FairTransaction
            {
                TransactionId = transactionId,
                From = request.From,
                To = request.To,
                Value = request.Value,
                Data = System.Text.Encoding.UTF8.GetBytes(request.Data ?? string.Empty),
                GasLimit = request.GasLimit,
                ProtectionLevel = request.ProtectionLevel,
                MaxSlippage = request.MaxSlippage,
                ExecuteAfter = request.ExecuteAfter,
                ExecuteBefore = request.ExecuteBefore,
                SubmittedAt = DateTime.UtcNow,
                Status = TransactionStatus.Pending
            };

            // Store transaction for fair ordering processing with resilience
            await StoreFairTransactionWithResilienceAsync(fairTransaction);

            Logger.LogInformation("Fair transaction {TransactionId} submitted successfully on {Blockchain}",
                transactionId, blockchainType);

            return transactionId;
        });
    }

    /// <summary>
    /// Analyzes fairness risk with resilience patterns.
    /// </summary>
    /// <param name="request">The transaction analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fairness analysis result.</returns>
    private async Task<FairnessAnalysisResult> AnalyzeFairnessRiskWithResilienceAsync(TransactionAnalysisRequest request, BlockchainType blockchainType)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker($"FairnessAnalysis_{blockchainType}", 3, TimeSpan.FromMinutes(2));
        
        return await ResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync(
            async () =>
            {
                return await ResilienceHelper.ExecuteWithTimeoutAsync(
                    () => AnalyzeFairnessRiskInternalAsync(request, blockchainType),
                    TimeSpan.FromSeconds(20),
                    Logger,
                    "AnalyzeFairnessRisk");
            },
            circuitBreaker,
            Logger,
            maxRetries: 2,
            baseDelay: TimeSpan.FromMilliseconds(300),
            operationName: $"AnalyzeFairnessRisk_{blockchainType}");
    }

    /// <summary>
    /// Internal method to analyze fairness risk (extracted for resilience wrapping).
    /// </summary>
    /// <param name="request">The transaction analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fairness analysis result.</returns>
    private async Task<FairnessAnalysisResult> AnalyzeFairnessRiskInternalAsync(TransactionAnalysisRequest request, BlockchainType blockchainType)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var analysisId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Analyzing fairness risk {AnalysisId} for transaction from {From} to {To}",
                    analysisId, request.From, request.To);

                // Validate analysis request
                if (string.IsNullOrEmpty(request.TransactionData))
                {
                    throw new ArgumentException("Transaction data is required for analysis");
                }

                // Perform comprehensive fairness analysis
                var riskFactors = new List<string>();
                var recommendations = new List<string>();
                decimal estimatedMev = 0m;
                string riskLevel = "Low";

                // Analyze transaction value and patterns
                if (request.Value > 1000000m) // Large transaction
                {
                    riskFactors.Add("Large transaction value detected");
                    estimatedMev += request.Value * 0.001m; // 0.1% potential MEV
                    riskLevel = "Medium";
                    recommendations.Add("Consider splitting large transactions");
                }

                // Analyze gas price patterns (for NeoX)
                if (blockchainType == BlockchainType.NeoX)
                {
                    var gasAnalysis = await AnalyzeGasPatternsWithResilienceAsync(request);
                    if (gasAnalysis.IsHighPriority)
                    {
                        riskFactors.Add("High gas price detected - potential front-running target");
                        estimatedMev += gasAnalysis.EstimatedMevExposure;
                        riskLevel = gasAnalysis.EstimatedMevExposure > 10m ? "High" : "Medium";
                        recommendations.Add("Use fair ordering protection to prevent front-running");
                    }
                }

                // Analyze transaction timing
                var timingAnalysis = AnalyzeTransactionTiming(request);
                if (timingAnalysis.IsSuspicious)
                {
                    riskFactors.Add("Suspicious transaction timing detected");
                    riskLevel = "High";
                    recommendations.Add("Delay transaction execution to avoid timing attacks");
                }

                // Analyze contract interactions
                if (!string.IsNullOrEmpty(request.To) && IsContractAddress(request.To))
                {
                    var contractAnalysis = await AnalyzeContractInteractionWithResilienceAsync(request);
                    if (contractAnalysis.HasMevRisk)
                    {
                        riskFactors.AddRange(contractAnalysis.RiskFactors);
                        estimatedMev += contractAnalysis.EstimatedMev;
                        if (contractAnalysis.RiskLevel == "Critical")
                        {
                            riskLevel = "Critical";
                        }
                        recommendations.AddRange(contractAnalysis.Recommendations);
                    }
                }

                // Calculate protection fee based on risk and value
                decimal protectionFee = CalculateProtectionFee(request.Value, estimatedMev, riskLevel);

                var result = new FairnessAnalysisResult
                {
                    TransactionHash = !string.IsNullOrEmpty(request.TransactionData) ?
                        ComputeTransactionHash(request.TransactionData) : string.Empty,
                    RiskLevel = riskLevel,
                    EstimatedMEV = estimatedMev,
                    DetectedRisks = riskFactors.Count > 0 ? riskFactors.ToArray() : new[] { "No significant risks detected" },
                    Recommendations = recommendations.Count > 0 ? recommendations.ToArray() : new[] { "Transaction appears fair" },
                    ProtectionFee = protectionFee,
                    AnalyzedAt = DateTime.UtcNow
                };

                Logger.LogInformation("Fairness analysis {AnalysisId}: Risk {RiskLevel}, MEV {EstimatedMev:F4}, Fee {ProtectionFee:F6} on {Blockchain}",
                    analysisId, result.RiskLevel, result.EstimatedMEV, result.ProtectionFee, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to analyze fairness risk {AnalysisId}", analysisId);

                return new FairnessAnalysisResult
                {
                    TransactionHash = string.Empty,
                    RiskLevel = "Error",
                    EstimatedMEV = 0.0m,
                    DetectedRisks = new[] { $"Analysis failed: {ex.Message}" },
                    Recommendations = new[] { "Unable to analyze transaction - proceed with caution" },
                    ProtectionFee = 0.0m,
                    AnalyzedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <summary>
    /// Stores a fair transaction with resilience patterns.
    /// </summary>
    /// <param name="transaction">The fair transaction to store.</param>
    private async Task StoreFairTransactionWithResilienceAsync(FairTransaction transaction)
    {
        var circuitBreaker = GetOrCreateCircuitBreaker("StorageOperations", 5, TimeSpan.FromMinutes(1));

        await ResilienceHelper.ExecuteWithRetryAndCircuitBreakerAsync<bool>(
            async () =>
            {
                try
                {
                    if (StorageProvider != null)
                    {
                        var key = $"fair_transaction_{transaction.TransactionId}";
                        var data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(transaction);
                        await StorageProvider.StoreAsync(key, data);
                    }

                    Logger.LogDebug("Stored fair transaction {TransactionId}", transaction.TransactionId);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to store fair transaction {TransactionId}", transaction.TransactionId);
                    throw;
                }
            },
            circuitBreaker,
            Logger,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            operationName: "StoreFairTransaction");
    }

    /// <summary>
    /// Analyzes gas patterns with resilience.
    /// </summary>
    /// <param name="request">The transaction analysis request.</param>
    /// <returns>Gas analysis result.</returns>
    private async Task<(bool IsHighPriority, decimal EstimatedMevExposure)> AnalyzeGasPatternsWithResilienceAsync(TransactionAnalysisRequest request)
    {
        return await ResilienceHelper.ExecuteWithRetryAsync(
            async () => await AnalyzeGasPatternsAsync(request),
            Logger,
            maxRetries: 2,
            baseDelay: TimeSpan.FromMilliseconds(50),
            operationName: "AnalyzeGasPatterns");
    }

    /// <summary>
    /// Analyzes contract interaction with resilience.
    /// </summary>
    /// <param name="request">The transaction analysis request.</param>
    /// <returns>Contract analysis result.</returns>
    private async Task<(bool HasMevRisk, List<string> RiskFactors, decimal EstimatedMev, string RiskLevel, List<string> Recommendations)> AnalyzeContractInteractionWithResilienceAsync(TransactionAnalysisRequest request)
    {
        return await ResilienceHelper.ExecuteWithRetryAsync(
            async () => await AnalyzeContractInteractionAsync(request),
            Logger,
            maxRetries: 2,
            baseDelay: TimeSpan.FromMilliseconds(100),
            operationName: "AnalyzeContractInteraction");
    }

    /// <summary>
    /// Gets circuit breaker status for monitoring.
    /// </summary>
    /// <returns>Circuit breaker status information.</returns>
    public Dictionary<string, object> GetCircuitBreakerStatus()
    {
        var status = new Dictionary<string, object>();

        foreach (var kvp in _circuitBreakers)
        {
            status[kvp.Key] = new
            {
                State = kvp.Value.State.ToString(),
                FailureCount = kvp.Value.FailureCount,
                NextAttemptTime = kvp.Value.NextAttemptTime
            };
        }

        return status;
    }

    /// <summary>
    /// Resets all circuit breakers (for administrative purposes).
    /// </summary>
    public void ResetCircuitBreakers()
    {
        lock (_circuitBreakerLock)
        {
            foreach (var circuitBreaker in _circuitBreakers.Values)
            {
                circuitBreaker.Reset();
            }
            
            Logger.LogInformation("All circuit breakers have been reset");
        }
    }
}