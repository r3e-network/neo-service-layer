using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Advanced.FairOrdering.Models;

/// <summary>
/// Represents an ordering pool for fair transaction ordering.
/// </summary>
public class OrderingPool
{
    /// <summary>
    /// Gets or sets the pool ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pool name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pool configuration.
    /// </summary>
    public OrderingPoolConfig Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the pending transactions.
    /// </summary>
    public List<PendingTransaction> PendingTransactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the pool status.
    /// </summary>
    public PoolStatus Status { get; set; } = PoolStatus.Active;

    /// <summary>
    /// Gets or sets the ordering algorithm.
    /// </summary>
    public OrderingAlgorithm OrderingAlgorithm { get; set; } = OrderingAlgorithm.FairQueue;

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether MEV protection is enabled.
    /// </summary>
    public bool MevProtectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the fairness level.
    /// </summary>
    public FairnessLevel FairnessLevel { get; set; } = FairnessLevel.Standard;

    /// <summary>
    /// Gets or sets the processed batches.
    /// </summary>
    public List<ProcessedBatch> ProcessedBatches { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
}

/// <summary>
/// Configuration for an ordering pool.
/// </summary>
public class OrderingPoolConfig
{
    /// <summary>
    /// Gets or sets the pool name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pool description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordering algorithm.
    /// </summary>
    public OrderingAlgorithm OrderingAlgorithm { get; set; } = OrderingAlgorithm.FairQueue;

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the batch timeout.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the fairness level.
    /// </summary>
    public FairnessLevel FairnessLevel { get; set; } = FairnessLevel.Standard;

    /// <summary>
    /// Gets or sets whether MEV protection is enabled.
    /// </summary>
    public bool MevProtectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum slippage tolerance (as a decimal, e.g., 0.01 = 1%).
    /// </summary>
    public decimal MaxSlippage { get; set; } = 0.005m;

    /// <summary>
    /// Gets or sets additional configuration parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents a transaction submission for fair ordering.
/// </summary>
public class TransactionSubmission
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the gas price.
    /// </summary>
    public decimal GasPrice { get; set; }

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public decimal GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the priority fee.
    /// </summary>
    public decimal PriorityFee { get; set; }

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    public string TransactionData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the transaction was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a pending transaction in the ordering pool.
/// </summary>
public class PendingTransaction
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the gas price.
    /// </summary>
    public decimal GasPrice { get; set; }

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public decimal GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the priority fee.
    /// </summary>
    public decimal PriorityFee { get; set; }

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets when the transaction was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the transaction priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Gets or sets the fairness score.
    /// </summary>
    public double FairnessScore { get; set; }
}

/// <summary>
/// Represents a processed batch of transactions.
/// </summary>
public class ProcessedBatch
{
    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pool ID.
    /// </summary>
    public string PoolId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions in the batch.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets when the batch was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when processing started.
    /// </summary>
    public DateTime ProcessingStarted { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when processing completed.
    /// </summary>
    public DateTime ProcessingCompleted { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ordering algorithm used.
    /// </summary>
    public OrderingAlgorithm OrderingAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the fairness score for the batch.
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the MEV protection effectiveness score.
    /// </summary>
    public double MevProtectionEffectiveness { get; set; }
}


