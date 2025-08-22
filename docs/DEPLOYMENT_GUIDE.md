# Neo Service Layer - Production Deployment Guide

## Table of Contents
- [Prerequisites](#prerequisites)
- [Deployment Strategies](#deployment-strategies)
- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Database Setup](#database-setup)
- [Security Configuration](#security-configuration)
- [Monitoring Setup](#monitoring-setup)
- [Scaling Configuration](#scaling-configuration)
- [Backup and Recovery](#backup-and-recovery)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements
- **CPU**: 8+ cores (16+ recommended for production)
- **Memory**: 16GB minimum (32GB+ recommended)
- **Storage**: 100GB SSD minimum (500GB+ for production)
- **OS**: Ubuntu 20.04/22.04 LTS, RHEL 8+, or compatible
- **Container Runtime**: Docker 20.10+ or containerd 1.5+
- **Kubernetes**: 1.24+ (if using K8s deployment)

### Software Dependencies
```bash
# Install Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Install kubectl
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

# Install Helm
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Install PostgreSQL client
sudo apt-get update
sudo apt-get install -y postgresql-client-14
```

### Network Requirements
- **Ports**:
  - 8080: HTTP API Gateway
  - 8443: HTTPS API Gateway
  - 5432: PostgreSQL Database
  - 6379: Redis Cache
  - 9090: Prometheus Metrics
  - 3000: Grafana Dashboard
  - 4317: OpenTelemetry Collector

## Deployment Strategies

### 1. Single-Node Docker Deployment
Best for: Development, testing, small-scale production

### 2. Multi-Node Docker Swarm
Best for: Medium-scale production with basic orchestration

### 3. Kubernetes Deployment
Best for: Large-scale production with advanced orchestration

### 4. SGX-Enabled Deployment
Best for: High-security environments requiring confidential computing

## Docker Deployment

### 1. Clone Repository
```bash
git clone https://github.com/your-org/neo-service-layer.git
cd neo-service-layer
```

### 2. Configure Environment
```bash
# Copy environment template
cp .env.example .env

# Edit configuration
nano .env

# Required changes:
# - Set strong passwords for all services
# - Configure JWT secret key
# - Set appropriate resource limits
# - Configure backup locations
```

### 3. Build Images
```bash
# Build all services
docker-compose build --parallel

# Or build specific service
docker-compose build api-gateway
```

### 4. Initialize Database
```bash
# Start PostgreSQL only
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
./scripts/wait-for-postgres.sh

# Run migrations
./scripts/run-database-migrations.sh migrate

# Load initial data (optional)
./scripts/load-seed-data.sh
```

### 5. Start Services
```bash
# Start all services
docker-compose up -d

# Verify all services are running
docker-compose ps

# Check logs
docker-compose logs -f api-gateway
```

### 6. Health Verification
```bash
# Check API health
curl http://localhost:8080/health

# Check database connectivity
curl http://localhost:8080/health/db

# Check Redis connectivity
curl http://localhost:8080/health/redis
```

## Kubernetes Deployment

### 1. Create Namespace
```bash
kubectl create namespace neo-service-layer
kubectl config set-context --current --namespace=neo-service-layer
```

### 2. Deploy Secrets
```bash
# Create database secret
kubectl create secret generic postgres-secret \
  --from-literal=password='YourSecurePassword'

# Create JWT secret
kubectl create secret generic jwt-secret \
  --from-literal=key='YourJWTSecretKey'

# Create Redis secret
kubectl create secret generic redis-secret \
  --from-literal=password='YourRedisPassword'
```

### 3. Deploy ConfigMaps
```bash
# Apply all ConfigMaps
kubectl apply -f k8s/configmaps/
```

### 4. Deploy PostgreSQL
```bash
# Deploy PostgreSQL StatefulSet
kubectl apply -f k8s/postgresql/

# Wait for PostgreSQL to be ready
kubectl wait --for=condition=ready pod -l app=postgresql --timeout=300s

# Run migrations
kubectl exec -it postgresql-0 -- psql -U neoservice_app -d neoservice < migrations/InitialCreate.sql
```

### 5. Deploy Redis
```bash
kubectl apply -f k8s/redis/
kubectl wait --for=condition=ready pod -l app=redis --timeout=120s
```

### 6. Deploy Services
```bash
# Deploy all services
kubectl apply -f k8s/services/

# Or use Kustomize
kubectl apply -k k8s/

# Verify deployments
kubectl get deployments
kubectl get pods
kubectl get services
```

### 7. Setup Ingress
```bash
# Install NGINX Ingress Controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml

# Apply ingress rules
kubectl apply -f k8s/ingress/neo-service-ingress.yaml
```

### 8. Enable Autoscaling
```bash
# Apply HPA configurations
kubectl apply -f k8s/autoscaling/

# Verify HPA
kubectl get hpa
```

## Database Setup

### 1. Production Configuration
```yaml
# PostgreSQL production settings
postgresql.conf: |
  max_connections = 200
  shared_buffers = 4GB
  effective_cache_size = 12GB
  maintenance_work_mem = 1GB
  checkpoint_completion_target = 0.9
  wal_buffers = 16MB
  default_statistics_target = 100
  random_page_cost = 1.1
  effective_io_concurrency = 200
  work_mem = 20MB
  min_wal_size = 2GB
  max_wal_size = 8GB
  max_worker_processes = 8
  max_parallel_workers_per_gather = 4
  max_parallel_workers = 8
  max_parallel_maintenance_workers = 4
```

### 2. Replication Setup
```bash
# Primary server
./scripts/setup-postgresql-primary.sh

# Replica servers
./scripts/setup-postgresql-replica.sh primary-host
```

### 3. Connection Pooling
```yaml
# PgBouncer configuration
pgbouncer.ini: |
  [databases]
  neoservice = host=postgres port=5432 dbname=neoservice
  
  [pgbouncer]
  pool_mode = transaction
  max_client_conn = 1000
  default_pool_size = 25
  reserve_pool_size = 5
  reserve_pool_timeout = 3
  server_lifetime = 3600
  server_idle_timeout = 600
```

## Security Configuration

### 1. TLS/SSL Setup
```bash
# Generate certificates
./scripts/generate-certificates.sh

# Configure TLS in services
kubectl create secret tls neo-service-tls \
  --cert=certs/server.crt \
  --key=certs/server.key
```

### 2. Network Policies
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: api-gateway-policy
spec:
  podSelector:
    matchLabels:
      app: api-gateway
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 8080
```

### 3. RBAC Configuration
```bash
# Apply RBAC rules
kubectl apply -f k8s/rbac/

# Create service accounts
kubectl create serviceaccount neo-service-sa
```

### 4. Security Scanning
```bash
# Scan images for vulnerabilities
trivy image neo-service-layer:latest

# Scan Kubernetes configurations
kubesec scan k8s/services/*.yaml

# Run security audit
./scripts/security-audit.sh
```

## Monitoring Setup

### 1. Deploy Prometheus
```bash
kubectl apply -f k8s/monitoring/prometheus-deployment.yaml
kubectl apply -f k8s/monitoring/prometheus-rules.yaml
```

### 2. Deploy Grafana
```bash
kubectl apply -f k8s/monitoring/grafana-deployment.yaml

# Get admin password
kubectl get secret grafana-secret -o jsonpath="{.data.admin-password}" | base64 --decode
```

### 3. Configure Alerts
```yaml
# alertmanager.yml
global:
  smtp_from: 'alerts@neoservice.local'
  smtp_smarthost: 'smtp.example.com:587'
  smtp_auth_username: 'alerts@neoservice.local'
  smtp_auth_password: 'password'

route:
  group_by: ['alertname', 'cluster', 'service']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 12h
  receiver: 'team-alerts'

receivers:
- name: 'team-alerts'
  email_configs:
  - to: 'team@example.com'
```

### 4. OpenTelemetry Setup
```bash
# Deploy OpenTelemetry Collector
kubectl apply -f k8s/monitoring/otel-collector.yaml

# Configure exporters
kubectl apply -f k8s/monitoring/otel-exporters.yaml
```

## Scaling Configuration

### 1. Horizontal Pod Autoscaling
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: api-gateway-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: api-gateway
  minReplicas: 3
  maxReplicas: 20
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### 2. Vertical Pod Autoscaling
```bash
# Install VPA
kubectl apply -f https://github.com/kubernetes/autoscaler/releases/latest/download/vertical-pod-autoscaler.yaml

# Configure VPA
kubectl apply -f k8s/autoscaling/vpa/
```

### 3. Cluster Autoscaling
```yaml
# For cloud providers (AWS, GCP, Azure)
apiVersion: autoscaling/v1
kind: ClusterAutoscaler
spec:
  minNodes: 3
  maxNodes: 50
  scaleDownDelay: 10m
  scaleDownUnneededTime: 10m
```

## Backup and Recovery

### 1. Database Backups
```bash
# Automated daily backups
0 2 * * * /opt/neo-service/scripts/backup-postgresql.sh

# Manual backup
./scripts/backup-postgresql.sh manual

# Verify backup
./scripts/verify-backup.sh backup-20240115-020000.sql.gz
```

### 2. Volume Snapshots
```yaml
apiVersion: snapshot.storage.k8s.io/v1
kind: VolumeSnapshot
metadata:
  name: postgres-snapshot
spec:
  volumeSnapshotClassName: fast-ssd-snapshot
  source:
    persistentVolumeClaimName: postgres-pvc
```

### 3. Disaster Recovery
```bash
# Full system backup
./scripts/disaster-recovery-backup.sh

# Restore from backup
./scripts/disaster-recovery-restore.sh backup-20240115.tar.gz

# Test recovery procedure
./scripts/test-disaster-recovery.sh
```

## Troubleshooting

### Common Issues

#### 1. Service Won't Start
```bash
# Check logs
kubectl logs -f deployment/api-gateway --tail=100

# Check events
kubectl get events --sort-by='.lastTimestamp'

# Describe pod
kubectl describe pod api-gateway-xxxxx
```

#### 2. Database Connection Issues
```bash
# Test connectivity
kubectl exec -it api-gateway-xxxxx -- nc -zv postgres 5432

# Check credentials
kubectl get secret postgres-secret -o yaml

# Verify database exists
kubectl exec -it postgresql-0 -- psql -U postgres -l
```

#### 3. High Memory Usage
```bash
# Check memory usage
kubectl top pods
kubectl top nodes

# Get detailed metrics
kubectl exec -it api-gateway-xxxxx -- cat /proc/meminfo

# Analyze heap dump
kubectl exec -it api-gateway-xxxxx -- dotnet-dump collect
```

#### 4. Performance Issues
```bash
# Enable detailed logging
kubectl set env deployment/api-gateway LOG_LEVEL=Debug

# Capture performance trace
kubectl exec -it api-gateway-xxxxx -- dotnet-trace collect

# Analyze with profiler
kubectl port-forward api-gateway-xxxxx 9999:9999
# Access profiler at http://localhost:9999
```

### Health Check Endpoints

| Endpoint | Description | Expected Response |
|----------|-------------|-------------------|
| `/health` | Overall health | 200 OK |
| `/health/ready` | Readiness check | 200 OK |
| `/health/live` | Liveness check | 200 OK |
| `/health/db` | Database health | 200 OK with connection info |
| `/health/redis` | Redis health | 200 OK with ping response |
| `/metrics` | Prometheus metrics | Text format metrics |

### Support Contacts

- **Production Issues**: ops-team@example.com
- **Security Incidents**: security@example.com
- **General Support**: support@example.com
- **On-Call**: +1-555-0100 (24/7)

## Post-Deployment Checklist

- [ ] All services are running and healthy
- [ ] Database migrations completed successfully
- [ ] SSL/TLS certificates installed and valid
- [ ] Monitoring and alerting configured
- [ ] Backup jobs scheduled and tested
- [ ] Security scanning completed
- [ ] Load testing performed
- [ ] Documentation updated
- [ ] Team trained on operations
- [ ] Disaster recovery plan tested