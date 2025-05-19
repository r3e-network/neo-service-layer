using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Tee.Shared.Functions;

namespace NeoServiceLayer.Tee.Host.Functions
{
    /// <summary>
    /// Extension methods for registering function execution services.
    /// </summary>
    public static class FunctionExecutionServiceExtensions
    {
        /// <summary>
        /// Adds function execution services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddFunctionExecutionServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register function execution enclave
            services.AddSingleton<IFunctionExecutionEnclave, FunctionExecutionEnclave>();

            // Register function execution service
            services.AddSingleton<IFunctionExecutionService, FunctionExecutionService>();

            return services;
        }
    }
}
