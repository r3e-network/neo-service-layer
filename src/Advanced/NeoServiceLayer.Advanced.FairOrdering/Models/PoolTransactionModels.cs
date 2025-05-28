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
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
}

/// <summary>
/// Represents ordering pool configuration.
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
    /// Gets or sets the maximum pool size.
    /// </summary>
    public int MaxPoolSize { get; set; } = 1000;

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
    /// Gets or sets the MEV protection level.
    /// </summary>
    public MevProtectionLevel MevProtection { get; set; } = MevProtectionLevel.High;

    /// <summary>
    /// Gets or sets whether MEV protection is enabled.
    /// </summary>
    public bool MevProtectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the fairness level.
    /// </summary>
    public FairnessLevel FairnessLevel { get; set; } = FairnessLevel.High;

    /// <summary>
    /// Gets or sets whether to enable priority fees.
    /// </summary>
    public bool EnablePriorityFees { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum priority fee.
    /// </summary>
    public decimal MaxPriorityFee { get; set; } = 1000;
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
    public long GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the priority fee.
    /// </summary>
    public decimal PriorityFee { get; set; }

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the submission timestamp.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ordering score.
    /// </summary>
    public double OrderingScore { get; set; }

    /// <summary>
    /// Gets or sets the MEV risk score.
    /// </summary>
    public double MevRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the fairness score.
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the transaction priority.
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a transaction submission request.
/// </summary>
public class TransactionSubmission
{
    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    [Required]
    public string TransactionData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    [Required]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    [Required]
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
    public long GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the priority fee.
    /// </summary>
    public decimal PriorityFee { get; set; }

    /// <summary>
    /// Gets or sets the desired fairness level.
    /// </summary>
    public FairnessLevel FairnessLevel { get; set; } = FairnessLevel.Standard;

    /// <summary>
    /// Gets or sets additional submission options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Represents a fair transaction for protected ordering.
/// </summary>
public class FairTransaction
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

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
    /// Gets or sets the transaction data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public decimal GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the protection level.
    /// </summary>
    public string ProtectionLevel { get; set; } = "Standard";

    /// <summary>
    /// Gets or sets the maximum slippage.
    /// </summary>
    public decimal MaxSlippage { get; set; }

    /// <summary>
    /// Gets or sets the earliest execution time.
    /// </summary>
    public DateTime? ExecuteAfter { get; set; }

    /// <summary>
    /// Gets or sets the latest execution time.
    /// </summary>
    public DateTime? ExecuteBefore { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public string Status { get; set; } = "Pending";
}
