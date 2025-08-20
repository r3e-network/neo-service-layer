using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;


namespace NeoServiceLayer.Services.CrossChain.Models;

/// <summary>
/// Represents a supported blockchain chain.
/// </summary>
public class SupportedChain
{
    /// <summary>
    /// Gets or sets the chain identifier.
    /// </summary>
    public string ChainId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chain name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType Type { get; set; }

    /// <summary>
    /// Gets or sets whether this is a testnet.
    /// </summary>
    public bool IsTestnet { get; set; }

    /// <summary>
    /// Gets or sets the chain configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

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

/// <summary>
/// Cross-chain message request
/// </summary>
public class CrossChainMessageRequest
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the recipient address (alias for Receiver).
    /// </summary>
    public string Recipient
    {
        get => Receiver;
        set => Receiver = value;
    }
    
    /// <summary>
    /// Gets or sets the message data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// Gets or sets the message nonce.
    /// </summary>
    public uint Nonce { get; set; }
    
    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cross-chain message status
/// </summary>
public class CrossChainMessageStatus
{
    public string MessageId { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Cross-chain message proof
/// </summary>
public class CrossChainMessageProof
{
    public string MessageId { get; set; } = string.Empty;
    public string MessageHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string ProofData { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    public string Sender { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the proof data bytes.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// Gets or sets the proof nonce.
    /// </summary>
    public uint Nonce { get; set; }
    
    /// <summary>
    /// Gets or sets the proof payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the source chain.
    /// </summary>
    public string SourceChain { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the target chain.
    /// </summary>
    public string TargetChain { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the proof timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Cross-chain message
/// </summary>
public class CrossChainMessage
{
    public string MessageId { get; set; } = string.Empty;
    public BlockchainType SourceChain { get; set; }
    public BlockchainType DestinationChain { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Cross-chain route information
/// </summary>
public class CrossChainRoute
{
    public BlockchainType Source { get; set; }
    public BlockchainType Destination { get; set; }
    public string[] IntermediateChains { get; set; } = Array.Empty<string>();
    public decimal EstimatedFee { get; set; }
    public TimeSpan EstimatedTime { get; set; }
    public double ReliabilityScore { get; set; }
}

/// <summary>
/// Cross-chain operation details
/// </summary>
public class CrossChainOperation
{
    public string Id { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public BlockchainType SourceChain { get; set; }
    public BlockchainType TargetChain { get; set; }
    public decimal Amount { get; set; }
    public string? Data { get; set; }
    public string Priority { get; set; } = "Normal";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Message status enumeration
/// </summary>
public enum MessageStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}


