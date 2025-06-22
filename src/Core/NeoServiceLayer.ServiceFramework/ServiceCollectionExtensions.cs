using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Neo Service Layer service framework to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNeoServiceFramework(this IServiceCollection services)
    {
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        services.AddSingleton<IServiceConfiguration, ServiceConfiguration>();
        services.AddSingleton<IServiceMetricsCollector, ServiceMetricsCollector>();
        services.AddSingleton<IServiceTemplateGenerator, ServiceTemplateGenerator>();
        return services;
    }

    /// <summary>
    /// Adds a Neo Service Layer service to the service collection.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNeoService<TService, TImplementation>(this IServiceCollection services)
        where TService : class, IService
        where TImplementation : class, TService
    {
        services.AddSingleton<TService, TImplementation>();
        services.AddSingleton<IService>(sp => sp.GetRequiredService<TService>());
        return services;
    }

    /// <summary>
    /// Adds a Neo Service Layer service to the service collection.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNeoService<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class, IService
    {
        services.AddSingleton<TService>(implementationFactory);
        services.AddSingleton<IService>(sp => sp.GetRequiredService<TService>());
        return services;
    }

    /// <summary>
    /// Adds a Neo Service Layer service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="service">The service instance.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNeoService(this IServiceCollection services, IService service)
    {
        services.AddSingleton(service);
        services.AddSingleton<IService>(service);
        return services;
    }

    /// <summary>
    /// Adds all Neo Service Layer services of a specific type to the service collection.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for services.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNeoServicesOfType<TService>(this IServiceCollection services, params System.Reflection.Assembly[] assemblies)
        where TService : class, IService
    {
        if (assemblies.Length == 0)
        {
            assemblies = new[] { typeof(TService).Assembly };
        }

        foreach (var assembly in assemblies)
        {
            var serviceTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(TService).IsAssignableFrom(t));

            foreach (var serviceType in serviceTypes)
            {
                var interfaces = serviceType.GetInterfaces()
                    .Where(i => i != typeof(IService) && typeof(IService).IsAssignableFrom(i));

                foreach (var @interface in interfaces)
                {
                    services.AddSingleton(@interface, serviceType);
                }

                services.AddSingleton<IService>(sp =>
                {
                    var interfaceType = interfaces.FirstOrDefault() ?? typeof(TService);
                    return (IService)sp.GetRequiredService(interfaceType);
                });
            }
        }

        return services;
    }

    /// <summary>
    /// Registers all Neo Service Layer services with the service registry.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void RegisterAllNeoServices(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        var services = serviceProvider.GetServices<IService>();

        foreach (var service in services)
        {
            registry.RegisterService(service);
        }
    }

    /// <summary>
    /// Validates all Neo Service Layer service dependencies.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>True if all service dependencies are valid, false otherwise.</returns>
    public static async Task<bool> ValidateAllNeoServiceDependenciesAsync(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IServiceRegistry>();
        var logger = serviceProvider.GetRequiredService<ILogger<IServiceRegistry>>();
        var services = registry.GetAllServices().ToList();
        var results = new List<bool>();

        foreach (var service in services)
        {
            try
            {
                var result = await service.ValidateDependenciesAsync(services);
                results.Add(result);

                if (!result)
                {
                    logger.LogError("Service {ServiceName} has invalid dependencies.", service.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate dependencies for service {ServiceName}.", service.Name);
                results.Add(false);
            }
        }

        return results.All(r => r);
    }

    /// <summary>
    /// Starts the Neo Service Layer metrics collection.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="interval">The collection interval.</param>
    public static void StartNeoServiceMetricsCollection(this IServiceProvider serviceProvider, TimeSpan interval)
    {
        var metricsCollector = serviceProvider.GetRequiredService<IServiceMetricsCollector>();
        metricsCollector.StartCollecting(interval);
    }

    /// <summary>
    /// Stops the Neo Service Layer metrics collection.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void StopNeoServiceMetricsCollection(this IServiceProvider serviceProvider)
    {
        var metricsCollector = serviceProvider.GetRequiredService<IServiceMetricsCollector>();
        metricsCollector.StopCollecting();
    }
}
