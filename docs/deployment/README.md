# Neo Service Layer - Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the Neo Service Layer with its **26 services** and **interactive web application**. The deployment includes the complete service ecosystem with Intel SGX + Occlum LibOS enclave support.

## 🌐 Web Application Deployment

The Neo Service Layer includes a **full-featured web application** that provides:
- **Interactive Service Testing**: All 26 services accessible via web interface
- **Real-time API Integration**: Direct communication with actual service endpoints
- **JWT Authentication**: Secure access with role-based permissions
- **Professional UI**: Modern, responsive interface with Bootstrap 5

**Quick Access After Deployment:**
- **Main Interface**: `http://localhost:5000`
- **Service Demo**: `http://localhost:5000/servicepages/servicedemo`
- **API Documentation**: `http://localhost:5000/swagger`

## Deployment Options

The Neo Service Layer can be deployed in the following ways:

1. **Single Node Deployment**: All services run on a single node.
2. **Clustered Deployment**: Services are distributed across multiple nodes for scalability and availability.
3. **Hybrid Deployment**: Some services run on-premises, while others run in the cloud.

## Prerequisites

### Hardware Requirements

- **CPU**: Intel CPU with SGX support (for enclave operations)
- **RAM**: Minimum 16 GB (32 GB recommended)
- **Storage**: Minimum 100 GB SSD
- **Network**: 1 Gbps Ethernet

### Software Requirements

- **Operating System**: Ubuntu 20.04 LTS or later
- **.NET SDK**: .NET 8.0 or later (current implementation uses .NET 8.0)
- **Docker**: Docker 20.10 or later (optional, for containerized deployment)
- **Kubernetes**: Kubernetes 1.25 or later (optional, for clustered deployment)
- **Intel SGX Driver**: SGX driver compatible with your CPU
- **Occlum LibOS**: Occlum 0.30.0 or later
- **Web Browser**: Modern browser for web application access

### Network Requirements

- **Inbound Ports**: 80, 443 (for API access)
- **Outbound Ports**: 10332 (Neo N3), 8545 (NeoX)
- **Firewall Rules**: Allow inbound and outbound traffic on the above ports

## Single Node Deployment

### Installation Steps

1. **Install Prerequisites**:

```bash
# Update package lists
sudo apt update

# Install .NET SDK 8.0
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y apt-transport-https
sudo apt install -y dotnet-sdk-8.0

# Install Docker (optional)
sudo apt install -y docker.io
sudo systemctl enable docker
sudo systemctl start docker
sudo usermod -aG docker $USER

# Install Intel SGX Driver
sudo apt install -y dkms
wget https://download.01.org/intel-sgx/sgx-linux/2.15/distro/ubuntu20.04-server/sgx_linux_x64_driver_2.11.0_2d2b795.bin
chmod +x sgx_linux_x64_driver_2.11.0_2d2b795.bin
sudo ./sgx_linux_x64_driver_2.11.0_2d2b795.bin

# Install Occlum LibOS
sudo apt install -y libsgx-dcap-ql libsgx-dcap-default-qpl libsgx-dcap-quote-verify
wget https://github.com/occlum/occlum/releases/download/0.30.0/occlum-0.30.0-ubuntu20.04-x86_64.tar.gz
tar -xzf occlum-0.30.0-ubuntu20.04-x86_64.tar.gz
cd occlum-0.30.0
sudo ./install.sh
```

2. **Clone the Repository**:

```bash
git clone https://github.com/neo-project/neo-service-layer.git
cd neo-service-layer
```

3. **Build the Solution**:

```bash
dotnet build
```

4. **Configure the Services**:

```bash
# Copy the example configuration
cp config/appsettings.example.json config/appsettings.json

# Edit the configuration
nano config/appsettings.json
```

5. **Start the Web Application**:

```bash
# Start the complete web application with all services
dotnet run --project src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj
```

**Alternative - Start API Only:**
```bash
# For API-only deployment
dotnet run --project src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj
```

### Configuration Options

Configuration files are located in the project directories:
- **Web Application**: `src/Web/NeoServiceLayer.Web/appsettings.json`
- **API Service**: `src/Api/NeoServiceLayer.Api/appsettings.json`

#### Key Configuration Sections:

**JWT Authentication:**
```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "neo-service-layer",
    "Audience": "neo-service-layer-users",
    "ExpirationHours": 1
  }
}
```

**Service Registration:**
```json
{
  "Services": {
    "AllServicesEnabled": true,
    "ServiceTimeout": 30000,
    "MaxConcurrentRequests": 100
  }
}
```

**Blockchain Configuration:**
```json
{
  "Blockchain": {
    "NeoN3": {
      "RpcUrl": "https://mainnet1.neo.coz.io:443",
      "NetworkMagic": 860833102
    },
    "NeoX": {
      "RpcUrl": "https://neox-rpc.example.com",
      "ChainId": 47803
    }
  }
}
```

**Intel SGX + Occlum Configuration:**
```json
{
  "Enclave": {
    "SimulationMode": false,
    "EnclaveImagePath": "/opt/occlum/enclave.signed",
    "AttestationRequired": true
  }
}
```

**Web Application Specific:**
```json
{
  "WebApp": {
    "EnableSwagger": true,
    "EnableCors": true,
    "AllowedOrigins": ["http://localhost:3000"],
    "DefaultTimeout": 30000
  }
}
```

### Post-Deployment Verification

After deploying the Neo Service Layer, verify that it is working correctly:

#### 1. **Web Application Verification**

```bash
# Check web application is accessible
curl http://localhost:5000/

# Access service demo page
open http://localhost:5000/servicepages/servicedemo

# Check Swagger documentation
open http://localhost:5000/swagger
```

#### 2. **API Health Checks**

```bash
# Overall system health
curl http://localhost:5000/api/health/check

# Service-specific health checks
curl http://localhost:5000/api/randomness/health
curl http://localhost:5000/api/keymanagement/health
```

#### 3. **Authentication Testing**

```bash
# Get demo JWT token
curl -X POST http://localhost:5000/api/auth/demo-token

# Use token for authenticated requests
TOKEN="your-jwt-token-here"
curl -H "Authorization: Bearer $TOKEN" \
     http://localhost:5000/api/keymanagement/list/NeoN3
```

#### 4. **Service Integration Testing**

```bash
# Test randomness service
curl -X POST http://localhost:5000/api/randomness/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"format": "hex", "byteCount": 32}'

# Test key management service
curl -X POST http://localhost:5000/api/keymanagement/generate/NeoN3 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"keyId": "test-key", "keyType": "ECDSA"}'
```

#### 5. **Service Count Verification**

Verify all 20+ services are available:
```bash
# Check total number of registered services
curl -H "Authorization: Bearer $TOKEN" \
     http://localhost:5000/api/health/services
```

## Clustered Deployment

### Kubernetes Deployment

1. **Create Kubernetes Manifests**:

```bash
# Create namespace
kubectl create namespace neo-service-layer

# Apply manifests
kubectl apply -f kubernetes/
```

2. **Configure Ingress**:

```bash
# Apply ingress manifest
kubectl apply -f kubernetes/ingress.yaml
```

3. **Scale Services**:

```bash
# Scale API service
kubectl scale deployment neo-service-layer-api --replicas=3 -n neo-service-layer

# Scale other services as needed
kubectl scale deployment neo-service-layer-randomness --replicas=2 -n neo-service-layer
```

### Docker Compose Deployment

1. **Create Docker Compose File**:

```bash
# Copy the example docker-compose file
cp docker-compose.example.yml docker-compose.yml

# Edit the docker-compose file
nano docker-compose.yml
```

2. **Start Services**:

```bash
docker-compose up -d
```

3. **Scale Services**:

```bash
docker-compose up -d --scale api=3 --scale randomness=2
```

## Hybrid Deployment

For a hybrid deployment, where some services run on-premises and others run in the cloud:

1. **Deploy Core Services On-Premises**:

```bash
# Deploy core services with web interface
dotnet run --project src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj
```

2. **Deploy Other Services in the Cloud**:

```bash
# Deploy to Azure
az deployment group create --resource-group neo-service-layer --template-file azure/template.json --parameters azure/parameters.json
```

## Security Considerations

### Intel SGX + Occlum LibOS Security
- **Enclave Attestation**: Configure remote attestation to verify enclave integrity
- **Secure Key Management**: All cryptographic keys stored within SGX enclaves
- **Trusted Execution**: Critical computations protected by hardware-level security
- **Memory Protection**: Enclave memory encrypted and isolated from OS

### Web Application Security
- **JWT Authentication**: Secure token-based authentication with configurable expiration
- **HTTPS Only**: Force HTTPS in production deployments
- **CORS Configuration**: Restrict allowed origins for cross-origin requests
- **Input Validation**: Comprehensive input validation on all endpoints
- **Rate Limiting**: Prevent abuse with configurable rate limits

### Network Security
- **Firewall Configuration**: Restrict access to necessary ports only
- **TLS 1.3**: Use latest TLS version for all communications
- **API Gateway**: Consider using API gateway for additional security layer
- **VPN Access**: Restrict administrative access through VPN

### Secrets Management
- **Environment Variables**: Use environment variables for sensitive configuration
- **Key Vault Integration**: Integrate with Azure Key Vault or similar solutions
- **Rotating Keys**: Implement automatic key rotation policies
- **Audit Logging**: Log all access to sensitive operations

## Monitoring and Maintenance

### Monitoring

#### Web Application Monitoring
- **Health Dashboard**: Access real-time health status via web interface
- **Service Status**: Monitor all 20+ services through the web application
- **Performance Metrics**: Real-time performance metrics and analytics
- **User Activity**: Monitor web application usage and API calls

#### API Monitoring
- **Health Endpoints**: Monitor service health using `/api/health/*` endpoints
- **Service Metrics**: Individual service metrics and performance data
- **Authentication Metrics**: Monitor JWT token usage and authentication patterns
- **Error Tracking**: Comprehensive error logging and tracking

#### System Monitoring
- **SGX Enclave Status**: Monitor enclave health and attestation status
- **Resource Usage**: CPU, memory, and disk usage monitoring
- **Blockchain Connectivity**: Monitor connections to Neo N3 and NeoX networks
- **Service Dependencies**: Monitor external service dependencies

### Maintenance

- **Backup**: Regularly backup configuration and data.
- **Updates**: Regularly update the Neo Service Layer to the latest version.
- **Scaling**: Scale services based on load and performance metrics.

## Troubleshooting

### Common Issues

#### Web Application Issues
- **Web App Not Loading**: Check if .NET 8.0 runtime is installed and port 5000 is available
- **Service Demo Page Empty**: Verify all service dependencies are registered in Program.cs
- **Authentication Errors**: Check JWT configuration and token generation
- **API Calls Failing**: Verify CORS settings and authentication headers

#### Service Integration Issues
- **Service Registration Failed**: Check service project references in web application
- **SGX Enclave Initialization Failed**: Verify SGX driver installation and CPU support
- **Service Timeout**: Increase timeout settings in configuration
- **Dependency Injection Errors**: Verify all services are properly registered

#### Infrastructure Issues
- **Blockchain Connection Failed**: Verify Neo N3/NeoX RPC endpoints are accessible
- **Port Conflicts**: Ensure ports 5000 (HTTP) and 5001 (HTTPS) are available
- **Memory Issues**: Ensure sufficient RAM for all 20+ services
- **Database Connection**: Verify database connections if using external storage

### Logs

#### Application Logs
- **Web Application**: Console output and configured log files
- **API Logs**: Structured logging with request/response details
- **Service Logs**: Individual service operation logs
- **Authentication Logs**: JWT token generation and validation logs

#### System Logs
- **SGX Enclave Logs**: Enclave initialization and operation logs
- **Performance Logs**: Service performance and timing metrics
- **Error Logs**: Detailed error information and stack traces
- **Audit Logs**: Security-related operations and access logs

#### Log Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "NeoServiceLayer": "Debug"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

## 📚 Related Documentation

### Neo Service Layer Documentation
- **[Architecture Overview](../architecture/ARCHITECTURE_OVERVIEW.md)** - Complete system architecture
- **[Services Documentation](../services/README.md)** - All 20+ services documentation
- **[Web Application Guide](../web/WEB_APPLICATION_GUIDE.md)** - Complete web app documentation
- **[API Reference](../web/API_REFERENCE.md)** - Detailed API documentation
- **[Development Guide](../development/README.md)** - Development and testing guidelines

### External Documentation
- **[Neo N3 Documentation](https://docs.neo.org/)** - Neo N3 blockchain documentation
- **[NeoX Documentation](https://docs.neo.org/neox/)** - NeoX EVM-compatible documentation
- **[Intel SGX Documentation](https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/overview.html)** - Intel SGX development
- **[Occlum LibOS Documentation](https://occlum.io/)** - Occlum LibOS for SGX
- **[ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)** - Web application framework

### Quick Start Checklist

✅ **Prerequisites Installed**
- [ ] .NET 8.0 SDK
- [ ] Intel SGX Driver
- [ ] Occlum LibOS
- [ ] Modern web browser

✅ **Deployment Completed**
- [ ] Repository cloned and built
- [ ] Configuration files updated
- [ ] Web application started
- [ ] All services accessible

✅ **Verification Passed**
- [ ] Web interface accessible at `http://localhost:5000`
- [ ] Service demo page functional
- [ ] API documentation available
- [ ] Authentication working
- [ ] All 20+ services responding
