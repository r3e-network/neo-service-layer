using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.Permissions.Models;

/// <summary>
/// Result of a policy operation.
/// </summary>
public class PolicyResult
{
    public bool Success { get; set; }
    public string PolicyId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a permission in the system.
/// </summary>
public class Permission
{
    /// <summary>
    /// Gets or sets the permission ID.
    /// </summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource identifier.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action allowed on the resource.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission scope.
    /// </summary>
    public PermissionScope Scope { get; set; }

    /// <summary>
    /// Gets or sets additional conditions for the permission.
    /// </summary>
    public Dictionary<string, object> Conditions { get; set; } = new();

    /// <summary>
    /// Gets or sets when the permission was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the permission expires (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Permission scope enumeration.
/// </summary>
public enum PermissionScope
{
    /// <summary>Global scope.</summary>
    Global,
    /// <summary>Service-specific scope.</summary>
    Service,
    /// <summary>User-specific scope.</summary>
    User,
    /// <summary>Resource-specific scope.</summary>
    Resource
}

/// <summary>
/// Represents a role in the system.
/// </summary>
public class Role
{
    /// <summary>
    /// Gets or sets the role ID.
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permissions associated with this role.
    /// </summary>
    public List<Permission> Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this is a system role.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Gets or sets the role priority (higher priority overrides lower).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets when the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets who created the role.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Data access policy for fine-grained access control.
/// </summary>
public class DataAccessPolicy
{
    /// <summary>
    /// Gets or sets the policy ID.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource pattern this policy applies to.
    /// </summary>
    public string ResourcePattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the principals (users/services) this policy applies to.
    /// </summary>
    public List<Principal> Principals { get; set; } = new();

    /// <summary>
    /// Gets or sets the allowed actions.
    /// </summary>
    public List<string> AllowedActions { get; set; } = new();

    /// <summary>
    /// Gets or sets the denied actions.
    /// </summary>
    public List<string> DeniedActions { get; set; } = new();

    /// <summary>
    /// Gets or sets the policy conditions.
    /// </summary>
    public PolicyConditions Conditions { get; set; } = new();

    /// <summary>
    /// Gets or sets the policy effect (Allow/Deny).
    /// </summary>
    public PolicyEffect Effect { get; set; }

    /// <summary>
    /// Gets or sets the policy priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets when the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the policy was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// Represents a principal (user or service) in the permission system.
/// </summary>
public class Principal
{
    /// <summary>
    /// Gets or sets the principal ID.
    /// </summary>
    public string PrincipalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the principal type.
    /// </summary>
    public PrincipalType Type { get; set; }

    /// <summary>
    /// Gets or sets the principal name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Principal type enumeration.
/// </summary>
public enum PrincipalType
{
    /// <summary>User principal.</summary>
    User,
    /// <summary>Service principal.</summary>
    Service,
    /// <summary>Role principal.</summary>
    Role,
    /// <summary>Group principal.</summary>
    Group
}

/// <summary>
/// Policy conditions for fine-grained control.
/// </summary>
public class PolicyConditions
{
    /// <summary>
    /// Gets or sets IP address restrictions.
    /// </summary>
    public List<string> IpAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets time-based restrictions.
    /// </summary>
    public TimeRestriction? TimeRestriction { get; set; }

    /// <summary>
    /// Gets or sets attribute-based conditions.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();

    /// <summary>
    /// Gets or sets blockchain-specific conditions.
    /// </summary>
    public BlockchainConditions? BlockchainConditions { get; set; }
}

/// <summary>
/// Time-based access restrictions.
/// </summary>
public class TimeRestriction
{
    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets allowed days of the week.
    /// </summary>
    public List<DayOfWeek> AllowedDays { get; set; } = new();

    /// <summary>
    /// Gets or sets allowed hours of the day.
    /// </summary>
    public List<int> AllowedHours { get; set; } = new();
}

/// <summary>
/// Blockchain-specific access conditions.
/// </summary>
public class BlockchainConditions
{
    /// <summary>
    /// Gets or sets required token balance.
    /// </summary>
    public decimal? MinimumTokenBalance { get; set; }

    /// <summary>
    /// Gets or sets required NFT ownership.
    /// </summary>
    public List<string> RequiredNFTs { get; set; } = new();

    /// <summary>
    /// Gets or sets required smart contract interactions.
    /// </summary>
    public List<string> RequiredContracts { get; set; } = new();
}

/// <summary>
/// Policy effect enumeration.
/// </summary>
public enum PolicyEffect
{
    /// <summary>Allow access.</summary>
    Allow,
    /// <summary>Deny access.</summary>
    Deny
}

/// <summary>
/// Access type enumeration.
/// </summary>
public enum AccessType
{
    /// <summary>Read access.</summary>
    Read,
    /// <summary>Write access.</summary>
    Write,
    /// <summary>Delete access.</summary>
    Delete,
    /// <summary>Execute access.</summary>
    Execute,
    /// <summary>Full access.</summary>
    Full
}

/// <summary>
/// Service permission registration.
/// </summary>
public class ServicePermissionRegistration
{
    /// <summary>
    /// Gets or sets the service ID.
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service permissions.
    /// </summary>
    public List<ServicePermission> Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the service API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service certificate thumbprint.
    /// </summary>
    public string? CertificateThumbprint { get; set; }
}

/// <summary>
/// Service-specific permission.
/// </summary>
public class ServicePermission
{
    /// <summary>
    /// Gets or sets the resource pattern.
    /// </summary>
    public string ResourcePattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the allowed access types.
    /// </summary>
    public List<AccessType> AllowedAccess { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this permission can be delegated.
    /// </summary>
    public bool CanDelegate { get; set; }
}

/// <summary>
/// Permission check result.
/// </summary>
public class PermissionCheckResult
{
    /// <summary>
    /// Gets or sets whether access is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the reason for denial (if applicable).
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// Gets or sets the matching policy (if any).
    /// </summary>
    public string? MatchingPolicy { get; set; }

    /// <summary>
    /// Gets or sets additional conditions that must be met.
    /// </summary>
    public Dictionary<string, object> AdditionalConditions { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp of the check.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Permission operation result.
/// </summary>
public class PermissionResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message (if any).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the affected permission ID.
    /// </summary>
    public string? PermissionId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Role operation result.
/// </summary>
public class RoleResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message (if any).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Grant permission request.
/// </summary>
public class GrantPermissionRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission to grant.
    /// </summary>
    public Permission Permission { get; set; } = new();

    /// <summary>
    /// Gets or sets who is granting the permission.
    /// </summary>
    public string GrantedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for granting.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Revoke permission request.
/// </summary>
public class RevokePermissionRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission ID to revoke.
    /// </summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who is revoking the permission.
    /// </summary>
    public string RevokedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for revoking.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Data access request for policy evaluation.
/// </summary>
public class DataAccessRequest
{
    /// <summary>
    /// Gets or sets the principal making the request.
    /// </summary>
    public Principal Principal { get; set; } = new();

    /// <summary>
    /// Gets or sets the resource being accessed.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action being performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request context.
    /// </summary>
    public RequestContext Context { get; set; } = new();
}

/// <summary>
/// Request context for policy evaluation.
/// </summary>
public class RequestContext
{
    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the request timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional context attributes.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Policy evaluation result.
/// </summary>
public class PolicyEvaluationResult
{
    /// <summary>
    /// Gets or sets whether access is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the evaluated policies.
    /// </summary>
    public List<EvaluatedPolicy> EvaluatedPolicies { get; set; } = new();

    /// <summary>
    /// Gets or sets the final decision reason.
    /// </summary>
    public string DecisionReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets obligations that must be fulfilled.
    /// </summary>
    public List<PolicyObligation> Obligations { get; set; } = new();
}

/// <summary>
/// Evaluated policy information.
/// </summary>
public class EvaluatedPolicy
{
    /// <summary>
    /// Gets or sets the policy ID.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the evaluation result.
    /// </summary>
    public PolicyEffect Effect { get; set; }

    /// <summary>
    /// Gets or sets whether the policy matched.
    /// </summary>
    public bool Matched { get; set; }
}

/// <summary>
/// Policy obligation that must be fulfilled.
/// </summary>
public class PolicyObligation
{
    /// <summary>
    /// Gets or sets the obligation type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the obligation parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Permission audit log entry.
/// </summary>
public class PermissionAuditLog
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public string LogId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the principal ID.
    /// </summary>
    public string PrincipalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the principal type.
    /// </summary>
    public PrincipalType PrincipalType { get; set; }

    /// <summary>
    /// Gets or sets the resource accessed.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether access was granted.
    /// </summary>
    public bool AccessGranted { get; set; }

    /// <summary>
    /// Gets or sets the denial reason (if applicable).
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// Gets or sets the request context.
    /// </summary>
    public RequestContext Context { get; set; } = new();
}

/// <summary>
/// Audit log filter.
/// </summary>
public class AuditLogFilter
{
    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the principal ID filter.
    /// </summary>
    public string? PrincipalId { get; set; }

    /// <summary>
    /// Gets or sets the resource filter.
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Gets or sets whether to include only denied access.
    /// </summary>
    public bool? OnlyDenied { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of records.
    /// </summary>
    public int MaxRecords { get; set; } = 1000;
}

/// <summary>
/// Token validation result.
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Gets or sets whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the principal ID from the token.
    /// </summary>
    public string? PrincipalId { get; set; }

    /// <summary>
    /// Gets or sets the token scope.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the token expiry.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the validation error (if any).
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Role assignment result.
/// </summary>
public class RoleAssignmentResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message (if any).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the affected user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected role ID.
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Service registration result.
/// </summary>
public class ServiceRegistrationResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message (if any).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the service ID.
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated API key (for new registrations).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}