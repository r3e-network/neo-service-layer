# Neo Service Layer

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/neo-project/neo-service-layer)
[![Test Coverage](https://img.shields.io/badge/coverage-80%25+-green)](https://github.com/neo-project/neo-service-layer)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![Intel SGX](https://img.shields.io/badge/Intel-SGX-blue)](https://software.intel.com/en-us/sgx)
[![Docker](https://img.shields.io/badge/docker-ready-blue)](https://www.docker.com/)

The Neo Service Layer is a **production-ready, enterprise-grade platform** that leverages Intel SGX with Occlum LibOS to provide secure, privacy-preserving services for the Neo blockchain ecosystem. It supports both Neo N3 and NeoX (EVM-compatible) blockchains with comprehensive AI-powered services.

> **Enterprise Ready** • **Hardware Security** • **Multi-Chain** • **AI-Powered** • **Production Tested**

## 🌟 Key Features

- **🔒 Trusted Execution Environment**: Intel SGX with Occlum LibOS for maximum security
- **🌐 Interactive Web Application**: Full-featured web interface with real-time service interaction
- **🤖 AI-Powered Services**: Pattern recognition, fraud detection, and predictive analytics
- **⛓️ Multi-Chain Support**: Neo N3 and Neo X blockchain integration
- **🏗️ Microservices Architecture**: 25 production-ready services
- **📊 Enterprise-Grade Quality**: 80%+ test coverage, comprehensive documentation
- **🚀 Production Ready**: Docker containerization, monitoring, and CI/CD

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Neo Service Layer                        │
├─────────────────────────────────────────────────────────────┤
│  🌐 Interactive Web Application (ASP.NET Core 9.0)        │
│     • Service Demonstrations  • JWT Authentication         │
│     • Real-time Testing      • API Documentation           │
├─────────────────────────────────────────────────────────────┤
│  🔌 RESTful API Layer (24 Service Controllers)            │
│     • Standardized APIs      • Swagger Documentation       │
│     • Authentication         • Rate Limiting               │
├─────────────────────────────────────────────────────────────┤
│  ⚙️ Service Framework & Registry                          │
│     • Service Lifecycle      • Dependency Injection        │
│     • Health Monitoring      • Configuration Management    │
├─────────────────────────────────────────────────────────────┤
│  🏢 Microservices Portfolio (25 Services)                 │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ │
│  │   Core (4)  │ │Security (5) │ │    AI (2)   │ │Advanced │ │
│  │Storage (3)  │ │Operations(4)│ │Infrastructure│ │   (1)   │ │
│  └─────────────┘ └─────────────┘ └───(4)────────┘ └─────────┘ │
├─────────────────────────────────────────────────────────────┤
│  🔒 Intel SGX + Occlum LibOS (Trusted Execution)          │
│     • Hardware Security      • Remote Attestation          │
│     • Confidential Computing • Secure Enclaves             │
├─────────────────────────────────────────────────────────────┤
│  ⛓️ Multi-Blockchain Integration                          │
│     • Neo N3 Native         • NeoX EVM-Compatible          │
│     • Cross-Chain Bridge    • Universal APIs               │
└─────────────────────────────────────────────────────────────┘
```

For detailed architecture information, see [Architecture Overview](docs/architecture/ARCHITECTURE_OVERVIEW.md).

## 🚀 Quick Start

### Prerequisites

- **.NET 9.0 SDK** or later
- **Docker** (for containerized deployment)
- **Git** for source control
- **Intel SGX SDK** (for enclave development)
- **Visual Studio 2022/2025** or **VS Code** (recommended)

### Installation

1. **Clone the repository:**
```bash
git clone https://github.com/neo-project/neo-service-layer.git
cd neo-service-layer
```

2. **Build the solution:**
```bash
dotnet build
```

3. **Run tests:**
```bash
dotnet test
```

4. **Start the Web Application:**
```bash
dotnet run --project src/Web/NeoServiceLayer.Web
```

5. **Access the Application:**
   - **🌐 Web Interface**: `http://localhost:5000` - Main application dashboard
   - **🎮 Service Demo**: `http://localhost:5000/servicepages/servicedemo` - Interactive service testing
   - **📚 API Documentation**: `http://localhost:5000/swagger` - Complete API reference

## 📊 Service Portfolio (25 Services)

The Neo Service Layer provides a comprehensive suite of production-ready services organized into six categories:

### **🔧 Core Services (4)**
1. **Key Management Service** - Generate and manage cryptographic keys securely
2. **Randomness Service** - Cryptographically secure random number generation
3. **Oracle Service** - External data feeds with cryptographic proofs
4. **Voting Service** - Advanced voting with ML strategies and council node monitoring

### **💾 Storage & Data Services (3)**
5. **Storage Service** - Encrypted data storage and retrieval
6. **Backup Service** - Automated backup and restore operations
7. **Configuration Service** - Dynamic system configuration management

### **🔒 Security Services (5)**
8. **Zero Knowledge Service** - ZK proof generation and verification
9. **Abstract Account Service** - Smart contract account management
10. **Compliance Service** - Regulatory compliance and AML/KYC checks
11. **Proof of Reserve Service** - Cryptographic asset verification
12. **Secrets Management Service** - Secure secrets storage and rotation

### **⚙️ Operations Services (4)**
13. **Automation Service** - Workflow automation and scheduling
14. **Monitoring Service** - System metrics and performance analytics
15. **Health Service** - System health diagnostics and reporting
16. **Notification Service** - Multi-channel notification system

### **🌐 Infrastructure Services (4)**
17. **Cross-Chain Service** - Multi-blockchain interoperability
18. **Compute Service** - Secure TEE computations
19. **Event Subscription Service** - Blockchain event monitoring
20. **Smart Contracts Service** - Smart contract deployment and management

### **🤖 AI Services (2)**
21. **Pattern Recognition Service** - AI-powered analysis and fraud detection
22. **Prediction Service** - Machine learning forecasting and analytics

### **🚀 Advanced Services (3)**
23. **Fair Ordering Service** - Transaction fairness and MEV protection
24. **Attestation Service** - SGX remote attestation and verification
25. **Network Security Service** - Secure enclave network communication

## 🌐 Interactive Web Application

The Neo Service Layer includes a **comprehensive web application** that provides:

### **🔴 Live Service Demonstrations**
- **Real-time Testing**: Interactive testing of all 25 services
- **Professional UI**: Modern, responsive interface built with Bootstrap 5
- **Service Categories**: Organized into 7 categories for easy navigation
- **Direct Integration**: Real communication with actual service endpoints

### **🔐 Security & Authentication**
- **JWT Authentication**: Secure API access with role-based permissions
- **Demo Mode**: Easy token generation for testing and development
- **Environment Configuration**: Flexible authentication for different environments

### **📊 Real-time Monitoring**
- **Service Health**: Live status indicators for all services
- **Performance Metrics**: Real-time performance and response time monitoring
- **System Analytics**: Comprehensive system health dashboards

## 🧪 Testing

The project includes comprehensive testing with **80%+ coverage**:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
```

### Test Categories

- **Unit Tests**: Individual component testing (1,000+ tests)
- **Integration Tests**: Cross-service workflows
- **Performance Tests**: Load testing and benchmarks
- **Security Tests**: Enclave and cryptographic validation

## 🔒 Security Features

- **Intel SGX**: Hardware-based trusted execution environment
- **Occlum LibOS**: Secure library operating system
- **Remote Attestation**: Cryptographic proof of execution integrity
- **Hardware Key Management**: SGX-secured cryptographic operations
- **Encrypted Storage**: AES-256-GCM encryption at rest
- **Secure Communication**: TLS 1.3 with certificate pinning

## 🏢 Production Features

- **🔄 High Availability**: Service redundancy and failover
- **📊 Monitoring**: Prometheus metrics and health checks
- **🐳 Containerization**: Docker with multi-stage builds
- **🚀 CI/CD**: Automated testing and deployment
- **📈 Scalability**: Horizontal scaling support
- **🛡️ Security**: Multi-layer security validation

## 📚 Documentation

### **🏗️ Architecture & Design**
- **[Architecture Overview](docs/architecture/ARCHITECTURE_OVERVIEW.md)** - System architecture and design patterns
- **[Service Framework Documentation](docs/architecture/service-framework.md)** - Service development framework
- **[Enclave Integration Guide](docs/architecture/enclave-integration.md)** - Intel SGX + Occlum LibOS integration

### **🛠️ Development Resources**
- **[Development Guide](docs/development/README.md)** - Development environment setup and workflow
- **[Coding Standards](docs/development/CODING_STANDARDS.md)** - Code style and best practices
- **[Testing Guide](docs/development/testing-guide.md)** - Testing strategies and frameworks

### **🚀 Deployment & Operations**
- **[Deployment Guide](docs/deployment/README.md)** - Production deployment instructions
- **[Security Guide](docs/security/README.md)** - Security best practices
- **[Troubleshooting Guide](docs/troubleshooting/README.md)** - Common issues and solutions

### **📊 Service Documentation**
- **[Services Overview](docs/services/README.md)** - Complete documentation for all 25 services
- **[API Reference](docs/api/README.md)** - RESTful API documentation

## 🐳 Docker Deployment

```bash
# Build and start all services
docker-compose up -d

# View service status
docker-compose ps

# View logs
docker-compose logs -f
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Workflow

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

- **📖 Documentation**: [docs/](docs/)
- **🐛 Issues**: [GitHub Issues](https://github.com/neo-project/neo-service-layer/issues)
- **💬 Discussions**: [GitHub Discussions](https://github.com/neo-project/neo-service-layer/discussions)
- **📧 Email**: support@neo.org

## 🗺️ Roadmap

- **Q1 2025**: Production deployment and monitoring
- **Q2 2025**: Advanced AI features and cross-chain expansion
- **Q3 2025**: Enterprise partnerships and ecosystem growth
- **Q4 2025**: Next-generation privacy features

---

**Built with ❤️ by the Neo Team**