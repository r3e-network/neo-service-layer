# Resilience Infrastructure Integration Guide

## Overview
The Neo Service Layer includes comprehensive resilience infrastructure using Polly policies to handle transient failures, circuit breaker patterns, timeouts, and bulkhead isolation.

## Integration Steps

### 1. Add Project Reference
Ensure your service project references the resilience infrastructure:

```xml
<ProjectReference Include="..\..\Infrastructure\NeoServiceLayer.Infrastructure.Resilience\NeoServiceLayer.Infrastructure.Resilience.csproj" />
```

### 2. Update Program.cs
In your service's `ConfigureServiceSpecific` method, add resilience configuration:

```csharp
protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
{
    var configuration = context.Configuration;

    // Add resilience infrastructure
    services.AddResilience(configuration);
    services.AddResiliencePolicies(configuration);
    
    // Add service-specific resilient clients
    services.AddResilientBlockchainClient(); // For blockchain operations
    services.AddResilientStorageClient();    // For storage operations
    
    // Your existing service configuration...
}
```

### 3. Add Using Statements
Include the necessary using statements at the top of your Program.cs:

```csharp
using NeoServiceLayer.Infrastructure.Resilience;
using Polly.Extensions.Http; // If using HTTP policy extensions
```

## Available Resilience Patterns

### 1. HTTP Client Policies
Pre-configured HTTP clients with retry, circuit breaker, and timeout policies:

- `ResilientHttpClient` - General purpose HTTP client
- `BlockchainClient` - Optimized for blockchain operations (longer timeouts, more retries)
- `StorageClient` - Optimized for storage operations
- Custom named clients with specific policies

### 2. Policy Interfaces
Inject `IResiliencePolicies` to access individual policies:

```csharp
public class MyService 
{
    private readonly IResiliencePolicies _resiliencePolicies;
    
    public MyService(IResiliencePolicies resiliencePolicies)
    {
        _resiliencePolicies = resiliencePolicies;
    }
    
    public async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
    {
        var policy = _resiliencePolicies.GetRetryPolicy<T>();
        return await policy.ExecuteAsync(operation);
    }
}
```

### 3. Service-Specific Policies

#### Blockchain Operations
- Higher retry counts (5 retries vs 3)
- Longer timeouts (60s vs 30s)
- More lenient circuit breaker thresholds

#### Database Operations
- Fast retries with exponential backoff
- Transient error detection
- Higher circuit breaker thresholds

#### External Service Operations
- Context-aware policies
- Bulkhead isolation for resource protection
- Service-specific configuration

## Configuration

### appsettings.json Example
```json
{
  "Resilience": {
    "RetryCount": 3,
    "RetryBaseDelaySeconds": 2,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerDurationSeconds": 30,
    "TimeoutSeconds": 30,
    
    "BlockchainRetryCount": 5,
    "BlockchainCircuitBreakerThreshold": 3,
    "BlockchainCircuitBreakerDuration": 60,
    "BlockchainTimeoutSeconds": 60
  }
}
```

### Service-Specific Configuration
Override global settings per service:

```json
{
  "Oracle": {
    "Resilience": {
      "ExternalServiceRetryCount": 5,
      "TimeoutSeconds": 45
    }
  }
}
```

## Usage Examples

### 1. HTTP Client with Resilience
```csharp
public class ExternalServiceClient
{
    private readonly HttpClient _httpClient;
    
    public ExternalServiceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BlockchainClient");
    }
    
    public async Task<string> GetDataAsync(string endpoint)
    {
        // Automatic retry, circuit breaker, and timeout handling
        var response = await _httpClient.GetAsync(endpoint);
        return await response.Content.ReadAsStringAsync();
    }
}
```

### 2. Custom Operation with Resilience
```csharp
public class ResilientService : ResilientServiceBase
{
    public ResilientService(IResiliencePolicies resiliencePolicies, ILogger<ResilientService> logger)
        : base(resiliencePolicies, logger)
    {
    }
    
    public async Task<Result> ProcessDataAsync(Data data)
    {
        return await ExecuteWithResilienceAsync(async ct =>
        {
            // Your business logic here
            return await ProcessData(data, ct);
        }, fallbackValue: Result.Empty);
    }
}
```

### 3. Blockchain-Specific Operations
```csharp
public class SmartContractService
{
    private readonly IBlockchainResiliencePolicy _blockchainPolicy;
    
    public SmartContractService(IBlockchainResiliencePolicy blockchainPolicy)
    {
        _blockchainPolicy = blockchainPolicy;
    }
    
    public async Task<TransactionResult> DeployContractAsync(byte[] contractCode)
    {
        return await _blockchainPolicy.ExecuteAsync(async () =>
        {
            // Blockchain operation with specialized resilience handling
            return await DeployToBlockchain(contractCode);
        });
    }
}
```

## Best Practices

1. **Choose Appropriate Policies**: Use blockchain policies for Neo operations, database policies for data access, and external service policies for third-party APIs.

2. **Configure Timeouts Appropriately**: Blockchain operations need longer timeouts than typical HTTP calls.

3. **Monitor Circuit Breaker State**: Circuit breakers protect your services but can impact availability. Monitor their state.

4. **Use Bulkhead Isolation**: For high-throughput services, configure bulkhead settings to prevent resource exhaustion.

5. **Test Failure Scenarios**: Verify your resilience policies work correctly under various failure conditions.

## Monitoring and Observability

The resilience infrastructure includes built-in logging for:
- Retry attempts with reasons
- Circuit breaker state changes
- Timeout occurrences
- Policy execution metrics

All events are logged with appropriate log levels for monitoring and alerting.

## Service Integration Status

✅ **Integrated Services:**
- SmartContracts Service (with blockchain-specific policies)
- Oracle Service (with external service policies)
- KeyManagement Service (with secure operation policies)
- Storage Service (with storage-specific policies)
- Notification Service (with external provider policies)
- Health Service (with monitoring-specific policies)

⏳ **Base Infrastructure:**
- MicroserviceHost includes default HTTP client resilience
- All services inherit basic retry capabilities

## Troubleshooting

### Common Issues:
1. **Missing Project Reference**: Ensure `NeoServiceLayer.Infrastructure.Resilience` is referenced
2. **Configuration Not Found**: Verify `Resilience` section exists in appsettings.json
3. **Policy Not Applied**: Check that `AddResilience()` is called before service registration

### Debug Logging:
Enable debug logging to see resilience policy execution details:
```json
{
  "Logging": {
    "LogLevel": {
      "NeoServiceLayer.Infrastructure.Resilience": "Debug"
    }
  }
}
```