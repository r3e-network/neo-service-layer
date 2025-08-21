# PostgreSQL Migration Guide - Neo Service Layer

## Overview

This guide covers the complete migration of the Neo Service Layer from disparate persistence mechanisms to a unified PostgreSQL database backend. All services, including the SGX confidential storage service, now use the same PostgreSQL database for persistence.

## Architecture Changes

### Before Migration
- **API Service**: Entity Framework with SQLite/SQL Server
- **SGX Enclave Storage**: File-based persistence
- **Oracle Service**: In-memory storage
- **Authentication**: Token-based with no persistence
- **Key Management**: File-based key storage
- **Voting**: In-memory governance data

### After Migration
- **All Services**: Unified PostgreSQL database
- **Schema Separation**: Logical separation using PostgreSQL schemas
- **SGX Integration**: Confidential data stored in PostgreSQL with encryption
- **Shared Infrastructure**: Single database instance for all services

## Database Schema Design

### Schema Organization

```
neo_service_layer (database)
├── core (schema)           # Core system entities and configuration
├── auth (schema)           # Authentication and authorization
├── sgx (schema)            # SGX confidential storage and sealed data
├── keymanagement (schema)  # Cryptographic key storage and rotation
├── oracle (schema)         # Oracle data feeds and pricing
├── voting (schema)         # Governance and voting mechanisms
├── crosschain (schema)     # Cross-chain bridge operations
├── monitoring (schema)     # System monitoring and audit logs
└── eventsourcing (schema)  # Event sourcing and CQRS patterns
```

### Key Features

- **Logical Separation**: Each service domain isolated in its own schema
- **Shared Infrastructure**: Common database instance reduces operational overhead
- **SGX Integration**: Encrypted confidential data storage in PostgreSQL
- **Audit Trail**: Comprehensive audit logging across all schemas
- **Performance Optimization**: SSD-optimized configuration for confidential computing

## Quick Start Guide

### Option 1: Docker Compose (Recommended for Development)

```bash
# 1. Set environment variables
export POSTGRES_PASSWORD="your_secure_password"
export JWT_SECRET_KEY="your_jwt_secret_key"
export SGX_ENCRYPTION_KEY="your_sgx_encryption_key"

# 2. Start all services with PostgreSQL
docker-compose -f docker-compose.postgresql.yml up -d

# 3. Verify database initialization
docker-compose -f docker-compose.postgresql.yml logs neo-postgres

# 4. Check service health
curl http://localhost:8080/health
```

### Option 2: Kubernetes (Production)

```bash
# 1. Create namespace and apply configurations
kubectl apply -f k8s/postgresql-deployment.yaml

# 2. Wait for PostgreSQL to be ready
kubectl wait --for=condition=ready pod -l app=postgres -n neo-service-layer --timeout=300s

# 3. Deploy Neo services
kubectl apply -f k8s/neo-services-deployment.yaml

# 4. Check deployment status
kubectl get pods -n neo-service-layer
```

### Option 3: Manual PostgreSQL Setup

```bash
# 1. Install PostgreSQL 16
sudo apt update && sudo apt install postgresql-16 postgresql-contrib-16

# 2. Create database and user
sudo -u postgres createdb neo_service_layer
sudo -u postgres createuser neo_user
sudo -u postgres psql -c "ALTER USER neo_user WITH PASSWORD 'your_password';"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE neo_service_layer TO neo_user;"

# 3. Initialize database schema
psql -h localhost -U neo_user -d neo_service_layer -f docker/postgres/init.sql

# 4. Configure PostgreSQL
sudo cp docker/postgres/postgresql.conf /etc/postgresql/16/main/
sudo systemctl restart postgresql

# 5. Update connection strings in appsettings.json
# ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=neo_service_layer;Username=neo_user;Password=your_password;SSL Mode=Require"
```

## Configuration

### Connection String Format

```
Host=<hostname>;Port=5432;Database=neo_service_layer;Username=neo_user;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

### Environment Variables

| Variable | Description | Required | Default |
|----------|-------------|----------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Yes | - |
| `POSTGRES_PASSWORD` | Database user password | Yes | - |
| `JWT_SECRET_KEY` | JWT signing key | Yes | - |
| `SGX__Database__EnableEncryption` | Enable SGX data encryption | No | `true` |
| `SGX__Database__EncryptionKey` | SGX encryption key | Conditional | - |
| `SGX__EnableHardwareMode` | Enable SGX hardware mode | No | `false` |

### SGX Confidential Storage Configuration

```json
{
  "SGX": {
    "EnableHardwareMode": false,
    "Database": {
      "EnableEncryption": true,
      "EnableIntegrityChecking": true,
      "DataRetentionDays": 90,
      "EncryptionKey": "${SGX_ENCRYPTION_KEY}"
    }
  }
}
```

## Database Operations

### Backup and Restore

#### Create Backup

```bash
# Full database backup
pg_dump -h localhost -U neo_user -d neo_service_layer > neo_backup_$(date +%Y%m%d_%H%M%S).sql

# Schema-specific backup
pg_dump -h localhost -U neo_user -d neo_service_layer -n sgx > sgx_backup_$(date +%Y%m%d_%H%M%S).sql

# Automated backup (using Docker)
docker-compose -f docker-compose.postgresql.yml run --rm neo-db-backup
```

#### Restore from Backup

```bash
# Restore full database
psql -h localhost -U neo_user -d neo_service_layer < neo_backup_20241121_120000.sql

# Restore specific schema
psql -h localhost -U neo_user -d neo_service_layer < sgx_backup_20241121_120000.sql
```

### Database Maintenance

#### Cleanup Expired Data

```sql
-- Manual cleanup
SELECT cleanup_expired_data();

-- Check cleanup status
SELECT * FROM monitoring.audit_logs WHERE action LIKE '%CLEANUP%' ORDER BY timestamp DESC LIMIT 10;
```

#### Performance Monitoring

```sql
-- Check database performance
SELECT * FROM monitoring.database_performance;

-- Monitor index usage
SELECT * FROM monitoring.index_usage WHERE idx_scan = 0;

-- Table usage statistics
SELECT * FROM monitoring.table_usage ORDER BY n_live_tup DESC;
```

## Security Considerations

### Database Security

1. **SSL/TLS Encryption**: All connections use SSL with minimum TLS 1.2
2. **Password Authentication**: SCRAM-SHA-256 password encryption
3. **Role-Based Access**: Separate roles for different access levels
4. **Network Security**: Kubernetes NetworkPolicy restricts database access

### SGX Integration Security

1. **Data Encryption**: All SGX data encrypted before storage
2. **Integrity Checking**: Cryptographic integrity verification
3. **Access Control**: Schema-level access restrictions
4. **Audit Trail**: Complete audit log for all SGX operations

### Security Roles

```sql
-- Readonly access (excludes SGX and key management)
GRANT neo_readonly TO monitoring_user;

-- Audit access (monitoring and event sourcing only)
GRANT neo_auditor TO compliance_user;

-- Backup access (read-only access to all data)
GRANT neo_backup TO backup_service;
```

## Performance Tuning

### PostgreSQL Configuration Highlights

```ini
# Memory settings optimized for containers
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 16MB
maintenance_work_mem = 128MB

# SSD optimization
random_page_cost = 1.1
effective_io_concurrency = 200

# Confidential computing optimization
max_connections = 200
wal_buffers = 16MB
checkpoint_completion_target = 0.9
```

### Monitoring Queries

```sql
-- Check slow queries
SELECT query, calls, total_time, mean_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;

-- Monitor connection usage
SELECT 
    state,
    count(*) as connection_count,
    max(now() - state_change) as max_age
FROM pg_stat_activity
GROUP BY state;

-- Check database size
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE schemaname IN ('core', 'auth', 'sgx', 'oracle', 'voting')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

## Troubleshooting

### Common Issues

#### Connection Issues

```bash
# Check PostgreSQL service status
docker-compose -f docker-compose.postgresql.yml ps neo-postgres
kubectl get pods -l app=postgres -n neo-service-layer

# View PostgreSQL logs
docker-compose -f docker-compose.postgresql.yml logs neo-postgres
kubectl logs -l app=postgres -n neo-service-layer

# Test connection
psql -h localhost -U neo_user -d neo_service_layer -c "SELECT version();"
```

#### SGX Storage Issues

```bash
# Check SGX service logs
docker-compose -f docker-compose.postgresql.yml logs neo-enclave-storage

# Verify SGX database connectivity
curl http://localhost:8080/health/sgx

# Check SGX data in database
psql -h localhost -U neo_user -d neo_service_layer -c "SELECT COUNT(*) FROM sgx.sealed_data_items;"
```

#### Performance Issues

```sql
-- Check for blocking queries
SELECT 
    blocked_locks.pid AS blocked_pid,
    blocked_activity.usename AS blocked_user,
    blocking_locks.pid AS blocking_pid,
    blocking_activity.usename AS blocking_user,
    blocked_activity.query AS blocked_statement
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;

-- Check autovacuum status
SELECT schemaname, tablename, last_vacuum, last_autovacuum, last_analyze, last_autoanalyze
FROM pg_stat_user_tables
WHERE schemaname IN ('core', 'auth', 'sgx', 'oracle', 'voting')
ORDER BY last_autovacuum DESC NULLS LAST;
```

### Health Checks

#### Database Health

```bash
# Application health check
curl http://localhost:8080/health

# Database-specific health
curl http://localhost:8080/health/postgresql

# SGX storage health
curl http://localhost:8080/health/sgx
```

#### Manual Health Verification

```sql
-- Verify all schemas exist
SELECT schema_name FROM information_schema.schemata 
WHERE schema_name IN ('core', 'auth', 'sgx', 'keymanagement', 'oracle', 'voting', 'crosschain', 'monitoring', 'eventsourcing');

-- Check table counts
SELECT 
    schemaname,
    COUNT(*) as table_count
FROM pg_tables 
WHERE schemaname IN ('core', 'auth', 'sgx', 'keymanagement', 'oracle', 'voting', 'crosschain', 'monitoring', 'eventsourcing')
GROUP BY schemaname;

-- Verify extensions
SELECT extname FROM pg_extension WHERE extname IN ('uuid-ossp', 'pgcrypto', 'pg_stat_statements');
```

## Migration from Existing Systems

### Pre-Migration Checklist

- [ ] Backup existing data
- [ ] Test PostgreSQL configuration
- [ ] Verify SGX encryption keys
- [ ] Update connection strings
- [ ] Plan downtime window
- [ ] Prepare rollback strategy

### Migration Steps

1. **Export Existing Data**
   ```bash
   # Export SQLite data (if applicable)
   sqlite3 existing.db ".dump" > existing_data.sql
   
   # Export file-based SGX data
   tar -czf sgx_backup.tar.gz /path/to/sgx/data
   ```

2. **Deploy PostgreSQL Infrastructure**
   ```bash
   docker-compose -f docker-compose.postgresql.yml up -d neo-postgres
   ```

3. **Initialize Database Schema**
   ```bash
   psql -h localhost -U neo_user -d neo_service_layer -f docker/postgres/init.sql
   ```

4. **Migrate Data** (Service-specific scripts required)
   ```bash
   # Custom migration scripts would go here
   # Example: python migrate_sgx_data.py
   ```

5. **Deploy Updated Services**
   ```bash
   docker-compose -f docker-compose.postgresql.yml up -d
   ```

6. **Verify Migration**
   ```bash
   curl http://localhost:8080/health
   ```

## Production Deployment

### Kubernetes Production Configuration

```yaml
# Production-ready PostgreSQL configuration
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres-production
spec:
  replicas: 1  # Single instance for confidential computing
  serviceName: postgres
  template:
    spec:
      containers:
      - name: postgres
        image: postgres:16-alpine
        resources:
          requests:
            memory: "2Gi"
            cpu: "1000m"
          limits:
            memory: "4Gi"
            cpu: "2000m"
        env:
        - name: POSTGRES_DB
          value: "neo_service_layer"
        # Additional production configurations...
```

### Monitoring and Alerting

```yaml
# Prometheus alerts for PostgreSQL
groups:
- name: postgresql
  rules:
  - alert: PostgreSQLDown
    expr: pg_up == 0
    for: 0m
    labels:
      severity: critical
    annotations:
      summary: PostgreSQL instance is down
      
  - alert: PostgreSQLHighConnections
    expr: pg_stat_database_numbackends / pg_settings_max_connections > 0.8
    for: 2m
    labels:
      severity: warning
    annotations:
      summary: PostgreSQL connection usage is high
```

## Support and Maintenance

### Regular Maintenance Tasks

1. **Daily**: Monitor health checks and performance metrics
2. **Weekly**: Review slow query logs and optimize as needed
3. **Monthly**: Update statistics and run VACUUM ANALYZE
4. **Quarterly**: Review and rotate encryption keys
5. **Annually**: Plan for PostgreSQL version upgrades

### Getting Help

- **Logs**: Check container/pod logs for detailed error information
- **Health Checks**: Use built-in health endpoints for status verification
- **Documentation**: Refer to PostgreSQL and SGX-specific documentation
- **Community**: Neo Service Layer community support channels

---

**Migration Complete**: All services including SGX confidential storage now use the unified PostgreSQL database for persistence.
