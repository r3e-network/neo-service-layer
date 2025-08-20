using System.Collections.Generic;
using System;

namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Mock blockchain block for testing.
/// </summary>
public class TestBlock
{
    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block number/index.
    /// </summary>
    public long Index { get; set; }

    /// <summary>
    /// Gets or sets the previous block hash.
    /// </summary>
    public string PreviousBlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the transactions in this block.
    /// </summary>
    public List<TestTransaction> Transactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the block size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the merkle root.
    /// </summary>
    public string MerkleRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block nonce.
    /// </summary>
    public long Nonce { get; set; }
}

/// <summary>
/// Mock blockchain transaction for testing.
/// </summary>
public class TestTransaction
{
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction fee.
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the block hash containing this transaction.
    /// </summary>
    public string? BlockHash { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public long GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the gas price.
    /// </summary>
    public decimal GasPrice { get; set; }

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    public string? Data { get; set; }
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
    /// Transaction is confirmed.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Transaction failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Transaction was rejected.
    /// </summary>
    Rejected
}

/// <summary>
/// Mock contract event for testing.
/// </summary>
public class TestContractEvent
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract address.
    /// </summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash that triggered this event.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block number.
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the event parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the event signature.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Gets or sets the event topics.
    /// </summary>
    public List<string> Topics { get; set; } = new();
}