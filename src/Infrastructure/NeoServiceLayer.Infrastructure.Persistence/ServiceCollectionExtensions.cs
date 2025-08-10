using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Persistence;

/// <summary>
/// Extension methods for adding persistence services to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds persistence services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Add persistent storage provider based on configuration
        var storageType = configuration["Storage:Provider"] ?? "OcclumFile";
        
        switch (storageType.ToLowerInvariant())
        {
            case "occlumfile":
                services.AddSingleton<IPersistentStorageProvider, OcclumFileStorageProvider>(provider =>
                {
                    var path = configuration["Storage:Path"] ?? "/secure_storage";
                    var logger = provider.GetRequiredService<ILogger<OcclumFileStorageProvider>>();
                    return new OcclumFileStorageProvider(path, logger);
                });
                break;
                
            default:
                throw new NotSupportedException($"Storage provider '{storageType}' is not supported.");
        }
        
        return services;
    }
}