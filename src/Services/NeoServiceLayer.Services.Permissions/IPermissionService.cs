using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Permissions.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Permissions;

/// <summary>
/// Interface for the permission management service.
/// </summary>
public interface IPermissionService : IService
{
    /// <summary>
    /// Checks if a user has permission to perform an action on a resource.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="resource">The resource identifier.</param>
    /// <param name="action">The action to perform.</param>
    /// <returns>True if permitted, false otherwise.</returns>
    Task<bool> CheckPermissionAsync(string userId, string resource, string action);

    /// <summary>
    /// Checks if a service has permission to access data.
    /// </summary>
    /// <param name="serviceId">The service ID.</param>
    /// <param name="dataKey">The data key.</param>
    /// <param name="accessType">The access type (read/write/delete).</param>
    /// <returns>Permission check result.</returns>
    Task<PermissionCheckResult> CheckServicePermissionAsync(string serviceId, string dataKey, AccessType accessType);

    /// <summary>
    /// Grants a permission to a user.
    /// </summary>
    /// <param name="request">The grant permission request.</param>
    /// <returns>Grant result.</returns>
    Task<Models.PermissionResult> GrantPermissionAsync(GrantPermissionRequest request);

    /// <summary>
    /// Revokes a permission from a user.
    /// </summary>
    /// <param name="request">The revoke permission request.</param>
    /// <returns>Revoke result.</returns>
    Task<Models.PermissionResult> RevokePermissionAsync(RevokePermissionRequest request);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="role">The role to create.</param>
    /// <returns>Creation result.</returns>
    Task<RoleResult> CreateRoleAsync(Models.Role role);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="role">The role to update.</param>
    /// <returns>Update result.</returns>
    Task<RoleResult> UpdateRoleAsync(Role role);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <returns>Deletion result.</returns>
    Task<RoleResult> DeleteRoleAsync(string roleId);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleId">The role ID.</param>
    /// <returns>Assignment result.</returns>
    Task<RoleAssignmentResult> AssignRoleAsync(string userId, string roleId);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleId">The role ID.</param>
    /// <returns>Removal result.</returns>
    Task<RoleAssignmentResult> RemoveRoleAsync(string userId, string roleId);

    /// <summary>
    /// Gets all roles for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of roles.</returns>
    Task<IEnumerable<Role>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Gets all permissions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of permissions.</returns>
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId);

    /// <summary>
    /// Creates a data access policy.
    /// </summary>
    /// <param name="policy">The policy to create.</param>
    /// <returns>Creation result.</returns>
    Task<PolicyResult> CreateDataAccessPolicyAsync(DataAccessPolicy policy);

    /// <summary>
    /// Updates a data access policy.
    /// </summary>
    /// <param name="policy">The policy to update.</param>
    /// <returns>Update result.</returns>
    Task<PolicyResult> UpdateDataAccessPolicyAsync(DataAccessPolicy policy);

    /// <summary>
    /// Deletes a data access policy.
    /// </summary>
    /// <param name="policyId">The policy ID.</param>
    /// <returns>Deletion result.</returns>
    Task<PolicyResult> DeleteDataAccessPolicyAsync(string policyId);

    /// <summary>
    /// Evaluates data access policies for a request.
    /// </summary>
    /// <param name="request">The access request.</param>
    /// <returns>Policy evaluation result.</returns>
    Task<PolicyEvaluationResult> EvaluateDataAccessAsync(DataAccessRequest request);

    /// <summary>
    /// Registers a service with its permissions.
    /// </summary>
    /// <param name="registration">The service registration.</param>
    /// <returns>Registration result.</returns>
    Task<ServiceRegistrationResult> RegisterServiceAsync(ServicePermissionRegistration registration);

    /// <summary>
    /// Updates service permissions.
    /// </summary>
    /// <param name="serviceId">The service ID.</param>
    /// <param name="permissions">The updated permissions.</param>
    /// <returns>Update result.</returns>
    Task<ServiceRegistrationResult> UpdateServicePermissionsAsync(string serviceId, IEnumerable<ServicePermission> permissions);

    /// <summary>
    /// Gets audit logs for permission checks.
    /// </summary>
    /// <param name="filter">The audit log filter.</param>
    /// <returns>Audit log entries.</returns>
    Task<IEnumerable<PermissionAuditLog>> GetAuditLogsAsync(AuditLogFilter filter);

    /// <summary>
    /// Validates a permission token.
    /// </summary>
    /// <param name="token">The permission token.</param>
    /// <returns>Token validation result.</returns>
    Task<TokenValidationResult> ValidatePermissionTokenAsync(string token);

    /// <summary>
    /// Generates a permission token for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="scope">The permission scope.</param>
    /// <param name="expiry">Token expiry time.</param>
    /// <returns>Generated token.</returns>
    Task<string> GeneratePermissionTokenAsync(string userId, string scope, TimeSpan expiry);
}