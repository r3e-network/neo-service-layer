# Neo Service Layer

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/neo-project/neo-service-layer)
[![Test Coverage](https://img.shields.io/badge/coverage-80%25+-green)](https://github.com/neo-project/neo-service-layer)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
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

## 🏢 Core Services

### **Blockchain Infrastructure**
- **🎲 Randomness Service**: Verifiable random number generation using SGX
- **🔮 Oracle Service**: Secure external data feeds with cryptographic proofs
- **🔐 Key Management Service**: Hardware-secured cryptographic key management
- **💾 Storage Service**: Encrypted data storage with compression and access control

### **AI & Analytics**
- **🧠 Pattern Recognition Service**: AI-powered fraud detection and behavioral analysis
- **📈 Prediction Service**: Machine learning forecasting and market analysis
- **⚖️ Fair Ordering Service**: MEV protection and transaction fairness
- **🔍 Zero-Knowledge Service**: Privacy-preserving computations

### **Enterprise Features**
- **📋 Compliance Service**: Regulatory compliance verification
- **🔔 Event Subscription Service**: Real-time blockchain event monitoring
- **⚙️ Automation Service**: Smart contract automation and triggers
- **🌉 Cross-Chain Service**: Multi-blockchain interoperability

### **Advanced Services**
- **🛡️ Proof of Reserve Service**: Cryptographic asset backing verification
- **💼 Abstract Account Service**: Account abstraction and gasless transactions
- **📊 Monitoring Service**: Comprehensive system health and metrics

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Neo Service Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Interactive Web Application (ASP.NET Core + Razor Pages)  │
├─────────────────────────────────────────────────────────────┤
│  RESTful API Layer (20+ Service Controllers)               │
├─────────────────────────────────────────────────────────────┤
│  Service Framework & Registry                               │
├─────────────────────────────────────────────────────────────┤
│  Microservices (AI, Oracle, Storage, etc.)                 │
├─────────────────────────────────────────────────────────────┤
│  Intel SGX + Occlum LibOS (Trusted Execution)              │
├─────────────────────────────────────────────────────────────┤
│  Neo N3 & Neo X Blockchain Integration                     │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- **.NET 9.0 SDK** or later
- **Docker** (for containerized deployment)
- **Git** for source control
- **Intel SGX SDK** (for enclave development)
- **Visual Studio 2025** or **VS Code** (recommended)

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
   - **Web Interface**: `http://localhost:5000` - Interactive service demonstrations
   - **Service Demo**: `http://localhost:5000/servicepages/servicedemo` - Live service testing
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

## 📚 Documentation

### **📖 Architecture & Design**
- [Architecture Overview](docs/architecture/ARCHITECTURE_OVERVIEW.md)
- [Service Framework](docs/architecture/service-framework.md)
- [Enclave Integration](docs/architecture/enclave-integration.md)

### **🌐 Web Application**
- [Web Application Guide](docs/web/WEB_APPLICATION_GUIDE.md)
- [Service Integration](docs/web/SERVICE_INTEGRATION.md)
- [Authentication & Security](docs/web/AUTHENTICATION.md)
- [API Reference](docs/web/API_REFERENCE.md)

### **🔧 Development**
- [Coding Standards](docs/development/CODING_STANDARDS.md)
- [Testing Guide](docs/development/testing-guide.md)
- [Adding New Services](docs/architecture/adding-new-services.md)

### **🚀 Deployment**
- [SGX Deployment Guide](docs/deployment/sgx-deployment-guide.md)
- [Occlum LibOS Guide](docs/deployment/occlum-libos-guide.md)
- [Production Configuration](docs/deployment/README.md)

### **📊 Services**
- [API Reference](docs/api/API_REFERENCE.md)
- [Service Documentation](docs/services/README.md)
- [Integration Examples](docs/workflows/README.md)

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