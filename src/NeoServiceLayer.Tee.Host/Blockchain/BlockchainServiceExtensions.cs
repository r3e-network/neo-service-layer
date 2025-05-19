using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain
{
    /// <summary>
    /// Extension methods for registering blockchain services.
    /// </summary>
    public static class BlockchainServiceExtensions
    {
        /// <summary>
        /// Adds blockchain services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddBlockchainServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register blockchain service factory
            services.AddSingleton<BlockchainServiceFactory>();

            // Register blockchain service
            services.AddSingleton<IBlockchainService>(provider =>
            {
                var factory = provider.GetRequiredService<BlockchainServiceFactory>();
                var blockchainType = configuration.GetValue<BlockchainType>("Blockchain:Type", BlockchainType.NeoN3);
                var rpcUrl = configuration.GetValue<string>("Blockchain:RpcUrl", "http://localhost:10332");
                var network = configuration.GetValue<string>("Blockchain:Network", "mainnet");

                return factory.CreateBlockchainService(blockchainType, rpcUrl, network);
            });

            // Register blockchain event listener
            services.AddSingleton<IBlockchainEventListener>(provider =>
            {
                var factory = provider.GetRequiredService<BlockchainServiceFactory>();
                var blockchainService = provider.GetRequiredService<IBlockchainService>();
                var storageManager = provider.GetRequiredService<Storage.IStorageManager>();

                return factory.CreateBlockchainEventListener(blockchainService, storageManager);
            });

            // Register blockchain event listener as a hosted service
            services.AddHostedService<BlockchainEventListenerService>();

            return services;
        }
    }
}
