# Neo Service Layer DevContainer - Quick Start Guide

## 🚀 Getting Started

### Step 1: Open in DevContainer
1. **Open VS Code** in the project directory
2. **Press `Ctrl+Shift+P`** (or `Cmd+Shift+P` on Mac)  
3. **Type**: "Dev Containers: Reopen in Container"
4. **Wait** for the container to build (10-15 minutes first time)

### Step 2: Verify Installation
Once the container is ready, open the integrated terminal and run:
```bash
# Check .NET installation
dotnet --version

# Check Rust installation
rustc --version

# Check SGX environment
source /opt/intel/sgxsdk/environment && echo "SGX SDK ready"

# Check Occlum environment  
source /opt/occlum/build/bin/occlum_bashrc && echo "Occlum ready"
```

### Step 3: Start the Application
```bash
# Start the complete Neo Service Layer
./start-dev.sh
```

## 🌐 Access Points

Once running, open these URLs in your browser:

| Service | URL | Description |
|---------|-----|-------------|
| **Main App** | http://localhost:5000 | Primary web interface |
| **API Docs** | http://localhost:5000/swagger | Swagger API documentation |
| **Health Check** | http://localhost:5000/health | System health status |
| **Service Info** | http://localhost:5000/api/info | Runtime service information |

## 🔐 Authentication

### Get a Demo Token
```bash
curl -X POST http://localhost:5000/api/auth/demo-token
```

### Use Token in API Calls
```bash
# Save token
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/demo-token | jq -r '.token')

# Use token in requests
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/info
```

## 🛠️ Development Workflow

### Building and Testing
```bash
# Build the entire solution
dotnet build

# Run tests
./test-all.sh

# Run specific service tests
dotnet test src/Services/NeoServiceLayer.Services.KeyManagement.Tests/

# Watch for changes during development
dotnet watch run --project src/Web/NeoServiceLayer.Web/
```

### SGX Development
```bash
# Load SGX environment
source /opt/intel/sgxsdk/environment

# Build SGX enclave code
cd src/Tee/NeoServiceLayer.Tee.Enclave
cargo build

# Run with SGX simulation
export SGX_MODE=SIM
./start-dev.sh
```

### Service Development
```bash
# Enable/disable services incrementally
./enable-services.sh

# Check service registration
curl http://localhost:5000/api/info | jq '.Features'
```

## 📝 Configuration Options

### Environment Variables
```bash
# Development (default)
export ASPNETCORE_ENVIRONMENT=Development
export SGX_MODE=SIM

# Production
export ASPNETCORE_ENVIRONMENT=Production
export SGX_MODE=HW  # Requires SGX hardware
```

### Service Configuration
Edit `src/Web/NeoServiceLayer.Web/appsettings.Development.json`:
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience"
  },
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  }
}
```

## 🔧 Troubleshooting

### Service Registration Issues
If services fail to register:
```bash
# Use the service helper
./enable-services.sh

# Check logs
tail -f src/Web/NeoServiceLayer.Web/logs/neo-service-layer-web-*.txt
```

### Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### SGX Issues
```bash
# Verify SGX simulation mode
echo $SGX_MODE  # Should be "SIM"

# Check SGX SDK
ls -la /opt/intel/sgxsdk/

# Test SGX sample
cd /opt/intel/sgxsdk/SampleCode/LocalAttestation
make SGX_MODE=SIM
```

### Network Issues
If you encounter Docker network problems:
```bash
# Run the network troubleshooting script (from host)
./fix-docker-network.ps1
```

## 📦 Available Services

The devcontainer includes all Neo Service Layer services:

### Core Services
- **Key Management** - Cryptographic key handling
- **Storage** - Distributed storage management  
- **Health** - System monitoring and health checks
- **Configuration** - Service configuration management

### Blockchain Services
- **Neo N3** - Neo blockchain integration
- **Neo X** - Neo X sidechain support
- **Oracle** - External data oracle services
- **Voting** - On-chain governance voting

### Advanced Services
- **Zero Knowledge** - Privacy-preserving computations
- **Proof of Reserve** - Asset backing verification
- **Cross Chain** - Inter-blockchain communication
- **Abstract Account** - Account abstraction layer

### Infrastructure Services
- **Monitoring** - Performance and metrics collection
- **Notification** - Event notification system
- **Backup** - Data backup and recovery
- **Automation** - Automated workflow execution

## 🎯 Next Steps

1. **Explore the API** using Swagger UI at http://localhost:5000/swagger
2. **Check service status** at http://localhost:5000/health
3. **Review service documentation** in the `/docs` directory
4. **Start developing** your blockchain applications!

## 📚 Additional Resources

- **Project Documentation**: `/docs` directory
- **Service Tests**: `/tests` directory  
- **Configuration**: `/src/Web/NeoServiceLayer.Web/appsettings.json`
- **SGX Documentation**: `/docs/security/sgx-guide.md`
- **Deployment Guide**: `/docs/deployment/`

---

**Happy developing with Neo Service Layer! 🎉** 