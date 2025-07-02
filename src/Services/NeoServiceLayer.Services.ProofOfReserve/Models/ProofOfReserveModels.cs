using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ProofOfReserve.Models;

/// <summary>
/// Represents a proof of reserve.
/// </summary>
public class ProofOfReserve
{
    /// <summary>
    /// Gets or sets the proof ID.
    /// </summary>
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reserve amount.
    /// </summary>
    public decimal ReserveAmount { get; set; }

    /// <summary>
    /// Gets or sets the liability amount.
    /// </summary>
    public decimal LiabilityAmount { get; set; }

    /// <summary>
    /// Gets or sets the reserve ratio.
    /// </summary>
    public decimal ReserveRatio { get; set; }

    /// <summary>
    /// Gets or sets the proof data.
    /// </summary>
    public Dictionary<string, object> ProofData { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the proof is verified.
    /// </summary>
    public bool IsVerified { get; set; }
    
    /// <summary>
    /// Gets or sets the merkle root.
    /// </summary>
    public string MerkleRoot { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when the proof was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a monitored asset.
/// </summary>
public class MonitoredAsset
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
    /// Gets or sets the asset name.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset type.
    /// </summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monitoring configuration.
    /// </summary>
    public Dictionary<string, object> MonitoringConfig { get; set; } = new();

    /// <summary>
    /// Gets or sets whether monitoring is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the current reserve ratio.
    /// </summary>
    public decimal CurrentReserveRatio { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum reserve ratio.
    /// </summary>
    public decimal MinReserveRatio { get; set; }
    
    /// <summary>
    /// Gets or sets the asset health status.
    /// </summary>
    public NeoServiceLayer.Core.ReserveHealthStatus Health { get; set; } = NeoServiceLayer.Core.ReserveHealthStatus.Healthy;
    
    /// <summary>
    /// Gets or sets when the asset was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
}

/// <summary>
/// Represents a reserve snapshot.
/// </summary>
public class ReserveSnapshot
{
    /// <summary>
    /// Gets or sets the snapshot ID.
    /// </summary>
    public string SnapshotId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reserve amount.
    /// </summary>
    public decimal ReserveAmount { get; set; }

    /// <summary>
    /// Gets or sets the liability amount.
    /// </summary>
    public decimal LiabilityAmount { get; set; }

    /// <summary>
    /// Gets or sets the reserve ratio.
    /// </summary>
    public decimal ReserveRatio { get; set; }

    /// <summary>
    /// Gets or sets the snapshot timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional data.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the total supply.
    /// </summary>
    public decimal TotalSupply { get; set; }
    
    /// <summary>
    /// Gets or sets the total reserves.
    /// </summary>
    public decimal TotalReserves { get; set; }
    
    /// <summary>
    /// Gets or sets the reserve addresses.
    /// </summary>
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets the reserve balances.
    /// </summary>
    public decimal[] ReserveBalances { get; set; } = Array.Empty<decimal>();
    
    /// <summary>
    /// Gets or sets the health status of this snapshot.
    /// </summary>
    public NeoServiceLayer.Core.ReserveHealthStatus Health { get; set; } = NeoServiceLayer.Core.ReserveHealthStatus.Healthy;
    
    /// <summary>
    /// Gets or sets the verification status.
    /// </summary>
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    
    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long BlockHeight { get; set; }
    
    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;
}

/// <summary>
/// Represents a reserve subscription.
/// </summary>
public class ReserveSubscription
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    public string AssetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents reserve alert configuration.
/// </summary>
public class ReserveAlertConfig
{
    /// <summary>
    /// Gets or sets the configuration ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    [Required]
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum reserve threshold.
    /// </summary>
    public decimal MinimumThreshold { get; set; }

    /// <summary>
    /// Gets or sets the warning threshold.
    /// </summary>
    public decimal WarningThreshold { get; set; }

    /// <summary>
    /// Gets or sets the critical threshold.
    /// </summary>
    public decimal CriticalThreshold { get; set; }

    /// <summary>
    /// Gets or sets the alert recipients.
    /// </summary>
    public List<string> AlertRecipients { get; set; } = new();

    /// <summary>
    /// Gets or sets the alert methods.
    /// </summary>
    public List<AlertMethod> AlertMethods { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the alert is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents alert method.
/// </summary>
public enum AlertMethod
{
    /// <summary>
    /// Email alert.
    /// </summary>
    Email,

    /// <summary>
    /// SMS alert.
    /// </summary>
    SMS,

    /// <summary>
    /// Webhook alert.
    /// </summary>
    Webhook,

    /// <summary>
    /// Push notification.
    /// </summary>
    PushNotification,

    /// <summary>
    /// Slack notification.
    /// </summary>
    Slack,

    /// <summary>
    /// Discord notification.
    /// </summary>
    Discord
}

/// <summary>
/// Represents a reserve verification request.
/// </summary>
public class ReserveVerificationRequest
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    [Required]
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the addresses to verify.
    /// </summary>
    public List<string> Addresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the verification type.
    /// </summary>
    public VerificationType VerificationType { get; set; } = VerificationType.Standard;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets additional verification parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the request timestamp.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents verification type.
/// </summary>
public enum VerificationType
{
    /// <summary>
    /// Basic verification.
    /// </summary>
    Basic,

    /// <summary>
    /// Standard verification.
    /// </summary>
    Standard,

    /// <summary>
    /// Comprehensive verification.
    /// </summary>
    Comprehensive,

    /// <summary>
    /// Audit-grade verification.
    /// </summary>
    AuditGrade
}

/// <summary>
/// Represents a reserve verification result.
/// </summary>
public class ReserveVerificationResult
{
    /// <summary>
    /// Gets or sets the verification ID.
    /// </summary>
    public string VerificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total verified reserves.
    /// </summary>
    public decimal TotalReserves { get; set; }

    /// <summary>
    /// Gets or sets the individual address balances.
    /// </summary>
    public List<AddressBalance> AddressBalances { get; set; } = new();

    /// <summary>
    /// Gets or sets the verification status.
    /// </summary>
    public VerificationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the verification timestamp.
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the proof data.
    /// </summary>
    public string ProofData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the verification was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents address balance.
/// </summary>
public class AddressBalance
{
    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the balance.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification timestamp.
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the block number at verification.
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash for verification.
    /// </summary>
    public string? TransactionHash { get; set; }
}

/// <summary>
/// Represents verification status.
/// </summary>
public enum VerificationStatus
{
    /// <summary>
    /// Verification is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Verification is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Verification completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Verification failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Verification was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents a reserve audit report.
/// </summary>
public class ReserveAuditReport
{
    /// <summary>
    /// Gets or sets the report ID.
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit period start.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the audit period end.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the audited assets.
    /// </summary>
    public List<AssetReserveInfo> AuditedAssets { get; set; } = new();

    /// <summary>
    /// Gets or sets the total reserve value in USD.
    /// </summary>
    public decimal TotalReserveValueUSD { get; set; }

    /// <summary>
    /// Gets or sets the audit findings.
    /// </summary>
    public List<AuditFinding> Findings { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall audit status.
    /// </summary>
    public AuditStatus AuditStatus { get; set; }

    /// <summary>
    /// Gets or sets the auditor information.
    /// </summary>
    public string Auditor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the digital signature.
    /// </summary>
    public string DigitalSignature { get; set; } = string.Empty;
}

/// <summary>
/// Represents asset reserve information.
/// </summary>
public class AssetReserveInfo
{
    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total reserves.
    /// </summary>
    public decimal TotalReserves { get; set; }

    /// <summary>
    /// Gets or sets the number of addresses.
    /// </summary>
    public int AddressCount { get; set; }

    /// <summary>
    /// Gets or sets the verification count.
    /// </summary>
    public int VerificationCount { get; set; }

    /// <summary>
    /// Gets or sets the last verification timestamp.
    /// </summary>
    public DateTime LastVerified { get; set; }

    /// <summary>
    /// Gets or sets the average confidence score.
    /// </summary>
    public double AverageConfidence { get; set; }
}

/// <summary>
/// Represents audit finding.
/// </summary>
public class AuditFinding
{
    /// <summary>
    /// Gets or sets the finding ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the finding type.
    /// </summary>
    public FindingType Type { get; set; }

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public SeverityLevel Severity { get; set; }

    /// <summary>
    /// Gets or sets the finding description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected asset.
    /// </summary>
    public string? AffectedAsset { get; set; }

    /// <summary>
    /// Gets or sets the affected addresses.
    /// </summary>
    public List<string> AffectedAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommendation.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the finding timestamp.
    /// </summary>
    public DateTime FoundAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents audit status.
/// </summary>
public enum AuditStatus
{
    /// <summary>
    /// Audit passed without issues.
    /// </summary>
    Passed,

    /// <summary>
    /// Audit passed with minor issues.
    /// </summary>
    PassedWithIssues,

    /// <summary>
    /// Audit failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Audit is incomplete.
    /// </summary>
    Incomplete
}

/// <summary>
/// Represents finding type.
/// </summary>
public enum FindingType
{
    /// <summary>
    /// Discrepancy in reserves.
    /// </summary>
    ReserveDiscrepancy,

    /// <summary>
    /// Missing verification.
    /// </summary>
    MissingVerification,

    /// <summary>
    /// Suspicious activity.
    /// </summary>
    SuspiciousActivity,

    /// <summary>
    /// Configuration issue.
    /// </summary>
    ConfigurationIssue,

    /// <summary>
    /// Security concern.
    /// </summary>
    SecurityConcern,

    /// <summary>
    /// Compliance issue.
    /// </summary>
    ComplianceIssue
}

/// <summary>
/// Represents severity level.
/// </summary>
public enum SeverityLevel
{
    /// <summary>
    /// Low severity.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity.
    /// </summary>
    Critical
}
