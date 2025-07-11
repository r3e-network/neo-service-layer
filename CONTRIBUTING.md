# Contributing to Neo Service Layer

[![Contributions Welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](https://github.com/r3e-network/neo-service-layer)
[![Code of Conduct](https://img.shields.io/badge/code%20of%20conduct-enforced-blue.svg)](CODE_OF_CONDUCT.md)
[![Good First Issues](https://img.shields.io/github/issues/r3e-network/neo-service-layer/good%20first%20issue)](https://github.com/r3e-network/neo-service-layer/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)

> **🤝 Welcome Contributors!** - Thank you for your interest in contributing to the Neo Service Layer microservices platform

## 🌟 Ways to Contribute

We welcome all forms of contributions:

### **Code Contributions**
- **🐛 Bug Fixes**: Help us squash bugs and improve stability
- **✨ New Features**: Implement new microservices or enhance existing ones
- **♻️ Refactoring**: Improve code quality and maintainability
- **⚡ Performance**: Optimize services for better performance
- **🔒 Security**: Enhance security features and fix vulnerabilities

### **Non-Code Contributions**
- **📚 Documentation**: Improve guides, API docs, and examples
- **🧪 Testing**: Add tests or improve test coverage
- **🎨 Design**: UI/UX improvements for the website
- **🌍 Translations**: Help translate documentation
- **💡 Ideas**: Suggest features and improvements

## 🚀 Getting Started

### **Prerequisites**

| Requirement | Version | Purpose |
|-------------|---------|----------|
| **.NET SDK** | 9.0+ | Core development |
| **Docker** | 24.0+ | Container development |
| **Git** | 2.40+ | Version control |
| **Node.js** | 18+ | Website development |
| **PostgreSQL** | 14+ | Database development |

### **Development Environment Setup**

```bash
# 1. Fork and clone the repository
git clone https://github.com/YOUR_USERNAME/neo-service-layer.git
cd neo-service-layer

# 2. Add upstream remote
git remote add upstream https://github.com/r3e-network/neo-service-layer.git

# 3. Install dependencies and build
dotnet restore
dotnet build

# 4. Start infrastructure services
docker-compose up -d

# 5. Run tests to verify setup
dotnet test

# 6. Start development
dotnet run --project src/Api/NeoServiceLayer.Api/
```

### **Quick Development Commands**

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Start specific service
dotnet run --project src/Services/NeoServiceLayer.Services.Storage/

# Format code
dotnet format

# Clean build artifacts
dotnet clean
```

## 📋 Development Workflow

### **1. Sync Your Fork**

```bash
# Fetch latest changes
git checkout main
git fetch upstream
git merge upstream/main
git push origin main
```

### **2. Create Feature Branch**

```bash
# Branch naming convention
git checkout -b <type>/<short-description>

# Examples:
git checkout -b feat/add-caching-service
git checkout -b fix/storage-memory-leak
git checkout -b docs/update-api-examples
```

### **3. Development Process**

#### **Code Quality Checklist**
- [ ] Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ ] Add XML documentation for public APIs
- [ ] Write unit tests (aim for 80%+ coverage)
- [ ] Update relevant documentation
- [ ] Run code analysis tools

#### **Before Committing**
```bash
# Format code
dotnet format

# Run tests
dotnet test

# Check for issues
dotnet build -warnaserror

# Run security scan
dotnet list package --vulnerable
```

### **4. Commit Guidelines**

We use [Conventional Commits](https://www.conventionalcommits.org/):

```bash
# Format: <type>(<scope>): <subject>

# Examples:
git commit -m "feat(storage): add encryption at rest support"
git commit -m "fix(auth): resolve JWT token expiration issue"
git commit -m "docs(readme): update quick start guide"
git commit -m "perf(oracle): optimize data aggregation queries"
git commit -m "test(notification): add integration tests for SMS provider"
```

#### **Commit Types**
| Type | Description | Example |
|------|-------------|---------|
| `feat` | New feature | `feat(api): add rate limiting` |
| `fix` | Bug fix | `fix(cache): memory leak in Redis client` |
| `docs` | Documentation | `docs(sdk): update authentication guide` |
| `style` | Code style | `style: format code with dotnet format` |
| `refactor` | Code refactoring | `refactor(core): extract common interfaces` |
| `perf` | Performance | `perf(query): optimize database queries` |
| `test` | Testing | `test(auth): add unit tests for JWT` |
| `build` | Build system | `build: update to .NET 9.0` |
| `ci` | CI/CD | `ci: add GitHub Actions workflow` |
| `chore` | Maintenance | `chore: update dependencies` |

### **5. Submit Pull Request**

#### **PR Checklist**
```markdown
## Description
Brief description of what this PR does

## Type of Change
- [ ] 🐛 Bug fix (non-breaking change)
- [ ] ✨ New feature (non-breaking change)
- [ ] 💥 Breaking change (fix or feature with breaking changes)
- [ ] 📚 Documentation update
- [ ] 🎨 Style update
- [ ] ♻️ Code refactor
- [ ] ⚡ Performance improvement
- [ ] ✅ Test update
- [ ] 🔧 Build configuration update

## Testing
- [ ] Unit tests pass locally
- [ ] Integration tests pass locally
- [ ] Manual testing completed
- [ ] Test coverage maintained/improved

## Checklist
- [ ] My code follows the project's style guidelines
- [ ] I have performed a self-review
- [ ] I have added tests that prove my fix/feature works
- [ ] New and existing unit tests pass locally
- [ ] I have updated relevant documentation
- [ ] My changes generate no new warnings
- [ ] Any dependent changes have been merged

## Related Issues
Closes #(issue number)

## Screenshots (if applicable)
Add screenshots for UI changes
```

## 🧪 Testing Requirements

### **Test Coverage Standards**

| Component Type | Required Coverage | Current Coverage |
|----------------|-------------------|------------------|
| **Core Services** | 90%+ | ✅ 92% |
| **API Controllers** | 85%+ | ✅ 87% |
| **Utilities** | 95%+ | ✅ 96% |
| **Infrastructure** | 80%+ | ✅ 83% |
| **Overall** | 85%+ | ✅ 86% |

### **Test Categories**

```bash
# Unit Tests - Fast, isolated tests
dotnet test --filter "Category=Unit"

# Integration Tests - Service interaction tests
dotnet test --filter "Category=Integration"

# Performance Tests - Load and stress tests
dotnet test --filter "Category=Performance"

# Security Tests - Cryptographic and security tests
dotnet test --filter "Category=Security"

# E2E Tests - End-to-end scenarios
dotnet test --filter "Category=E2E"
```

### **Writing Tests**

#### **Unit Test Example**
```csharp
[Fact]
[Category("Unit")]
public async Task StorageService_StoreDocument_ShouldEncryptData()
{
    // Arrange
    var service = new StorageService(_mockCrypto.Object, _mockRepo.Object);
    var document = new Document { Content = "sensitive data" };
    
    // Act
    var result = await service.StoreDocumentAsync(document);
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.IsEncrypted);
    _mockCrypto.Verify(x => x.Encrypt(It.IsAny<byte[]>()), Times.Once);
}
```

#### **Integration Test Example**
```csharp
[Fact]
[Category("Integration")]
public async Task ApiGateway_ServiceDiscovery_ShouldRouteToHealthyService()
{
    // Arrange
    await using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/v1/storage/health");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("healthy", content);
}
```

### **Test Organization**

```
tests/
├── Unit/
│   ├── NeoServiceLayer.Core.Tests/
│   ├── NeoServiceLayer.Services.Storage.Tests/
│   └── NeoServiceLayer.Api.Tests/
├── Integration/
│   ├── NeoServiceLayer.Integration.Tests/
│   └── NeoServiceLayer.E2E.Tests/
├── Performance/
│   └── NeoServiceLayer.Performance.Tests/
└── Shared/
    └── NeoServiceLayer.TestUtilities/
```

## 📝 Code Standards

### **C# Coding Conventions**

```csharp
// ✅ Good: Clear naming and structure
public sealed class StorageService : IStorageService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<StorageService> _logger;
    
    public StorageService(
        IEncryptionService encryptionService,
        ILogger<StorageService> logger)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Stores a document with encryption.
    /// </summary>
    /// <param name="document">The document to store.</param>
    /// <returns>The stored document result.</returns>
    /// <exception cref="StorageException">Thrown when storage fails.</exception>
    public async Task<DocumentResult> StoreDocumentAsync(Document document)
    {
        _logger.LogInformation("Storing document {DocumentId}", document.Id);
        
        try
        {
            var encrypted = await _encryptionService.EncryptAsync(document.Content);
            return await StoreEncryptedDataAsync(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store document {DocumentId}", document.Id);
            throw new StorageException($"Failed to store document {document.Id}", ex);
        }
    }
}
```

### **Code Quality Rules**

#### **Naming Conventions**
- **Classes**: PascalCase (e.g., `StorageService`)
- **Interfaces**: IPascalCase (e.g., `IStorageService`)
- **Methods**: PascalCase (e.g., `StoreDocumentAsync`)
- **Parameters**: camelCase (e.g., `documentId`)
- **Private fields**: _camelCase (e.g., `_logger`)
- **Constants**: UPPER_CASE (e.g., `MAX_RETRY_COUNT`)

#### **Async/Await Best Practices**
```csharp
// ✅ Good: Async all the way
public async Task<Result> ProcessAsync()
{
    var data = await GetDataAsync();
    return await TransformDataAsync(data);
}

// ❌ Bad: Blocking async code
public Result Process()
{
    var data = GetDataAsync().Result; // Don't do this!
    return TransformDataAsync(data).Result;
}
```

#### **Exception Handling**
```csharp
// ✅ Good: Specific exception handling with logging
try
{
    await ProcessDataAsync();
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed");
    return BadRequest(ex.Message);
}
catch (NotFoundException ex)
{
    _logger.LogWarning(ex, "Resource not found");
    return NotFound(ex.Message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error occurred");
    return StatusCode(500, "An error occurred");
}
```

### **Documentation Standards**

#### **XML Documentation**
```csharp
/// <summary>
/// Manages cryptographic key operations using Intel SGX.
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Generates a new cryptographic key.
    /// </summary>
    /// <param name="algorithm">The key algorithm to use.</param>
    /// <param name="keySize">The size of the key in bits.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the generated key.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the algorithm is not supported.
    /// </exception>
    /// <example>
    /// <code>
    /// var key = await keyService.GenerateKeyAsync(KeyAlgorithm.RSA, 2048);
    /// </code>
    /// </example>
    Task<CryptographicKey> GenerateKeyAsync(KeyAlgorithm algorithm, int keySize);
}
```

## 🔒 Security Guidelines

### **Security-First Development**

#### **Secure Coding Practices**
```csharp
// ✅ Good: Input validation and sanitization
public async Task<IActionResult> CreateUser(CreateUserRequest request)
{
    // Validate input
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    // Sanitize input
    request.Email = request.Email.Trim().ToLowerInvariant();
    request.Name = HtmlEncoder.Default.Encode(request.Name);
    
    // Process securely
    var hashedPassword = await _passwordHasher.HashPasswordAsync(request.Password);
    // Never log sensitive data
    _logger.LogInformation("Creating user with email {Email}", request.Email);
}

// ✅ Good: Secure cryptographic operations
public async Task<byte[]> EncryptDataAsync(byte[] data)
{
    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.Mode = CipherMode.GCM;
    
    // Generate secure random IV
    aes.GenerateIV();
    
    // Use key from secure storage
    aes.Key = await _keyVault.GetEncryptionKeyAsync();
    
    // Encrypt with authentication tag
    using var encryptor = aes.CreateEncryptor();
    return await EncryptWithAuthenticationAsync(data, encryptor, aes.IV);
}
```

#### **Common Security Pitfalls to Avoid**
- ❌ Hardcoding secrets or credentials
- ❌ Logging sensitive information
- ❌ Using weak cryptographic algorithms
- ❌ Trusting user input without validation
- ❌ Exposing internal error details to users
- ❌ Using predictable random values for security

### **Reporting Security Vulnerabilities**

⚠️ **DO NOT** create public GitHub issues for security vulnerabilities!

#### **Responsible Disclosure Process**

1. **Email**: Send details to `security@r3e.network`
2. **Include**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact assessment
   - Suggested fix (if available)
3. **Timeline**: Allow 90 days for patch development
4. **Recognition**: Security researchers will be credited

#### **Security Issue Template**
```markdown
Subject: [SECURITY] Vulnerability in [Component]

## Summary
Brief description of the security issue

## Details
- **Component**: Affected service/component
- **Version**: Affected versions
- **Severity**: Critical/High/Medium/Low

## Reproduction Steps
1. Step one
2. Step two
3. ...

## Impact
Potential security impact

## Suggested Fix
Recommended solution (optional)
```

## 🏗️ Microservice Development

### **Creating a New Microservice**

#### **1. Service Scaffolding**
```bash
# Use the service template
dotnet new webapi -n NeoServiceLayer.Services.YourService
cd NeoServiceLayer.Services.YourService

# Add required packages
dotnet add package NeoServiceLayer.ServiceFramework
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
```

#### **2. Service Structure**
```
src/Services/NeoServiceLayer.Services.YourService/
├── Controllers/
│   └── YourServiceController.cs      # API endpoints
├── Services/
│   ├── IYourService.cs              # Service interface
│   └── YourService.cs               # Service implementation
├── Models/
│   ├── Requests/                    # Request DTOs
│   └── Responses/                   # Response DTOs
├── Infrastructure/
│   ├── HealthChecks/               # Health check implementations
│   └── ServiceDiscovery/           # Consul registration
├── Program.cs                       # Service entry point
├── appsettings.json                # Configuration
├── Dockerfile                       # Container definition
└── README.md                        # Service documentation
```

#### **3. Service Implementation**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add service framework
builder.Services.AddServiceFramework(options =>
{
    options.ServiceName = "your-service";
    options.ServiceVersion = "1.0.0";
});

// Add your service
builder.Services.AddScoped<IYourService, YourService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<YourServiceHealthCheck>("service")
    .AddCheck<DependencyHealthCheck>("dependencies");

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

var app = builder.Build();

// Configure pipeline
app.UseServiceFramework();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
```

#### **4. Service Registration**
```csharp
// Infrastructure/ServiceDiscovery/ServiceRegistration.cs
public class ServiceRegistration : IHostedService
{
    private readonly IConsulClient _consul;
    private readonly IConfiguration _configuration;
    private string _registrationId;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var registration = new AgentServiceRegistration
        {
            ID = $"{ServiceName}-{Guid.NewGuid()}",
            Name = ServiceName,
            Address = _configuration["Service:Address"],
            Port = _configuration.GetValue<int>("Service:Port"),
            Tags = new[] { "api", "v1" },
            Check = new AgentServiceCheck
            {
                HTTP = $"http://localhost:{Port}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5)
            }
        };

        await _consul.Agent.ServiceRegister(registration, cancellationToken);
        _registrationId = registration.ID;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consul.Agent.ServiceDeregister(_registrationId, cancellationToken);
    }
}
```

### **5. Docker Configuration**
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NeoServiceLayer.Services.YourService.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.YourService.dll"]
```

## 📚 Documentation Requirements

### **Service Documentation Template**
```markdown
# Your Service

## Overview
Brief description of what this service does

## API Endpoints
- `GET /api/v1/your-service/resource` - Get resources
- `POST /api/v1/your-service/resource` - Create resource
- `PUT /api/v1/your-service/resource/{id}` - Update resource
- `DELETE /api/v1/your-service/resource/{id}` - Delete resource

## Configuration
| Variable | Description | Default |
|----------|-------------|---------|
| `Service:Port` | Service port | 8080 |
| `Service:Name` | Service name | your-service |

## Dependencies
- PostgreSQL for data persistence
- Redis for caching
- Other services this depends on

## Health Checks
- `/health` - Overall service health
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

## Examples
Show API usage examples with curl or SDK
```

## 🌟 Recognition & Community

### **Contributors Hall of Fame**
We recognize all contributors in our:
- 📜 [Contributors List](https://github.com/r3e-network/neo-service-layer/graphs/contributors)
- 🎉 Release notes
- 🏆 Annual contributor awards
- 💝 Special thanks section

### **First-Time Contributors**
We ❤️ first-time contributors! Look for issues labeled:
- `good first issue` - Great for beginners
- `help wanted` - We need your help!
- `documentation` - Help improve docs

### **Community Channels**
- **💬 Discussions**: [GitHub Discussions](https://github.com/r3e-network/neo-service-layer/discussions)
- **🐛 Issues**: [GitHub Issues](https://github.com/r3e-network/neo-service-layer/issues)
- **📧 Email**: contributors@r3e.network

## 🎯 Next Steps

1. **Find an Issue**: Browse [open issues](https://github.com/r3e-network/neo-service-layer/issues)
2. **Ask Questions**: Use [discussions](https://github.com/r3e-network/neo-service-layer/discussions) for help
3. **Submit PR**: Follow the guidelines above
4. **Celebrate**: You're now a contributor! 🎉

---

**Thank you for contributing to the Neo Service Layer! Together, we're building the future of blockchain microservices.** 🚀

**Built with ❤️ by the Neo Community**