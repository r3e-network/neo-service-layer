using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.ProofOfReserve.Models;

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
