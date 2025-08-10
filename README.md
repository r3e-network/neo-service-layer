# Neo Service Layer

A comprehensive microservices platform for building secure, scalable blockchain applications on Neo N3 and Neo X.

## Overview

Neo Service Layer provides a production-ready framework for building microservices with advanced features including:

- 🔐 **Intel SGX Enclave Support** - Secure computation in trusted execution environments
- ⛓️ **Blockchain Integration** - Native support for Neo N3 and Neo X
- 🚀 **High Performance** - Optimized for scalability with auto-scaling capabilities
- 🛡️ **Enterprise Security** - JWT auth, encryption, secure key management
- 📊 **Full Observability** - Prometheus metrics, distributed tracing, health monitoring
- 🔧 **Developer Friendly** - Service generator, comprehensive documentation

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker & Docker Compose
- Kubernetes (for production deployment)
- Intel SGX SDK (optional, for enclave features)

### Create a New Service

```bash
# Basic service
./scripts/create-new-service.sh MyService

# Service with blockchain support
./scripts/create-new-service.sh BlockchainService --blockchain

# Service with enclave support
./scripts/create-new-service.sh SecureService --enclave
```

### Build and Run

```bash
# Build all services
dotnet build

# Run tests
dotnet test

# Start local development environment
docker-compose -f docker-compose.dev.yml up

# Deploy to Kubernetes
./scripts/production-deployment.sh
```

## Architecture

The platform consists of 40+ microservices built on a robust service framework:

### Core Services
- **Storage** - Encrypted data storage with multiple backends
- **Key Management** - Secure cryptographic key lifecycle
- **Oracle** - External data integration for smart contracts
- **Notification** - Multi-channel notification delivery
- **Health & Monitoring** - System observability

### Specialized Services
- **Smart Contracts** - Neo blockchain integration
- **AI Services** - Prediction and pattern recognition
- **Zero Knowledge** - Privacy-preserving computations
- **Social Recovery** - Secure account recovery
- **Cross-Chain** - Inter-blockchain communication

### Infrastructure
- **Service Framework** - Base classes and common patterns
- **Resilience** - Circuit breakers, retries, timeouts
- **Security** - Authentication, authorization, encryption
- **Persistence** - Entity Framework Core with multiple providers

## Key Features

### 🏗️ Service Framework
- Consistent service patterns
- Automatic health checks and metrics
- Dependency management
- Service discovery with Consul
- Built-in resilience patterns

### 🔒 Security
- JWT authentication
- Role-based authorization
- Intel SGX enclave support
- End-to-end encryption
- Secure secrets management
- Network policies

### 📈 Scalability
- Horizontal pod autoscaling
- Resource quotas and limits
- Service mesh ready
- Database sharding support
- Caching strategies

### 🔍 Observability
- Prometheus metrics
- Grafana dashboards
- Distributed tracing
- Centralized logging
- Real-time health monitoring

## Documentation

- [Project Structure](docs/PROJECT_STRUCTURE.md) - Detailed project organization
- [Service Framework Guide](docs/guides/SERVICE_FRAMEWORK_GUIDE.md) - Framework documentation
- [Deployment Guide](docs/deployment/) - Production deployment instructions
- [API Documentation](docs/api/) - Service API references

## Development

### Project Structure
```
neo-service-layer/
├── src/                    # Source code
│   ├── Core/              # Framework and shared components
│   ├── Services/          # Microservices
│   ├── Infrastructure/    # Cross-cutting concerns
│   └── AI/                # AI/ML services
├── tests/                  # Test projects
├── contracts-neo-n3/       # Smart contracts
├── k8s/                    # Kubernetes configs
├── scripts/                # Utility scripts
└── docs/                   # Documentation
```

### Creating Services

Services can be created in under 5 minutes using the generator:

```bash
./scripts/create-new-service.sh ServiceName [options]

Options:
  --enclave        Intel SGX support
  --blockchain     Blockchain integration
  --ai             AI service base
  --crypto         Cryptographic operations
  --data           Data-intensive service
```

### Testing

```bash
# Unit tests
dotnet test --filter Category=Unit

# Integration tests
dotnet test --filter Category=Integration

# Run all tests
dotnet test
```

### Local Development

```bash
# Start infrastructure (PostgreSQL, Redis, RabbitMQ)
docker-compose -f docker-compose.dev.yml up -d

# Run a specific service
cd src/Services/NeoServiceLayer.Services.Storage
dotnet run

# Or run all services
docker-compose up
```

## Deployment

### Kubernetes

```bash
# Generate secrets
./scripts/generate-secrets.sh

# Deploy to Kubernetes
kubectl apply -f k8s/base/
kubectl apply -f k8s/services/

# Or use the deployment script
./scripts/production-deployment.sh
```

### Configuration

Services use hierarchical configuration:
- `appsettings.json` - Base configuration
- `appsettings.{Environment}.json` - Environment overrides
- Environment variables - Runtime overrides
- Kubernetes ConfigMaps/Secrets - Production config

## Monitoring

### Health Checks
- `GET /health` - Basic health check
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

### Metrics
- `GET /metrics` - Prometheus metrics endpoint
- Custom business metrics per service
- Resource usage tracking

### Dashboards
Pre-configured Grafana dashboards for:
- Service overview
- Performance metrics
- Resource usage
- Business KPIs

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Guidelines
- Follow the existing code patterns
- Write unit tests for new features
- Update documentation
- Use the service generator for new services

## Support

- [Issues](https://github.com/your-org/neo-service-layer/issues) - Report bugs or request features
- [Discussions](https://github.com/your-org/neo-service-layer/discussions) - Ask questions and share ideas
- [Wiki](https://github.com/your-org/neo-service-layer/wiki) - Additional documentation

## License

[Your License Here]

## Acknowledgments

Built with:
- .NET 9.0
- Neo Blockchain
- Intel SGX
- Kubernetes
- And many other excellent open source projects