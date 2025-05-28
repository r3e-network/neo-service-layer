# Neo Service Layer - Deployment Guide

## Overview

This guide provides instructions for deploying the Neo Service Layer in various environments. It covers prerequisites, installation steps, configuration options, and post-deployment verification.

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
- **.NET SDK**: .NET 9.0 or later
- **Docker**: Docker 20.10 or later (optional, for containerized deployment)
- **Kubernetes**: Kubernetes 1.25 or later (optional, for clustered deployment)
- **Intel SGX Driver**: SGX driver compatible with your CPU
- **Occlum LibOS**: Occlum 0.30.0 or later

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

# Install .NET SDK
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y apt-transport-https
sudo apt install -y dotnet-sdk-9.0

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

5. **Start the Services**:

```bash
dotnet run --project src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj
```

### Configuration Options

The main configuration file is `config/appsettings.json`. Here are the key configuration options:

- **API**: Configuration for the API service.
  - **Url**: The URL to listen on.
  - **CorsOrigins**: Allowed CORS origins.
  - **RateLimit**: Rate limiting configuration.

- **Services**: Configuration for each service.
  - **Randomness**: Configuration for the Randomness service.
  - **Oracle**: Configuration for the Oracle service.
  - **KeyManagement**: Configuration for the Key Management service.
  - **Compute**: Configuration for the Compute service.
  - **Storage**: Configuration for the Storage service.
  - **Compliance**: Configuration for the Compliance service.
  - **EventSubscription**: Configuration for the Event Subscription service.

- **Blockchain**: Configuration for blockchain integration.
  - **NeoN3**: Configuration for Neo N3 integration.
    - **RpcUrl**: The URL of the Neo N3 RPC server.
    - **NetworkMagic**: The network magic number.
  - **NeoX**: Configuration for NeoX integration.
    - **RpcUrl**: The URL of the NeoX RPC server.
    - **ChainId**: The chain ID.

- **Enclave**: Configuration for enclave operations.
  - **SimulationMode**: Whether to run in simulation mode.
  - **EnclaveImagePath**: The path to the enclave image.
  - **EnclaveConfigPath**: The path to the enclave configuration.

- **Logging**: Configuration for logging.
  - **LogLevel**: The log level.
  - **LogPath**: The path to the log file.

### Post-Deployment Verification

After deploying the Neo Service Layer, verify that it is working correctly:

1. **Check Service Status**:

```bash
curl http://localhost:5000/api/v1/health
```

2. **Test API Endpoints**:

```bash
# Generate a random number
curl -X POST http://localhost:5000/api/v1/randomness/generate \
  -H "Content-Type: application/json" \
  -d '{"blockchain": "neo-n3", "min": 1, "max": 100}'
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
# Deploy core services
dotnet run --project src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj --services Core,KeyManagement,Compute
```

2. **Deploy Other Services in the Cloud**:

```bash
# Deploy to Azure
az deployment group create --resource-group neo-service-layer --template-file azure/template.json --parameters azure/parameters.json
```

## Security Considerations

- **Enclave Attestation**: Configure enclave attestation to ensure the integrity of the enclave.
- **API Authentication**: Configure API authentication to secure API access.
- **Network Security**: Configure firewalls and network security groups to restrict access.
- **Secrets Management**: Use a secure secrets management solution to store sensitive information.
- **Logging and Monitoring**: Configure logging and monitoring to detect and respond to security incidents.

## Monitoring and Maintenance

### Monitoring

- **Health Checks**: Monitor service health using the `/api/v1/health` endpoint.
- **Metrics**: Monitor service metrics using the `/api/v1/metrics` endpoint.
- **Logs**: Monitor service logs for errors and warnings.

### Maintenance

- **Backup**: Regularly backup configuration and data.
- **Updates**: Regularly update the Neo Service Layer to the latest version.
- **Scaling**: Scale services based on load and performance metrics.

## Troubleshooting

### Common Issues

- **Enclave Initialization Failed**: Verify that the SGX driver is installed and the CPU supports SGX.
- **API Connection Failed**: Verify that the API service is running and accessible.
- **Blockchain Connection Failed**: Verify that the blockchain RPC server is running and accessible.

### Logs

- **API Logs**: Located at `logs/api.log`.
- **Service Logs**: Located at `logs/{service-name}.log`.
- **Enclave Logs**: Located at `logs/enclave.log`.

## References

- [Neo Service Layer Architecture](../architecture/README.md)
- [Neo Service Layer API](../api/README.md)
- [Neo Service Layer Services](../services/README.md)
- [Neo N3 Documentation](https://docs.neo.org/)
- [NeoX Documentation](https://docs.neo.org/neox/)
- [Intel SGX Documentation](https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/overview.html)
- [Occlum LibOS Documentation](https://occlum.io/)
