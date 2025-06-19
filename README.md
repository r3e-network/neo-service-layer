# Neo Service Layer

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/neo-project/neo-service-layer)
[![Test Coverage](https://img.shields.io/badge/coverage-80%25+-green)](https://github.com/neo-project/neo-service-layer)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

The Neo Service Layer is a **production-ready, enterprise-grade platform** that leverages Intel SGX with Occlum LibOS to provide secure, privacy-preserving services for the Neo blockchain ecosystem. It supports both Neo N3 and NeoX (EVM-compatible) blockchains with comprehensive AI-powered services.

## 🌟 Key Features

- **🔒 Trusted Execution Environment**: Intel SGX with Occlum LibOS for maximum security
- **🌐 Interactive Web Application**: Full-featured web interface with real-time service interaction
- **🤖 AI-Powered Services**: Pattern recognition, fraud detection, and predictive analytics
- **⛓️ Multi-Chain Support**: Neo N3 and Neo X blockchain integration
- **🏗️ Microservices Architecture**: 20+ production-ready services
- **📊 Enterprise-Grade Quality**: 80%+ test coverage, comprehensive documentation
- **🚀 Production Ready**: Docker containerization, monitoring, and CI/CD

## 🎯 Key Features & Benefits

### **🔒 Enterprise-Grade Security**
- **Intel SGX + Occlum LibOS**: Hardware-level security for critical operations
- **Cryptographic Verification**: All operations cryptographically verifiable
- **Privacy Protection**: Zero-knowledge proofs and confidential computing
- **Compliance Ready**: Built-in regulatory compliance and audit capabilities

### **🌐 Complete Web Integration**
- **Interactive Interface**: Full-featured web application for all services
- **Real-time Testing**: Live service demonstrations and API testing
- **Professional UI**: Modern, responsive design with service-specific interfaces
- **Comprehensive Documentation**: Built-in API documentation and user guides

### **🤖 Advanced AI Capabilities**
- **Pattern Recognition**: AI-powered fraud detection and behavioral analysis
- **Predictive Analytics**: Machine learning forecasting and market analysis
- **Real-time Processing**: Stream processing for real-time AI inference
- **Model Management**: Versioning and deployment of ML models

### **⛓️ Multi-Blockchain Support**
- **Neo N3 Integration**: Native support for Neo N3 blockchain
- **NeoX Compatibility**: Full EVM-compatible functionality
- **Cross-Chain Bridge**: Seamless asset transfers between chains
- **Universal APIs**: Consistent API design across all blockchain types

### **🚀 Production Ready**
- **High Availability**: Designed for 99.9%+ uptime
- **Scalable Architecture**: Microservices with horizontal scaling
- **Comprehensive Testing**: 80%+ test coverage with automated CI/CD
- **Docker Support**: Containerized deployment and orchestration

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Neo Service Layer                        │
├─────────────────────────────────────────────────────────────┤
│  🌐 Interactive Web Application (ASP.NET Core 8.0)        │
│     • Service Demonstrations  • JWT Authentication         │
│     • Real-time Testing      • API Documentation           │
├─────────────────────────────────────────────────────────────┤
│  🔌 RESTful API Layer (20+ Service Controllers)           │
│     • Standardized APIs      • Swagger Documentation       │
│     • Authentication         • Rate Limiting               │
├─────────────────────────────────────────────────────────────┤
│  ⚙️ Service Framework & Registry                          │
│     • Service Lifecycle      • Dependency Injection        │
│     • Health Monitoring      • Configuration Management    │
├─────────────────────────────────────────────────────────────┤
│  🏢 Microservices Portfolio (20+ Services)                │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ │
│  │   Core (4)  │ │Security (4) │ │    AI (2)   │ │Advanced │ │
│  │Storage (3)  │ │Operations(4)│ │Infrastructure│ │ (2+)    │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘ │
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

- **.NET 8.0 SDK** or later
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

## 🌐 Interactive Web Application

The Neo Service Layer includes a **comprehensive web application** that provides:

### **🔴 Live Service Demonstrations**
- **Real-time Testing**: Interactive testing of all 20+ services
- **Professional UI**: Modern, responsive interface built with Bootstrap 5
- **Service Categories**: Organized into 6 categories for easy navigation
- **Direct Integration**: Real communication with actual service endpoints (no mock data)

### **🔐 Security & Authentication**
- **JWT Authentication**: Secure API access with role-based permissions
- **Demo Mode**: Easy token generation for testing and development
- **Environment Configuration**: Flexible authentication for different environments

### **📊 Real-time Monitoring**
- **Service Health**: Live status indicators for all services
- **Performance Metrics**: Real-time performance and response time monitoring
- **System Analytics**: Comprehensive system health dashboards

### **🎨 User Experience**
- **Cross-Platform**: Works seamlessly on desktop, tablet, and mobile devices
- **Service-Specific UI**: Tailored interfaces for each service type
- **Error Handling**: Comprehensive error reporting and user feedback
- **Documentation Integration**: Built-in help and API documentation

## 📊 Service Portfolio (20+ Services)

The Neo Service Layer provides a comprehensive suite of production-ready services organized into six categories:

### **🔧 Core Services (4)**
Essential blockchain operations:
1. **Key Management Service** - Generate and manage cryptographic keys securely
2. **Randomness Service** - Cryptographically secure random number generation
3. **Oracle Service** - External data feeds with cryptographic proofs
4. **Voting Service** - Decentralized voting and governance proposals

### **💾 Storage & Data Services (3)**
Data management and persistence:
5. **Storage Service** - Encrypted data storage and retrieval
6. **Backup Service** - Automated backup and restore operations
7. **Configuration Service** - Dynamic system configuration management

### **🔒 Security Services (4)**
Advanced security and privacy features:
8. **Zero Knowledge Service** - ZK proof generation and verification
9. **Abstract Account Service** - Smart contract account management
10. **Compliance Service** - Regulatory compliance and AML/KYC checks
11. **Proof of Reserve Service** - Cryptographic asset verification

### **⚙️ Operations Services (4)**
System management and monitoring:
12. **Automation Service** - Workflow automation and scheduling
13. **Monitoring Service** - System metrics and performance analytics
14. **Health Service** - System health diagnostics and reporting
15. **Notification Service** - Multi-channel notification system

### **🌐 Infrastructure Services (3)**
Multi-chain and compute services:
16. **Cross-Chain Service** - Multi-blockchain interoperability
17. **Compute Service** - Secure TEE computations
18. **Event Subscription Service** - Blockchain event monitoring

### **🤖 AI Services (2)**
Machine learning and analytics:
19. **Pattern Recognition Service** - AI-powered analysis and fraud detection
20. **Prediction Service** - Machine learning forecasting and analytics

### **🚀 Advanced Services (2+)**
Specialized blockchain features:
21. **Fair Ordering Service** - Transaction fairness and MEV protection
22. **Additional Services** - Continuously expanding based on ecosystem needs
   - **Swagger API**: `http://localhost:5000/swagger` - API documentation
   - **Health Check**: `http://localhost:5000/health` - System status

### Docker Deployment

```bash
# Build and start all services
docker-compose up -d

# View service status
docker-compose ps

# View logs
docker-compose logs -f
```

## 🌐 Web Application

The Neo Service Layer includes a **comprehensive web application** that provides an interactive interface to all services. The web application features:

### **🎯 Key Features:**
- **🔴 Live Service Demonstrations**: Interactive testing of all 20+ services
- **🔐 JWT Authentication**: Secure API access with role-based permissions
- **📊 Real-time Monitoring**: Service status and system health indicators
- **🎨 Professional UI**: Modern, responsive interface with service-specific designs
- **📱 Cross-Platform**: Works on desktop, tablet, and mobile devices

### **🛠️ Available Services:**

| Category | Services | Description |
|----------|----------|-------------|
| **🔧 Core** | Key Management, Randomness, Oracle, Voting | Essential blockchain operations |
| **💾 Storage** | Storage, Backup, Configuration | Data management and persistence |
| **🔒 Security** | Zero Knowledge, Abstract Account, Compliance, Proof of Reserve | Advanced security features |
| **⚙️ Operations** | Automation, Monitoring, Health, Notification | System management |
| **🌐 Infrastructure** | Cross-Chain, Compute, Event Subscription | Multi-chain and compute services |
| **🤖 AI** | Pattern Recognition, Prediction | Machine learning capabilities |

### **📋 Web Application Structure:**
```
src/Web/NeoServiceLayer.Web/
├── Controllers/           # 20+ API controllers for all services
├── Pages/                # Razor pages for web interface
│   ├── ServicePages/     # Service-specific pages
│   └── Shared/          # Shared layouts and components
├── Models/              # Request/response models
├── wwwroot/             # Static assets (CSS, JS, images)
└── Program.cs           # Application configuration
```

### **🚀 Getting Started with the Web App:**

1. **Start the application:**
   ```bash
   dotnet run --project src/Web/NeoServiceLayer.Web
   ```

2. **Navigate to the service demo:**
   ```
   http://localhost:5000/servicepages/servicedemo
   ```

3. **Explore services:**
   - Click any service card to test functionality
   - View real-time responses from actual services
   - Monitor system health and status

### **🔑 Authentication:**
The web application uses JWT authentication. A demo token is automatically generated for testing purposes with full permissions.

### **📈 Service Integration:**
All web demonstrations call **real service endpoints** - no simulated responses. Each service interaction:
- Authenticates via JWT tokens
- Calls actual service implementations
- Returns real data from the service layer
- Displays comprehensive error handling

## 📚 Comprehensive Documentation

### **🌐 Web Application Documentation**
- **[Web Application Guide](docs/web/WEB_APPLICATION_GUIDE.md)** - Complete introduction and setup guide
- **[Service Integration Guide](docs/web/SERVICE_INTEGRATION.md)** - Technical integration documentation
- **[Authentication & Security](docs/web/AUTHENTICATION.md)** - JWT authentication and security implementation
- **[API Reference](docs/web/API_REFERENCE.md)** - Complete API documentation for all 20+ services

### **🏗️ Architecture & Design**
- **[Architecture Overview](docs/architecture/ARCHITECTURE_OVERVIEW.md)** - System architecture and design patterns
- **[Service Framework Documentation](docs/architecture/service-framework.md)** - Service development framework
- **[Enclave Integration Guide](docs/architecture/enclave-integration.md)** - Intel SGX + Occlum LibOS integration

### **🛠️ Development Resources**
- **[Development Guide](docs/development/README.md)** - Development environment setup and workflow
- **[Coding Standards](docs/development/CODING_STANDARDS.md)** - Code style and best practices
- **[Testing Guide](docs/development/testing-guide.md)** - Testing strategies and frameworks
- **[Adding New Services](docs/architecture/adding-new-services.md)** - Guide for extending the platform

### **🚀 Deployment & Operations**
- **[Deployment Guide](docs/deployment/README.md)** - Production deployment instructions
- **[Configuration Management](docs/deployment/configuration.md)** - Environment configuration
- **[Security Considerations](docs/security/README.md)** - Security best practices
- **[Troubleshooting Guide](docs/troubleshooting/README.md)** - Common issues and solutions

### **📊 Service Documentation**
- **[Services Overview](docs/services/README.md)** - Complete documentation for all 20+ services
- **[API Reference](docs/api/README.md)** - RESTful API documentation
- **[Integration Examples](docs/workflows/README.md)** - Service integration patterns and examples

### **🔒 Security Documentation**
- **[Intel SGX Implementation](docs/security/sgx-implementation.md)** - SGX enclave development
- **[Occlum LibOS Guide](docs/security/occlum-guide.md)** - Occlum LibOS configuration
- **[Cryptographic Standards](docs/security/cryptography.md)** - Cryptographic implementation details

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
dotnet test --filter Category=Performance
```

### Test Categories

- **Unit Tests**: Individual component testing
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