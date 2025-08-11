using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.ServiceFramework.Permissions;
using NeoServiceLayer.Services.Permissions;

namespace NeoServiceLayer.ServiceFramework.Extensions;

/// <summary>
/// Extension methods for setting up service permissions.
/// </summary>
public static class ServicePermissionExtensions
{
    /// <summary>
    /// Configures permission-aware services in the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPermissionAwareServices(this IServiceCollection services)
    {
        // Register the permission setup service
        services.AddSingleton<IHostedService, PermissionSetupService>();
        services.AddScoped<PermissionHelper>();

        return services;
    }

    /// <summary>
    /// Sets up permissions for a specific service.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="config">The permission configuration.</param>
    /// <returns>Task representing the setup operation.</returns>
    public static async Task SetupServicePermissionsAsync(
        this IServiceProvider serviceProvider,
        ServicePermissionConfiguration config)
    {
        await PermissionHelper.SetupServicePermissionsAsync(
            serviceProvider,
            config.ServiceName,
            config.ResourcePrefix,
            config.CreateDefaultRoles ? null : config.CustomPermissions,
            config.CreateDefaultRoles ? null : config.CustomRoles,
            config.CreateDefaultPolicies ? null : config.CustomPolicies);
    }

    /// <summary>
    /// Bulk setup permissions for multiple services.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="configurations">The service configurations.</param>
    /// <returns>Task representing the setup operation.</returns>
    public static async Task SetupBulkServicePermissionsAsync(
        this IServiceProvider serviceProvider,
        params ServicePermissionConfiguration[] configurations)
    {
        var tasks = configurations.Select(config => serviceProvider.SetupServicePermissionsAsync(config));
        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Background service that sets up permissions for all registered services.
/// </summary>
public class PermissionSetupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PermissionSetupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionSetupService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public PermissionSetupService(IServiceProvider serviceProvider, ILogger<PermissionSetupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the permission setup for all services.
    /// </summary>
    /// <param name="stoppingToken">The stopping token.</param>
    /// <returns>Task representing the operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for all services to be initialized
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        try
        {
            _logger.LogInformation("Starting automatic permission setup for all services");

            // Setup permissions for all known services
            var configurations = GetDefaultServiceConfigurations();
            
            using var scope = _serviceProvider.CreateScope();
            await scope.ServiceProvider.SetupBulkServicePermissionsAsync(configurations);
            
            _logger.LogInformation("Completed automatic permission setup for {Count} services", configurations.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic permission setup");
        }
    }

    /// <summary>
    /// Gets default configurations for all known services.
    /// </summary>
    /// <returns>Array of service permission configurations.</returns>
    private static ServicePermissionConfiguration[] GetDefaultServiceConfigurations()
    {
        return new[]
        {
            new ServicePermissionConfiguration
            {
                ServiceName = "VotingService",
                ResourcePrefix = "voting",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "StorageService",
                ResourcePrefix = "storage",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "KeyManagementService",
                ResourcePrefix = "keys",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "AbstractAccountService",
                ResourcePrefix = "accounts",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "SocialRecoveryService",
                ResourcePrefix = "recovery",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "SmartContractsService",
                ResourcePrefix = "contracts",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "OracleService",
                ResourcePrefix = "oracle",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "NotificationService",
                ResourcePrefix = "notifications",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "ZeroKnowledgeService",
                ResourcePrefix = "zk",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "RandomnessService",
                ResourcePrefix = "randomness",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "BackupService",
                ResourcePrefix = "backup",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "ConfigurationService",
                ResourcePrefix = "config",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "ComplianceService",
                ResourcePrefix = "compliance",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "ProofOfReserveService",
                ResourcePrefix = "reserves",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "SecretsManagementService",
                ResourcePrefix = "secrets",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "AutomationService",
                ResourcePrefix = "automation",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "MonitoringService",
                ResourcePrefix = "monitoring",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "HealthService",
                ResourcePrefix = "health",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "StatisticsService",
                ResourcePrefix = "stats",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "CrossChainService",
                ResourcePrefix = "crosschain",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "ComputeService",
                ResourcePrefix = "compute",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "EventSubscriptionService",
                ResourcePrefix = "events",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "NetworkSecurityService",
                ResourcePrefix = "netsec",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            },
            new ServicePermissionConfiguration
            {
                ServiceName = "EnclaveStorageService",
                ResourcePrefix = "enclave",
                AutoRegister = true,
                CreateDefaultRoles = true,
                CreateDefaultPolicies = true
            }
        };
    }
}