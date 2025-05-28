# Neo Service Layer - Development Guide

## Overview

This guide provides instructions for setting up a development environment for the Neo Service Layer and contributing to the project. It covers prerequisites, development setup, coding standards, testing, and contribution guidelines.

## Prerequisites

### Development Environment

- **Operating System**: Windows 10/11, macOS, or Linux
- **.NET SDK**: .NET 9.0 or later
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

### Run the Solution

```bash
dotnet run --project src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj
```

### Configure Development Environment

```bash
# Copy the example configuration
cp config/appsettings.Development.example.json config/appsettings.Development.json

# Edit the configuration
nano config/appsettings.Development.json
```

## Project Structure

The Neo Service Layer project is structured as follows:

- **src**: Source code
  - **Api**: API service
  - **Core**: Core framework
  - **Infrastructure**: Infrastructure components
  - **Services**: Service implementations
  - **Tee**: Trusted Execution Environment components
    - **Host**: Host application
    - **Enclave**: Enclave code
  - **Shared**: Shared components

- **tests**: Test code
  - **Api**: API service tests
  - **Core**: Core framework tests
  - **Infrastructure**: Infrastructure component tests
  - **Services**: Service implementation tests
  - **Tee**: Trusted Execution Environment component tests
  - **Shared**: Shared component tests

- **docs**: Documentation
  - **api**: API documentation
  - **architecture**: Architecture documentation
  - **deployment**: Deployment documentation
  - **development**: Development documentation
  - **services**: Service documentation
  - **workflows**: Workflow documentation

- **scripts**: Scripts for building, testing, and deployment

- **config**: Configuration files

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
dotnet test tests/Services/NeoServiceLayer.Services.Randomness.Tests

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

## Adding a New Service

To add a new service to the Neo Service Layer, follow these steps:

1. **Create Service Interface**: Create an interface for the service in the Core project
2. **Create Service Implementation**: Create an implementation of the service in the Services project
3. **Create Enclave Code**: Create enclave code for the service in the Tee.Enclave project
4. **Create Host Code**: Create host code for the service in the Tee.Host project
5. **Create API Endpoints**: Create API endpoints for the service in the Api project
6. **Create Tests**: Create tests for the service in the tests project
7. **Update Documentation**: Update documentation for the service in the docs project

For detailed instructions, see [Adding New Services](../architecture/adding-new-services.md).

## References

- [Neo Service Layer Architecture](../architecture/README.md)
- [Neo Service Layer API](../api/README.md)
- [Neo Service Layer Services](../services/README.md)
- [Neo Service Layer Workflows](../workflows/README.md)
- [Neo N3 Documentation](https://docs.neo.org/)
- [NeoX Documentation](https://docs.neo.org/neox/)
- [Intel SGX Documentation](https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/overview.html)
- [Occlum LibOS Documentation](https://occlum.io/)
