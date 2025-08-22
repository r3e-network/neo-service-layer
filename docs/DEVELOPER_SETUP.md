# Neo Service Layer - Developer Setup Guide

## Table of Contents
- [Prerequisites](#prerequisites)
- [Development Environment Setup](#development-environment-setup)
- [Building from Source](#building-from-source)
- [Running Locally](#running-locally)
- [Development Workflow](#development-workflow)
- [Testing](#testing)
- [Debugging](#debugging)
- [Contributing](#contributing)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

#### Minimum Requirements
- **OS**: Ubuntu 20.04 LTS, macOS 11+, Windows 10/11 with WSL2
- **CPU**: 4 cores
- **RAM**: 8GB
- **Storage**: 50GB free space
- **Network**: Stable internet connection

#### Recommended Requirements
- **OS**: Ubuntu 22.04 LTS or macOS 13+
- **CPU**: 8+ cores
- **RAM**: 16GB+
- **Storage**: 100GB+ SSD
- **GPU**: Optional for ML workloads

### Required Software

#### Core Dependencies
```bash
# .NET SDK 8.0
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 8.0.100

# Node.js 18+ and npm
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Docker and Docker Compose
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# PostgreSQL Client
sudo apt-get install -y postgresql-client-14

# Redis CLI
sudo apt-get install -y redis-tools

# Git
sudo apt-get install -y git
```

#### Development Tools
```bash
# Visual Studio Code
wget -q https://packages.microsoft.com/keys/microsoft.asc -O- | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://packages.microsoft.com/repos/vscode stable main"
sudo apt-get update
sudo apt-get install -y code

# VS Code Extensions
code --install-extension ms-dotnettools.csharp
code --install-extension ms-azuretools.vscode-docker
code --install-extension ms-kubernetes-tools.vscode-kubernetes-tools
code --install-extension redhat.vscode-yaml
code --install-extension ms-vscode.powershell
```

#### Optional Tools for SGX Development
```bash
# Intel SGX SDK (for SGX development)
wget https://download.01.org/intel-sgx/latest/linux-latest/distro/ubuntu20.04-server/sgx_linux_x64_sdk.bin
chmod +x sgx_linux_x64_sdk.bin
./sgx_linux_x64_sdk.bin

# Occlum (for confidential computing)
docker pull occlum/occlum:latest
```

## Development Environment Setup

### 1. Clone Repository
```bash
# Clone the repository
git clone https://github.com/your-org/neo-service-layer.git
cd neo-service-layer

# Create development branch
git checkout -b dev/your-feature
```

### 2. Environment Configuration
```bash
# Copy environment template
cp .env.example .env.development

# Edit development configuration
nano .env.development
```

#### Development Environment Variables
```env
# Development Settings
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
DEV_SWAGGER_ENABLED=true
DEV_DETAILED_ERRORS=true
DEV_SEED_DATA=true

# Local Services
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=neoservice_dev
POSTGRES_USER=developer
POSTGRES_PASSWORD=DevPassword123!

REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=DevRedisPassword123!

# JWT Settings (Development)
JWT_SECRET_KEY=DevSecretKeyThatIsLongEnoughForDevelopment2024!
JWT_EXPIRATION=86400

# SGX Mode (Simulation for development)
SGX_MODE=Simulation

# API Ports
API_HTTP_PORT=8080
API_HTTPS_PORT=8443

# Monitoring (Optional for development)
PROMETHEUS_PORT=9090
GRAFANA_PORT=3000
```

### 3. Install Dependencies
```bash
# Restore .NET packages
dotnet restore

# Install npm packages (for frontend/tools)
npm install

# Install development tools
dotnet tool install --global dotnet-ef
dotnet tool install --global dotnet-aspnet-codegenerator
dotnet tool install --global dotnet-dump
dotnet tool install --global dotnet-trace
dotnet tool install --global dotnet-counters
```

### 4. Setup Local Database
```bash
# Start PostgreSQL container
docker run -d \
  --name postgres-dev \
  -e POSTGRES_USER=developer \
  -e POSTGRES_PASSWORD=DevPassword123! \
  -e POSTGRES_DB=neoservice_dev \
  -p 5432:5432 \
  postgres:14-alpine

# Wait for PostgreSQL to be ready
until docker exec postgres-dev pg_isready; do sleep 1; done

# Run database migrations
dotnet ef database update -p src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence \
  -s src/Api/NeoServiceLayer.Api

# Seed development data (optional)
dotnet run --project tools/DataSeeder
```

### 5. Setup Local Redis
```bash
# Start Redis container
docker run -d \
  --name redis-dev \
  -p 6379:6379 \
  redis:7-alpine redis-server --requirepass DevRedisPassword123!

# Verify Redis connection
redis-cli -a DevRedisPassword123! ping
```

## Building from Source

### Build All Projects
```bash
# Clean previous builds
dotnet clean

# Build in Debug mode
dotnet build --configuration Debug

# Build in Release mode
dotnet build --configuration Release

# Build specific project
dotnet build src/Api/NeoServiceLayer.Api
```

### Build Docker Images
```bash
# Build all services
docker-compose -f docker-compose.dev.yml build

# Build specific service
docker-compose -f docker-compose.dev.yml build api-gateway

# Build with BuildKit (faster)
DOCKER_BUILDKIT=1 docker-compose -f docker-compose.dev.yml build --parallel
```

## Running Locally

### 1. Using dotnet CLI
```bash
# Run API Gateway
dotnet run --project src/Api/NeoServiceLayer.Api

# Run with watch mode (auto-reload)
dotnet watch run --project src/Api/NeoServiceLayer.Api

# Run with specific configuration
dotnet run --project src/Api/NeoServiceLayer.Api \
  --configuration Debug \
  --launch-profile Development
```

### 2. Using Docker Compose
```bash
# Start all services
docker-compose -f docker-compose.dev.yml up

# Start specific services
docker-compose -f docker-compose.dev.yml up api-gateway postgres redis

# Start in background
docker-compose -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.dev.yml logs -f api-gateway
```

### 3. Using Visual Studio Code
```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Api/NeoServiceLayer.Api/bin/Debug/net8.0/NeoServiceLayer.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Api/NeoServiceLayer.Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:8080;https://localhost:8443"
      }
    }
  ]
}
```

### 4. Verify Services
```bash
# Check API health
curl http://localhost:8080/health

# Open Swagger UI
open http://localhost:8080/swagger

# Check database connection
curl http://localhost:8080/health/db

# Check Redis connection
curl http://localhost:8080/health/redis
```

## Development Workflow

### 1. Feature Development
```bash
# Create feature branch
git checkout -b feature/your-feature-name

# Make changes
# ... edit files ...

# Run tests
dotnet test

# Commit changes
git add .
git commit -m "feat: Add new feature"

# Push to remote
git push origin feature/your-feature-name
```

### 2. Code Generation

#### Generate Controller
```bash
dotnet aspnet-codegenerator controller \
  -name MyController \
  -api \
  -async \
  -outDir Controllers \
  -namespace NeoServiceLayer.Api.Controllers
```

#### Generate Entity and Migration
```bash
# Create entity class
# ... create src/Core/NeoServiceLayer.Core/Entities/MyEntity.cs ...

# Add to DbContext
# ... update NeoServiceLayerDbContext.cs ...

# Generate migration
dotnet ef migrations add AddMyEntity \
  -p src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence \
  -s src/Api/NeoServiceLayer.Api

# Apply migration
dotnet ef database update \
  -p src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence \
  -s src/Api/NeoServiceLayer.Api
```

### 3. Hot Reload Development
```bash
# Enable hot reload
export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true

# Run with hot reload
dotnet watch run --project src/Api/NeoServiceLayer.Api

# Frontend hot reload (if applicable)
npm run dev
```

## Testing

### Unit Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test tests/NeoServiceLayer.Core.Tests

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~MyTestClass.MyTestMethod"
```

### Integration Tests
```bash
# Start test environment
docker-compose -f docker-compose.test.yml up -d

# Run integration tests
dotnet test tests/NeoServiceLayer.Integration.Tests

# Clean up
docker-compose -f docker-compose.test.yml down -v
```

### E2E Tests
```bash
# Install Playwright (for E2E testing)
npx playwright install

# Run E2E tests
npm run test:e2e

# Run with UI
npx playwright test --ui
```

### Performance Tests
```bash
# Run load tests with k6
k6 run tests/performance/load-test.js

# Run stress tests
k6 run tests/performance/stress-test.js

# Generate HTML report
k6 run --out html=report.html tests/performance/load-test.js
```

## Debugging

### 1. Visual Studio Code Debugging
```json
// .vscode/launch.json - Attach to Process
{
  "name": "Attach to Process",
  "type": "coreclr",
  "request": "attach",
  "processId": "${command:pickProcess}"
}
```

### 2. Remote Debugging
```bash
# Start with debugging enabled
dotnet run --project src/Api/NeoServiceLayer.Api \
  --configuration Debug \
  -- --urls "http://0.0.0.0:8080"

# Attach debugger from VS Code
# Select "Attach to Process" and choose dotnet process
```

### 3. Docker Debugging
```yaml
# docker-compose.debug.yml
services:
  api-gateway:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - DOTNET_RUNNING_IN_CONTAINER=true
    ports:
      - "8080:8080"
      - "10222:10222" # Debugger port
    command: >
      sh -c "dotnet dev-certs https --trust &&
             dotnet watch run --no-launch-profile"
```

### 4. Logging and Tracing
```csharp
// Enable detailed logging in appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

### 5. Database Query Debugging
```csharp
// Enable EF Core query logging
optionsBuilder.UseNpgsql(connectionString)
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors()
    .LogTo(Console.WriteLine, LogLevel.Information);
```

## Contributing

### Code Style Guidelines

#### C# Conventions
```csharp
// Use PascalCase for public members
public class MyClass
{
    public string PropertyName { get; set; }
    
    // Use camelCase for private fields
    private readonly ILogger<MyClass> _logger;
    
    // Use async/await properly
    public async Task<Result> DoSomethingAsync()
    {
        return await Task.FromResult(new Result());
    }
}
```

#### File Organization
```
src/
├── Api/                    # API layer
├── Core/                   # Domain entities and interfaces
├── Infrastructure/         # Data access and external services
└── Services/              # Business logic services

tests/
├── Unit/                  # Unit tests
├── Integration/           # Integration tests
└── E2E/                   # End-to-end tests
```

### Pull Request Process

1. **Create Feature Branch**
```bash
git checkout -b feature/your-feature
```

2. **Make Changes and Test**
```bash
# Make your changes
# Run tests
dotnet test
# Run linting
dotnet format
```

3. **Commit with Conventional Commits**
```bash
git commit -m "feat: Add new feature"
# Types: feat, fix, docs, style, refactor, test, chore
```

4. **Push and Create PR**
```bash
git push origin feature/your-feature
# Create PR on GitHub
```

5. **PR Checklist**
- [ ] Tests pass
- [ ] Code follows style guidelines
- [ ] Documentation updated
- [ ] No security vulnerabilities
- [ ] Performance impact considered

## Troubleshooting

### Common Issues

#### 1. Database Connection Failed
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Check connection string
echo $POSTGRES_HOST
echo $POSTGRES_PORT

# Test connection
psql -h localhost -U developer -d neoservice_dev
```

#### 2. Port Already in Use
```bash
# Find process using port
lsof -i :8080

# Kill process
kill -9 <PID>

# Or change port in launchSettings.json
```

#### 3. Build Errors
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force

# Clean and rebuild
dotnet clean
dotnet build --no-cache
```

#### 4. Migration Errors
```bash
# Revert last migration
dotnet ef migrations remove

# Reset database
dotnet ef database drop --force
dotnet ef database update
```

#### 5. Docker Issues
```bash
# Clean Docker resources
docker system prune -a

# Rebuild without cache
docker-compose build --no-cache

# Reset Docker
docker-compose down -v
docker-compose up --build
```

### Getting Help

- **Documentation**: Check `/docs` folder
- **Issues**: https://github.com/your-org/neo-service-layer/issues
- **Discord**: Join our developer Discord
- **Stack Overflow**: Tag with `neo-service-layer`

## Additional Resources

### Documentation
- [Architecture Overview](./ARCHITECTURE.md)
- [API Documentation](./API_DOCUMENTATION.md)
- [PostgreSQL Persistence](./POSTGRESQL_PERSISTENCE.md)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)

### Tools and Extensions
- [Postman Collection](../postman/neo-service-layer.json)
- [VS Code Settings](../.vscode/settings.json)
- [Docker Compose Files](../docker-compose.*.yml)

### Learning Resources
- [.NET Documentation](https://docs.microsoft.com/dotnet)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Docker Documentation](https://docs.docker.com)
- [PostgreSQL Documentation](https://www.postgresql.org/docs)