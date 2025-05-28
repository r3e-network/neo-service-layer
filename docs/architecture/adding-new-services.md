# Neo Service Layer - Adding New Services

This guide provides step-by-step instructions for adding new services to the Neo Service Layer.

## Prerequisites

- .NET 9.0 SDK
- Visual Studio 2025 or later (optional)
- Git

## Step 1: Create a New Service Project

First, create a new project for your service:

```bash
dotnet new classlib -n NeoServiceLayer.Services.YourService -o src/Services/NeoServiceLayer.Services.YourService -f net9.0
```

Add the project to the solution:

```bash
dotnet sln add src/Services/NeoServiceLayer.Services.YourService/NeoServiceLayer.Services.YourService.csproj
```

Add the necessary references:

```bash
dotnet add src/Services/NeoServiceLayer.Services.YourService/NeoServiceLayer.Services.YourService.csproj reference src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj
```

## Step 2: Define the Service Interface

Create a new file for your service interface:

```csharp
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.YourService;

/// <summary>
/// Interface for the YourService service.
/// </summary>
public interface IYourService : IService
{
    // Define service-specific methods here
    Task<string> DoSomethingAsync(string input);
}
```

For services that require enclave operations, implement the IEnclaveService interface:

```csharp
public interface IYourService : IEnclaveService
{
    // ...
}
```

For services that support blockchain operations, implement the IBlockchainService interface:

```csharp
public interface IYourService : IBlockchainService
{
    // ...
}
```

For services that require both enclave and blockchain operations, implement both interfaces:

```csharp
public interface IYourService : IEnclaveService, IBlockchainService
{
    // ...
}
```

## Step 3: Implement the Service

Create a new file for your service implementation:

```csharp
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;

namespace NeoServiceLayer.Services.YourService;

/// <summary>
/// Implementation of the YourService service.
/// </summary>
public class YourService : ServiceBase, IYourService
{
    private readonly IServiceConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="YourService"/> class.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    public YourService(IServiceConfiguration configuration, ILogger<YourService> logger)
        : base("YourService", "Description of your service", "1.0.0", logger)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public async Task<string> DoSomethingAsync(string input)
    {
        // Implement your service-specific logic here
        await Task.Delay(100); // Simulate some work
        return $"Processed: {input}";
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        // Initialize your service here
        await Task.Delay(100); // Simulate some work
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        // Start your service here
        await Task.Delay(100); // Simulate some work
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        // Stop your service here
        await Task.Delay(100); // Simulate some work
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check the health of your service here
        await Task.Delay(100); // Simulate some work
        return ServiceHealth.Healthy;
    }
}
```

For services that require enclave operations, inherit from EnclaveServiceBase:

```csharp
public class YourService : EnclaveServiceBase, IYourService
{
    // ...

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        // Initialize the enclave here
        await Task.Delay(100); // Simulate some work
        return true;
    }
}
```

For services that support blockchain operations, inherit from BlockchainServiceBase:

```csharp
public class YourService : BlockchainServiceBase, IYourService
{
    public YourService(IServiceConfiguration configuration, ILogger<YourService> logger)
        : base("YourService", "Description of your service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        // ...
    }
}
```

For services that require both enclave and blockchain operations, inherit from EnclaveBlockchainServiceBase:

```csharp
public class YourService : EnclaveBlockchainServiceBase, IYourService
{
    public YourService(IServiceConfiguration configuration, ILogger<YourService> logger)
        : base("YourService", "Description of your service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        // ...
    }
}
```

## Step 4: Create Tests for the Service

Create a new test project for your service:

```bash
dotnet new xunit -n NeoServiceLayer.Services.YourService.Tests -o tests/Services/NeoServiceLayer.Services.YourService.Tests -f net9.0
```

Add the test project to the solution:

```bash
dotnet sln add tests/Services/NeoServiceLayer.Services.YourService.Tests/NeoServiceLayer.Services.YourService.Tests.csproj
```

Add the necessary references:

```bash
dotnet add tests/Services/NeoServiceLayer.Services.YourService.Tests/NeoServiceLayer.Services.YourService.Tests.csproj reference src/Services/NeoServiceLayer.Services.YourService/NeoServiceLayer.Services.YourService.csproj src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj
```

Add the Moq package for mocking:

```bash
dotnet add tests/Services/NeoServiceLayer.Services.YourService.Tests/NeoServiceLayer.Services.YourService.Tests.csproj package Moq
```

Implement tests for your service:

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;

namespace NeoServiceLayer.Services.YourService.Tests;

public class YourServiceTests
{
    private readonly Mock<ILogger<YourService>> _loggerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly YourService _service;

    public YourServiceTests()
    {
        _loggerMock = new Mock<ILogger<YourService>>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _service = new YourService(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _service.InitializeAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var result = await _service.StartAsync();

        // Assert
        Assert.True(result);
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.StopAsync();

        // Assert
        Assert.True(result);
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthy()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetHealthAsync();

        // Assert
        Assert.Equal(ServiceHealth.Healthy, result);
    }

    [Fact]
    public async Task DoSomethingAsync_ShouldReturnProcessedInput()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.DoSomethingAsync("test");

        // Assert
        Assert.Equal("Processed: test", result);
    }
}
```

## Step 5: Create Documentation for the Service

Create a new documentation file for your service:

```markdown
# Neo Service Layer - YourService

## Overview

YourService provides [description of your service].

## Features

- Feature 1
- Feature 2
- Feature 3

## Architecture

[Description of the service architecture]

## API Reference

### IYourService Interface

```csharp
public interface IYourService : IService
{
    Task<string> DoSomethingAsync(string input);
}
```

#### Methods

- **DoSomethingAsync**: [Description of the method]
  - Parameters:
    - `input`: [Description of the parameter]
  - Returns: [Description of the return value]

## Usage Examples

[Examples of how to use the service]

## Security Considerations

[Description of security considerations]

## Conclusion

[Conclusion about the service]
```

## Step 6: Register the Service

In your application's startup code, register the service with the dependency injection system:

```csharp
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.YourService;

// ...

services.AddNeoServiceFramework();
services.AddNeoService<IYourService, YourService>();

// ...

serviceProvider.RegisterAllNeoServices();
```

## Conclusion

By following these steps, you can easily add new services to the Neo Service Layer. The service framework provides a standardized way to create, register, and manage services, ensuring consistency and reliability across the platform.
