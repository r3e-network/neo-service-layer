using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace NeoServiceLayer.Infrastructure.ServiceMesh;

/// <summary>
/// Configuration for service mesh patterns including service discovery, load balancing, and resilience.
/// </summary>
public static class ServiceMeshConfiguration
{
    /// <summary>
    /// Adds service mesh capabilities to the service collection.
    /// </summary>
    public static IServiceCollection AddServiceMesh(this IServiceCollection services, IConfiguration configuration)
    {
        // Add service discovery
        services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
        services.AddSingleton<ILoadBalancer, RoundRobinLoadBalancer>();
        
        // Add service registry
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        
        // Add health checks for service mesh
        services.AddHealthChecks()
            .AddCheck<ServiceMeshHealthCheck>("service_mesh");
        
        // Configure HTTP clients with service mesh patterns
        services.AddHttpClient("ServiceMesh")
            .AddHttpMessageHandler<ServiceMeshHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetBulkheadPolicy());
        
        // Add distributed tracing
        services.AddSingleton<IDistributedTracing, JaegerTracing>();
        
        // Add service mesh middleware
        services.AddTransient<ServiceMeshMiddleware>();
        
        // Add sidecar proxy simulation
        services.AddSingleton<ISidecarProxy, EnvoySidecarProxy>();
        
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
                    var logger = context.Values.ContainsKey("logger") ? context.Values["logger"] as ILogger : null;
                    logger?.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                onBreak: (result, duration) =>
                {
                    logger?.LogWarning("Circuit breaker opened for {Duration}", duration);
                },
                onReset: () =>
                {
                    logger?.LogInformation("Circuit breaker reset");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy()
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            10, // Max parallel executions
            20  // Max queued items
        );
    }
}

/// <summary>
/// Service mesh handler for intercepting and routing requests.
/// </summary>
public class ServiceMeshHandler : DelegatingHandler
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILoadBalancer _loadBalancer;
    private readonly IDistributedTracing _tracing;
    private readonly ILogger<ServiceMeshHandler> _logger;

    public ServiceMeshHandler(
        IServiceDiscovery serviceDiscovery,
        ILoadBalancer loadBalancer,
        IDistributedTracing tracing,
        ILogger<ServiceMeshHandler> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _loadBalancer = loadBalancer;
        _tracing = tracing;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Start distributed trace
        using var span = _tracing.StartSpan("http_request", request.RequestUri?.ToString());
        
        try
        {
            // Service discovery
            var serviceName = ExtractServiceName(request.RequestUri);
            var instances = await _serviceDiscovery.GetServiceInstancesAsync(serviceName, cancellationToken)
                .ConfigureAwait(false);
            
            if (instances.Count == 0)
            {
                throw new InvalidOperationException($"No instances found for service {serviceName}");
            }
            
            // Load balancing
            var instance = _loadBalancer.SelectInstance(instances);
            
            // Update request URI with selected instance
            request.RequestUri = new Uri(instance.BuildUri(request.RequestUri.PathAndQuery));
            
            // Add tracing headers
            span.SetTag("service", serviceName);
            span.SetTag("instance", instance.Id);
            request.Headers.Add("X-Trace-Id", span.TraceId);
            request.Headers.Add("X-Span-Id", span.SpanId);
            
            // Send request
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            
            // Record metrics
            span.SetTag("http.status_code", (int)response.StatusCode);
            
            return response;
        }
        catch (Exception ex)
        {
            span.SetTag("error", true);
            span.SetTag("error.message", ex.Message);
            _logger.LogError(ex, "Service mesh request failed");
            throw;
        }
    }

    private string ExtractServiceName(Uri uri)
    {
        // Extract service name from URI
        // Example: http://user-service/api/users -> user-service
        return uri?.Host ?? "unknown";
    }
}

/// <summary>
/// Service discovery interface.
/// </summary>
public interface IServiceDiscovery
{
    Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default);
    Task RegisterServiceAsync(ServiceInstance instance, CancellationToken cancellationToken = default);
    Task DeregisterServiceAsync(string instanceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Load balancer interface.
/// </summary>
public interface ILoadBalancer
{
    ServiceInstance SelectInstance(List<ServiceInstance> instances);
}

/// <summary>
/// Service instance representation.
/// </summary>
public class ServiceInstance
{
    public string Id { get; set; }
    public string ServiceName { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public HealthStatus Health { get; set; }
    public DateTime LastHealthCheck { get; set; }

    public string BuildUri(string path)
    {
        return $"http://{Host}:{Port}{path}";
    }
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    Unknown,
    Healthy,
    Unhealthy,
    Critical
}

/// <summary>
/// Consul-based service discovery implementation.
/// </summary>
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly ILogger<ConsulServiceDiscovery> _logger;
    private readonly Dictionary<string, List<ServiceInstance>> _serviceCache = new();

    public ConsulServiceDiscovery(ILogger<ConsulServiceDiscovery> logger)
    {
        _logger = logger;
        InitializeDefaultServices();
    }

    private void InitializeDefaultServices()
    {
        // Initialize with default service instances for testing
        _serviceCache["user-service"] = new List<ServiceInstance>
        {
            new() { Id = "user-1", ServiceName = "user-service", Host = "localhost", Port = 5001, Health = HealthStatus.Healthy },
            new() { Id = "user-2", ServiceName = "user-service", Host = "localhost", Port = 5002, Health = HealthStatus.Healthy }
        };
        
        _serviceCache["oracle-service"] = new List<ServiceInstance>
        {
            new() { Id = "oracle-1", ServiceName = "oracle-service", Host = "localhost", Port = 5010, Health = HealthStatus.Healthy }
        };
    }

    public async Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false); // Simulate async operation
        
        if (_serviceCache.TryGetValue(serviceName, out var instances))
        {
            return instances.Where(i => i.Health == HealthStatus.Healthy).ToList();
        }
        
        return new List<ServiceInstance>();
    }

    public async Task RegisterServiceAsync(ServiceInstance instance, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false); // Simulate async operation
        
        if (!_serviceCache.ContainsKey(instance.ServiceName))
        {
            _serviceCache[instance.ServiceName] = new List<ServiceInstance>();
        }
        
        _serviceCache[instance.ServiceName].Add(instance);
        _logger.LogInformation("Registered service instance {InstanceId} for {ServiceName}", 
            instance.Id, instance.ServiceName);
    }

    public async Task DeregisterServiceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false); // Simulate async operation
        
        foreach (var kvp in _serviceCache)
        {
            kvp.Value.RemoveAll(i => i.Id == instanceId);
        }
        
        _logger.LogInformation("Deregistered service instance {InstanceId}", instanceId);
    }
}

/// <summary>
/// Round-robin load balancer implementation.
/// </summary>
public class RoundRobinLoadBalancer : ILoadBalancer
{
    private readonly Dictionary<string, int> _counters = new();
    private readonly object _lock = new();

    public ServiceInstance SelectInstance(List<ServiceInstance> instances)
    {
        if (instances == null || instances.Count == 0)
        {
            throw new ArgumentException("No instances available");
        }

        if (instances.Count == 1)
        {
            return instances[0];
        }

        lock (_lock)
        {
            var key = string.Join(",", instances.Select(i => i.Id).OrderBy(id => id));
            
            if (!_counters.ContainsKey(key))
            {
                _counters[key] = 0;
            }

            var index = _counters[key] % instances.Count;
            _counters[key]++;

            return instances[index];
        }
    }
}