# Neo Service Layer - Database Sharding Strategy

## Overview

This document outlines the database sharding strategy for the Neo Service Layer to address scalability concerns and eliminate the single PostgreSQL instance bottleneck identified in the performance analysis.

## Current State

- **Single PostgreSQL Instance**: All services currently share one database
- **Performance Bottleneck**: Database becomes a limiting factor at scale
- **Resource Contention**: Services compete for database resources
- **Backup Complexity**: Single large database difficult to backup/restore

## Proposed Sharding Strategy

### 1. Service-Based Sharding (Primary Approach)

Separate databases per service or service group based on data isolation requirements and performance characteristics.

```
┌─────────────────────────────────────────────────────────────┐
│                     Database Architecture                     │
├─────────────────────────────────────────────────────────────┤
│  Core Services DB Cluster                                    │
│  ├── neo_storage_db (Storage Service)                       │
│  ├── neo_keymanagement_db (Key Management)                  │
│  └── neo_security_db (Secrets, Auth)                        │
├─────────────────────────────────────────────────────────────┤
│  Blockchain Services DB Cluster                              │
│  ├── neo_oracle_db (Oracle Service)                         │
│  ├── neo_smartcontracts_db (Smart Contracts)                │
│  └── neo_crosschain_db (Cross-chain)                        │
├─────────────────────────────────────────────────────────────┤
│  Application Services DB Cluster                             │
│  ├── neo_notification_db (Notifications)                    │
│  ├── neo_automation_db (Automation)                         │
│  └── neo_voting_db (Voting)                                 │
├─────────────────────────────────────────────────────────────┤
│  Analytics DB Cluster                                        │
│  ├── neo_monitoring_db (Monitoring, Metrics)                │
│  ├── neo_analytics_db (AI Services)                         │
│  └── neo_reporting_db (Reports)                             │
└─────────────────────────────────────────────────────────────┘
```

### 2. Data Categories

#### Hot Data (High Transaction Rate)
- **Database**: PostgreSQL with read replicas
- **Services**: Storage, Oracle, Smart Contracts
- **Strategy**: Master-slave replication with read load balancing

#### Warm Data (Moderate Access)
- **Database**: PostgreSQL standard
- **Services**: Notification, Automation, Key Management
- **Strategy**: Single instance with regular backups

#### Cold Data (Archive/Analytics)
- **Database**: PostgreSQL with TimescaleDB extension
- **Services**: Monitoring, Analytics, Audit logs
- **Strategy**: Time-series optimization, data retention policies

### 3. Implementation Phases

#### Phase 1: Database Separation (Week 1-2)
```yaml
# Docker Compose configuration for separated databases
version: '3.8'
services:
  postgres-core:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: neo_core
      POSTGRES_USER: neo_core_user
      POSTGRES_PASSWORD: ${CORE_DB_PASSWORD}
    volumes:
      - core_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  postgres-blockchain:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: neo_blockchain
      POSTGRES_USER: neo_blockchain_user
      POSTGRES_PASSWORD: ${BLOCKCHAIN_DB_PASSWORD}
    volumes:
      - blockchain_data:/var/lib/postgresql/data
    ports:
      - "5433:5432"

  postgres-analytics:
    image: timescale/timescaledb:latest-pg16
    environment:
      POSTGRES_DB: neo_analytics
      POSTGRES_USER: neo_analytics_user
      POSTGRES_PASSWORD: ${ANALYTICS_DB_PASSWORD}
    volumes:
      - analytics_data:/var/lib/postgresql/data
    ports:
      - "5434:5432"
```

#### Phase 2: Read Replica Setup (Week 2-3)

##### PostgreSQL Streaming Replication
```sql
-- On Master
CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replica_password';

-- postgresql.conf
wal_level = replica
max_wal_senders = 3
wal_keep_segments = 64
synchronous_commit = on

-- pg_hba.conf
host replication replicator replica_ip/32 md5
```

##### Application Configuration
```csharp
// Connection string configuration with read replicas
public class DatabaseConfiguration
{
    public string WriteConnectionString { get; set; }
    public List<string> ReadConnectionStrings { get; set; }
}

// Usage in services
services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var httpContext = serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
    var isReadOperation = httpContext?.Request.Method == "GET";
    
    var config = serviceProvider.GetService<DatabaseConfiguration>();
    var connectionString = isReadOperation && config.ReadConnectionStrings.Any()
        ? config.ReadConnectionStrings[Random.Next(config.ReadConnectionStrings.Count)]
        : config.WriteConnectionString;
        
    options.UseNpgsql(connectionString);
});
```

#### Phase 3: Horizontal Sharding (Week 3-4)

For high-volume tables, implement horizontal sharding:

```sql
-- Shard by user_id (example)
CREATE TABLE transactions_shard_0 (CHECK (user_id % 4 = 0)) INHERITS (transactions);
CREATE TABLE transactions_shard_1 (CHECK (user_id % 4 = 1)) INHERITS (transactions);
CREATE TABLE transactions_shard_2 (CHECK (user_id % 4 = 2)) INHERITS (transactions);
CREATE TABLE transactions_shard_3 (CHECK (user_id % 4 = 3)) INHERITS (transactions);

-- Create routing function
CREATE OR REPLACE FUNCTION transactions_insert_trigger()
RETURNS TRIGGER AS $$
BEGIN
    IF (NEW.user_id % 4 = 0) THEN
        INSERT INTO transactions_shard_0 VALUES (NEW.*);
    ELSIF (NEW.user_id % 4 = 1) THEN
        INSERT INTO transactions_shard_1 VALUES (NEW.*);
    ELSIF (NEW.user_id % 4 = 2) THEN
        INSERT INTO transactions_shard_2 VALUES (NEW.*);
    ELSIF (NEW.user_id % 4 = 3) THEN
        INSERT INTO transactions_shard_3 VALUES (NEW.*);
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;
```

### 4. Connection Pooling Strategy

```yaml
# PgBouncer configuration for connection pooling
[databases]
neo_storage = host=postgres-core port=5432 dbname=neo_storage
neo_storage_ro = host=postgres-core-replica port=5432 dbname=neo_storage

[pgbouncer]
pool_mode = transaction
max_client_conn = 1000
default_pool_size = 25
min_pool_size = 10
reserve_pool_size = 5
reserve_pool_timeout = 3
server_lifetime = 3600
server_idle_timeout = 600
```

### 5. Service-Specific Configurations

#### Storage Service
```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "Sharding": {
      "Strategy": "RangeSharding",
      "ShardKey": "created_date",
      "Shards": [
        {
          "Name": "storage_2024",
          "Range": "2024-01-01 to 2024-12-31",
          "ConnectionString": "Host=postgres-storage-2024"
        },
        {
          "Name": "storage_2025",
          "Range": "2025-01-01 to 2025-12-31",
          "ConnectionString": "Host=postgres-storage-2025"
        }
      ]
    }
  }
}
```

#### Oracle Service
```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "ReadReplicas": [
      "Host=postgres-oracle-replica1",
      "Host=postgres-oracle-replica2"
    ],
    "CacheStrategy": "ReadThrough",
    "CacheTTL": 300
  }
}
```

### 6. Migration Strategy

#### Step 1: Schema Extraction
```bash
# Export schemas for each service
pg_dump -h localhost -U neo_user -d neo_service_layer \
  -n storage_schema -s > storage_schema.sql

pg_dump -h localhost -U neo_user -d neo_service_layer \
  -n oracle_schema -s > oracle_schema.sql
```

#### Step 2: Data Migration
```bash
# Migrate data with minimal downtime
pg_dump -h localhost -U neo_user -d neo_service_layer \
  -t storage_* --data-only | \
  psql -h postgres-core -U neo_core_user -d neo_storage
```

#### Step 3: Application Cutover
```csharp
// Feature flag for gradual rollout
if (FeatureFlags.UseShardedDatabase)
{
    services.AddDbContext<StorageContext>(options =>
        options.UseNpgsql(Configuration["ShardedDatabase:Storage"]));
}
else
{
    services.AddDbContext<StorageContext>(options =>
        options.UseNpgsql(Configuration["Database:DefaultConnection"]));
}
```

### 7. Monitoring and Optimization

#### Key Metrics
```sql
-- Connection pool efficiency
SELECT datname, numbackends, 
       pg_size_pretty(pg_database_size(datname)) as size
FROM pg_stat_database
WHERE datname NOT IN ('template0', 'template1', 'postgres');

-- Query performance by database
SELECT datname, 
       mean_exec_time,
       calls,
       total_exec_time
FROM pg_stat_statements
JOIN pg_database ON dbid = oid
ORDER BY mean_exec_time DESC;
```

#### Grafana Dashboard Queries
```promql
# Database connections by service
sum by (service, database) (pg_stat_database_numbackends)

# Query latency percentiles
histogram_quantile(0.95, 
  sum by (service, le) (
    rate(postgresql_query_duration_seconds_bucket[5m])
  )
)

# Replication lag
pg_replication_lag_seconds
```

### 8. Disaster Recovery

#### Backup Strategy
```yaml
# Kubernetes CronJob for automated backups
apiVersion: batch/v1
kind: CronJob
metadata:
  name: database-backup
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: postgres:16-alpine
            command:
            - /bin/sh
            - -c
            - |
              # Backup each shard
              for db in core blockchain analytics; do
                pg_dump -h postgres-$db -U neo_$db_user -d neo_$db | \
                  gzip > /backup/neo_$db_$(date +%Y%m%d).sql.gz
              done
            volumeMounts:
            - name: backup
              mountPath: /backup
          volumes:
          - name: backup
            persistentVolumeClaim:
              claimName: backup-pvc
```

### 9. Cost Optimization

- **Right-sizing**: Start with smaller instances, scale based on metrics
- **Reserved Instances**: Use cloud provider reserved instances for predictable workloads
- **Storage Tiering**: Move old data to cheaper storage classes
- **Compression**: Enable PostgreSQL compression for large tables

### 10. Security Considerations

- **Network Isolation**: Each database cluster in separate network segment
- **Encryption**: TLS for all database connections
- **Access Control**: Service-specific database users with minimal permissions
- **Audit Logging**: Enable PostgreSQL audit extension

## Implementation Timeline

- **Week 1**: Set up separate database instances
- **Week 2**: Migrate schemas and data
- **Week 3**: Configure read replicas
- **Week 4**: Implement connection pooling and monitoring
- **Week 5**: Performance testing and optimization
- **Week 6**: Production rollout with feature flags

## Success Metrics

- **Query Latency**: < 50ms p95 for read operations
- **Write Throughput**: > 10,000 TPS per shard
- **Connection Pool Efficiency**: > 80% connection reuse
- **Replication Lag**: < 100ms for read replicas
- **Availability**: 99.99% uptime per database cluster

## Rollback Plan

If issues arise during migration:
1. Feature flags to revert to monolithic database
2. Keep original database running for 30 days
3. Continuous replication from sharded to monolithic
4. One-command rollback script

This sharding strategy provides the scalability needed for production workloads while maintaining data consistency and operational simplicity.