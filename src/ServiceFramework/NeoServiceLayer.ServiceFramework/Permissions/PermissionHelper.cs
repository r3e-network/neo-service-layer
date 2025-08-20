using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Permissions;
using NeoServiceLayer.Services.Permissions.Models;
using System.Threading;


namespace NeoServiceLayer.ServiceFramework.Permissions;

/// <summary>
/// Helper class for common permission operations and service setup.
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// Creates default permissions for common service patterns.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="resourcePrefix">The resource prefix (e.g., "voting", "storage").</param>
    /// <param name="includeAdminAccess">Whether to include admin access permissions.</param>
    /// <returns>List of default service permissions.</returns>
    public static List<ServicePermission> CreateDefaultPermissions(
        string serviceName,
        string resourcePrefix,
        bool includeAdminAccess = true)
    {
        var permissions = new List<ServicePermission>();

        // Read access for own resources
        permissions.Add(new ServicePermission
        {
            ResourcePattern = $"{resourcePrefix}:{serviceName}:*",
            AllowedAccess = new List<AccessType> { AccessType.Read },
            CanDelegate = false
        });

        // Write access for own resources
        permissions.Add(new ServicePermission
        {
            ResourcePattern = $"{resourcePrefix}:{serviceName}:*",
            AllowedAccess = new List<AccessType> { AccessType.Write },
            CanDelegate = false
        });

        // Delete access for own resources
        permissions.Add(new ServicePermission
        {
            ResourcePattern = $"{resourcePrefix}:{serviceName}:*",
            AllowedAccess = new List<AccessType> { AccessType.Delete },
            CanDelegate = false
        });

        if (includeAdminAccess)
        {
            // Full access for admin operations
            permissions.Add(new ServicePermission
            {
                ResourcePattern = $"{resourcePrefix}:admin:*",
                AllowedAccess = new List<AccessType> { AccessType.Full },
                CanDelegate = true
            });
        }

        return permissions;
    }

    /// <summary>
    /// Creates a basic data access policy for a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="resourcePattern">The resource pattern.</param>
    /// <param name="allowedActions">The allowed actions.</param>
    /// <param name="principals">The principals that can access.</param>
    /// <returns>A configured data access policy.</returns>
    public static DataAccessPolicy CreateServicePolicy(
        string serviceName,
        string resourcePattern,
        List<string> allowedActions,
        List<Principal>? principals = null)
    {
        return new DataAccessPolicy
        {
            Name = $"{serviceName} Access Policy",
            Description = $"Default access policy for {serviceName} service",
            ResourcePattern = resourcePattern,
            AllowedActions = allowedActions,
            DeniedActions = new List<string>(),
            Principals = principals ?? new List<Principal>(),
            Effect = PolicyEffect.Allow,
            Priority = 100,
            IsEnabled = true,
            Conditions = new PolicyConditions()
        };
    }

    /// <summary>
    /// Creates common roles for a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="resourcePrefix">The resource prefix.</param>
    /// <returns>List of default roles.</returns>
    public static List<Role> CreateServiceRoles(string serviceName, string resourcePrefix)
    {
        var roles = new List<Role>();

        // Service Admin Role
        var adminRole = new Role
        {
            RoleId = $"{serviceName.ToLower()}-admin",
            Name = $"{serviceName} Administrator",
            Description = $"Full administrative access to {serviceName} service",
            IsSystem = false,
            Priority = 900,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            Permissions = new List<Permission>
            {
                new Permission
                {
                    PermissionId = Guid.NewGuid().ToString(),
                    Resource = $"{resourcePrefix}:*",
                    Action = "*",
                    Scope = PermissionScope.Service,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        roles.Add(adminRole);

        // Service User Role
        var userRole = new Role
        {
            RoleId = $"{serviceName.ToLower()}-user",
            Name = $"{serviceName} User",
            Description = $"Standard user access to {serviceName} service",
            IsSystem = false,
            Priority = 100,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            Permissions = new List<Permission>
            {
                new Permission
                {
                    PermissionId = Guid.NewGuid().ToString(),
                    Resource = $"{resourcePrefix}:user:*",
                    Action = "read,write",
                    Scope = PermissionScope.User,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        roles.Add(userRole);

        // Service ReadOnly Role
        var readOnlyRole = new Role
        {
            RoleId = $"{serviceName.ToLower()}-readonly",
            Name = $"{serviceName} Read-Only",
            Description = $"Read-only access to {serviceName} service",
            IsSystem = false,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            Permissions = new List<Permission>
            {
                new Permission
                {
                    PermissionId = Guid.NewGuid().ToString(),
                    Resource = $"{resourcePrefix}:*",
                    Action = "read",
                    Scope = PermissionScope.Service,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        roles.Add(readOnlyRole);

        return roles;
    }

    /// <summary>
    /// Sets up initial permissions for a service.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="resourcePrefix">The resource prefix.</param>
    /// <param name="customPermissions">Optional custom permissions.</param>
    /// <param name="customRoles">Optional custom roles.</param>
    /// <param name="customPolicies">Optional custom policies.</param>
    /// <returns>Task representing the setup operation.</returns>
    public static async Task SetupServicePermissionsAsync(
        IServiceProvider serviceProvider,
        string serviceName,
        string resourcePrefix,
        List<ServicePermission>? customPermissions = null,
        List<Role>? customRoles = null,
        List<DataAccessPolicy>? customPolicies = null)
    {
        var permissionService = serviceProvider.GetService<IPermissionService>();
        var logger = serviceProvider.GetService<ILogger<PermissionHelper>>();

        if (permissionService == null)
        {
            logger?.LogWarning("Permission service not available, skipping permission setup for {ServiceName}", serviceName);
            return;
        }

        try
        {
            // 1. Register service with permissions
            var permissions = customPermissions ?? CreateDefaultPermissions(serviceName, resourcePrefix);
            var registration = new ServicePermissionRegistration
            {
                ServiceName = serviceName,
                Description = $"Permissions for {serviceName} service",
                Permissions = permissions
            };

            var registrationResult = await permissionService.RegisterServiceAsync(registration);
            if (!registrationResult.Success)
            {
                logger?.LogError("Failed to register service permissions for {ServiceName}: {Error}",
                    serviceName, registrationResult.ErrorMessage);
                return;
            }

            logger?.LogInformation("Registered service permissions for {ServiceName}", serviceName);

            // 2. Create roles
            var roles = customRoles ?? CreateServiceRoles(serviceName, resourcePrefix);
            foreach (var role in roles)
            {
                var roleResult = await permissionService.CreateRoleAsync(role);
                if (roleResult.Success)
                {
                    logger?.LogDebug("Created role {RoleName} for service {ServiceName}", role.Name, serviceName);
                }
                else
                {
                    logger?.LogWarning("Failed to create role {RoleName} for service {ServiceName}: {Error}",
                        role.Name, serviceName, roleResult.ErrorMessage);
                }
            }

            // 3. Create policies
            var policies = customPolicies ?? new List<DataAccessPolicy>
            {
                CreateServicePolicy(serviceName, $"{resourcePrefix}:*", new List<string> { "read", "write" })
            };

            foreach (var policy in policies)
            {
                var policyResult = await permissionService.CreateDataAccessPolicyAsync(policy);
                if (policyResult.Success)
                {
                    logger?.LogDebug("Created policy {PolicyName} for service {ServiceName}", policy.Name, serviceName);
                }
                else
                {
                    logger?.LogWarning("Failed to create policy {PolicyName} for service {ServiceName}: {Error}",
                        policy.Name, serviceName, policyResult.ErrorMessage);
                }
            }

            logger?.LogInformation("Successfully set up permissions for service {ServiceName}", serviceName);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error setting up permissions for service {ServiceName}", serviceName);
        }
    }

    /// <summary>
    /// Creates time-based access restrictions.
    /// </summary>
    /// <param name="businessHoursOnly">Whether to restrict to business hours.</param>
    /// <param name="allowedDays">Specific days of the week to allow.</param>
    /// <param name="startTime">Start time for access.</param>
    /// <param name="endTime">End time for access.</param>
    /// <returns>Configured time restrictions.</returns>
    public static TimeRestriction CreateTimeRestriction(
        bool businessHoursOnly = false,
        List<DayOfWeek>? allowedDays = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var restriction = new TimeRestriction
        {
            StartTime = startTime,
            EndTime = endTime,
            AllowedDays = allowedDays ?? new List<DayOfWeek>(),
            AllowedHours = new List<int>()
        };

        if (businessHoursOnly)
        {
            // Business hours: 9 AM to 5 PM, Monday to Friday
            restriction.AllowedDays = new List<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            };
            restriction.AllowedHours = Enumerable.Range(9, 8).ToList(); // 9 AM to 4 PM (inclusive)
        }

        return restriction;
    }

    /// <summary>
    /// Creates IP-based access restrictions.
    /// </summary>
    /// <param name="allowedIps">List of allowed IP addresses or CIDR ranges.</param>
    /// <returns>Policy conditions with IP restrictions.</returns>
    public static PolicyConditions CreateIpRestrictions(List<string> allowedIps)
    {
        return new PolicyConditions
        {
            IpAddresses = allowedIps,
            Attributes = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates blockchain-based access conditions.
    /// </summary>
    /// <param name="minimumBalance">Minimum token balance required.</param>
    /// <param name="requiredNFTs">Required NFT contracts.</param>
    /// <param name="requiredContracts">Required smart contract interactions.</param>
    /// <returns>Blockchain conditions.</returns>
    public static BlockchainConditions CreateBlockchainConditions(
        decimal? minimumBalance = null,
        List<string>? requiredNFTs = null,
        List<string>? requiredContracts = null)
    {
        return new BlockchainConditions
        {
            MinimumTokenBalance = minimumBalance,
            RequiredNFTs = requiredNFTs ?? new List<string>(),
            RequiredContracts = requiredContracts ?? new List<string>()
        };
    }
}

/// <summary>
/// Configuration class for setting up service permissions.
/// </summary>
public class ServicePermissionConfiguration
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource prefix.
    /// </summary>
    public string ResourcePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to auto-register permissions.
    /// </summary>
    public bool AutoRegister { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to create default roles.
    /// </summary>
    public bool CreateDefaultRoles { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to create default policies.
    /// </summary>
    public bool CreateDefaultPolicies { get; set; } = true;

    /// <summary>
    /// Gets or sets custom permissions.
    /// </summary>
    public List<ServicePermission> CustomPermissions { get; set; } = new();

    /// <summary>
    /// Gets or sets custom roles.
    /// </summary>
    public List<Role> CustomRoles { get; set; } = new();

    /// <summary>
    /// Gets or sets custom policies.
    /// </summary>
    public List<DataAccessPolicy> CustomPolicies { get; set; } = new();

    /// <summary>
    /// Gets or sets time restrictions.
    /// </summary>
    public TimeRestriction? TimeRestrictions { get; set; }

    /// <summary>
    /// Gets or sets IP restrictions.
    /// </summary>
    public List<string> AllowedIpAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets blockchain conditions.
    /// </summary>
    public BlockchainConditions? BlockchainConditions { get; set; }
}