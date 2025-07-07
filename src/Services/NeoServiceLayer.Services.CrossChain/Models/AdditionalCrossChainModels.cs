using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.CrossChain.Models;

/// <summary>
/// Request to verify a message
/// </summary>
public class VerifyMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string SourceChain { get; set; } = string.Empty;
}

/// <summary>
/// Result of message verification
/// </summary>
public class MessageVerificationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public Dictionary<string, object> VerificationDetails { get; set; } = new();
}

/// <summary>
/// Request for fee estimation
/// </summary>
public class FeeEstimationRequest
{
    public string SourceChain { get; set; } = string.Empty;
    public string TargetChain { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OperationType { get; set; } = string.Empty;
}

/// <summary>
/// Fee estimation result
/// </summary>
public class FeeEstimate
{
    public decimal BaseFee { get; set; }
    public decimal BridgeFee { get; set; }
    public decimal GasFee { get; set; }
    public decimal TotalFee { get; set; }
    public string FeeAsset { get; set; } = string.Empty;
    public TimeSpan EstimatedDuration { get; set; }
    public DateTime EstimatedAt { get; set; }
}

/// <summary>
/// Cross-chain transfer result
/// </summary>
public class CrossChainTransferResult
{
    public bool Success { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public string SourceTxHash { get; set; } = string.Empty;
    public string TargetTxHash { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime InitiatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Bridge information
/// </summary>
public class BridgeInfo
{
    public string BridgeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SourceChain { get; set; } = string.Empty;
    public string TargetChain { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> SupportedAssets { get; set; } = new();
    public decimal MinTransferAmount { get; set; }
    public decimal MaxTransferAmount { get; set; }
    public TimeSpan AverageTransferTime { get; set; }
}

/// <summary>
/// Transfer status information
/// </summary>
public class TransferStatus
{
    public string TransferId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SourceChain { get; set; } = string.Empty;
    public string TargetChain { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public int Confirmations { get; set; }
    public int RequiredConfirmations { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> TransactionHashes { get; set; } = new();
}

/// <summary>
/// Request to establish event listener
/// </summary>
public class EventListenerRequest
{
    public string ChainId { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public List<string> EventTypes { get; set; } = new();
    public string CallbackUrl { get; set; } = string.Empty;
    public Dictionary<string, object> FilterParameters { get; set; } = new();
}

/// <summary>
/// Result of a cross-chain contract call
/// </summary>
public class ContractCallResult
{
    public bool Success { get; set; }
    public object ReturnValue { get; set; } = new();
    public string Exception { get; set; } = string.Empty;
    public long GasConsumed { get; set; }
    public List<string> Notifications { get; set; } = new();
    public string SourceChain { get; set; } = string.Empty;
    public string TargetChain { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
}
