# Neo Service Layer - Service Framework

## Overview

The Neo Service Layer Service Framework provides a comprehensive, standardized foundation for creating, registering, and managing services within the Neo Service Layer. It is designed to support the platform's **15 focused services** across three categories: Core Infrastructure, Specialized AI, and Advanced Infrastructure services. The framework ensures consistency, reliability, and seamless integration with Intel SGX + Occlum LibOS enclaves.

## Core Components

### Service Interfaces

The service framework defines a hierarchy of interfaces that services implement based on their requirements:

#### Core Service Interfaces

- **IService**: Base interface for all services providing lifecycle management (initialize, start, stop) and health monitoring
- **IEnclaveService**: Interface for services requiring Intel SGX + Occlum LibOS enclave operations
- **IBlockchainService**: Interface for services supporting blockchain operations (Neo N3/NeoX)

#### Specialized Service Interfaces

- **IPredictionService**: Interface for AI-powered prediction and forecasting services
- **IPatternRecognitionService**: Interface for AI-powered pattern detection and classification services
- **IZeroKnowledgeService**: Interface for privacy-preserving computation services
- **IFairOrderingService**: Interface for transaction fairness and MEV protection services

### Service Base Classes

The framework provides base classes implementing common functionality:

#### Core Base Classes

- **ServiceBase**: Base implementation of IService with lifecycle management
- **EnclaveServiceBase**: Base for services requiring enclave operations
- **BlockchainServiceBase**: Base for services supporting blockchain operations
- **EnclaveBlockchainServiceBase**: Combined base for services requiring both enclave and blockchain operations

#### Specialized Base Classes

- **AIServiceBase**: Base class for AI-powered services with model management
- **CryptographicServiceBase**: Base class for cryptographic services with key management
- **DataServiceBase**: Base class for data-intensive services with storage integration

### Service Registry

The service registry provides a centralized way to register and discover services:

- **IServiceRegistry**: Interface for the service registry, providing methods to register, unregister, and discover services.
- **ServiceRegistry**: Implementation of the IServiceRegistry interface, managing the lifecycle of registered services.

### Service Configuration

The service configuration system provides a way to configure services:

- **IServiceConfiguration**: Interface for service configuration, providing methods to get and set configuration values.
- **ServiceConfiguration**: Implementation of the IServiceConfiguration interface, managing configuration values.

### Dependency Injection

The service framework integrates with the .NET dependency injection system:

- **ServiceCollectionExtensions**: Extension methods for IServiceCollection, providing methods to register services and the service framework

## Service Categories

The Neo Service Layer organizes services into three distinct categories:

### Core Infrastructure Services (11)
Essential blockchain infrastructure services that provide foundational capabilities:
- Randomness, Oracle, Key Management, Compute, Storage
- Compliance, Event Subscription, Automation, Cross-Chain
- Proof of Reserve, Zero-Knowledge

### Specialized AI Services (2)
AI-powered services that bring intelligence to smart contracts:
- Prediction Service (forecasting, sentiment analysis)
- Pattern Recognition Service (fraud detection, classification)

### Advanced Infrastructure Services (2+)
Sophisticated services that enable advanced blockchain capabilities:
- Fair Ordering Service (transaction fairness, MEV protection)
- Future services based on ecosystem needs

## Creating a New Service

To create a new service, follow these comprehensive steps:

### 1. Project Setup

Create a new service project following the naming convention:

```bash
# For Core Infrastructure Services
dotnet new classlib -n NeoServiceLayer.Services.YourService -f net9.0

# For AI Services
dotnet new classlib -n NeoServiceLayer.AI.YourService -f net9.0

# For Advanced Infrastructure Services
dotnet new classlib -n NeoServiceLayer.Advanced.YourService -f net9.0
```

### 2. Add Dependencies

Add necessary project references:

```bash
dotnet add reference ../../../Core/NeoServiceLayer.Core
dotnet add reference ../../../Core/NeoServiceLayer.ServiceFramework
dotnet add reference ../../../Infrastructure/NeoServiceLayer.Infrastructure
```

### 3. Define Service Interface

Choose the appropriate base interface based on service requirements:

```csharp
// For basic services
public interface IYourService : IService
{
    Task<YourResult> PerformOperationAsync(YourRequest request);
}

// For enclave-enabled services
public interface IYourService : IEnclaveService, IBlockchainService
{
    Task<YourResult> SecureOperationAsync(YourRequest request, BlockchainType blockchainType);
}

// For AI services
public interface IYourService : IEnclaveService, IBlockchainService
{
    Task<PredictionResult> PredictAsync(PredictionRequest request, BlockchainType blockchainType);
    Task<string> RegisterModelAsync(ModelRegistration registration, BlockchainType blockchainType);
}
```

### 4. Implement Service Class

Choose the appropriate base class:

```csharp
// For enclave-enabled blockchain services (most common)
public class YourService : EnclaveBlockchainServiceBase, IYourService
{
    public YourService(ILogger<YourService> logger, IServiceConfiguration configuration)
        : base("YourService", "Description of your service", "1.0.0", logger, configuration)
    {
        // Set supported blockchains
        SupportedBlockchains = new[] { BlockchainType.NeoN3, BlockchainType.NeoX };
    }

    public async Task<YourResult> SecureOperationAsync(YourRequest request, BlockchainType blockchainType)
    {
        // Validate blockchain support
        if (!SupportsBlockchain(blockchainType))
            throw new NotSupportedException($"Blockchain {blockchainType} not supported");

        // Perform secure operation in enclave
        var result = await ExecuteInEnclaveAsync(async () =>
        {
            // Enclave-protected logic here
            return ProcessRequest(request);
        });

        return result;
    }

    protected override async Task<bool> OnInitializeAsync()
    {
        // Initialize service-specific resources
        Logger.LogInformation("Initializing {ServiceName}", ServiceName);

        // Initialize enclave if required
        if (!await InitializeEnclaveAsync())
        {
            Logger.LogError("Failed to initialize enclave for {ServiceName}", ServiceName);
            return false;
        }

        return true;
    }

    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting {ServiceName}", ServiceName);
        // Start service operations
        return true;
    }

    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping {ServiceName}", ServiceName);
        // Clean up resources
        return true;
    }

    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check service health
        var health = IsEnclaveInitialized ? ServiceHealth.Healthy : ServiceHealth.Unhealthy;
        return Task.FromResult(health);
    }
}
```

### 5. Register Service

Register the service with dependency injection:

```csharp
// In Program.cs or Startup.cs
services.AddNeoService<IYourService, YourService>();

// For services with specific configuration
services.AddNeoService<IYourService, YourService>(options =>
{
    options.EnableEnclave = true;
    options.SupportedBlockchains = new[] { BlockchainType.NeoN3, BlockchainType.NeoX };
});
```

### 6. Create Tests

Create comprehensive tests for your service:

```csharp
[TestClass]
public class YourServiceTests
{
    [TestMethod]
    public async Task SecureOperationAsync_ValidRequest_ReturnsExpectedResult()
    {
        // Arrange
        var service = CreateService();
        var request = new YourRequest { /* test data */ };

        // Act
        var result = await service.SecureOperationAsync(request, BlockchainType.NeoN3);

        // Assert
        Assert.IsNotNull(result);
        // Additional assertions
    }

    private IYourService CreateService()
    {
        var logger = Mock.Of<ILogger<YourService>>();
        var configuration = Mock.Of<IServiceConfiguration>();
        return new YourService(logger, configuration);
    }
}
```

### 7. Create Documentation

Create service documentation following the template:

```markdown
# Neo Service Layer - Your Service

## Overview
Brief description of the service and its purpose.

## Features
- List of key features
- Supported capabilities

## API Reference
Detailed API documentation with examples.

## Use Cases
Real-world use cases and examples.

## Security Considerations
Security aspects and best practices.
```

## Service Lifecycle

Services in the Neo Service Layer follow a standard lifecycle:

1. **Initialization**: The service is initialized, setting up any required resources.
2. **Starting**: The service is started, beginning its normal operation.
3. **Running**: The service is running, performing its normal operations.
4. **Stopping**: The service is stopping, cleaning up any resources.
5. **Stopped**: The service is stopped, no longer performing operations.

The service framework provides methods to manage this lifecycle:

- **InitializeAsync**: Initializes the service.
- **StartAsync**: Starts the service.
- **StopAsync**: Stops the service.
- **GetHealthAsync**: Gets the health status of the service.

## Service Health

Services report their health status using the ServiceHealth enum:

- **Healthy**: The service is healthy and functioning normally.
- **Degraded**: The service is degraded but still functioning.
- **Unhealthy**: The service is unhealthy and not functioning properly.
- **NotRunning**: The service is not running.

## Enclave Integration

Services that require enclave operations can implement the IEnclaveService interface and inherit from the EnclaveServiceBase class. This provides additional methods for enclave initialization:

- **InitializeEnclaveAsync**: Initializes the enclave.
- **IsEnclaveInitialized**: Gets a value indicating whether the enclave is initialized.

## Blockchain Integration

Services that support blockchain operations can implement the IBlockchainService interface and inherit from the BlockchainServiceBase class. This provides methods to check blockchain type support:

- **SupportedBlockchains**: Gets the supported blockchain types.
- **SupportsBlockchain**: Checks if a specific blockchain type is supported.

## Service Patterns and Best Practices

### Enclave Usage Patterns

#### When to Use Enclaves
- **Cryptographic Operations**: Key generation, signing, encryption
- **Sensitive Data Processing**: User secrets, private computations
- **Verification Operations**: Proof generation, data integrity checks
- **AI Model Execution**: Protecting proprietary models and data

#### Enclave Best Practices
- Keep enclave code minimal and focused
- Validate all inputs before enclave execution
- Use secure communication channels
- Implement proper error handling and logging

### Blockchain Integration Patterns

#### Multi-Blockchain Support
```csharp
public class MultiBlockchainService : EnclaveBlockchainServiceBase
{
    public MultiBlockchainService() : base(...)
    {
        SupportedBlockchains = new[] { BlockchainType.NeoN3, BlockchainType.NeoX };
    }

    public async Task<Result> ProcessAsync(Request request, BlockchainType blockchainType)
    {
        return blockchainType switch
        {
            BlockchainType.NeoN3 => await ProcessNeoN3Async(request),
            BlockchainType.NeoX => await ProcessNeoXAsync(request),
            _ => throw new NotSupportedException($"Blockchain {blockchainType} not supported")
        };
    }
}
```

### Error Handling and Resilience

#### Service-Level Error Handling
```csharp
public async Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<T>> operation)
{
    try
    {
        var result = await operation();
        return ServiceResult<T>.Success(result);
    }
    catch (ValidationException ex)
    {
        Logger.LogWarning(ex, "Validation error in {ServiceName}", ServiceName);
        return ServiceResult<T>.Failure("Invalid input parameters");
    }
    catch (EnclaveException ex)
    {
        Logger.LogError(ex, "Enclave error in {ServiceName}", ServiceName);
        return ServiceResult<T>.Failure("Secure operation failed");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Unexpected error in {ServiceName}", ServiceName);
        return ServiceResult<T>.Failure("Internal service error");
    }
}
```

### Performance Optimization

#### Caching Strategies
- Use distributed caching for frequently accessed data
- Implement cache invalidation strategies
- Cache enclave initialization results when possible

#### Async/Await Best Practices
- Use ConfigureAwait(false) for library code
- Avoid blocking async operations
- Use cancellation tokens for long-running operations

### Security Considerations

#### Input Validation
```csharp
public async Task<Result> ValidatedOperationAsync(Request request)
{
    // Validate inputs before processing
    if (!IsValidRequest(request))
        throw new ValidationException("Invalid request parameters");

    // Sanitize inputs
    var sanitizedRequest = SanitizeRequest(request);

    // Process in enclave
    return await ExecuteInEnclaveAsync(() => ProcessRequest(sanitizedRequest));
}
```

#### Secure Logging
- Never log sensitive data
- Use structured logging with appropriate log levels
- Implement log sanitization for enclave operations

## Testing Strategies

### Unit Testing
- Test service logic independently of enclaves
- Use mocking for external dependencies
- Test error handling and edge cases

### Integration Testing
- Test with SGX simulation mode
- Verify blockchain integration
- Test service lifecycle management

### Performance Testing
- Benchmark enclave operations
- Test under load conditions
- Verify memory and resource usage

## Monitoring and Observability

### Health Checks
Implement comprehensive health checks:
```csharp
protected override async Task<ServiceHealth> OnGetHealthAsync()
{
    var checks = new List<HealthCheck>
    {
        await CheckEnclaveHealth(),
        await CheckBlockchainConnectivity(),
        await CheckDependencyHealth()
    };

    return checks.All(c => c.IsHealthy) ? ServiceHealth.Healthy : ServiceHealth.Degraded;
}
```

### Metrics and Telemetry
- Track service performance metrics
- Monitor enclave operation success rates
- Implement distributed tracing

## Conclusion

The Neo Service Layer Service Framework provides a comprehensive, production-ready foundation for building secure, scalable blockchain infrastructure services. By following the established patterns and best practices, developers can create services that:

- ✅ **Integrate seamlessly** with Intel SGX + Occlum LibOS enclaves
- ✅ **Support multiple blockchains** (Neo N3 and NeoX)
- ✅ **Follow consistent patterns** across all service categories
- ✅ **Maintain high security standards** for sensitive operations
- ✅ **Provide reliable performance** under production loads
- ✅ **Enable easy testing and monitoring** throughout the development lifecycle

The framework's modular design ensures that new services can be added efficiently while maintaining the platform's security, reliability, and performance characteristics.
