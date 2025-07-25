﻿namespace NeoServiceLayer.Services.Compliance.Models;

/// <summary>
/// Represents a compliance condition for rules.
/// </summary>
public class ComplianceCondition
{
    /// <summary>
    /// Gets or sets the condition identifier.
    /// </summary>
    public string ConditionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition operator.
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition value.
    /// </summary>
    public object Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the condition is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents violation information.
/// </summary>
public class ViolationInfo
{
    /// <summary>
    /// Gets or sets the violation identifier.
    /// </summary>
    public string ViolationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation type.
    /// </summary>
    public string ViolationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets the violation status.
    /// </summary>
    public ViolationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the violation timestamp.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the related rule identifier.
    /// </summary>
    public string? RuleId { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a date range for filtering.
/// </summary>
public class DateRange
{
    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets whether the range is valid.
    /// </summary>
    public bool IsValid => StartDate <= EndDate;
}

/// <summary>
/// Represents a severity range for filtering.
/// </summary>
public class SeverityRange
{
    /// <summary>
    /// Gets or sets the minimum severity level.
    /// </summary>
    public int MinSeverity { get; set; }

    /// <summary>
    /// Gets or sets the maximum severity level.
    /// </summary>
    public int MaxSeverity { get; set; }

    /// <summary>
    /// Gets whether the range is valid.
    /// </summary>
    public bool IsValid => MinSeverity <= MaxSeverity && MinSeverity >= 0 && MaxSeverity <= 100;
}

/// <summary>
/// Represents audit scope configuration.
/// </summary>
public class AuditScope
{
    /// <summary>
    /// Gets or sets the addresses to include in the audit.
    /// </summary>
    public List<string> Addresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction types to audit.
    /// </summary>
    public List<string> TransactionTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the date range for the audit.
    /// </summary>
    public DateRange? DateRange { get; set; }

    /// <summary>
    /// Gets or sets the compliance rules to check.
    /// </summary>
    public List<string> ComplianceRules { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include historical data.
    /// </summary>
    public bool IncludeHistoricalData { get; set; } = true;
}

/// <summary>
/// Represents audit schedule configuration.
/// </summary>
public class AuditSchedule
{
    /// <summary>
    /// Gets or sets the schedule type.
    /// </summary>
    public AuditScheduleType ScheduleType { get; set; }

    /// <summary>
    /// Gets or sets the interval for recurring audits.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Gets or sets the specific execution time for scheduled audits.
    /// </summary>
    public DateTime? ScheduledTime { get; set; }

    /// <summary>
    /// Gets or sets the days of week for weekly schedules.
    /// </summary>
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Represents audit notification settings.
/// </summary>
public class AuditNotificationSettings
{
    /// <summary>
    /// Gets or sets whether to send notifications on audit start.
    /// </summary>
    public bool NotifyOnStart { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to send notifications on audit completion.
    /// </summary>
    public bool NotifyOnCompletion { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to send notifications on violations found.
    /// </summary>
    public bool NotifyOnViolations { get; set; } = true;

    /// <summary>
    /// Gets or sets the email addresses to notify.
    /// </summary>
    public List<string> EmailAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the webhook URLs to call.
    /// </summary>
    public List<string> WebhookUrls { get; set; } = new();
}

/// <summary>
/// Represents a remediation action.
/// </summary>
public class RemediationAction
{
    /// <summary>
    /// Gets or sets the action identifier.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action type.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target completion date.
    /// </summary>
    public DateTime? TargetDate { get; set; }

    /// <summary>
    /// Gets or sets the action status.
    /// </summary>
    public RemediationActionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the assigned person or team.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Gets or sets the action priority.
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Gets or sets additional action parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Represents contact information.
/// </summary>
public class ContactInformation
{
    /// <summary>
    /// Gets or sets the contact name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the organization.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets the job title.
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// Represents compliance report data.
/// </summary>
public class ComplianceReportData : Dictionary<string, object>
{
    /// <summary>
    /// Gets or sets the report summary.
    /// </summary>
    public ComplianceReportSummary Summary { get; set; } = new();

    /// <summary>
    /// Gets or sets the detailed findings.
    /// </summary>
    public List<ComplianceFinding> Findings { get; set; } = new();

    /// <summary>
    /// Gets or sets the metrics data.
    /// </summary>
    public Dictionary<string, decimal> Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the raw data (if applicable).
    /// </summary>
    public string? RawData { get; set; }

    /// <summary>
    /// Initializes a new instance of the ComplianceReportData class.
    /// </summary>
    public ComplianceReportData()
    {
        // Pre-populate with common keys for indexing
        this["summary"] = Summary;
        this["findings"] = Findings;
        this["metrics"] = Metrics;
        this["rawData"] = RawData;
    }
}

/// <summary>
/// Represents compliance report summary.
/// </summary>
public class ComplianceReportSummary
{
    /// <summary>
    /// Gets or sets the total transactions checked.
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Gets or sets the number of violations found.
    /// </summary>
    public int ViolationsFound { get; set; }

    /// <summary>
    /// Gets or sets the overall compliance score.
    /// </summary>
    public int ComplianceScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the period covered.
    /// </summary>
    public DateRange Period { get; set; } = new();
}

/// <summary>
/// Represents a compliance finding.
/// </summary>
public class ComplianceFinding
{
    /// <summary>
    /// Gets or sets the finding identifier.
    /// </summary>
    public string FindingId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the finding type.
    /// </summary>
    public string FindingType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets the finding description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related rule.
    /// </summary>
    public string? RuleId { get; set; }

    /// <summary>
    /// Gets or sets the affected entities.
    /// </summary>
    public List<string> AffectedEntities { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommended actions.
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// Represents audit results.
/// </summary>
public class AuditResults
{
    /// <summary>
    /// Gets or sets the total items audited.
    /// </summary>
    public int TotalItemsAudited { get; set; }

    /// <summary>
    /// Gets or sets the number of violations found.
    /// </summary>
    public int ViolationsFound { get; set; }

    /// <summary>
    /// Gets or sets the compliance score.
    /// </summary>
    public int ComplianceScore { get; set; }

    /// <summary>
    /// Gets or sets the detailed findings.
    /// </summary>
    public List<ComplianceFinding> Findings { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance metrics.
    /// </summary>
    public Dictionary<string, decimal> Metrics { get; set; } = new();
}

/// <summary>
/// Represents an audit log entry.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the log entry identifier.
    /// </summary>
    public string LogId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional context data.
    /// </summary>
    public Dictionary<string, string> Context { get; set; } = new();
}

/// <summary>
/// Represents a compliance violation.
/// </summary>
public class ComplianceViolation
{
    /// <summary>
    /// Gets or sets the violation identifier.
    /// </summary>
    public string ViolationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation type.
    /// </summary>
    public string ViolationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public ViolationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the related transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the related address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the violation timestamp.
    /// </summary>
    public DateTime ReportedAt { get; set; }

    /// <summary>
    /// Gets or sets the resolution timestamp.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets the source of the violation report.
    /// </summary>
    public string Source { get; set; } = "Manual";

    /// <summary>
    /// Gets or sets the evidence.
    /// </summary>
    public Dictionary<string, string> Evidence { get; set; } = new();
}

/// <summary>
/// Represents a remediation plan.
/// </summary>
public class RemediationPlan
{
    /// <summary>
    /// Gets or sets the plan identifier.
    /// </summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation identifier.
    /// </summary>
    public string ViolationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plan title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plan description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remediation actions.
    /// </summary>
    public List<RemediationAction> Actions { get; set; } = new();

    /// <summary>
    /// Gets or sets the plan status.
    /// </summary>
    public RemediationPlanStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the target completion date.
    /// </summary>
    public DateTime? TargetCompletionDate { get; set; }

    /// <summary>
    /// Gets or sets the actual completion date.
    /// </summary>
    public DateTime? ActualCompletionDate { get; set; }

    /// <summary>
    /// Gets or sets the assigned responsible party.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Gets or sets the priority level.
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// Represents compliance trend data.
/// </summary>
public class ComplianceTrendData
{
    /// <summary>
    /// Gets or sets the data point timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the compliance score at this point.
    /// </summary>
    public int ComplianceScore { get; set; }

    /// <summary>
    /// Gets or sets the number of violations at this point.
    /// </summary>
    public int ViolationCount { get; set; }

    /// <summary>
    /// Gets or sets the number of checks performed.
    /// </summary>
    public int CheckCount { get; set; }

    /// <summary>
    /// Gets or sets additional metrics.
    /// </summary>
    public Dictionary<string, decimal> Metrics { get; set; } = new();
}

/// <summary>
/// Represents a compliance report.
/// </summary>
public class ComplianceReport
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
    public string Status { get; set; } = "Generated";

    /// <summary>
    /// Gets or sets the report data.
    /// </summary>
    public ComplianceReportData ReportData { get; set; } = new();

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the report parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the report format.
    /// </summary>
    public string Format { get; set; } = "JSON";
}

/// <summary>
/// Represents certification details.
/// </summary>
public class CertificationDetails
{
    /// <summary>
    /// Gets or sets the certification identifier.
    /// </summary>
    public string CertificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certification type.
    /// </summary>
    public string CertificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public string EntityIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certification scope.
    /// </summary>
    public List<string> Scope { get; set; } = new();

    /// <summary>
    /// Gets or sets the certification status.
    /// </summary>
    public CertificationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the issuance date.
    /// </summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the issuing authority.
    /// </summary>
    public string? IssuingAuthority { get; set; }

    /// <summary>
    /// Gets or sets the certificate number.
    /// </summary>
    public string? CertificateNumber { get; set; }
}

// Enums

/// <summary>
/// Audit status enumeration.
/// </summary>
public enum AuditStatus
{
    /// <summary>
    /// Audit is pending start.
    /// </summary>
    Pending,

    /// <summary>
    /// Audit is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Audit has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Audit has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Audit has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Audit has been paused.
    /// </summary>
    Paused
}

/// <summary>
/// Audit schedule type enumeration.
/// </summary>
public enum AuditScheduleType
{
    /// <summary>
    /// One-time execution.
    /// </summary>
    OneTime,

    /// <summary>
    /// Daily recurring execution.
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly recurring execution.
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly recurring execution.
    /// </summary>
    Monthly,

    /// <summary>
    /// Custom interval execution.
    /// </summary>
    Custom
}

/// <summary>
/// Violation status enumeration.
/// </summary>
public enum ViolationStatus
{
    /// <summary>
    /// Violation is open and needs attention.
    /// </summary>
    Open,

    /// <summary>
    /// Violation is under investigation.
    /// </summary>
    UnderInvestigation,

    /// <summary>
    /// Violation is in progress of being resolved.
    /// </summary>
    InProgress,

    /// <summary>
    /// Violation has been resolved.
    /// </summary>
    Resolved,

    /// <summary>
    /// Violation has been dismissed as false positive.
    /// </summary>
    Dismissed,

    /// <summary>
    /// Violation has been closed.
    /// </summary>
    Closed
}

/// <summary>
/// Remediation action status enumeration.
/// </summary>
public enum RemediationActionStatus
{
    /// <summary>
    /// Action is planned but not started.
    /// </summary>
    Planned,

    /// <summary>
    /// Action is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Action has been completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Action has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Action is blocked by dependencies.
    /// </summary>
    Blocked,

    /// <summary>
    /// Action has failed.
    /// </summary>
    Failed
}

/// <summary>
/// Remediation plan status enumeration.
/// </summary>
public enum RemediationPlanStatus
{
    /// <summary>
    /// Plan is in draft state.
    /// </summary>
    Draft,

    /// <summary>
    /// Plan is active and being executed.
    /// </summary>
    Active,

    /// <summary>
    /// Plan has been completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Plan has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Plan is on hold.
    /// </summary>
    OnHold,

    /// <summary>
    /// Plan has failed.
    /// </summary>
    Failed
}

/// <summary>
/// Certification status enumeration.
/// </summary>
public enum CertificationStatus
{
    /// <summary>
    /// Certification request is pending review.
    /// </summary>
    Pending,

    /// <summary>
    /// Certification is under review.
    /// </summary>
    UnderReview,

    /// <summary>
    /// Certification has been approved.
    /// </summary>
    Approved,

    /// <summary>
    /// Certification has been rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Certification is active and valid.
    /// </summary>
    Active,

    /// <summary>
    /// Certification has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Certification has been revoked.
    /// </summary>
    Revoked,

    /// <summary>
    /// Certification has been suspended.
    /// </summary>
    Suspended
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace level logging.
    /// </summary>
    Trace,

    /// <summary>
    /// Debug level logging.
    /// </summary>
    Debug,

    /// <summary>
    /// Information level logging.
    /// </summary>
    Information,

    /// <summary>
    /// Warning level logging.
    /// </summary>
    Warning,

    /// <summary>
    /// Error level logging.
    /// </summary>
    Error,

    /// <summary>
    /// Critical level logging.
    /// </summary>
    Critical
}

// AML/KYC Supporting Types

/// <summary>
/// Identity document information.
/// </summary>
public class IdentityDocument
{
    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document number.
    /// </summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuing country.
    /// </summary>
    public string IssuingCountry { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiry date.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the document data.
    /// </summary>
    public string DocumentData { get; set; } = string.Empty;
}

/// <summary>
/// Personal information for KYC.
/// </summary>
public class PersonalInformation
{
    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the nationality.
    /// </summary>
    public string Nationality { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public ComplianceAddress Address { get; set; } = new();

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? EmailAddress { get; set; }
}

/// <summary>
/// Transaction data for AML screening.
/// </summary>
public class TransactionData
{
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from address.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the to address.
    /// </summary>
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Suspicious activity details.
/// </summary>
public class SuspiciousActivityDetails
{
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the indicators.
    /// </summary>
    public List<string> Indicators { get; set; } = new();

    /// <summary>
    /// Gets or sets the related transactions.
    /// </summary>
    public List<string> RelatedTransactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the evidence.
    /// </summary>
    public Dictionary<string, string> Evidence { get; set; } = new();

    /// <summary>
    /// Gets or sets when the activity occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Watchlist filter criteria.
/// </summary>
public class WatchlistFilter
{
    /// <summary>
    /// Gets or sets the risk level filter.
    /// </summary>
    public string? RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the date added from filter.
    /// </summary>
    public DateTime? AddedFrom { get; set; }

    /// <summary>
    /// Gets or sets the date added to filter.
    /// </summary>
    public DateTime? AddedTo { get; set; }

    /// <summary>
    /// Gets or sets the reason filter.
    /// </summary>
    public string? ReasonContains { get; set; }

    /// <summary>
    /// Gets or sets whether to include expired entries.
    /// </summary>
    public bool IncludeExpired { get; set; } = false;
}

/// <summary>
/// Risk factor information.
/// </summary>
public class RiskFactor
{
    /// <summary>
    /// Gets or sets the factor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the factor value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the weight.
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Verification details.
/// </summary>
public class VerificationDetails
{
    /// <summary>
    /// Gets or sets the verification method.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification steps.
    /// </summary>
    public List<VerificationStep> Steps { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Verification step information.
/// </summary>
public class VerificationStep
{
    /// <summary>
    /// Gets or sets the step name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the step passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the step details.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets when the step was completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// KYC history entry.
/// </summary>
public class KycHistoryEntry
{
    /// <summary>
    /// Gets or sets the entry ID.
    /// </summary>
    public string EntryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the performed by user.
    /// </summary>
    public string? PerformedBy { get; set; }
}

/// <summary>
/// Screening result information.
/// </summary>
public class ScreeningResult
{
    /// <summary>
    /// Gets or sets the screening type.
    /// </summary>
    public string ScreeningType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether a match was found.
    /// </summary>
    public bool MatchFound { get; set; }

    /// <summary>
    /// Gets or sets the match details.
    /// </summary>
    public List<string> Matches { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Notification details.
/// </summary>
public class NotificationDetails
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipients.
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Gets or sets when the notification was sent.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gets or sets the notification method.
    /// </summary>
    public string Method { get; set; } = string.Empty;
}

/// <summary>
/// Watchlist entry information.
/// </summary>
public class WatchlistEntry
{
    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when it was added.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Gets or sets who added it.
    /// </summary>
    public string AddedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when it expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Risk factor result.
/// </summary>
public class RiskFactorResult
{
    /// <summary>
    /// Gets or sets the factor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the score contribution.
    /// </summary>
    public int ScoreContribution { get; set; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Risk history entry.
/// </summary>
public class RiskHistoryEntry
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the risk score.
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets what triggered the change.
    /// </summary>
    public string Trigger { get; set; } = string.Empty;
}

/// <summary>
/// Risk analysis details.
/// </summary>
public class RiskAnalysis
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the methodology.
    /// </summary>
    public string Methodology { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data sources.
    /// </summary>
    public List<string> DataSources { get; set; } = new();

    /// <summary>
    /// Gets or sets the findings.
    /// </summary>
    public List<string> Findings { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime PerformedAt { get; set; }
}

/// <summary>
/// Compliance address information.
/// </summary>
public class ComplianceAddress
{
    /// <summary>
    /// Gets or sets the street address.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;
}
