# Backup and Disaster Recovery Plan

This document outlines the comprehensive backup and disaster recovery strategy for the Neo Service Layer platform.

## Overview

The Neo Service Layer implements a multi-layered backup and disaster recovery solution designed to ensure business continuity with the following objectives:

- **Recovery Time Objective (RTO)**: 4 hours maximum
- **Recovery Point Objective (RPO)**: 24 hours maximum (1 hour for critical data)
- **Availability Target**: 99.9% uptime
- **Data Retention**: 30 days (databases), 90 days (disaster recovery)

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Primary Site  │    │   Backup Store  │    │    DR Site      │
│                 │    │                 │    │                 │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │ PostgreSQL  ├─┼────┤►│ S3 Backups  │ │    │ │ PostgreSQL  │ │
│ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │ MongoDB     ├─┼────┤►│ Velero      │◄┼────┤►│ MongoDB     │ │
│ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │ Redis       ├─┼────┤►│ Cross-Region│ │    │ │ Redis       │ │
│ └─────────────┘ │    │ │ Replication │ │    │ └─────────────┘ │
│ ┌─────────────┐ │    │ └─────────────┘ │    │ ┌─────────────┐ │
│ │ Kubernetes  │ │    │                 │    │ │ Kubernetes  │ │
│ └─────────────┘ │    │                 │    │ └─────────────┘ │
└─────────────────┘    └─────────────────┘    └─────────────────┘
     us-east-1              us-east-1              us-west-2
```

## Backup Strategy

### 1. Database Backups

#### PostgreSQL (Primary Database)
- **Schedule**: Daily at 2:00 AM UTC
- **Method**: `pg_dump` with custom format + SQL dump
- **Compression**: gzip level 9
- **Storage**: AWS S3 with server-side encryption
- **Retention**: 30 days
- **Validation**: Weekly restore tests

```bash
# Manual backup command
kubectl create job postgres-backup-manual --from=cronjob/postgres-backup -n neo-service-layer
```

#### MongoDB (Document Store)
- **Schedule**: Daily at 2:30 AM UTC
- **Method**: `mongodump` with gzip compression
- **Storage**: AWS S3 with versioning
- **Retention**: 30 days
- **Validation**: Automated restore verification

#### Redis (Cache)
- **Schedule**: Every 6 hours
- **Method**: RDB snapshots via `BGSAVE`
- **Storage**: AWS S3
- **Retention**: 7 days (acceptable for cache)
- **Note**: Data loss acceptable due to cache nature

### 2. Configuration Backups

#### Kubernetes Resources
- **Schedule**: Daily at 3:00 AM UTC
- **Components**: ConfigMaps, Secrets, Deployments, Services, Ingress
- **Method**: `kubectl` export to YAML
- **Storage**: AWS S3 with encryption
- **Retention**: 30 days

#### Application Configuration
- **Files**: appsettings.json, environment variables
- **Storage**: Version controlled in Git + S3 backup
- **Encryption**: Sensitive data encrypted at rest

### 3. Infrastructure Backups

#### Velero (Kubernetes Backup)
- **Schedule**: Daily at 4:00 AM UTC (full cluster)
- **Components**: All namespaces, PVs, cluster resources
- **Storage**: AWS S3 with cross-region replication
- **Retention**: 90 days for disaster recovery
- **Features**: Application-consistent snapshots

## Monitoring and Alerting

### Backup Metrics
- **Success Rate**: Track backup job completion
- **Backup Size**: Monitor for anomalies
- **Backup Age**: Alert if backups are stale
- **Storage Usage**: Monitor S3 bucket growth

### Prometheus Metrics
```
# Backup success indicator
backup_success{type="postgres"} 1

# Backup age in hours
backup_age_hours{type="postgres"} 12.5

# Total backup size in bytes
backup_total_size_bytes{type="postgres"} 1073741824
```

### Grafana Dashboard
- Backup success rates over time
- Backup size trends
- Alert status and history
- Recovery time estimates

### Alerts
- **Critical**: Backup failed or missing (>25 hours)
- **Warning**: Backup size anomaly detected
- **Info**: Backup job completed successfully

## Disaster Recovery Procedures

### DR Site Setup
- **Location**: AWS us-west-2 (different region)
- **Infrastructure**: Kubernetes cluster with identical configuration
- **Network**: VPC peering for secure communication
- **Storage**: EBS snapshots and S3 cross-region replication

### Failover Process

#### Automated Failover (RTO: 2 hours)
1. Health monitoring detects primary site failure
2. DNS automatically switches to DR site
3. Applications start in DR environment
4. Data restored from latest backups

#### Manual Failover (RTO: 4 hours)
1. Assess disaster impact using DR script
2. Execute failover procedure
3. Restore data from backups
4. Update DNS records
5. Verify service functionality

### Failover Execution
```bash
# Use the DR management script
./scripts/disaster-recovery/dr-plan.sh

# Options:
# 1. Assess Disaster Impact
# 2. Initiate Failover to DR Site  
# 3. Perform Failback to Primary Site
# 4. Run DR Drill (Non-disruptive)
# 5. Generate DR Report
```

### Failback Process
1. Verify primary site is operational
2. Sync data from DR to primary
3. Switch traffic back to primary
4. Scale down DR environment

## Backup Validation

### Automated Validation
- **Schedule**: Weekly
- **Process**: Restore to test environment
- **Validation**: Data integrity checks
- **Reporting**: Success/failure notifications

### Manual Validation
```bash
# Run comprehensive backup validation
./scripts/backup-validation.sh

# Validates:
# - PostgreSQL backup integrity
# - MongoDB backup completeness
# - Configuration backup contents
# - Backup job status
```

### Validation Report
```
Backup Validation Report
========================
Date: 2025-01-20 10:30:00
Validation Namespace: backup-validation

Summary:
- PostgreSQL Backup: PASS
- MongoDB Backup: PASS
- Redis Backup: PASS
- Configuration Backup: PASS
- Backup Jobs: PASS
```

## Security

### Encryption
- **At Rest**: All backups encrypted with AES-256
- **In Transit**: TLS 1.3 for data transfer
- **Keys**: AWS KMS managed encryption keys

### Access Control
- **IAM Roles**: Least privilege access to backup resources
- **Kubernetes RBAC**: Service accounts with minimal permissions
- **Audit Logging**: All backup operations logged

### Data Classification
- **Highly Sensitive**: User data, financial records (full encryption)
- **Sensitive**: Application logs, metrics (encrypted storage)
- **Public**: Configuration templates, documentation (standard storage)

## Compliance and Governance

### Retention Policies
- **Operational Backups**: 30 days
- **Compliance Backups**: 7 years (where required)
- **DR Backups**: 90 days
- **Test Backups**: 7 days

### Audit Requirements
- Monthly backup inventory reports
- Quarterly DR drill documentation
- Annual disaster recovery plan review
- Compliance certification updates

## Operational Procedures

### Daily Operations
1. **06:00 UTC**: Review overnight backup job status
2. **06:30 UTC**: Check backup monitoring dashboard
3. **07:00 UTC**: Validate backup metrics and alerts

### Weekly Operations
1. **Monday**: Run backup validation suite
2. **Wednesday**: Review backup storage usage
3. **Friday**: Update DR documentation if needed

### Monthly Operations
1. **First Monday**: Conduct DR drill exercise
2. **Mid-month**: Review and update backup retention
3. **End of month**: Generate compliance reports

### Quarterly Operations
1. **Disaster Recovery Testing**: Full failover test
2. **Backup Strategy Review**: Update RTO/RPO targets
3. **Security Assessment**: Review encryption and access
4. **Documentation Update**: Refresh all DR procedures

## Performance Optimization

### Backup Performance
- **Parallel Processing**: Multiple backup jobs run concurrently
- **Incremental Backups**: Planned for large datasets
- **Compression**: Balance between speed and space
- **Network Optimization**: Dedicated backup network paths

### Storage Optimization
- **Lifecycle Policies**: Automatic transition to cheaper storage
- **Deduplication**: Reduce storage requirements
- **Cross-Region Replication**: Asynchronous for performance
- **Cleanup Automation**: Remove expired backups automatically

## Cost Management

### Storage Costs
- **S3 Standard**: Hot backups (0-30 days)
- **S3 IA**: Warm backups (30-90 days)  
- **S3 Glacier**: Cold backups (90+ days)
- **Cross-Region**: DR backups in different region

### Optimization Strategies
- **Compression**: Reduce backup sizes by 70-80%
- **Retention Tuning**: Balance compliance with cost
- **Storage Tiering**: Automatic lifecycle transitions
- **Resource Scheduling**: Off-peak backup windows

## Testing and Validation

### DR Drill Types

#### Level 1: Backup Validation (Weekly)
- Restore backups to test environment
- Validate data integrity
- Automated testing

#### Level 2: Partial Failover (Monthly)
- Failover single service to DR
- Test application functionality
- Measure recovery times

#### Level 3: Full DR Test (Quarterly)
- Complete site failover
- End-to-end testing
- Stakeholder involvement

#### Level 4: Disaster Simulation (Annually)
- Simulate real disaster scenarios
- Test communication procedures
- Update disaster response plans

### Success Criteria
- **RTO Achievement**: Recovery within 4 hours
- **RPO Achievement**: Data loss <24 hours
- **Functionality**: All critical services operational
- **Performance**: >80% of normal performance in DR
- **Data Integrity**: 100% data validation success

## Troubleshooting

### Common Issues

#### Backup Job Failures
```bash
# Check job logs
kubectl logs -n neo-service-layer -l job-name=postgres-backup

# Check CronJob status
kubectl describe cronjob postgres-backup -n neo-service-layer

# Manual job execution
kubectl create job postgres-backup-debug --from=cronjob/postgres-backup -n neo-service-layer
```

#### Storage Issues
```bash
# Check S3 bucket permissions
aws s3api get-bucket-policy --bucket neo-service-layer-backups

# Verify encryption settings
aws s3api get-bucket-encryption --bucket neo-service-layer-backups

# Check cross-region replication
aws s3api get-bucket-replication --bucket neo-service-layer-backups
```

#### Network Connectivity
```bash
# Test S3 connectivity from pods
kubectl run test-pod --image=amazon/aws-cli -it --rm -- s3 ls neo-service-layer-backups

# Check DNS resolution
kubectl run test-dns --image=busybox -it --rm -- nslookup s3.amazonaws.com
```

### Recovery Scenarios

#### Scenario 1: Single Service Failure
- Impact: One service unavailable
- RTO: 30 minutes
- Procedure: Restart service, check recent backups

#### Scenario 2: Database Corruption
- Impact: Data loss risk
- RTO: 2 hours
- Procedure: Stop writes, restore from backup, validate

#### Scenario 3: Complete Site Failure
- Impact: Full service outage
- RTO: 4 hours
- Procedure: Execute full DR failover

#### Scenario 4: Data Center Disaster
- Impact: Infrastructure destroyed
- RTO: 6 hours
- Procedure: Rebuild in DR region, restore all services

## Contact Information

### Escalation Matrix
- **L1 Support**: On-call engineer (response: 15 min)
- **L2 Support**: Database specialist (response: 30 min)
- **L3 Support**: System architect (response: 1 hour)
- **Executive**: CTO notification (for major incidents)

### Communication Channels
- **Slack**: #neo-incidents
- **Email**: sre@neoservicelayer.io
- **Phone**: Emergency hotline (+1-xxx-xxx-xxxx)
- **Status Page**: status.neoservicelayer.io

## References

- [AWS RDS Backup Best Practices](https://docs.aws.amazon.com/RDS/latest/UserGuide/CHAP_CommonTasks.BackupRestore.html)
- [Kubernetes Backup with Velero](https://velero.io/docs/)
- [MongoDB Backup Strategies](https://docs.mongodb.com/manual/core/backups/)
- [PostgreSQL Backup Documentation](https://www.postgresql.org/docs/current/backup.html)