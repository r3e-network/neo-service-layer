using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Permissions.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Permissions;

/// <summary>
/// Interface for the Permissions service providing role-based access control.
/// </summary>
public interface IPermissionsService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Creates a new role with specified permissions.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="permissions">The list of permissions for the role.</param>
    /// <param name="description">The role description.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the role was created successfully.</returns>
    Task<bool> CreateRoleAsync(
        string roleName,
        IEnumerable<string> permissions,
        string description,
        BlockchainType blockchainType);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleName">The role to assign.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the role was assigned successfully.</returns>
    Task<bool> AssignRoleAsync(
        string userId,
        string roleName,
        BlockchainType blockchainType);

    /// <summary>
    /// Revokes a role from a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleName">The role to revoke.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the role was revoked successfully.</returns>
    Task<bool> RevokeRoleAsync(
        string userId,
        string roleName,
        BlockchainType blockchainType);

    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the user has the permission.</returns>
    Task<bool> HasPermissionAsync(
        string userId,
        string permission,
        BlockchainType blockchainType);

    /// <summary>
    /// Gets all roles assigned to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of roles.</returns>
    Task<IEnumerable<Role>> GetUserRolesAsync(
        string userId,
        BlockchainType blockchainType);

    /// <summary>
    /// Gets all permissions for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of permissions.</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(
        string userId,
        BlockchainType blockchainType);

    /// <summary>
    /// Adds a permission to a role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="permission">The permission to add.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the permission was added successfully.</returns>
    Task<bool> AddPermissionToRoleAsync(
        string roleName,
        string permission,
        BlockchainType blockchainType);

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="permission">The permission to remove.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the permission was removed successfully.</returns>
    Task<bool> RemovePermissionFromRoleAsync(
        string roleName,
        string permission,
        BlockchainType blockchainType);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <param name="roleName">The role to delete.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the role was deleted successfully.</returns>
    Task<bool> DeleteRoleAsync(
        string roleName,
        BlockchainType blockchainType);

    /// <summary>
    /// Gets all defined roles.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of all roles.</returns>
    Task<IEnumerable<Role>> GetAllRolesAsync(BlockchainType blockchainType);

    /// <summary>
    /// Creates a permission policy.
    /// </summary>
    /// <param name="policy">The policy to create.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the policy was created successfully.</returns>
    Task<bool> CreatePolicyAsync(
        PermissionPolicy policy,
        BlockchainType blockchainType);

    /// <summary>
    /// Evaluates a permission policy for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="policyName">The policy to evaluate.</param>
    /// <param name="context">The evaluation context.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The policy evaluation result.</returns>
    Task<PolicyEvaluationResult> EvaluatePolicyAsync(
        string userId,
        string policyName,
        Dictionary<string, object> context,
        BlockchainType blockchainType);

    /// <summary>
    /// Gets the audit log for permission changes.
    /// </summary>
    /// <param name="startTime">The start time for the audit log.</param>
    /// <param name="endTime">The end time for the audit log.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of audit entries.</returns>
    Task<IEnumerable<PermissionAuditEntry>> GetAuditLogAsync(
        DateTime startTime,
        DateTime endTime,
        BlockchainType blockchainType);

    /// <summary>
    /// Evaluates data access policies for a request.
    /// </summary>
    Task<Models.PolicyEvaluationResult> EvaluateDataAccessAsync(DataAccessRequest request);

    /// <summary>
    /// Registers a service with its permissions.
    /// </summary>
    Task<ServiceRegistrationResult> RegisterServiceAsync(Models.ServicePermissionRegistration registration);

    /// <summary>
    /// Updates service permissions.
    /// </summary>
    Task<ServiceRegistrationResult> UpdateServicePermissionsAsync(string serviceId, List<Models.ServicePermission> permissions);

    /// <summary>
    /// Gets audit logs with optional filtering.
    /// </summary>
    Task<IEnumerable<Models.PermissionAuditLog>> GetAuditLogsAsync(AuditLogFilter filter);

    /// <summary>
    /// Validates a permission token.
    /// </summary>
    Task<TokenValidationResult> ValidatePermissionTokenAsync(string token);

    /// <summary>
    /// Generates a permission token for a user.
    /// </summary>
    Task<string> GeneratePermissionTokenAsync(string userId, string scope, TimeSpan expiry);
}

/// <summary>
/// Represents a role in the permission system.
/// </summary>
public class Role
{
    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of permissions.
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the role is system-defined.
    /// </summary>
    public bool IsSystemRole { get; set; }
}

/// <summary>
/// Represents a permission policy.
/// </summary>
public class PermissionPolicy
{
    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy rules.
    /// </summary>
    public List<PolicyRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the policy effect (Allow/Deny).
    /// </summary>
    public PolicyEffect Effect { get; set; }

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Represents a policy rule.
/// </summary>
public class PolicyRule
{
    /// <summary>
    /// Gets or sets the resource pattern.
    /// </summary>
    public string ResourcePattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the allowed actions.
    /// </summary>
    public List<string> Actions { get; set; } = new();

    /// <summary>
    /// Gets or sets the conditions.
    /// </summary>
    public Dictionary<string, object> Conditions { get; set; } = new();
}

/// <summary>
/// Policy effect enumeration.
/// </summary>
public enum PolicyEffect
{
    /// <summary>
    /// Allow the action.
    /// </summary>
    Allow,

    /// <summary>
    /// Deny the action.
    /// </summary>
    Deny
}

/// <summary>
/// Represents a policy evaluation result.
/// </summary>
public class PolicyEvaluationResult
{
    /// <summary>
    /// Gets or sets whether access is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the matched policy.
    /// </summary>
    public string? MatchedPolicy { get; set; }

    /// <summary>
    /// Gets or sets the reason for the decision.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional context.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Represents an audit entry for permission changes.
/// </summary>
public class PermissionAuditEntry
{
    /// <summary>
    /// Gets or sets the audit entry ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user who made the change.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target of the action.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the old value.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets whether the action was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
}