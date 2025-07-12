# Neo Service Layer

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/r3e-network/neo-service-layer)
[![Test Coverage](https://img.shields.io/badge/coverage-85%25+-green)](https://github.com/r3e-network/neo-service-layer)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![Intel SGX](https://img.shields.io/badge/Intel-SGX-blue)](https://software.intel.com/en-us/sgx)
[![Docker](https://img.shields.io/badge/docker-ready-blue)](https://www.docker.com/)
[![Microservices](https://img.shields.io/badge/architecture-microservices-green)](https://microservices.io/)

A **production-ready, enterprise-grade microservices platform** leveraging Intel SGX with Occlum LibOS to provide secure, privacy-preserving services for the Neo blockchain ecosystem. Supporting both Neo N3 and NeoX (EVM-compatible) blockchains with comprehensive AI-powered services.

> **🎉 PRODUCTION READY** • **✅ CLEAN ARCHITECTURE** • **🚀 MICROSERVICES** • **🔧 FULLY TESTED**

## 🌟 Key Features

- **🏗️ Microservices Architecture** - Scalable, distributed service design
- **🔒 Intel SGX Integration** - Hardware-level security with trusted execution
- **⛓️ Multi-Blockchain Support** - Neo N3 and NeoX compatibility
- **🤖 AI-Powered Services** - Advanced pattern recognition and prediction
- **🔄 Service Discovery** - Consul-based service registry
- **📊 Observability** - Distributed tracing, metrics, and monitoring
- **🐳 Container Ready** - Full Docker and Kubernetes support
- **🧪 Comprehensive Testing** - Unit, integration, and performance tests

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Neo Service Layer Platform                  │
├─────────────────────────────────────────────────────────────────┤
│  🌐 API Gateway                                                │
│     • Rate Limiting         • Authentication & Authorization    │
│     • Request Routing       • Load Balancing                   │
├─────────────────────────────────────────────────────────────────┤
│  🔍 Service Discovery & Configuration                          │
│     • Consul Registry       • Dynamic Configuration            │
│     • Health Monitoring     • Circuit Breakers                 │
├─────────────────────────────────────────────────────────────────┤
│  📊 Observability Stack                                        │
│     • Jaeger Tracing        • Prometheus Metrics               │
│     • Grafana Dashboards    • Centralized Logging              │
├─────────────────────────────────────────────────────────────────┤
│  ⚙️ Core Microservices                                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │
│  │   Storage   │ │Key Mgmt (4) │ │    AI (2)   │ │ Cross-Chain │ │
│  │   Service   │ │Crypto Svcs  │ │ Services    │ │   Bridge    │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │
│  │ Notification│ │ Monitoring  │ │ Compliance  │ │   Oracle    │ │
│  │   Service   │ │   Service   │ │   Service   │ │   Service   │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│  🔒 Intel SGX + Occlum LibOS (Trusted Execution)              │
│     • Hardware Security      • Remote Attestation              │
│     • Confidential Computing • Secure Enclaves                 │
├─────────────────────────────────────────────────────────────────┤
│  ⛓️ Blockchain Integration Layer                               │
│     • Neo N3 Native         • NeoX EVM-Compatible              │
│     • Smart Contract APIs   • Cross-Chain Protocols           │
└─────────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- **.NET 9.0 SDK** or later
- **Docker** and **Docker Compose** 
- **Git** for source control
- **8GB+ RAM** for full microservices deployment

### Option 1: Complete Microservices Stack

```bash
# 1. Clone the repository
git clone https://github.com/r3e-network/neo-service-layer.git
cd neo-service-layer

# 2. Setup environment variables
cp .env.example .env
# Generate secure credentials (optional for production)
./scripts/generate-secure-credentials.sh

# 3. Start complete microservices stack
docker-compose -f docker-compose.microservices-complete.yml up -d

# 4. Verify services are running
docker ps
curl http://localhost:7000/health  # API Gateway
curl http://localhost:8500/v1/catalog/services  # Consul UI
```

### Option 2: Basic Development Setup

```bash
# 1. Clone and build
git clone https://github.com/r3e-network/neo-service-layer.git
cd neo-service-layer

# 2. Setup environment
cp .env.example .env
# Edit .env with your configuration

# 3. Start infrastructure services
docker-compose up -d

# 4. Run the API service
dotnet run --project src/Api/NeoServiceLayer.Api/

# 5. Access API at http://localhost:5000
```

### Option 3: Individual Service Development

```bash
# Build all services
dotnet build

# Run specific service (example: Storage Service)
dotnet run --project src/Services/NeoServiceLayer.Services.Storage/

# Run with specific configuration
dotnet run --project src/Services/NeoServiceLayer.Services.Storage/ --environment Development
```

## 🌐 Service Endpoints

### API Gateway (Port 7000)
- **🏠 Gateway**: `http://localhost:7000/` - Service information  
- **💚 Health**: `http://localhost:7000/health` - Health check
- **📊 Metrics**: `http://localhost:7000/metrics` - Prometheus metrics
- **📚 Swagger**: `http://localhost:7000/swagger` - API documentation

### Infrastructure Services
- **🔍 Consul**: `http://localhost:8500` - Service discovery UI
- **📊 Jaeger**: `http://localhost:16686` - Distributed tracing
- **📈 Grafana**: `http://localhost:3000` - Monitoring dashboards
- **🔴 Prometheus**: `http://localhost:9090` - Metrics collection

### Individual Services (Development)
- **🗄️ Storage Service**: `http://localhost:8081`
- **🔑 Key Management**: `http://localhost:8082` 
- **📧 Notification Service**: `http://localhost:8083`
- **🤖 AI Pattern Recognition**: `http://localhost:8084`
- **⚙️ Configuration Service**: `http://localhost:8085`

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run integration tests without infrastructure
dotnet test tests/Integration/NeoServiceLayer.Integration.Tests/ \
  --filter "FullyQualifiedName~MockedServiceTests"

# Run performance tests
dotnet test tests/Performance/NeoServiceLayer.Performance.Tests/
```

### Integration Testing

The project includes multiple testing approaches:

- **✅ In-Memory Tests** - No infrastructure required
- **✅ Mocked Service Tests** - HTTP mocking for service interactions  
- **✅ Container Tests** - Using Testcontainers when available
- **✅ Full Integration Tests** - Complete infrastructure testing

## 🐳 Docker Deployment

### Available Configurations

```bash
# Full microservices stack with observability
docker-compose -f docker-compose.microservices-complete.yml up -d

# Basic microservices setup
docker-compose -f docker-compose.microservices.yml up -d

# Development setup with infrastructure only
docker-compose up -d

# Individual service containers
docker build -f docker/microservices/services/storage/Dockerfile . -t neo-storage-service
docker run -p 8081:8080 neo-storage-service
```

### Scaling Services

```bash
# Scale specific services
docker-compose -f docker-compose.microservices-complete.yml up -d --scale storage-service=3
docker-compose -f docker-compose.microservices-complete.yml up -d --scale notification-service=2

# Check service instances
curl http://localhost:8500/v1/health/service/storage-service
```

## 🔧 Development

### Project Structure

```
neo-service-layer/
├── src/
│   ├── Api/                              # API Gateway and main API
│   ├── Core/                             # Core libraries and shared code
│   ├── Infrastructure/                   # Cross-cutting infrastructure
│   ├── Services/                         # Individual microservices
│   ├── Gateway/                          # API Gateway implementation
│   ├── SDK/                              # Client SDK for services
│   └── Tee/                              # Intel SGX/TEE components
├── docker/
│   ├── base/                             # Base Docker images
│   └── microservices/                    # Service-specific Dockerfiles
├── tests/
│   ├── Unit/                             # Unit tests by component
│   ├── Integration/                      # Integration and API tests
│   └── Performance/                      # Load and performance tests
├── docs/                                 # Documentation
├── scripts/                              # Build and deployment scripts
└── monitoring/                           # Observability configurations
```

### Adding New Services

1. **Create Service Project**
   ```bash
   mkdir src/Services/NeoServiceLayer.Services.YourService
   dotnet new webapi -n NeoServiceLayer.Services.YourService
   ```

2. **Implement Service Interface**
   ```csharp
   public interface IYourService : IService
   {
       Task<ServiceResult> YourMethodAsync();
   }
   ```

3. **Add Service Registration**
   ```csharp
   services.AddScoped<IYourService, YourService>();
   services.AddServiceDiscovery("your-service");
   ```

4. **Create Docker Configuration**
   ```dockerfile
   # docker/microservices/services/yourservice/Dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:9.0
   COPY src/Services/NeoServiceLayer.Services.YourService/ app/
   ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.YourService.dll"]
   ```

### Package Management

The project uses **Central Package Version Management**:

- All package versions defined in `Directory.Packages.props`
- No version conflicts across projects
- Consistent dependency management
- Security updates centrally managed

## 📊 Monitoring & Observability

### Distributed Tracing
- **Jaeger** for distributed tracing
- **OpenTelemetry** instrumentation
- Request correlation across services
- Performance bottleneck identification

### Metrics Collection
- **Prometheus** metrics collection
- **Grafana** dashboards for visualization
- Custom business metrics
- Infrastructure monitoring

### Service Health
- Health check endpoints for all services
- Consul health monitoring
- Circuit breaker patterns
- Automatic service recovery

## 🔐 Security Configuration

### Environment Setup

1. **Copy the example environment file**:
   ```bash
   cp .env.example .env
   ```

2. **Generate secure credentials** (for production):
   ```bash
   ./scripts/generate-secure-credentials.sh
   ```

3. **Configure JWT settings** in your environment:
   ```bash
   JWT_SECRET_KEY=your-secure-key-here
   JWT_ISSUER=neo-service-layer
   JWT_AUDIENCE=neo-service-layer-clients
   JWT_EXPIRATION_MINUTES=60
   ```

### Security Best Practices

- **Never commit** `.env` files to version control
- **Use strong**, randomly generated passwords and keys
- **Enable** all JWT validation options in production
- **Configure** appropriate token expiration times
- **Review** [SECURITY.md](SECURITY.md) for vulnerability reporting

## 🔒 Security Features

### Intel SGX Integration
- **Trusted Execution Environment** for sensitive operations
- **Remote Attestation** for enclave verification
- **Occlum LibOS** for confidential computing
- **Secure key management** within enclaves

### API Security
- **JWT Authentication** with standardized configuration
- **Rate limiting** and DDoS protection
- **Input validation** and sanitization
- **HTTPS/TLS** encryption
- **Environment-based secrets** management
- **Secure credential generation** scripts

### Network Security
- **Service mesh** integration ready
- **mTLS** between services
- **Network policies** for container communication
- **Secrets management** for sensitive data

## 📚 Documentation

### Core Documentation
- **[Architecture Guide](docs/architecture/README.md)** - System design and patterns
- **[Deployment Guide](docs/deployment/DEPLOYMENT_GUIDE.md)** - Production deployment
- **[API Reference](docs/api/README.md)** - Complete API documentation
- **[Services Guide](docs/services/README.md)** - Individual service documentation

### Development Guides
- **[Contributing Guidelines](CONTRIBUTING.md)** - How to contribute
- **[Testing Strategy](TESTING.md)** - Testing approaches and guidelines
- **[Coding Standards](docs/development/CODING_STANDARDS.md)** - Code quality standards

### Operations Guides
- **[Monitoring Setup](docs/monitoring/README.md)** - Observability configuration
- **[Troubleshooting](docs/troubleshooting/README.md)** - Common issues and solutions
- **[Security Guidelines](docs/security/README.md)** - Security best practices

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Workflow

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Make** your changes with comprehensive tests
4. **Ensure** all builds pass (`dotnet build`)
5. **Run** tests (`dotnet test`)
6. **Commit** your changes (`git commit -m 'Add amazing feature'`)
7. **Push** to the branch (`git push origin feature/amazing-feature`)
8. **Open** a Pull Request

### Code Quality

- All code must pass build and tests
- Follow established coding standards
- Include comprehensive tests for new features
- Update documentation for user-facing changes
- Ensure Docker builds succeed

## 🗺️ Roadmap

### Current Status: Production Ready ✅

**✅ Completed:**
- Microservices architecture implementation
- Service discovery and configuration
- API Gateway with rate limiting
- Comprehensive testing infrastructure
- Docker containerization
- Observability stack
- Clean project organization

**🔄 In Progress:**
- Enhanced Intel SGX integration
- Advanced AI service capabilities
- Cross-chain bridge implementation
- Kubernetes deployment manifests

**📋 Planned:**
- Service mesh integration (Istio/Linkerd)
- Advanced security features
- Enterprise governance tools
- Multi-cloud deployment support

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

- **📖 Documentation**: [docs/](docs/)
- **🐛 Issues**: [GitHub Issues](https://github.com/r3e-network/neo-service-layer/issues)
- **💬 Discussions**: [GitHub Discussions](https://github.com/r3e-network/neo-service-layer/discussions)
- **📧 Email**: support@r3e.network

## 🌟 Acknowledgments

- **Neo Team** for the foundational blockchain technology
- **Intel** for SGX technology and support
- **Open Source Community** for the amazing tools and libraries
- **Contributors** who help make this project better

---

**🚀 Ready for Production - Start Building the Future of Secure Blockchain Services!**

**Built with ❤️ for the Neo Ecosystem**