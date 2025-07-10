using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using Polly;
using Polly.CircuitBreaker;

namespace NeoServiceLayer.SDK
{
    /// <summary>
    /// Client SDK for Neo Service Layer microservices
    /// </summary>
    public class NeoServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceRegistry _serviceRegistry;
        private readonly ILogger<NeoServiceClient> _logger;
        private readonly NeoServiceClientOptions _options;
        private readonly Dictionary<string, IAsyncPolicy<HttpResponseMessage>> _policies;

        public NeoServiceClient(HttpClient httpClient, IServiceRegistry serviceRegistry,
            ILogger<NeoServiceClient> logger, NeoServiceClientOptions? options = null)
        {
            _httpClient = httpClient;
            _serviceRegistry = serviceRegistry;
            _logger = logger;
            _options = options ?? new NeoServiceClientOptions();
            _policies = CreatePolicies();
        }

        /// <summary>
        /// Create client from configuration
        /// </summary>
        public static NeoServiceClient CreateFromConfiguration(IConfiguration configuration)
        {
            var httpClient = new HttpClient();
            var serviceRegistry = new ConsulServiceRegistry(configuration,
                new LoggerFactory().CreateLogger<ConsulServiceRegistry>());
            var logger = new LoggerFactory().CreateLogger<NeoServiceClient>();

            var options = new NeoServiceClientOptions();
            configuration.GetSection("NeoServiceClient").Bind(options);

            return new NeoServiceClient(httpClient, serviceRegistry, logger, options);
        }

        /// <summary>
        /// Get service client for specific service type
        /// </summary>
        public IServiceClient<TService> GetService<TService>() where TService : class
        {
            var serviceType = typeof(TService).Name.Replace("Service", "").Replace("I", "");
            return new ServiceClient<TService>(this, serviceType);
        }

        /// <summary>
        /// Call a service endpoint
        /// </summary>
        public async Task<TResponse?> CallServiceAsync<TResponse>(
            string serviceType,
            string endpoint,
            HttpMethod method,
            object? payload = null,
            CancellationToken cancellationToken = default)
        {
            var service = await DiscoverHealthyServiceAsync(serviceType, cancellationToken);
            if (service == null)
            {
                throw new ServiceUnavailableException($"No healthy instances of {serviceType} available");
            }

            var url = $"{service.Protocol}://{service.HostName}:{service.Port}{endpoint}";
            var policy = _policies[serviceType];

            var response = await policy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(method, url);

                if (payload != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    request.Content = JsonContent.Create(payload);
                }

                return await _httpClient.SendAsync(request, cancellationToken);
            });

            response.EnsureSuccessStatusCode();

            if (typeof(TResponse) == typeof(string))
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return (TResponse)(object)content;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Discover a healthy service instance
        /// </summary>
        private async Task<ServiceInfo?> DiscoverHealthyServiceAsync(
            string serviceType,
            CancellationToken cancellationToken)
        {
            var services = await _serviceRegistry.DiscoverServicesAsync(serviceType, cancellationToken);
            var healthyServices = services.Where(s => s.Status == ServiceStatus.Healthy).ToList();

            if (!healthyServices.Any())
            {
                _logger.LogWarning("No healthy instances found for service type {ServiceType}", serviceType);
                return null;
            }

            // Simple random load balancing
            var random = new Random();
            return healthyServices[random.Next(healthyServices.Count)];
        }

        /// <summary>
        /// Create resilience policies for each service
        /// </summary>
        private Dictionary<string, IAsyncPolicy<HttpResponseMessage>> CreatePolicies()
        {
            var policies = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();

            // Default policy with retry and circuit breaker
            var defaultPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    _options.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} after {Timespan}ms", retryCount, timespan.TotalMilliseconds);
                    })
                .WrapAsync(
                    Policy<HttpResponseMessage>
                        .Handle<HttpRequestException>()
                        .OrResult(r => !r.IsSuccessStatusCode)
                        .CircuitBreakerAsync(
                            _options.CircuitBreakerThreshold,
                            TimeSpan.FromSeconds(_options.CircuitBreakerDuration),
                            onBreak: (result, duration) =>
                            {
                                _logger.LogWarning("Circuit breaker opened for {Duration}s", duration.TotalSeconds);
                            },
                            onReset: () =>
                            {
                                _logger.LogInformation("Circuit breaker reset");
                            }));

            // Add policies for each service type
            foreach (var serviceType in GetKnownServiceTypes())
            {
                policies[serviceType] = defaultPolicy;
            }

            return policies;
        }

        private List<string> GetKnownServiceTypes()
        {
            return new List<string>
            {
                "Notification", "Configuration", "Backup", "Storage",
                "SmartContracts", "CrossChain", "Oracle", "ProofOfReserve",
                "KeyManagement", "ZeroKnowledge", "Compliance", "AbstractAccount",
                "Monitoring", "Health", "Automation", "EventSubscription"
            };
        }
    }

    /// <summary>
    /// Service client interface
    /// </summary>
    public interface IServiceClient<TService> where TService : class
    {
        Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
        Task<TResponse?> PostAsync<TResponse>(string endpoint, object payload, CancellationToken cancellationToken = default);
        Task<TResponse?> PutAsync<TResponse>(string endpoint, object payload, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service client implementation
    /// </summary>
    internal class ServiceClient<TService> : IServiceClient<TService> where TService : class
    {
        private readonly NeoServiceClient _client;
        private readonly string _serviceType;

        public ServiceClient(NeoServiceClient client, string serviceType)
        {
            _client = client;
            _serviceType = serviceType;
        }

        public Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        {
            return _client.CallServiceAsync<TResponse>(_serviceType, endpoint, HttpMethod.Get, null, cancellationToken);
        }

        public Task<TResponse?> PostAsync<TResponse>(string endpoint, object payload, CancellationToken cancellationToken = default)
        {
            return _client.CallServiceAsync<TResponse>(_serviceType, endpoint, HttpMethod.Post, payload, cancellationToken);
        }

        public Task<TResponse?> PutAsync<TResponse>(string endpoint, object payload, CancellationToken cancellationToken = default)
        {
            return _client.CallServiceAsync<TResponse>(_serviceType, endpoint, HttpMethod.Put, payload, cancellationToken);
        }

        public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            var result = await _client.CallServiceAsync<object>(_serviceType, endpoint, HttpMethod.Delete, null, cancellationToken);
            return result != null;
        }
    }

    /// <summary>
    /// Client options
    /// </summary>
    public class NeoServiceClientOptions
    {
        public int RetryCount { get; set; } = 3;
        public int CircuitBreakerThreshold { get; set; } = 5;
        public int CircuitBreakerDuration { get; set; } = 30;
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Service unavailable exception
    /// </summary>
    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException(string message) : base(message) { }
    }
}
