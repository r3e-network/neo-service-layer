# Neo Service Layer - Operational Runbook

## Table of Contents
- [Service Overview](#service-overview)
- [Critical Services](#critical-services)
- [Deployment Procedures](#deployment-procedures)
- [Monitoring & Alerts](#monitoring--alerts)
- [Incident Response](#incident-response)
- [Disaster Recovery](#disaster-recovery)
- [Common Issues & Solutions](#common-issues--solutions)

## Service Overview

The Neo Service Layer is a microservices-based platform providing blockchain services for Neo N3 and Neo X networks.

### Architecture Components
- **API Gateway**: Entry point for all client requests
- **Core Services**: Storage, KeyManagement, Configuration, Notification, CrossChain, Oracle
- **Infrastructure**: PostgreSQL, Redis, RabbitMQ, Consul
- **Monitoring**: Prometheus, Grafana, ELK Stack

## Critical Services

### Priority 1 (Must be operational)
- **API Gateway** - All client traffic entry point
- **KeyManagement Service** - Cryptographic operations
- **PostgreSQL Database** - Primary data store
- **Redis Cache** - Session and cache storage

### Priority 2 (Should be operational)
- **Storage Service** - Data storage operations
- **Configuration Service** - Dynamic configuration
- **Consul** - Service discovery
- **RabbitMQ** - Message queuing

### Priority 3 (Can tolerate downtime)
- **Notification Service** - Email/SMS notifications
- **CrossChain Service** - Cross-chain operations
- **Oracle Service** - External data feeds
- **Monitoring Stack** - Metrics and logging

## Deployment Procedures

### Rolling Deployment
```bash
# 1. Update Docker images
docker pull neo-service-layer/[service]:latest

# 2. Deploy to staging
kubectl apply -f k8s/services/[service].yaml -n neo-staging

# 3. Verify staging
./scripts/health-check.sh staging

# 4. Deploy to production (canary)
kubectl set image deployment/[service] [service]=neo-service-layer/[service]:latest \
  -n neo-production --record

# 5. Monitor metrics
kubectl rollout status deployment/[service] -n neo-production

# 6. Complete rollout or rollback
kubectl rollout undo deployment/[service] -n neo-production # if issues
```

### Emergency Rollback
```bash
# Immediate rollback to previous version
kubectl rollout undo deployment/[service] -n neo-production

# Rollback to specific revision
kubectl rollout undo deployment/[service] --to-revision=2 -n neo-production

# Check rollout history
kubectl rollout history deployment/[service] -n neo-production
```

## Monitoring & Alerts

### Key Metrics to Monitor

| Metric | Warning Threshold | Critical Threshold | Action |
|--------|------------------|-------------------|---------|
| API Response Time | > 500ms | > 1000ms | Scale horizontally |
| Error Rate | > 1% | > 5% | Check logs, possible rollback |
| CPU Usage | > 70% | > 90% | Scale up pods |
| Memory Usage | > 80% | > 95% | Scale up pods |
| Database Connections | > 80% | > 95% | Increase connection pool |
| Queue Depth | > 1000 | > 5000 | Scale consumers |

### Health Check Endpoints
- **Global**: `GET /health`
- **Service-specific**: `GET /api/v1/[service]/health`
- **Database**: `GET /health/db`
- **Redis**: `GET /health/cache`

### Alert Response

#### High Error Rate Alert
1. Check Grafana dashboard for error patterns
2. Review logs: `kubectl logs -n neo-production deployment/[service] --tail=100`
3. Check recent deployments: `kubectl rollout history deployment/[service]`
4. If related to recent deployment, rollback
5. Scale up if load-related

#### Database Connection Pool Exhausted
1. Check active connections: `SELECT count(*) FROM pg_stat_activity;`
2. Kill idle connections: `SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle' AND state_change < NOW() - INTERVAL '10 minutes';`
3. Increase connection pool in configuration
4. Restart affected services

## Incident Response

### Severity Levels
- **P1**: Complete service outage, data loss risk
- **P2**: Major feature unavailable, significant degradation
- **P3**: Minor feature unavailable, workaround available
- **P4**: Cosmetic issues, no functional impact

### Response Procedures

#### P1 Incident Response
1. **Immediate Actions** (< 5 minutes)
   - Create incident channel in Slack
   - Page on-call engineer
   - Start incident timeline documentation

2. **Diagnosis** (< 15 minutes)
   - Check monitoring dashboards
   - Review recent changes
   - Identify affected services

3. **Mitigation** (< 30 minutes)
   - Apply immediate fix or rollback
   - Communicate status to stakeholders
   - Monitor recovery

4. **Resolution**
   - Verify all services operational
   - Document root cause
   - Schedule post-mortem

### Common Issues & Solutions

#### Issue: Service won't start
```bash
# Check logs
kubectl logs -n neo-production deployment/[service] --tail=100

# Check configuration
kubectl describe configmap [service]-config -n neo-production

# Check secrets
kubectl get secrets -n neo-production

# Restart service
kubectl rollout restart deployment/[service] -n neo-production
```

#### Issue: Database connection errors
```bash
# Check database status
kubectl exec -it postgres-0 -n neo-production -- psql -U neouser -c "SELECT 1;"

# Check connection string
kubectl get secret db-connection -n neo-production -o yaml

# Restart connection pool
kubectl delete pod -l app=[service] -n neo-production
```

#### Issue: High memory usage
```bash
# Check memory usage
kubectl top pods -n neo-production

# Get heap dump (Java services)
kubectl exec [pod-name] -n neo-production -- jmap -dump:format=b,file=/tmp/heap.dump 1

# Increase memory limits
kubectl set resources deployment/[service] -n neo-production \
  --limits=memory=2Gi --requests=memory=1Gi
```

## Disaster Recovery

### Backup Procedures
- **Database**: Daily automated backups, 30-day retention
- **Configuration**: Git repository, versioned
- **Secrets**: Encrypted backup in secure storage

### Recovery Time Objectives
- **RTO (Recovery Time Objective)**: 4 hours
- **RPO (Recovery Point Objective)**: 1 hour

### Disaster Recovery Steps
1. **Assess damage scope**
2. **Activate DR environment**
3. **Restore database from backup**
4. **Redeploy services**
5. **Verify data integrity**
6. **Switch DNS to DR site**
7. **Monitor and validate**

### Data Recovery
```bash
# Restore PostgreSQL backup
pg_restore -h postgres.dr.example.com -U neouser -d neo_service_layer backup.dump

# Restore Redis snapshot
redis-cli --rdb /backup/redis.rdb

# Verify data integrity
./scripts/data-validation.sh
```

## Maintenance Procedures

### Scheduled Maintenance
1. **Notification**: 72 hours advance notice
2. **Timing**: During low-traffic window (2-4 AM UTC)
3. **Duration**: Maximum 2-hour window
4. **Rollback plan**: Always prepared

### Database Maintenance
```sql
-- Vacuum and analyze
VACUUM ANALYZE;

-- Reindex
REINDEX DATABASE neo_service_layer;

-- Update statistics
ANALYZE;
```

### Certificate Renewal
```bash
# Check certificate expiry
openssl x509 -in /etc/ssl/certs/neo-service.crt -noout -dates

# Renew certificate
certbot renew --webroot -w /var/www/certbot

# Reload services
kubectl rollout restart deployment -n neo-production
```

## Contact Information

### Escalation Path
1. **On-Call Engineer**: PagerDuty rotation
2. **Team Lead**: [Contact via Slack]
3. **Platform Team**: [platform-team@example.com]
4. **Management**: [For P1 incidents only]

### External Dependencies
- **Neo RPC Nodes**: [status.neo.org]
- **Cloud Provider**: [AWS/Azure/GCP support]
- **CDN**: [Cloudflare support]

## Appendix

### Useful Commands
```bash
# Get all pods status
kubectl get pods -n neo-production

# Tail logs from all pods of a service
kubectl logs -n neo-production -l app=[service] --tail=50 -f

# Port forward for debugging
kubectl port-forward -n neo-production deployment/[service] 8080:8080

# Execute command in pod
kubectl exec -it [pod-name] -n neo-production -- /bin/bash

# Get service endpoints
kubectl get endpoints -n neo-production

# Check resource usage
kubectl top nodes
kubectl top pods -n neo-production
```

### Log Queries
```
# Kibana queries
service:"storage-service" AND level:"ERROR"
response_time:>1000 AND status_code:5*
kubernetes.namespace:"neo-production" AND message:"timeout"
```

### Database Queries
```sql
-- Active connections
SELECT pid, usename, application_name, client_addr, state 
FROM pg_stat_activity;

-- Slow queries
SELECT query, calls, mean_exec_time 
FROM pg_stat_statements 
ORDER BY mean_exec_time DESC 
LIMIT 10;

-- Table sizes
SELECT schemaname, tablename, 
       pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables 
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

---
**Last Updated**: 2024-08-09
**Version**: 1.0.0
**Maintained By**: Platform Team