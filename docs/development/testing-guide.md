# Neo Service Layer - Testing Guide

## Overview

This guide provides instructions for testing the Neo Service Layer. It covers unit testing, integration testing, end-to-end testing, and performance testing, as well as best practices for writing effective tests.

## Testing Approach

The Neo Service Layer uses a comprehensive testing approach that includes:

- **Unit Testing**: Testing individual components in isolation.
- **Integration Testing**: Testing components working together.
- **End-to-End Testing**: Testing the entire system.
- **Performance Testing**: Testing the performance of the system.
- **Security Testing**: Testing the security of the system.

## Testing Frameworks

The Neo Service Layer uses the following testing frameworks:

- **xUnit**: For unit and integration tests.
- **Moq**: For mocking dependencies.
- **FluentAssertions**: For assertions.
- **BenchmarkDotNet**: For performance testing.
- **NBomber**: For load testing.

## Unit Testing

Unit tests test individual components in isolation, with dependencies mocked or stubbed.

### Writing Unit Tests

1. **Create a Test Class**: Create a test class for the component you want to test.
2. **Mock Dependencies**: Mock the dependencies of the component.
3. **Create the Component**: Create an instance of the component with the mocked dependencies.
4. **Call Methods**: Call the methods you want to test.
5. **Assert Results**: Assert that the results are as expected.

### Example Unit Test

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Host.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.Randomness.Tests
{
    public class RandomnessServiceTests
    {
        private readonly Mock<IEnclaveManager> _enclaveManagerMock;
        private readonly Mock<IServiceConfiguration> _configurationMock;
        private readonly Mock<ILogger<RandomnessService>> _loggerMock;
        private readonly RandomnessService _service;

        public RandomnessServiceTests()
        {
            _enclaveManagerMock = new Mock<IEnclaveManager>();
            _configurationMock = new Mock<IServiceConfiguration>();
            _loggerMock = new Mock<ILogger<RandomnessService>>();

            _configurationMock
                .Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            _enclaveManagerMock
                .Setup(e => e.InitializeAsync())
                .ReturnsAsync(true);

            _enclaveManagerMock
                .Setup(e => e.ExecuteJavaScriptAsync(It.IsAny<string>()))
                .ReturnsAsync("42");

            _service = new RandomnessService(_enclaveManagerMock.Object, _configurationMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task InitializeAsync_ShouldInitializeEnclave()
        {
            // Act
            await _service.InitializeAsync();

            // Assert
            _enclaveManagerMock.Verify(e => e.InitializeAsync(), Times.Once);
            _service.IsEnclaveInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task GenerateRandomNumberAsync_ShouldReturnRandomNumber()
        {
            // Arrange
            await _service.InitializeAsync();
            await _service.StartAsync();

            // Act
            var result = await _service.GenerateRandomNumberAsync(1, 100, BlockchainType.NeoN3);

            // Assert
            result.Should().Be(42);
            _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
```

### Running Unit Tests

```bash
# Run all unit tests
dotnet test --filter "Category=Unit"

# Run unit tests for a specific service
dotnet test tests/Services/NeoServiceLayer.Services.Randomness.Tests
```

## Integration Testing

Integration tests test components working together, with real dependencies or realistic mocks.

### Writing Integration Tests

1. **Create a Test Class**: Create a test class for the components you want to test.
2. **Set Up Dependencies**: Set up the dependencies of the components.
3. **Create the Components**: Create instances of the components with the dependencies.
4. **Call Methods**: Call the methods you want to test.
5. **Assert Results**: Assert that the results are as expected.

### Example Integration Test

```csharp
using Xunit;
using FluentAssertions;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Host.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NeoServiceLayer.Services.Randomness.Tests
{
    public class RandomnessServiceIntegrationTests
    {
        private readonly ServiceProvider _serviceProvider;

        public RandomnessServiceIntegrationTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IServiceConfiguration, ServiceConfiguration>();
            services.AddSingleton<IEnclaveManager, EnclaveManager>();
            services.AddSingleton<IRandomnessService, RandomnessService>();
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task GenerateRandomNumberAsync_ShouldReturnRandomNumber()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<IRandomnessService>();
            await service.InitializeAsync();
            await service.StartAsync();

            // Act
            var result = await service.GenerateRandomNumberAsync(1, 100, BlockchainType.NeoN3);

            // Assert
            result.Should().BeGreaterOrEqualTo(1);
            result.Should().BeLessOrEqualTo(100);
        }
    }
}
```

### Running Integration Tests

```bash
# Run all integration tests
dotnet test --filter "Category=Integration"

# Run integration tests for a specific service
dotnet test tests/Services/NeoServiceLayer.Services.Randomness.Tests --filter "Category=Integration"
```

## End-to-End Testing

End-to-end tests test the entire system, from the API to the services to the blockchain.

### Writing End-to-End Tests

1. **Create a Test Class**: Create a test class for the end-to-end scenario you want to test.
2. **Set Up the System**: Set up the entire system, including the API, services, and blockchain.
3. **Call API Endpoints**: Call the API endpoints you want to test.
4. **Assert Results**: Assert that the results are as expected.

### Example End-to-End Test

```csharp
using Xunit;
using FluentAssertions;
using NeoServiceLayer.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace NeoServiceLayer.Tests.EndToEnd
{
    public class RandomnessServiceEndToEndTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public RandomnessServiceEndToEndTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GenerateRandomNumber_ShouldReturnRandomNumber()
        {
            // Arrange
            var request = new
            {
                blockchain = "neo-n3",
                min = 1,
                max = 100
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/randomness/generate", content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<RandomnessResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Assert
            responseObject.Success.Should().BeTrue();
            responseObject.Data.Value.Should().BeGreaterOrEqualTo(1);
            responseObject.Data.Value.Should().BeLessOrEqualTo(100);
        }

        private class RandomnessResponse
        {
            public bool Success { get; set; }
            public RandomnessData Data { get; set; }
            public object Error { get; set; }
            public object Meta { get; set; }
        }

        private class RandomnessData
        {
            public int Value { get; set; }
            public string Proof { get; set; }
            public string Timestamp { get; set; }
        }
    }
}
```

### Running End-to-End Tests

```bash
# Run all end-to-end tests
dotnet test --filter "Category=EndToEnd"

# Run end-to-end tests for a specific service
dotnet test tests/EndToEnd/NeoServiceLayer.Tests.EndToEnd --filter "FullyQualifiedName~RandomnessService"
```

## Performance Testing

Performance tests test the performance of the system, including throughput, latency, and resource usage.

### Writing Performance Tests

1. **Create a Benchmark Class**: Create a benchmark class for the component you want to test.
2. **Set Up the Component**: Set up the component with realistic dependencies.
3. **Create Benchmark Methods**: Create methods that benchmark the component.
4. **Run the Benchmark**: Run the benchmark and analyze the results.

### Example Performance Test

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Host.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tests.Performance
{
    [MemoryDiagnoser]
    public class RandomnessServiceBenchmarks
    {
        private IRandomnessService _service;

        [GlobalSetup]
        public async Task Setup()
        {
            var enclaveManager = new EnclaveManager();
            var configuration = new ServiceConfiguration();
            var logger = new NullLogger<RandomnessService>();
            _service = new RandomnessService(enclaveManager, configuration, logger);
            await _service.InitializeAsync();
            await _service.StartAsync();
        }

        [Benchmark]
        public async Task GenerateRandomNumber()
        {
            await _service.GenerateRandomNumberAsync(1, 100, BlockchainType.NeoN3);
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            await _service.StopAsync();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<RandomnessServiceBenchmarks>();
        }
    }
}
```

### Running Performance Tests

```bash
# Run performance tests
dotnet run -c Release --project tests/Performance/NeoServiceLayer.Tests.Performance
```

## Security Testing

Security tests test the security of the system, including authentication, authorization, and data protection.

### Writing Security Tests

1. **Create a Test Class**: Create a test class for the security aspect you want to test.
2. **Set Up the System**: Set up the system with security features enabled.
3. **Attempt Security Breaches**: Attempt to breach the security of the system.
4. **Assert Results**: Assert that the security breaches are prevented.

### Example Security Test

```csharp
using Xunit;
using FluentAssertions;
using NeoServiceLayer.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace NeoServiceLayer.Tests.Security
{
    public class ApiSecurityTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public ApiSecurityTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task AccessApiWithoutApiKey_ShouldReturnUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/randomness/generate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AccessApiWithInvalidApiKey_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-API-Key", "invalid-api-key");

            // Act
            var response = await _client.GetAsync("/api/v1/randomness/generate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
```

### Running Security Tests

```bash
# Run security tests
dotnet test --filter "Category=Security"
```

## Test Coverage

The Neo Service Layer aims for high test coverage, with a target of at least 80% code coverage.

### Measuring Test Coverage

```bash
# Install coverage tools
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Generate coverage report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html
```

## Best Practices

### General Best Practices

- **Test One Thing**: Each test should test one thing.
- **Arrange, Act, Assert**: Structure tests with Arrange, Act, Assert sections.
- **Descriptive Names**: Use descriptive names for tests.
- **Independent Tests**: Tests should be independent of each other.
- **Fast Tests**: Tests should be fast to run.
- **Deterministic Tests**: Tests should be deterministic.

### Unit Testing Best Practices

- **Mock Dependencies**: Mock dependencies to isolate the component being tested.
- **Test Edge Cases**: Test edge cases and error conditions.
- **Test Public API**: Test the public API of the component.
- **Avoid Testing Implementation Details**: Avoid testing implementation details.

### Integration Testing Best Practices

- **Use Realistic Dependencies**: Use realistic dependencies or realistic mocks.
- **Test Interactions**: Test interactions between components.
- **Test Configuration**: Test different configurations.

### End-to-End Testing Best Practices

- **Test Critical Paths**: Test critical paths through the system.
- **Test User Scenarios**: Test realistic user scenarios.
- **Test Error Handling**: Test error handling and recovery.

## References

- [Neo Service Layer Architecture](../architecture/README.md)
- [Neo Service Layer API](../api/README.md)
- [Neo Service Layer Services](../services/README.md)
- [Neo Service Layer Development Guide](README.md)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/guides/getting-started.html)
