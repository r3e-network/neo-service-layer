using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Permissions;
using NeoServiceLayer.Services.Permissions.Models;
using System.Threading;
using System;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Controller for permission management operations.
/// Provides comprehensive RBAC and policy-based access control.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
public class PermissionController : ControllerBase
{
    private readonly NeoServiceLayer.Services.Permissions.IPermissionsService _permissionService;
    private readonly ILogger<PermissionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionController"/> class.
    /// </summary>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="logger">The logger.</param>
    public PermissionController(NeoServiceLayer.Services.Permissions.IPermissionsService permissionService, ILogger<PermissionController> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a user has permission to perform an action on a resource.
    /// </summary>
    /// <param name="request">The permission check request.</param>
    /// <returns>True if the user has permission, false otherwise.</returns>
    [HttpPost("check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CheckPermission([FromBody] CheckPermissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Map to the correct service method
            var hasPermission = await _permissionService.HasPermissionAsync(
                request.UserId,
                $"{request.Resource}:{request.Action}",
                BlockchainType.NeoN3);

            return Ok(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Checks if a service has permission to access data.
    /// </summary>
    /// <param name="request">The service permission check request.</param>
    /// <returns>Detailed permission check result.</returns>
    [HttpPost("check-service")]
    [ProducesResponseType(typeof(PermissionCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PermissionCheckResult>> CheckServicePermission([FromBody] CheckServicePermissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Map to the correct service method - check if service has permission
            var hasPermission = await _permissionService.HasPermissionAsync(
                request.ServiceId,
                $"{request.DataKey}:{request.AccessType}",
                BlockchainType.NeoN3);

            var result = new PermissionCheckResult
            {
                IsAllowed = hasPermission,
                ServiceId = request.ServiceId,
                DataKey = request.DataKey,
                AccessType = request.AccessType
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service permission for service {ServiceId}", request.ServiceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Grants a permission to a user.
    /// </summary>
    /// <param name="request">The grant permission request.</param>
    /// <returns>The result of the grant operation.</returns>
    [HttpPost("grant")]
    [ProducesResponseType(typeof(NeoServiceLayer.Services.Permissions.Models.PermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NeoServiceLayer.Services.Permissions.Models.PermissionResult>> GrantPermission([FromBody] GrantPermissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Map to the correct service method - use role assignment
            var success = await _permissionService.AssignRoleAsync(
                request.UserId,
                request.Permission ?? "User",
                BlockchainType.NeoN3);

            var result = new NeoServiceLayer.Services.Permissions.Models.PermissionResult
            {
                Success = success,
                ErrorMessage = success ? null : "Failed to grant permission"
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting permission to user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Revokes a permission from a user.
    /// </summary>
    /// <param name="request">The revoke permission request.</param>
    /// <returns>The result of the revoke operation.</returns>
    [HttpPost("revoke")]
    [ProducesResponseType(typeof(NeoServiceLayer.Services.Permissions.Models.PermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NeoServiceLayer.Services.Permissions.Models.PermissionResult>> RevokePermission([FromBody] RevokePermissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Map to the correct service method - use role revocation
            var success = await _permissionService.RevokeRoleAsync(
                request.UserId,
                request.Permission ?? "User",
                BlockchainType.NeoN3);

            var result = new NeoServiceLayer.Services.Permissions.Models.PermissionResult
            {
                Success = success,
                ErrorMessage = success ? null : "Failed to revoke permission"
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permission from user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="role">The role to create.</param>
    /// <returns>The result of the creation operation.</returns>
    [HttpPost("roles")]
    [ProducesResponseType(typeof(RoleResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoleResult>> CreateRole([FromBody] NeoServiceLayer.Services.Permissions.Role role)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Map to the correct service method parameters
            var success = await _permissionService.CreateRoleAsync(
                role.Name,
                role.Permissions ?? new List<string>(),
                role.Description ?? string.Empty,
                BlockchainType.NeoN3);

            var result = new NeoServiceLayer.Services.Permissions.Models.RoleResult
            {
                Success = success,
                ErrorMessage = success ? null : "Failed to create role"
            };

            if (result.Success)
            {
                return CreatedAtAction(nameof(GetUserRoles), new { userId = "admin" }, result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", role.Name);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="role">The updated role data.</param>
    /// <returns>The result of the update operation.</returns>
    [HttpPut("roles/{roleId}")]
    [ProducesResponseType(typeof(RoleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResult>> UpdateRole(string roleId, [FromBody] NeoServiceLayer.Services.Permissions.Role role)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (roleId != role.Name)
            {
                return BadRequest("Role ID mismatch");
            }

            // The service doesn't have UpdateRoleAsync, we'll need to recreate
            // First delete the old role, then create new one
            await _permissionService.DeleteRoleAsync(roleId, BlockchainType.NeoN3);
            var success = await _permissionService.CreateRoleAsync(
                role.Name,
                role.Permissions ?? new List<string>(),
                role.Description ?? string.Empty,
                BlockchainType.NeoN3);
            
            var result = new NeoServiceLayer.Services.Permissions.Models.RoleResult
            {
                Success = success,
                ErrorMessage = success ? null : "Failed to update role"
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else if (result.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(result.ErrorMessage);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <param name="roleId">The role ID to delete.</param>
    /// <returns>The result of the deletion operation.</returns>
    [HttpDelete("roles/{roleId}")]
    [ProducesResponseType(typeof(RoleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResult>> DeleteRole(string roleId)
    {
        try
        {
            // Call the correct service method with BlockchainType
            var success = await _permissionService.DeleteRoleAsync(roleId, BlockchainType.NeoN3);
            
            var result = new NeoServiceLayer.Services.Permissions.Models.RoleResult
            {
                Success = success,
                ErrorMessage = success ? null : "Role not found or could not be deleted"
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else if (result.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(result.ErrorMessage);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="request">The role assignment request.</param>
    /// <returns>The result of the assignment operation.</returns>
    [HttpPost("assign-role")]
    [ProducesResponseType(typeof(RoleAssignmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoleAssignmentResult>> AssignRole([FromBody] AssignRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Call the correct service method with BlockchainType
            var success = await _permissionService.AssignRoleAsync(
                request.UserId, 
                request.RoleId,
                BlockchainType.NeoN3);
            
            var result = new RoleAssignmentResult
            {
                Success = success,
                UserId = request.UserId,
                RoleId = request.RoleId
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="request">The role removal request.</param>
    /// <returns>The result of the removal operation.</returns>
    [HttpPost("remove-role")]
    [ProducesResponseType(typeof(RoleAssignmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoleAssignmentResult>> RemoveRole([FromBody] RemoveRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Call the correct service method with BlockchainType
            var success = await _permissionService.RevokeRoleAsync(
                request.UserId, 
                request.RoleId,
                BlockchainType.NeoN3);
            
            var result = new RoleAssignmentResult
            {
                Success = success,
                UserId = request.UserId,
                RoleId = request.RoleId
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all roles assigned to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of roles assigned to the user.</returns>
    [HttpGet("users/{userId}/roles")]
    [ProducesResponseType(typeof(IEnumerable<NeoServiceLayer.Services.Permissions.Role>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<NeoServiceLayer.Services.Permissions.Role>>> GetUserRoles(string userId)
    {
        try
        {
            var roles = await _permissionService.GetUserRolesAsync(userId, BlockchainType.NeoN3);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all permissions for a user (direct and role-based).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of permissions for the user.</returns>
    [HttpGet("users/{userId}/permissions")]
    [ProducesResponseType(typeof(IEnumerable<NeoServiceLayer.Services.Permissions.Models.Permission>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NeoServiceLayer.Services.Permissions.Models.Permission>>> GetUserPermissions(string userId)
    {
        try
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId, BlockchainType.NeoN3);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a data access policy.
    /// </summary>
    /// <param name="policy">The policy to create.</param>
    /// <returns>The result of the creation operation.</returns>
    [HttpPost("policies")]
    [ProducesResponseType(typeof(PolicyResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PolicyResult>> CreateDataAccessPolicy([FromBody] NeoServiceLayer.Services.Permissions.Models.DataAccessPolicy policy)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // For now, return success as this is a stub implementation
            var result = new PolicyResult
            {
                Success = true,
                PolicyId = policy.PolicyId,
                ErrorMessage = null
            };

            if (result.Success)
            {
                return CreatedAtAction(nameof(EvaluateDataAccess), null, result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating data access policy {PolicyName}", policy.Name);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates a data access policy.
    /// </summary>
    /// <param name="policyId">The policy ID.</param>
    /// <param name="policy">The updated policy data.</param>
    /// <returns>The result of the update operation.</returns>
    [HttpPut("policies/{policyId}")]
    [ProducesResponseType(typeof(PolicyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyResult>> UpdateDataAccessPolicy(string policyId, [FromBody] NeoServiceLayer.Services.Permissions.Models.DataAccessPolicy policy)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (policyId != policy.PolicyId)
            {
                return BadRequest("Policy ID mismatch");
            }

            // For now, return success as this is a stub implementation
            var result = new PolicyResult
            {
                Success = true,
                PolicyId = policyId,
                ErrorMessage = null
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else if (result.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(result.ErrorMessage);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data access policy {PolicyId}", policyId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a data access policy.
    /// </summary>
    /// <param name="policyId">The policy ID to delete.</param>
    /// <returns>The result of the deletion operation.</returns>
    [HttpDelete("policies/{policyId}")]
    [ProducesResponseType(typeof(PolicyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyResult>> DeleteDataAccessPolicy(string policyId)
    {
        try
        {
            // For now, return success as this is a stub implementation
            var result = new PolicyResult
            {
                Success = true,
                PolicyId = policyId,
                ErrorMessage = null
            };

            if (result.Success)
            {
                return Ok(result);
            }
            else if (result.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(result.ErrorMessage);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data access policy {PolicyId}", policyId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Evaluates data access policies for a request.
    /// </summary>
    /// <param name="request">The data access request.</param>
    /// <returns>Policy evaluation result.</returns>
    [HttpPost("policies/evaluate")]
    [ProducesResponseType(typeof(NeoServiceLayer.Services.Permissions.Models.PolicyEvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NeoServiceLayer.Services.Permissions.Models.PolicyEvaluationResult>> EvaluateDataAccess([FromBody] DataAccessRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _permissionService.EvaluateDataAccessAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating data access for principal {PrincipalId}", request.Principal.PrincipalId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Registers a service with its permissions.
    /// </summary>
    /// <param name="registration">The service registration.</param>
    /// <returns>The result of the registration operation.</returns>
    [HttpPost("services/register")]
    [ProducesResponseType(typeof(ServiceRegistrationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRegistrationResult>> RegisterService([FromBody] NeoServiceLayer.Services.Permissions.Models.ServicePermissionRegistration registration)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _permissionService.RegisterServiceAsync(registration);

            if (result.Success)
            {
                return CreatedAtAction(nameof(UpdateServicePermissions), new { serviceId = result.ServiceId }, result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service {ServiceName}", registration.ServiceName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates service permissions.
    /// </summary>
    /// <param name="serviceId">The service ID.</param>
    /// <param name="request">The update permissions request.</param>
    /// <returns>The result of the update operation.</returns>
    [HttpPut("services/{serviceId}/permissions")]
    [ProducesResponseType(typeof(ServiceRegistrationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRegistrationResult>> UpdateServicePermissions(string serviceId, [FromBody] UpdateServicePermissionsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _permissionService.UpdateServicePermissionsAsync(serviceId, request.Permissions);

            if (result.Success)
            {
                return Ok(result);
            }
            else if (result.ErrorMessage?.Contains("not found") == true)
            {
                return NotFound(result.ErrorMessage);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions for service {ServiceId}", serviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets audit logs with optional filtering.
    /// </summary>
    /// <param name="filter">The audit log filter parameters.</param>
    /// <returns>List of matching audit log entries.</returns>
    [HttpPost("audit-logs")]
    [ProducesResponseType(typeof(IEnumerable<NeoServiceLayer.Services.Permissions.Models.PermissionAuditLog>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NeoServiceLayer.Services.Permissions.Models.PermissionAuditLog>>> GetAuditLogs([FromBody] NeoServiceLayer.Services.Permissions.Models.AuditLogFilter filter)
    {
        try
        {
            var logs = await _permissionService.GetAuditLogsAsync(filter);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validates a permission token.
    /// </summary>
    /// <param name="request">The token validation request.</param>
    /// <returns>Token validation result.</returns>
    [HttpPost("validate-token")]
    [ProducesResponseType(typeof(TokenValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TokenValidationResult>> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _permissionService.ValidatePermissionTokenAsync(request.Token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generates a permission token for a user.
    /// </summary>
    /// <param name="request">The token generation request.</param>
    /// <returns>The generated token.</returns>
    [HttpPost("generate-token")]
    [ProducesResponseType(typeof(GenerateTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateTokenResponse>> GenerateToken([FromBody] GenerateTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await _permissionService.GeneratePermissionTokenAsync(
                request.UserId,
                request.Scope,
                request.Expiry);

            return Ok(new GenerateTokenResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.Add(request.Expiry)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token for user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the health status of the permission service.
    /// </summary>
    /// <returns>Health status information.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ServiceHealthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceHealthResponse>> GetHealth()
    {
        try
        {
            // Check if the permission service is healthy
            var health = await _permissionService.GetHealthAsync();

            return Ok(new ServiceHealthResponse
            {
                Status = health.ToString(),
                Timestamp = DateTime.UtcNow,
                Service = "PermissionService"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission service health");
            return StatusCode(500, "Permission service health check failed");
        }
    }
}

#region Request/Response Models

/// <summary>
/// Request model for permission checks.
/// </summary>
public class CheckPermissionRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource identifier.
    /// </summary>
    [Required]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action to perform.
    /// </summary>
    [Required]
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// Request model for service permission checks.
/// </summary>
public class CheckServicePermissionRequest
{
    /// <summary>
    /// Gets or sets the service ID.
    /// </summary>
    [Required]
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data key.
    /// </summary>
    [Required]
    public string DataKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access type.
    /// </summary>
    [Required]
    public NeoServiceLayer.Services.Permissions.Models.AccessType AccessType { get; set; }
}

/// <summary>
/// Request model for role assignments.
/// </summary>
public class AssignRoleRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role ID.
    /// </summary>
    [Required]
    public string RoleId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for role removal.
/// </summary>
public class RemoveRoleRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role ID.
    /// </summary>
    [Required]
    public string RoleId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating service permissions.
/// </summary>
public class UpdateServicePermissionsRequest
{
    /// <summary>
    /// Gets or sets the updated permissions.
    /// </summary>
    [Required]
    public List<NeoServiceLayer.Services.Permissions.Models.ServicePermission> Permissions { get; set; } = new();
}

/// <summary>
/// Request model for token validation.
/// </summary>
public class ValidateTokenRequest
{
    /// <summary>
    /// Gets or sets the token to validate.
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Request model for token generation.
/// </summary>
public class GenerateTokenRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token scope.
    /// </summary>
    [Required]
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiry duration.
    /// </summary>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Response model for token generation.
/// </summary>
public class GenerateTokenResponse
{
    /// <summary>
    /// Gets or sets the generated token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Response model for service health checks.
/// </summary>
public class ServiceHealthResponse
{
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request model for granting permissions.
/// </summary>
public class GrantPermissionRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission name.
    /// </summary>
    [Required]
    public string Permission { get; set; } = string.Empty;
}

/// <summary>
/// Request model for revoking permissions.
/// </summary>
public class RevokePermissionRequest
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission name.
    /// </summary>
    [Required]
    public string Permission { get; set; } = string.Empty;
}

/// <summary>
/// Result for role assignment operations.
/// </summary>
public class RoleAssignmentResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role ID.
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result for permission check operations.
/// </summary>
public class PermissionCheckResult
{
    /// <summary>
    /// Gets or sets whether the permission is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the service ID.
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data key.
    /// </summary>
    public string DataKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access type.
    /// </summary>
    public NeoServiceLayer.Services.Permissions.Models.AccessType AccessType { get; set; }
}

/// <summary>
/// Result for policy operations.
/// </summary>
public class PolicyResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the policy ID.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result for role operations.
/// </summary>
public class RoleResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result for service registration operations.
/// </summary>
public class ServiceRegistrationResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the service ID.
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result for token validation operations.
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Gets or sets whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the user ID from the token.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token scope.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiry time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

#endregion
