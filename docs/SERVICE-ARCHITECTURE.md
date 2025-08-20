# Neo Service Layer - Service Architecture Documentation

## Overview

The Neo Service Layer provides a comprehensive, well-organized service architecture that makes it easy to add new services, enable inter-service communication, and maintain a scalable microservices ecosystem.

## Architecture Components

### 1. Service Registry
- **Purpose**: Central registry for service discovery and registration
- **Features**:
  - Dynamic service registration/unregistration
  - Service health monitoring
  - Service endpoint management
  - Load balancing configuration
  - Service metadata and capabilities

### 2. Service Communication
- **Purpose**: Inter-service communication layer
- **Features**:
  - Multiple communication patterns (Request/Response, Pub/Sub, Streaming)
  - Protocol abstraction (HTTP, gRPC, Message Queue)
  - Circuit breaker and retry policies
  - Service proxies for type-safe communication
  - Event-driven architecture support

### 3. Service Orchestrator
- **Purpose**: Manages service lifecycle and dependencies
- **Features**:
  - Dependency resolution
  - Topological service startup/shutdown
  - Health monitoring
  - Metrics collection
  - Circular dependency detection

### 4. Service Builder
- **Purpose**: Fluent API for service configuration
- **Features**:
  - Simplified service registration
  - Dependency injection integration
  - Interceptor support
  - Health check configuration
  - Metrics and tracing setup

## Quick Start Guide

### 1. Basic Service Setup

```csharp
// In Program.cs or Startup.cs
services.AddNeoServiceLayer(configuration, options =>
{
    options.EnableServiceDiscovery = true;
    options.EnableInterServiceCommunication = true;
    options.EnableOrchestration = true;
    options.AutoRegisterServices = true;
});
```

### 2. Creating a New Service

```csharp
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.ServiceArchitecture;

namespace NeoServiceLayer.Services.YourService
{
    [Service(
        Name = "YourService",
        Version = "1.0.0",
        Description = "Description of your service",
        Lifetime = ServiceLifetime.Scoped
    )]
    [ServiceDependency(typeof(IKeyManagementService))]
    [ServiceDependency(typeof(IStorageService))]
    public class YourService : ServiceBase, IYourService
    {
        private readonly IKeyManagementService _keyManagement;
        private readonly IStorageService _storage;
        private readonly ILogger<YourService> _logger;

        public YourService(
            IKeyManagementService keyManagement,
            IStorageService storage,
            ILogger<YourService> logger)
            : base("YourService", "1.0.0", "Description of your service")
        {
            _keyManagement = keyManagement;
            _storage = storage;
            _logger = logger;
        }

        public override async Task<bool> InitializeAsync()
        {
            _logger.LogInformation("Initializing {ServiceName}", Name);
            
            // Perform initialization logic
            // e.g., validate configuration, setup connections
            
            return true;
        }

        public override async Task<bool> StartAsync()
        {
            _logger.LogInformation("Starting {ServiceName}", Name);
            
            // Start any background processes or listeners
            
            IsRunning = true;
            return true;
        }

        public override async Task<bool> StopAsync()
        {
            _logger.LogInformation("Stopping {ServiceName}", Name);
            
            // Cleanup resources, stop processes
            
            IsRunning = false;
            return true;
        }

        public override async Task<ServiceHealth> GetHealthAsync()
        {
            // Check service health
            if (!IsRunning)
                return ServiceHealth.NotRunning;

            // Check dependencies
            var keyManagementHealth = await _keyManagement.GetHealthAsync();
            if (keyManagementHealth != ServiceHealth.Healthy)
                return ServiceHealth.Degraded;

            return ServiceHealth.Healthy;
        }

        // Implement your service-specific methods
        public async Task<YourResult> YourMethodAsync(YourRequest request)
        {
            // Implementation
        }
    }

    public interface IYourService : IService
    {
        Task<YourResult> YourMethodAsync(YourRequest request);
    }
}
```

### 3. Registering the Service

#### Option 1: Automatic Registration (Recommended)
```csharp
// Services with [Service] attribute are automatically registered
services.AddNeoServiceLayer(configuration, options =>
{
    options.AutoRegisterServices = true;
    options.ServiceAssemblies = new[] 
    { 
        typeof(YourService).Assembly 
    };
});
```

#### Option 2: Manual Registration
```csharp
services.AddNeoService<IYourService, YourService>(config =>
{
    config.Lifetime = ServiceLifetime.Scoped;
    config.EnableHealthCheck = true;
    config.EnableMetrics = true;
    config.EnableTracing = true;
});
```

#### Option 3: Using Service Builder
```csharp
services.AddNeoServiceLayer(configuration)
    .AddService<IYourService, YourService>(
        lifetime: ServiceLifetime.Scoped,
        configureOptions: options =>
        {
            options.Name = "YourService";
            options.Version = "1.0.0";
            options.Dependencies = new[] 
            { 
                typeof(IKeyManagementService),
                typeof(IStorageService) 
            };
            options.EnableHealthChecks = true;
            options.EnableMetrics = true;
        })
    .Build();
```

### 4. Inter-Service Communication

#### Calling Another Service
```csharp
public class YourService : ServiceBase, IYourService
{
    private readonly IServiceCommunication _communication;
    
    public async Task<Result> CallAnotherServiceAsync()
    {
        // Option 1: Direct service call
        var response = await _communication.SendAsync<Request, Response>(
            "TargetServiceId",
            new Request { Data = "test" },
            new CommunicationOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                RetryCount = 3
            });

        // Option 2: Using service proxy
        var otherService = _communication.CreateProxy<IOtherService>("OtherServiceId");
        var result = await otherService.SomeMethodAsync(parameters);
        
        return result;
    }
}
```

#### Publishing Events
```csharp
public class YourService : ServiceBase, IYourService
{
    private readonly IServiceCommunication _communication;
    
    public async Task PublishEventAsync()
    {
        var serviceEvent = new YourServiceEvent
        {
            EventType = "YourEventType",
            Data = "Event data",
            SourceServiceId = ServiceId
        };

        await _communication.PublishEventAsync(serviceEvent);
    }
}

public class YourServiceEvent : ServiceEventBase
{
    public override string EventType => "YourService.EventName";
    public string Data { get; set; }
}
```

#### Subscribing to Events
```csharp
public class YourService : ServiceBase, IYourService
{
    private IDisposable _eventSubscription;
    
    public override async Task<bool> StartAsync()
    {
        // Subscribe to events from other services
        _eventSubscription = await _communication.SubscribeAsync<OtherServiceEvent>(
            async (eventData) =>
            {
                _logger.LogInformation("Received event: {EventType}", eventData.EventType);
                await HandleEventAsync(eventData);
            },
            new EventSubscriptionOptions
            {
                Durable = true,
                MaxConcurrency = 5
            });
        
        return true;
    }
    
    public override async Task<bool> StopAsync()
    {
        _eventSubscription?.Dispose();
        return true;
    }
}
```

### 5. Service Groups

```csharp
services.AddNeoServiceGroup("SecurityServices", group =>
{
    group.AddService<IAuthenticationService, AuthenticationService>()
         .AddService<IAuthorizationService, AuthorizationService>()
         .AddService<IEncryptionService, EncryptionService>()
         .WithLifetime(ServiceLifetime.Singleton)
         .EnableHealthChecks()
         .EnableMetrics();
});
```

### 6. Advanced Configuration

#### Service with Interceptors
```csharp
services.AddNeoService<IYourService, YourService>(config =>
{
    config.Interceptors = new[]
    {
        typeof(LoggingInterceptor),
        typeof(CachingInterceptor),
        typeof(ValidationInterceptor)
    };
});

public class LoggingInterceptor : IServiceInterceptor
{
    public async Task<object> InterceptAsync(InterceptionContext context, Func<Task<object>> next)
    {
        Console.WriteLine($"Before: {context.MethodName}");
        var result = await next();
        Console.WriteLine($"After: {context.MethodName}");
        return result;
    }
}
```

#### Service with Custom Health Check
```csharp
public class YourService : ServiceBase, IYourService
{
    public override async Task<ServiceHealth> GetHealthAsync()
    {
        var health = new ServiceHealthBuilder()
            .CheckDependency(_keyManagement)
            .CheckDependency(_storage)
            .CheckCondition(IsConnected, "Database connection")
            .CheckCondition(HasValidLicense, "License validation")
            .Build();
        
        return health;
    }
}
```

## Service Lifecycle

### Startup Sequence
1. **Registration**: Services are registered with the container
2. **Discovery**: Services discover each other through the registry
3. **Dependency Resolution**: Dependencies are resolved and validated
4. **Initialization**: Services are initialized in dependency order
5. **Start**: Services are started and begin accepting requests
6. **Health Monitoring**: Continuous health checks begin

### Shutdown Sequence
1. **Stop Accepting**: Services stop accepting new requests
2. **Drain**: Existing requests are allowed to complete
3. **Stop**: Services are stopped in reverse dependency order
4. **Cleanup**: Resources are released and connections closed
5. **Unregister**: Services are removed from the registry

## Best Practices

### 1. Service Design
- Keep services focused on a single responsibility
- Design for failure and implement circuit breakers
- Use async/await throughout for scalability
- Implement comprehensive health checks
- Version your service APIs

### 2. Dependencies
- Declare all dependencies explicitly
- Avoid circular dependencies
- Use dependency injection consistently
- Consider using service proxies for remote services

### 3. Communication
- Choose appropriate communication patterns
- Implement retry logic with exponential backoff
- Use circuit breakers to prevent cascading failures
- Consider eventual consistency for distributed operations

### 4. Monitoring
- Implement detailed health checks
- Expose relevant metrics
- Use distributed tracing for debugging
- Log important events and errors

### 5. Testing
```csharp
[TestClass]
public class YourServiceTests
{
    private IServiceProvider _serviceProvider;
    private IYourService _service;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure test services
        services.AddNeoServiceLayer(configuration, options =>
        {
            options.EnableOrchestration = false; // Disable for unit tests
        });
        
        // Add mocks
        services.AddSingleton<IKeyManagementService>(Mock.Of<IKeyManagementService>());
        services.AddSingleton<IStorageService>(Mock.Of<IStorageService>());
        
        // Add service under test
        services.AddNeoService<IYourService, YourService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<IYourService>();
    }

    [TestMethod]
    public async Task YourMethod_Should_Return_Expected_Result()
    {
        // Arrange
        var request = new YourRequest { /* ... */ };
        
        // Act
        var result = await _service.YourMethodAsync(request);
        
        // Assert
        Assert.IsNotNull(result);
        // Additional assertions
    }
}
```

## Configuration

### appsettings.json
```json
{
  "NeoServiceLayer": {
    "ServiceDiscovery": {
      "RegistryUrl": "http://localhost:8500",
      "DiscoveryInterval": "00:00:30",
      "EnableAutoDiscovery": true
    },
    "Communication": {
      "Protocol": "Http",
      "DefaultTimeout": "00:00:30",
      "MaxRetries": 3,
      "EnableCircuitBreaker": true
    },
    "Orchestration": {
      "AutoStart": true,
      "HealthCheckInterval": "00:00:30",
      "StartupStrategy": "DependencyOrder",
      "ShutdownStrategy": "Graceful"
    },
    "Services": {
      "YourService": {
        "Enabled": true,
        "Endpoints": [
          {
            "Protocol": "Http",
            "Address": "localhost",
            "Port": 5001,
            "Path": "/api/yourservice"
          }
        ],
        "Configuration": {
          "MaxConcurrentRequests": 100,
          "RequestTimeout": "00:00:30",
          "CacheEnabled": true,
          "CacheDuration": "00:05:00"
        }
      }
    }
  }
}
```

## Troubleshooting

### Common Issues

1. **Service Not Starting**
   - Check dependency resolution
   - Verify all required services are registered
   - Check logs for initialization errors

2. **Communication Failures**
   - Verify service endpoints are correct
   - Check network connectivity
   - Review timeout settings

3. **Performance Issues**
   - Enable metrics to identify bottlenecks
   - Check for synchronous blocking calls
   - Review cache configuration

4. **Circular Dependencies**
   - Review service dependencies
   - Consider using events instead of direct calls
   - Refactor to break circular references

## Migration Guide

### Migrating Existing Services

1. **Implement IService Interface**
```csharp
public class ExistingService : ServiceBase, IExistingService
{
    // Existing code
}
```

2. **Add Service Attributes**
```csharp
[Service(Name = "ExistingService", Version = "1.0.0")]
[ServiceDependency(typeof(IDependency))]
public class ExistingService : ServiceBase, IExistingService
```

3. **Update Registration**
```csharp
// Old
services.AddSingleton<IExistingService, ExistingService>();

// New
services.AddNeoService<IExistingService, ExistingService>();
```

4. **Add Health Checks**
```csharp
public override async Task<ServiceHealth> GetHealthAsync()
{
    // Implement health check logic
}
```

## Support

For questions or issues:
- GitHub Issues: https://github.com/neoservicelayer/core/issues
- Documentation: https://docs.neoservicelayer.com
- Email: support@neoservicelayer.com