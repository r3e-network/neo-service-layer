# Neo Service Layer - Production Deployment Guide

This guide provides comprehensive instructions for deploying the Neo Service Layer to production environments with enterprise-grade reliability, security, and scalability.

## 🏗️ Architecture Overview

The Neo Service Layer is a microservices-based blockchain service platform designed for:
- **High Availability**: Multi-replica deployments with auto-scaling
- **Security**: SGX/TEE enclaves, network policies, and zero-trust architecture
- **Observability**: Comprehensive monitoring, tracing, and metrics
- **Resilience**: Circuit breakers, retries, and graceful degradation

## 📋 Prerequisites

### Infrastructure Requirements
- **Kubernetes Cluster**: v1.25+ with at least 3 worker nodes
- **Node Specifications**: 
  - CPU: 4+ cores per node
  - Memory: 16GB+ per node
  - Storage: 100GB+ SSD per node
- **Network**: Load balancer with SSL/TLS termination
- **DNS**: Custom domain configured for public access

### Required Tools
```bash
# Install required CLI tools
kubectl version --client      # v1.25+
helm version                  # v3.10+
docker --version             # v20.10+
```

### External Dependencies
- **PostgreSQL**: v15+ (managed or self-hosted)
- **Redis**: v7+ (managed or cluster mode)
- **RabbitMQ**: v3.11+ (clustered for HA)
- **SSL Certificates**: Let's Encrypt or commercial CA

## 🚀 Quick Start Deployment

### 1. Clone and Configure
```bash
git clone https://github.com/your-org/neo-service-layer.git
cd neo-service-layer

# Copy and edit configuration
cp k8s/secret.yaml k8s/secret-production.yaml
```

### 2. Configure Secrets
Edit `k8s/secret-production.yaml` with production values:

```yaml
stringData:
  # Database - Use strong passwords
  Database__Password: "$(openssl rand -base64 32)"
  
  # Redis - Enable password authentication
  Redis__Password: "$(openssl rand -base64 32)"
  
  # RabbitMQ - Secure messaging
  RabbitMQ__Password: "$(openssl rand -base64 32)"
  
  # JWT - 256-bit key for production
  Jwt__Key: "$(openssl rand -base64 64)"
  
  # Encryption - For sensitive data
  Encryption__Key: "$(openssl rand -base64 32)"
  Encryption__IV: "$(openssl rand -base64 16)"
  
  # API Keys - External service integration
  Oracle__ApiKey: "your-oracle-api-key"
  CrossChain__ApiKey: "your-crosschain-api-key"
```

### 3. Deploy with Script
```bash
# Make deployment script executable
chmod +x scripts/deploy-k8s.sh

# Deploy to production
./scripts/deploy-k8s.sh production
```

### 4. Verify Deployment
```bash
# Check all pods are running
kubectl get pods -n neo-service-layer

# Check services are accessible
kubectl get services -n neo-service-layer

# Test API Gateway health
curl -k https://api.neo-service.io/health
```

## 🔧 Detailed Configuration

### Environment Configuration

#### ConfigMap Settings
Key configurations in `k8s/configmap.yaml`:

```yaml
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  
  # Database Connection
  Database__Provider: "PostgreSQL"
  Database__ConnectionString: "Host=postgres-service;Port=5432;Database=neoservicelayer;Username=nsluser"
  
  # Redis Configuration
  Redis__Configuration: "redis-service:6379"
  Redis__InstanceName: "NSL"
  
  # Service Discovery
  Consul__Host: "consul-service"
  Consul__Port: "8500"
  
  # Performance Tuning
  RateLimiting__PermitLimit: "100"
  RateLimiting__Window: "60"
  
  # Circuit Breaker Settings
  CircuitBreaker__FailureThreshold: "5"
  CircuitBreaker__BreakDuration: "30"
```

#### Production Secrets Management

**Option 1: Kubernetes Secrets** (Basic)
```bash
kubectl apply -f k8s/secret-production.yaml
```

**Option 2: External Secret Store** (Recommended)
```bash
# Install External Secrets Operator
helm repo add external-secrets https://charts.external-secrets.io
helm install external-secrets external-secrets/external-secrets -n external-secrets-system --create-namespace

# Configure with Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
```

### SSL/TLS Configuration

#### Certificate Management
```bash
# Install cert-manager
helm repo add jetstack https://charts.jetstack.io
helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --version v1.13.0 \
  --set installCRDs=true

# Apply Let's Encrypt ClusterIssuer
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@neo-service.io
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

### Database Setup

#### PostgreSQL Production Configuration
```bash
# Using managed PostgreSQL (recommended)
# Azure Database for PostgreSQL, AWS RDS, or Google Cloud SQL

# Or deploy PostgreSQL with high availability
helm repo add bitnami https://charts.bitnami.com/bitnami
helm install postgresql-ha bitnami/postgresql-ha \
  --namespace neo-service-layer \
  --set persistence.size=100Gi \
  --set metrics.enabled=true
```

#### Database Migration
```bash
# Run database migrations
kubectl run migration --rm -i --tty \
  --image=neoservicelayer/api:latest \
  --env="ConnectionStrings__DefaultConnection=Host=postgres;Database=neoservicelayer;Username=nsluser;Password=your-password" \
  -- dotnet ef database update
```

### Monitoring and Observability

#### Prometheus and Grafana Setup
```bash
# Install monitoring stack
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install kube-prometheus-stack prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --set prometheus.prometheusSpec.serviceMonitorSelectorNilUsesHelmValues=false
```

#### Jaeger Tracing
```bash
# Install Jaeger Operator
kubectl create namespace observability-system
kubectl apply -f https://github.com/jaegertracing/jaeger-operator/releases/download/v1.49.0/jaeger-operator.yaml -n observability-system

# Deploy Jaeger instance
kubectl apply -f - <<EOF
apiVersion: jaegertracing.io/v1
kind: Jaeger
metadata:
  name: neo-service-jaeger
  namespace: neo-service-layer
spec:
  strategy: production
  storage:
    type: elasticsearch
    options:
      es:
        server-urls: http://elasticsearch:9200
EOF
```

## 📊 Production Scaling

### Horizontal Pod Autoscaler (HPA)
HPA is already configured in service deployments:

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
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

### Vertical Pod Autoscaler (VPA)
```bash
# Install VPA (optional)
git clone https://github.com/kubernetes/autoscaler.git
cd autoscaler/vertical-pod-autoscaler/
./hack/vpa-up.sh
```

### Cluster Autoscaler
Configure based on your cloud provider:

**AWS EKS:**
```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/autoscaler/master/cluster-autoscaler/cloudprovider/aws/examples/cluster-autoscaler-autodiscover.yaml
```

**Azure AKS:**
```bash
az aks update \
  --resource-group myResourceGroup \
  --name myAKSCluster \
  --enable-cluster-autoscaler \
  --min-count 3 \
  --max-count 10
```

## 🔒 Security Hardening

### Network Security

#### Network Policies
Network policies are included in the deployment to secure pod-to-pod communication:

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: neo-service-network-policy
spec:
  podSelector: {}
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - podSelector: {}
    ports:
    - protocol: TCP
      port: 8080
```

#### Pod Security Standards
```bash
# Apply Pod Security Standards
kubectl label namespace neo-service-layer \
  pod-security.kubernetes.io/enforce=restricted \
  pod-security.kubernetes.io/audit=restricted \
  pod-security.kubernetes.io/warn=restricted
```

### RBAC Configuration
```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: neo-service-account
  namespace: neo-service-layer
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: neo-service-role
rules:
- apiGroups: [""]
  resources: ["configmaps", "secrets"]
  verbs: ["get", "list"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: neo-service-binding
subjects:
- kind: ServiceAccount
  name: neo-service-account
roleRef:
  kind: Role
  name: neo-service-role
  apiGroup: rbac.authorization.k8s.io
```

## 🔍 Monitoring and Alerting

### Health Check Endpoints

The platform provides comprehensive health checks:

- **`/health`**: Detailed health status with all dependencies
- **`/health/ready`**: Kubernetes readiness probe
- **`/health/live`**: Kubernetes liveness probe
- **`/info`**: Service version and environment information

### Key Metrics to Monitor

#### Application Metrics
- **Request Rate**: Requests per second per service
- **Response Time**: P50, P95, P99 percentiles
- **Error Rate**: 4xx/5xx errors as percentage
- **Throughput**: Successful operations per second

#### Infrastructure Metrics
- **CPU Usage**: Target <70% average
- **Memory Usage**: Target <80% of limits
- **Network I/O**: Monitor for bottlenecks
- **Disk Usage**: Monitor PostgreSQL and logs

#### Business Metrics
- **Active Connections**: Connected clients
- **Transaction Volume**: Processed transactions
- **Oracle Requests**: External API calls
- **Cache Hit Rate**: Redis performance

### Alerting Rules

```yaml
groups:
- name: neo-service-layer
  rules:
  - alert: HighErrorRate
    expr: rate(http_requests_total{code=~"5.."}[5m]) > 0.1
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "High error rate detected"
      
  - alert: HighMemoryUsage
    expr: container_memory_usage_bytes / container_spec_memory_limit_bytes > 0.9
    for: 10m
    labels:
      severity: critical
    annotations:
      summary: "Container memory usage over 90%"
      
  - alert: DatabaseConnectionFailed
    expr: up{job="postgres-exporter"} == 0
    for: 2m
    labels:
      severity: critical
    annotations:
      summary: "PostgreSQL connection failed"
```

## 🔄 Backup and Recovery

### Database Backup Strategy

#### Automated Backups
```bash
# PostgreSQL backup CronJob
kubectl apply -f - <<EOF
apiVersion: batch/v1
kind: CronJob
metadata:
  name: postgres-backup
  namespace: neo-service-layer
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          restartPolicy: OnFailure
          containers:
          - name: postgres-backup
            image: postgres:16-alpine
            env:
            - name: PGPASSWORD
              valueFrom:
                secretKeyRef:
                  name: neo-service-secrets
                  key: Database__Password
            command:
            - /bin/bash
            - -c
            - |
              pg_dump -h postgres-service -U nsluser neoservicelayer | \
              gzip > /backup/neoservicelayer-\$(date +%Y%m%d-%H%M%S).sql.gz
              # Upload to cloud storage (AWS S3, Azure Blob, etc.)
            volumeMounts:
            - name: backup-storage
              mountPath: /backup
          volumes:
          - name: backup-storage
            persistentVolumeClaim:
              claimName: backup-pvc
EOF
```

### Application State Backup
```bash
# Backup Kubernetes resources
kubectl get all,configmaps,secrets,pvc -n neo-service-layer -o yaml > neo-service-layer-backup.yaml

# Schedule regular backups with Velero
velero install --provider aws --bucket neo-service-backups
velero schedule create daily-backup --schedule="0 1 * * *" --include-namespaces neo-service-layer
```

## 🚨 Troubleshooting

### Common Issues

#### Pod Startup Issues
```bash
# Check pod events
kubectl describe pod <pod-name> -n neo-service-layer

# Check logs
kubectl logs <pod-name> -n neo-service-layer --previous

# Check resource constraints
kubectl top pods -n neo-service-layer
```

#### Database Connection Issues
```bash
# Test database connectivity
kubectl run postgres-test --rm -i --tty \
  --image=postgres:16-alpine \
  --env="PGPASSWORD=your-password" \
  -- psql -h postgres-service -U nsluser -d neoservicelayer -c "SELECT version();"
```

#### Performance Issues
```bash
# Check resource usage
kubectl top pods -n neo-service-layer
kubectl top nodes

# Check HPA status
kubectl get hpa -n neo-service-layer

# View detailed metrics
kubectl port-forward svc/prometheus-server 9090:80 -n monitoring
# Open http://localhost:9090
```

### Log Analysis

#### Centralized Logging
```bash
# Install ELK Stack
helm repo add elastic https://helm.elastic.co
helm install elasticsearch elastic/elasticsearch -n logging --create-namespace
helm install kibana elastic/kibana -n logging
helm install filebeat elastic/filebeat -n logging
```

#### Log Queries
```bash
# Search for errors
kubectl logs -l app=api-gateway -n neo-service-layer | grep ERROR

# Follow logs in real-time
kubectl logs -f deployment/api-gateway -n neo-service-layer
```

## 📈 Performance Optimization

### Database Optimization

#### Connection Pooling
Configure in `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=neoservicelayer;Username=nsluser;Password=password;Pooling=true;MinPoolSize=5;MaxPoolSize=100;ConnectionIdleLifetime=300"
  }
}
```

#### Query Optimization
```sql
-- Add indexes for frequently queried columns
CREATE INDEX CONCURRENTLY idx_transactions_timestamp ON transactions(created_at);
CREATE INDEX CONCURRENTLY idx_users_active ON users(is_active, created_at);

-- Monitor slow queries
SELECT query, calls, total_time, mean_time 
FROM pg_stat_statements 
ORDER BY total_time DESC LIMIT 10;
```

### Redis Optimization

#### Memory Configuration
```yaml
# Redis configuration
apiVersion: v1
kind: ConfigMap
metadata:
  name: redis-config
data:
  redis.conf: |
    maxmemory 512mb
    maxmemory-policy allkeys-lru
    save 900 1
    save 300 10
    save 60 10000
```

### Application Optimization

#### Resource Limits
```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

#### JVM Tuning (if applicable)
```yaml
env:
- name: JVM_OPTS
  value: "-Xms256m -Xmx512m -XX:+UseG1GC -XX:MaxGCPauseMillis=200"
```

## 🔄 Continuous Deployment

### GitOps with ArgoCD

```bash
# Install ArgoCD
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

# Create application
kubectl apply -f - <<EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: neo-service-layer
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/your-org/neo-service-layer
    targetRevision: main
    path: k8s
  destination:
    server: https://kubernetes.default.svc
    namespace: neo-service-layer
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
EOF
```

### Blue-Green Deployment
```bash
# Using Argo Rollouts
kubectl apply -f - <<EOF
apiVersion: argoproj.io/v1alpha1
kind: Rollout
metadata:
  name: api-gateway-rollout
spec:
  replicas: 5
  strategy:
    blueGreen:
      activeService: api-gateway-active
      previewService: api-gateway-preview
      autoPromotionEnabled: false
      prePromotionAnalysis:
        templates:
        - templateName: success-rate
        args:
        - name: service-name
          value: api-gateway-preview
      postPromotionAnalysis:
        templates:
        - templateName: success-rate
        args:
        - name: service-name
          value: api-gateway-active
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
    spec:
      containers:
      - name: api-gateway
        image: neoservicelayer/api-gateway:latest
EOF
```

## 📞 Support and Maintenance

### Regular Maintenance Tasks

#### Weekly Tasks
- [ ] Review application logs for errors
- [ ] Check resource usage and scaling metrics
- [ ] Verify backup integrity
- [ ] Update security patches

#### Monthly Tasks
- [ ] Review and rotate secrets
- [ ] Analyze performance trends
- [ ] Update dependencies
- [ ] Capacity planning review

#### Quarterly Tasks
- [ ] Disaster recovery testing
- [ ] Security audit
- [ ] Performance benchmarking
- [ ] Architecture review

### Emergency Procedures

#### Service Degradation
1. Check health endpoints: `/health`, `/health/ready`
2. Review recent deployments and rollback if needed
3. Scale up replicas temporarily: `kubectl scale deployment api-gateway --replicas=10`
4. Check external dependencies (database, Redis, etc.)

#### Complete Service Outage
1. Check cluster health: `kubectl get nodes`
2. Verify ingress controller: `kubectl get pods -n ingress-nginx`
3. Review load balancer configuration
4. Execute disaster recovery plan

### Contact Information

- **On-call Engineering**: +1-XXX-XXX-XXXX
- **DevOps Team**: devops@neo-service.io
- **Security Team**: security@neo-service.io
- **Documentation**: https://docs.neo-service.io

---

## 🎉 Deployment Complete!

Your Neo Service Layer is now running in production with:

✅ **High Availability**: Multi-replica deployments with auto-scaling  
✅ **Security**: Network policies, secrets management, and TLS encryption  
✅ **Monitoring**: Comprehensive metrics, logging, and alerting  
✅ **Performance**: Optimized configurations and caching  
✅ **Resilience**: Circuit breakers, retries, and health checks  

**Next Steps:**
1. Configure monitoring dashboards
2. Set up alerting notifications
3. Perform load testing
4. Document operational procedures
5. Train the operations team

For support and updates, visit our [documentation site](https://docs.neo-service.io) or contact the team.