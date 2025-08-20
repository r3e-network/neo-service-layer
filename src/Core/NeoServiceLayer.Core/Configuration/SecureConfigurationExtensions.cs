using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Extension methods for configuring secure configuration providers
    /// </summary>
    public static class SecureConfigurationExtensions
    {
        /// <summary>
        /// Adds secure configuration services to the DI container
        /// </summary>
        public static IServiceCollection AddSecureConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<ISecureConfigurationProvider, SecureConfigurationProvider>();
            return services;
        }

        /// <summary>
        /// Validates required configuration values are present and not placeholders
        /// </summary>
        public static IHost ValidateRequiredConfiguration(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<ISecureConfigurationProvider>();
            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

            // Only validate in production
            if (!environment.IsProduction())
                return host;

            // Define required configuration keys for production
            var requiredKeys = new[]
            {
                "JWT_SECRET_KEY",
                "NEO_N3_RPC_URL",
                "NEO_X_RPC_URL"
            };

            foreach (var key in requiredKeys)
            {
                var value = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException(
                        $"Required environment variable '{key}' is not set. " +
                        $"Please configure all required values for production deployment.");
                }

                // Check for common placeholder patterns
                if (IsPlaceholder(value))
                {
                    throw new InvalidOperationException(
                        $"Environment variable '{key}' contains a placeholder value: '{value}'. " +
                        $"Please set the actual production value.");
                }
            }

            return host;
        }

        /// <summary>
        /// Configures the application to load environment-specific configuration
        /// </summary>
        public static IConfigurationBuilder AddSecureConfiguration(this IConfigurationBuilder builder)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Add environment variables with proper prefixes
            builder.AddEnvironmentVariables("NEOSERVICE_");
            builder.AddEnvironmentVariables("SECRET_");
            builder.AddEnvironmentVariables("CONNECTIONSTRINGS_");

            // Add .env file support for local development
            if (environment == "Development")
            {
                var envFile = ".env.development";
                if (System.IO.File.Exists(envFile))
                {
                    builder.AddJsonFile(envFile, optional: true, reloadOnChange: true);
                }
            }

            return builder;
        }

        private static bool IsPlaceholder(string value)
        {
            var placeholders = new[]
            {
                "your-", "placeholder", "example", "changeme",
                "todo", "fixme", "xxx", "dummy", "test-",
                "sample", "demo", "<", "{{", "__"
            };

            var lowerValue = value.ToLower();
            foreach (var placeholder in placeholders)
            {
                if (lowerValue.Contains(placeholder))
                    return true;
            }

            return false;
        }
    }
}
