# Neo Service Layer - Development Guide

## 1. Introduction

This document provides guidelines and instructions for developing the Neo Service Layer (NSL). It covers the development environment setup, coding standards, testing procedures, and contribution guidelines.

## 2. Development Environment Setup

### 2.1 Prerequisites

- Windows 10/11 or Ubuntu 20.04+
- .NET 9.0 SDK
- Docker Desktop
- Visual Studio 2022 or Visual Studio Code
- Git
- Intel SGX SDK
- Occlum

### 2.2 Windows Setup

#### 2.2.1 Install .NET 9.0 SDK

Download and install the .NET 9.0 SDK from the [official .NET website](https://dotnet.microsoft.com/download/dotnet/9.0).

#### 2.2.2 Install Docker Desktop

Download and install Docker Desktop from the [official Docker website](https://www.docker.com/products/docker-desktop).

#### 2.2.3 Install Visual Studio 2022

Download and install Visual Studio 2022 from the [official Visual Studio website](https://visualstudio.microsoft.com/vs/).

#### 2.2.4 Install Git

Download and install Git from the [official Git website](https://git-scm.com/downloads).

#### 2.2.5 Install Intel SGX SDK

Download and install the Intel SGX SDK from the [official Intel website](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions/sdk.html).

### 2.3 Ubuntu Setup

#### 2.3.1 Install .NET 9.0 SDK

```bash
wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.100
export PATH=$PATH:$HOME/.dotnet
```

#### 2.3.2 Install Docker

```bash
sudo apt-get update
sudo apt-get install -y apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
sudo apt-get update
sudo apt-get install -y docker-ce
```

#### 2.3.3 Install Visual Studio Code

```bash
sudo apt-get install -y wget gpg
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg
sudo install -D -o root -g root -m 644 packages.microsoft.gpg /etc/apt/keyrings/packages.microsoft.gpg
sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/keyrings/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'
rm -f packages.microsoft.gpg
sudo apt-get update
sudo apt-get install -y code
```

#### 2.3.4 Install Git

```bash
sudo apt-get install -y git
```

#### 2.3.5 Install Intel SGX SDK

```bash
wget https://download.01.org/intel-sgx/sgx-linux/2.15/distro/ubuntu20.04-server/sgx_linux_x64_sdk_2.15.100.3.bin
chmod +x sgx_linux_x64_sdk_2.15.100.3.bin
./sgx_linux_x64_sdk_2.15.100.3.bin --prefix=/opt/intel
source /opt/intel/sgxsdk/environment
```

#### 2.3.6 Install Occlum

```bash
docker pull occlum/occlum:0.29.0-ubuntu20.04
```

### 2.4 Clone Repository

```bash
git clone https://github.com/your-org/neo-service-layer.git
cd neo-service-layer
```

### 2.5 Build Solution

```bash
dotnet restore
dotnet build
```

### 2.6 Run Tests

```bash
dotnet test
```

## 3. Project Structure

The Neo Service Layer solution is structured as follows:

```
NeoServiceLayer/
├── src/
│   ├── NeoServiceLayer.Api/                # API project
│   ├── NeoServiceLayer.Core/               # Core domain models and services
│   ├── NeoServiceLayer.Infrastructure/     # Infrastructure services
│   ├── NeoServiceLayer.Tee.Host/           # TEE host process
│   │   ├── Occlum/                         # Occlum integration
│   │   └── Storage/                        # Storage implementations
│   ├── NeoServiceLayer.Tee.Enclave/        # TEE enclave code (.NET and C++)
│   │   └── Enclave/                        # Native enclave code (C++)
│   └── NeoServiceLayer.Shared/             # Shared components
├── tests/
│   ├── NeoServiceLayer.Api.Tests/
│   ├── NeoServiceLayer.Core.Tests/
│   ├── NeoServiceLayer.Infrastructure.Tests/
│   ├── NeoServiceLayer.Tee.Host.Tests/
│   ├── NeoServiceLayer.Tee.Enclave.Tests/
│   ├── NeoServiceLayer.Occlum.Tests/       # Occlum-specific tests
│   └── NeoServiceLayer.Integration.Tests/  # Integration tests
├── docs/
│   ├── Architecture.md
│   ├── API.md
│   ├── Deployment.md
│   ├── Development.md
│   └── persistent-storage.md               # Persistent storage documentation
├── examples/
│   ├── SmartContractExecution/
│   └── JavaScriptFunctions/
└── deployment/
    └── docker/                             # Docker configuration files
```

## 4. Coding Standards

### 4.1 C# Coding Standards

- Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use PascalCase for class names, method names, and property names
- Use camelCase for local variables and parameters
- Use UPPER_CASE for constants
- Use `_camelCase` for private fields
- Use meaningful names for variables, methods, and classes
- Keep methods short and focused on a single responsibility
- Use XML documentation comments for public APIs
- Use async/await for asynchronous operations
- Use nullable reference types
- Use pattern matching where appropriate

### 4.2 C++ Coding Standards

- Follow the [Google C++ Style Guide](https://google.github.io/styleguide/cppguide.html)
- Use CamelCase for class names
- Use snake_case for function names, variable names, and file names
- Use UPPER_CASE for constants and macros
- Use meaningful names for variables, functions, and classes
- Keep functions short and focused on a single responsibility
- Use comments to explain complex code
- Use smart pointers instead of raw pointers
- Use const wherever possible
- Use modern C++ features (C++17 or later)

### 4.3 Git Workflow

- Use feature branches for new features
- Use bugfix branches for bug fixes
- Use pull requests for code reviews
- Squash commits before merging
- Write meaningful commit messages
- Keep commits small and focused
- Rebase feature branches on main before merging

## 5. Testing

### 5.1 Unit Testing

- Write unit tests for all public methods
- Use xUnit for .NET code
- Use GoogleTest for C++ code
- Use Moq for mocking in .NET
- Use GoogleMock for mocking in C++
- Aim for >80% code coverage
- Run unit tests before committing code

### 5.2 Integration Testing

- Write integration tests for component interactions
- Use xUnit for integration tests
- Use Docker for integration test environment
- Run integration tests in CI/CD pipeline

### 5.3 Performance Testing

- Write performance tests for critical paths
- Use BenchmarkDotNet for .NET performance tests
- Use Google Benchmark for C++ performance tests
- Run performance tests in CI/CD pipeline

### 5.4 Security Testing

- Use static code analysis tools
- Use dynamic code analysis tools
- Use fuzz testing for critical components
- Run security tests in CI/CD pipeline

## 6. Debugging

### 6.1 Debugging .NET Code

- Use Visual Studio debugger
- Use Visual Studio Code debugger
- Use dotnet-trace for performance analysis
- Use dotnet-dump for memory analysis

### 6.2 Debugging C++ Code

- Use Visual Studio debugger
- Use GDB for Linux debugging
- Use Valgrind for memory analysis
- Use Intel VTune for performance analysis

### 6.3 Debugging SGX Enclaves

- Use SGX Debug Mode
- Use SGX GDB
- Use SGX Enclave Debugger
- Use SGX Enclave Memory Dump

## 7. Contribution Guidelines

### 7.1 Pull Request Process

1. Create a feature branch from main
2. Implement the feature or fix the bug
3. Write tests for the feature or bug fix
4. Run all tests locally
5. Create a pull request
6. Address review comments
7. Squash commits
8. Merge the pull request

### 7.2 Code Review Guidelines

- Review code for correctness
- Review code for performance
- Review code for security
- Review code for maintainability
- Review code for testability
- Review code for documentation
- Review code for adherence to coding standards
