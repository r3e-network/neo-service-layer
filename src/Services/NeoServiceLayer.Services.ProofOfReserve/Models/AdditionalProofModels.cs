using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.ProofOfReserve.Models;

/// <summary>
/// Request to register an asset for proof of reserve monitoring
/// </summary>
public class AssetRegistrationRequest
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string WalletAddress { get; set; } = string.Empty;
    public decimal MinimumReserveRatio { get; set; }
    public decimal MinReserveRatio  // Alias for MinimumReserveRatio
    {
        get => MinimumReserveRatio;
        set => MinimumReserveRatio = value;
    }
    public decimal TotalSupply { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Request to update reserve data for an asset
/// </summary>
public class ReserveUpdateRequest
{
    public string AssetId { get; set; } = string.Empty;
    public decimal ReserveAmount { get; set; }
    public decimal LiabilityAmount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> ValidationData { get; set; } = new();
    public string? AuditorSignature { get; set; }
    public Dictionary<string, object>? AuditData { get; set; }
    public string[] ReserveAddresses { get; set; } = Array.Empty<string>();
    public decimal[] ReserveBalances { get; set; } = Array.Empty<decimal>();
    
    /// <summary>
    /// Gets or sets the source of the audit information.
    /// </summary>
    public string? AuditSource { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the audit was performed.
    /// </summary>
    public DateTime? AuditTimestamp { get; set; }
}


/// <summary>
/// Status information for reserves
/// </summary>
public class ReserveStatusInfo
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public decimal CurrentReserve { get; set; }
    public decimal CurrentLiability { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal TotalReserves { get; set; }
    public decimal ReserveRatio { get; set; }
    public NeoServiceLayer.Core.ReserveHealthStatus Health { get; set; } = new();
    public NeoServiceLayer.Core.ReserveHealthStatus HealthStatus { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime LastAudit { get; set; }
    public string[] ReserveBreakdown { get; set; } = Array.Empty<string>();
    public bool IsCompliant { get; set; }
    public string ComplianceNotes { get; set; } = string.Empty;
}

/// <summary>
/// Types of reserve alerts
/// </summary>
public enum ReserveAlertType
{
    Warning,
    Critical,
    LowReserve,
    LowReserveRatio,
    ComplianceViolation,
    AuditOverdue,
    HighVolatility,
    SystemError,
    ValidationFailure
}

/// <summary>
/// Request to create a proof of reserve
/// </summary>
public class ProofOfReserveRequest
{
    public string AssetId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime? SnapshotDate { get; set; }
}

/// <summary>
/// Result of proof of reserve operation
/// </summary>
public class ProofOfReserveResult
{
    public bool Success { get; set; }
    public string ProofId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> ProofData { get; set; } = new();
}

/// <summary>
/// Request to verify a proof of reserve
/// </summary>
public class VerifyProofRequest
{
    public string ProofId { get; set; } = string.Empty;
    public string ProofData { get; set; } = string.Empty;
    public Dictionary<string, object> VerificationParameters { get; set; } = new();
}

/// <summary>
/// Request to publish a proof
/// </summary>
public class PublishProofRequest
{
    public string ProofId { get; set; } = string.Empty;
    public string PublicationTarget { get; set; } = string.Empty;
    public Dictionary<string, object> PublicationParameters { get; set; } = new();
}

/// <summary>
/// Result of publish operation
/// </summary>
public class PublishResult
{
    public bool Success { get; set; }
    public string PublicationId { get; set; } = string.Empty;
    public string PublicationUrl { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}

/// <summary>
/// Request to schedule proof generation
/// </summary>
public class ScheduleProofRequest
{
    public string AssetId { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public Dictionary<string, object> ScheduleParameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Result of schedule operation
/// </summary>
public class ScheduleResult
{
    public bool Success { get; set; }
    public string ScheduleId { get; set; } = string.Empty;
    public string NextRunTime { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Proof details information
/// </summary>
public class ProofDetails
{
    public string ProofId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public decimal ReserveAmount { get; set; }
    public decimal LiabilityAmount { get; set; }
    public decimal ReserveRatio { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Proof summary information
/// </summary>
public class ProofSummary
{
    public string ProofId { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public decimal ReserveRatio { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Reserve statistics
/// </summary>
public class ReserveStatistics
{
    public int TotalProofs { get; set; }
    public int ValidProofs { get; set; }
    public decimal AverageReserveRatio { get; set; }
    public Dictionary<string, decimal> AssetReserveRatios { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
