namespace NeoServiceLayer.Core;

// Zero-Knowledge Service Models
public class ProofRequest
{
    public string CircuitId { get; set; } = string.Empty;
    public Dictionary<string, object> PublicInputs { get; set; } = new();
    public Dictionary<string, object> PrivateInputs { get; set; } = new();
    public string ProofSystem { get; set; } = "groth16";
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ProofResult
{
    public string ProofId { get; set; } = Guid.NewGuid().ToString();
    public string Proof { get; set; } = string.Empty;
    public string[] PublicSignals { get; set; } = Array.Empty<string>();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string VerificationKey { get; set; } = string.Empty;
}

public class ProofVerification
{
    public string Proof { get; set; } = string.Empty;
    public string[] PublicSignals { get; set; } = Array.Empty<string>();
    public string VerificationKey { get; set; } = string.Empty;
    public string CircuitId { get; set; } = string.Empty;
}

// Zero-Knowledge Computation Models
public class ZkComputationRequest
{
    public string ComputationId { get; set; } = Guid.NewGuid().ToString();
    public string CircuitId { get; set; } = string.Empty;
    public Dictionary<string, object> Inputs { get; set; } = new();
    public Dictionary<string, object> PrivateInputs { get; set; } = new();
    public string ComputationType { get; set; } = "proof-generation";
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ZkComputationResult
{
    public string ComputationId { get; set; } = string.Empty;
    public string CircuitId { get; set; } = string.Empty;
    public object[] Results { get; set; } = Array.Empty<object>();
    public string Proof { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Cryptographic Service Models
public class CryptoKeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Fair Ordering Service Models (Basic types only - detailed models in service-specific namespace)
public class FairTransactionRequest
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Data { get; set; } = string.Empty;
    public decimal GasLimit { get; set; }
    public string ProtectionLevel { get; set; } = "Standard";
    public decimal MaxSlippage { get; set; }
    public DateTime? ExecuteAfter { get; set; }
    public DateTime? ExecuteBefore { get; set; }
    public string TransactionData { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class FairnessAnalysisResult
{
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
    public string TransactionId { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public double FairnessScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public decimal EstimatedMEV { get; set; }
    public string[] RiskFactors { get; set; } = Array.Empty<string>();
    public string[] DetectedRisks { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    public decimal ProtectionFee { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class TransactionAnalysisRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string TransactionData { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

// Risk Assessment Models (for PatternRecognition)
public class RiskAssessmentRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public Dictionary<string, object> TransactionData { get; set; } = new();
    public string[] RiskFactors { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ModelId { get; set; } = "default-risk-model";
}

public class RiskAssessmentResult
{
    public string AssessmentId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string[] RiskFactors { get; set; } = Array.Empty<string>();
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Notification Service Models
public class SubscriptionResult
{
    public string SubscriptionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ActiveSubscriptionsCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class UnsubscribeFromNotificationsRequest
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = Array.Empty<string>();
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class NotificationPreferences
{
    public string UserId { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = true;
    public bool WebhookEnabled { get; set; } = false;
    public string[] NotificationTypes { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

// Miscellaneous Enums
public enum AssetType
{
    Native,
    Token,
    Stablecoin,
    Wrapped,
    Synthetic,
    NFT
}

public enum RiskLevel
{
    Minimal,
    Low,
    Medium,
    High,
    Critical
}
