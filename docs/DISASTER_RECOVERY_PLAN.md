# Disaster Recovery Plan - Neo Service Layer

## Table of Contents
1. [Overview](#overview)
2. [Recovery Objectives](#recovery-objectives)
3. [Disaster Scenarios](#disaster-scenarios)
4. [Recovery Procedures](#recovery-procedures)
5. [Testing Schedule](#testing-schedule)
6. [Contact Information](#contact-information)

## Overview

This document outlines the disaster recovery (DR) procedures for the Neo Service Layer platform. It covers various failure scenarios and provides step-by-step recovery instructions.

### Key Components
- **API Gateway**: Entry point for all services
- **Microservices**: Core business logic
- **PostgreSQL Database**: Primary data store
- **Redis Cache**: Session and cache storage
- **Smart Contracts**: Blockchain components
- **Intel SGX Enclaves**: Secure computation

## Recovery Objectives

### Recovery Time Objective (RTO)
- **Critical Services**: 1 hour
- **Non-critical Services**: 4 hours
- **Full Platform**: 8 hours

### Recovery Point Objective (RPO)
- **Database**: 15 minutes
- **Redis Cache**: 1 hour (acceptable data loss)
- **Configuration**: 0 minutes (version controlled)
- **Smart Contracts**: 0 minutes (immutable on blockchain)

## Disaster Scenarios

### 1. Database Failure

#### Detection
- Health check alerts
- Connection pool exhaustion
- Application errors

#### Recovery Steps
```bash
# 1. Verify database status
docker exec neo-postgres pg_isready || echo "Database is down"

# 2. Attempt restart
docker restart neo-postgres

# 3. If restart fails, restore from backup
./scripts/restore-database.sh latest

# 4. Verify data integrity
docker exec neo-postgres psql -U $DB_USER -d $DB_NAME -c "SELECT COUNT(*) FROM information_schema.tables;"
```

### 2. Service Outage

#### Detection
- Prometheus alerts
- Health check failures
- User reports

#### Recovery Steps
```bash
# 1. Identify failed services
docker-compose -f docker-compose.production.yml ps

# 2. Check service logs
docker-compose -f docker-compose.production.yml logs --tail=100 <service_name>

# 3. Restart failed service
docker-compose -f docker-compose.production.yml restart <service_name>

# 4. If persistent failure, redeploy
docker-compose -f docker-compose.production.yml up -d --force-recreate <service_name>
```

### 3. Complete Infrastructure Failure

#### Detection
- Multiple service alerts
- Infrastructure monitoring alerts
- Complete platform unavailability

#### Recovery Steps
```bash
# 1. Provision new infrastructure
terraform apply -var-file=production.tfvars

# 2. Deploy platform
./scripts/quick-deploy.sh docker production

# 3. Restore data
./scripts/restore-all.sh latest

# 4. Verify services
./scripts/smoke-tests.sh https://api.neo-service-layer.com
```

### 4. Blockchain Node Failure

#### Detection
- Smart contract invocation failures
- Blockchain sync lag alerts
- RPC timeout errors

#### Recovery Steps
```bash
# 1. Switch to backup RPC endpoint
export NEO_N3_RPC_URL=https://mainnet2.neo.coz.io:443
export NEO_X_RPC_URL=https://backup.neox.org:443

# 2. Update service configuration
docker-compose -f docker-compose.production.yml up -d --force-recreate

# 3. Verify blockchain connectivity
curl -X POST $NEO_N3_RPC_URL \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"getblockcount","params":[],"id":1}'
```

### 5. Security Breach

#### Detection
- Suspicious activity alerts
- Unauthorized access attempts
- Data integrity violations

#### Immediate Actions
```bash
# 1. Isolate affected systems
./scripts/emergency-shutdown.sh

# 2. Rotate all credentials
./scripts/rotate-all-credentials.sh

# 3. Review audit logs
./scripts/security-audit.sh --last-24h

# 4. Deploy with new credentials
./scripts/secure-redeploy.sh
```

## Recovery Procedures

### Database Recovery

```bash
#!/bin/bash
# restore-database.sh

BACKUP_FILE=$1
if [ -z "$BACKUP_FILE" ]; then
    BACKUP_FILE=$(ls -t /var/backups/neo-service-layer/*-database.sql.gz | head -1)
fi

echo "Restoring database from: $BACKUP_FILE"

# Stop dependent services
docker-compose -f docker-compose.production.yml stop api-gateway

# Restore database
gunzip -c "$BACKUP_FILE" | docker exec -i neo-postgres psql -U $DB_USER -d $DB_NAME

# Restart services
docker-compose -f docker-compose.production.yml start api-gateway

# Verify
docker exec neo-postgres psql -U $DB_USER -d $DB_NAME -c "\dt"
```

### Full Platform Recovery

```bash
#!/bin/bash
# restore-all.sh

BACKUP_TIMESTAMP=$1
if [ -z "$BACKUP_TIMESTAMP" ]; then
    BACKUP_TIMESTAMP="latest"
fi

echo "Starting full platform recovery..."

# 1. Restore database
./scripts/restore-database.sh $BACKUP_TIMESTAMP

# 2. Restore Redis
./scripts/restore-redis.sh $BACKUP_TIMESTAMP

# 3. Restore configurations
./scripts/restore-configs.sh $BACKUP_TIMESTAMP

# 4. Redeploy services
docker-compose -f docker-compose.production.yml up -d

# 5. Wait for services
sleep 30

# 6. Run health checks
./scripts/health-check-all.sh

# 7. Run smoke tests
./scripts/smoke-tests.sh http://localhost
```

### Rollback Procedure

```bash
#!/bin/bash
# rollback.sh

PREVIOUS_VERSION=$1
if [ -z "$PREVIOUS_VERSION" ]; then
    PREVIOUS_VERSION=$(git tag --sort=-creatordate | head -2 | tail -1)
fi

echo "Rolling back to version: $PREVIOUS_VERSION"

# 1. Checkout previous version
git checkout $PREVIOUS_VERSION

# 2. Rebuild and deploy
docker-compose -f docker-compose.production.yml build
docker-compose -f docker-compose.production.yml up -d

# 3. Run migrations if needed
docker exec neo-api-gateway dotnet ef database migrate

# 4. Verify
./scripts/smoke-tests.sh http://localhost
```

## Testing Schedule

### Monthly Tests
- Database backup and restore
- Service failover
- Monitoring alert verification

### Quarterly Tests
- Full platform recovery
- Rollback procedures
- Security incident response

### Annual Tests
- Complete infrastructure rebuild
- Cross-region failover
- Extended downtime simulation

## Contact Information

### Primary Contacts

| Role | Name | Phone | Email |
|------|------|-------|-------|
| Incident Commander | John Doe | +1-555-0100 | john.doe@company.com |
| Platform Lead | Jane Smith | +1-555-0101 | jane.smith@company.com |
| Database Admin | Bob Johnson | +1-555-0102 | bob.johnson@company.com |
| Security Lead | Alice Brown | +1-555-0103 | alice.brown@company.com |

### Escalation Path

1. **Level 1**: On-call engineer (PagerDuty)
2. **Level 2**: Platform Lead
3. **Level 3**: Engineering Director
4. **Level 4**: CTO

### External Contacts

| Service | Contact | Phone | Email |
|---------|---------|-------|-------|
| AWS Support | Premium Support | +1-800-xxx-xxxx | support@aws.com |
| Azure Support | Premier Support | +1-800-xxx-xxxx | support@azure.com |
| Neo Foundation | Technical Support | - | support@neo.org |

## Recovery Validation

### Checklist
- [ ] All services are running
- [ ] Health checks passing
- [ ] Database connectivity verified
- [ ] Redis connectivity verified
- [ ] Blockchain RPC accessible
- [ ] Smart contracts responding
- [ ] Authentication working
- [ ] API endpoints tested
- [ ] Monitoring active
- [ ] Logs flowing
- [ ] Backups running
- [ ] SSL certificates valid

### Post-Recovery Actions
1. Document incident timeline
2. Update runbooks if needed
3. Conduct post-mortem
4. Implement preventive measures
5. Test fixes

## Appendix

### Useful Commands

```bash
# Check all service status
docker-compose -f docker-compose.production.yml ps

# View recent logs
docker-compose -f docker-compose.production.yml logs --tail=1000 --since=1h

# Database connection test
docker exec neo-postgres pg_isready

# Redis connection test
docker exec neo-redis redis-cli ping

# Blockchain connectivity test
curl -s $NEO_N3_RPC_URL | jq .

# Force recreate all services
docker-compose -f docker-compose.production.yml up -d --force-recreate

# Emergency shutdown
docker-compose -f docker-compose.production.yml down

# Backup now
./scripts/backup-automation.sh
```

### Recovery Scripts Location
- `/home/ubuntu/neo-service-layer/scripts/`
- Backup location: `/var/backups/neo-service-layer/`
- Cloud backups: S3 bucket `neo-service-layer-backups`

---

**Document Version**: 1.0.0  
**Last Updated**: $(date)  
**Next Review**: Quarterly