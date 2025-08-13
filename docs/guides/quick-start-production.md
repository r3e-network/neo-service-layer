# Neo Service Layer - Production Quick Start Guide

## Overview

This guide provides step-by-step instructions for deploying the Neo Service Layer with production-ready security, monitoring, and SGX enclave support. All critical security vulnerabilities have been addressed in this version.

## Prerequisites

### System Requirements

**Hardware**:
- Intel CPU with SGX support (for production SGX features)
- Minimum 16GB RAM (32GB recommended)
- SSD storage (minimum 100GB available)

**Software**:
- Ubuntu 20.04 LTS or later
- .NET 8.0 SDK
- Docker and Docker Compose
- Intel SGX SDK (for SGX features)

**Network**:
- HTTPS certificates for production deployment
- Firewall configuration for required ports
- Load balancer (recommended for production)

### Intel SGX Setup (Optional but Recommended)

```bash
# Install SGX SDK and driver
wget https://download.01.org/intel-sgx/sgx-linux/2.19/distro/ubuntu20.04-server/sgx_linux_x64_sdk_2.19.100.3.bin
chmod +x sgx_linux_x64_sdk_2.19.100.3.bin
./sgx_linux_x64_sdk_2.19.100.3.bin

# Install SGX driver
sudo apt update
sudo apt install -y sgx-aesm-service libsgx-aesm-launch-plugin
sudo systemctl enable aesmd
sudo systemctl start aesmd
```

## Quick Deployment

### 1. Clone and Build

```bash
# Clone repository
git clone https://github.com/your-org/neo-service-layer.git
cd neo-service-layer

# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build --configuration Release
```

### 2. Configuration

Create production configuration file:

```bash
# Create production appsettings
cat > appsettings.Production.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Security": {
    "EncryptionAlgorithm": "AES-256-GCM",
    "KeyRotationIntervalHours": 24,
    "MaxInputSizeMB": 10,
    "EnableRateLimiting": true,
    "DefaultRateLimitRequests": 100,
    "RateLimitWindowMinutes": 1
  },
  "Resilience": {
    "DefaultMaxRetries": 3,
    "DefaultBackoffMs": 1000,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerTimeoutMinutes": 1,
    "CircuitBreakerResetTimeoutMinutes": 5
  },
  "Observability": {
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableHealthChecks": true,
    "ServiceName": "NeoServiceLayer",
    "ServiceVersion": "2.0.0"
  },
  "Tee": {
    "EnclaveType": "SGX",
    "EnclavePath": "./enclave",
    "DebugMode": false,
    "MaxEnclaveMemoryMB": 512,
    "EnableAttestation": true
  }
}
EOF
```

### 3. Environment Variables

```bash
# Set production environment
export ASPNETCORE_ENVIRONMENT=Production

# Set JWT secret (generate secure key)
export JWT_SECRET_KEY="$(openssl rand -base64 64)"

# Set database connection (if using database)
export NEO_CONNECTION_STRING="your_database_connection_string"

# Set SGX configuration
export SGX_MODE=HW  # Use HW for production, SIM for testing
export SGX_DEBUG=0  # Disable debug in production
```

### 4. SSL/TLS Configuration

```bash
# Generate development certificate (replace with CA-signed cert in production)
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# For production, use your CA-signed certificate
# Copy certificate files to appropriate location
# Configure HTTPS in appsettings.json
```

### 5. Run Application

```bash
# Run API server
cd src/Api/NeoServiceLayer.Api
dotnet run --configuration Release --environment Production

# Or run web application
cd src/Web/NeoServiceLayer.Web
dotnet run --configuration Release --environment Production
```

## Docker Deployment

### 1. Build Docker Images

```bash
# Build API image
docker build -f src/Api/NeoServiceLayer.Api/Dockerfile -t neo-service-layer-api:latest .

# Build Web image
docker build -f src/Web/NeoServiceLayer.Web/Dockerfile -t neo-service-layer-web:latest .
```

### 2. Docker Compose

Create `docker-compose.production.yml`:

```yaml
version: '3.8'

services:
  neo-api:
    image: neo-service-layer-api:latest
    container_name: neo-api
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - SGX_MODE=HW
    ports:
      - "5000:80"
      - "5001:443"
    volumes:
      - ./certs:/app/certs:ro
      - /dev/sgx:/dev/sgx
    devices:
      - /dev/sgx_enclave:/dev/sgx_enclave
      - /dev/sgx_provision:/dev/sgx_provision
    depends_on:
      - redis
      - postgres
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s

  neo-web:
    image: neo-service-layer-web:latest
    container_name: neo-web
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    ports:
      - "8000:80"
      - "8001:443"
    depends_on:
      - neo-api
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  redis:
    image: redis:7-alpine
    container_name: neo-redis
    restart: unless-stopped
    command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"

  postgres:
    image: postgres:15-alpine
    container_name: neo-postgres
    restart: unless-stopped
    environment:
      - POSTGRES_DB=neoservice
      - POSTGRES_USER=neoservice
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  nginx:
    image: nginx:alpine
    container_name: neo-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/nginx/certs:ro
    depends_on:
      - neo-api
      - neo-web

volumes:
  redis_data:
  postgres_data:

networks:
  default:
    driver: bridge
```

### 3. Deploy with Docker Compose

```bash
# Set required environment variables
export JWT_SECRET_KEY="$(openssl rand -base64 64)"
export REDIS_PASSWORD="$(openssl rand -base64 32)"
export POSTGRES_PASSWORD="$(openssl rand -base64 32)"

# Deploy stack
docker-compose -f docker-compose.production.yml up -d

# Check status
docker-compose -f docker-compose.production.yml ps
```

## Health Checks and Monitoring

### 1. Health Check Endpoints

```bash
# API health check
curl https://your-domain.com/health

# Detailed health check
curl https://your-domain.com/health/detailed

# Metrics endpoint
curl https://your-domain.com/metrics
```

### 2. Security Validation

```bash
# Test security features
curl -X POST https://your-domain.com/api/security/validate \
  -H "Content-Type: application/json" \
  -d '{"input": "normal text"}'

# Test with SQL injection (should be blocked)
curl -X POST https://your-domain.com/api/security/validate \
  -H "Content-Type: application/json" \
  -d '{"input": "'; DROP TABLE users; --"}'
```

### 3. SGX Validation

```bash
# Check SGX status
curl https://your-domain.com/api/enclave/status

# Test enclave operations
curl -X POST https://your-domain.com/api/enclave/execute \
  -H "Content-Type: application/json" \
  -d '{"script": "Math.sqrt(16)", "data": "{}"}'
```

## Security Configuration

### 1. Input Validation

The system automatically validates all input against:
- SQL injection attacks
- Cross-site scripting (XSS)
- Code injection attempts
- Malformed input

Configuration in `appsettings.json`:
```json
{
  "Security": {
    "ValidationSettings": {
      "EnableSqlInjectionCheck": true,
      "EnableXssCheck": true,
      "EnableCodeInjectionCheck": true,
      "MaxInputSize": 10485760
    }
  }
}
```

### 2. Encryption

All sensitive data is encrypted using AES-256-GCM:
```json
{
  "Security": {
    "EncryptionSettings": {
      "Algorithm": "AES-256-GCM",
      "KeySize": 256,
      "KeyRotationIntervalHours": 24
    }
  }
}
```

### 3. Rate Limiting

Protect against brute force and DoS attacks:
```json
{
  "Security": {
    "RateLimiting": {
      "EnableRateLimiting": true,
      "DefaultRequests": 100,
      "WindowMinutes": 1,
      "BlockDurationMinutes": 15
    }
  }
}
```

### 4. SGX Security

Hardware-backed security with Intel SGX:
```json
{
  "Tee": {
    "SecuritySettings": {
      "EnableAttestation": true,
      "AttestationProvider": "Intel",
      "MaxDataSize": 104857600,
      "MaxExecutionTime": 30000
    }
  }
}
```

## Performance Optimization

### 1. Production Settings

```json
{
  "Performance": {
    "EnableResponseCaching": true,
    "EnableResponseCompression": true,
    "MaxConcurrentConnections": 1000,
    "RequestTimeoutSeconds": 30
  }
}
```

### 2. Database Optimization

```json
{
  "Database": {
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "MaxPoolSize": 100,
    "EnableSensitiveDataLogging": false
  }
}
```

## Testing Production Deployment

### 1. Run Comprehensive Tests

```bash
# Run all tests
./scripts/run-tests.sh

# Run security-only tests
./scripts/run-tests.sh --security-only

# Run performance benchmarks
./scripts/run-tests.sh --performance-only
```

### 2. Load Testing

```bash
# Install load testing tools
npm install -g artillery

# Create load test configuration
cat > load-test.yml << EOF
config:
  target: 'https://your-domain.com'
  phases:
    - duration: 60
      arrivalRate: 10
    - duration: 120
      arrivalRate: 20
    - duration: 60
      arrivalRate: 10

scenarios:
  - name: "API Health Check"
    requests:
      - get:
          url: "/health"
  - name: "Security Validation"
    requests:
      - post:
          url: "/api/security/validate"
          json:
            input: "test input"
EOF

# Run load test
artillery run load-test.yml
```

## Monitoring and Observability

### 1. Metrics Collection

The system provides comprehensive metrics:
- Request/response metrics
- Security threat detection metrics
- SGX operation metrics
- Performance metrics
- Error rates and latencies

### 2. Logging

Structured logging with correlation IDs:
```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "INFO",
  "message": "Security validation completed",
  "correlationId": "abc123",
  "userId": "user123",
  "requestId": "req456",
  "duration": 25.5,
  "threatDetected": false
}
```

### 3. Health Monitoring

Configure health check monitoring:
```bash
# Install monitoring agent
curl -sSL https://get.datadoghq.com/install.sh | DD_AGENT_MAJOR_VERSION=7 DD_API_KEY=your_key bash

# Configure health check monitoring
cat > /etc/datadog-agent/conf.d/http_check.d/conf.yaml << EOF
init_config:

instances:
  - name: neo-service-layer-api
    url: https://your-domain.com/health
    timeout: 5
    method: GET
EOF
```

## Troubleshooting

### 1. Common Issues

**SGX Not Available**:
```bash
# Check SGX support
ls /dev/sgx*

# Check SGX service
systemctl status aesmd

# Check SGX capability
sgx-detect
```

**SSL Certificate Issues**:
```bash
# Verify certificate
openssl x509 -in certificate.crt -text -noout

# Test SSL connection
openssl s_client -connect your-domain.com:443
```

**Database Connection Issues**:
```bash
# Test database connection
psql -h localhost -U neoservice -d neoservice -c "SELECT 1;"
```

### 2. Log Analysis

```bash
# View application logs
docker logs neo-api --tail 100 -f

# Search for errors
docker logs neo-api 2>&1 | grep -i error

# Monitor security events
docker logs neo-api 2>&1 | grep "security\|threat"
```

## Production Checklist

### Pre-Deployment
- [ ] SSL certificates installed and configured
- [ ] Environment variables configured securely
- [ ] Database migrations applied
- [ ] SGX hardware verified (if applicable)
- [ ] Load balancer configured
- [ ] Firewall rules configured
- [ ] Backup strategy implemented

### Post-Deployment
- [ ] Health checks passing
- [ ] Security tests passing
- [ ] Performance tests meeting requirements
- [ ] Monitoring and alerting configured
- [ ] Log aggregation configured
- [ ] Backup verification completed
- [ ] Disaster recovery plan tested

### Security Verification
- [ ] Input validation working correctly
- [ ] Rate limiting enforced
- [ ] Encryption functioning properly
- [ ] Authentication working
- [ ] SGX attestation successful (if applicable)
- [ ] Security monitoring active
- [ ] Vulnerability scanning completed

This production deployment provides enterprise-grade security with comprehensive protection against all vulnerabilities identified in the system review. The deployment includes input validation, encryption, SGX hardware security, rate limiting, and comprehensive monitoring.