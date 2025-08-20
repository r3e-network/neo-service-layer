using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Centralized service endpoint configuration
    /// </summary>
    public class ServiceEndpoints
    {
        /// <summary>
        /// Neo N3 RPC endpoint
        /// </summary>
        public string NeoN3RpcUrl { get; set; } = string.Empty;

        /// <summary>
        /// Neo N3 WebSocket endpoint
        /// </summary>
        public string NeoN3WebSocketUrl { get; set; } = string.Empty;

        /// <summary>
        /// Neo X RPC endpoint
        /// </summary>
        public string NeoXRpcUrl { get; set; } = string.Empty;

        /// <summary>
        /// Neo X WebSocket endpoint
        /// </summary>
        public string NeoXWebSocketUrl { get; set; } = string.Empty;

        /// <summary>
        /// Redis cache endpoint
        /// </summary>
        public string RedisConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Jaeger tracing endpoint
        /// </summary>
        public string JaegerEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Database connection string
        /// </summary>
        public string DatabaseConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Allowed CORS origins
        /// </summary>
        public List<string> CorsOrigins { get; set; } = new();

        /// <summary>
        /// Creates ServiceEndpoints from environment variables or configuration
        /// </summary>
        public static ServiceEndpoints FromConfiguration(ISecureConfigurationProvider config)
        {
            var endpoints = new ServiceEndpoints();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

            // Neo N3 endpoints
            endpoints.NeoN3RpcUrl = GetEndpoint(config, "NEO_N3_RPC_URL", "Blockchain:NeoN3:RpcUrl",
                isDevelopment ? "http://localhost:20332" : null);
            endpoints.NeoN3WebSocketUrl = GetEndpoint(config, "NEO_N3_WEBSOCKET_URL", "Blockchain:NeoN3:WebSocketUrl",
                isDevelopment ? "ws://localhost:20334" : null);

            // Neo X endpoints
            endpoints.NeoXRpcUrl = GetEndpoint(config, "NEO_X_RPC_URL", "Blockchain:NeoX:RpcUrl",
                isDevelopment ? "http://localhost:8545" : null);
            endpoints.NeoXWebSocketUrl = GetEndpoint(config, "NEO_X_WEBSOCKET_URL", "Blockchain:NeoX:WebSocketUrl",
                isDevelopment ? "ws://localhost:8546" : null);

            // Redis endpoint
            endpoints.RedisConnectionString = GetEndpoint(config, "REDIS_CONNECTION_STRING", "Cache:Redis:Configuration",
                isDevelopment ? "localhost:6379" : null);

            // Jaeger endpoint
            endpoints.JaegerEndpoint = GetEndpoint(config, "JAEGER_ENDPOINT", "Monitoring:OpenTelemetry:Exporters:Jaeger:Endpoint",
                isDevelopment ? "http://localhost:14268/api/traces" : null);

            // Database connection
            endpoints.DatabaseConnectionString = GetEndpoint(config, "DATABASE_CONNECTION_STRING", "Database:ConnectionString",
                isDevelopment ? "Host=localhost;Port=5432;Database=neo_service_layer;Username=postgres;Password=CHANGE_ME_IN_DEV" : null);

            // CORS origins
            var corsOrigins = new List<string>();
            for (int i = 1; i <= 10; i++)
            {
                var origin = Environment.GetEnvironmentVariable($"CORS_ORIGIN_{i}");
                if (!string.IsNullOrEmpty(origin))
                {
                    corsOrigins.Add(origin);
                }
            }

            // Add default development origins if none specified
            if (corsOrigins.Count == 0 && isDevelopment)
            {
                corsOrigins.AddRange(new[] {
                    "http://localhost:3000",
                    "https://localhost:3001",
                    "http://localhost:5000",
                    "https://localhost:5001"
                });
            }

            endpoints.CorsOrigins = corsOrigins;

            return endpoints;
        }

        private static string GetEndpoint(ISecureConfigurationProvider config, string envKey, string configKey, string? developmentDefault)
        {
            // Try environment variable first
            var value = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(value))
                return value;

            // Try configuration
            var configValue = config.GetSecureValueAsync(configKey).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(configValue))
                return configValue;

            // Use development default if available
            if (!string.IsNullOrEmpty(developmentDefault))
                return developmentDefault;

            // Throw if no value found in production
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Required endpoint configuration '{envKey}' or '{configKey}' not found");
            }

            return string.Empty;
        }

        /// <summary>
        /// Validates all endpoints are properly configured
        /// </summary>
        public void Validate()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var isProduction = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

            if (isProduction)
            {
                // Validate no localhost URLs in production
                ValidateNoLocalhost(nameof(NeoN3RpcUrl), NeoN3RpcUrl);
                ValidateNoLocalhost(nameof(NeoN3WebSocketUrl), NeoN3WebSocketUrl);
                ValidateNoLocalhost(nameof(NeoXRpcUrl), NeoXRpcUrl);
                ValidateNoLocalhost(nameof(NeoXWebSocketUrl), NeoXWebSocketUrl);
                ValidateNoLocalhost(nameof(RedisConnectionString), RedisConnectionString);
                ValidateNoLocalhost(nameof(JaegerEndpoint), JaegerEndpoint);

                // Validate CORS origins
                foreach (var origin in CorsOrigins)
                {
                    if (origin.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"CORS origin contains localhost in production: {origin}");
                    }
                }
            }
        }

        private void ValidateNoLocalhost(string name, string value)
        {
            if (!string.IsNullOrEmpty(value) && value.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{name} contains localhost URL in production: {value}");
            }
        }
    }
}
