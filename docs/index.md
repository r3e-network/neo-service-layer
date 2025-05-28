# Neo Service Layer Documentation

Welcome to the comprehensive Neo Service Layer documentation. This documentation provides detailed information about the Neo Service Layer, its architecture, services, APIs, and development guidelines.

## Overview

The Neo Service Layer (NSL) is the most advanced blockchain infrastructure platform that leverages Intel SGX with Occlum LibOS enclaves to provide secure, privacy-preserving services for the Neo blockchain ecosystem. It supports both Neo N3 and NeoX (EVM-compatible) blockchains, offering unprecedented capabilities for decentralized applications.

## Service Portfolio

The Neo Service Layer consists of **15 focused services** organized into three categories:

### Core Infrastructure Services (11)

1. **[Randomness Service](services/randomness-service.md)** - Verifiable random number generation
2. **[Oracle Service](services/oracle-service.md)** - External data feeds and price aggregation
3. **[Key Management Service](services/key-management-service.md)** - Cryptographic key management
4. **[Compute Service](services/compute-service.md)** - Secure JavaScript execution
5. **[Storage Service](services/storage-service.md)** - Encrypted data storage
6. **[Compliance Service](services/compliance-service.md)** - Regulatory compliance automation
7. **[Event Subscription Service](services/event-subscription-service.md)** - Blockchain event monitoring
8. **[Automation Service](services/automation-service.md)** - Smart contract automation
9. **[Cross-Chain Service](services/cross-chain-service.md)** - Cross-chain interoperability
10. **[Proof of Reserve Service](services/proof-of-reserve-service.md)** - Asset backing verification
11. **[Zero-Knowledge Service](services/zero-knowledge-service.md)** - Privacy-preserving computations

### Specialized AI Services (2)

12. **[Prediction Service](services/prediction-service.md)** - AI-powered forecasting and sentiment analysis
13. **[Pattern Recognition Service](services/pattern-recognition-service.md)** - Fraud detection and behavioral analysis

### Advanced Infrastructure Services (2)

14. **[Fair Ordering Service](services/fair-ordering-service.md)** - Transaction fairness and MEV protection
15. **Future Services** - Additional services based on ecosystem needs

For detailed information about all services, see [Services Overview](services/README.md).

## Architecture

The Neo Service Layer is built on a sophisticated modular architecture designed for security, scalability, and extensibility:

### Core Components

- **[Service Framework](architecture/service-framework.md)** - Foundation for all services with registration, configuration, and lifecycle management
- **[Enclave Integration](architecture/enclave-integration.md)** - Intel SGX with Occlum LibOS integration for secure execution
- **[Blockchain Integration](architecture/blockchain-integration.md)** - Neo N3 and NeoX blockchain connectivity and interaction
- **[Persistent Storage](architecture/persistent-storage.md)** - Multi-provider encrypted storage with transaction support
- **[API Layer](api/README.md)** - RESTful API endpoints with authentication and rate limiting

### Key Features

- **Intel SGX + Occlum LibOS**: Hardware-level security for critical operations
- **Multi-Blockchain Support**: Native support for both Neo N3 and NeoX
- **Modular Design**: Easy to extend with new services
- **Production Ready**: Comprehensive testing, monitoring, and deployment support

For detailed architecture information, see [Architecture Overview](architecture/README.md).

## Getting Started

### Prerequisites

- **.NET 9.0 SDK** - Latest .NET runtime and development tools
- **Visual Studio 2022/2025** or **VS Code** - Recommended IDEs
- **Git** - Version control system
- **Intel SGX SDK** - For enclave development and testing
- **Occlum LibOS** - For secure enclave execution
- **Docker** (optional) - For containerized deployment

### Quick Start

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

4. **Start the services:**
```bash
dotnet run --project src/NeoServiceLayer.Api
```

5. **Access the API at:** `https://localhost:5001`

For detailed setup instructions, see [Development Guide](development/README.md).

## Documentation Structure

### 📚 **Core Documentation**
- **[Services](services/README.md)** - Complete service documentation and APIs
- **[Architecture](architecture/README.md)** - System architecture and design patterns
- **[API Reference](api/README.md)** - RESTful API documentation and SDKs
- **[Development](development/README.md)** - Development guides and best practices

### 🔧 **Development Resources**
- **[Adding New Services](architecture/adding-new-services.md)** - Guide for extending the platform
- **[Enclave Development](architecture/enclave-development.md)** - Intel SGX + Occlum LibOS development
- **[Testing Guide](development/testing-guide.md)** - Comprehensive testing strategies
- **[Deployment](deployment/README.md)** - Production deployment guidelines

### 📋 **Reference Materials**
- **[Security](security/README.md)** - Security considerations and best practices
- **[Troubleshooting](troubleshooting/README.md)** - Common issues and solutions
- **[FAQ](faq.md)** - Frequently asked questions
- **[Roadmap](roadmap/implementation-roadmap.md)** - Development roadmap and milestones

## Key Features

### 🔒 **Security First**
- Intel SGX with Occlum LibOS enclaves for hardware-level security
- Cryptographic verification of all operations
- Privacy-preserving computation and data processing

### 🌐 **Multi-Blockchain Support**
- Native Neo N3 integration with C# smart contracts
- NeoX (EVM-compatible) support with Solidity contracts
- Cross-chain interoperability and asset transfers

### 🤖 **Advanced Capabilities**
- AI-powered prediction and pattern recognition
- Zero-knowledge proof generation and verification
- Fair transaction ordering and MEV protection

### 🚀 **Production Ready**
- Comprehensive testing and monitoring
- High availability and scalability
- Professional documentation and support

## Contributing

We welcome contributions from the community! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on:

- Code of conduct and community standards
- Development workflow and pull request process
- Testing requirements and quality standards
- Documentation guidelines and standards

## Support

- **Documentation**: Complete guides and API references
- **Community**: Join our developer community for support
- **Issues**: Report bugs and request features on GitHub
- **Security**: Report security issues through responsible disclosure

---

**Neo Service Layer** - The most advanced blockchain infrastructure platform powered by Intel SGX with Occlum LibOS enclaves.
