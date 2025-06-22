using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Advanced.FairOrdering.Models;

/// <summary>
/// Represents a fair transaction in the ordering system.
/// </summary>
public class FairTransaction
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

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
    /// Gets or sets when the transaction was ordered.
    /// </summary>
    public DateTime? OrderedAt { get; set; }

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

    /// <summary>
    /// Gets or sets the MEV protection score.
    /// </summary>
    public double MevProtectionScore { get; set; }

    /// <summary>
    /// Gets or sets the ordering pool ID.
    /// </summary>
    public string? PoolId { get; set; }

    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public string? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the MEV protection level.
    /// </summary>
    public string ProtectionLevel { get; set; } = "Standard";

    /// <summary>
    /// Gets or sets the maximum slippage tolerance.
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
    /// Gets or sets additional transaction metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
