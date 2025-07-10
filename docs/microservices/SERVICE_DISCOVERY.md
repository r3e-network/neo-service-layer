# Service Discovery and Inter-Service Communication

This document describes how services discover and communicate with each other in the Neo Service Layer microservices architecture.

## Overview

The Neo Service Layer uses Consul for service discovery, enabling services to:
- Automatically register themselves on startup
- Discover other services dynamically
- Handle service failures gracefully
- Load balance between multiple instances

## Service Registration

### Automatic Registration

Each microservice automatically registers itself with Consul on startup using the `MicroserviceHost` base class:

```csharp
public class NotificationServiceHost : MicroserviceHost<NotificationService>
{
    protected override async Task OnServiceStartedAsync()
    {
        // Service is automatically registered
        // Registration includes:
        // - Service name and type
        // - Host and port information
        // - Health check endpoint
        // - Service tags and metadata
    }
}
```

### Registration Details

Services register with the following information:

```json
{
  "ID": "notification-service-abc123",
  "Name": "notification-service",
  "Tags": ["v1", "production"],
  "Address": "10.0.1.5",
  "Port": 5010,
  "Check": {
    "HTTP": "http://10.0.1.5:5010/health",
    "Interval": "30s",
    "Timeout": "10s"
  },
  "Meta": {
    "version": "1.0.0",
    "protocol": "http",
    "weight": "100"
  }
}
```

## Service Discovery

### Discovering Services

Services can discover other services using the `IServiceRegistry`:

```csharp
public class OrderService
{
    private readonly IServiceRegistry _serviceRegistry;
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Discover notification service
        var notificationServices = await _serviceRegistry.DiscoverServicesAsync("notification");
        var service = notificationServices.FirstOrDefault();
        
        if (service != null)
        {
            // Call notification service
            var client = new HttpClient();
            client.BaseAddress = new Uri($"http://{service.HostName}:{service.Port}");
            
            await client.PostAsJsonAsync("/api/notifications/send", new
            {
                type = "order_confirmation",
                recipient = order.CustomerEmail,
                data = order
            });
        }
    }
}
```

### Using Service Clients

The SDK provides typed clients for easier service communication:

```csharp
public class OrderController : ControllerBase
{
    private readonly INotificationClient _notificationClient;
    
    public OrderController(INotificationClient notificationClient)
    {
        _notificationClient = notificationClient;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        // Process order...
        
        // Send notification
        await _notificationClient.SendNotificationAsync(new Notification
        {
            Type = NotificationType.OrderConfirmation,
            Recipient = order.CustomerEmail,
            TemplateData = order
        });
        
        return Ok(order);
    }
}
```

## Load Balancing

### Client-Side Load Balancing

Services use client-side load balancing to distribute requests:

```csharp
public class LoadBalancedClient
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILoadBalancer _loadBalancer;
    
    public async Task<T> CallServiceAsync<T>(string serviceName, string endpoint)
    {
        // Get all healthy instances
        var services = await _serviceRegistry.DiscoverServicesAsync(serviceName);
        
        // Select instance using load balancer
        var service = _loadBalancer.SelectService(services);
        
        // Make request
        var client = new HttpClient();
        client.BaseAddress = new Uri($"http://{service.HostName}:{service.Port}");
        
        var response = await client.GetAsync(endpoint);
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

### Load Balancing Strategies

Available strategies:
- **Round Robin** (default): Distributes requests evenly
- **Least Connections**: Routes to instance with fewest active connections
- **Random**: Randomly selects an instance
- **Weighted**: Routes based on instance weights
- **Consistent Hashing**: Routes based on request hash

## Health Checks

### Service Health Monitoring

Consul continuously monitors service health:

```csharp
public class HealthCheckEndpoint
{
    [HttpGet("/health")]
    public async Task<IActionResult> CheckHealth()
    {
        // Check service dependencies
        var dbHealthy = await CheckDatabaseAsync();
        var cacheHealthy = await CheckCacheAsync();
        
        if (dbHealthy && cacheHealthy)
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    database = "healthy",
                    cache = "healthy"
                }
            });
        }
        
        return StatusCode(503, new
        {
            status = "unhealthy",
            timestamp = DateTime.UtcNow,
            checks = new
            {
                database = dbHealthy ? "healthy" : "unhealthy",
                cache = cacheHealthy ? "healthy" : "unhealthy"
            }
        });
    }
}
```

### Handling Unhealthy Services

Services are automatically removed from discovery when unhealthy:

```csharp
public class ResilientServiceClient
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ICircuitBreaker _circuitBreaker;
    
    public async Task<T> CallServiceWithFallbackAsync<T>(string serviceName, Func<T> fallback)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var services = await _serviceRegistry.DiscoverServicesAsync(serviceName);
                
                if (!services.Any())
                {
                    // No healthy services available
                    return fallback();
                }
                
                // Call service...
            });
        }
        catch (CircuitBreakerOpenException)
        {
            // Circuit breaker is open, use fallback
            return fallback();
        }
    }
}
```

## Service Mesh Integration

### Using the API Gateway

All external requests go through the API Gateway:

```
Client -> API Gateway -> Service Discovery -> Microservice
```

The gateway automatically:
- Discovers services
- Routes requests
- Handles authentication
- Applies rate limiting
- Monitors performance

### Direct Service-to-Service Communication

Services can communicate directly for internal operations:

```csharp
// In Notification Service
public class NotificationService
{
    private readonly IConfigurationClient _configClient;
    
    public async Task SendEmailAsync(EmailMessage message)
    {
        // Get email configuration from Configuration Service
        var smtpConfig = await _configClient.GetConfigurationAsync<SmtpConfig>("email:smtp");
        
        // Send email using configuration
        using var smtpClient = new SmtpClient(smtpConfig.Host, smtpConfig.Port);
        await smtpClient.SendAsync(message);
    }
}
```

## Configuration

### Service Discovery Configuration

Configure service discovery in `appsettings.json`:

```json
{
  "ServiceDiscovery": {
    "Type": "Consul",
    "Address": "http://consul:8500",
    "Datacenter": "dc1",
    "ServiceName": "notification-service",
    "ServicePort": 80,
    "HealthCheckInterval": "30s",
    "DeregisterCriticalAfter": "5m",
    "Tags": ["v1", "production"],
    "Meta": {
      "version": "1.0.0",
      "protocol": "http"
    }
  }
}
```

### Environment Variables

Services can be configured via environment variables:

```bash
# Service discovery
CONSUL_ADDRESS=http://consul:8500
CONSUL_DATACENTER=dc1
SERVICE_NAME=notification-service
SERVICE_PORT=80

# Service-specific
NOTIFICATION_SMTP_HOST=smtp.gmail.com
NOTIFICATION_SMTP_PORT=587
```

## Resilience Patterns

### Circuit Breaker

Prevent cascading failures:

```csharp
services.AddHttpClient<INotificationClient>("notification", client =>
{
    client.BaseAddress = new Uri("http://notification-service");
})
.AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (result, duration) => 
            {
                Log.Warning("Circuit breaker opened for {Duration}", duration);
            },
            onReset: () => 
            {
                Log.Information("Circuit breaker reset");
            });
}
```

### Retry with Exponential Backoff

Handle transient failures:

```csharp
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
            });
}
```

### Timeout

Prevent hanging requests:

```csharp
static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(10); // 10 seconds
}
```

## Monitoring and Observability

### Distributed Tracing

Track requests across services:

```csharp
public class TracingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Extract or create trace ID
        var traceId = context.Request.Headers["X-Trace-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        
        // Add to response
        context.Response.Headers.Add("X-Trace-ID", traceId);
        
        // Log with trace ID
        using (LogContext.PushProperty("TraceId", traceId))
        {
            await next(context);
        }
    }
}
```

### Service Metrics

Monitor service performance:

```csharp
public class MetricsMiddleware
{
    private readonly IMetricsCollector _metrics;
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await next(context);
            
            _metrics.RecordRequestDuration(
                context.Request.Path,
                context.Request.Method,
                context.Response.StatusCode,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _metrics.RecordError(context.Request.Path, ex.GetType().Name);
            throw;
        }
    }
}
```

## Best Practices

### 1. Service Naming

Use consistent naming conventions:
- Service names: `notification-service`, `configuration-service`
- Endpoints: `/api/notifications`, `/api/configuration`
- Health checks: `/health`
- Metrics: `/metrics`

### 2. Versioning

Support multiple versions:
```csharp
// Register with version tag
services.AddServiceDiscovery(options =>
{
    options.Tags = new[] { "v1", "v2-preview" };
});

// Discover specific version
var servicesV2 = await _serviceRegistry.DiscoverServicesAsync("notification", tags: new[] { "v2-preview" });
```

### 3. Graceful Shutdown

Deregister services on shutdown:
```csharp
public class GracefulShutdown : IHostedService
{
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Deregister from service discovery
        await _serviceRegistry.DeregisterServiceAsync(_serviceId);
        
        // Wait for in-flight requests
        await _requestTracker.WaitForCompletionAsync();
    }
}
```

### 4. Security

Secure service communication:
- Use mTLS between services
- Validate service identities
- Encrypt sensitive data
- Apply network policies

### 5. Testing

Test service discovery:
```csharp
[Fact]
public async Task Service_Should_Discover_Dependencies()
{
    // Arrange
    var mockRegistry = new Mock<IServiceRegistry>();
    mockRegistry.Setup(r => r.DiscoverServicesAsync("notification"))
        .ReturnsAsync(new[] { new ServiceInfo { HostName = "localhost", Port = 5010 } });
    
    var client = new OrderService(mockRegistry.Object);
    
    // Act
    await client.ProcessOrderAsync(new Order());
    
    // Assert
    mockRegistry.Verify(r => r.DiscoverServicesAsync("notification"), Times.Once);
}
```

## Troubleshooting

### Common Issues

1. **Service not discoverable**
   - Check service registration logs
   - Verify Consul connectivity
   - Ensure health checks are passing

2. **Intermittent connection failures**
   - Check network policies
   - Verify service health
   - Review circuit breaker status

3. **High latency**
   - Check service distribution
   - Review load balancing strategy
   - Monitor network performance

### Debugging Tools

```bash
# List all services
curl http://localhost:8500/v1/catalog/services

# Get service instances
curl http://localhost:8500/v1/catalog/service/notification-service

# Check service health
curl http://localhost:8500/v1/health/service/notification-service

# View service configuration
curl http://localhost:8500/v1/kv/config/notification-service

# Monitor service metrics
curl http://localhost:5010/metrics
```

## Migration Guide

### Moving from Monolith to Microservices

1. **Extract Service Interface**
```csharp
// Before: Direct class usage
public class OrderController
{
    private readonly NotificationService _notificationService;
}

// After: Interface-based
public class OrderController
{
    private readonly INotificationClient _notificationClient;
}
```

2. **Update Dependency Injection**
```csharp
// Monolith mode
services.AddScoped<INotificationService, NotificationService>();

// Microservices mode
services.AddScoped<INotificationClient, NotificationClient>();
```

3. **Handle Network Failures**
```csharp
// Add resilience
services.AddHttpClient<INotificationClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

## Conclusion

Service discovery enables the Neo Service Layer to scale horizontally while maintaining reliability. By following these patterns and best practices, services can communicate efficiently and handle failures gracefully.