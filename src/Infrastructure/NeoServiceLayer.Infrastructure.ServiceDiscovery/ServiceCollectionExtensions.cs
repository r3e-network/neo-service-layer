using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NeoServiceLayer.Infrastructure.ServiceDiscovery;

/// <summary>
/// Extension methods for adding service discovery to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds service discovery services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Consul service registry
        services.AddSingleton<IServiceRegistry, ConsulServiceRegistry>();
        
        // Configure Consul settings
        services.Configure<ConsulOptions>(configuration.GetSection("Consul"));
        
        return services;
    }
}

/// <summary>
/// Options for Consul configuration.
/// </summary>
public class ConsulOptions
{
    /// <summary>
    /// Gets or sets the Consul address.
    /// </summary>
    public string Address { get; set; } = "http://localhost:8500";
    
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }
    
    /// <summary>
    /// Gets or sets the service ID.
    /// </summary>
    public string? ServiceId { get; set; }
}