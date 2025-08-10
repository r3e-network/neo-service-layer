# Neo Service Layer - Project Structure

## Overview

The Neo Service Layer is organized as a clean, modular microservices architecture built on .NET 9.0. This document describes the project structure after cleanup and organization.

## Directory Structure

```
neo-service-layer/
├── src/                           # Source code
│   ├── Core/                      # Core framework and shared components
│   │   ├── NeoServiceLayer.Core/  # Core interfaces and models
│   │   ├── NeoServiceLayer.ServiceFramework/  # Service base classes and framework
│   │   └── NeoServiceLayer.Shared/  # Shared utilities
│   │
│   ├── Services/                  # Microservices
│   │   ├── NeoServiceLayer.Services.Storage/
│   │   ├── NeoServiceLayer.Services.KeyManagement/
│   │   ├── NeoServiceLayer.Services.Notification/
│   │   ├── NeoServiceLayer.Services.Oracle/
│   │   └── ... (40+ services)
│   │
│   ├── AI/                        # AI/ML services
│   │   ├── NeoServiceLayer.AI.Prediction/
│   │   └── NeoServiceLayer.AI.PatternRecognition/
│   │
│   ├── Infrastructure/            # Infrastructure components
│   │   ├── NeoServiceLayer.Infrastructure/
│   │   ├── NeoServiceLayer.Infrastructure.Persistence/
│   │   ├── NeoServiceLayer.Infrastructure.Resilience/
│   │   └── NeoServiceLayer.Infrastructure.Security/
│   │
│   ├── Tee/                       # Trusted Execution Environment
│   │   ├── NeoServiceLayer.Tee.Host/
│   │   └── NeoServiceLayer.Tee.Enclave/
│   │
│   ├── Blockchain/                # Blockchain integrations
│   │   ├── NeoServiceLayer.Neo.N3/
│   │   └── NeoServiceLayer.Neo.X/
│   │
│   ├── Advanced/                  # Advanced features
│   │   └── NeoServiceLayer.Advanced.FairOrdering/
│   │
│   └── Api/                       # API Gateway
│       └── NeoServiceLayer.Api/
│
├── tests/                         # Test projects
│   ├── Unit/                      # Unit tests
│   ├── Integration/               # Integration tests
│   └── Services/                  # Service-specific tests
│
├── contracts-neo-n3/              # Neo N3 smart contracts
│   └── src/
│       ├── Core/                  # Core contracts (ServiceRegistry, etc.)
│       ├── ProductionReady/       # Production-ready contracts
│       └── Services/              # Service contracts
│
├── k8s/                          # Kubernetes configurations
│   ├── base/                     # Base configurations
│   │   ├── namespace.yaml
│   │   ├── resource-quotas.yaml
│   │   ├── network-policies.yaml
│   │   ├── pod-security-standards.yaml
│   │   ├── hpa.yaml             # Horizontal Pod Autoscaling
│   │   └── service-mesh.yaml
│   │
│   └── services/                 # Service-specific configs
│
├── monitoring/                   # Monitoring configurations
│   ├── prometheus.yml           # Prometheus config
│   └── grafana/                 # Grafana dashboards
│       └── dashboards/
│
├── scripts/                      # Utility scripts
│   ├── create-new-service.sh    # Service generator
│   ├── generate-secrets.sh      # Secrets generation
│   ├── production-deployment.sh # Deployment automation
│   ├── validate-system.sh       # System validation
│   └── cleanup-project.sh       # Project cleanup
│
├── docs/                         # Documentation
│   ├── architecture/            # Architecture docs
│   ├── guides/                  # User guides
│   ├── api/                     # API documentation
│   ├── deployment/              # Deployment docs
│   ├── security/                # Security docs
│   └── services/                # Service-specific docs
│
├── docker-compose.yml            # Docker compose for local dev
├── docker-compose.test.yml       # Docker compose for testing
├── docker-compose.dev.yml        # Docker compose for development
├── NeoServiceLayer.sln          # Solution file
├── Directory.Packages.props      # Central package management
├── .gitignore                   # Git ignore patterns
└── README.md                    # Project documentation
```

## Key Components

### 1. Core Framework (`src/Core/`)
- **ServiceFramework**: Base classes for all services
  - `ServiceBase` - Standard service implementation
  - `EnclaveServiceBase` - Services with SGX support
  - `BlockchainServiceBase` - Blockchain-integrated services
  - `MicroserviceHost` - Service hosting and lifecycle

### 2. Services (`src/Services/`)
Over 40 microservices including:
- **Storage Services**: Secure data storage with encryption
- **Key Management**: Cryptographic key lifecycle
- **Oracle Services**: External data integration
- **Smart Contract Services**: Blockchain interaction
- **Monitoring & Health**: System observability
- **Security Services**: Authentication, authorization, encryption

### 3. Infrastructure (`src/Infrastructure/`)
- **Persistence**: Entity Framework Core, storage providers
- **Resilience**: Polly integration, circuit breakers
- **Security**: Authentication, encryption, secure communication
- **Service Discovery**: Consul integration

### 4. Testing (`tests/`)
- Unit tests for individual components
- Integration tests for service interactions
- Performance tests
- End-to-end tests

### 5. Smart Contracts (`contracts-neo-n3/`)
- Core contracts for service registry
- Production-ready token contracts
- Service-specific contracts
- Security features (reentrancy guards, input validation)

### 6. Kubernetes (`k8s/`)
- Production-ready configurations
- Resource quotas and limits
- Network policies
- Auto-scaling configurations
- Service mesh integration

### 7. Monitoring (`monitoring/`)
- Prometheus configuration
- Grafana dashboards
- Alert rules
- Performance metrics

## Development Workflow

### Creating a New Service
```bash
./scripts/create-new-service.sh ServiceName [options]
```

Options:
- `--enclave` - Add Intel SGX support
- `--blockchain` - Add blockchain integration
- `--ai` - Use AI service base
- `--crypto` - Use cryptographic service base

### Building the Project
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Local Development
```bash
docker-compose -f docker-compose.dev.yml up
```

### Deployment
```bash
./scripts/production-deployment.sh
```

## Configuration

### Central Package Management
All NuGet packages are managed centrally in `Directory.Packages.props`

### Service Configuration
Each service has:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Runtime environment
- `DB_PASSWORD` - Database password (from secrets)
- `JWT_SECRET` - JWT signing key (from secrets)
- Service-specific variables

## Security

### Built-in Security Features
- JWT authentication
- Role-based authorization
- Intel SGX enclave support
- End-to-end encryption
- Secure key management
- Network policies
- Pod security standards

### Secrets Management
```bash
./scripts/generate-secrets.sh
```

## Monitoring and Observability

### Health Checks
- `/health` - Basic health
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Metrics
- `/metrics` - Prometheus metrics
- Custom service metrics
- Resource usage tracking

### Distributed Tracing
- OpenTelemetry integration
- Jaeger for trace visualization

## Documentation

### Available Documentation
- `README.md` - Project overview
- `docs/PROJECT_STRUCTURE.md` - This file
- `docs/guides/SERVICE_FRAMEWORK_GUIDE.md` - Framework guide
- `docs/deployment/` - Deployment documentation
- Service-specific READMEs in each service directory

## Maintenance

### Cleanup Script
```bash
./scripts/cleanup-project.sh
```

Removes:
- Build artifacts (bin/obj)
- Temporary files
- Log files
- Test results
- Empty directories

### Validation
```bash
./scripts/validate-system.sh
```

Validates:
- Service configurations
- Dependencies
- Build status
- Deployment readiness

## Contributing

1. Create a new feature branch
2. Use the service generator for new services
3. Follow the established patterns
4. Write tests
5. Update documentation
6. Submit a pull request

## License

[Your License Here]