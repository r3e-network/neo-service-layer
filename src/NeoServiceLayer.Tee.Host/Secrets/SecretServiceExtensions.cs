using System;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Tee.Shared.Secrets;

namespace NeoServiceLayer.Tee.Host.Secrets
{
    /// <summary>
    /// Extension methods for registering secret services.
    /// </summary>
    public static class SecretServiceExtensions
    {
        /// <summary>
        /// Adds secret services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSecretServices(this IServiceCollection services)
        {
            // Register secret encryption service
            services.AddSingleton<ISecretEncryptionService, SecretEncryptionService>();

            // Register secret manager
            services.AddSingleton<ISecretManager, SecretManager>();

            return services;
        }
    }
}
