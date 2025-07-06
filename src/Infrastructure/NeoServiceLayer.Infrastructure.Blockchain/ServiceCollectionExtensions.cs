using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.Infrastructure.Blockchain;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Extension methods for registering blockchain infrastructure components.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Neo Service Layer core services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNeoServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        // Add blockchain infrastructure
        services.AddBlockchainInfrastructure(configuration);

        // Add TEE Enclave services
        services.AddScoped<NeoServiceLayer.Tee.Enclave.IEnclaveWrapper, NeoServiceLayer.Tee.Enclave.ProductionSGXEnclaveWrapper>();

        // Add TEE Host services
        services.AddScoped<NeoServiceLayer.Tee.Host.Services.IEnclaveManager, NeoServiceLayer.Tee.Host.Services.EnclaveManager>();
        services.AddScoped<NeoServiceLayer.Tee.Host.Services.EnclaveManager>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<NeoServiceLayer.Tee.Host.Services.EnclaveManager>>();
            var enclaveWrapper = serviceProvider.GetRequiredService<NeoServiceLayer.Tee.Enclave.IEnclaveWrapper>();
            return new NeoServiceLayer.Tee.Host.Services.EnclaveManager(logger, enclaveWrapper);
        });

        return services;
    }
    /// <summary>
    /// Adds blockchain infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchainInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register core infrastructure
        services.AddCoreInfrastructure(configuration);

        // Register blockchain clients
        services.AddBlockchainClients(configuration);

        return services;
    }

    /// <summary>
    /// Adds core infrastructure services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register HTTP client
        services.AddHttpClient();

        // Register HTTP client service
        services.AddSingleton<IHttpClientService, HttpClientService>();

        // Register blockchain configuration
        services.Configure<BlockchainConfiguration>(configuration.GetSection("Blockchain"));

        // Register blockchain client factory
        services.AddSingleton<IBlockchainClientFactory, BlockchainClientFactory>();

        return services;
    }

    /// <summary>
    /// Adds blockchain client services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchainClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Register blockchain clients as factory-created instances
        services.AddTransient<IBlockchainClient>(provider =>
        {
            var factory = provider.GetRequiredService<IBlockchainClientFactory>();
            // Default to Neo N3, but this could be configurable
            return factory.CreateClient(BlockchainType.NeoN3);
        });

        return services;
    }
}
