using System.Collections.Generic;
using System;

namespace NeoServiceLayer.Core;

// Proof of Reserve Models

/// <summary>
/// Health status information for reserves
/// </summary>
public class ReserveHealthStatus
{
    public string AssetId { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public bool HasWarnings => Warnings?.Any() ?? false;
    public string StatusLevel { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public decimal CurrentRatio { get; set; }
    public decimal MinimumRatio { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Static constant for healthy status
    /// </summary>
    public static string Healthy => "Healthy";
}

/// <summary>
/// Reserve alert information
/// </summary>

/// <summary>
/// Enumeration of reserve health status values
/// </summary>
public enum ReserveHealthStatusEnum
{
    Healthy,
    Warning,
    Undercollateralized,
    Critical,
    Unknown
}

public class ReserveAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
}

/// <summary>
/// Audit report for reserves
/// </summary>
public class AuditReport
{
    public string ReportId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal AverageReserveRatio { get; set; }
    public List<ReserveAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Alert configuration for reserves - extended version
/// </summary>
public class ReserveAlertConfig
{
    public string AlertId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal ThresholdValue { get; set; }
    public decimal Threshold { get; set; }
    public decimal ThresholdPercentage { get; set; }
    public decimal MinimumThreshold { get; set; }
    public decimal WarningThreshold { get; set; }
    public decimal CriticalThreshold { get; set; }
    public string[] AlertRecipients { get; set; } = Array.Empty<string>();
    public bool IsEnabled { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public string NotificationChannel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Basic proof of reserve class
/// </summary>
public class ProofOfReserve
{
    public string ProofId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public decimal ReserveAmount { get; set; }
    public decimal LiabilityAmount { get; set; }
    public decimal ReserveRatio { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal TotalReserves { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; }
    public string MerkleRoot { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public Dictionary<string, object> ProofData { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the block height where this proof was recorded.
    /// </summary>
    public long BlockHeight { get; set; }
    
    /// <summary>
    /// Gets or sets the block hash where this proof was recorded.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;
}

/// <summary>
/// Snapshot of reserve data at a point in time
/// </summary>
public class ReserveSnapshot
{
    public string SnapshotId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public decimal ReserveAmount { get; set; }
    public decimal LiabilityAmount { get; set; }
    public decimal ReserveRatio { get; set; }
    public decimal TotalReserves { get; set; }
    public decimal TotalSupply { get; set; }
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
    public Dictionary<string, decimal> ReserveBalances { get; set; } = new();
    public ReserveHealthStatus Health { get; set; } = new();
    public Dictionary<string, object> SnapshotData { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Monitored asset information
/// </summary>
public class MonitoredAsset
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification for proof of reserve events
/// </summary>
public class ProofOfReserveNotification
{
    public string NotificationId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> NotificationData { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Proof of Reserve Models
    /// <summary>
    /// Gets or sets the reserve amount.
    /// </summary>
    /// <summary>
    /// Gets or sets the liability amount.
    /// </summary>
    /// <summary>
    /// Gets or sets the proof data.
    /// </summary>
    /// <summary>
    /// Gets or sets the timestamp (alias for GeneratedAt).
    /// </summary>
    /// <summary>
    /// Gets or sets whether the proof is verified.
    /// </summary>
// Proof of Reserve Enums
/// <summary>
/// Represents a reserve alert configuration.
/// </summary>
    /// <summary>
    /// Gets or sets the alert ID.
    /// </summary>
    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    /// <summary>
    /// Gets or sets the alert name.
    /// </summary>
    /// <summary>
    /// Gets or sets the alert type.
    /// </summary>
    /// <summary>
    /// Gets or sets the threshold value.
    /// </summary>
    /// <summary>
    /// Gets or sets whether the alert is enabled.
    /// </summary>
    /// <summary>
    /// Gets or sets when the alert was created.
    /// </summary>
    /// <summary>
    /// Gets or sets the minimum threshold for alerts.
    /// </summary>
    /// <summary>
    /// Gets or sets the warning threshold for alerts.
    /// </summary>
    /// <summary>
    /// Gets or sets the critical threshold for alerts.
    /// </summary>
    /// <summary>
    /// Gets or sets the alert recipients.
    /// </summary>
    /// <summary>
    /// Gets or sets the threshold percentage.
    /// </summary>
/// <summary>
/// Represents a subscription to reserve updates.
/// </summary>
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    /// <summary>
    /// Gets or sets the asset identifier.
    /// </summary>
    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    /// <summary>
    /// Gets or sets when the subscription was created.
    /// </summary>
    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    /// <summary>
    /// Gets or sets subscription filters.
    /// </summary>
/// <summary>
/// Represents a reserve asset for monitoring.
/// </summary>
    /// <summary>
    /// Gets or sets the asset identifier.
    /// </summary>
    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    /// <summary>
    /// Gets or sets whether the asset is active.
    /// </summary>
    /// <summary>
    /// Gets or sets the minimum reserve ratio.
    /// </summary>
    /// <summary>
    /// Gets or sets the current reserve ratio.
    /// </summary>
    /// <summary>
    /// Gets or sets the asset health status.
    /// </summary>
    /// <summary>
    /// Gets or sets when the asset was last updated.
    /// </summary>
    /// <summary>
    /// Gets or sets the reserve snapshots.
    /// </summary>
    /// <summary>
    /// Gets or sets the proofs of reserve.
    /// </summary>
/// <summary>
/// Represents a proof of reserve notification.
/// </summary>
    /// <summary>
    /// Gets or sets the notification identifier.
    /// </summary>
    /// <summary>
    /// Gets or sets the asset identifier.
    /// </summary>
    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    /// <summary>
    /// Gets or sets the alert type.
    /// </summary>
    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    /// <summary>
    /// Gets or sets the notification severity.
    /// </summary>
    /// <summary>
    /// Gets or sets when the notification was created.
    /// </summary>
    /// <summary>
    /// Gets or sets the notification timestamp.
    /// </summary>
    /// <summary>
    /// Gets or sets additional notification data.
    /// </summary>
    /// <summary>
    /// Gets or sets notification metadata.
    /// </summary>
/// <summary>
/// Represents a reserve status information.
/// </summary>
    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    /// <summary>
    /// Gets or sets the total supply.
    /// </summary>
    /// <summary>
    /// Gets or sets the total reserves.
    /// </summary>
    /// <summary>
    /// Gets or sets the reserve ratio.
    /// </summary>
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    /// <summary>
    /// Gets or sets when last updated.
    /// </summary>
    /// <summary>
    /// Gets or sets when last audited.
    /// </summary>
    /// <summary>
    /// Gets or sets the reserve breakdown.
    /// </summary>
    /// <summary>
    /// Gets or sets whether the asset is compliant.
    /// </summary>
    /// <summary>
    /// Gets or sets compliance notes.
    /// </summary>
/// <summary>
/// Represents a proof generation request.
/// </summary>
    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    /// <summary>
    /// Gets or sets the proof type.
    /// </summary>
    /// <summary>
    /// Gets or sets whether to include transaction history.
    /// </summary>
    /// <summary>
    /// Gets or sets whether to include signatures.
    /// </summary>
    /// <summary>
    /// Gets or sets additional proof parameters.
    /// </summary>
/// <summary>
/// Represents audit report request.
/// </summary>
    /// <summary>
    /// Gets or sets the asset ID.
    /// </summary>
    /// <summary>
    /// Gets or sets the from date.
    /// </summary>
    /// <summary>
    /// Gets or sets the to date.
    /// </summary>
    /// <summary>
    /// Gets or sets whether to include transaction details.
    /// </summary>
    /// <summary>
    /// Gets or sets whether to include compliance check.
    /// </summary>
    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
/// <summary>
/// Represents proof type.
/// </summary>
    /// <summary>
    /// Basic proof.
    /// </summary>
    /// <summary>
    /// Merkle proof.
    /// </summary>
    /// <summary>
    /// Zero-knowledge proof.
    /// </summary>
    /// <summary>
    /// Cryptographic proof.
    /// </summary>
