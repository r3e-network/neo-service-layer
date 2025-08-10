#!/bin/bash
# Script to generate README documentation for services missing documentation

set -euo pipefail

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${GREEN}Neo Service Layer - Service Documentation Generator${NC}"
echo "================================================="

# Services that need documentation
SERVICES=(
    "AbstractAccount"
    "Automation"
    "Backup"
    "Configuration"
    "CrossChain"
    "EnclaveStorage"
    "Health"
    "Monitoring"
    "NetworkSecurity"
    "Notification"
    "ProofOfReserve"
    "SecretsManagement"
    "SmartContracts"
    "SmartContracts.NeoN3"
    "SocialRecovery"
    "Voting"
    "ZeroKnowledge"
    "AI.PatternRecognition"
    "AI.Prediction"
    "Advanced.FairOrdering"
)

# Function to generate README for a service
generate_readme() {
    local service_name=$1
    local service_path=$2
    local readme_path="$service_path/README.md"
    
    # Skip if README already exists
    if [ -f "$readme_path" ]; then
        echo -e "${YELLOW}Skipping $service_name - README already exists${NC}"
        return
    fi
    
    echo -e "${BLUE}Generating README for $service_name...${NC}"
    
    # Determine service type and features based on name
    local description=""
    local features=""
    local api_endpoints=""
    
    case "$service_name" in
        "AbstractAccount")
            description="Provides account abstraction functionality for Neo blockchain, enabling smart contract wallets and advanced account features."
            features="- Smart contract wallets\n- Multi-signature support\n- Social recovery integration\n- Gas abstraction\n- Batch transactions"
            api_endpoints="/accounts, /accounts/{address}, /accounts/{address}/execute"
            ;;
        "Automation")
            description="Enables automated smart contract execution and scheduled tasks on the Neo blockchain."
            features="- Scheduled contract calls\n- Conditional automation\n- Event-driven triggers\n- Gas optimization\n- Batch automation"
            api_endpoints="/automations, /automations/{id}, /automations/{id}/trigger"
            ;;
        "Backup")
            description="Provides comprehensive backup and recovery services for blockchain data and configurations."
            features="- Automated backups\n- Point-in-time recovery\n- Cross-region replication\n- Encryption at rest\n- Backup verification"
            api_endpoints="/backups, /backups/{id}, /backups/restore"
            ;;
        "Health")
            description="Monitors and reports the health status of all Neo Service Layer components."
            features="- Real-time health checks\n- Service dependency mapping\n- Automated alerts\n- Health history tracking\n- Custom health metrics"
            api_endpoints="/health, /health/live, /health/ready, /health/services"
            ;;
        "Monitoring")
            description="Comprehensive monitoring solution for Neo Service Layer with metrics collection and analysis."
            features="- Prometheus metrics integration\n- Custom metric collection\n- Performance tracking\n- Resource utilization monitoring\n- Alert management"
            api_endpoints="/metrics, /metrics/custom, /alerts, /dashboards"
            ;;
        "NetworkSecurity")
            description="Provides network-level security features including firewall rules, DDoS protection, and intrusion detection."
            features="- Dynamic firewall rules\n- DDoS mitigation\n- Intrusion detection\n- Network segmentation\n- Traffic analysis"
            api_endpoints="/security/rules, /security/threats, /security/audit"
            ;;
        "Notification")
            description="Multi-channel notification service for blockchain events and system alerts."
            features="- Email notifications\n- SMS alerts\n- Webhook integration\n- Event filtering\n- Template management"
            api_endpoints="/notifications/send, /notifications/templates, /notifications/subscriptions"
            ;;
        "CrossChain")
            description="Enables cross-chain interoperability between Neo and other blockchain networks."
            features="- Cross-chain transfers\n- Asset bridging\n- Message passing\n- Liquidity pools\n- Multi-chain validation"
            api_endpoints="/bridges, /bridges/{id}/transfer, /bridges/{id}/status"
            ;;
        "ZeroKnowledge")
            description="Implements zero-knowledge proof protocols for privacy-preserving operations."
            features="- ZK-SNARK support\n- Private transactions\n- Confidential contracts\n- Proof generation\n- Verification services"
            api_endpoints="/proofs/generate, /proofs/verify, /proofs/circuits"
            ;;
        *)
            description="Service providing specialized functionality for the Neo Service Layer."
            features="- Core functionality\n- High availability\n- Scalable architecture\n- Security features\n- API integration"
            api_endpoints="/api/v1/endpoint"
            ;;
    esac
    
    # Generate README content
    cat > "$readme_path" << EOF
# Neo Service Layer - $service_name Service

## Overview

$description

## Features

$features

## Architecture

The $service_name service is built on a microservices architecture with the following components:

- **API Layer**: RESTful API endpoints for external communication
- **Business Logic**: Core service functionality and processing
- **Data Layer**: Persistent storage and caching
- **Security**: Authentication, authorization, and encryption
- **Monitoring**: Health checks and metrics collection

## Configuration

### Environment Variables

\`\`\`bash
# Service Configuration
SERVICE_NAME=$service_name
SERVICE_PORT=8080
LOG_LEVEL=Information

# Database Configuration
DB_CONNECTION_STRING=Host=localhost;Database=neo_service_layer;Username=neo_user

# Redis Configuration
REDIS_CONNECTION=localhost:6379

# Security
JWT_SECRET=your-secret-key
ENABLE_AUTH=true

# Service Discovery
CONSUL_ENABLED=true
CONSUL_ADDRESS=http://consul:8500
\`\`\`

### Configuration File

\`\`\`json
{
  "Service": {
    "Name": "$service_name",
    "Version": "1.0.0",
    "Port": 8080
  },
  "Security": {
    "EnableAuth": true,
    "RequireHttps": true
  },
  "HealthChecks": {
    "Enabled": true,
    "Interval": 30
  }
}
\`\`\`

## API Endpoints

### Base URL
\`\`\`
https://api.neo-service-layer.com/v1/$service_name
\`\`\`

### Endpoints

#### Health Check
\`\`\`
GET /health
\`\`\`

Returns the health status of the service.

**Response:**
\`\`\`json
{
  "status": "Healthy",
  "timestamp": "2025-01-10T10:00:00Z",
  "version": "1.0.0",
  "dependencies": {
    "database": "Healthy",
    "redis": "Healthy"
  }
}
\`\`\`

#### Main Endpoints
\`\`\`
$api_endpoints
\`\`\`

## Deployment

### Docker

\`\`\`bash
docker build -t neo-service-layer/$service_name:latest .
docker run -p 8080:8080 neo-service-layer/$service_name:latest
\`\`\`

### Kubernetes

\`\`\`bash
kubectl apply -f k8s/services/$service_name.yaml
\`\`\`

### Docker Compose

\`\`\`yaml
version: '3.8'
services:
  $service_name:
    image: neo-service-layer/$service_name:latest
    ports:
      - "8080:8080"
    environment:
      - DB_CONNECTION_STRING=Host=postgres;Database=neo_service_layer
      - REDIS_CONNECTION=redis:6379
    depends_on:
      - postgres
      - redis
\`\`\`

## Monitoring

### Metrics

The service exposes Prometheus metrics at \`/metrics\`:

- \`${service_name}_requests_total\`: Total number of requests
- \`${service_name}_request_duration_seconds\`: Request duration histogram
- \`${service_name}_errors_total\`: Total number of errors
- \`${service_name}_active_connections\`: Number of active connections

### Health Checks

- **Liveness**: \`/health/live\` - Basic service liveness
- **Readiness**: \`/health/ready\` - Service ready to accept traffic
- **Startup**: \`/health/startup\` - Service initialization complete

## Security

### Authentication

The service uses JWT bearer token authentication. Include the token in the Authorization header:

\`\`\`
Authorization: Bearer <your-jwt-token>
\`\`\`

### Authorization

Role-based access control (RBAC) is implemented with the following roles:
- \`admin\`: Full access
- \`user\`: Read/write access
- \`readonly\`: Read-only access

### Encryption

- All data is encrypted in transit using TLS 1.3
- Sensitive data is encrypted at rest using AES-256

## Development

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code

### Building

\`\`\`bash
dotnet build
dotnet test
dotnet publish -c Release
\`\`\`

### Running Locally

\`\`\`bash
dotnet run --project src/Services/NeoServiceLayer.Services.$service_name
\`\`\`

### Testing

\`\`\`bash
# Unit tests
dotnet test tests/Unit/NeoServiceLayer.Services.$service_name.Tests

# Integration tests
dotnet test tests/Integration/NeoServiceLayer.Services.$service_name.Integration.Tests
\`\`\`

## Troubleshooting

### Common Issues

1. **Connection Refused**
   - Check if the service is running: \`docker ps\`
   - Verify port configuration: \`netstat -tlnp | grep 8080\`

2. **Database Connection Failed**
   - Verify connection string
   - Check database is running
   - Ensure network connectivity

3. **Authentication Errors**
   - Verify JWT token is valid
   - Check token expiration
   - Ensure correct permissions

### Logs

View service logs:
\`\`\`bash
# Docker
docker logs neo-$service_name

# Kubernetes
kubectl logs -n neo-service-layer deployment/$service_name
\`\`\`

## Contributing

Please see the main [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

This service is part of the Neo Service Layer and is licensed under the MIT License.
EOF

    echo -e "${GREEN}✓ Generated README for $service_name${NC}"
}

# Generate documentation for each service
GENERATED_COUNT=0
for service in "${SERVICES[@]}"; do
    # Find the service directory
    if [ -d "src/Services/NeoServiceLayer.Services.$service" ]; then
        generate_readme "$service" "src/Services/NeoServiceLayer.Services.$service"
        ((GENERATED_COUNT++))
    elif [ -d "src/AI/NeoServiceLayer.$service" ]; then
        generate_readme "$service" "src/AI/NeoServiceLayer.$service"
        ((GENERATED_COUNT++))
    elif [ -d "src/Advanced/NeoServiceLayer.$service" ]; then
        generate_readme "$service" "src/Advanced/NeoServiceLayer.$service"
        ((GENERATED_COUNT++))
    else
        echo -e "${YELLOW}Warning: Could not find directory for service: $service${NC}"
    fi
done

echo -e "\n${GREEN}Documentation generation complete!${NC}"
echo -e "${BLUE}Generated $GENERATED_COUNT README files${NC}"
echo -e "${YELLOW}Note: Please review and customize each README based on actual service functionality${NC}"