# Neo Service Layer - AbstractAccount Service

## Overview

Provides account abstraction functionality for Neo blockchain, enabling smart contract wallets and advanced account features.

## Features

- Smart contract wallets\n- Multi-signature support\n- Social recovery integration\n- Gas abstraction\n- Batch transactions

## Architecture

The AbstractAccount service is built on a microservices architecture with the following components:

- **API Layer**: RESTful API endpoints for external communication
- **Business Logic**: Core service functionality and processing
- **Data Layer**: Persistent storage and caching
- **Security**: Authentication, authorization, and encryption
- **Monitoring**: Health checks and metrics collection

## Configuration

### Environment Variables

```bash
# Service Configuration
SERVICE_NAME=AbstractAccount
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
```

### Configuration File

```json
{
  "Service": {
    "Name": "AbstractAccount",
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
```

## API Endpoints

### Base URL
```
https://api.neo-service-layer.com/v1/AbstractAccount
```

### Endpoints

#### Health Check
```
GET /health
```

Returns the health status of the service.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2025-01-10T10:00:00Z",
  "version": "1.0.0",
  "dependencies": {
    "database": "Healthy",
    "redis": "Healthy"
  }
}
```

#### Main Endpoints
```
/accounts, /accounts/{address}, /accounts/{address}/execute
```

## Deployment

### Docker

```bash
docker build -t neo-service-layer/AbstractAccount:latest .
docker run -p 8080:8080 neo-service-layer/AbstractAccount:latest
```

### Kubernetes

```bash
kubectl apply -f k8s/services/AbstractAccount.yaml
```

### Docker Compose

```yaml
version: '3.8'
services:
  AbstractAccount:
    image: neo-service-layer/AbstractAccount:latest
    ports:
      - "8080:8080"
    environment:
      - DB_CONNECTION_STRING=Host=postgres;Database=neo_service_layer
      - REDIS_CONNECTION=redis:6379
    depends_on:
      - postgres
      - redis
```

## Monitoring

### Metrics

The service exposes Prometheus metrics at `/metrics`:

- `AbstractAccount_requests_total`: Total number of requests
- `AbstractAccount_request_duration_seconds`: Request duration histogram
- `AbstractAccount_errors_total`: Total number of errors
- `AbstractAccount_active_connections`: Number of active connections

### Health Checks

- **Liveness**: `/health/live` - Basic service liveness
- **Readiness**: `/health/ready` - Service ready to accept traffic
- **Startup**: `/health/startup` - Service initialization complete

## Security

### Authentication

The service uses JWT bearer token authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Authorization

Role-based access control (RBAC) is implemented with the following roles:
- `admin`: Full access
- `user`: Read/write access
- `readonly`: Read-only access

### Encryption

- All data is encrypted in transit using TLS 1.3
- Sensitive data is encrypted at rest using AES-256

## Development

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code

### Building

```bash
dotnet build
dotnet test
dotnet publish -c Release
```

### Running Locally

```bash
dotnet run --project src/Services/NeoServiceLayer.Services.AbstractAccount
```

### Testing

```bash
# Unit tests
dotnet test tests/Unit/NeoServiceLayer.Services.AbstractAccount.Tests

# Integration tests
dotnet test tests/Integration/NeoServiceLayer.Services.AbstractAccount.Integration.Tests
```

## Troubleshooting

### Common Issues

1. **Connection Refused**
   - Check if the service is running: `docker ps`
   - Verify port configuration: `netstat -tlnp | grep 8080`

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
```bash
# Docker
docker logs neo-AbstractAccount

# Kubernetes
kubectl logs -n neo-service-layer deployment/AbstractAccount
```

## Contributing

Please see the main [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

This service is part of the Neo Service Layer and is licensed under the MIT License.
