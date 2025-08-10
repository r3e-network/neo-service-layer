# Neo Service Layer - Service Framework Guide

## Overview

The Neo Service Layer provides a comprehensive service framework that ensures all microservices follow consistent patterns and best practices. The framework is built on top of .NET 9.0 and provides extensive features for building scalable, secure, and maintainable services.

## Framework Architecture

### Core Components

1. **ServiceBase** - The foundation class that all services inherit from
2. **MicroserviceHost** - Handles service hosting, configuration, and lifecycle
3. **Service Registry** - Manages service discovery and dependency resolution
4. **Health Monitoring** - Built-in health checks and status reporting
5. **Metrics Collection** - Automatic performance and operational metrics
6. **Configuration Management** - Centralized configuration with hot-reload support

### Service Base Classes

The framework provides specialized base classes for different service types:

- **ServiceBase** - Standard services
- **EnclaveServiceBase** - Services with Intel SGX enclave support
- **BlockchainServiceBase** - Services with blockchain integration
- **EnclaveBlockchainServiceBase** - Combined enclave and blockchain support
- **AIServiceBase** - AI/ML services with model management
- **CryptographicServiceBase** - Cryptographic operations
- **DataServiceBase** - Data-intensive services with persistence
- **PersistentServiceBase** - Services requiring state persistence

## Framework Features

### 1. Service Lifecycle Management

All services follow a consistent lifecycle:

```csharp
public interface IService
{
    // Service metadata
    string Name { get; }
    string Description { get; }
    string Version { get; }
    bool IsRunning { get; }

    // Lifecycle methods
    Task<bool> InitializeAsync();
    Task<bool> StartAsync();
    Task<bool> StopAsync();
    
    // Health and monitoring
    Task<ServiceHealth> GetHealthAsync();
    Task<IDictionary<string, object>> GetMetricsAsync();
    
    // Dependencies
    Task<bool> ValidateDependenciesAsync(IEnumerable<IService> availableServices);
}
```

### 2. Dependency Management

Services can declare dependencies on other services:

```csharp
// In service constructor
AddRequiredDependency("Storage", "1.0.0");
AddOptionalDependency("Cache", "1.0.0");
AddRequiredDependency<IKeyManagementService>("KeyManagement", "1.0.0");
```

### 3. Capability Declaration

Services declare their capabilities for discovery:

```csharp
// In service constructor
AddCapability<IStorageService>();
AddCapability<IEnclaveService>();
```

### 4. Health Monitoring

Built-in health check system with three states:
- **Healthy** - Service is fully operational
- **Degraded** - Service is operational but with reduced capacity
- **Unhealthy** - Service is not operational

### 5. Metrics Collection

Automatic collection of:
- Request counts and rates
- Success/failure metrics
- Processing times
- Resource usage (CPU, memory)
- Custom service-specific metrics

### 6. Configuration Management

Hierarchical configuration with support for:
- appsettings.json
- Environment-specific overrides
- Environment variables
- Secrets management
- Hot-reload capabilities

### 7. Service Discovery

Automatic service registration with Consul:
- Health check endpoints
- Service metadata
- Capability discovery
- Load balancing support

### 8. Resilience Patterns

Built-in support for:
- Circuit breakers
- Retry policies
- Timeout handling
- Bulkhead isolation
- Rate limiting

## Creating a New Service

### Method 1: Using the Service Generator Script

The easiest way to create a new service:

```bash
./scripts/create-new-service.sh MyService [options]

Options:
  --enclave        Include enclave support
  --blockchain     Include blockchain support
  --ai             Use AI service base
  --crypto         Use cryptographic service base
  --data           Use data service base
  --minimal        Create minimal version
  --no-tests       Skip test project creation
  --no-docker      Skip Dockerfile creation
```

### Method 2: Manual Implementation

1. **Create Service Interface**
```csharp
public interface IMyService : IService
{
    Task<MyResponse> ProcessAsync(MyRequest request);
}
```

2. **Implement Service**
```csharp
public class MyService : ServiceBase, IMyService
{
    public MyService(IServiceConfiguration configuration, ILogger<MyService> logger)
        : base("MyService", "My Service Description", "1.0.0", logger)
    {
        // Add capabilities and dependencies
        AddCapability<IMyService>();
        AddOptionalDependency("Storage", "1.0.0");
    }

    protected override async Task<bool> OnInitializeAsync()
    {
        // Initialize service
        return true;
    }

    protected override async Task<bool> OnStartAsync()
    {
        // Start service
        return true;
    }

    protected override async Task<bool> OnStopAsync()
    {
        // Stop service
        return true;
    }

    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check health
        return ServiceHealth.Healthy;
    }

    public async Task<MyResponse> ProcessAsync(MyRequest request)
    {
        // Implement business logic
    }
}
```

3. **Create Host Configuration**
```csharp
public class MyServiceHost : MicroserviceHost<MyService>
{
    public MyServiceHost(string[] args) : base(args) { }

    protected override void ConfigureServiceSpecific(
        WebHostBuilderContext context, 
        IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
    }
}
```

4. **Create Program.cs**
```csharp
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var host = new MyServiceHost(args);
        return await host.RunAsync();
    }
}
```

## Service Implementation Guidelines

### 1. Constructor Pattern
- Initialize readonly fields
- Set up dependencies
- Declare capabilities
- Configure metadata

### 2. Initialization
- Validate configuration
- Initialize connections
- Set up resources
- Verify dependencies

### 3. Health Checks
- Check critical dependencies
- Verify resource availability
- Monitor performance metrics
- Report degraded states appropriately

### 4. Error Handling
- Use structured logging
- Handle exceptions gracefully
- Implement circuit breakers
- Provide meaningful error responses

### 5. Metrics
- Track all operations
- Monitor resource usage
- Record performance data
- Export custom metrics

## Framework Extension Points

### Custom Service Base Classes

Create specialized base classes for your domain:

```csharp
public abstract class MyDomainServiceBase : ServiceBase
{
    protected readonly IMyDomainContext _context;

    protected MyDomainServiceBase(
        string name, 
        string description, 
        string version,
        IMyDomainContext context,
        ILogger logger)
        : base(name, description, version, logger)
    {
        _context = context;
    }

    // Add domain-specific methods
}
```

### Custom Health Checks

Implement domain-specific health checks:

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Check database connectivity
    }
}
```

### Custom Metrics Collectors

Add specialized metrics collection:

```csharp
public class CustomMetricsCollector : IMetricsCollector
{
    public async Task<Dictionary<string, object>> CollectAsync()
    {
        // Collect custom metrics
    }
}
```

## Best Practices

### 1. Service Design
- Keep services focused on a single domain
- Design for failure and recovery
- Implement proper health checks
- Use async/await throughout

### 2. Configuration
- Use strongly-typed configuration
- Validate configuration on startup
- Support configuration hot-reload
- Never hardcode secrets

### 3. Logging
- Use structured logging
- Log at appropriate levels
- Include correlation IDs
- Avoid logging sensitive data

### 4. Testing
- Write unit tests for business logic
- Integration tests for service interactions
- Load tests for performance
- Chaos tests for resilience

### 5. Deployment
- Use health check endpoints
- Implement graceful shutdown
- Configure resource limits
- Enable monitoring and alerts

## Service Communication Patterns

### 1. Direct HTTP/gRPC
- Use typed clients
- Implement retry policies
- Handle timeouts properly
- Use circuit breakers

### 2. Message Queue
- Use for async operations
- Implement idempotency
- Handle poison messages
- Monitor queue depths

### 3. Service Mesh
- Use for traffic management
- Implement mutual TLS
- Configure load balancing
- Enable distributed tracing

## Monitoring and Observability

### 1. Health Endpoints
- `/health` - Basic health check
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### 2. Metrics Endpoints
- `/metrics` - Prometheus format
- `/metrics/custom` - Custom metrics

### 3. Distributed Tracing
- OpenTelemetry integration
- Correlation ID propagation
- Span creation and tagging

## Security Considerations

### 1. Authentication
- JWT bearer tokens
- Certificate authentication
- API key authentication

### 2. Authorization
- Role-based access control
- Policy-based authorization
- Resource-based permissions

### 3. Data Protection
- Encryption at rest
- Encryption in transit
- Key rotation support
- Secure key storage

## Troubleshooting

### Common Issues

1. **Service fails to start**
   - Check configuration validity
   - Verify dependencies are available
   - Review startup logs

2. **Health check failures**
   - Check dependency health
   - Verify resource availability
   - Review error logs

3. **Performance issues**
   - Check metrics dashboard
   - Review resource usage
   - Analyze distributed traces

### Debugging Tools

1. **Logging**
   - Enable debug logging
   - Use correlation IDs
   - Check centralized logs

2. **Metrics**
   - Prometheus queries
   - Grafana dashboards
   - Custom metric analysis

3. **Tracing**
   - Jaeger UI
   - Trace analysis
   - Latency investigation

## Framework Evolution

The service framework is continuously evolving. Future enhancements include:

- Enhanced service mesh integration
- Advanced chaos engineering support
- Improved AI/ML service patterns
- Extended blockchain support
- Advanced security features

## Conclusion

The Neo Service Layer framework provides a robust foundation for building microservices. By following the patterns and practices outlined in this guide, you can create services that are:

- **Consistent** - Following established patterns
- **Reliable** - With proper error handling and recovery
- **Observable** - With comprehensive monitoring
- **Secure** - With built-in security features
- **Scalable** - Ready for production workloads

For additional support, refer to the example services in the codebase or use the service generator script to quickly create new services.