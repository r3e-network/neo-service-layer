# Neo Service Layer - Operational Excellence Guide

## Overview

This guide provides comprehensive operational procedures for managing the Neo Service Layer microservices architecture in production. It covers monitoring, troubleshooting, scaling, security, and disaster recovery.

## Table of Contents

1. [Monitoring & Observability](#monitoring--observability)
2. [Troubleshooting Guide](#troubleshooting-guide)
3. [Scaling & Performance](#scaling--performance)
4. [Security Operations](#security-operations)
5. [Backup & Disaster Recovery](#backup--disaster-recovery)
6. [Incident Response](#incident-response)
7. [Maintenance Procedures](#maintenance-procedures)

## Monitoring & Observability

### Key Metrics Dashboard

#### Service Level Indicators (SLIs)
- **Availability**: Target 99.9% uptime
- **Latency**: P95 < 500ms, P99 < 1s
- **Error Rate**: < 0.1% for critical operations
- **Throughput**: Support 10K+ requests per minute

#### Critical Alerts
```yaml
# High Priority (Page On-Call)
- Service down for > 30 seconds
- Error rate > 1% for > 1 minute
- P95 latency > 2 seconds for > 2 minutes
- Database connections > 80%
- Pod crash loops

# Medium Priority (Slack Notification)
- Error rate > 0.5% for > 5 minutes
- P95 latency > 1 second for > 5 minutes
- CPU usage > 80% for > 10 minutes
- Memory usage > 85% for > 10 minutes
- Disk usage > 90%
```

### Grafana Dashboards

#### 1. Service Overview Dashboard
```bash
# Access URL
https://monitoring.neo-service-layer.com/grafana/d/neo-overview

# Key Panels
- Service uptime and availability
- Request rate per service
- Error rate trends
- Response time percentiles
- Active connections
```

#### 2. Infrastructure Dashboard
```bash
# Access URL  
https://monitoring.neo-service-layer.com/grafana/d/neo-infrastructure

# Key Panels
- Node resource utilization
- Pod resource consumption
- Network traffic patterns
- Storage utilization
- Kubernetes events
```

#### 3. Database Dashboard
```bash
# Access URL
https://monitoring.neo-service-layer.com/grafana/d/neo-database

# Key Panels
- Connection pool status
- Query performance metrics
- Database size and growth
- Backup status
- Replication lag
```

### Log Management

#### Centralized Logging
```bash
# View service logs
kubectl logs -f deployment/neo-auth-service -n neo-services

# Search logs across all services
kubectl logs -n neo-services -l app.kubernetes.io/name=neo-service --tail=100

# Filter by error level
kubectl logs -n neo-services -l app.kubernetes.io/name=neo-service | grep "ERROR\|FATAL"

# View logs from specific time range
kubectl logs --since=1h deployment/neo-auth-service -n neo-services
```

#### Log Analysis Queries
```bash
# Authentication failures
grep "Authentication failed" /var/log/neo-services/auth/*.log | tail -50

# Database connection issues
grep "database.*connection.*failed" /var/log/neo-services/*/*.log

# High latency requests
grep "duration.*[5-9][0-9][0-9][0-9]ms" /var/log/neo-services/*/*.log
```

### Distributed Tracing

#### Jaeger Analysis
```bash
# Access URL
https://tracing.neo-service-layer.com/jaeger

# Common Trace Queries
- Service: neo-auth-service, Operation: POST /api/auth/login
- Service: neo-oracle-service, Tags: error=true
- Service: neo-compute-service, Min Duration: 1s
```

## Troubleshooting Guide

### Common Issues and Solutions

#### 1. Service Not Responding
```bash
# Diagnosis Steps
kubectl get pods -n neo-services
kubectl describe pod <pod-name> -n neo-services
kubectl logs <pod-name> -n neo-services

# Common Causes
- Resource limits exceeded
- Database connection issues
- Configuration errors
- Image pull failures

# Resolution
kubectl rollout restart deployment/<service-name> -n neo-services
```

#### 2. Database Connection Issues
```bash
# Check database status
kubectl get pods -n neo-databases
kubectl exec -it <postgres-pod> -n neo-databases -- pg_isready

# Test connection from service
kubectl exec -it <service-pod> -n neo-services -- \
  psql -h neo-postgres -U neo_user -d neo_auth_db -c "SELECT 1"

# Check connection pool status
kubectl exec -it <service-pod> -n neo-services -- \
  curl localhost:8080/health/database
```

#### 3. High Memory Usage
```bash
# Check memory usage
kubectl top pods -n neo-services

# Analyze memory patterns
kubectl exec -it <pod-name> -n neo-services -- \
  cat /proc/meminfo

# Scale horizontally if needed
kubectl scale deployment <service-name> --replicas=5 -n neo-services
```

#### 4. Service Mesh Issues
```bash
# Check Istio configuration
istioctl analyze

# Verify service mesh connectivity
istioctl proxy-config endpoints <pod-name>

# Check mTLS status
istioctl authn tls-check <service-name>.neo-services.svc.cluster.local
```

### Performance Troubleshooting

#### Slow Response Times
```bash
# 1. Check resource utilization
kubectl top pods -n neo-services
kubectl describe hpa -n neo-services

# 2. Analyze database queries
kubectl exec -it <postgres-pod> -n neo-databases -- \
  psql -d neo_auth_db -c "SELECT * FROM pg_stat_activity WHERE state = 'active';"

# 3. Check cache hit rates
kubectl exec -it <redis-pod> -n neo-databases -- \
  redis-cli info stats | grep cache_hits

# 4. Review distributed traces
# Access Jaeger UI and analyze slow traces
```

#### High Error Rates
```bash
# 1. Identify error sources
kubectl logs -n neo-services -l app.kubernetes.io/name=neo-service --tail=1000 | grep ERROR

# 2. Check circuit breaker status
kubectl get destinationrule -n neo-services -o yaml

# 3. Verify external dependencies
kubectl exec -it <pod-name> -n neo-services -- \
  curl -I https://external-api.example.com/health

# 4. Check rate limiting
kubectl describe configmap rate-limit-config -n neo-infrastructure
```

## Scaling & Performance

### Horizontal Pod Autoscaling

#### Configure HPA
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: neo-auth-service-hpa
  namespace: neo-services
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: neo-auth-service
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
  behavior:
    scaleUp:
      stabilizationWindowSeconds: 60
      selectPolicy: Max
      policies:
      - type: Percent
        value: 100
        periodSeconds: 15
    scaleDown:
      stabilizationWindowSeconds: 300
      selectPolicy: Min
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
```

#### Manual Scaling
```bash
# Scale up during high load
kubectl scale deployment neo-auth-service --replicas=10 -n neo-services

# Scale down during low load
kubectl scale deployment neo-auth-service --replicas=3 -n neo-services

# Check scaling status
kubectl get hpa -n neo-services
```

### Performance Optimization

#### Database Optimization
```sql
-- Monitor slow queries
SELECT query, mean_time, calls 
FROM pg_stat_statements 
WHERE mean_time > 100 
ORDER BY mean_time DESC 
LIMIT 10;

-- Check index usage
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
WHERE idx_scan = 0;

-- Connection pool optimization
SHOW max_connections;
SHOW shared_buffers;
SHOW effective_cache_size;
```

#### Cache Optimization
```bash
# Redis performance tuning
kubectl exec -it <redis-pod> -n neo-databases -- \
  redis-cli config set maxmemory-policy allkeys-lru

# Check cache hit rates
kubectl exec -it <redis-pod> -n neo-databases -- \
  redis-cli info stats | grep keyspace_hits

# Monitor cache size
kubectl exec -it <redis-pod> -n neo-databases -- \
  redis-cli info memory | grep used_memory_human
```

## Security Operations

### Security Monitoring

#### Real-time Threats
```bash
# Check failed authentication attempts
kubectl logs -n neo-services -l app=neo-auth-service | \
  grep "Authentication failed" | tail -20

# Monitor unusual traffic patterns
kubectl logs -n istio-system -l app=istio-proxy | \
  grep "response_code.*[45][0-9][0-9]" | tail -50

# Check certificate expiration
kubectl get certificates -n neo-infrastructure
```

#### Security Scans
```bash
# Vulnerability scanning
kubectl exec -it security-scanner -- \
  trivy image ghcr.io/neo-service-layer/neo-auth-service:latest

# Network policy validation
kubectl get networkpolicies -n neo-services
kubectl describe networkpolicy neo-production-network-policy -n neo-services
```

### Access Control Management

#### RBAC Updates
```bash
# Check current permissions
kubectl auth can-i list pods --as=system:serviceaccount:neo-services:neo-auth-service

# Update service account permissions
kubectl patch serviceaccount neo-auth-service -n neo-services \
  -p '{"metadata":{"annotations":{"description":"Updated permissions for auth service"}}}'

# Audit RBAC policies
kubectl auth reconcile -f rbac-policies.yaml --dry-run
```

### Certificate Management

#### SSL/TLS Certificate Rotation
```bash
# Check certificate status
kubectl get certificates -n neo-infrastructure
kubectl describe certificate neo-service-layer-tls -n neo-infrastructure

# Force certificate renewal
kubectl delete certificate neo-service-layer-tls -n neo-infrastructure
kubectl apply -f k8s/infrastructure/certificates.yaml

# Verify certificate validity
kubectl get secret neo-service-layer-tls -n neo-infrastructure -o yaml | \
  grep tls.crt | base64 -d | openssl x509 -text -noout
```

## Backup & Disaster Recovery

### Backup Procedures

#### Database Backups
```bash
# Manual database backup
kubectl exec -it <postgres-pod> -n neo-databases -- \
  pg_dump -U neo_user neo_auth_db > auth_backup_$(date +%Y%m%d).sql

# Verify backup integrity
kubectl exec -it <postgres-pod> -n neo-databases -- \
  pg_dump -U neo_user neo_auth_db --schema-only | head -50

# List existing backups
kubectl exec -it backup-storage-pod -n neo-databases -- \
  ls -la /backup/ | grep neo-db-backup
```

#### Configuration Backups
```bash
# Backup Kubernetes configurations
kubectl get all --all-namespaces -o yaml > neo-k8s-backup-$(date +%Y%m%d).yaml

# Backup secrets (encrypted)
kubectl get secrets --all-namespaces -o yaml > neo-secrets-backup-$(date +%Y%m%d).yaml

# Backup persistent volume claims
kubectl get pvc --all-namespaces -o yaml > neo-pvc-backup-$(date +%Y%m%d).yaml
```

### Disaster Recovery

#### Complete System Recovery
```bash
# 1. Restore infrastructure
kubectl apply -f k8s/namespaces/
kubectl apply -f k8s/infrastructure/

# 2. Restore databases from backup
kubectl exec -it <postgres-pod> -n neo-databases -- \
  psql -U neo_user -d postgres -c "CREATE DATABASE neo_auth_db_restore;"
kubectl exec -i <postgres-pod> -n neo-databases -- \
  psql -U neo_user neo_auth_db_restore < auth_backup_20240127.sql

# 3. Restore services
kubectl apply -f k8s/services/

# 4. Verify system health
./scripts/deploy-production.sh --verify-health-only
```

#### Point-in-Time Recovery
```bash
# 1. Stop services to prevent data changes
kubectl scale deployment --replicas=0 -n neo-services --all

# 2. Restore database to specific point in time
kubectl exec -it <postgres-pod> -n neo-databases -- \
  pg_restore -U neo_user -d neo_auth_db --clean backup_file.sql

# 3. Restart services
kubectl scale deployment --replicas=3 -n neo-services --all

# 4. Validate data integrity
kubectl exec -it <service-pod> -n neo-services -- \
  curl localhost:8080/health/database
```

## Incident Response

### Incident Classification

#### Severity Levels
- **P0 (Critical)**: Complete service outage, data loss, security breach
- **P1 (High)**: Partial service outage, significant performance degradation
- **P2 (Medium)**: Minor service issues, non-critical feature unavailable
- **P3 (Low)**: Cosmetic issues, documentation problems

#### Response Times
- **P0**: 15 minutes (immediate response)
- **P1**: 1 hour (urgent response)
- **P2**: 4 hours (standard response)
- **P3**: 24 hours (next business day)

### Incident Response Playbooks

#### P0: Complete Service Outage
```bash
# 1. Immediate assessment
kubectl get pods --all-namespaces | grep -v Running
kubectl get nodes
kubectl top nodes

# 2. Check recent changes
kubectl rollout history deployment/neo-auth-service -n neo-services
kubectl get events --sort-by='.lastTimestamp' -n neo-services

# 3. Emergency rollback if needed
kubectl rollout undo deployment/neo-auth-service -n neo-services

# 4. Communicate status
# Post to status page and notify stakeholders

# 5. Detailed investigation
kubectl describe pod <failing-pod> -n neo-services
kubectl logs <failing-pod> -n neo-services --previous
```

#### P1: Performance Degradation
```bash
# 1. Identify bottlenecks
kubectl top pods -n neo-services
kubectl get hpa -n neo-services

# 2. Check database performance
kubectl exec -it <postgres-pod> -n neo-databases -- \
  psql -d neo_auth_db -c "SELECT * FROM pg_stat_activity WHERE wait_event IS NOT NULL;"

# 3. Scale resources if needed
kubectl patch hpa neo-auth-service-hpa -n neo-services \
  -p '{"spec":{"maxReplicas":20}}'

# 4. Monitor improvement
watch kubectl top pods -n neo-services
```

### Post-Incident Analysis

#### Incident Report Template
```markdown
# Incident Report: [Title]

**Date**: [Date]
**Duration**: [Start] - [End]
**Severity**: P[0-3]
**Impact**: [Description of user impact]

## Timeline
- [Time]: Issue detected
- [Time]: Response team notified
- [Time]: Initial investigation started
- [Time]: Root cause identified
- [Time]: Fix implemented
- [Time]: Service restored
- [Time]: Incident closed

## Root Cause
[Detailed explanation of what caused the incident]

## Resolution
[What was done to resolve the incident]

## Lessons Learned
[What we learned from this incident]

## Action Items
- [ ] [Action item 1] - Owner: [Name] - Due: [Date]
- [ ] [Action item 2] - Owner: [Name] - Due: [Date]

## Prevention
[Steps to prevent similar incidents in the future]
```

## Maintenance Procedures

### Planned Maintenance

#### Service Updates
```bash
# 1. Pre-maintenance checklist
kubectl get pods -n neo-services
kubectl get hpa -n neo-services
kubectl top nodes

# 2. Create maintenance window notification
# Post to status page and notify users

# 3. Perform rolling update
kubectl set image deployment/neo-auth-service \
  auth-service=ghcr.io/neo-service-layer/neo-auth-service:v2.1.0 \
  -n neo-services

# 4. Monitor deployment
kubectl rollout status deployment/neo-auth-service -n neo-services

# 5. Verify health after update
kubectl exec -it <pod-name> -n neo-services -- \
  curl localhost:8080/health

# 6. Close maintenance window
# Update status page and notify completion
```

#### Database Maintenance
```bash
# 1. Schedule maintenance window (low traffic period)

# 2. Create database backup
kubectl exec -it <postgres-pod> -n neo-databases -- \
  pg_dump -U neo_user neo_auth_db > pre_maintenance_backup.sql

# 3. Perform maintenance operations
kubectl exec -it <postgres-pod> -n neo-databases -- \
  psql -d neo_auth_db -c "VACUUM ANALYZE;"

# 4. Update database statistics
kubectl exec -it <postgres-pod> -n neo-databases -- \
  psql -d neo_auth_db -c "REINDEX DATABASE neo_auth_db;"

# 5. Verify database health
kubectl exec -it <postgres-pod> -n neo-databases -- \
  psql -d neo_auth_db -c "SELECT pg_database_size('neo_auth_db');"
```

### Capacity Planning

#### Resource Usage Analysis
```bash
# CPU usage trends (last 7 days)
kubectl exec -it prometheus -n neo-monitoring -- \
  promtool query instant 'avg_over_time(cpu_usage_percent[7d])'

# Memory usage trends
kubectl exec -it prometheus -n neo-monitoring -- \
  promtool query instant 'avg_over_time(memory_usage_percent[7d])'

# Network traffic patterns
kubectl exec -it prometheus -n neo-monitoring -- \
  promtool query instant 'rate(network_bytes_total[1h])'

# Storage growth rate
kubectl exec -it prometheus -n neo-monitoring -- \
  promtool query instant 'increase(disk_usage_bytes[7d])'
```

#### Scaling Recommendations
```bash
# Analyze HPA metrics
kubectl describe hpa -n neo-services

# Review resource requests vs limits
kubectl describe pods -n neo-services | grep -A 2 "Requests:"

# Check node capacity
kubectl describe nodes | grep -A 5 "Capacity:"

# Storage growth analysis
kubectl get pvc -n neo-databases -o custom-columns=NAME:.metadata.name,SIZE:.spec.resources.requests.storage,USED:.status.capacity.storage
```

---

## Emergency Contacts

### On-Call Escalation
1. **Primary On-Call**: [Phone] [Email]
2. **Secondary On-Call**: [Phone] [Email]  
3. **Engineering Manager**: [Phone] [Email]
4. **Infrastructure Team**: [Slack Channel]

### External Vendors
- **Cloud Provider**: [Support Link] [Case Portal]
- **Database Vendor**: [Support Phone] [Portal]
- **Monitoring Service**: [Status Page] [Support]

---

**Operational Excellence is a continuous journey. Regular reviews and improvements of these procedures ensure system reliability and performance.**