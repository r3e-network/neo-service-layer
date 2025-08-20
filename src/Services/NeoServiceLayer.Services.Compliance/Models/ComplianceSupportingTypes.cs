using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Compliance.Models;

/// <summary>
/// Represents a compliance condition for rules.
/// </summary>
public class ComplianceCondition
{
    /// <summary>
    /// Gets or sets the condition identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

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
    public object Value { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the condition is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents a compliance rule.
/// </summary>
public class ComplianceRule
{
    public string Id { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;  // Additional property
    public bool IsActive { get; set; }
    public List<ComplianceCondition> Conditions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    
    // Additional properties for compatibility
    public string RuleName 
    { 
        get => Name; 
        set => Name = value; 
    }
    public string RuleDescription 
    { 
        get => Description; 
        set => Description = value; 
    }
    public int Severity { get; set; } = 1;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool Enabled 
    { 
        get => IsActive; 
        set => IsActive = value; 
    }
}

/// <summary>
/// Represents transaction data for compliance checking.
/// </summary>
public class TransactionData
{
    public string Id { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents violation information.
/// </summary>
public class ViolationInfo
{
    /// <summary>
    /// Gets or sets the violation identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the violation type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the violation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public string Severity { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the violation status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the violation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the related rule identifier.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the result of a risk factor evaluation.
/// </summary>
public class RiskFactorResult
{
    /// <summary>
    /// Gets or sets the risk factor identifier.
    /// </summary>
    public string FactorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the risk factor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the risk score (0.0 to 1.0).
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level (Low, Medium, High, Critical).
    /// </summary>
    public string RiskLevel { get; set; } = "Medium";

    /// <summary>
    /// Gets or sets the confidence of the evaluation.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the description of the risk factor.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the score contribution.
    /// </summary>
    public double ScoreContribution { get; set; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any mitigating factors.
    /// </summary>
    public List<string> MitigatingFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets recommended actions.
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>
    /// Gets or sets when the evaluation was performed.
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata about the evaluation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a compliance violation.
/// </summary>
public class ComplianceViolation
{
    /// <summary>
    /// Gets or sets the violation identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation ID (alias for Id).
    /// </summary>
    public string ViolationId
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Gets or sets the violation type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the related address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets when the violation occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets when the violation was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a compliance report.
/// </summary>
public class ComplianceReport
{
    /// <summary>
    /// Gets or sets the report identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report ID (alias for Id).
    /// </summary>
    public string ReportId
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Gets or sets the report name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the report type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the report data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the report status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents audit scope configuration.
/// </summary>
public class AuditScope
{
    /// <summary>
    /// Gets or sets the addresses to include.
    /// </summary>
    public List<string> Addresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction types.
    /// </summary>
    public List<string> TransactionTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the date range.
    /// </summary>
    public DateRange DateRange { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include historical data.
    /// </summary>
    public bool IncludeHistorical { get; set; }
}

/// <summary>
/// Represents a date range.
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
    public bool IsValid => EndDate >= StartDate;
}

/// <summary>
/// Represents a severity range.
/// </summary>
public class SeverityRange
{
    /// <summary>
    /// Gets or sets the minimum severity.
    /// </summary>
    public string MinSeverity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum severity.
    /// </summary>
    public string MaxSeverity { get; set; } = string.Empty;
}

/// <summary>
/// Represents audit schedule configuration.
/// </summary>
public class AuditSchedule
{
    /// <summary>
    /// Gets or sets the schedule type.
    /// </summary>
    public string ScheduleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the interval.
    /// </summary>
    public TimeSpan Interval { get; set; }

    /// <summary>
    /// Gets or sets whether enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Represents audit notification settings.
/// </summary>
public class AuditNotificationSettings
{
    /// <summary>
    /// Gets or sets whether to notify on start.
    /// </summary>
    public bool NotifyOnStart { get; set; }

    /// <summary>
    /// Gets or sets whether to notify on completion.
    /// </summary>
    public bool NotifyOnCompletion { get; set; }

    /// <summary>
    /// Gets or sets email addresses.
    /// </summary>
    public List<string> EmailAddresses { get; set; } = new();
}

/// <summary>
/// Represents a remediation action.
/// </summary>
public class RemediationAction
{
    /// <summary>
    /// Gets or sets the action ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents contact information.
/// </summary>
public class ContactInformation
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Represents identity document information.
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
    public DateTime ExpiryDate { get; set; }
}

/// <summary>
/// Represents personal information.
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
}

/// <summary>
/// Represents suspicious activity details.
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
    /// Gets or sets when it occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Enumeration for watchlist filter types.
/// </summary>
public enum WatchlistFilter
{
    All,
    Active,
    Inactive,
    PoliticallyExposed,
    Sanctions,
    Criminal
}

/// <summary>
/// Enumeration for risk factor levels.
/// </summary>
public enum RiskFactorLevel
{
    LowRisk,
    MediumRisk,
    HighRisk,
    CriticalRisk,
    Unknown
}

/// <summary>
/// Enumeration for risk levels.
/// </summary>
public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical,
    Unknown
}

/// <summary>
/// Enumeration for report types.
/// </summary>
public enum ReportType
{
    Compliance,
    Audit,
    Transaction,
    Violation,
    Risk,
    Certification
}

/// <summary>
/// Enumeration for report status.
/// </summary>
public enum ReportStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Enumeration for violation status.
/// </summary>
public enum ViolationStatus
{
    Open,
    InReview,
    Resolved,
    Dismissed,
    Escalated
}

/// <summary>
/// Enumeration for audit status.
/// </summary>
public enum AuditStatus
{
    Scheduled,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Enumeration for audit findings.
/// </summary>
public enum AuditFinding
{
    NoIssues,
    MinorIssues,
    MajorIssues,
    CriticalIssues,
    ComplianceViolation
}

/// <summary>
/// Enumeration for certification status.
/// </summary>
public enum CertificationStatus
{
    Valid,
    Expired,
    Suspended,
    Revoked,
    Pending
}

// Additional Compliance Types

public enum RemediationStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Verified
}

public class RemediationStep
{
    public string StepId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RemediationStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ResponsibleParty { get; set; } = string.Empty;
}

public enum KycStatus
{
    NotStarted,
    Pending,
    InReview,
    Approved,
    Rejected,
    Expired
}

public enum KycLevel
{
    None,
    Basic,
    Enhanced,
    Full
}

public class ComplianceMetrics
{
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
    public double ComplianceScore { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class AmlAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
}

/// <summary>
/// Represents a verification step in the KYC process.
/// </summary>
public class VerificationStep
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Represents verification details for KYC.
/// </summary>
public class VerificationDetails
{
    public List<VerificationStep> Steps { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents a KYC history entry.
/// </summary>
public class KycHistoryEntry
{
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Represents a screening result for AML/KYC.
/// </summary>
public class ScreeningResult
{
    public string Type { get; set; } = string.Empty;
    public bool IsClear { get; set; }
    public List<string> Matches { get; set; } = new();
    public double RiskScore { get; set; }
}