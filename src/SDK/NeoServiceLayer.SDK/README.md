# Neo Service Layer SDK

Client SDK for easy interaction with Neo Service Layer microservices.

## Installation

```bash
dotnet add package NeoServiceLayer.SDK
```

## Quick Start

```csharp
using NeoServiceLayer.SDK;
using Microsoft.Extensions.Configuration;

// Create client from configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var client = NeoServiceClient.CreateFromConfiguration(configuration);

// Or create manually
var httpClient = new HttpClient();
var serviceRegistry = new ConsulServiceRegistry(configuration, logger);
var client = new NeoServiceClient(httpClient, serviceRegistry, logger);
```

## Using Services

### Notification Service

```csharp
// Get notification service client
var notificationService = client.GetService<INotificationService>();

// Send a notification
var request = new SendNotificationRequest
{
    Recipient = "user@example.com",
    Subject = "Test Notification",
    Message = "This is a test message",
    Channel = NotificationChannel.Email
};

var result = await notificationService.PostAsync<NotificationResult>(
    "/api/notification/send", 
    request);

// Check status
var status = await notificationService.GetAsync<NotificationStatus>(
    $"/api/notification/status/{result.NotificationId}");
```

### Configuration Service

```csharp
var configService = client.GetService<IConfigurationService>();

// Get configuration
var config = await configService.GetAsync<ConfigurationValue>(
    "/api/configuration/myapp/database-url");

// Set configuration
var updated = await configService.PostAsync<bool>(
    "/api/configuration/myapp/database-url",
    new { value = "postgres://newhost/db" });
```

### Smart Contracts Service

```csharp
var contractService = client.GetService<ISmartContractsService>();

// Deploy a contract
var deployment = await contractService.PostAsync<ContractDeploymentResult>(
    "/api/smart-contracts/deploy",
    new
    {
        contractCode = "0x...",
        network = "NeoN3"
    });

// Call a contract method
var result = await contractService.PostAsync<ContractInvocationResult>(
    "/api/smart-contracts/invoke",
    new
    {
        contractHash = deployment.ContractHash,
        method = "transfer",
        parameters = new[] { "sender", "recipient", 100 }
    });
```

## Direct Service Calls

For more control, use the client directly:

```csharp
// Call any service endpoint
var response = await client.CallServiceAsync<MyResponse>(
    "Notification",              // Service type
    "/api/notification/custom",  // Endpoint
    HttpMethod.Post,            // HTTP method
    new { data = "value" }      // Payload
);
```

## Configuration

Configure the SDK via appsettings.json:

```json
{
  "NeoServiceClient": {
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDuration": 30,
    "TimeoutSeconds": 30
  },
  "Consul": {
    "Address": "http://localhost:8500",
    "Datacenter": "dc1"
  }
}
```

## Error Handling

The SDK includes built-in resilience:
- Automatic retries with exponential backoff
- Circuit breaker pattern
- Service discovery with health checks

```csharp
try
{
    var result = await service.GetAsync<Data>("/api/data");
}
catch (ServiceUnavailableException ex)
{
    // No healthy service instances available
    logger.LogError(ex, "Service unavailable");
}
catch (HttpRequestException ex)
{
    // Network or HTTP error
    logger.LogError(ex, "Request failed");
}
catch (TaskCanceledException ex)
{
    // Request timeout
    logger.LogError(ex, "Request timed out");
}
```

## Advanced Usage

### Custom Policies

```csharp
// Create custom retry policy
var retryPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .WaitAndRetryAsync(
        5,
        retryAttempt => TimeSpan.FromSeconds(retryAttempt),
        onRetry: (outcome, timespan) =>
        {
            logger.LogWarning("Retrying after {delay}ms", timespan.TotalMilliseconds);
        });

// Use with client
var options = new NeoServiceClientOptions
{
    RetryCount = 5,
    CircuitBreakerThreshold = 10
};

var client = new NeoServiceClient(httpClient, serviceRegistry, logger, options);
```

### Service Discovery

```csharp
// Manually discover services
var services = await serviceRegistry.DiscoverServicesAsync("Notification");
var healthyService = services.FirstOrDefault(s => s.Status == ServiceStatus.Healthy);

if (healthyService != null)
{
    var url = $"{healthyService.Protocol}://{healthyService.HostName}:{healthyService.Port}";
    // Use service...
}
```

### Metrics Collection

```csharp
// Add metrics collection
services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();

// Track custom metrics
metricsCollector.RecordServiceCall("Notification", duration, success);
```

## Testing

For unit tests, mock the service registry:

```csharp
var mockRegistry = new Mock<IServiceRegistry>();
mockRegistry.Setup(r => r.DiscoverServicesAsync("Notification", It.IsAny<CancellationToken>()))
    .ReturnsAsync(new[] { new ServiceInfo { /* ... */ } });

var client = new NeoServiceClient(httpClient, mockRegistry.Object, logger);
```

## Contributing

See the main repository for contribution guidelines.

## License

[Your License]