# Neo Service Layer - Pre-Deployment Checklist

## Infrastructure Requirements

### Kubernetes Cluster
- [ ] Kubernetes 1.28+ cluster with at least 3 nodes
- [ ] Minimum 16 CPU cores and 64GB RAM total capacity
- [ ] StorageClass configured (preferably SSD-backed)
- [ ] Ingress controller installed (NGINX or similar)
- [ ] Cert-manager for SSL/TLS certificates

### Network Requirements
- [ ] DNS resolution configured for:
  - `api.neo-service-layer.com`
  - `auth.neo-service-layer.com` 
  - `oracle.neo-service-layer.com`
  - `monitoring.neo-service-layer.com`
  - `tracing.neo-service-layer.com`
- [ ] External load balancer or ingress configured
- [ ] Firewall rules allow traffic on ports 80, 443, and health check ports

### Database Prerequisites
- [ ] PostgreSQL 15+ accessible from cluster
- [ ] Database user with CREATE DATABASE privileges
- [ ] Connection pooling configured (recommended: PgBouncer)
- [ ] Backup strategy in place

### Optional Components
- [ ] Istio service mesh installed (recommended)
- [ ] ArgoCD for GitOps (recommended)
- [ ] External secret management (HashiCorp Vault, AWS Secrets Manager)

## Security Configuration

### Secrets Management
- [ ] Create Kubernetes secrets for:
  ```bash
  # Database passwords
  kubectl create secret generic neo-db-secret \
    --from-literal=password="your-secure-db-password" \
    -n neo-databases
  
  # JWT signing keys
  kubectl create secret generic neo-jwt-secret \
    --from-literal=key="your-jwt-signing-key" \
    -n neo-services
  
  # Container registry credentials
  kubectl create secret docker-registry ghcr-secret \
    --docker-server=ghcr.io \
    --docker-username="your-username" \
    --docker-password="your-token" \
    -n neo-services
  ```

### Network Security
- [ ] Network policies configured for namespace isolation
- [ ] Pod Security Standards enforced
- [ ] Service mesh mTLS enabled (if using Istio)

### RBAC Configuration
- [ ] Service accounts created with minimal privileges
- [ ] ClusterRoles and RoleBindings configured
- [ ] External authentication configured (OIDC recommended)

## Environment Variables

Create `.env` file with required configuration:

```bash
# Database Configuration
DB_HOST=your-postgres-host
DB_PORT=5432
DB_USER=neo_admin
DB_PASSWORD=your-secure-password
DB_NAME=neo_service_layer

# Redis Configuration  
REDIS_HOST=neo-redis
REDIS_PORT=6379
REDIS_PASSWORD=your-redis-password

# JWT Configuration
JWT_SECRET_KEY=your-jwt-secret-key-minimum-256-bits
JWT_ISSUER=neo-auth-service
JWT_AUDIENCE=neo-service-layer

# Monitoring Configuration
PROMETHEUS_ENDPOINT=http://neo-prometheus:9090
GRAFANA_ADMIN_PASSWORD=your-grafana-password
JAEGER_ENDPOINT=http://jaeger-collector:14268/api/traces

# External Services
NOTIFICATION_WEBHOOK_URL=your-slack-webhook-url
BACKUP_STORAGE_ENDPOINT=your-s3-endpoint
```

## Pre-Flight Checks

Run these commands to verify readiness:

```bash
# Check cluster connectivity
kubectl cluster-info

# Verify node resources
kubectl describe nodes | grep -E "(Name:|CPU:|Memory:)"

# Check storage classes
kubectl get storageclass

# Verify DNS resolution (from within cluster)
kubectl run dns-test --image=busybox --rm -it --restart=Never -- nslookup kubernetes.default

# Test database connectivity
kubectl run db-test --image=postgres:15 --rm -it --restart=Never -- \
  psql -h $DB_HOST -U $DB_USER -d postgres -c "SELECT version();"
```

## Migration Execution Modes

### Full Migration (Recommended)
```bash
export MIGRATION_MODE=full
./scripts/migration/00-migration-orchestrator.sh
```

### Phased Approach
```bash
# Phase 1: Database migration only
export MIGRATION_MODE=database-only
./scripts/migration/00-migration-orchestrator.sh

# Phase 2: Service extraction only  
export MIGRATION_MODE=services-only
./scripts/migration/00-migration-orchestrator.sh

# Phase 3: Deployment only
export MIGRATION_MODE=deploy-only
./scripts/migration/00-migration-orchestrator.sh
```

### Dry Run Testing
```bash
export DRY_RUN=true
./scripts/migration/00-migration-orchestrator.sh
```

## Success Criteria

After migration, verify these metrics:

### Health Checks
- [ ] All pods in `Running` state
- [ ] Health endpoints responding (200 OK):
  - `http://neo-auth-service/health`
  - `http://neo-oracle-service/health`
  - `http://neo-compute-service/health`

### Connectivity
- [ ] Service-to-service communication working
- [ ] Database connections established
- [ ] External API endpoints accessible

### Monitoring
- [ ] Prometheus collecting metrics from all services
- [ ] Grafana dashboards displaying data
- [ ] Jaeger receiving traces from services

### Security
- [ ] mTLS communication between services (if Istio enabled)
- [ ] JWT authentication working
- [ ] Network policies blocking unauthorized traffic

## Rollback Plan

If deployment fails, execute rollback:

```bash
# Automated rollback
export ROLLBACK_ON_FAILURE=true
./scripts/migration/00-migration-orchestrator.sh

# Manual rollback steps
kubectl delete namespace neo-services --grace-period=30
kubectl delete namespace neo-monitoring --grace-period=30
# Restore database from backup if needed
```

## Support Contacts

- **Architecture Questions**: Review `/docs/architecture/`
- **Migration Issues**: Check migration logs in `/logs/`
- **Kubernetes Issues**: Verify cluster configuration
- **Database Issues**: Check database connectivity and permissions

---

**Pre-deployment checklist complete!** ✅

Once all items are verified, proceed with migration execution.