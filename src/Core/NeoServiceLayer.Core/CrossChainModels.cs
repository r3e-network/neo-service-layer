namespace NeoServiceLayer.Core;

// Cross-Chain Service Models
public class CrossChainMessageRequest
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Nonce { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CrossChainTransferRequest
{
    public string TransferId { get; set; } = Guid.NewGuid().ToString();
    public string TokenAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class RemoteCallRequest
{
    public string CallId { get; set; } = Guid.NewGuid().ToString();
    public string ContractAddress { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public object[] Parameters { get; set; } = Array.Empty<object>();
    public string Caller { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CrossChainMessageStatus
{
    public string MessageId { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CrossChainMessage
{
    public string MessageId { get; set; } = string.Empty;
    public BlockchainType SourceChain { get; set; }
    public BlockchainType DestinationChain { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public MessageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CrossChainRoute
{
    public BlockchainType Source { get; set; }
    public BlockchainType Destination { get; set; }
    public string[] IntermediateChains { get; set; } = Array.Empty<string>();
    public decimal EstimatedFee { get; set; }
    public TimeSpan EstimatedTime { get; set; }
    public double ReliabilityScore { get; set; }
}

public class CrossChainOperation
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString();
    public OperationType Type { get; set; }
    public BlockchainType SourceChain { get; set; }
    public BlockchainType DestinationChain { get; set; }
    public decimal Amount { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class CrossChainMessageProof
{
    public string MessageId { get; set; } = string.Empty;
    public string MessageHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string[] MerkleProof { get; set; } = Array.Empty<string>();
    public string Sender { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Nonce { get; set; }
    public BlockchainType SourceChain { get; set; }
    public BlockchainType TargetChain { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public byte[] ProofData { get; set; } = Array.Empty<byte>();
}

public class SupportedChain
{
    public BlockchainType ChainType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChainId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string[] SupportedTokens { get; set; } = Array.Empty<string>();
}

public class TokenMapping
{
    public string SourceToken { get; set; } = string.Empty;
    public string DestinationToken { get; set; } = string.Empty;
    public BlockchainType SourceChain { get; set; }
    public BlockchainType DestinationChain { get; set; }
    public decimal ExchangeRate { get; set; } = 1.0m;
    public bool IsActive { get; set; } = true;
}

// Cross-Chain Enums
public enum MessageStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum OperationType
{
    Message,
    Transfer,
    RemoteCall
}
