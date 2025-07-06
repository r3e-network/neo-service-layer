# Neo Service Layer - Development Guide

## Overview

This guide provides comprehensive instructions for developing the Neo Service Layer with its **26 services** and **interactive web application**. It covers development environment setup, project structure, coding standards, testing strategies, and contribution guidelines for the complete ecosystem.

## 🌐 Web Application Development

The Neo Service Layer includes a full-featured web application built with:
- **ASP.NET Core 8.0** with Razor Pages
- **Bootstrap 5** for responsive UI
- **JavaScript ES6+** for client-side functionality
- **JWT Authentication** with role-based authorization
- **OpenAPI/Swagger** for API documentation
- **Real-time Service Integration** with all 26 services

## Prerequisites

### Development Environment

- **Operating System**: Windows 10/11, macOS, or Linux
- **.NET SDK**: .NET 8.0 (current implementation)
- **IDE**: Visual Studio 2025, Visual Studio Code, or JetBrains Rider
- **Git**: Git 2.30 or later
- **Intel SGX SDK**: SGX SDK 2.15 or later (for enclave development)
- **Occlum LibOS**: Occlum 0.30.0 or later (for enclave development)
- **Docker**: Docker 20.10 or later (optional, for containerized development)

### Knowledge Requirements

- **C#**: Proficiency in C# programming
- **C++**: Familiarity with C++ programming (for enclave development)
- **JavaScript**: Familiarity with JavaScript programming (for function execution)
- **Blockchain**: Understanding of blockchain concepts
- **Neo N3**: Familiarity with Neo N3 blockchain
- **NeoX**: Familiarity with NeoX blockchain
- **SGX**: Understanding of Intel SGX and Trusted Execution Environments
- **Occlum LibOS**: Understanding of Occlum LibOS

## Development Setup

### Clone the Repository

```bash
git clone https://github.com/neo-project/neo-service-layer.git
cd neo-service-layer
```

### Install Dependencies

```bash
# Install .NET dependencies
dotnet restore

# Install SGX SDK (Windows)
# Download from https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/overview.html

# Install SGX SDK (Linux)
sudo apt install -y dkms
wget https://download.01.org/intel-sgx/sgx-linux/2.15/distro/ubuntu20.04-server/sgx_linux_x64_driver_2.11.0_2d2b795.bin
chmod +x sgx_linux_x64_driver_2.11.0_2d2b795.bin
sudo ./sgx_linux_x64_driver_2.11.0_2d2b795.bin

# Install Occlum LibOS (Linux)
sudo apt install -y libsgx-dcap-ql libsgx-dcap-default-qpl libsgx-dcap-quote-verify
wget https://github.com/occlum/occlum/releases/download/0.30.0/occlum-0.30.0-ubuntu20.04-x86_64.tar.gz
tar -xzf occlum-0.30.0-ubuntu20.04-x86_64.tar.gz
cd occlum-0.30.0
sudo ./install.sh
```

### Build the Solution

```bash
dotnet build
```

### Run the Web Application

```bash
# Run the complete web application with all services
dotnet run --project src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj

# Access the application at:
# - Main Interface: http://localhost:5000
# - Service Demo: http://localhost:5000/servicepages/servicedemo
# - API Documentation: http://localhost:5000/swagger
```

**Alternative - API Only:**
```bash
# For API-only development
dotnet run --project src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj
```

### Configure Development Environment

```bash
# Configure web application
cp src/Web/NeoServiceLayer.Web/appsettings.example.json src/Web/NeoServiceLayer.Web/appsettings.Development.json

# Configure API service (if running separately)
cp src/Api/NeoServiceLayer.Api/appsettings.example.json src/Api/NeoServiceLayer.Api/appsettings.Development.json

# Edit configurations as needed
nano src/Web/NeoServiceLayer.Web/appsettings.Development.json
```

## Project Structure

The Neo Service Layer is organized as a comprehensive solution with 20+ services:

### Source Code (`src/`)

```
src/
├── Web/
│   └── NeoServiceLayer.Web/          # Interactive web application
├── Api/
│   └── NeoServiceLayer.Api/          # RESTful API service
├── Core/
│   ├── NeoServiceLayer.Core/         # Core framework and interfaces
│   └── NeoServiceLayer.ServiceFramework/  # Service lifecycle management
├── Services/                        # 20+ Service implementations
│   ├── NeoServiceLayer.Services.KeyManagement/
│   ├── NeoServiceLayer.Services.Randomness/
│   ├── NeoServiceLayer.Services.Oracle/
│   ├── NeoServiceLayer.Services.Voting/
│   ├── NeoServiceLayer.Services.Storage/
│   ├── NeoServiceLayer.Services.Backup/
│   ├── NeoServiceLayer.Services.Configuration/
│   ├── NeoServiceLayer.Services.ZeroKnowledge/
│   ├── NeoServiceLayer.Services.AbstractAccount/
│   ├── NeoServiceLayer.Services.Compliance/
│   ├── NeoServiceLayer.Services.ProofOfReserve/
│   ├── NeoServiceLayer.Services.Automation/
│   ├── NeoServiceLayer.Services.Monitoring/
│   ├── NeoServiceLayer.Services.Health/
│   ├── NeoServiceLayer.Services.Notification/
│   ├── NeoServiceLayer.Services.CrossChain/
│   ├── NeoServiceLayer.Services.Compute/
│   ├── NeoServiceLayer.Services.EventSubscription/
│   ├── NeoServiceLayer.AI.PatternRecognition/
│   └── NeoServiceLayer.AI.Prediction/
├── Advanced/
│   └── NeoServiceLayer.Advanced.FairOrdering/
├── Tee/                            # Trusted Execution Environment
│   ├── NeoServiceLayer.Tee.Host/     # Host application
│   └── NeoServiceLayer.Tee.Enclave/  # SGX + Occlum enclave code
└── Infrastructure/
    └── NeoServiceLayer.Infrastructure/  # Shared infrastructure
```

### Tests (`tests/`)

```
tests/
├── Web/
│   └── NeoServiceLayer.Web.Tests/     # Web application tests
├── Api/
│   └── NeoServiceLayer.Api.Tests/     # API service tests
├── Services/                       # Service-specific tests
│   ├── NeoServiceLayer.Services.*.Tests/
│   └── ...
├── Tee/
│   └── NeoServiceLayer.Tee.*.Tests/   # SGX enclave tests
└── TestInfrastructure/
    └── NeoServiceLayer.TestInfrastructure/  # Shared test utilities
```

### Documentation (`docs/`)

```
docs/
├── web/                    # Web application documentation
├── api/                    # API documentation
├── architecture/           # System architecture
├── services/               # Service documentation
├── deployment/             # Deployment guides
├── development/            # Development guides
├── security/               # Security documentation
└── troubleshooting/        # Troubleshooting guides
```

## Coding Standards

The Neo Service Layer project follows these coding standards:

### C# Coding Standards

- **Naming Conventions**:
  - **PascalCase**: Classes, methods, properties, constants, events, enum values
  - **camelCase**: Local variables, method parameters
  - **_camelCase**: Private fields
  - **UPPER_CASE**: Static readonly fields

- **Code Organization**:
  - One class per file
  - Namespace matches folder structure
  - Using directives at the top of the file
  - Members organized by accessibility (public, internal, protected, private)

- **Code Style**:
  - Use var only when the type is obvious
  - Use expression-bodied members for simple methods and properties
  - Use null-conditional operators and null-coalescing operators
  - Use string interpolation instead of string.Format
  - Use async/await for asynchronous operations

### C++ Coding Standards (for Enclave Development)

- **Naming Conventions**:
  - **PascalCase**: Classes, structs, enums, type aliases
  - **camelCase**: Functions, variables, parameters
  - **UPPER_CASE**: Macros, constants

- **Code Organization**:
  - One class per file
  - Header files use include guards
  - Forward declarations to minimize includes
  - Members organized by accessibility (public, protected, private)

- **Code Style**:
  - Use const whenever possible
  - Use references instead of pointers when possible
  - Use smart pointers instead of raw pointers
  - Use auto only when the type is obvious
  - Use range-based for loops when possible

### Documentation Standards

- **XML Documentation**:
  - All public members should have XML documentation
  - XML documentation should include summary, parameters, returns, and exceptions
  - XML documentation should be clear and concise

- **Comments**:
  - Use comments to explain why, not what
  - Use comments to explain complex algorithms
  - Use comments to explain workarounds or hacks

## Testing

The Neo Service Layer project uses the following testing frameworks:

- **xUnit**: For unit tests
- **Moq**: For mocking dependencies
- **FluentAssertions**: For assertions

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Services/NeoServiceLayer.Services.Randomness.Tests/

# Run web application tests
dotnet test tests/Web/NeoServiceLayer.Web.Tests/

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with specific filter
dotnet test --filter "Category=Unit"
```

### Writing Tests

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test components working together
- **End-to-End Tests**: Test the entire system

Example unit test:

```csharp
[Fact]
public async Task GenerateRandomNumber_WithValidRange_ReturnsNumberInRange()
{
    // Arrange
    var enclaveManagerMock = new Mock<IEnclaveManager>();
    enclaveManagerMock.Setup(e => e.ExecuteJavaScriptAsync(It.IsAny<string>()))
        .ReturnsAsync("42");
    
    var service = new RandomnessService(enclaveManagerMock.Object, new ServiceConfiguration(), new NullLogger<RandomnessService>());
    await service.InitializeAsync();
    await service.StartAsync();
    
    // Act
    var result = await service.GenerateRandomNumberAsync(1, 100, BlockchainType.NeoN3);
    
    // Assert
    result.Should().BeGreaterOrEqualTo(1);
    result.Should().BeLessOrEqualTo(100);
}
```

## Debugging

### Debugging .NET Code

- **Visual Studio**: Use the built-in debugger
- **Visual Studio Code**: Use the C# extension
- **JetBrains Rider**: Use the built-in debugger

### Debugging Enclave Code

- **SGX Simulation Mode**: Use SGX simulation mode for debugging
- **Occlum Debug Mode**: Use Occlum debug mode for debugging
- **Logging**: Add logging statements to enclave code

## Contributing

### Contribution Process

1. **Fork the Repository**: Fork the repository on GitHub
2. **Create a Branch**: Create a branch for your changes
3. **Make Changes**: Make your changes following the coding standards
4. **Write Tests**: Write tests for your changes
5. **Run Tests**: Run tests to ensure they pass
6. **Submit Pull Request**: Submit a pull request with your changes

### Pull Request Guidelines

- **Title**: Use a clear and descriptive title
- **Description**: Provide a detailed description of the changes
- **Issue Reference**: Reference any related issues
- **Changelog**: Include a changelog entry
- **Tests**: Include tests for new functionality
- **Documentation**: Update documentation for new features or changes

### Code Review Process

1. **Automated Checks**: Automated checks for coding standards and tests
2. **Peer Review**: Review by project maintainers
3. **Feedback**: Feedback and requested changes
4. **Approval**: Approval by project maintainers
5. **Merge**: Merge into the main branch

## 🔧 Web Application Development

### Adding a New Service to Web Application

1. **Create Service Implementation** (if new):
```bash
dotnet new classlib -n NeoServiceLayer.Services.YourService \
  -o src/Services/NeoServiceLayer.Services.YourService -f net8.0
```

2. **Add Web Application Reference**:
```bash
dotnet add src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj \
  reference src/Services/NeoServiceLayer.Services.YourService/
```

3. **Register Service in Program.cs**:
```csharp
// In src/Web/NeoServiceLayer.Web/Program.cs
builder.Services.AddScoped<IYourService, YourService>();
```

4. **Create API Controller**:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class YourServiceController : ControllerBase
{
    private readonly IYourService _yourService;
    
    [HttpPost("endpoint")]
    public async Task<IActionResult> YourEndpoint([FromBody] YourRequest request)
    {
        var result = await _yourService.ProcessAsync(request);
        return Ok(new { success = true, data = result });
    }
}
```

5. **Add to Service Demo Page**:
```javascript
// In src/Web/NeoServiceLayer.Web/Pages/ServicePages/ServiceDemo.cshtml
// Add service section to the web interface
```

6. **Update Documentation**:
- Add service to [API Reference](../web/API_REFERENCE.md)
- Update [Service Integration Guide](../web/SERVICE_INTEGRATION.md)

### Web Application Architecture

```
Web Application
├── Controllers/           # API controllers for each service
├── Pages/
│   ├── Shared/            # Shared layout and components
│   └── ServicePages/      # Service demonstration pages
├── wwwroot/
│   ├── css/               # Stylesheets
│   ├── js/                # JavaScript files
│   └── lib/               # Third-party libraries
├── Models/                # View models and DTOs
├── Services/              # Web application services
└── Configuration/         # Web app configuration
```

## 🔄 Development Workflow

### Daily Development

1. **Start Development Environment**:
```bash
# Start web application with hot reload
dotnet watch run --project src/Web/NeoServiceLayer.Web
```

2. **Test Changes**:
```bash
# Access web interface
open http://localhost:5000/servicepages/servicedemo

# Test API endpoints
curl -H "Authorization: Bearer $(curl -X POST http://localhost:5000/api/auth/demo-token | jq -r '.token')" \
     http://localhost:5000/api/yourservice/endpoint
```

3. **Run Tests**:
```bash
# Run all tests
dotnet test

# Run specific service tests
dotnet test tests/Services/NeoServiceLayer.Services.YourService.Tests/

# Run web application tests
dotnet test tests/Web/NeoServiceLayer.Web.Tests/
```

### Integration Testing

```csharp
[Collection("Web Application")]
public class ServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public ServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task ServiceDemo_Page_Should_Load_All_Services()
    {
        // Test that service demo page loads and all services are accessible
        var response = await _client.GetAsync("/servicepages/servicedemo");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Key Management Service");
        content.Should().Contain("Randomness Service");
        // ... test for all 20+ services
    }
}
```

## 📚 Related Documentation

### Neo Service Layer Documentation
- **[Architecture Overview](../architecture/ARCHITECTURE_OVERVIEW.md)** - System architecture and design
- **[Services Documentation](../services/README.md)** - All 20+ services documentation
- **[Web Application Guide](../web/WEB_APPLICATION_GUIDE.md)** - Complete web app guide
- **[API Reference](../web/API_REFERENCE.md)** - Detailed API documentation
- **[Service Integration](../web/SERVICE_INTEGRATION.md)** - Service integration patterns
- **[Authentication & Security](../web/AUTHENTICATION.md)** - Security implementation
- **[Deployment Guide](../deployment/README.md)** - Production deployment

### Framework Documentation
- **[ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)** - Web framework
- **[Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)** - UI framework
- **[xUnit Documentation](https://xunit.net/)** - Testing framework
- **[Moq Documentation](https://github.com/moq/moq4)** - Mocking framework

### Blockchain Documentation
- **[Neo N3 Documentation](https://docs.neo.org/)** - Neo N3 blockchain
- **[NeoX Documentation](https://docs.neo.org/neox/)** - NeoX EVM-compatible chain

### Security Documentation
- **[Intel SGX Documentation](https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/overview.html)** - Intel SGX
- **[Occlum LibOS Documentation](https://occlum.io/)** - Occlum LibOS for SGX

## ✅ Development Checklist

### Environment Setup
- [ ] .NET 8.0 SDK installed
- [ ] Intel SGX driver installed (for enclave development)
- [ ] Occlum LibOS installed (for enclave development)
- [ ] Repository cloned and dependencies restored
- [ ] Web application builds and runs successfully

### Development Workflow
- [ ] Web application accessible at `http://localhost:5000`
- [ ] Service demo page functional with all 20+ services
- [ ] API documentation available at `/swagger`
- [ ] Authentication working with JWT tokens
- [ ] All tests passing

### Before Contributing
- [ ] Code follows established patterns and standards
- [ ] New services registered in web application
- [ ] API endpoints properly documented
- [ ] Unit and integration tests written
- [ ] Documentation updated
- [ ] Pull request follows contribution guidelines
