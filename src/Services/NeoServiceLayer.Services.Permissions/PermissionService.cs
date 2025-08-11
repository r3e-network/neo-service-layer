using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Services.Permissions.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Permissions;

/// <summary>
/// Implementation of the permission management service.
/// </summary>
public class PermissionService : EnclaveBlockchainServiceBase, IPermissionService
{
    private readonly SGXPersistence _sgxPersistence;
    private readonly ConcurrentDictionary<string, Role> _roles = new();
    private readonly ConcurrentDictionary<string, List<string>> _userRoles = new();
    private readonly ConcurrentDictionary<string, List<Permission>> _userPermissions = new();
    private readonly ConcurrentDictionary<string, DataAccessPolicy> _policies = new();
    private readonly ConcurrentDictionary<string, ServicePermissionRegistration> _serviceRegistrations = new();
    private readonly ConcurrentDictionary<string, List<PermissionAuditLog>> _auditLogs = new();
    private readonly string _jwtSecret;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionService"/> class.
    /// </summary>
    public PermissionService(
        ILogger<PermissionService> logger,
        IEnclaveManager enclaveManager,
        IEnclaveStorageService? enclaveStorage = null,
        string? jwtSecret = null)
        : base("PermissionService", "Comprehensive permission and access control service", "1.0.0", 
               logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _sgxPersistence = new SGXPersistence("PermissionService", enclaveStorage, logger);
        _jwtSecret = jwtSecret ?? GenerateDefaultSecret();
        
        AddCapability<IPermissionService>();
        AddDependency(new ServiceDependency("EnclaveStorageService", false, "1.0.0"));
        
        // Initialize default roles
        InitializeDefaultRoles();
    }

    /// <inheritdoc/>
    public async Task<bool> CheckPermissionAsync(string userId, string resource, string action)
    {
        try
        {
            // Log the permission check for auditing
            var auditLog = new PermissionAuditLog
            {
                LogId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                PrincipalId = userId,
                PrincipalType = PrincipalType.User,
                Resource = resource,
                Action = action,
                Context = new RequestContext { Timestamp = DateTime.UtcNow }
            };

            // Check direct user permissions
            if (_userPermissions.TryGetValue(userId, out var permissions))
            {
                var hasDirectPermission = permissions.Any(p => 
                    MatchesResource(p.Resource, resource) && 
                    MatchesAction(p.Action, action) &&
                    (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.UtcNow));

                if (hasDirectPermission)
                {
                    auditLog.AccessGranted = true;
                    await LogAuditAsync(auditLog);
                    return true;
                }
            }

            // Check role-based permissions
            if (_userRoles.TryGetValue(userId, out var userRoleIds))
            {
                foreach (var roleId in userRoleIds)
                {
                    if (_roles.TryGetValue(roleId, out var role))
                    {
                        var hasRolePermission = role.Permissions.Any(p =>
                            MatchesResource(p.Resource, resource) &&
                            MatchesAction(p.Action, action) &&
                            (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.UtcNow));

                        if (hasRolePermission)
                        {
                            auditLog.AccessGranted = true;
                            await LogAuditAsync(auditLog);
                            return true;
                        }
                    }
                }
            }

            // Evaluate data access policies
            var policyResult = await EvaluateDataAccessAsync(new DataAccessRequest
            {
                Principal = new Principal { PrincipalId = userId, Type = PrincipalType.User },
                Resource = resource,
                Action = action,
                Context = new RequestContext { Timestamp = DateTime.UtcNow }
            });

            auditLog.AccessGranted = policyResult.IsAllowed;
            auditLog.DenialReason = policyResult.IsAllowed ? null : policyResult.DecisionReason;
            await LogAuditAsync(auditLog);

            return policyResult.IsAllowed;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking permission for user {UserId}, resource {Resource}, action {Action}", 
                userId, resource, action);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PermissionCheckResult> CheckServicePermissionAsync(string serviceId, string dataKey, AccessType accessType)
    {
        try
        {
            var result = new PermissionCheckResult
            {
                Timestamp = DateTime.UtcNow
            };

            // Check if service is registered
            if (!_serviceRegistrations.TryGetValue(serviceId, out var registration))
            {
                result.IsAllowed = false;
                result.DenialReason = "Service not registered";
                return result;
            }

            // Check service permissions
            var hasPermission = registration.Permissions.Any(p =>
                MatchesResource(p.ResourcePattern, dataKey) &&
                p.AllowedAccess.Contains(accessType));

            if (hasPermission)
            {
                result.IsAllowed = true;
                result.MatchingPolicy = $"Service registration for {serviceId}";
            }
            else
            {
                // Check additional policies
                var policyResult = await EvaluateDataAccessAsync(new DataAccessRequest
                {
                    Principal = new Principal { PrincipalId = serviceId, Type = PrincipalType.Service, Name = registration.ServiceName },
                    Resource = dataKey,
                    Action = accessType.ToString(),
                    Context = new RequestContext { Timestamp = DateTime.UtcNow }
                });

                result.IsAllowed = policyResult.IsAllowed;
                result.DenialReason = policyResult.IsAllowed ? null : policyResult.DecisionReason;
                result.MatchingPolicy = policyResult.EvaluatedPolicies.FirstOrDefault(p => p.Matched)?.PolicyName;
            }

            // Log the check
            await LogAuditAsync(new PermissionAuditLog
            {
                LogId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                PrincipalId = serviceId,
                PrincipalType = PrincipalType.Service,
                Resource = dataKey,
                Action = accessType.ToString(),
                AccessGranted = result.IsAllowed,
                DenialReason = result.DenialReason,
                Context = new RequestContext { Timestamp = DateTime.UtcNow }
            });

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking service permission for service {ServiceId}, dataKey {DataKey}, accessType {AccessType}", 
                serviceId, dataKey, accessType);
            
            return new PermissionCheckResult
            {
                IsAllowed = false,
                DenialReason = "Permission check failed: " + ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<PermissionResult> GrantPermissionAsync(GrantPermissionRequest request)
    {
        try
        {
            var permission = request.Permission;
            permission.PermissionId = Guid.NewGuid().ToString();
            permission.CreatedAt = DateTime.UtcNow;

            // Add to user permissions
            _userPermissions.AddOrUpdate(request.UserId,
                new List<Permission> { permission },
                (key, existing) =>
                {
                    existing.Add(permission);
                    return existing;
                });

            // Persist to SGX storage
            await _sgxPersistence.StoreUserPermissionsAsync(request.UserId, 
                _userPermissions[request.UserId], BlockchainType.NeoN3);

            // Log the grant
            await LogAuditAsync(new PermissionAuditLog
            {
                LogId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                PrincipalId = request.GrantedBy,
                PrincipalType = PrincipalType.User,
                Resource = permission.Resource,
                Action = $"GRANT:{permission.Action}",
                AccessGranted = true,
                Context = new RequestContext 
                { 
                    Timestamp = DateTime.UtcNow,
                    Attributes = new Dictionary<string, object>
                    {
                        ["targetUser"] = request.UserId,
                        ["reason"] = request.Reason ?? "No reason provided"
                    }
                }
            });

            Logger.LogInformation("Granted permission {PermissionId} to user {UserId} by {GrantedBy}", 
                permission.PermissionId, request.UserId, request.GrantedBy);

            return new PermissionResult
            {
                Success = true,
                PermissionId = permission.PermissionId,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error granting permission to user {UserId}", request.UserId);
            return new PermissionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<PermissionResult> RevokePermissionAsync(RevokePermissionRequest request)
    {
        try
        {
            var revoked = false;
            
            if (_userPermissions.TryGetValue(request.UserId, out var permissions))
            {
                var permissionToRemove = permissions.FirstOrDefault(p => p.PermissionId == request.PermissionId);
                if (permissionToRemove != null)
                {
                    permissions.Remove(permissionToRemove);
                    revoked = true;

                    // Persist to SGX storage
                    await _sgxPersistence.StoreUserPermissionsAsync(request.UserId, permissions, BlockchainType.NeoN3);

                    // Log the revocation
                    await LogAuditAsync(new PermissionAuditLog
                    {
                        LogId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        PrincipalId = request.RevokedBy,
                        PrincipalType = PrincipalType.User,
                        Resource = permissionToRemove.Resource,
                        Action = $"REVOKE:{permissionToRemove.Action}",
                        AccessGranted = true,
                        Context = new RequestContext 
                        { 
                            Timestamp = DateTime.UtcNow,
                            Attributes = new Dictionary<string, object>
                            {
                                ["targetUser"] = request.UserId,
                                ["reason"] = request.Reason ?? "No reason provided"
                            }
                        }
                    });
                }
            }

            if (revoked)
            {
                Logger.LogInformation("Revoked permission {PermissionId} from user {UserId} by {RevokedBy}", 
                    request.PermissionId, request.UserId, request.RevokedBy);

                return new PermissionResult
                {
                    Success = true,
                    PermissionId = request.PermissionId,
                    Timestamp = DateTime.UtcNow
                };
            }
            else
            {
                return new PermissionResult
                {
                    Success = false,
                    ErrorMessage = "Permission not found",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error revoking permission {PermissionId} from user {UserId}", 
                request.PermissionId, request.UserId);
            return new PermissionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<RoleResult> CreateRoleAsync(Role role)
    {
        try
        {
            if (_roles.ContainsKey(role.RoleId))
            {
                return new RoleResult
                {
                    Success = false,
                    ErrorMessage = "Role already exists",
                    Timestamp = DateTime.UtcNow
                };
            }

            role.CreatedAt = DateTime.UtcNow;
            _roles[role.RoleId] = role;

            // Persist to SGX storage
            await _sgxPersistence.StoreRoleAsync(role, BlockchainType.NeoN3);

            Logger.LogInformation("Created role {RoleId} with {PermissionCount} permissions", 
                role.RoleId, role.Permissions.Count);

            return new RoleResult
            {
                Success = true,
                Role = role,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating role {RoleId}", role.RoleId);
            return new RoleResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<RoleResult> UpdateRoleAsync(Role role)
    {
        try
        {
            if (!_roles.ContainsKey(role.RoleId))
            {
                return new RoleResult
                {
                    Success = false,
                    ErrorMessage = "Role not found",
                    Timestamp = DateTime.UtcNow
                };
            }

            if (_roles[role.RoleId].IsSystem)
            {
                return new RoleResult
                {
                    Success = false,
                    ErrorMessage = "Cannot modify system role",
                    Timestamp = DateTime.UtcNow
                };
            }

            _roles[role.RoleId] = role;

            // Persist to SGX storage
            await _sgxPersistence.StoreRoleAsync(role, BlockchainType.NeoN3);

            Logger.LogInformation("Updated role {RoleId}", role.RoleId);

            return new RoleResult
            {
                Success = true,
                Role = role,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating role {RoleId}", role.RoleId);
            return new RoleResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<RoleResult> DeleteRoleAsync(string roleId)
    {
        try
        {
            if (_roles.TryGetValue(roleId, out var role))
            {
                if (role.IsSystem)
                {
                    return new RoleResult
                    {
                        Success = false,
                        ErrorMessage = "Cannot delete system role",
                        Timestamp = DateTime.UtcNow
                    };
                }

                _roles.TryRemove(roleId, out _);

                // Remove role from all users
                foreach (var userRoles in _userRoles.Values)
                {
                    userRoles.Remove(roleId);
                }

                // Delete from SGX storage
                await _sgxPersistence.DeleteRoleAsync(roleId, BlockchainType.NeoN3);

                Logger.LogInformation("Deleted role {RoleId}", roleId);

                return new RoleResult
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow
                };
            }

            return new RoleResult
            {
                Success = false,
                ErrorMessage = "Role not found",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return new RoleResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<RoleAssignmentResult> AssignRoleAsync(string userId, string roleId)
    {
        try
        {
            if (!_roles.ContainsKey(roleId))
            {
                return new RoleAssignmentResult
                {
                    Success = false,
                    ErrorMessage = "Role not found",
                    UserId = userId,
                    RoleId = roleId,
                    Timestamp = DateTime.UtcNow
                };
            }

            _userRoles.AddOrUpdate(userId,
                new List<string> { roleId },
                (key, existing) =>
                {
                    if (!existing.Contains(roleId))
                    {
                        existing.Add(roleId);
                    }
                    return existing;
                });

            // Persist to SGX storage
            await _sgxPersistence.StoreUserRolesAsync(userId, _userRoles[userId], BlockchainType.NeoN3);

            Logger.LogInformation("Assigned role {RoleId} to user {UserId}", roleId, userId);

            return new RoleAssignmentResult
            {
                Success = true,
                UserId = userId,
                RoleId = roleId,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
            return new RoleAssignmentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                UserId = userId,
                RoleId = roleId,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<RoleAssignmentResult> RemoveRoleAsync(string userId, string roleId)
    {
        try
        {
            if (_userRoles.TryGetValue(userId, out var roles))
            {
                roles.Remove(roleId);

                // Persist to SGX storage
                await _sgxPersistence.StoreUserRolesAsync(userId, roles, BlockchainType.NeoN3);

                Logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, userId);

                return new RoleAssignmentResult
                {
                    Success = true,
                    UserId = userId,
                    RoleId = roleId,
                    Timestamp = DateTime.UtcNow
                };
            }

            return new RoleAssignmentResult
            {
                Success = false,
                ErrorMessage = "User or role assignment not found",
                UserId = userId,
                RoleId = roleId,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
            return new RoleAssignmentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                UserId = userId,
                RoleId = roleId,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Role>> GetUserRolesAsync(string userId)
    {
        var roles = new List<Role>();
        
        if (_userRoles.TryGetValue(userId, out var roleIds))
        {
            foreach (var roleId in roleIds)
            {
                if (_roles.TryGetValue(roleId, out var role))
                {
                    roles.Add(role);
                }
            }
        }

        await Task.CompletedTask;
        return roles;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId)
    {
        var allPermissions = new List<Permission>();

        // Direct permissions
        if (_userPermissions.TryGetValue(userId, out var directPermissions))
        {
            allPermissions.AddRange(directPermissions);
        }

        // Role-based permissions
        var userRoles = await GetUserRolesAsync(userId);
        foreach (var role in userRoles)
        {
            allPermissions.AddRange(role.Permissions);
        }

        // Remove duplicates and expired permissions
        return allPermissions
            .Where(p => !p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.UtcNow)
            .GroupBy(p => new { p.Resource, p.Action })
            .Select(g => g.First())
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<PolicyResult> CreateDataAccessPolicyAsync(DataAccessPolicy policy)
    {
        try
        {
            policy.PolicyId = Guid.NewGuid().ToString();
            policy.CreatedAt = DateTime.UtcNow;
            policy.ModifiedAt = DateTime.UtcNow;

            _policies[policy.PolicyId] = policy;

            // Persist to SGX storage
            await _sgxPersistence.StorePolicyAsync(policy, BlockchainType.NeoN3);

            Logger.LogInformation("Created data access policy {PolicyId}: {PolicyName}", 
                policy.PolicyId, policy.Name);

            return new PolicyResult
            {
                Success = true,
                Policy = policy,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating data access policy {PolicyName}", policy.Name);
            return new PolicyResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<PolicyResult> UpdateDataAccessPolicyAsync(DataAccessPolicy policy)
    {
        try
        {
            if (!_policies.ContainsKey(policy.PolicyId))
            {
                return new PolicyResult
                {
                    Success = false,
                    ErrorMessage = "Policy not found",
                    Timestamp = DateTime.UtcNow
                };
            }

            policy.ModifiedAt = DateTime.UtcNow;
            _policies[policy.PolicyId] = policy;

            // Persist to SGX storage
            await _sgxPersistence.StorePolicyAsync(policy, BlockchainType.NeoN3);

            Logger.LogInformation("Updated data access policy {PolicyId}: {PolicyName}", 
                policy.PolicyId, policy.Name);

            return new PolicyResult
            {
                Success = true,
                Policy = policy,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating data access policy {PolicyId}", policy.PolicyId);
            return new PolicyResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<PolicyResult> DeleteDataAccessPolicyAsync(string policyId)
    {
        try
        {
            if (_policies.TryRemove(policyId, out var policy))
            {
                // Delete from SGX storage
                await _sgxPersistence.DeletePolicyAsync(policyId, BlockchainType.NeoN3);

                Logger.LogInformation("Deleted data access policy {PolicyId}: {PolicyName}", 
                    policyId, policy.Name);

                return new PolicyResult
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow
                };
            }

            return new PolicyResult
            {
                Success = false,
                ErrorMessage = "Policy not found",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting data access policy {PolicyId}", policyId);
            return new PolicyResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<PolicyEvaluationResult> EvaluateDataAccessAsync(DataAccessRequest request)
    {
        var result = new PolicyEvaluationResult
        {
            EvaluatedPolicies = new List<EvaluatedPolicy>(),
            Obligations = new List<PolicyObligation>()
        };

        var matchingPolicies = _policies.Values
            .Where(p => p.IsEnabled && PolicyMatches(p, request))
            .OrderByDescending(p => p.Priority)
            .ToList();

        foreach (var policy in matchingPolicies)
        {
            var evaluatedPolicy = new EvaluatedPolicy
            {
                PolicyId = policy.PolicyId,
                PolicyName = policy.Name,
                Effect = policy.Effect
            };

            if (await EvaluatePolicyConditionsAsync(policy, request))
            {
                evaluatedPolicy.Matched = true;
                result.EvaluatedPolicies.Add(evaluatedPolicy);

                if (policy.Effect == PolicyEffect.Deny)
                {
                    result.IsAllowed = false;
                    result.DecisionReason = $"Denied by policy: {policy.Name}";
                    return result;
                }
                else if (policy.Effect == PolicyEffect.Allow)
                {
                    result.IsAllowed = true;
                    result.DecisionReason = $"Allowed by policy: {policy.Name}";
                    // Continue evaluating for deny policies
                }
            }
        }

        if (!result.IsAllowed && string.IsNullOrEmpty(result.DecisionReason))
        {
            result.DecisionReason = "No matching allow policy found";
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<ServiceRegistrationResult> RegisterServiceAsync(ServicePermissionRegistration registration)
    {
        try
        {
            registration.ServiceId = registration.ServiceId ?? Guid.NewGuid().ToString();
            registration.ApiKey = GenerateApiKey();

            _serviceRegistrations[registration.ServiceId] = registration;

            // Persist to SGX storage
            await _sgxPersistence.StoreServiceRegistrationAsync(registration, BlockchainType.NeoN3);

            Logger.LogInformation("Registered service {ServiceId}: {ServiceName}", 
                registration.ServiceId, registration.ServiceName);

            return new ServiceRegistrationResult
            {
                Success = true,
                ServiceId = registration.ServiceId,
                ApiKey = registration.ApiKey,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error registering service {ServiceName}", registration.ServiceName);
            return new ServiceRegistrationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceRegistrationResult> UpdateServicePermissionsAsync(string serviceId, IEnumerable<ServicePermission> permissions)
    {
        try
        {
            if (_serviceRegistrations.TryGetValue(serviceId, out var registration))
            {
                registration.Permissions = permissions.ToList();

                // Persist to SGX storage
                await _sgxPersistence.StoreServiceRegistrationAsync(registration, BlockchainType.NeoN3);

                Logger.LogInformation("Updated permissions for service {ServiceId}", serviceId);

                return new ServiceRegistrationResult
                {
                    Success = true,
                    ServiceId = serviceId,
                    Timestamp = DateTime.UtcNow
                };
            }

            return new ServiceRegistrationResult
            {
                Success = false,
                ErrorMessage = "Service not found",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating service permissions for {ServiceId}", serviceId);
            return new ServiceRegistrationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ServiceId = serviceId,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PermissionAuditLog>> GetAuditLogsAsync(AuditLogFilter filter)
    {
        var allLogs = _auditLogs.Values.SelectMany(logs => logs);

        if (filter.StartDate.HasValue)
        {
            allLogs = allLogs.Where(log => log.Timestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            allLogs = allLogs.Where(log => log.Timestamp <= filter.EndDate.Value);
        }

        if (!string.IsNullOrEmpty(filter.PrincipalId))
        {
            allLogs = allLogs.Where(log => log.PrincipalId == filter.PrincipalId);
        }

        if (!string.IsNullOrEmpty(filter.Resource))
        {
            allLogs = allLogs.Where(log => log.Resource.Contains(filter.Resource));
        }

        if (filter.OnlyDenied.HasValue && filter.OnlyDenied.Value)
        {
            allLogs = allLogs.Where(log => !log.AccessGranted);
        }

        await Task.CompletedTask;
        return allLogs.OrderByDescending(log => log.Timestamp).Take(filter.MaxRecords).ToList();
    }

    /// <inheritdoc/>
    public async Task<TokenValidationResult> ValidatePermissionTokenAsync(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var principalId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var scope = principal.FindFirst("scope")?.Value;

            await Task.CompletedTask;

            return new TokenValidationResult
            {
                IsValid = true,
                PrincipalId = principalId,
                Scope = scope,
                ExpiresAt = jwtToken.ValidTo
            };
        }
        catch (SecurityTokenValidationException ex)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                Error = ex.Message
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating permission token");
            return new TokenValidationResult
            {
                IsValid = false,
                Error = "Token validation failed"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> GeneratePermissionTokenAsync(string userId, string scope, TimeSpan expiry)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("scope", scope),
            new Claim("jti", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "NeoServiceLayer.PermissionService",
            audience: "NeoServiceLayer",
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: credentials
        );

        await Task.CompletedTask;
        return _tokenHandler.WriteToken(token);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Permission Service");

        try
        {
            // Load persisted data
            await LoadPersistedDataAsync();
            
            Logger.LogInformation("Permission Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Permission Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Permission Service");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Permission Service");
        
        // Persist current state
        await PersistAllDataAsync();
        
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Permission Service enclave operations");
        
        try
        {
            // Initialize permission evaluation in enclave
            if (_enclaveManager != null)
            {
                await _enclaveManager.ExecuteJavaScriptAsync(@"
                    function evaluatePermissions(request) {
                        // Privacy-preserving permission evaluation
                        return JSON.stringify({ allowed: true });
                    }
                ");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Permission Service enclave operations");
            return false;
        }
    }

    // Private helper methods
    private void InitializeDefaultRoles()
    {
        // Admin role
        var adminRole = new Role
        {
            RoleId = "admin",
            Name = "Administrator",
            Description = "Full system access",
            IsSystem = true,
            Priority = 1000,
            Permissions = new List<Permission>
            {
                new Permission
                {
                    PermissionId = Guid.NewGuid().ToString(),
                    Resource = "*",
                    Action = "*",
                    Scope = PermissionScope.Global,
                    CreatedAt = DateTime.UtcNow
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        _roles[adminRole.RoleId] = adminRole;

        // Service role
        var serviceRole = new Role
        {
            RoleId = "service",
            Name = "Service Account",
            Description = "Default service permissions",
            IsSystem = true,
            Priority = 500,
            Permissions = new List<Permission>
            {
                new Permission
                {
                    PermissionId = Guid.NewGuid().ToString(),
                    Resource = "storage:*",
                    Action = "read,write",
                    Scope = PermissionScope.Service,
                    CreatedAt = DateTime.UtcNow
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        _roles[serviceRole.RoleId] = serviceRole;

        // User role
        var userRole = new Role
        {
            RoleId = "user",
            Name = "User",
            Description = "Default user permissions",
            IsSystem = true,
            Priority = 100,
            Permissions = new List<Permission>
            {
                new Permission
                {
                    PermissionId = Guid.NewGuid().ToString(),
                    Resource = "user:self:*",
                    Action = "read,write",
                    Scope = PermissionScope.User,
                    CreatedAt = DateTime.UtcNow
                }
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        _roles[userRole.RoleId] = userRole;
    }

    private bool MatchesResource(string pattern, string resource)
    {
        if (pattern == "*") return true;
        if (pattern == resource) return true;

        // Simple wildcard matching
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return resource.StartsWith(prefix);
        }

        return false;
    }

    private bool MatchesAction(string allowedActions, string requestedAction)
    {
        if (allowedActions == "*") return true;
        
        var actions = allowedActions.Split(',').Select(a => a.Trim());
        return actions.Contains(requestedAction) || actions.Contains("*");
    }

    private bool PolicyMatches(DataAccessPolicy policy, DataAccessRequest request)
    {
        // Check resource pattern
        if (!MatchesResource(policy.ResourcePattern, request.Resource))
        {
            return false;
        }

        // Check principals
        if (policy.Principals.Any())
        {
            var principalMatches = policy.Principals.Any(p =>
                p.PrincipalId == request.Principal.PrincipalId ||
                (p.Type == PrincipalType.Role && _userRoles.GetValueOrDefault(request.Principal.PrincipalId)?.Contains(p.PrincipalId) == true));

            if (!principalMatches)
            {
                return false;
            }
        }

        // Check actions
        if (policy.AllowedActions.Any() && !policy.AllowedActions.Contains(request.Action) && !policy.AllowedActions.Contains("*"))
        {
            return false;
        }

        if (policy.DeniedActions.Contains(request.Action))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> EvaluatePolicyConditionsAsync(DataAccessPolicy policy, DataAccessRequest request)
    {
        if (policy.Conditions == null)
        {
            return true;
        }

        // Check IP restrictions
        if (policy.Conditions.IpAddresses.Any() && !string.IsNullOrEmpty(request.Context.IpAddress))
        {
            if (!policy.Conditions.IpAddresses.Contains(request.Context.IpAddress))
            {
                return false;
            }
        }

        // Check time restrictions
        if (policy.Conditions.TimeRestriction != null)
        {
            var now = DateTime.UtcNow;
            var timeRestriction = policy.Conditions.TimeRestriction;

            if (timeRestriction.StartTime.HasValue && now < timeRestriction.StartTime.Value)
            {
                return false;
            }

            if (timeRestriction.EndTime.HasValue && now > timeRestriction.EndTime.Value)
            {
                return false;
            }

            if (timeRestriction.AllowedDays.Any() && !timeRestriction.AllowedDays.Contains(now.DayOfWeek))
            {
                return false;
            }

            if (timeRestriction.AllowedHours.Any() && !timeRestriction.AllowedHours.Contains(now.Hour))
            {
                return false;
            }
        }

        // Check blockchain conditions (would require blockchain integration)
        if (policy.Conditions.BlockchainConditions != null)
        {
            // This would be implemented with actual blockchain queries
            await Task.CompletedTask;
        }

        return true;
    }

    private async Task LogAuditAsync(PermissionAuditLog log)
    {
        var dateKey = log.Timestamp.ToString("yyyy-MM-dd");
        _auditLogs.AddOrUpdate(dateKey,
            new List<PermissionAuditLog> { log },
            (key, existing) =>
            {
                existing.Add(log);
                // Keep only last 1000 logs per day
                if (existing.Count > 1000)
                {
                    existing.RemoveRange(0, existing.Count - 1000);
                }
                return existing;
            });

        // Persist audit logs periodically
        if (_auditLogs[dateKey].Count % 100 == 0)
        {
            await _sgxPersistence.StoreAuditLogsAsync(dateKey, _auditLogs[dateKey], BlockchainType.NeoN3);
        }
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    private string GenerateDefaultSecret()
    {
        var bytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    private async Task LoadPersistedDataAsync()
    {
        try
        {
            // Load roles
            var roles = await _sgxPersistence.GetAllRolesAsync(BlockchainType.NeoN3);
            foreach (var role in roles)
            {
                _roles[role.RoleId] = role;
            }

            // Load user roles
            var userRoles = await _sgxPersistence.GetAllUserRolesAsync(BlockchainType.NeoN3);
            foreach (var kvp in userRoles)
            {
                _userRoles[kvp.Key] = kvp.Value;
            }

            // Load user permissions
            var userPermissions = await _sgxPersistence.GetAllUserPermissionsAsync(BlockchainType.NeoN3);
            foreach (var kvp in userPermissions)
            {
                _userPermissions[kvp.Key] = kvp.Value;
            }

            // Load policies
            var policies = await _sgxPersistence.GetAllPoliciesAsync(BlockchainType.NeoN3);
            foreach (var policy in policies)
            {
                _policies[policy.PolicyId] = policy;
            }

            // Load service registrations
            var registrations = await _sgxPersistence.GetAllServiceRegistrationsAsync(BlockchainType.NeoN3);
            foreach (var registration in registrations)
            {
                _serviceRegistrations[registration.ServiceId] = registration;
            }

            Logger.LogInformation("Loaded persisted permission data: {Roles} roles, {Policies} policies, {Services} services",
                _roles.Count, _policies.Count, _serviceRegistrations.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persisted permission data");
        }
    }

    private async Task PersistAllDataAsync()
    {
        try
        {
            // Persist all data to SGX storage
            foreach (var role in _roles.Values.Where(r => !r.IsSystem))
            {
                await _sgxPersistence.StoreRoleAsync(role, BlockchainType.NeoN3);
            }

            foreach (var kvp in _userRoles)
            {
                await _sgxPersistence.StoreUserRolesAsync(kvp.Key, kvp.Value, BlockchainType.NeoN3);
            }

            foreach (var kvp in _userPermissions)
            {
                await _sgxPersistence.StoreUserPermissionsAsync(kvp.Key, kvp.Value, BlockchainType.NeoN3);
            }

            foreach (var policy in _policies.Values)
            {
                await _sgxPersistence.StorePolicyAsync(policy, BlockchainType.NeoN3);
            }

            foreach (var registration in _serviceRegistrations.Values)
            {
                await _sgxPersistence.StoreServiceRegistrationAsync(registration, BlockchainType.NeoN3);
            }

            Logger.LogInformation("Persisted all permission data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting permission data");
        }
    }

    /// <summary>
    /// Inner class for SGX persistence operations.
    /// </summary>
    private class SGXPersistence : NeoServiceLayer.ServiceFramework.SGXPersistenceBase
    {
        public SGXPersistence(string serviceName, IEnclaveStorageService? enclaveStorage, ILogger logger) 
            : base(serviceName, enclaveStorage, logger)
        {
        }

        public async Task<bool> StoreRoleAsync(Role role, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync($"role:{role.RoleId}", role, 
                new Dictionary<string, object> { ["type"] = "role" }, blockchainType);
        }

        public async Task<Role?> GetRoleAsync(string roleId, BlockchainType blockchainType)
        {
            return await RetrieveSecurelyAsync<Role>($"role:{roleId}", blockchainType);
        }

        public async Task<bool> DeleteRoleAsync(string roleId, BlockchainType blockchainType)
        {
            return await DeleteSecurelyAsync($"role:{roleId}", blockchainType);
        }

        public async Task<List<Role>> GetAllRolesAsync(BlockchainType blockchainType)
        {
            var roles = new List<Role>();
            var items = await ListStoredItemsAsync("role:", blockchainType);
            
            if (items != null)
            {
                foreach (var item in items.Items)
                {
                    var role = await RetrieveSecurelyAsync<Role>(item.Key.Replace($"{_serviceName}:", ""), blockchainType);
                    if (role != null)
                    {
                        roles.Add(role);
                    }
                }
            }
            
            return roles;
        }

        public async Task<bool> StoreUserRolesAsync(string userId, List<string> roleIds, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync($"userroles:{userId}", roleIds, 
                new Dictionary<string, object> { ["type"] = "user_roles" }, blockchainType);
        }

        public async Task<Dictionary<string, List<string>>> GetAllUserRolesAsync(BlockchainType blockchainType)
        {
            var userRoles = new Dictionary<string, List<string>>();
            var items = await ListStoredItemsAsync("userroles:", blockchainType);
            
            if (items != null)
            {
                foreach (var item in items.Items)
                {
                    var key = item.Key.Replace($"{_serviceName}:userroles:", "");
                    var roles = await RetrieveSecurelyAsync<List<string>>(item.Key.Replace($"{_serviceName}:", ""), blockchainType);
                    if (roles != null)
                    {
                        userRoles[key] = roles;
                    }
                }
            }
            
            return userRoles;
        }

        public async Task<bool> StoreUserPermissionsAsync(string userId, List<Permission> permissions, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync($"userpermissions:{userId}", permissions, 
                new Dictionary<string, object> { ["type"] = "user_permissions" }, blockchainType);
        }

        public async Task<Dictionary<string, List<Permission>>> GetAllUserPermissionsAsync(BlockchainType blockchainType)
        {
            var userPermissions = new Dictionary<string, List<Permission>>();
            var items = await ListStoredItemsAsync("userpermissions:", blockchainType);
            
            if (items != null)
            {
                foreach (var item in items.Items)
                {
                    var key = item.Key.Replace($"{_serviceName}:userpermissions:", "");
                    var permissions = await RetrieveSecurelyAsync<List<Permission>>(item.Key.Replace($"{_serviceName}:", ""), blockchainType);
                    if (permissions != null)
                    {
                        userPermissions[key] = permissions;
                    }
                }
            }
            
            return userPermissions;
        }

        public async Task<bool> StorePolicyAsync(DataAccessPolicy policy, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync($"policy:{policy.PolicyId}", policy, 
                new Dictionary<string, object> { ["type"] = "data_access_policy" }, blockchainType);
        }

        public async Task<bool> DeletePolicyAsync(string policyId, BlockchainType blockchainType)
        {
            return await DeleteSecurelyAsync($"policy:{policyId}", blockchainType);
        }

        public async Task<List<DataAccessPolicy>> GetAllPoliciesAsync(BlockchainType blockchainType)
        {
            var policies = new List<DataAccessPolicy>();
            var items = await ListStoredItemsAsync("policy:", blockchainType);
            
            if (items != null)
            {
                foreach (var item in items.Items)
                {
                    var policy = await RetrieveSecurelyAsync<DataAccessPolicy>(item.Key.Replace($"{_serviceName}:", ""), blockchainType);
                    if (policy != null)
                    {
                        policies.Add(policy);
                    }
                }
            }
            
            return policies;
        }

        public async Task<bool> StoreServiceRegistrationAsync(ServicePermissionRegistration registration, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync($"service:{registration.ServiceId}", registration, 
                new Dictionary<string, object> { ["type"] = "service_registration" }, blockchainType);
        }

        public async Task<List<ServicePermissionRegistration>> GetAllServiceRegistrationsAsync(BlockchainType blockchainType)
        {
            var registrations = new List<ServicePermissionRegistration>();
            var items = await ListStoredItemsAsync("service:", blockchainType);
            
            if (items != null)
            {
                foreach (var item in items.Items)
                {
                    var registration = await RetrieveSecurelyAsync<ServicePermissionRegistration>(item.Key.Replace($"{_serviceName}:", ""), blockchainType);
                    if (registration != null)
                    {
                        registrations.Add(registration);
                    }
                }
            }
            
            return registrations;
        }

        public async Task<bool> StoreAuditLogsAsync(string dateKey, List<PermissionAuditLog> logs, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync($"audit:{dateKey}", logs, 
                new Dictionary<string, object> { ["type"] = "audit_logs", ["date"] = dateKey }, blockchainType);
        }
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
        /// Gets or sets the error message (if any).
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the policy.
        /// </summary>
        public DataAccessPolicy? Policy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}