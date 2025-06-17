# Neo Service Layer - Testing Strategy

## Overview

This document outlines the comprehensive testing strategy for the Neo Service Layer, including unit tests, integration tests, performance tests, and testing infrastructure.

## Testing Architecture

### Test Categories

1. **Unit Tests** - Test individual components in isolation
2. **Integration Tests** - Test component interactions and API endpoints
3. **Performance Tests** - Test system performance and scalability
4. **Security Tests** - Test authentication, authorization, and security features
5. **End-to-End Tests** - Test complete user workflows

### Test Structure

```
tests/
├── Api/                          # API layer tests
│   └── NeoServiceLayer.Api.Tests/
├── Core/                         # Core infrastructure tests
│   └── NeoServiceLayer.ServiceFramework.Tests/
├── Services/                     # Service layer tests
│   ├── NeoServiceLayer.Services.KeyManagement.Tests/
│   ├── NeoServiceLayer.Services.Oracle.Tests/
│   └── ...
├── AI/                          # AI service tests
│   ├── NeoServiceLayer.AI.PatternRecognition.Tests/
│   └── NeoServiceLayer.AI.Prediction.Tests/
├── Blockchain/                  # Blockchain integration tests
│   ├── NeoServiceLayer.Neo.N3.Tests/
│   └── NeoServiceLayer.Neo.X.Tests/
├── Integration/                 # Integration tests
│   └── NeoServiceLayer.Integration.Tests/
└── TestInfrastructure/         # Shared test utilities
```

## Testing Frameworks and Tools

### Primary Testing Stack

- **xUnit** - Primary testing framework
- **Moq** - Mocking framework for dependencies
- **FluentAssertions** - Assertion library for readable tests
- **AutoFixture** - Test data generation
- **Bogus** - Fake data generation
- **Testcontainers** - Integration testing with containers

### Code Coverage

- **Coverlet** - Code coverage collection
- **ReportGenerator** - Coverage report generation
- **Target Coverage**: 80% minimum, 90% preferred

### Performance Testing

- **NBomber** - Load testing framework
- **BenchmarkDotNet** - Micro-benchmarking

## Testing Patterns

### Unit Test Patterns

#### Arrange-Act-Assert (AAA)

```csharp
[Fact]
public async Task GenerateKey_WithValidRequest_ShouldReturnKeyMetadata()
{
    // Arrange
    var request = new GenerateKeyRequest
    {
        KeyId = "test-key",
        KeyType = "Secp256k1"
    };
    
    _mockService.Setup(s => s.GenerateKeyAsync(It.IsAny<GenerateKeyRequest>()))
               .ReturnsAsync(new KeyMetadata { KeyId = "test-key" });

    // Act
    var result = await _controller.GenerateKey(request, "NeoN3");

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<ApiResponse<KeyMetadata>>(okResult.Value);
    Assert.True(response.Success);
    Assert.Equal("test-key", response.Data.KeyId);
}
```

#### Test Data Builders

```csharp
public class KeyMetadataBuilder
{
    private KeyMetadata _keyMetadata = new KeyMetadata();

    public KeyMetadataBuilder WithKeyId(string keyId)
    {
        _keyMetadata.KeyId = keyId;
        return this;
    }

    public KeyMetadataBuilder WithKeyType(string keyType)
    {
        _keyMetadata.KeyType = keyType;
        return this;
    }

    public KeyMetadata Build() => _keyMetadata;
}
```

### Integration Test Patterns

#### API Integration Tests

```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetKeys_ShouldReturnKeyList()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await _client.GetAsync("/api/v1/keymanagement/keys/NeoN3");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<KeyMetadata>>(content);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}
```

### Mock Patterns

#### Service Mocking

```csharp
public class ServiceTestBase
{
    protected readonly Mock<ILogger<TService>> LoggerMock;
    protected readonly Mock<IConfiguration> ConfigurationMock;
    
    protected ServiceTestBase()
    {
        LoggerMock = new Mock<ILogger<TService>>();
        ConfigurationMock = new Mock<IConfiguration>();
        
        // Setup common mock behaviors
        ConfigurationMock.Setup(c => c["SomeKey"]).Returns("SomeValue");
    }
}
```

## Test Data Management

### Test Data Strategies

1. **In-Memory Data** - For unit tests
2. **Test Containers** - For integration tests requiring databases
3. **Mock Data** - For external service dependencies
4. **Fixture Data** - For consistent test scenarios

### Test Data Examples

```csharp
public static class TestData
{
    public static KeyMetadata CreateValidKeyMetadata() => new KeyMetadata
    {
        KeyId = "test-key-" + Guid.NewGuid().ToString("N")[..8],
        KeyType = "Secp256k1",
        KeyUsage = "Sign,Verify",
        Exportable = false,
        CreatedAt = DateTime.UtcNow,
        PublicKeyHex = "0x" + new string('a', 66)
    };

    public static FraudDetectionRequest CreateFraudDetectionRequest() => new FraudDetectionRequest
    {
        TransactionData = new Dictionary<string, object>
        {
            { "amount", 1000 },
            { "timestamp", DateTime.UtcNow },
            { "fromAddress", "test-address-1" },
            { "toAddress", "test-address-2" }
        },
        Sensitivity = DetectionSensitivity.Standard
    };
}
```

## Test Configuration

### Test Settings

```json
{
  "Blockchain": {
    "NeoN3": {
      "RpcUrl": "http://localhost:20332",
      "NetworkMagic": 860833102
    },
    "NeoX": {
      "RpcUrl": "http://localhost:8545",
      "ChainId": 12227332
    }
  },
  "KeyManagement": {
    "MaxKeyCount": 100
  },
  "AI": {
    "ModelCacheSize": 5,
    "EnableGpuAcceleration": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning"
    }
  }
}
```

### Environment-Specific Configuration

- **Development** - Full logging, mock services
- **Testing** - Minimal logging, in-memory databases
- **CI/CD** - Optimized for speed, containerized dependencies

## Test Execution

### Running Tests

#### All Tests
```bash
./run-tests-comprehensive.ps1
```

#### Unit Tests Only
```bash
./run-tests-comprehensive.ps1 -Unit
```

#### Integration Tests Only
```bash
./run-tests-comprehensive.ps1 -Integration
```

#### With Coverage
```bash
./run-tests-comprehensive.ps1 -Coverage
```

#### Specific Filter
```bash
./run-tests-comprehensive.ps1 -Filter "Category=KeyManagement"
```

### CI/CD Integration

#### GitHub Actions Example

```yaml
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Run Tests
      run: ./run-tests-comprehensive.ps1 -Coverage
      shell: pwsh
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        file: TestResults/coverage.cobertura.xml
```

## Test Quality Standards

### Code Coverage Targets

- **Unit Tests**: 90% minimum
- **Integration Tests**: 70% minimum
- **Overall**: 80% minimum

### Test Quality Metrics

1. **Test Reliability** - Tests should be deterministic
2. **Test Speed** - Unit tests < 100ms, Integration tests < 5s
3. **Test Maintainability** - Clear, readable, and well-documented
4. **Test Independence** - Tests should not depend on each other

### Test Naming Conventions

```csharp
// Pattern: MethodName_Scenario_ExpectedResult
[Fact]
public async Task GenerateKey_WithValidRequest_ShouldReturnKeyMetadata()

[Fact]
public async Task GenerateKey_WithInvalidKeyType_ShouldThrowArgumentException()

[Fact]
public async Task GenerateKey_WhenServiceUnavailable_ShouldReturnServiceError()
```

## Performance Testing

### Load Testing Scenarios

1. **Key Generation** - 100 concurrent key generations
2. **Fraud Detection** - 1000 fraud detection requests/minute
3. **Prediction API** - 500 prediction requests/minute
4. **Pattern Recognition** - 200 pattern analysis requests/minute

### Performance Benchmarks

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class KeyManagementBenchmarks
{
    [Benchmark]
    public async Task GenerateKey_Secp256k1()
    {
        // Benchmark key generation performance
    }

    [Benchmark]
    public async Task SignData_ECDSA()
    {
        // Benchmark signing performance
    }
}
```

## Security Testing

### Security Test Categories

1. **Authentication Tests** - JWT token validation
2. **Authorization Tests** - Role-based access control
3. **Input Validation Tests** - SQL injection, XSS prevention
4. **Rate Limiting Tests** - API rate limiting enforcement
5. **Encryption Tests** - Data encryption/decryption

### Security Test Examples

```csharp
[Fact]
public async Task Api_WithoutAuthentication_ShouldReturnUnauthorized()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/v1/keymanagement/keys/NeoN3");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task Api_WithInvalidRole_ShouldReturnForbidden()
{
    var client = CreateClientWithRole("User");
    var response = await client.DeleteAsync("/api/v1/keymanagement/keys/test-key/NeoN3");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

## Troubleshooting

### Common Test Issues

1. **Flaky Tests** - Use deterministic test data and proper cleanup
2. **Slow Tests** - Optimize database operations and use mocking
3. **Test Dependencies** - Ensure proper test isolation
4. **Environment Issues** - Use consistent test configuration

### Debugging Tests

```csharp
[Fact]
public async Task DebugTest()
{
    // Use ITestOutputHelper for debugging
    _output.WriteLine($"Test data: {JsonSerializer.Serialize(testData)}");
    
    // Use debugger breakpoints
    System.Diagnostics.Debugger.Break();
}
```

## Best Practices

### Do's

- ✅ Write tests first (TDD approach)
- ✅ Use descriptive test names
- ✅ Keep tests simple and focused
- ✅ Use proper mocking for external dependencies
- ✅ Clean up test data after each test
- ✅ Use test categories for organization
- ✅ Maintain high code coverage

### Don'ts

- ❌ Don't test implementation details
- ❌ Don't write tests that depend on external services
- ❌ Don't ignore failing tests
- ❌ Don't write overly complex test setups
- ❌ Don't test multiple concerns in one test
- ❌ Don't hardcode test data that could change

## Continuous Improvement

### Test Metrics Tracking

- Test execution time trends
- Code coverage trends
- Test failure rates
- Test maintenance effort

### Regular Test Reviews

- Monthly test suite review
- Quarterly performance benchmark review
- Annual testing strategy review

---

This testing strategy ensures comprehensive coverage of the Neo Service Layer while maintaining high quality, performance, and reliability standards. 