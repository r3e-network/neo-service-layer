using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Permissions.Models;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Permissions;

/// <summary>
/// Permissions service implementation providing role-based access control.
/// </summary>
public class PermissionsService : ServiceFramework.EnclaveBlockchainServiceBase, IPermissionsService
{
    private readonly IServiceConfiguration _configuration;
    private readonly IEnclaveManager _enclaveManager;
    private readonly IBlockchainClientFactory _blockchainClientFactory;
    private readonly ConcurrentDictionary<string, Role> _roles = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userRoles = new();
    private readonly ConcurrentDictionary<string, PermissionPolicy> _policies = new();
    private readonly ConcurrentList<PermissionAuditEntry> _auditLog = new();
    private readonly SemaphoreSlim _permissionSemaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionsService"/> class.
    /// </summary>
    public PermissionsService(
        IServiceConfiguration configuration,
        IEnclaveManager enclaveManager,
        IBlockchainClientFactory blockchainClientFactory,
        ILogger<PermissionsService> logger)
        : base("Permissions", "Role-Based Access Control Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(enclaveManager);
        ArgumentNullException.ThrowIfNull(blockchainClientFactory);

        _configuration = configuration;
        _enclaveManager = enclaveManager;
        _blockchainClientFactory = blockchainClientFactory;

        InitializeService();
    }

    /// <summary>
    /// Initializes the service with default roles and permissions.
    /// </summary>
    private void InitializeService()
    {
        // Add capabilities
        AddCapability<IPermissionsService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("SupportedBlockchains", "NeoN3,NeoX");
        SetMetadata("MaxRoles", "1000");
        SetMetadata("MaxPolicies", "500");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");

        // Initialize default system roles
        InitializeDefaultRoles();
    }

    /// <summary>
    /// Initializes default system roles.
    /// </summary>
    private void InitializeDefaultRoles()
    {
        // Admin role
        _roles["admin"] = new Role
        {
            Name = "admin",
            Description = "System administrator with full permissions",
            Permissions = new List<string> { "*" },
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsSystemRole = true
        };

        // User role
        _roles["user"] = new Role
        {
            Name = "user",
            Description = "Standard user with basic permissions",
            Permissions = new List<string>
            {
                "read:*",
                "create:own",
                "update:own",
                "delete:own"
            },
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsSystemRole = true
        };

        // Guest role
        _roles["guest"] = new Role
        {
            Name = "guest",
            Description = "Guest user with read-only permissions",
            Permissions = new List<string> { "read:public" },
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsSystemRole = true
        };

        // Service role
        _roles["service"] = new Role
        {
            Name = "service",
            Description = "Service account with API permissions",
            Permissions = new List<string>
            {
                "api:*",
                "read:*",
                "write:service"
            },
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsSystemRole = true
        };
    }

    /// <inheritdoc/>
    public async Task<bool> CreateRoleAsync(
        string roleName,
        IEnumerable<string> permissions,
        string description,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await _permissionSemaphore.WaitAsync();
        try
        {
            if (_roles.ContainsKey(roleName))
            {
                Logger.LogWarning("Role {RoleName} already exists", roleName);
                return false;
            }

            var role = new Role
            {
                Name = roleName,
                Description = description,
                Permissions = permissions.ToList(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                IsSystemRole = false
            };

            if (_roles.TryAdd(roleName, role))
            {
                await LogAuditEntryAsync("CreateRole", roleName, null, JsonSerializer.Serialize(role), true, blockchainType);
                Logger.LogInformation("Created role {RoleName} with {PermissionCount} permissions",
                    roleName, role.Permissions.Count);
                return true;
            }

            return false;
        }
        finally
        {
            _permissionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AssignRoleAsync(
        string userId,
        string roleName,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await _permissionSemaphore.WaitAsync();
        try
        {
            if (!_roles.ContainsKey(roleName))
            {
                Logger.LogWarning("Role {RoleName} does not exist", roleName);
                return false;
            }

            var userRoles = _userRoles.GetOrAdd(userId, _ => new HashSet<string>());

            if (userRoles.Add(roleName))
            {
                await LogAuditEntryAsync("AssignRole", $"{userId}:{roleName}", null, roleName, true, blockchainType);
                Logger.LogInformation("Assigned role {RoleName} to user {UserId}", roleName, userId);
                return true;
            }

            Logger.LogWarning("User {UserId} already has role {RoleName}", userId, roleName);
            return false;
        }
        finally
        {
            _permissionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeRoleAsync(
        string userId,
        string roleName,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await _permissionSemaphore.WaitAsync();
        try
        {
            if (_userRoles.TryGetValue(userId, out var userRoles))
            {
                if (userRoles.Remove(roleName))
                {
                    await LogAuditEntryAsync("RevokeRole", $"{userId}:{roleName}", roleName, null, true, blockchainType);
                    Logger.LogInformation("Revoked role {RoleName} from user {UserId}", roleName, userId);
                    return true;
                }
            }

            Logger.LogWarning("User {UserId} does not have role {RoleName}", userId, roleName);
            return false;
        }
        finally
        {
            _permissionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(
        string userId,
        string permission,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await Task.CompletedTask;

        if (_userRoles.TryGetValue(userId, out var userRoles))
        {
            foreach (var roleName in userRoles)
            {
                if (_roles.TryGetValue(roleName, out var role))
                {
                    // Check for wildcard permission
                    if (role.Permissions.Contains("*"))
                    {
                        return true;
                    }

                    // Check for exact permission
                    if (role.Permissions.Contains(permission))
                    {
                        return true;
                    }

                    // Check for pattern matching (e.g., "read:*" matches "read:document")
                    foreach (var rolePermission in role.Permissions)
                    {
                        if (MatchesPermissionPattern(rolePermission, permission))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Role>> GetUserRolesAsync(
        string userId,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        await Task.CompletedTask;

        var roles = new List<Role>();

        if (_userRoles.TryGetValue(userId, out var userRoleNames))
        {
            foreach (var roleName in userRoleNames)
            {
                if (_roles.TryGetValue(roleName, out var role))
                {
                    roles.Add(role);
                }
            }
        }

        return roles;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(
        string userId,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        await Task.CompletedTask;

        var permissions = new HashSet<string>();

        if (_userRoles.TryGetValue(userId, out var userRoleNames))
        {
            foreach (var roleName in userRoleNames)
            {
                if (_roles.TryGetValue(roleName, out var role))
                {
                    foreach (var permission in role.Permissions)
                    {
                        permissions.Add(permission);
                    }
                }
            }
        }

        return permissions;
    }

    /// <inheritdoc/>
    public async Task<bool> AddPermissionToRoleAsync(
        string roleName,
        string permission,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await _permissionSemaphore.WaitAsync();
        try
        {
            if (_roles.TryGetValue(roleName, out var role))
            {
                if (role.IsSystemRole)
                {
                    Logger.LogWarning("Cannot modify system role {RoleName}", roleName);
                    return false;
                }

                if (!role.Permissions.Contains(permission))
                {
                    var oldPermissions = JsonSerializer.Serialize(role.Permissions);
                    role.Permissions.Add(permission);
                    role.ModifiedAt = DateTime.UtcNow;

                    await LogAuditEntryAsync("AddPermissionToRole", $"{roleName}:{permission}",
                        oldPermissions, JsonSerializer.Serialize(role.Permissions), true, blockchainType);

                    Logger.LogInformation("Added permission {Permission} to role {RoleName}", permission, roleName);
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _permissionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemovePermissionFromRoleAsync(
        string roleName,
        string permission,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await _permissionSemaphore.WaitAsync();
        try
        {
            if (_roles.TryGetValue(roleName, out var role))
            {
                if (role.IsSystemRole)
                {
                    Logger.LogWarning("Cannot modify system role {RoleName}", roleName);
                    return false;
                }

                if (role.Permissions.Remove(permission))
                {
                    role.ModifiedAt = DateTime.UtcNow;

                    await LogAuditEntryAsync("RemovePermissionFromRole", $"{roleName}:{permission}",
                        permission, null, true, blockchainType);

                    Logger.LogInformation("Removed permission {Permission} from role {RoleName}", permission, roleName);
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _permissionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteRoleAsync(
        string roleName,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await _permissionSemaphore.WaitAsync();
        try
        {
            if (_roles.TryGetValue(roleName, out var role))
            {
                if (role.IsSystemRole)
                {
                    Logger.LogWarning("Cannot delete system role {RoleName}", roleName);
                    return false;
                }

                if (_roles.TryRemove(roleName, out _))
                {
                    // Remove role from all users
                    foreach (var userRoles in _userRoles.Values)
                    {
                        userRoles.Remove(roleName);
                    }

                    await LogAuditEntryAsync("DeleteRole", roleName, JsonSerializer.Serialize(role), null, true, blockchainType);
                    Logger.LogInformation("Deleted role {RoleName}", roleName);
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _permissionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Role>> GetAllRolesAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        await Task.CompletedTask;
        return _roles.Values.ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> CreatePolicyAsync(
        PermissionPolicy policy,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await _permissionSemaphore.WaitAsync();
        try
        {
            if (_policies.TryAdd(policy.Name, policy))
            {
                await LogAuditEntryAsync("CreatePolicy", policy.Name, null,
                    JsonSerializer.Serialize(policy), true, blockchainType);

                Logger.LogInformation("Created policy {PolicyName} with {RuleCount} rules",
                    policy.Name, policy.Rules.Count);
                return true;
            }

            return false;
        }
        finally
        {
            _permissionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PolicyEvaluationResult> EvaluatePolicyAsync(
        string userId,
        string policyName,
        Dictionary<string, object> context,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        await Task.CompletedTask;

        if (!_policies.TryGetValue(policyName, out var policy))
        {
            return new PolicyEvaluationResult
            {
                IsAllowed = false,
                Reason = $"Policy {policyName} not found"
            };
        }

        // Evaluate policy rules
        foreach (var rule in policy.Rules.OrderBy(r => policy.Priority))
        {
            if (EvaluateRule(rule, context))
            {
                var userPermissions = await GetUserPermissionsAsync(userId, blockchainType);
                var hasRequiredPermissions = rule.Actions.All(action =>
                    userPermissions.Any(p => MatchesPermissionPattern(p, action)));

                if (hasRequiredPermissions)
                {
                    return new PolicyEvaluationResult
                    {
                        IsAllowed = policy.Effect == PolicyEffect.Allow,
                        MatchedPolicy = policy.Name,
                        Reason = $"Matched rule in policy {policy.Name}",
                        Context = context
                    };
                }
            }
        }

        return new PolicyEvaluationResult
        {
            IsAllowed = false,
            Reason = "No matching policy rules",
            Context = context
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PermissionAuditEntry>> GetAuditLogAsync(
        DateTime startTime,
        DateTime endTime,
        BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        await Task.CompletedTask;

        return _auditLog
            .Where(entry => entry.Timestamp >= startTime &&
                           entry.Timestamp <= endTime &&
                           entry.BlockchainType == blockchainType)
            .OrderByDescending(entry => entry.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Evaluates a policy rule against a context.
    /// </summary>
    private bool EvaluateRule(PolicyRule rule, Dictionary<string, object> context)
    {
        // Check if resource pattern matches
        if (context.TryGetValue("resource", out var resource) && resource is string resourceStr)
        {
            if (!MatchesResourcePattern(rule.ResourcePattern, resourceStr))
            {
                return false;
            }
        }

        // Check conditions
        foreach (var condition in rule.Conditions)
        {
            if (!context.TryGetValue(condition.Key, out var contextValue) ||
                !Equals(contextValue, condition.Value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a permission matches a pattern.
    /// </summary>
    private bool MatchesPermissionPattern(string pattern, string permission)
    {
        if (pattern == "*" || pattern == permission)
        {
            return true;
        }

        // Convert pattern to regex (e.g., "read:*" -> "^read:.*$")
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(permission, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Checks if a resource matches a pattern.
    /// </summary>
    private bool MatchesResourcePattern(string pattern, string resource)
    {
        return MatchesPermissionPattern(pattern, resource);
    }

    /// <summary>
    /// Logs an audit entry.
    /// </summary>
    private async Task LogAuditEntryAsync(
        string action,
        string target,
        string? oldValue,
        string? newValue,
        bool success,
        BlockchainType blockchainType)
    {
        var entry = new PermissionAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            UserId = "system", // In real implementation, get from context
            Action = action,
            Target = target,
            OldValue = oldValue,
            NewValue = newValue,
            Success = success,
            BlockchainType = blockchainType
        };

        _auditLog.Add(entry);

        // Keep audit log size manageable
        while (_auditLog.Count > 10000)
        {
            _auditLog.TryTake(out _);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Permissions service");
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Permissions service enclave");
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        // Initialize enclave if needed
        if (!IsEnclaveInitialized)
        {
            await InitializeEnclaveAsync();
        }

        Logger.LogInformation("Permissions service started successfully");
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Permissions service stopped");
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check basic service health
            return await Task.FromResult(ServiceHealth.Healthy);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");
            return await Task.FromResult(ServiceHealth.Unhealthy);
        }
    }

    // New method implementations for IPermissionsService
    public async Task<Models.PolicyEvaluationResult> EvaluateDataAccessAsync(DataAccessRequest request)
    {
        return await Task.FromResult(new Models.PolicyEvaluationResult
        {
            IsAllowed = true,
            DecisionReason = "Default implementation",
            EvaluatedPolicies = new List<EvaluatedPolicy>(),
            Obligations = new List<PolicyObligation>()
        });
    }

    public async Task<ServiceRegistrationResult> RegisterServiceAsync(Models.ServicePermissionRegistration registration)
    {
        return await Task.FromResult(new ServiceRegistrationResult
        {
            Success = true,
            ServiceId = registration.ServiceId,
            ErrorMessage = string.Empty
        });
    }

    public async Task<ServiceRegistrationResult> UpdateServicePermissionsAsync(string serviceId, List<Models.ServicePermission> permissions)
    {
        return await Task.FromResult(new ServiceRegistrationResult
        {
            Success = true,
            ServiceId = serviceId,
            ErrorMessage = string.Empty
        });
    }

    public async Task<IEnumerable<Models.PermissionAuditLog>> GetAuditLogsAsync(AuditLogFilter filter)
    {
        return await Task.FromResult(new List<Models.PermissionAuditLog>());
    }

    public async Task<TokenValidationResult> ValidatePermissionTokenAsync(string token)
    {
        return await Task.FromResult(new TokenValidationResult
        {
            IsValid = true,
            PrincipalId = "default",
            Scope = "default",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
    }

    public async Task<string> GeneratePermissionTokenAsync(string userId, string scope, TimeSpan expiry)
    {
        return await Task.FromResult($"token_{userId}_{scope}_{Guid.NewGuid()}");
    }
}

/// <summary>
/// Thread-safe list implementation.
/// </summary>
internal class ConcurrentList<T> : IEnumerable<T>
{
    private readonly List<T> _list = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _list.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public void Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            _list.Add(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool TryTake(out T? item)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_list.Count > 0)
            {
                item = _list[0];
                _list.RemoveAt(0);
                return true;
            }
            item = default;
            return false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        _lock.EnterReadLock();
        try
        {
            return _list.ToList().GetEnumerator();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}