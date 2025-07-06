namespace NeoServiceLayer.Services.Compliance.Models;

/// <summary>
/// Result model for compliance check operations.
/// </summary>
public class ComplianceCheckResult
{
    /// <summary>
    /// Gets or sets whether the compliance check passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the compliance score (0-100).
    /// </summary>
    public int ComplianceScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the list of violations found.
    /// </summary>
    public List<RuleViolation> Violations { get; set; } = new();

    /// <summary>
    /// Gets or sets the check timestamp.
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Gets or sets the check identifier.
    /// </summary>
    public string CheckId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details about the check.
    /// </summary>
    public Dictionary<string, string> Details { get; set; } = new();
}

/// <summary>
/// Result model for compliance report generation.
/// </summary>
public class ComplianceReportResult
{
    /// <summary>
    /// Gets or sets the report identifier.
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report name.
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report type.
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report generation status.
    /// </summary>
    public string Status { get; set; } = "Generating";

    /// <summary>
    /// Gets or sets the report data.
    /// </summary>
    public ComplianceReportData? ReportData { get; set; }

    /// <summary>
    /// Gets or sets the report file URL (if applicable).
    /// </summary>
    public string? ReportUrl { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

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
    public string RuleId { get; set; } = string.Empty;

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
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result model for getting compliance rules.
/// </summary>
public class GetComplianceRulesResult
{
    /// <summary>
    /// Gets or sets the list of compliance rules.
    /// </summary>
    public List<ComplianceRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of rules.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMorePages => (PageNumber * PageSize) < TotalCount;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

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
    public string AuditId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit name.
    /// </summary>
    public string AuditName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit status.
    /// </summary>
    public AuditStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the audit progress percentage.
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the audit start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the audit completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the audit results.
    /// </summary>
    public AuditResults? Results { get; set; }

    /// <summary>
    /// Gets or sets the audit logs.
    /// </summary>
    public List<AuditLogEntry>? Logs { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated completion time.
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Gets or sets the audit progress.
    /// </summary>
    public int Progress { get; set; }
}

/// <summary>
/// Result model for violation operations.
/// </summary>
public class ViolationResult
{
    /// <summary>
    /// Gets or sets the violation identifier.
    /// </summary>
    public string ViolationId { get; set; } = string.Empty;

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
    /// Gets or sets the violation severity.
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets the violation status.
    /// </summary>
    public ViolationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the reported violation.
    /// </summary>
    public ComplianceViolation? Violation { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result model for getting violations.
/// </summary>
public class GetViolationsResult
{
    /// <summary>
    /// Gets or sets the list of violations.
    /// </summary>
    public List<ComplianceViolation> Violations { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of violations.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMorePages => (PageNumber * PageSize) < TotalCount;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

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
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the created remediation plan.
    /// </summary>
    public RemediationPlan? Plan { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the estimated completion time.
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }
}

/// <summary>
/// Result model for compliance dashboard.
/// </summary>
public class ComplianceDashboardResult
{
    /// <summary>
    /// Gets or sets the overall compliance score.
    /// </summary>
    public int OverallComplianceScore { get; set; }

    /// <summary>
    /// Gets or sets the total number of checks performed.
    /// </summary>
    public int TotalChecks { get; set; }

    /// <summary>
    /// Gets or sets the number of passed checks.
    /// </summary>
    public int PassedChecks { get; set; }

    /// <summary>
    /// Gets or sets the number of failed checks.
    /// </summary>
    public int FailedChecks { get; set; }

    /// <summary>
    /// Gets or sets the total number of violations.
    /// </summary>
    public int TotalViolations { get; set; }

    /// <summary>
    /// Gets or sets the number of active violations.
    /// </summary>
    public int ActiveViolations { get; set; }

    /// <summary>
    /// Gets or sets the number of resolved violations.
    /// </summary>
    public int ResolvedViolations { get; set; }

    /// <summary>
    /// Gets or sets the trend data.
    /// </summary>
    public List<ComplianceTrendData>? TrendData { get; set; }

    /// <summary>
    /// Gets or sets the violation breakdown by type.
    /// </summary>
    public Dictionary<string, int> ViolationsByType { get; set; } = new();

    /// <summary>
    /// Gets or sets the compliance metrics.
    /// </summary>
    public Dictionary<string, decimal> Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the dashboard generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;
}

/// <summary>
/// Result model for certification requests.
/// </summary>
public class CertificationResult
{
    /// <summary>
    /// Gets or sets the certification request identifier.
    /// </summary>
    public string CertificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certification status.
    /// </summary>
    public CertificationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the certification details.
    /// </summary>
    public CertificationDetails? Certification { get; set; }

    /// <summary>
    /// Gets or sets the estimated completion date.
    /// </summary>
    public DateTime? EstimatedCompletionDate { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request timestamp.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the estimated completion time.
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Gets or sets the certification type.
    /// </summary>
    public string CertificationType { get; set; } = string.Empty;
}

/// <summary>
/// Verification result for compliance checks.
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the verification passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the list of rule violations.
    /// </summary>
    public List<RuleViolation> Violations { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk score (0-100, where 0 is no risk and 100 is highest risk).
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the verification timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the verification ID.
    /// </summary>
    public string VerificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public NeoServiceLayer.Core.BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the proof.
    /// </summary>
    public string Proof { get; set; } = string.Empty;
}

/// <summary>
/// Rule violation information.
/// </summary>
public class RuleViolation
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description.
    /// </summary>
    public string RuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity (0-100, where 0 is lowest severity and 100 is highest severity).
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets the violation details.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation timestamp.
    /// </summary>
    public DateTime ViolatedAt { get; set; }

    /// <summary>
    /// Gets or sets the related transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the related address.
    /// </summary>
    public string? Address { get; set; }
}

/// <summary>
/// Compliance rule definition.
/// </summary>
public class ComplianceRule
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description (alias for Description).
    /// </summary>
    public string RuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule conditions.
    /// </summary>
    public List<ComplianceCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the rule type.
    /// </summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the severity (0-100, where 0 is lowest severity and 100 is highest severity).
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the rule category.
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Gets or sets the rule tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the created by user.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last modified by user.
    /// </summary>
    public string LastModifiedBy { get; set; } = string.Empty;
}

// AML/KYC Response Models

/// <summary>
/// Response model for KYC verification.
/// </summary>
public class KycVerificationResult
{
    /// <summary>
    /// Gets or sets the verification ID.
    /// </summary>
    public string VerificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the verification was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the KYC status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification level achieved.
    /// </summary>
    public string VerificationLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification details.
    /// </summary>
    public VerificationDetails Details { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the verification timestamp.
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for KYC status.
/// </summary>
public class KycStatusResult
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current KYC status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification level.
    /// </summary>
    public string VerificationLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the KYC was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the verification history.
    /// </summary>
    public List<KycHistoryEntry>? History { get; set; }
}

/// <summary>
/// Response model for AML screening.
/// </summary>
public class AmlScreeningResult
{
    /// <summary>
    /// Gets or sets the screening ID.
    /// </summary>
    public string ScreeningId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether screening passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the risk score (0-100).
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the screening results.
    /// </summary>
    public List<ScreeningResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the screening timestamp.
    /// </summary>
    public DateTime ScreenedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for suspicious activity report.
/// </summary>
public class SuspiciousActivityResult
{
    /// <summary>
    /// Gets or sets the report ID.
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the report was filed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the report status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the case number if applicable.
    /// </summary>
    public string? CaseNumber { get; set; }

    /// <summary>
    /// Gets or sets whether authorities were notified.
    /// </summary>
    public bool AuthoritiesNotified { get; set; }

    /// <summary>
    /// Gets or sets the notification details.
    /// </summary>
    public NotificationDetails? NotificationDetails { get; set; }

    /// <summary>
    /// Gets or sets when the report was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for watchlist operations.
/// </summary>
public class WatchlistResult
{
    /// <summary>
    /// Gets or sets the watchlist entries.
    /// </summary>
    public List<WatchlistEntry> Entries { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count.
    /// </summary>
    public int TotalCount { get; set; }

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
    public bool Success { get; set; }
}

/// <summary>
/// Response model for watchlist operations.
/// </summary>
public class WatchlistOperationResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the affected address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets when the operation occurred.
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for risk assessment.
/// </summary>
public class RiskAssessmentResult
{
    /// <summary>
    /// Gets or sets the assessment ID.
    /// </summary>
    public string AssessmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall risk score (0-100).
    /// </summary>
    public int OverallRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public List<RiskFactorResult> Factors { get; set; } = new();

    /// <summary>
    /// Gets or sets mitigation recommendations.
    /// </summary>
    public List<string> Mitigations { get; set; } = new();

    /// <summary>
    /// Gets or sets when the assessment was performed.
    /// </summary>
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for risk profile.
/// </summary>
public class RiskProfileResult
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current risk score.
    /// </summary>
    public int CurrentRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the risk history.
    /// </summary>
    public List<RiskHistoryEntry> History { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk categories.
    /// </summary>
    public Dictionary<string, int> RiskCategories { get; set; } = new();

    /// <summary>
    /// Gets or sets when the profile was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the detailed analysis.
    /// </summary>
    public RiskAnalysis? DetailedAnalysis { get; set; }
}
