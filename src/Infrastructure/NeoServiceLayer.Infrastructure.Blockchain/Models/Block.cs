using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Represents a blockchain block.
/// </summary>
public class Block
{
    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous block hash.
    /// </summary>
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long Height { get; set; }

    /// <summary>
    /// Gets or sets the block timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the transactions in the block.
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the block size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the gas used in the block.
    /// </summary>
    public decimal GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the gas limit for the block.
    /// </summary>
    public decimal GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the block producer/miner address.
    /// </summary>
    public string Producer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional block metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}