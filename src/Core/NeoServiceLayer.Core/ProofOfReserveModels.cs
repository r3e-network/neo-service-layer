namespace NeoServiceLayer.Core;

// Proof of Reserve Models
public class ProofOfReserve
{
    public string ProofId { get; set; } = Guid.NewGuid().ToString();
    public string AssetId { get; set; } = string.Empty;
    public decimal TotalSupply { get; set; }
    public decimal TotalReserves { get; set; }
    public decimal ReserveRatio { get; set; }
    public string MerkleRoot { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; } = string.Empty;
}

public class ReserveAlert
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public string AssetId { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public ReserveAlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AuditReport
{
    public string ReportId { get; set; } = Guid.NewGuid().ToString();
    public string AssetId { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public ReserveSnapshot[] Snapshots { get; set; } = Array.Empty<ReserveSnapshot>();
    public string Summary { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalSnapshots { get; set; }
    public decimal AverageReserveRatio { get; set; }
    public decimal MinReserveRatio { get; set; }
    public decimal MaxReserveRatio { get; set; }
    public decimal CompliancePercentage { get; set; }
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class AssetRegistrationRequest
{
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public AssetType Type { get; set; }
    public BlockchainType BlockchainType { get; set; }
    public string ContractAddress { get; set; } = string.Empty;
    public decimal TotalSupply { get; set; }
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
    public decimal MinReserveRatio { get; set; } = 1.0m;
    public string Owner { get; set; } = string.Empty;
    public int MonitoringFrequencyMinutes { get; set; } = 60;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ReserveUpdateRequest
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public decimal NewReserveAmount { get; set; }
    public string UpdateReason { get; set; } = string.Empty;
    public string AuditorSignature { get; set; } = string.Empty;
    public string[] NewReserveAddresses { get; set; } = Array.Empty<string>();
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
    public decimal[] ReserveBalances { get; set; } = Array.Empty<decimal>();
    public int? NewMonitoringFrequencyMinutes { get; set; }
    public bool ForceSnapshot { get; set; }
    public string AuditSource { get; set; } = string.Empty;
    public DateTime AuditTimestamp { get; set; } = DateTime.UtcNow;
    public object AuditData { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class MonitoredAsset
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public AssetType Type { get; set; }
    public BlockchainType BlockchainType { get; set; }
    public string ContractAddress { get; set; } = string.Empty;
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
    public ReserveStatus Status { get; set; }
    public ReserveHealthStatus Health { get; set; } = ReserveHealthStatus.Unknown;
    public decimal MinReserveRatio { get; set; } = 1.0m;
    public decimal CurrentReserveRatio { get; set; }
    public string Owner { get; set; } = string.Empty;
    public ReserveSnapshot? LastSnapshot { get; set; }
    public TimeSpan MonitoringFrequency { get; set; } = TimeSpan.FromHours(1);
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ReserveSnapshot
{
    public string SnapshotId { get; set; } = Guid.NewGuid().ToString();
    public string AssetId { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public decimal TotalReserve { get; set; }
    public decimal TotalReserves { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal ReserveRatio { get; set; }
    public ReserveStatus Status { get; set; }
    public ReserveHealthStatus Health { get; set; } = ReserveHealthStatus.Unknown;
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
    public decimal[] ReserveBalances { get; set; } = Array.Empty<decimal>();
    public Dictionary<string, decimal> AddressBalances { get; set; } = new();
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; } = string.Empty;
    public string MerkleRoot { get; set; } = string.Empty;
    public string ProofHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Proof of Reserve Enums
public enum ReserveStatus
{
    Healthy,
    PartiallyBacked,
    Undercollateralized,
    Offline,
    Unknown
}

public enum ReserveHealthStatus
{
    Unknown,
    Healthy,
    Warning,
    Critical,
    Undercollateralized
}

public enum ReserveAlertType
{
    LowReserve,
    LowReserveRatio,
    ComplianceViolation,
    HighVolatility,
    AuditOverdue,
    SystemError,
    SecurityBreach
}

public enum AlertType
{
    LowReserve,
    HighVolatility,
    SystemError,
    SecurityBreach
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical,
    Warning
}

/// <summary>
/// Represents a reserve alert configuration.
/// </summary>
public class ReserveAlertConfig
{
    /// <summary>
    /// Gets or sets the alert ID.
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert name.
    /// </summary>
    public string AlertName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert type.
    /// </summary>
    public ReserveAlertType Type { get; set; }

    /// <summary>
    /// Gets or sets the threshold value.
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// Gets or sets whether the alert is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets when the alert was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a subscription to reserve updates.
/// </summary>
public class ReserveSubscription
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset identifier.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets subscription filters.
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();
}

/// <summary>
/// Represents a reserve asset for monitoring.
/// </summary>
public class ReserveAsset
{
    /// <summary>
    /// Gets or sets the asset identifier.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the asset is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum reserve ratio.
    /// </summary>
    public decimal MinReserveRatio { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the current reserve ratio.
    /// </summary>
    public decimal CurrentReserveRatio { get; set; }

    /// <summary>
    /// Gets or sets the asset health status.
    /// </summary>
    public ReserveStatus Health { get; set; } = ReserveStatus.Unknown;

    /// <summary>
    /// Gets or sets when the asset was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the reserve snapshots.
    /// </summary>
    public List<ReserveSnapshot> Snapshots { get; set; } = new();

    /// <summary>
    /// Gets or sets the proofs of reserve.
    /// </summary>
    public List<ProofOfReserve> Proofs { get; set; } = new();
}

/// <summary>
/// Represents a proof of reserve notification.
/// </summary>
public class ProofOfReserveNotification
{
    /// <summary>
    /// Gets or sets the notification identifier.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset identifier.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert type.
    /// </summary>
    public string AlertType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification severity.
    /// </summary>
    public AlertSeverity Severity { get; set; } = AlertSeverity.Low;

    /// <summary>
    /// Gets or sets when the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the notification timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional notification data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets notification metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}



/// <summary>
/// Represents a reserve status information.
/// </summary>
public class ReserveStatusInfo
{
    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total supply.
    /// </summary>
    public decimal TotalSupply { get; set; }

    /// <summary>
    /// Gets or sets the total reserves.
    /// </summary>
    public decimal TotalReserves { get; set; }

    /// <summary>
    /// Gets or sets the reserve ratio.
    /// </summary>
    public decimal ReserveRatio { get; set; }

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public ReserveHealthStatus Health { get; set; }

    /// <summary>
    /// Gets or sets when last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets when last audited.
    /// </summary>
    public DateTime LastAudit { get; set; }

    /// <summary>
    /// Gets or sets the reserve breakdown.
    /// </summary>
    public string[] ReserveBreakdown { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether the asset is compliant.
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// Gets or sets compliance notes.
    /// </summary>
    public string ComplianceNotes { get; set; } = string.Empty;
}

/// <summary>
/// Represents a proof generation request.
/// </summary>
public class ProofGenerationRequest
{
    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proof type.
    /// </summary>
    public ProofType ProofType { get; set; } = ProofType.MerkleProof;

    /// <summary>
    /// Gets or sets whether to include transaction history.
    /// </summary>
    public bool IncludeTransactionHistory { get; set; }

    /// <summary>
    /// Gets or sets whether to include signatures.
    /// </summary>
    public bool IncludeSignatures { get; set; }

    /// <summary>
    /// Gets or sets additional proof parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents audit report request.
/// </summary>
public class AuditReportRequest
{
    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Gets or sets whether to include transaction details.
    /// </summary>
    public bool IncludeTransactionDetails { get; set; }

    /// <summary>
    /// Gets or sets whether to include compliance check.
    /// </summary>
    public bool IncludeComplianceCheck { get; set; }

    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents proof type.
/// </summary>
public enum ProofType
{
    /// <summary>
    /// Basic proof.
    /// </summary>
    Basic,

    /// <summary>
    /// Merkle proof.
    /// </summary>
    MerkleProof,

    /// <summary>
    /// Zero-knowledge proof.
    /// </summary>
    ZeroKnowledge,

    /// <summary>
    /// Cryptographic proof.
    /// </summary>
    Cryptographic
}
