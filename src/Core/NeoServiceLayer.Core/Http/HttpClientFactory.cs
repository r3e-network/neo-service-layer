using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace NeoServiceLayer.Core.Http
{
    /// <summary>
    /// Factory for creating resilient HTTP clients with connection pooling and retry policies.
    /// </summary>
    public static class HttpClientFactory
    {
        private static readonly SemaphoreSlim ConnectionSemaphore = new(100); // Limit concurrent connections
        
        /// <summary>
        /// Configures HTTP client services with resilience policies.
        /// </summary>
        public static IServiceCollection AddResilientHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient("Default", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "NeoServiceLayer/2.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 50,
                EnableMultipleHttp2Connections = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
            
            return services;
        }
        
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        ILogger? logger = null;
                        if (context.Values is IDictionary<string, object> dict && dict.ContainsKey("logger"))
                        {
                            logger = dict["logger"] as ILogger;
                        }
                        
                        logger?.LogWarning(
                            "HTTP retry {RetryCount} after {TimeSpan}ms", 
                            retryCount, 
                            timespan.TotalMilliseconds);
                    });
        }
        
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, timespan) =>
                    {
                        // Log circuit breaker opening
                    },
                    onReset: () =>
                    {
                        // Log circuit breaker closing
                    });
        }
        
        /// <summary>
        /// Acquires a connection slot with rate limiting.
        /// </summary>
        public static async Task<IDisposable> AcquireConnectionAsync(CancellationToken cancellationToken = default)
        {
            await ConnectionSemaphore.WaitAsync(cancellationToken);
            return new ConnectionReleaser(ConnectionSemaphore);
        }
        
        private class ConnectionReleaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            
            public ConnectionReleaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }
            
            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}