# Neo Service Layer Testing Guide

[![Test Coverage](https://img.shields.io/badge/coverage-86%25-brightgreen)](https://github.com/r3e-network/neo-service-layer)
[![Tests](https://img.shields.io/badge/tests-passing-green)](https://github.com/r3e-network/neo-service-layer/actions)
[![Test Framework](https://img.shields.io/badge/framework-xUnit-blue)](https://xunit.net/)
[![Performance Tests](https://img.shields.io/badge/performance-NBomber-orange)](https://nbomber.com/)

> **🧪 Comprehensive Testing Strategy** - Complete guide for testing the Neo Service Layer microservices platform

## 🚀 Quick Start

### **Instant Test Commands**

```bash
# Run all unit tests (fastest, recommended for development)
dotnet test --filter "Category!=Integration&Category!=Performance"

# Run integration tests (requires infrastructure)
docker-compose up -d
dotnet test --filter "Category=Integration"

# Run specific service tests
dotnet test --filter "FullyQualifiedName~StorageService"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### **Using Test Scripts**

```bash
# Linux/macOS - Unit tests with coverage
./scripts/testing/run-unit-tests.sh

# Windows - PowerShell with coverage report
./scripts/testing/run-all-tests.ps1 -Coverage

# Run specific test category
./scripts/testing/run-tests.sh --category Unit --coverage
```

## 📋 Test Categories & Organization

### **Test Category Matrix**

| Category | Purpose | Infrastructure | Execution Time | CI/CD |
|----------|---------|----------------|----------------|-------|
| **Unit** | Business logic validation | None | < 1ms per test | ✅ Always |
| **Integration** | Service interaction | Docker required | 10-100ms | ✅ PR & Main |
| **Performance** | Load & stress testing | Full stack | 1-5 minutes | ❌ Nightly |
| **Security** | Security validation | Mock services | < 10ms | ✅ Always |
| **E2E** | End-to-end workflows | Full deployment | 30s-2min | ✅ Main only |

### **Test Project Structure**

```
tests/
├── Unit/                                    # Fast, isolated tests
│   ├── Core/
│   │   ├── NeoServiceLayer.Core.Tests/
│   │   └── NeoServiceLayer.Shared.Tests/
│   ├── Services/
│   │   ├── Storage.Tests/
│   │   ├── KeyManagement.Tests/
│   │   └── Notification.Tests/
│   └── Infrastructure/
│       └── ServiceFramework.Tests/
├── Integration/                             # Service interaction tests
│   ├── NeoServiceLayer.Integration.Tests/
│   │   ├── Microservices/
│   │   ├── ServiceDiscovery/
│   │   └── Database/
│   └── NeoServiceLayer.E2E.Tests/
├── Performance/                             # Load and stress tests
│   └── NeoServiceLayer.Performance.Tests/
│       ├── LoadTests/
│       ├── StressTests/
│       └── Benchmarks/
└── Shared/                                  # Test utilities
    └── NeoServiceLayer.TestUtilities/
        ├── Fixtures/
        ├── Builders/
        └── Extensions/
```

## 🧪 Writing Tests

### **Unit Test Example**

```csharp
[Fact]
[Category("Unit")]
public async Task StorageService_StoreDocument_ShouldEncryptData()
{
    // Arrange
    var mockEncryption = new Mock<IEncryptionService>();
    var mockRepository = new Mock<IDocumentRepository>();
    var service = new StorageService(mockEncryption.Object, mockRepository.Object);
    
    var document = new DocumentBuilder()
        .WithContent("sensitive data")
        .Build();
    
    mockEncryption
        .Setup(x => x.EncryptAsync(It.IsAny<byte[]>()))
        .ReturnsAsync(new byte[] { 1, 2, 3 });
    
    // Act
    var result = await service.StoreDocumentAsync(document);
    
    // Assert
    result.Should().NotBeNull();
    result.IsEncrypted.Should().BeTrue();
    mockEncryption.Verify(x => x.EncryptAsync(It.IsAny<byte[]>()), Times.Once);
}
```

### **Integration Test Example**

```csharp
[Fact]
[Category("Integration")]
public async Task ApiGateway_HealthCheck_ShouldReturnHealthyStatus()
{
    // Arrange
    await using var factory = new CustomWebApplicationFactory();
    var client = factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/health");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    var health = JsonSerializer.Deserialize<HealthCheckResponse>(content);
    health.Status.Should().Be("Healthy");
}
```

### **Performance Test Example**

```csharp
[Fact]
[Category("Performance")]
public void StorageService_ConcurrentWrites_ShouldHandleLoad()
{
    var scenario = Scenario.Create("concurrent_writes", async context =>
    {
        var request = Http.CreateRequest("POST", "http://localhost:8081/api/v1/storage/documents")
            .WithHeader("Authorization", "Bearer " + context.Data["token"])
            .WithBody(new { content = "test data" });
            
        var response = await Http.Send(request);
        
        return response.IsOk ? Response.Ok() : Response.Fail();
    })
    .WithLoadSimulations(
        Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30)),
        Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30))
    );
    
    NBomberRunner
        .RegisterScenarios(scenario)
        .Run();
}

## 📊 Test Coverage & Reporting

### **Coverage Standards**

| Component Type | Required Coverage | Current Status |
|----------------|-------------------|----------------|
| **Core Services** | 90%+ | ✅ 92% |
| **API Controllers** | 85%+ | ✅ 87% |
| **Service Framework** | 95%+ | ✅ 96% |
| **Utilities** | 95%+ | ✅ 96% |
| **Infrastructure** | 80%+ | ✅ 83% |
| **Overall** | 85%+ | ✅ 86% |

### **Coverage Reports**

```bash
# Generate HTML coverage report
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" \
                -targetdir:"./CoverageReport" \
                -reporttypes:"Html;Badges;TextSummary"

# View coverage report
open ./CoverageReport/index.html

# Generate coverage for specific project
dotnet test src/Services/Storage/Storage.Tests \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults/Storage
```

### **Test Result Locations**

```
TestResults/
├── coverage.cobertura.xml          # Raw coverage data
├── TestRun-{timestamp}.trx         # Test results
└── {TestProject}/
    └── coverage.cobertura.xml      # Project-specific coverage

CoverageReport/
├── index.html                      # Interactive HTML report
├── badge_combined.svg              # Coverage badge
└── Summary.txt                     # Text summary
```

## 🔧 CI/CD Integration

### **GitHub Actions Workflow**

Our CI/CD pipeline (`.github/workflows/microservices-ci-cd.yml`) provides:

```yaml
name: Microservices CI/CD

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        service: [storage, key-management, notification, oracle]
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x
    
    - name: Run Tests
      run: |
        dotnet test tests/Unit/${{ matrix.service }}.Tests \
          --collect:"XPlat Code Coverage" \
          --logger "trx;LogFileName=${{ matrix.service }}.trx"
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        file: ./TestResults/**/coverage.cobertura.xml
        flags: ${{ matrix.service }}
```

### **Test Pipeline Stages**

1. **🏗️ Build Stage**
   - Compile all projects
   - Restore NuGet packages
   - Run code analysis

2. **🧪 Test Stage**
   - Unit tests (parallel execution)
   - Integration tests (with test containers)
   - Security scans

3. **📊 Report Stage**
   - Generate coverage reports
   - Upload to Codecov
   - Comment on PRs

4. **🐳 Container Stage**
   - Build Docker images
   - Run container tests
   - Push to registry (main branch)

## 🛠️ Test Infrastructure

### **Test Containers Setup**

```csharp
// TestContainerFixture.cs
public class TestContainerFixture : IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private readonly IContainer _redisContainer;
    
    public TestContainerFixture()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:16-alpine")
            .WithEnvironment("POSTGRES_PASSWORD", "test")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
            
        _redisContainer = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync()
        );
    }
    
    public string PostgresConnectionString => 
        $"Host=localhost;Port={_postgresContainer.GetMappedPublicPort(5432)};Database=test;Username=postgres;Password=test";
}
```

### **Mock Service Builder**

```csharp
// ServiceMockBuilder.cs
public class ServiceMockBuilder
{
    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);
    
    public Mock<IStorageService> BuildStorageService()
    {
        var mock = _mockRepository.Create<IStorageService>();
        
        mock.Setup(x => x.StoreDocumentAsync(It.IsAny<Document>()))
            .ReturnsAsync((Document doc) => new DocumentResult 
            { 
                Id = Guid.NewGuid(), 
                IsEncrypted = true 
            });
            
        return mock;
    }
    
    public Mock<IKeyManagementService> BuildKeyManagementService()
    {
        var mock = _mockRepository.Create<IKeyManagementService>();
        
        mock.Setup(x => x.GenerateKeyAsync(It.IsAny<KeyAlgorithm>(), It.IsAny<int>()))
            .ReturnsAsync(new CryptographicKey 
            { 
                Id = Guid.NewGuid(), 
                Algorithm = KeyAlgorithm.RSA 
            });
            
        return mock;
    }
}
```

## 🚀 Performance Testing

### **Load Test Configuration**

```json
// nbomber-config.json
{
  "TestSuite": "NeoServiceLayer",
  "TestName": "storage_service_load_test",
  "TargetScenarios": ["storage_write", "storage_read"],
  "GlobalSettings": {
    "ScenariosSettings": [
      {
        "ScenarioName": "storage_write",
        "LoadSimulationsSettings": [
          {
            "Type": "inject_per_sec",
            "Rate": 100,
            "During": "00:00:30"
          }
        ]
      }
    ],
    "ReportFormats": ["Html", "Csv", "Txt"]
  }
}
```

### **Benchmark Tests**

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class CryptographyBenchmark
{
    private byte[] _data;
    private IEncryptionService _service;
    
    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[1024 * 1024]; // 1MB
        _service = new AesEncryptionService();
    }
    
    [Benchmark]
    public async Task<byte[]> EncryptData() => 
        await _service.EncryptAsync(_data);
    
    [Benchmark]
    public async Task<byte[]> DecryptData() => 
        await _service.DecryptAsync(_encryptedData);
}
```

## 🏆 Best Practices

### **Test Design Principles**

1. **🎯 Single Responsibility** - Each test validates one behavior
2. **🔄 Repeatable** - Tests produce consistent results
3. **🚀 Fast** - Unit tests < 1ms, Integration < 100ms
4. **🔒 Isolated** - No shared state between tests
5. **📝 Descriptive** - Test names clearly state intent

### **Testing Checklist**

- [ ] **Arrange-Act-Assert** pattern followed
- [ ] **No hardcoded values** - Use builders/fixtures
- [ ] **Async tests** use async/await properly
- [ ] **Mocks verified** for expected interactions
- [ ] **Edge cases** covered (null, empty, invalid)
- [ ] **Error scenarios** tested explicitly
- [ ] **Performance** considered for data-heavy tests

### **Common Pitfalls to Avoid**

```csharp
// ❌ Bad: Shared state between tests
public class BadTest
{
    private static readonly List<string> _items = new();
    
    [Fact]
    public void Test1() => _items.Add("test");
    
    [Fact]
    public void Test2() => Assert.Empty(_items); // Fails!
}

// ✅ Good: Isolated test state
public class GoodTest
{
    [Fact]
    public void Test1()
    {
        var items = new List<string>();
        items.Add("test");
        Assert.Single(items);
    }
}
```

## 📚 Testing Resources

### **Tools & Frameworks**
- **[xUnit](https://xunit.net/)** - Test framework
- **[Moq](https://github.com/moq/moq4)** - Mocking library
- **[FluentAssertions](https://fluentassertions.com/)** - Assertion library
- **[Testcontainers](https://dotnet.testcontainers.org/)** - Container testing
- **[NBomber](https://nbomber.com/)** - Load testing
- **[BenchmarkDotNet](https://benchmarkdotnet.org/)** - Micro-benchmarks

### **Documentation**
- **[Unit Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)** - Microsoft guide
- **[Integration Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)** - ASP.NET Core testing
- **[Performance Testing Guide](docs/testing/performance-testing.md)** - Internal guide

## 🎯 Next Steps

1. **Run Tests**: Start with `dotnet test` to verify setup
2. **Check Coverage**: Generate and review coverage reports
3. **Add Tests**: Contribute tests for uncovered code
4. **Improve Performance**: Run benchmarks and optimize

---

**🧪 Quality is everyone's responsibility. Test early, test often, test well!**

**Built with ❤️ and tested with confidence**