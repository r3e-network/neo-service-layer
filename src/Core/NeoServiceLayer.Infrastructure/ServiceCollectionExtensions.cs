using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds blockchain clients to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="rpcUrls">The RPC URLs for each blockchain type.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddBlockchainClients(this IServiceCollection services, Dictionary<BlockchainType, string> rpcUrls)
    {
        services.AddSingleton<IBlockchainClientFactory>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new BlockchainClientFactory(loggerFactory, rpcUrls);
        });

        return services;
    }

    /// <summary>
    /// Adds a blockchain client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="rpcUrl">The RPC URL.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddBlockchainClient(this IServiceCollection services, BlockchainType blockchainType, string rpcUrl)
    {
        return services.AddBlockchainClients(new Dictionary<BlockchainType, string>
        {
            { blockchainType, rpcUrl }
        });
    }
}
