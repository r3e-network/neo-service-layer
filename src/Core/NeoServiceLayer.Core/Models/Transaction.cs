using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models;

/// <summary>
/// Represents a blockchain transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block hash containing this transaction.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Gets or sets the transaction index in the block.
    /// </summary>
    public int Index { get; set; }

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
    /// Gets or sets the gas used.
    /// </summary>
    public decimal GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the transaction data/input.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional transaction metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Additional properties for compatibility with Neo client usage
    public string Sender { get => From; set => From = value; }
    public string Recipient { get => To; set => To = value; }
}

/// <summary>
/// Transaction status enumeration.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction was successful.
    /// </summary>
    Success,

    /// <summary>
    /// Transaction failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Transaction was reverted.
    /// </summary>
    Reverted
}