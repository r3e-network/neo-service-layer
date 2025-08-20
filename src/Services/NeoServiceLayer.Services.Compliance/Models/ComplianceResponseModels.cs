using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Compliance.Models;

/// <summary>
/// Basic verification result for compliance operations.
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// Gets or sets whether the verification passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the verification status message.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification details.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the verification was performed.
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the verification identifier.
    /// </summary>
    public string VerificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error messages.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of verification.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public string BlockchainType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether verification passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the risk score.
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the proof data.
    /// </summary>
    public Dictionary<string, object> Proof { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of violations found (compatibility property).
    /// </summary>
    public List<ComplianceViolation> Violations { get; set; } = new();
}

/// <summary>
/// Result model for compliance check operations.
/// </summary>
public class ComplianceCheckResult
{
    /// <summary>
    /// Gets or sets whether the compliance check passed.
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// Gets or sets the compliance score (0-100).
    /// </summary>
    [Range(0, 100)]
    public double ComplianceScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the list of violations found.
    /// </summary>
    public List<ComplianceViolation> Violations { get; set; } = new();

    /// <summary>
    /// Gets or sets the check timestamp.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the check identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CheckId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details about the check.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Result model for compliance report generation.
/// </summary>
public class ComplianceReportResult
{
    /// <summary>
    /// Gets or sets the report identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report name.
    /// </summary>
    [Required]
    [StringLength(255)]
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report type.
    /// </summary>
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Gets or sets the report generation status.
    /// </summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the report data.
    /// </summary>
    public string ReportData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report file URL (if applicable).
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the report expiration timestamp.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Result model for compliance rule operations.
/// </summary>
public class ComplianceRuleResult
{
    /// <summary>
    /// Gets or sets the rule identifier.
    /// </summary>
    [StringLength(64)]
    public string? RuleId { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the created or updated rule.
    /// </summary>
    public ComplianceRule? Rule { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result model for getting compliance rules.
/// </summary>
public class GetComplianceRulesResult
{
    /// <summary>
    /// Gets or sets the list of compliance rules.
    /// </summary>
    [Required]
    public List<ComplianceRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of rules.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result model for audit operations.
/// </summary>
public class AuditResult
{
    /// <summary>
    /// Gets or sets the audit identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AuditId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit status.
    /// </summary>
    public AuditStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the audit progress percentage.
    /// </summary>
    [Range(0, 100)]
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the audit results summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit findings.
    /// </summary>
    public List<AuditFinding> Findings { get; set; } = new();

    /// <summary>
    /// Gets or sets when the audit was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the audit was completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the auditor identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string AuditorId { get; set; } = string.Empty;
}

/// <summary>
/// Result model for violation reporting operations.
/// </summary>
public class ViolationResult
{
    /// <summary>
    /// Gets or sets the violation identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ViolationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the violation was successfully reported.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the violation status.
    /// </summary>
    public ViolationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the violation was reported.
    /// </summary>
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the reporter identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string ReporterId { get; set; } = string.Empty;
}

/// <summary>
/// Result model for getting violations.
/// </summary>
public class GetViolationsResult
{
    /// <summary>
    /// Gets or sets the list of violations.
    /// </summary>
    [Required]
    public List<ComplianceViolation> Violations { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of violations.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result model for remediation plan operations.
/// </summary>
public class RemediationPlanResult
{
    /// <summary>
    /// Gets or sets the remediation plan identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the plan was successfully created.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the plan status.
    /// </summary>
    public RemediationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the remediation steps.
    /// </summary>
    public List<RemediationStep> Steps { get; set; } = new();

    /// <summary>
    /// Gets or sets the estimated completion time.
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Gets or sets when the plan was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the plan creator identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string CreatorId { get; set; } = string.Empty;
}

/// <summary>
/// Result model for compliance dashboard operations.
/// </summary>
public class ComplianceDashboardResult
{
    /// <summary>
    /// Gets or sets the overall compliance score.
    /// </summary>
    [Range(0, 100)]
    public double OverallComplianceScore { get; set; }

    /// <summary>
    /// Gets or sets the compliance metrics.
    /// </summary>
    public ComplianceMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets recent violations.
    /// </summary>
    public List<ComplianceViolation> RecentViolations { get; set; } = new();

    /// <summary>
    /// Gets or sets compliance trends.
    /// </summary>
    public ComplianceTrends Trends { get; set; } = new();

    /// <summary>
    /// Gets or sets when the dashboard was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result model for certification operations.
/// </summary>
public class CertificationResult
{
    /// <summary>
    /// Gets or sets the certification identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CertificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certification status.
    /// </summary>
    public CertificationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the certificate data (if issued).
    /// </summary>
    public string? CertificateData { get; set; }

    /// <summary>
    /// Gets or sets when the certification was issued.
    /// </summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>
    /// Gets or sets when the certification expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the certification authority.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional certification details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

// AML/KYC Result Models

/// <summary>
/// Result model for KYC verification operations.
/// </summary>
public class KycVerificationResult
{
    /// <summary>
    /// Gets or sets the verification identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string VerificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether KYC verification passed.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the verification status.
    /// </summary>
    public KycStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the verification confidence score.
    /// </summary>
    [Range(0, 100)]
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets any issues found during verification.
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// Gets or sets when the verification was performed.
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result model for KYC status operations.
/// </summary>
public class KycStatusResult
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current KYC status.
    /// </summary>
    public KycStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the KYC was last verified.
    /// </summary>
    public DateTime? LastVerifiedAt { get; set; }

    /// <summary>
    /// Gets or sets when the KYC expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the verification level.
    /// </summary>
    public KycLevel Level { get; set; }
}

/// <summary>
/// Result model for AML screening operations.
/// </summary>
public class AmlScreeningResult
{
    /// <summary>
    /// Gets or sets the screening identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ScreeningId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the transaction passed AML screening.
    /// </summary>
    public bool Cleared { get; set; }

    /// <summary>
    /// Gets or sets the AML risk score.
    /// </summary>
    [Range(0, 100)]
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets any AML alerts triggered.
    /// </summary>
    public List<AmlAlert> Alerts { get; set; } = new();

    /// <summary>
    /// Gets or sets when the screening was performed.
    /// </summary>
    public DateTime ScreenedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result model for suspicious activity reporting.
/// </summary>
public class SuspiciousActivityResult
{
    /// <summary>
    /// Gets or sets the report identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the report was successfully filed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the report status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the report was filed.
    /// </summary>
    public DateTime FiledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the regulatory reference number.
    /// </summary>
    public string? RegulatoryReference { get; set; }
}

/// <summary>
/// Result model for watchlist operations.
/// </summary>
public class WatchlistResult
{
    /// <summary>
    /// Gets or sets the watchlist entries.
    /// </summary>
    [Required]
    public List<WatchlistEntry> Entries { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of entries.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets when the watchlist was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;
}

/// <summary>
/// Result model for watchlist operations.
/// </summary>
public class WatchlistOperationResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the operation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected entry identifier.
    /// </summary>
    public string? EntryId { get; set; }

    /// <summary>
    /// Gets or sets when the operation was performed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the address associated with the operation.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Gets or sets when the operation was performed.
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result model for risk assessment operations.
/// </summary>
public class RiskAssessmentResult
{
    /// <summary>
    /// Gets or sets the assessment identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string AssessmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall risk score.
    /// </summary>
    [Range(0, 100)]
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level classification.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the individual risk factors.
    /// </summary>
    public List<RiskFactor> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets recommended actions.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets when the assessment was performed.
    /// </summary>
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the overall risk score.
    /// </summary>
    public double OverallRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public Dictionary<string, double> Factors { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk mitigations.
    /// </summary>
    public List<string> Mitigations { get; set; } = new();
}

/// <summary>
/// Result model for risk profile operations.
/// </summary>
public class RiskProfileResult
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the risk profile.
    /// </summary>
    [Required]
    public RiskProfile Profile { get; set; } = new();

    /// <summary>
    /// Gets or sets when the profile was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the profile validity period.
    /// </summary>
    public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddMonths(6);

    /// <summary>
    /// Gets or sets the current risk score.
    /// </summary>
    public double CurrentRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the risk history.
    /// </summary>
    public List<RiskHistoryEntry> History { get; set; } = new();

    /// <summary>
    /// Gets or sets risk categories.
    /// </summary>
    public Dictionary<string, double> RiskCategories { get; set; } = new();

    /// <summary>
    /// Gets or sets detailed analysis.
    /// </summary>
    public RiskAnalysis DetailedAnalysis { get; set; } = new();
}

/// <summary>
/// Represents compliance trends data.
/// </summary>
public class ComplianceTrends
{
    /// <summary>
    /// Gets or sets the total compliance score.
    /// </summary>
    public decimal ComplianceScore { get; set; }
    
    /// <summary>
    /// Gets or sets the trend direction.
    /// </summary>
    public string TrendDirection { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the data points.
    /// </summary>
    public List<DataPoint> DataPoints { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the period.
    /// </summary>
    public string Period { get; set; } = string.Empty;
}

/// <summary>
/// Represents a data point in trends.
/// </summary>
public class DataPoint
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public decimal Value { get; set; }
    
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Represents a watchlist entry.
/// </summary>
public class WatchlistEntry
{
    /// <summary>
    /// Gets or sets the entry ID.
    /// </summary>
    public string EntryId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the entity address.
    /// </summary>
    public string EntityAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the reason for listing.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public string Severity { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when the entry was added.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets when the entry expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Gets or sets whether the entry is active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a risk profile.
/// </summary>
public class RiskProfile
{
    /// <summary>
    /// Gets or sets the risk score.
    /// </summary>
    public decimal RiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public List<RiskFactor> RiskFactors { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the risk assessment date.
    /// </summary>
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a risk factor.
/// </summary>
public class RiskFactor
{
    /// <summary>
    /// Gets or sets the factor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the factor weight.
    /// </summary>
    public decimal Weight { get; set; }
    
    /// <summary>
    /// Gets or sets the factor score.
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Gets or sets the factor description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a risk history entry.
/// </summary>
public class RiskHistoryEntry
{
    /// <summary>
    /// Gets or sets the entry ID.
    /// </summary>
    public string EntryId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the risk score at this point in time.
    /// </summary>
    public double RiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the risk level at this point in time.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }
    
    /// <summary>
    /// Gets or sets when this assessment was made.
    /// </summary>
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets any notes or comments.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the timestamp for this entry.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets what triggered this risk assessment.
    /// </summary>
    public string Trigger { get; set; } = string.Empty;
}

/// <summary>
/// Represents a detailed risk analysis.
/// </summary>
public class RiskAnalysis
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the risk categories.
    /// </summary>
    public Dictionary<string, double> Categories { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the risk indicators.
    /// </summary>
    public List<string> Indicators { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the methodology used for analysis.
    /// </summary>
    public string Methodology { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the data sources used for analysis.
    /// </summary>
    public List<string> DataSources { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the findings from the analysis.
    /// </summary>
    public List<string> Findings { get; set; } = new();
    
    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}