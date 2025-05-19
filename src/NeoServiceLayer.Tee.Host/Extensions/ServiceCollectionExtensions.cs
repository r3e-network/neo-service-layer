using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Interfaces;
using NeoServiceLayer.Tee.Host.Models;
using NeoServiceLayer.Tee.Host.Occlum;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the TEE services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddTeeServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Configure TEE options
            services.Configure<TeeOptions>(configuration.GetSection("Tee"));

            // Register Occlum services
            services.AddSingleton<IOcclumManager, OcclumManager>();
            
            // Register TEE interfaces
            services.AddSingleton<OcclumInterface>();
            services.AddSingleton<IOcclumInterface>(sp => sp.GetRequiredService<OcclumInterface>());

            // Register the OpenEnclave adapter for backward compatibility
            services.AddSingleton<OpenEnclaveTeeInterface>();
            services.AddSingleton<IOpenEnclaveInterface>(sp =>
            {
                // Try to get the OpenEnclaveTeeInterface first, fallback to adapter if not available
                try
                {
                    return sp.GetRequiredService<OpenEnclaveTeeInterface>();
                }
                catch
                {
                    var logger = sp.GetRequiredService<ILogger<OpenEnclaveAdapter>>();
                    var occlumInterface = sp.GetRequiredService<IOcclumInterface>();
                    return new OpenEnclaveAdapter(occlumInterface, logger);
                }
            });

            // Register TEE client - now using Occlum by default
            services.AddSingleton<ITeeClient, OcclumTeeClient>();

            // Register TEE service
            services.AddSingleton<ITeeService, TeeService>();

            return services;
        }
    }
}
