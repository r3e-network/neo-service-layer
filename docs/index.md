# Neo Service Layer Documentation

Welcome to the comprehensive Neo Service Layer documentation. This documentation provides detailed information about the Neo Service Layer, its architecture, services, APIs, web application, and development guidelines.

## Overview

The Neo Service Layer (NSL) is a **production-ready, enterprise-grade platform** that leverages Intel SGX with Occlum LibOS enclaves to provide secure, privacy-preserving services for the Neo blockchain ecosystem. It supports both Neo N3 and NeoX (EVM-compatible) blockchains with a comprehensive **interactive web application** and full API access.

## 🌐 Interactive Web Application

The Neo Service Layer includes a **full-featured web application** that provides:

- **🔴 Live Service Demonstrations**: Interactive testing of all 26 services
- **🔐 JWT Authentication**: Secure API access with role-based permissions
- **📊 Real-time Monitoring**: Service status and system health indicators
- **🎨 Professional UI**: Modern, responsive interface with service-specific designs
- **📱 Cross-Platform**: Works on desktop, tablet, and mobile devices

**Quick Access:**
- **Web Interface**: `http://localhost:5000` - Main application interface
- **Service Demo**: `http://localhost:5000/servicepages/servicedemo` - Interactive service testing
- **API Documentation**: `http://localhost:5000/swagger` - Complete API reference

For complete web application documentation, see **[Web Application Guide](web/WEB_APPLICATION_GUIDE.md)**.

## Service Portfolio

The Neo Service Layer consists of **26 production-ready services** organized into seven categories:

### **🔧 Core Services (4)**
Essential blockchain operations:
1. **[Key Management Service](services/key-management-service.md)** - Generate and manage cryptographic keys securely
2. **[Randomness Service](services/randomness-service.md)** - Cryptographically secure random number generation
3. **[Oracle Service](services/oracle-service.md)** - External data feeds with cryptographic proofs
4. **[Voting Service](services/voting-service.md)** - Decentralized voting and governance proposals

### **💾 Storage & Data Services (3)**
Data management and persistence:
5. **[Storage Service](services/storage-service.md)** - Encrypted data storage and retrieval
6. **[Backup Service](services/backup-service.md)** - Automated backup and restore operations
7. **[Configuration Service](services/configuration-service.md)** - Dynamic system configuration management

### **🔒 Security Services (6)**
Advanced security and privacy features:
8. **[Zero Knowledge Service](services/zero-knowledge-service.md)** - ZK proof generation and verification
9. **[Abstract Account Service](services/abstract-account-service.md)** - Smart contract account management
10. **[Compliance Service](services/compliance-service.md)** - Regulatory compliance and AML/KYC checks
11. **[Proof of Reserve Service](services/proof-of-reserve-service.md)** - Cryptographic asset verification
12. **[Secrets Management Service](services/secrets-management-service.md)** - Secure secrets storage and rotation
13. **[Social Recovery Service](services/social-recovery-service.md)** - Decentralized account recovery with reputation-based guardians

### **⚙️ Operations Services (4)**
System management and monitoring:
12. **[Automation Service](services/automation-service.md)** - Workflow automation and scheduling
13. **[Monitoring Service](services/monitoring-service.md)** - System metrics and performance analytics
14. **[Health Service](services/health-service.md)** - System health diagnostics and reporting
15. **[Notification Service](services/notification-service.md)** - Multi-channel notification system

### **🌐 Infrastructure Services (4)**
Multi-chain and compute services:
16. **[Cross-Chain Service](services/cross-chain-service.md)** - Multi-blockchain interoperability
17. **[Compute Service](services/compute-service.md)** - Secure TEE computations
18. **[Event Subscription Service](services/event-subscription-service.md)** - Blockchain event monitoring
19. **[Smart Contracts Service](services/smart-contracts-service.md)** - Smart contract deployment and management

### **🤖 AI Services (2)**
Machine learning and analytics:
20. **[Pattern Recognition Service](services/pattern-recognition-service.md)** - AI-powered analysis and fraud detection
21. **[Prediction Service](services/prediction-service.md)** - Machine learning forecasting and analytics

### **🚀 Advanced Services (3)**
Specialized blockchain features:
22. **[Fair Ordering Service](services/fair-ordering-service.md)** - Transaction fairness and MEV protection
23. **[Attestation Service](services/attestation-service.md)** - SGX remote attestation and verification
24. **[Network Security Service](services/network-security-service.md)** - Secure enclave network communication

For detailed information about all services, see [Services Overview](services/README.md).

## Architecture

The Neo Service Layer is built on a modern, production-ready architecture that combines enterprise-grade security with developer-friendly interfaces:

### System Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    Neo Service Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Interactive Web Application (ASP.NET Core + Razor Pages)  │
├─────────────────────────────────────────────────────────────┤
│  RESTful API Layer (26 Service Controllers)                │
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

### Core Components

- **[Web Application](web/WEB_APPLICATION_GUIDE.md)** - Interactive interface for all services with real-time testing
- **[Service Framework](architecture/service-framework.md)** - Foundation for all services with registration and lifecycle management
- **[API Layer](api/README.md)** - Comprehensive RESTful APIs with JWT authentication and Swagger documentation
- **[Enclave Integration](architecture/enclave-integration.md)** - Intel SGX with Occlum LibOS integration for secure execution
- **[Blockchain Integration](architecture/blockchain-integration.md)** - Neo N3 and NeoX blockchain connectivity

### Key Features

- **🌐 Interactive Web Interface**: Complete service testing and management interface
- **🔒 Intel SGX + Occlum LibOS**: Hardware-level security for critical operations
- **⛓️ Multi-Blockchain Support**: Native support for both Neo N3 and NeoX
- **🏗️ Microservices Architecture**: 26 independent, scalable services
- **🔐 JWT Authentication**: Secure API access with role-based permissions
- **📊 Real-time Monitoring**: Comprehensive health and performance monitoring
- **🚀 Production Ready**: Docker containerization, CI/CD, and comprehensive testing

For detailed architecture information, see [Architecture Overview](architecture/ARCHITECTURE_OVERVIEW.md).

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
