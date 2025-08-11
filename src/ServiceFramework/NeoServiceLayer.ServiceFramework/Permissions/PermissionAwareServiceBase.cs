using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Permissions;
using NeoServiceLayer.Services.Permissions.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.ServiceFramework.Permissions;

/// <summary>
/// Base class for services that require permission-aware operations.
/// Provides automatic permission checking and service registration capabilities.
/// </summary>
public abstract class PermissionAwareServiceBase : EnclaveBlockchainServiceBase
{
    private readonly IServiceProvider? _serviceProvider;
    private IPermissionService? _permissionService;
    private bool _permissionsRegistered;
    private readonly object _registrationLock = new();

    /// <summary>
    /// Gets the permission service instance.
    /// </summary>
    protected IPermissionService? PermissionService
    {
        get
        {
            if (_permissionService == null && _serviceProvider != null)
            {
                _permissionService = _serviceProvider.GetService<IPermissionService>();
            }
            return _permissionService;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionAwareServiceBase"/> class.
    /// </summary>
    protected PermissionAwareServiceBase(
        string serviceName,
        string description,
        string version,
        ILogger logger,
        BlockchainType[] supportedBlockchains,
        IEnclaveManager enclaveManager,
        IServiceProvider? serviceProvider = null)
        : base(serviceName, description, version, logger, supportedBlockchains, enclaveManager)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        var baseResult = await base.OnInitializeAsync();
        
        if (baseResult)
        {
            // Auto-register service permissions if configured
            await RegisterServicePermissionsAsync();
        }
        
        return baseResult;
    }

    /// <summary>
    /// Checks if the current context has permission to perform the specified operation.
    /// </summary>
    /// <param name="resource">The resource being accessed.</param>
    /// <param name="action">The action being performed.</param>
    /// <param name="userId">Optional user ID (if null, attempts to extract from current context).</param>
    /// <param name="serviceId">Optional service ID (if null, uses current service name).</param>
    /// <returns>True if permission is granted.</returns>
    protected async Task<bool> CheckPermissionAsync(
        string resource, 
        string action, 
        string? userId = null, 
        string? serviceId = null)
    {
        if (PermissionService == null)
        {
            Logger.LogDebug("Permission service not available, allowing operation");
            return true; // Allow if no permission service
        }

        try
        {
            // If service ID provided, check service permissions
            if (!string.IsNullOrEmpty(serviceId))
            {
                var accessType = MapActionToAccessType(action);
                var result = await PermissionService.CheckServicePermissionAsync(serviceId, resource, accessType);
                return result.IsAllowed;
            }

            // If user ID provided, check user permissions
            if (!string.IsNullOrEmpty(userId))
            {
                return await PermissionService.CheckPermissionAsync(userId, resource, action);
            }

            // Default to service-level check using current service name
            var serviceAccessType = MapActionToAccessType(action);
            var serviceResult = await PermissionService.CheckServicePermissionAsync(ServiceName, resource, serviceAccessType);
            return serviceResult.IsAllowed;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking permission for resource {Resource}, action {Action}", resource, action);
            return false; // Deny on error
        }
    }

    /// <summary>
    /// Ensures permission for the calling method based on RequirePermissionAttribute.
    /// </summary>
    /// <param name="callerName">The name of the calling method.</param>
    /// <param name="additionalContext">Additional context for permission checking.</param>
    /// <returns>True if permission is granted.</returns>
    protected async Task<bool> EnsurePermissionAsync(
        [CallerMemberName] string callerName = "",
        Dictionary<string, object>? additionalContext = null)
    {
        if (PermissionService == null)
        {
            Logger.LogDebug("Permission service not available, allowing operation");
            return true;
        }

        try
        {
            // Get method info for the caller
            var method = GetType().GetMethod(callerName);
            if (method == null)
            {
                Logger.LogWarning("Could not find method {MethodName} for permission check", callerName);
                return true; // Allow if we can't find the method
            }

            // Check for AllowAnonymousAccess attribute
            if (method.GetCustomAttribute<AllowAnonymousAccessAttribute>() != null ||
                GetType().GetCustomAttribute<AllowAnonymousAccessAttribute>() != null)
            {
                return true;
            }

            // Get RequirePermission attributes (method level first, then class level)
            var permissionAttrs = method.GetCustomAttributes<RequirePermissionAttribute>().ToList();
            if (!permissionAttrs.Any())
            {
                permissionAttrs = GetType().GetCustomAttributes<RequirePermissionAttribute>().ToList();
            }

            // If no permission requirements, allow by default
            if (!permissionAttrs.Any())
            {
                Logger.LogDebug("No permission requirements found for method {MethodName}, allowing", callerName);
                return true;
            }

            // Check each permission requirement
            foreach (var attr in permissionAttrs)
            {
                var resource = BuildResourcePath(attr.Resource, additionalContext);
                var hasPermission = await CheckPermissionAsync(resource, attr.Action);

                if (!hasPermission)
                {
                    var message = attr.DenialMessage ?? 
                        $"Access denied to {resource} for action {attr.Action}";
                    Logger.LogWarning("Permission denied for method {MethodName}: {Message}", callerName, message);
                    return false;
                }
            }

            Logger.LogDebug("Permission granted for method {MethodName}", callerName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking permissions for method {MethodName}", callerName);
            return false;
        }
    }

    /// <summary>
    /// Executes a permission-protected operation.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="resource">The resource being accessed.</param>
    /// <param name="action">The action being performed.</param>
    /// <param name="fallbackValue">The value to return if permission is denied.</param>
    /// <returns>The result of the operation or fallback value.</returns>
    protected async Task<T> ExecuteWithPermissionAsync<T>(
        Func<Task<T>> operation,
        string resource,
        string action,
        T fallbackValue = default(T)!)
    {
        if (await CheckPermissionAsync(resource, action))
        {
            return await operation();
        }

        Logger.LogWarning("Operation denied due to insufficient permissions: {Resource}:{Action}", resource, action);
        return fallbackValue;
    }

    /// <summary>
    /// Executes a permission-protected operation without return value.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="resource">The resource being accessed.</param>
    /// <param name="action">The action being performed.</param>
    /// <returns>True if the operation was executed.</returns>
    protected async Task<bool> ExecuteWithPermissionAsync(
        Func<Task> operation,
        string resource,
        string action)
    {
        if (await CheckPermissionAsync(resource, action))
        {
            await operation();
            return true;
        }

        Logger.LogWarning("Operation denied due to insufficient permissions: {Resource}:{Action}", resource, action);
        return false;
    }

    /// <summary>
    /// Registers service permissions automatically based on attributes and method signatures.
    /// </summary>
    private async Task RegisterServicePermissionsAsync()
    {
        if (_permissionsRegistered || PermissionService == null)
            return;

        lock (_registrationLock)
        {
            if (_permissionsRegistered)
                return;

            try
            {
                var serviceAttr = GetType().GetCustomAttribute<ServicePermissionsAttribute>();
                if (serviceAttr == null || !serviceAttr.AutoRegister)
                {
                    Logger.LogDebug("Service {ServiceName} not configured for auto permission registration", ServiceName);
                    return;
                }

                // Register service permissions in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RegisterServicePermissionsInternalAsync(serviceAttr);
                        Logger.LogInformation("Successfully registered permissions for service {ServiceName}", ServiceName);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to register permissions for service {ServiceName}", ServiceName);
                    }
                });

                _permissionsRegistered = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during permission registration for service {ServiceName}", ServiceName);
            }
        }
    }

    /// <summary>
    /// Internal method to register service permissions.
    /// </summary>
    private async Task RegisterServicePermissionsInternalAsync(ServicePermissionsAttribute serviceAttr)
    {
        var permissions = new List<ServicePermission>();

        // Analyze all public methods for permission requirements
        var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType != typeof(object) && !m.IsSpecialName);

        foreach (var method in methods)
        {
            var permissionAttrs = method.GetCustomAttributes<RequirePermissionAttribute>().ToList();
            
            // If method doesn't have explicit permissions, create default based on method name
            if (!permissionAttrs.Any())
            {
                var action = DetermineActionFromMethodName(method.Name);
                var resourcePattern = $"{serviceAttr.ResourcePrefix}:*";
                
                permissions.Add(new ServicePermission
                {
                    ResourcePattern = resourcePattern,
                    AllowedAccess = new List<AccessType> { MapActionToAccessType(action) },
                    CanDelegate = false
                });
            }
            else
            {
                // Add explicit permissions
                foreach (var attr in permissionAttrs)
                {
                    var resourcePattern = attr.Resource.Contains(':') 
                        ? attr.Resource 
                        : $"{serviceAttr.ResourcePrefix}:{attr.Resource}";
                        
                    permissions.Add(new ServicePermission
                    {
                        ResourcePattern = resourcePattern,
                        AllowedAccess = new List<AccessType> { MapActionToAccessType(attr.Action) },
                        CanDelegate = false
                    });
                }
            }
        }

        // Create service registration
        var registration = new ServicePermissionRegistration
        {
            ServiceName = ServiceName,
            Description = serviceAttr.Description ?? $"Auto-generated permissions for {ServiceName}",
            Permissions = permissions.DistinctBy(p => new { p.ResourcePattern, p.AllowedAccess }).ToList()
        };

        // Register with permission service
        var result = await PermissionService.RegisterServiceAsync(registration);
        if (!result.Success)
        {
            Logger.LogError("Failed to register service permissions: {ErrorMessage}", result.ErrorMessage);
        }
        else
        {
            Logger.LogInformation("Registered {PermissionCount} permissions for service {ServiceName}", 
                permissions.Count, ServiceName);
        }
    }

    /// <summary>
    /// Builds a resource path with context substitution.
    /// </summary>
    private string BuildResourcePath(string pattern, Dictionary<string, object>? context)
    {
        if (context == null)
            return pattern;

        var result = pattern;
        foreach (var kvp in context)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
        }
        return result;
    }

    /// <summary>
    /// Maps an action string to AccessType enum.
    /// </summary>
    private static AccessType MapActionToAccessType(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "read" or "get" or "list" or "query" => AccessType.Read,
            "write" or "create" or "update" or "set" => AccessType.Write,
            "delete" or "remove" => AccessType.Delete,
            "execute" or "run" or "invoke" => AccessType.Execute,
            "*" or "full" => AccessType.Full,
            _ => AccessType.Read // Default to read
        };
    }

    /// <summary>
    /// Determines the action type from a method name.
    /// </summary>
    private static string DetermineActionFromMethodName(string methodName)
    {
        var lower = methodName.ToLowerInvariant();
        
        if (lower.StartsWith("get") || lower.StartsWith("list") || lower.StartsWith("find") || 
            lower.StartsWith("query") || lower.StartsWith("check") || lower.Contains("exists"))
            return "read";
            
        if (lower.StartsWith("create") || lower.StartsWith("add") || lower.StartsWith("insert") ||
            lower.StartsWith("update") || lower.StartsWith("modify") || lower.StartsWith("set") ||
            lower.StartsWith("save") || lower.StartsWith("store"))
            return "write";
            
        if (lower.StartsWith("delete") || lower.StartsWith("remove") || lower.StartsWith("clear"))
            return "delete";
            
        if (lower.StartsWith("execute") || lower.StartsWith("run") || lower.StartsWith("invoke") ||
            lower.StartsWith("process") || lower.StartsWith("perform"))
            return "execute";
            
        return "read"; // Default fallback
    }
}