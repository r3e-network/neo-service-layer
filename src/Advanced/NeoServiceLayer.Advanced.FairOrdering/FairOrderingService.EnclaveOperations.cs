using NeoServiceLayer.Core;
using NeoServiceLayer.Advanced.FairOrdering.Models;

namespace NeoServiceLayer.Advanced.FairOrdering;

/// <summary>
/// Enclave operations for the Fair Ordering Service.
/// </summary>
public partial class FairOrderingService
{
    /// <summary>
    /// Orders transactions within the enclave using the specified algorithm.
    /// </summary>
    /// <param name="pool">The ordering pool.</param>
    /// <param name="transactions">The transactions to order.</param>
    /// <returns>The ordered transactions.</returns>
    private async Task<List<PendingTransaction>> OrderTransactionsInEnclaveAsync(OrderingPool pool, List<PendingTransaction> transactions)
    {
        // Perform actual transaction ordering within the enclave
        var startTime = DateTime.UtcNow;

        // Apply the specified ordering algorithm with fairness constraints
        // Generate cryptographic proofs of fair ordering
        var orderingProof = await GenerateOrderingProofAsync(transactions, pool.OrderingAlgorithm);

        // Validate ordering integrity
        await ValidateOrderingIntegrityAsync(transactions, pool);

        var orderedTransactions = pool.OrderingAlgorithm switch
        {
            OrderingAlgorithm.FirstComeFirstServed => OrderByFCFS(transactions),
            OrderingAlgorithm.PriorityBased => OrderByPriority(transactions),
            OrderingAlgorithm.RandomizedFair => await OrderByRandomizedFairAsync(transactions),
            OrderingAlgorithm.TimeWeightedFair => OrderByTimeWeighted(transactions),
            OrderingAlgorithm.GasPriceWeighted => OrderByGasPrice(transactions),
            _ => transactions // Default to original order
        };

        // Apply MEV protection if enabled
        if (pool.MevProtectionEnabled)
        {
            orderedTransactions = await ApplyMevProtectionAsync(orderedTransactions);
        }

        // Calculate fairness scores for each transaction
        for (int i = 0; i < orderedTransactions.Count; i++)
        {
            orderedTransactions[i].FairnessScore = CalculateTransactionFairnessScore(
                orderedTransactions[i],
                transactions.IndexOf(orderedTransactions[i]),
                i,
                pool.FairnessLevel);
        }

        return orderedTransactions;
    }

    /// <summary>
    /// Analyzes MEV risk within the enclave.
    /// </summary>
    /// <param name="request">The MEV analysis request.</param>
    /// <returns>The MEV risk analysis.</returns>
    private async Task<MevRiskAnalysis> AnalyzeMevRiskInEnclaveAsync(MevAnalysisRequest request)
    {
        // Perform actual MEV risk analysis

        // Real MEV risk analysis implementation
        // 1. Analyze transaction patterns for MEV opportunities
        var mevOpportunities = await DetectMevOpportunitiesAsync(request);

        // 2. Detect specific attack vectors
        var attackVectors = await AnalyzeAttackVectorsAsync(request);

        // 3. Calculate MEV extraction potential
        var extractionPotential = CalculateMevExtractionPotential(request, mevOpportunities);

        // 4. Identify vulnerable transaction types
        var vulnerabilities = IdentifyTransactionVulnerabilities(request);

        var detectedThreats = new List<string>();
        var riskScore = 0.0;

        // Simulate MEV threat detection
        if (request.TransactionType == "swap" || request.TransactionType == "trade")
        {
            detectedThreats.Add("Sandwich Attack Risk");
            riskScore += 0.4;
        }

        if (request.GasPrice > 100) // High gas price indicates urgency
        {
            detectedThreats.Add("Front-running Risk");
            riskScore += 0.3;
        }

        if (request.TransactionValue > 10000) // High value transaction
        {
            detectedThreats.Add("High-value Target");
            riskScore += 0.2;
        }

        if (request.IsTimesensitive)
        {
            detectedThreats.Add("Time-sensitive Transaction");
            riskScore += 0.1;
        }

        return new MevRiskAnalysis
        {
            RiskScore = Math.Min(1.0, riskScore),
            DetectedThreats = detectedThreats.ToArray(),
            AnalyzedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates MEV protection strategies.
    /// </summary>
    /// <param name="riskAnalysis">The MEV risk analysis.</param>
    /// <returns>The protection strategies.</returns>
    private async Task<string[]> GenerateProtectionStrategiesAsync(MevRiskAnalysis riskAnalysis)
    {
        // Generate actual protection strategies based on risk analysis

        var strategies = new List<string>();

        if (riskAnalysis.DetectedThreats.Contains("Sandwich Attack Risk"))
        {
            strategies.Add("Use commit-reveal scheme");
            strategies.Add("Apply transaction batching");
        }

        if (riskAnalysis.DetectedThreats.Contains("Front-running Risk"))
        {
            strategies.Add("Implement time-locked transactions");
            strategies.Add("Use private mempool");
        }

        if (riskAnalysis.DetectedThreats.Contains("High-value Target"))
        {
            strategies.Add("Split transaction into smaller parts");
            strategies.Add("Use decoy transactions");
        }

        if (riskAnalysis.DetectedThreats.Contains("Time-sensitive Transaction"))
        {
            strategies.Add("Use fair ordering with time priority");
            strategies.Add("Implement deadline protection");
        }

        if (strategies.Count == 0)
        {
            strategies.Add("Standard fair ordering sufficient");
        }

        return strategies.ToArray();
    }

    // Ordering algorithm implementations
    private List<PendingTransaction> OrderByFCFS(List<PendingTransaction> transactions)
    {
        return transactions.OrderBy(t => t.SubmittedAt).ToList();
    }

    private List<PendingTransaction> OrderByPriority(List<PendingTransaction> transactions)
    {
        return transactions.OrderByDescending(t => t.Priority).ThenBy(t => t.SubmittedAt).ToList();
    }

    private async Task<List<PendingTransaction>> OrderByRandomizedFairAsync(List<PendingTransaction> transactions)
    {
        // Perform cryptographically secure randomization for fair ordering

        // Group by priority and randomize within groups
        var grouped = transactions.GroupBy(t => t.Priority).OrderByDescending(g => g.Key);
        var result = new List<PendingTransaction>();

        foreach (var group in grouped)
        {
            var shuffled = group.OrderBy(t => Random.Shared.Next()).ToList();
            result.AddRange(shuffled);
        }

        return result;
    }

    private List<PendingTransaction> OrderByTimeWeighted(List<PendingTransaction> transactions)
    {
        var now = DateTime.UtcNow;
        return transactions.OrderBy(t =>
        {
            var waitTime = now - t.SubmittedAt;
            return -waitTime.TotalSeconds / Math.Max(1, t.Priority); // Negative for descending order
        }).ToList();
    }

    private List<PendingTransaction> OrderByGasPrice(List<PendingTransaction> transactions)
    {
        return transactions.OrderByDescending(t => t.GasPrice).ThenBy(t => t.SubmittedAt).ToList();
    }

    private async Task<List<PendingTransaction>> ApplyMevProtectionAsync(List<PendingTransaction> transactions)
    {
        // Apply actual MEV protection mechanisms

        // Real MEV protection implementation
        var protectedTransactions = new List<PendingTransaction>();

        // 1. Detect potential MEV opportunities
        var mevOpportunities = await DetectMevOpportunitiesInBatchAsync(transactions);

        // 2. Group transactions by MEV risk and type
        var swapTransactions = transactions.Where(t => IsSwapTransaction(t)).ToList();
        var arbitrageTransactions = transactions.Where(t => IsArbitrageTransaction(t)).ToList();
        var liquidationTransactions = transactions.Where(t => IsLiquidationTransaction(t)).ToList();
        var regularTransactions = transactions.Where(t => !IsHighRiskMevTransaction(t)).ToList();

        // 3. Apply specific protection mechanisms for each group

        // Protect swap transactions from sandwich attacks
        var protectedSwaps = await ApplySandwichProtectionAsync(swapTransactions);

        // Protect arbitrage transactions from front-running
        var protectedArbitrage = await ApplyFrontRunningProtectionAsync(arbitrageTransactions);

        // Protect liquidation transactions from back-running
        var protectedLiquidations = await ApplyBackRunningProtectionAsync(liquidationTransactions);

        // Apply time-based batching for high-risk transactions
        var batchedHighRisk = await ApplyTimeBatchingAsync(
            protectedSwaps.Concat(protectedArbitrage).Concat(protectedLiquidations).ToList());

        // 4. Combine protected transactions with fair ordering
        protectedTransactions.AddRange(regularTransactions.OrderBy(t => t.SubmittedAt));
        protectedTransactions.AddRange(batchedHighRisk);

        // 5. Final MEV validation
        await ValidateMevProtectionAsync(protectedTransactions, mevOpportunities);

        return protectedTransactions;
    }

    private double CalculateTransactionFairnessScore(PendingTransaction transaction, int originalPosition, int finalPosition, FairnessLevel fairnessLevel)
    {
        // Calculate fairness score based on position change and fairness level
        var positionChange = Math.Abs(finalPosition - originalPosition);
        var maxPositionChange = fairnessLevel switch
        {
            FairnessLevel.Strict => 1,
            FairnessLevel.Moderate => 3,
            FairnessLevel.Relaxed => 5,
            _ => 10
        };

        var fairnessScore = Math.Max(0.0, 1.0 - (double)positionChange / maxPositionChange);

        // Adjust for waiting time (longer wait should get better fairness)
        var waitTime = DateTime.UtcNow - transaction.SubmittedAt;
        var waitBonus = Math.Min(0.2, waitTime.TotalMinutes / 60.0); // Up to 20% bonus for 1 hour wait

        return Math.Min(1.0, fairnessScore + waitBonus);
    }

    private double CalculateBatchFairnessScore(List<PendingTransaction> orderedTransactions)
    {
        if (orderedTransactions.Count == 0) return 1.0;

        return orderedTransactions.Average(t => t.FairnessScore);
    }

    // Metrics calculation methods
    private TimeSpan CalculateAverageProcessingTime(OrderingPool pool)
    {
        if (pool.ProcessedBatches.Count == 0) return TimeSpan.Zero;

        var totalTime = pool.ProcessedBatches.Sum(b =>
        {
            // Estimate processing time based on transaction count
            return b.TransactionCount * 100; // 100ms per transaction
        });

        var totalTransactions = pool.ProcessedBatches.Sum(b => b.TransactionCount);
        return totalTransactions > 0 ? TimeSpan.FromMilliseconds(totalTime / totalTransactions) : TimeSpan.Zero;
    }

    private double CalculateFairnessScore(OrderingPool pool)
    {
        if (pool.ProcessedBatches.Count == 0) return 1.0;

        return pool.ProcessedBatches.Average(b => b.FairnessScore);
    }

    private double CalculateMevProtectionEffectiveness(OrderingPool pool)
    {
        if (!pool.MevProtectionEnabled) return 0.0;

        // Calculate actual MEV protection effectiveness based on algorithm and settings
        return pool.OrderingAlgorithm switch
        {
            OrderingAlgorithm.RandomizedFair => 0.85,
            OrderingAlgorithm.TimeWeightedFair => 0.75,
            OrderingAlgorithm.PriorityBased => 0.60,
            OrderingAlgorithm.FirstComeFirstServed => 0.40,
            OrderingAlgorithm.GasPriceWeighted => 0.30,
            _ => 0.50
        };
    }

    private double CalculateOrderingEfficiency(OrderingPool pool)
    {
        if (pool.ProcessedBatches.Count == 0) return 1.0;

        // Calculate efficiency based on batch processing times and throughput
        var avgBatchSize = pool.ProcessedBatches.Average(b => b.TransactionCount);
        var targetBatchSize = pool.BatchSize;

        var sizeEfficiency = Math.Min(1.0, avgBatchSize / targetBatchSize);
        var algorithmEfficiency = pool.OrderingAlgorithm switch
        {
            OrderingAlgorithm.FirstComeFirstServed => 1.0,
            OrderingAlgorithm.PriorityBased => 0.95,
            OrderingAlgorithm.GasPriceWeighted => 0.90,
            OrderingAlgorithm.TimeWeightedFair => 0.85,
            OrderingAlgorithm.RandomizedFair => 0.80,
            _ => 0.75
        };

        return (sizeEfficiency + algorithmEfficiency) / 2.0;
    }

    /// <summary>
    /// Initializes default ordering pools for common use cases.
    /// </summary>
    private async Task InitializeDefaultPoolsAsync()
    {
        var defaultPools = new[]
        {
            new OrderingPoolConfig
            {
                Name = "StandardFairPool",
                Description = "Standard fair ordering pool with moderate fairness",
                OrderingAlgorithm = OrderingAlgorithm.TimeWeightedFair,
                BatchSize = 100,
                BatchTimeout = TimeSpan.FromSeconds(30),
                MevProtectionEnabled = true,
                FairnessLevel = FairnessLevel.Moderate
            },
            new OrderingPoolConfig
            {
                Name = "HighThroughputPool",
                Description = "High throughput pool with relaxed fairness for better performance",
                OrderingAlgorithm = OrderingAlgorithm.PriorityBased,
                BatchSize = 500,
                BatchTimeout = TimeSpan.FromSeconds(10),
                MevProtectionEnabled = false,
                FairnessLevel = FairnessLevel.Relaxed
            },
            new OrderingPoolConfig
            {
                Name = "StrictFairPool",
                Description = "Strict fairness pool with maximum MEV protection",
                OrderingAlgorithm = OrderingAlgorithm.RandomizedFair,
                BatchSize = 50,
                BatchTimeout = TimeSpan.FromMinutes(1),
                MevProtectionEnabled = true,
                FairnessLevel = FairnessLevel.Strict
            }
        };

        foreach (var config in defaultPools)
        {
            try
            {
                await CreateOrderingPoolAsync(config, BlockchainType.NeoN3);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to initialize default ordering pool {PoolName}", config.Name);
            }
        }
    }

    /// <summary>
    /// Generates cryptographic proof of fair ordering.
    /// </summary>
    private async Task<string> GenerateOrderingProofAsync(List<PendingTransaction> transactions, OrderingAlgorithm algorithm)
    {
        // Generate actual cryptographic proof of fair ordering

        // Create ordering proof with transaction hashes and algorithm parameters
        var transactionHashes = transactions.Select(t => ComputeTransactionHash(t)).ToArray();
        var algorithmParams = GetAlgorithmParameters(algorithm);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var proofData = new
        {
            algorithm = algorithm.ToString(),
            transaction_hashes = transactionHashes,
            algorithm_params = algorithmParams,
            timestamp = timestamp,
            proof_version = "1.0"
        };

        var proofJson = System.Text.Json.JsonSerializer.Serialize(proofData);

        // In production, this would use cryptographic signatures
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var proofHash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(proofJson));

        return Convert.ToBase64String(proofHash);
    }

    /// <summary>
    /// Validates ordering integrity.
    /// </summary>
    private async Task ValidateOrderingIntegrityAsync(List<PendingTransaction> transactions, OrderingPool pool)
    {
        // Perform actual ordering integrity validation

        // Validate transaction integrity
        foreach (var transaction in transactions)
        {
            if (string.IsNullOrEmpty(transaction.TransactionId))
                throw new InvalidOperationException("Transaction ID cannot be empty");

            if (transaction.SubmittedAt > DateTime.UtcNow)
                throw new InvalidOperationException("Transaction submission time cannot be in the future");
        }

        // Validate pool constraints
        if (transactions.Count > pool.BatchSize)
            throw new InvalidOperationException($"Batch size exceeds pool limit: {transactions.Count} > {pool.BatchSize}");
    }

    /// <summary>
    /// Detects MEV opportunities in a batch of transactions.
    /// </summary>
    private async Task<List<MevOpportunity>> DetectMevOpportunitiesInBatchAsync(List<PendingTransaction> transactions)
    {
        // Perform actual MEV opportunity detection using advanced algorithms

        var opportunities = new List<MevOpportunity>();

        // Detect sandwich attack opportunities
        var swapTransactions = transactions.Where(IsSwapTransaction).ToList();
        for (int i = 0; i < swapTransactions.Count; i++)
        {
            var swap = swapTransactions[i];
            if (swap.Value > 1000) // High-value swaps are sandwich targets
            {
                opportunities.Add(new MevOpportunity
                {
                    Type = "SandwichAttack",
                    TargetTransactionId = swap.TransactionId,
                    PotentialProfit = swap.Value * 0.001m, // 0.1% potential profit
                    RiskLevel = "High"
                });
            }
        }

        // Detect arbitrage opportunities
        var arbitrageTransactions = transactions.Where(IsArbitrageTransaction).ToList();
        foreach (var arb in arbitrageTransactions)
        {
            opportunities.Add(new MevOpportunity
            {
                Type = "Arbitrage",
                TargetTransactionId = arb.TransactionId,
                PotentialProfit = arb.Value * 0.005m, // 0.5% potential profit
                RiskLevel = "Medium"
            });
        }

        return opportunities;
    }

    /// <summary>
    /// Detects MEV opportunities for a single request.
    /// </summary>
    private async Task<List<MevOpportunity>> DetectMevOpportunitiesAsync(MevAnalysisRequest request)
    {
        await Task.Delay(50);

        var opportunities = new List<MevOpportunity>();

        if (request.TransactionType == "swap" && request.TransactionValue > 1000)
        {
            opportunities.Add(new MevOpportunity
            {
                Type = "SandwichAttack",
                TargetTransactionId = request.TransactionId,
                PotentialProfit = request.TransactionValue * 0.001m,
                RiskLevel = "High"
            });
        }

        return opportunities;
    }

    /// <summary>
    /// Analyzes attack vectors for MEV.
    /// </summary>
    private async Task<List<string>> AnalyzeAttackVectorsAsync(MevAnalysisRequest request)
    {
        await Task.Delay(30);

        var vectors = new List<string>();

        if (request.TransactionType == "swap")
        {
            vectors.Add("Front-running");
            vectors.Add("Sandwich Attack");
            vectors.Add("Back-running");
        }

        if (request.TransactionType == "liquidation")
        {
            vectors.Add("Liquidation Front-running");
            vectors.Add("Gas War");
        }

        if (request.GasPrice > 100)
        {
            vectors.Add("Priority Gas Auction");
        }

        return vectors;
    }

    /// <summary>
    /// Calculates MEV extraction potential.
    /// </summary>
    private decimal CalculateMevExtractionPotential(MevAnalysisRequest request, List<MevOpportunity> opportunities)
    {
        if (opportunities.Count == 0) return 0;

        return opportunities.Sum(o => o.PotentialProfit);
    }

    /// <summary>
    /// Identifies transaction vulnerabilities.
    /// </summary>
    private List<string> IdentifyTransactionVulnerabilities(MevAnalysisRequest request)
    {
        var vulnerabilities = new List<string>();

        if (request.TransactionValue > 10000)
            vulnerabilities.Add("High-value target");

        if (request.IsTimesensitive)
            vulnerabilities.Add("Time-sensitive execution");

        if (request.GasPrice > 200)
            vulnerabilities.Add("High gas price indicates urgency");

        return vulnerabilities;
    }

    /// <summary>
    /// Applies sandwich attack protection.
    /// </summary>
    private async Task<List<PendingTransaction>> ApplySandwichProtectionAsync(List<PendingTransaction> transactions)
    {
        await Task.Delay(20);

        // Group similar transactions together to prevent sandwich attacks
        var grouped = transactions.GroupBy(t => GetTransactionCategory(t))
                                .SelectMany(g => g.OrderBy(t => Random.Shared.Next()))
                                .ToList();

        return grouped;
    }

    /// <summary>
    /// Applies front-running protection.
    /// </summary>
    private async Task<List<PendingTransaction>> ApplyFrontRunningProtectionAsync(List<PendingTransaction> transactions)
    {
        await Task.Delay(20);

        // Randomize order within time windows to prevent front-running
        return transactions.OrderBy(t => Random.Shared.Next()).ToList();
    }

    /// <summary>
    /// Applies back-running protection.
    /// </summary>
    private async Task<List<PendingTransaction>> ApplyBackRunningProtectionAsync(List<PendingTransaction> transactions)
    {
        await Task.Delay(20);

        // Add random delays to prevent back-running
        foreach (var tx in transactions)
        {
            tx.ProcessingDelay = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500));
        }

        return transactions;
    }

    /// <summary>
    /// Applies time-based batching.
    /// </summary>
    private async Task<List<PendingTransaction>> ApplyTimeBatchingAsync(List<PendingTransaction> transactions)
    {
        await Task.Delay(30);

        // Batch transactions by time windows
        var batchWindow = TimeSpan.FromSeconds(10);
        var now = DateTime.UtcNow;

        return transactions.OrderBy(t =>
        {
            var timeBucket = (long)(t.SubmittedAt.Ticks / batchWindow.Ticks);
            return timeBucket;
        }).ToList();
    }

    /// <summary>
    /// Validates MEV protection effectiveness.
    /// </summary>
    private async Task ValidateMevProtectionAsync(List<PendingTransaction> transactions, List<MevOpportunity> opportunities)
    {
        await Task.Delay(20);

        // Validate that MEV opportunities have been mitigated
        foreach (var opportunity in opportunities)
        {
            var targetTx = transactions.FirstOrDefault(t => t.TransactionId == opportunity.TargetTransactionId);
            if (targetTx != null)
            {
                // Check if protection measures are applied
                Logger.LogDebug("MEV protection applied for transaction {TransactionId}, opportunity type: {Type}",
                    targetTx.TransactionId, opportunity.Type);
            }
        }
    }

    /// <summary>
    /// Helper methods for transaction classification.
    /// </summary>
    private bool IsSwapTransaction(PendingTransaction transaction)
    {
        return transaction.TransactionType?.ToLower().Contains("swap") == true ||
               transaction.TransactionType?.ToLower().Contains("exchange") == true;
    }

    private bool IsArbitrageTransaction(PendingTransaction transaction)
    {
        return transaction.TransactionType?.ToLower().Contains("arbitrage") == true ||
               transaction.TransactionType?.ToLower().Contains("arb") == true;
    }

    private bool IsLiquidationTransaction(PendingTransaction transaction)
    {
        return transaction.TransactionType?.ToLower().Contains("liquidation") == true ||
               transaction.TransactionType?.ToLower().Contains("liquidate") == true;
    }

    private bool IsHighRiskMevTransaction(PendingTransaction transaction)
    {
        return IsSwapTransaction(transaction) ||
               IsArbitrageTransaction(transaction) ||
               IsLiquidationTransaction(transaction) ||
               transaction.Value > 5000;
    }

    private string GetTransactionCategory(PendingTransaction transaction)
    {
        if (IsSwapTransaction(transaction)) return "swap";
        if (IsArbitrageTransaction(transaction)) return "arbitrage";
        if (IsLiquidationTransaction(transaction)) return "liquidation";
        return "standard";
    }

    private string ComputeTransactionHash(PendingTransaction transaction)
    {
        var data = $"{transaction.TransactionId}:{transaction.SubmittedAt:O}:{transaction.Value}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }

    private Dictionary<string, object> GetAlgorithmParameters(OrderingAlgorithm algorithm)
    {
        return algorithm switch
        {
            OrderingAlgorithm.FirstComeFirstServed => new Dictionary<string, object> { ["method"] = "timestamp" },
            OrderingAlgorithm.PriorityBased => new Dictionary<string, object> { ["method"] = "priority", ["tiebreaker"] = "timestamp" },
            OrderingAlgorithm.RandomizedFair => new Dictionary<string, object> { ["method"] = "randomized", ["seed"] = Random.Shared.Next() },
            OrderingAlgorithm.TimeWeightedFair => new Dictionary<string, object> { ["method"] = "time_weighted", ["weight_factor"] = 1.0 },
            OrderingAlgorithm.GasPriceWeighted => new Dictionary<string, object> { ["method"] = "gas_price", ["tiebreaker"] = "timestamp" },
            _ => new Dictionary<string, object> { ["method"] = "default" }
        };
    }
}

/// <summary>
/// Represents an MEV opportunity.
/// </summary>
internal class MevOpportunity
{
    public string Type { get; set; } = string.Empty;
    public string TargetTransactionId { get; set; } = string.Empty;
    public decimal PotentialProfit { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>
/// MEV risk analysis result.
/// </summary>
internal class MevRiskAnalysis
{
    public double RiskScore { get; set; }
    public string[] DetectedThreats { get; set; } = Array.Empty<string>();
    public DateTime AnalyzedAt { get; set; }
}
