using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Compliance.Models;

/// <summary>
/// Request model for compliance check operations.
/// </summary>
public class ComplianceCheckRequest
{
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the transaction data to check.
    /// </summary>
    [Required]
    public string TransactionData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    [Required]
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the contract address if applicable.
    /// </summary>
    [StringLength(42)]
    public string? ContractAddress { get; set; }

    /// <summary>
    /// Gets or sets additional context for the compliance check.
    /// </summary>
    public Dictionary<string, string> Context { get; set; } = new();
}

/// <summary>
/// Request model for generating compliance reports.
/// </summary>
public class ComplianceReportRequest
{
    /// <summary>
    /// Gets or sets the report type.
    /// </summary>
    [Required]
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date for the report.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the report.
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the addresses to include in the report.
    /// </summary>
    public List<string> Addresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction types to include.
    /// </summary>
    public List<string> TransactionTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the compliance rules to check against.
    /// </summary>
    public List<string> RuleIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the output format for the report.
    /// </summary>
    public string OutputFormat { get; set; } = "JSON";

    /// <summary>
    /// Gets or sets additional parameters for the report.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Request model for creating compliance rules.
/// </summary>
public class CreateComplianceRuleRequest
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description (alias for Description).
    /// </summary>
    [Required]
    [StringLength(500)]
    public string RuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule type.
    /// </summary>
    [Required]
    public string RuleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    [Range(0, 100)]
    public int Severity { get; set; } = 50;

    /// <summary>
    /// Gets or sets the rule conditions.
    /// </summary>
    public List<ComplianceCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the rule is enabled (alias for Enabled).
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the rule category.
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Gets or sets the rule tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Request model for updating compliance rules.
/// </summary>
public class UpdateComplianceRuleRequest
{
    /// <summary>
    /// Gets or sets the rule ID to update.
    /// </summary>
    [Required]
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated rule name.
    /// </summary>
    [StringLength(100)]
    public string? RuleName { get; set; }

    /// <summary>
    /// Gets or sets the updated rule description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated rule description (alias for Description).
    /// </summary>
    [StringLength(500)]
    public string? RuleDescription { get; set; }

    /// <summary>
    /// Gets or sets the updated rule conditions.
    /// </summary>
    public List<ComplianceCondition>? Conditions { get; set; }

    /// <summary>
    /// Gets or sets the updated rule parameters.
    /// </summary>
    public Dictionary<string, string>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the updated severity level.
    /// </summary>
    [Range(0, 100)]
    public int? Severity { get; set; }

    /// <summary>
    /// Gets or sets whether the rule is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether the rule is enabled (alias for Enabled).
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the updated rule category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the updated rule tags.
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request model for deleting compliance rules.
/// </summary>
public class DeleteComplianceRuleRequest
{
    /// <summary>
    /// Gets or sets the rule ID to delete.
    /// </summary>
    [Required]
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for deletion.
    /// </summary>
    [StringLength(200)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets whether to perform a soft delete.
    /// </summary>
    public bool SoftDelete { get; set; } = true;
}

/// <summary>
/// Request model for getting compliance rules.
/// </summary>
public class GetComplianceRulesRequest
{
    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the rule category filter.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the enabled status filter.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the rule type filter.
    /// </summary>
    public string? RuleType { get; set; }

    /// <summary>
    /// Gets or sets the search query for rule names.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortField { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public string SortDirection { get; set; } = "DESC";

    /// <summary>
    /// Gets or sets whether to only return enabled rules.
    /// </summary>
    public bool IsEnabledOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of records to skip for pagination.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets or sets the number of records to take for pagination.
    /// </summary>
    public int Take => PageSize;
}

/// <summary>
/// Request model for starting audits.
/// </summary>
public class StartAuditRequest
{
    /// <summary>
    /// Gets or sets the audit name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string AuditName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit type.
    /// </summary>
    [Required]
    public string AuditType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audit scope.
    /// </summary>
    [Required]
    public AuditScope Scope { get; set; } = new();

    /// <summary>
    /// Gets or sets the audit schedule.
    /// </summary>
    public AuditSchedule? Schedule { get; set; }

    /// <summary>
    /// Gets or sets the notification settings.
    /// </summary>
    public AuditNotificationSettings? NotificationSettings { get; set; }

    /// <summary>
    /// Gets or sets additional audit parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Request model for getting audit status.
/// </summary>
public class GetAuditStatusRequest
{
    /// <summary>
    /// Gets or sets the audit ID.
    /// </summary>
    [Required]
    public string AuditId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include detailed results.
    /// </summary>
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include logs.
    /// </summary>
    public bool IncludeLogs { get; set; } = false;
}

/// <summary>
/// Request model for reporting violations.
/// </summary>
public class ReportViolationRequest
{
    /// <summary>
    /// Gets or sets the violation type.
    /// </summary>
    [Required]
    public string ViolationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the violation.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash related to the violation.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the address related to the violation.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the severity of the violation.
    /// </summary>
    [Range(0, 100)]
    public int Severity { get; set; } = 50;

    /// <summary>
    /// Gets or sets the evidence or supporting data.
    /// </summary>
    public Dictionary<string, string> Evidence { get; set; } = new();

    /// <summary>
    /// Gets or sets the source of the violation report.
    /// </summary>
    public string Source { get; set; } = "Manual";
}

/// <summary>
/// Request model for getting violations.
/// </summary>
public class GetViolationsRequest
{
    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the violation type filter.
    /// </summary>
    public string? ViolationType { get; set; }

    /// <summary>
    /// Gets or sets the severity range filter.
    /// </summary>
    public SeverityRange? SeverityRange { get; set; }

    /// <summary>
    /// Gets or sets the date range filter.
    /// </summary>
    public DateRange? DateRange { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the address filter.
    /// </summary>
    public string? Address { get; set; }
}

/// <summary>
/// Request model for creating remediation plans.
/// </summary>
public class CreateRemediationPlanRequest
{
    /// <summary>
    /// Gets or sets the violation ID.
    /// </summary>
    [Required]
    public string ViolationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remediation plan title.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remediation plan description.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remediation actions.
    /// </summary>
    public List<RemediationAction> Actions { get; set; } = new();

    /// <summary>
    /// Gets or sets the target completion date.
    /// </summary>
    public DateTime? TargetCompletionDate { get; set; }

    /// <summary>
    /// Gets or sets the assigned responsible party.
    /// </summary>
    [StringLength(100)]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Gets or sets the priority level.
    /// </summary>
    public string Priority { get; set; } = "Medium";
}

/// <summary>
/// Request model for compliance dashboard.
/// </summary>
public class ComplianceDashboardRequest
{
    /// <summary>
    /// Gets or sets the time range for the dashboard data.
    /// </summary>
    public DateRange? DateRange { get; set; }

    /// <summary>
    /// Gets or sets the specific metrics to include.
    /// </summary>
    public List<string> Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the grouping level for data aggregation.
    /// </summary>
    public string GroupBy { get; set; } = "Day";

    /// <summary>
    /// Gets or sets whether to include trend analysis.
    /// </summary>
    public bool IncludeTrends { get; set; } = true;
}

/// <summary>
/// Request model for certification requests.
/// </summary>
public class RequestCertificationRequest
{
    /// <summary>
    /// Gets or sets the certification type.
    /// </summary>
    [Required]
    public string CertificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity requesting certification.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity address or identifier.
    /// </summary>
    [Required]
    public string EntityIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certification scope.
    /// </summary>
    public List<string> Scope { get; set; } = new();

    /// <summary>
    /// Gets or sets supporting documentation.
    /// </summary>
    public Dictionary<string, string> Documentation { get; set; } = new();

    /// <summary>
    /// Gets or sets the contact information.
    /// </summary>
    public ContactInformation? ContactInfo { get; set; }
}

// AML/KYC Request Models

/// <summary>
/// Request model for KYC verification.
/// </summary>
public class KycVerificationRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification type.
    /// </summary>
    [Required]
    public string VerificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity documents.
    /// </summary>
    public List<IdentityDocument> Documents { get; set; } = new();

    /// <summary>
    /// Gets or sets the personal information.
    /// </summary>
    [Required]
    public PersonalInformation PersonalInfo { get; set; } = new();

    /// <summary>
    /// Gets or sets the verification level requested.
    /// </summary>
    public string VerificationLevel { get; set; } = "Standard";
}

/// <summary>
/// Request model for getting KYC status.
/// </summary>
public class GetKycStatusRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include verification history.
    /// </summary>
    public bool IncludeHistory { get; set; } = false;
}

/// <summary>
/// Request model for AML screening.
/// </summary>
public class AmlScreeningRequest
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    [Required]
    public TransactionData Transaction { get; set; } = new();

    /// <summary>
    /// Gets or sets the screening types to perform.
    /// </summary>
    public List<string> ScreeningTypes { get; set; } = new() { "Sanctions", "PEP", "Watchlist" };

    /// <summary>
    /// Gets or sets the risk threshold.
    /// </summary>
    [Range(0, 100)]
    public int RiskThreshold { get; set; } = 70;
}

/// <summary>
/// Request model for reporting suspicious activity.
/// </summary>
public class SuspiciousActivityRequest
{
    /// <summary>
    /// Gets or sets the activity type.
    /// </summary>
    [Required]
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related entity.
    /// </summary>
    [Required]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the activity details.
    /// </summary>
    [Required]
    public SuspiciousActivityDetails Details { get; set; } = new();

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public string Severity { get; set; } = "Medium";

    /// <summary>
    /// Gets or sets whether to notify authorities.
    /// </summary>
    public bool NotifyAuthorities { get; set; } = false;
}

/// <summary>
/// Request model for getting watchlist.
/// </summary>
public class GetWatchlistRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the filter criteria.
    /// </summary>
    public WatchlistFilter? Filter { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public string SortBy { get; set; } = "DateAdded";
}

/// <summary>
/// Request model for adding to watchlist.
/// </summary>
public class AddToWatchlistRequest
{
    /// <summary>
    /// Gets or sets the address to add.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for adding.
    /// </summary>
    [Required]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = "Medium";

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request model for removing from watchlist.
/// </summary>
public class RemoveFromWatchlistRequest
{
    /// <summary>
    /// Gets or sets the address to remove.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for removal.
    /// </summary>
    public string? RemovalReason { get; set; }
}

/// <summary>
/// Request model for risk assessment.
/// </summary>
public class RiskAssessmentRequest
{
    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    [Required]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    [Required]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assessment factors.
    /// </summary>
    public List<RiskFactor> Factors { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include historical data.
    /// </summary>
    public bool IncludeHistory { get; set; } = true;

    /// <summary>
    /// Gets or sets the assessment depth.
    /// </summary>
    public string AssessmentDepth { get; set; } = "Standard";
}

/// <summary>
/// Request model for getting risk profile.
/// </summary>
public class GetRiskProfileRequest
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    [Required]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include detailed analysis.
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the time range for historical data.
    /// </summary>
    public DateRange? TimeRange { get; set; }
}
