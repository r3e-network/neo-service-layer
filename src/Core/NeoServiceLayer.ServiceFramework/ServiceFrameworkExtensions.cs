using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using System.Reflection;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Extension methods for configuring the Neo Service Layer framework.
/// </summary>
public static class ServiceFrameworkExtensions
{
    /// <summary>
    /// Adds the Neo Service Layer framework to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNeoServiceFramework(this IServiceCollection services, IConfiguration configuration)
    {
        // Add core framework services
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        services.AddSingleton<ServiceMetricsCollector>();
        services.AddSingleton<ServiceTemplateGenerator>();

        // Add configuration
        services.Configure<ServiceFrameworkOptions>(configuration.GetSection("ServiceFramework"));

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<ServiceFrameworkHealthCheck>("service-framework");

        // Add metrics
        services.AddSingleton<ServiceFrameworkMetrics>();

        return services;
    }

    /// <summary>
    /// Adds a Neo service to the service collection.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNeoService<TInterface, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TInterface : class, IService
        where TImplementation : class, TInterface
    {
        // Register the service
        services.Add(new ServiceDescriptor(typeof(TInterface), typeof(TImplementation), serviceLifetime));

        // Register as hosted service if it implements IHostedService
        if (typeof(IHostedService).IsAssignableFrom(typeof(TImplementation)))
        {
            services.AddSingleton<IHostedService>(provider => (IHostedService)provider.GetRequiredService<TInterface>());
        }

        // Register health check if it implements IHealthCheck
        if (typeof(IHealthCheck).IsAssignableFrom(typeof(TImplementation)))
        {
            var serviceName = typeof(TImplementation).Name.ToLowerInvariant().Replace("service", "");
            services.AddHealthChecks()
                .AddTypeActivatedCheck<IHealthCheck>(serviceName, typeof(TImplementation));
        }

        return services;
    }

    /// <summary>
    /// Adds persistent storage to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPersistentStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var storageConfig = configuration.GetSection("Storage");
        var providerType = storageConfig.GetValue<string>("Provider", "OcclumFile");

        switch (providerType.ToLowerInvariant())
        {
            case "occlumfile":
                services.AddSingleton<IPersistentStorageProvider>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<OcclumFileStorageProvider>>();
                    var storagePath = storageConfig.GetValue<string>("Path", "/secure_storage");
                    return new OcclumFileStorageProvider(storagePath, logger);
                });
                break;

            case "rocksdb":
                // RocksDB provider - fallback to Occlum file storage with warning
                services.AddSingleton<IPersistentStorageProvider>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<OcclumFileStorageProvider>>();
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var storagePath = configuration["Storage:Path"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage", "rocksdb-fallback");
                    logger.LogWarning("RocksDB storage provider not yet implemented. Falling back to Occlum file storage at {Path}. This may have different performance characteristics!", storagePath);
                    return new OcclumFileStorageProvider(storagePath, logger);
                });
                break;

            case "leveldb":
                // LevelDB provider - fallback to Occlum file storage with warning
                services.AddSingleton<IPersistentStorageProvider>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<OcclumFileStorageProvider>>();
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var storagePath = configuration["Storage:Path"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage", "leveldb-fallback");
                    logger.LogWarning("LevelDB storage provider not yet implemented. Falling back to Occlum file storage at {Path}. This may have different performance characteristics!", storagePath);
                    return new OcclumFileStorageProvider(storagePath, logger);
                });
                break;

            default:
                throw new ArgumentException($"Unknown storage provider: {providerType}");
        }

        return services;
    }

    /// <summary>
    /// Registers all Neo services from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for services.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNeoServicesFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var serviceTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IService).IsAssignableFrom(t))
                .ToList();

            foreach (var serviceType in serviceTypes)
            {
                var interfaces = serviceType.GetInterfaces()
                    .Where(i => i != typeof(IService) && typeof(IService).IsAssignableFrom(i))
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddSingleton(interfaceType, serviceType);

                    // Register as hosted service if applicable
                    if (typeof(IHostedService).IsAssignableFrom(serviceType))
                    {
                        services.AddSingleton<IHostedService>(provider => (IHostedService)provider.GetRequiredService(interfaceType));
                    }

                    // Register health check if applicable
                    if (typeof(IHealthCheck).IsAssignableFrom(serviceType))
                    {
                        var serviceName = serviceType.Name.ToLowerInvariant().Replace("service", "");
                        services.AddHealthChecks()
                            .AddTypeActivatedCheck<IHealthCheck>(serviceName, serviceType);
                    }
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Configures the service registry with all registered services.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ConfigureServiceRegistryAsync(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        var services = serviceProvider.GetServices<IService>();

        foreach (var service in services)
        {
            await registry.RegisterServiceAsync(service);
        }
    }

    /// <summary>
    /// Validates all service dependencies.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task<bool> ValidateServiceDependenciesAsync(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        var services = await registry.GetAllServicesAsync();

        var allValid = true;
        foreach (var service in services)
        {
            var isValid = await service.ValidateDependenciesAsync(services);
            if (!isValid)
            {
                allValid = false;
            }
        }

        return allValid;
    }
}

/// <summary>
/// Configuration options for the service framework.
/// </summary>
public class ServiceFrameworkOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic service discovery.
    /// </summary>
    public bool EnableAutoDiscovery { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the service startup timeout.
    /// </summary>
    public TimeSpan ServiceStartupTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the health check interval.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the metrics collection interval.
    /// </summary>
    public TimeSpan MetricsCollectionInterval { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Health check for the service framework.
/// </summary>
public class ServiceFrameworkHealthCheck : IHealthCheck
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<ServiceFrameworkHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFrameworkHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceRegistry">The service registry.</param>
    /// <param name="logger">The logger.</param>
    public ServiceFrameworkHealthCheck(IServiceRegistry serviceRegistry, ILogger<ServiceFrameworkHealthCheck> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var services = await _serviceRegistry.GetAllServicesAsync();
            var healthyServices = 0;
            var unhealthyServices = 0;
            var totalServices = services.Count();

            foreach (var service in services)
            {
                var health = await service.GetHealthAsync();
                if (health == ServiceHealth.Healthy)
                {
                    healthyServices++;
                }
                else
                {
                    unhealthyServices++;
                }
            }

            var data = new Dictionary<string, object>
            {
                ["total_services"] = totalServices,
                ["healthy_services"] = healthyServices,
                ["unhealthy_services"] = unhealthyServices
            };

            if (unhealthyServices == 0)
            {
                return HealthCheckResult.Healthy($"All {totalServices} services are healthy", data);
            }
            else if (healthyServices > unhealthyServices)
            {
                return HealthCheckResult.Degraded($"{unhealthyServices} of {totalServices} services are unhealthy", null, data);
            }
            else
            {
                return HealthCheckResult.Unhealthy($"{unhealthyServices} of {totalServices} services are unhealthy", null, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service framework health check failed");
            return HealthCheckResult.Unhealthy("Service framework health check failed", ex);
        }
    }
}

/// <summary>
/// Metrics collector for the service framework.
/// </summary>
public class ServiceFrameworkMetrics
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<ServiceFrameworkMetrics> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFrameworkMetrics"/> class.
    /// </summary>
    /// <param name="serviceRegistry">The service registry.</param>
    /// <param name="logger">The logger.</param>
    public ServiceFrameworkMetrics(IServiceRegistry serviceRegistry, ILogger<ServiceFrameworkMetrics> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Collects metrics from all registered services.
    /// </summary>
    /// <returns>A dictionary containing all service metrics.</returns>
    public async Task<Dictionary<string, object>> CollectMetricsAsync()
    {
        var allMetrics = new Dictionary<string, object>();

        try
        {
            var services = await _serviceRegistry.GetAllServicesAsync();

            foreach (var service in services)
            {
                try
                {
                    var serviceMetrics = await service.GetMetricsAsync();
                    allMetrics[$"service_{service.Name.ToLowerInvariant()}"] = serviceMetrics;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect metrics from service {ServiceName}", service.Name);
                }
            }

            // Add framework-level metrics
            allMetrics["framework_total_services"] = services.Count();
            allMetrics["framework_collection_time"] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect service framework metrics");
        }

        return allMetrics;
    }
}
