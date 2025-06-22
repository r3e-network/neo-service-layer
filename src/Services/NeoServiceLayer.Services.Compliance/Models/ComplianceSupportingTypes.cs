namespace NeoServiceLayer.Services.Compliance.Models;

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
