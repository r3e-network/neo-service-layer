using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Extension methods for easy service registration and management.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds a service to the dependency injection container with automatic configuration.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNeoServiceWithExtensions<TService, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class, IService
        where TImplementation : class, TService
    {
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));

        return services;
    }

    /// <summary>
    /// Adds a blockchain service with automatic blockchain type configuration.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="supportedBlockchains">The supported blockchain types.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchainServiceWithExtensions<TService, TImplementation>(
        this IServiceCollection services,
        BlockchainType[] supportedBlockchains,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class, IBlockchainService
        where TImplementation : class, TService
    {
        services.AddNeoServiceWithExtensions<TService, TImplementation>(lifetime);

        // Register blockchain-specific configuration
        services.Configure<BlockchainServiceOptions<TService>>(options =>
        {
            options.SupportedBlockchains = supportedBlockchains;
        });

        return services;
    }

    /// <summary>
    /// Adds an enclave service with automatic enclave configuration.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="enclaveRequired">Whether enclave is required.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEnclaveServiceWithExtensions<TService, TImplementation>(
        this IServiceCollection services,
        bool enclaveRequired = true,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class, IEnclaveService
        where TImplementation : class, TService
    {
        services.AddNeoServiceWithExtensions<TService, TImplementation>(lifetime);

        // Register enclave-specific configuration
        services.Configure<EnclaveServiceOptions<TService>>(options =>
        {
            options.EnclaveRequired = enclaveRequired;
        });

        return services;
    }

    /// <summary>
    /// Adds a service with automatic health check registration.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="healthCheckName">The health check name.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceWithHealthCheck<TService, TImplementation>(
        this IServiceCollection services,
        string? healthCheckName = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class, IService
        where TImplementation : class, TService
    {
        services.AddNeoServiceWithExtensions<TService, TImplementation>(lifetime);

        // Register health check
        var checkName = healthCheckName ?? typeof(TService).Name;
        services.AddHealthChecks()
            .AddCheck<ServiceHealthCheck<TService>>(checkName);

        return services;
    }

    /// <summary>
    /// Adds all services from an assembly automatically.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="lifetime">The default service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServicesFromAssembly(
        this IServiceCollection services,
        System.Reflection.Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
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
                services.Add(new ServiceDescriptor(interfaceType, serviceType, lifetime));
            }

            services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));
        }

        return services;
    }

    /// <summary>
    /// Configures service dependencies automatically.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureServiceDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IServiceDependencyResolver, DefaultServiceDependencyResolver>();
        services.AddSingleton<IServiceLifecycleManager, DefaultServiceLifecycleManager>();

        return services;
    }

    /// <summary>
    /// Validates all service registrations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ValidateServiceRegistrations(this IServiceCollection services)
    {
        services.AddSingleton<IServiceValidator, DefaultServiceValidator>();

        return services;
    }
}

/// <summary>
/// Configuration options for blockchain services.
/// </summary>
/// <typeparam name="TService">The service type.</typeparam>
public class BlockchainServiceOptions<TService>
    where TService : IBlockchainService
{
    /// <summary>
    /// Gets or sets the supported blockchain types.
    /// </summary>
    public BlockchainType[] SupportedBlockchains { get; set; } = Array.Empty<BlockchainType>();
}

/// <summary>
/// Configuration options for enclave services.
/// </summary>
/// <typeparam name="TService">The service type.</typeparam>
public class EnclaveServiceOptions<TService>
    where TService : IEnclaveService
{
    /// <summary>
    /// Gets or sets whether enclave is required.
    /// </summary>
    public bool EnclaveRequired { get; set; } = true;
}

/// <summary>
/// Service health check implementation.
/// </summary>
/// <typeparam name="TService">The service type.</typeparam>
public class ServiceHealthCheck<TService> : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    where TService : IService
{
    private readonly TService _service;
    private readonly ILogger<ServiceHealthCheck<TService>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceHealthCheck{TService}"/> class.
    /// </summary>
    /// <param name="service">The service to check.</param>
    /// <param name="logger">The logger.</param>
    public ServiceHealthCheck(TService service, ILogger<ServiceHealthCheck<TService>> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _service.GetHealthAsync();

            return health switch
            {
                ServiceHealth.Healthy => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"{typeof(TService).Name} is healthy"),
                ServiceHealth.Degraded => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"{typeof(TService).Name} is degraded"),
                ServiceHealth.Unhealthy => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"{typeof(TService).Name} is unhealthy"),
                ServiceHealth.NotRunning => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"{typeof(TService).Name} is not running"),
                _ => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"{typeof(TService).Name} has unknown status")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for {ServiceType}", typeof(TService).Name);
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"{typeof(TService).Name} health check failed", ex);
        }
    }
}

/// <summary>
/// Interface for service dependency resolution.
/// </summary>
public interface IServiceDependencyResolver
{
    /// <summary>
    /// Resolves dependencies for a service.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="availableServices">Available services.</param>
    /// <returns>True if all dependencies are resolved.</returns>
    Task<bool> ResolveDependenciesAsync(IService service, IEnumerable<IService> availableServices);
}

/// <summary>
/// Interface for service lifecycle management.
/// </summary>
public interface IServiceLifecycleManager
{
    /// <summary>
    /// Starts all services in the correct order.
    /// </summary>
    /// <param name="services">The services to start.</param>
    /// <returns>True if all services started successfully.</returns>
    Task<bool> StartServicesAsync(IEnumerable<IService> services);

    /// <summary>
    /// Stops all services in the correct order.
    /// </summary>
    /// <param name="services">The services to stop.</param>
    /// <returns>True if all services stopped successfully.</returns>
    Task<bool> StopServicesAsync(IEnumerable<IService> services);
}

/// <summary>
/// Interface for service validation.
/// </summary>
public interface IServiceValidator
{
    /// <summary>
    /// Validates service configuration.
    /// </summary>
    /// <param name="services">The services to validate.</param>
    /// <returns>Validation results.</returns>
    Task<ServiceValidationResult> ValidateAsync(IEnumerable<IService> services);
}

/// <summary>
/// Service validation result.
/// </summary>
public class ServiceValidationResult
{
    /// <summary>
    /// Gets or sets whether validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
