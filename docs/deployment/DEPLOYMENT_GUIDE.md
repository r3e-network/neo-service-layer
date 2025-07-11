# Neo Service Layer Deployment Guide

[![Deployment](https://img.shields.io/badge/deployment-production%20ready-green)](https://github.com/r3e-network/neo-service-layer)
[![Docker](https://img.shields.io/badge/docker-supported-blue)](https://www.docker.com/)
[![Kubernetes](https://img.shields.io/badge/k8s-ready-blue)](https://kubernetes.io/)

> **🚀 Production-Ready Microservices Deployment** - Complete guide for deploying the Neo Service Layer platform

## Table of Contents

- [Prerequisites](#prerequisites)
- [Deployment Architectures](#deployment-architectures) 
- [Quick Start Deployment](#quick-start-deployment)
- [Production Deployment](#production-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Security Configuration](#security-configuration)
- [Monitoring & Observability](#monitoring--observability)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

**Minimum Requirements:**
- **CPU**: 4 cores
- **RAM**: 8GB
- **Storage**: 50GB SSD
- **Network**: 1Gbps connection

**Production Requirements:**
- **CPU**: 8+ cores
- **RAM**: 16GB+
- **Storage**: 200GB+ SSD
- **Network**: 10Gbps connection
- **Load Balancer**: External load balancer for high availability

### Software Requirements

**Required:**
- **.NET 9.0 SDK** or later
- **Docker** 24.0+ and **Docker Compose** 2.0+
- **Git** for source control

**Optional (for advanced features):**
- **Kubernetes** 1.25+ (for K8s deployment)
- **Intel SGX SDK** (for hardware enclaves)
- **Helm** 3.0+ (for Kubernetes deployments)

### Network Requirements

**Required Ports:**
- **7000**: API Gateway (public)
- **8500**: Consul UI (private)
- **16686**: Jaeger UI (private)
- **3000**: Grafana (private)
- **9090**: Prometheus (private)

**Service Ports (Internal):**
- **8081-8099**: Individual microservices
- **5432**: PostgreSQL
- **6379**: Redis
- **5672**: RabbitMQ

## Deployment Architectures

### 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────┐
│                Load Balancer                    │
├─────────────────────────────────────────────────┤
│  🌐 API Gateway (Port 7000)                    │
│     • Authentication    • Rate Limiting         │
│     • Request Routing   • Circuit Breakers      │
├─────────────────────────────────────────────────┤
│  🔍 Service Discovery (Consul)                 │
│  📊 Observability (Jaeger + Prometheus)        │
├─────────────────────────────────────────────────┤
│  ⚙️ Microservices Layer                        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │ Storage  │ │ Key Mgmt │ │   AI     │       │
│  │ Service  │ │ Service  │ │ Services │       │
│  └──────────┘ └──────────┘ └──────────┘       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │  Oracle  │ │Cross-Chain│ │Notification│     │
│  │ Service  │ │ Service   │ │ Service  │       │
│  └──────────┘ └──────────┘ └──────────┘       │
├─────────────────────────────────────────────────┤
│  🗃️ Data Layer                                 │
│     • PostgreSQL    • Redis    • RabbitMQ      │
└─────────────────────────────────────────────────┘
```

### Deployment Options

#### 1. **Development Setup**
- Single node deployment
- All services on one machine
- Simplified configuration
- Perfect for development and testing

#### 2. **Production Single-Node**
- Optimized single-node deployment
- Docker-based with resource limits
- Basic monitoring and logging
- Suitable for small to medium workloads

#### 3. **Production Multi-Node**
- Distributed across multiple nodes
- High availability configuration
- Advanced monitoring and alerting
- Enterprise-grade deployment

#### 4. **Kubernetes Cluster**
- Container orchestration
- Auto-scaling capabilities
- Multi-cloud deployment ready
- Maximum scalability and resilience

## Quick Start Deployment

### Option 1: Complete Microservices Stack (Recommended)

```bash
# 1. Clone the repository
git clone https://github.com/r3e-network/neo-service-layer.git
cd neo-service-layer

# 2. Start complete microservices stack
docker-compose -f docker-compose.microservices-complete.yml up -d

# 3. Wait for services to start (2-3 minutes)
docker ps

# 4. Verify deployment
curl http://localhost:7000/health
curl http://localhost:8500/v1/catalog/services
```

### Option 2: Basic Development Setup

```bash
# 1. Clone repository
git clone https://github.com/r3e-network/neo-service-layer.git
cd neo-service-layer

# 2. Start infrastructure services
docker-compose up -d

# 3. Build and run API service
dotnet run --project src/Api/NeoServiceLayer.Api/

# 4. Access API at http://localhost:5000
```

### Option 3: Individual Service Development

```bash
# Build all services
dotnet build

# Start infrastructure
docker-compose up -d postgres redis consul

# Run individual services
dotnet run --project src/Services/NeoServiceLayer.Services.Storage/ &
dotnet run --project src/Services/NeoServiceLayer.Services.KeyManagement/ &
dotnet run --project src/Services/NeoServiceLayer.Services.Notification/ &

# Run API Gateway
dotnet run --project src/Gateway/NeoServiceLayer.Gateway.Api/
```

## Production Deployment

### Pre-deployment Checklist

- [ ] Hardware/VM requirements met
- [ ] Network ports configured
- [ ] SSL certificates obtained
- [ ] Environment variables configured
- [ ] Backup strategy defined
- [ ] Monitoring tools configured
- [ ] Security review completed

### 1. Environment Setup

```bash
# Create production user
sudo useradd -r -s /bin/false neoservice
sudo mkdir -p /opt/neo-service-layer
sudo chown neoservice:neoservice /opt/neo-service-layer

# Setup directories
sudo mkdir -p /opt/neo-service-layer/{data,logs,config,ssl}
sudo chown -R neoservice:neoservice /opt/neo-service-layer
```

### 2. SSL Certificate Configuration

```bash
# Generate self-signed certificate (for testing)
openssl req -x509 -newkey rsa:4096 -keyout /opt/neo-service-layer/ssl/private.key \
  -out /opt/neo-service-layer/ssl/certificate.crt -days 365 -nodes

# Or use Let's Encrypt (recommended)
sudo certbot certonly --standalone -d your-domain.com
```

### 3. Production Configuration

Create production environment file:

```bash
# /opt/neo-service-layer/.env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:7000;http://+:7001

# Database Configuration
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=neoservice_prod;Username=neouser;Password=STRONG_PASSWORD

# Redis Configuration
ConnectionStrings__Redis=localhost:6379,password=REDIS_PASSWORD

# Security
JWT_SECRET=YOUR_SUPER_SECURE_JWT_SECRET_32_CHARS_MIN
ENCRYPTION_KEY=YOUR_32_CHAR_ENCRYPTION_KEY_HERE

# External Services
NEO_N3_RPC_URL=https://rpc.neo.org:443
NEO_X_RPC_URL=https://neoxt4seed1.ngd.network:443

# Consul Configuration
CONSUL_HTTP_ADDR=http://localhost:8500

# Monitoring
JAEGER_AGENT_HOST=localhost
JAEGER_AGENT_PORT=6831
```

### 4. Database Setup

```bash
# Start PostgreSQL
docker run -d --name neo-postgres-prod \
  -e POSTGRES_DB=neoservice_prod \
  -e POSTGRES_USER=neouser \
  -e POSTGRES_PASSWORD=STRONG_PASSWORD \
  -v /opt/neo-service-layer/data/postgres:/var/lib/postgresql/data \
  -p 5432:5432 \
  --restart unless-stopped \
  postgres:16-alpine

# Initialize database
dotnet ef database update --project src/Api/NeoServiceLayer.Api/
```

### 5. Redis Setup

```bash
# Start Redis
docker run -d --name neo-redis-prod \
  -v /opt/neo-service-layer/data/redis:/data \
  -p 6379:6379 \
  --restart unless-stopped \
  redis:7-alpine redis-server --requirepass REDIS_PASSWORD
```

### 6. Service Discovery Setup

```bash
# Start Consul
docker run -d --name neo-consul-prod \
  -v /opt/neo-service-layer/data/consul:/consul/data \
  -p 8500:8500 \
  -p 8600:8600/udp \
  --restart unless-stopped \
  consul:latest agent -server -bootstrap-expect=1 -ui -client=0.0.0.0
```

### 7. Deploy Microservices

```bash
# Build and deploy all services
docker-compose -f docker-compose.microservices-complete.yml \
  --env-file /opt/neo-service-layer/.env \
  up -d

# Verify deployment
curl -k https://localhost:7000/health
curl http://localhost:8500/v1/catalog/services
```

### 8. Configure Reverse Proxy (Nginx)

```nginx
# /etc/nginx/sites-available/neo-service-layer
server {
    listen 80;
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /opt/neo-service-layer/ssl/certificate.crt;
    ssl_certificate_key /opt/neo-service-layer/ssl/private.key;

    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";

    # API Gateway
    location / {
        proxy_pass http://localhost:7000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req zone=api burst=20 nodelay;
}
```

## Kubernetes Deployment

### 1. Prerequisites

```bash
# Install kubectl and helm
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
curl https://get.helm.sh/helm-v3.13.0-linux-amd64.tar.gz | tar -xzO linux-amd64/helm > /usr/local/bin/helm
```

### 2. Deploy with Helm

```bash
# Create namespace
kubectl create namespace neo-service-layer

# Deploy with Helm
helm install neo-service-layer ./charts/neo-service-layer \
  --namespace neo-service-layer \
  --set image.tag=latest \
  --set ingress.enabled=true \
  --set ingress.hosts[0].host=api.your-domain.com
```

### 3. Kubernetes Manifests

```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: neo-service-layer
---
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: neo-config
  namespace: neo-service-layer
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  CONSUL_HTTP_ADDR: "http://consul:8500"
---
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: neo-api-gateway
  namespace: neo-service-layer
spec:
  replicas: 3
  selector:
    matchLabels:
      app: neo-api-gateway
  template:
    metadata:
      labels:
        app: neo-api-gateway
    spec:
      containers:
      - name: api-gateway
        image: neo-service-layer/api-gateway:latest
        ports:
        - containerPort: 7000
        envFrom:
        - configMapRef:
            name: neo-config
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
```

### 4. Deploy Services

```bash
# Apply all manifests
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n neo-service-layer
kubectl get services -n neo-service-layer
```

## Security Configuration

### 1. JWT Configuration

```json
{
  "Authentication": {
    "JwtSettings": {
      "SecretKey": "your-super-secure-secret-key-32-chars-minimum",
      "Issuer": "neo-service-layer",
      "Audience": "neo-service-layer-api",
      "ExpirationMinutes": 60,
      "RefreshExpirationDays": 7
    }
  }
}
```

### 2. API Rate Limiting

```json
{
  "RateLimiting": {
    "General": {
      "EnableRateLimiting": true,
      "PermitLimit": 100,
      "Window": "00:01:00",
      "ReplenishmentPeriod": "00:00:10",
      "QueueLimit": 0
    }
  }
}
```

### 3. CORS Configuration

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-frontend.com",
      "https://your-admin.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["Authorization", "Content-Type"],
    "AllowCredentials": true
  }
}
```

### 4. SSL/TLS Configuration

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://+:7000",
        "Certificate": {
          "Path": "/opt/neo-service-layer/ssl/certificate.pfx",
          "Password": "certificate-password"
        }
      }
    }
  }
}
```

## Monitoring & Observability

### 1. Prometheus Configuration

```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'neo-services'
    static_configs:
      - targets:
          - 'localhost:7000'  # API Gateway
          - 'localhost:8081'  # Storage Service
          - 'localhost:8082'  # Key Management
          - 'localhost:8083'  # Notification Service

  - job_name: 'consul'
    static_configs:
      - targets: ['localhost:8500']
```

### 2. Grafana Dashboard

```bash
# Start Grafana
docker run -d --name neo-grafana \
  -p 3000:3000 \
  -v /opt/neo-service-layer/data/grafana:/var/lib/grafana \
  --restart unless-stopped \
  grafana/grafana:latest

# Import pre-built dashboard
curl -X POST \
  http://admin:admin@localhost:3000/api/dashboards/db \
  -H 'Content-Type: application/json' \
  -d @grafana/provisioning/dashboards/microservices-dashboard.json
```

### 3. Jaeger Tracing

```bash
# Start Jaeger
docker run -d --name neo-jaeger \
  -p 16686:16686 \
  -p 14268:14268 \
  --restart unless-stopped \
  jaegertracing/all-in-one:latest
```

### 4. Log Aggregation

```bash
# Configure centralized logging
docker run -d --name neo-loki \
  -p 3100:3100 \
  -v /opt/neo-service-layer/data/loki:/loki \
  --restart unless-stopped \
  grafana/loki:latest
```

## Health Checks & Monitoring

### 1. Service Health Endpoints

```bash
# Check overall system health
curl http://localhost:7000/health

# Check individual service health
curl http://localhost:8081/health  # Storage
curl http://localhost:8082/health  # Key Management
curl http://localhost:8083/health  # Notification
```

### 2. Automated Health Monitoring

```bash
#!/bin/bash
# /opt/neo-service-layer/scripts/health-check.sh

SERVICES=(
  "http://localhost:7000/health"
  "http://localhost:8081/health"
  "http://localhost:8082/health"
  "http://localhost:8083/health"
)

for service in "${SERVICES[@]}"; do
  if ! curl -sf "$service" > /dev/null; then
    echo "ALERT: Service $service is down"
    # Send notification/alert
  fi
done
```

### 3. Performance Monitoring

```bash
# Monitor resource usage
docker stats

# Check service discovery
curl http://localhost:8500/v1/health/service/storage-service

# Monitor distributed traces
open http://localhost:16686
```

## Backup & Recovery

### 1. Database Backup

```bash
#!/bin/bash
# Backup script
BACKUP_DIR="/opt/neo-service-layer/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# PostgreSQL backup
docker exec neo-postgres-prod pg_dump -U neouser neoservice_prod > \
  "$BACKUP_DIR/postgres_$TIMESTAMP.sql"

# Redis backup
docker exec neo-redis-prod redis-cli BGSAVE
cp /opt/neo-service-layer/data/redis/dump.rdb \
  "$BACKUP_DIR/redis_$TIMESTAMP.rdb"
```

### 2. Configuration Backup

```bash
# Backup configuration files
tar -czf "/opt/neo-service-layer/backups/config_$TIMESTAMP.tar.gz" \
  /opt/neo-service-layer/config/ \
  /opt/neo-service-layer/.env
```

### 3. Automated Backup

```bash
# Add to crontab
0 2 * * * /opt/neo-service-layer/scripts/backup.sh
```

## Troubleshooting

### Common Issues

#### 1. Service Discovery Issues
```bash
# Check Consul status
curl http://localhost:8500/v1/status/leader

# Re-register services
docker-compose restart consul
```

#### 2. Database Connection Issues
```bash
# Check PostgreSQL logs
docker logs neo-postgres-prod

# Test connection
docker exec neo-postgres-prod psql -U neouser -d neoservice_prod -c "SELECT 1"
```

#### 3. Certificate Issues
```bash
# Verify certificate
openssl x509 -in /opt/neo-service-layer/ssl/certificate.crt -text -noout

# Test SSL endpoint
curl -k https://localhost:7000/health
```

#### 4. Memory Issues
```bash
# Check container memory usage
docker stats

# Adjust memory limits in docker-compose.yml
services:
  api-gateway:
    deploy:
      resources:
        limits:
          memory: 2G
```

### Logging and Debugging

```bash
# View service logs
docker logs neo-api-gateway
docker logs neo-storage-service

# Follow logs in real-time
docker logs -f neo-api-gateway

# Check system logs
journalctl -u docker
```

### Performance Optimization

```bash
# Optimize Docker settings
echo '{"log-driver":"json-file","log-opts":{"max-size":"10m","max-file":"3"}}' > /etc/docker/daemon.json
systemctl restart docker

# Optimize PostgreSQL
# Edit postgresql.conf
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 4MB
```

## Maintenance

### Regular Maintenance Tasks

1. **Weekly:**
   - Review service logs
   - Check disk space usage
   - Verify backup integrity
   - Update security patches

2. **Monthly:**
   - Rotate logs
   - Update dependencies
   - Performance review
   - Security audit

3. **Quarterly:**
   - Disaster recovery test
   - Capacity planning review
   - Architecture review
   - Documentation updates

## Support

- **📖 Documentation**: [Complete deployment docs](../README.md)
- **🐛 Issues**: [GitHub Issues](https://github.com/r3e-network/neo-service-layer/issues)
- **💬 Support**: [GitHub Discussions](https://github.com/r3e-network/neo-service-layer/discussions)
- **📧 Contact**: deployment-support@r3e.network

---

**🚀 Your Neo Service Layer is now production-ready! Monitor, maintain, and scale as needed.**