using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Services;

namespace NeoServiceLayer.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the core services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Register JavaScript service
            services.AddSingleton<IJavaScriptService, JavaScriptService>();

            return services;
        }
    }
}
