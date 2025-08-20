using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Services.SmartContracts.NeoX.Models;

/// <summary>
/// Represents a cross-chain transaction request.
/// </summary>
public class CrossChainTransactionRequest
{
    /// <summary>
    /// The source blockchain identifier.
    /// </summary>
    [Required]
    public string SourceBlockchain { get; set; } = string.Empty;

    /// <summary>
    /// The target blockchain identifier.
    /// </summary>
    [Required]
    public string TargetBlockchain { get; set; } = string.Empty;

    /// <summary>
    /// The contract address on the source blockchain.
    /// </summary>
    [Required]
    public string SourceContract { get; set; } = string.Empty;

    /// <summary>
    /// The contract address on the target blockchain.
    /// </summary>
    [Required]
    public string TargetContract { get; set; } = string.Empty;

    /// <summary>
    /// The method to call on the target contract.
    /// </summary>
    [Required]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Parameters for the method call.
    /// </summary>
    public object[]? Parameters { get; set; }

    /// <summary>
    /// Value to transfer (if applicable).
    /// </summary>
    public decimal? Value { get; set; }

    /// <summary>
    /// Gas limit for the transaction.
    /// </summary>
    public long? GasLimit { get; set; }
}

/// <summary>
/// Result of a cross-chain transaction.
/// </summary>
public class CrossChainTransactionResult
{
    /// <summary>
    /// The transaction hash on the source blockchain.
    /// </summary>
    public string SourceTransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// The transaction hash on the target blockchain.
    /// </summary>
    public string TargetTransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Whether the cross-chain transaction was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the transaction failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The block number on the source blockchain.
    /// </summary>
    public long SourceBlockNumber { get; set; }

    /// <summary>
    /// The block number on the target blockchain.
    /// </summary>
    public long TargetBlockNumber { get; set; }

    /// <summary>
    /// Gas consumed on the source blockchain.
    /// </summary>
    public long SourceGasConsumed { get; set; }

    /// <summary>
    /// Gas consumed on the target blockchain.
    /// </summary>
    public long TargetGasConsumed { get; set; }

    /// <summary>
    /// Return value from the target contract method.
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// Cross-chain bridge fee.
    /// </summary>
    public decimal BridgeFee { get; set; }

    /// <summary>
    /// Timestamp when the cross-chain transaction was completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Bridge contract configuration.
/// </summary>
public class BridgeConfiguration
{
    /// <summary>
    /// The bridge contract address on the source blockchain.
    /// </summary>
    public string SourceBridgeAddress { get; set; } = string.Empty;

    /// <summary>
    /// The bridge contract address on the target blockchain.
    /// </summary>
    public string TargetBridgeAddress { get; set; } = string.Empty;

    /// <summary>
    /// Minimum confirmation blocks required.
    /// </summary>
    public int MinConfirmations { get; set; } = 6;

    /// <summary>
    /// Bridge operator addresses for multi-sig.
    /// </summary>
    public List<string> Operators { get; set; } = new();

    /// <summary>
    /// Required signature threshold.
    /// </summary>
    public int SignatureThreshold { get; set; } = 2;

    /// <summary>
    /// Bridge fee percentage (0.01 = 1%).
    /// </summary>
    public decimal FeePercentage { get; set; } = 0.001m;
}
