# Neo Service Layer - PostgreSQL Unified Persistence Documentation

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Database Schema](#database-schema)
- [Implementation Guide](#implementation-guide)
- [Configuration](#configuration)
- [Migration Management](#migration-management)
- [Security](#security)
- [Performance Optimization](#performance-optimization)
- [Monitoring & Health Checks](#monitoring--health-checks)
- [Backup & Recovery](#backup--recovery)
- [Troubleshooting](#troubleshooting)

## Overview

The Neo Service Layer implements a unified PostgreSQL persistence layer that provides centralized data management for all microservices, including SGX confidential computing, blockchain services, and monitoring systems.

### Key Features
- **Unified Schema**: Single database with logical schema separation
- **Transaction Support**: ACID compliance with distributed transaction coordination
- **Performance Optimized**: Indexed queries, connection pooling, and caching
- **Security First**: Row-level security, encryption at rest, and audit logging
- **High Availability**: Replication, automated backups, and disaster recovery
- **SGX Integration**: Secure storage for sealed data with attestation support

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  API Gateway │ Auth Service │ Compute │ Monitoring │ etc.   │
├─────────────────────────────────────────────────────────────┤
│                  Repository Pattern Layer                    │
├─────────────────────────────────────────────────────────────┤
│                    Unit of Work Pattern                      │
├─────────────────────────────────────────────────────────────┤
│                  Entity Framework Core                       │
├─────────────────────────────────────────────────────────────┤
│                    PostgreSQL Database                       │
│  ┌──────┬──────┬──────┬──────┬──────┬──────┬──────┬──────┐│
│  │ Core │ SGX  │ Auth │Compute│Oracle│Voting│ ... │Monitor││
│  └──────┴──────┴──────┴──────┴──────┴──────┴──────┴──────┘│
└─────────────────────────────────────────────────────────────┘
```

### Repository Pattern Implementation

```csharp
// Generic repository interface
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsAsync(Guid id);
}

// Unit of Work pattern
public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> GetRepository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

## Database Schema

### Schema Organization

The database is organized into logical schemas for service separation:

| Schema | Purpose | Key Tables |
|--------|---------|------------|
| `core` | System core entities | users, services, configurations |
| `sgx` | SGX confidential computing | sealed_data_items, attestations, policies |
| `auth` | Authentication & authorization | roles, permissions, sessions |
| `compute` | Computation services | computations, results, permissions |
| `oracle` | Oracle services | data_feeds, requests, responses |
| `voting` | Voting & governance | proposals, votes, voting_powers |
| `crosschain` | Cross-chain operations | transactions, bridge_operations |
| `monitoring` | System monitoring | metrics, alerts, audit_logs |
| `eventsourcing` | Event sourcing | events, snapshots, aggregates |

### Key Entity Relationships

```sql
-- Example: SGX Sealed Data with Policy
sealed_data_items
    ├── sealing_policies (N:1)
    └── enclave_attestations (1:N)

-- Example: Computation with Results
computations
    ├── computation_statuses (1:N)
    ├── computation_results (1:N)
    └── computation_permissions (1:N)
```

## Implementation Guide

### 1. Service Base Class

All services should inherit from `BasePostgreSQLService`:

```csharp
public class MyService : BasePostgreSQLService<MyService>, IMyService
{
    private readonly IGenericRepository<MyEntity> _repository;
    
    public MyService(IUnitOfWork unitOfWork, ILogger<MyService> logger)
        : base(unitOfWork, logger)
    {
        _repository = unitOfWork.GetRepository<MyEntity>();
    }
    
    public async Task<MyEntity> CreateAsync(MyEntityDto dto)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var entity = new MyEntity
            {
                // Map from DTO
            };
            
            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            
            return entity;
        }, "CreateMyEntity");
    }
}
```

### 2. Entity Configuration

Configure entities in the DbContext:

```csharp
modelBuilder.Entity<MyEntity>(entity =>
{
    entity.ToTable("my_entities", "myschema");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
    entity.HasIndex(e => e.CreatedAt);
    
    // Configure relationships
    entity.HasOne(e => e.Parent)
          .WithMany(p => p.Children)
          .HasForeignKey(e => e.ParentId);
});
```

### 3. Repository Usage

```csharp
// Simple query
var entity = await _repository.GetByIdAsync(id);

// Complex query with specification
var specification = new ActiveEntitiesSpecification()
    .WithPaging(pageNumber, pageSize)
    .OrderBy(e => e.CreatedAt);
    
var entities = await _repository.GetAsync(specification);

// Transaction example
await _unitOfWork.BeginTransactionAsync();
try
{
    await _repository.AddAsync(entity1);
    await _repository.AddAsync(entity2);
    await _unitOfWork.SaveChangesAsync();
    await _unitOfWork.CommitAsync();
}
catch
{
    await _unitOfWork.RollbackAsync();
    throw;
}
```

## Configuration

### Connection String Configuration

```json
// appsettings.json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=neoservice;Username=neoservice_app;Password=YourSecurePassword;Include Error Detail=true;Log Parameters=true"
  }
}
```

### Service Registration

```csharp
// Program.cs or Startup.cs
services.AddPostgreSQLPersistence(configuration);
services.AddPostgreSQLHealthChecks(configuration);
```

### Environment Variables

```bash
# Database
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=neoservice
POSTGRES_USER=neoservice_app
POSTGRES_PASSWORD=SecurePassword123!

# Connection Pool
DB_MAX_POOL_SIZE=100
DB_CONNECTION_TIMEOUT=30
DB_COMMAND_TIMEOUT=30

# Performance
DB_ENABLE_RETRIES=true
DB_MAX_RETRY_COUNT=3
DB_RETRY_DELAY_MS=1000
```

## Migration Management

### Creating Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName -c NeoServiceLayerDbContext

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script -o migration.sql
```

### Running Migrations

```bash
# Using the migration runner script
./scripts/run-database-migrations.sh migrate

# Check migration status
./scripts/run-database-migrations.sh status

# Rollback last migration
./scripts/run-database-migrations.sh rollback
```

### Migration Best Practices

1. **Always test migrations** in a development environment first
2. **Create rollback scripts** for each migration
3. **Use transactions** for data migrations
4. **Version control** all migration files
5. **Document breaking changes** in migration comments

## Security

### Connection Security

```yaml
# PostgreSQL SSL Configuration
ssl: on
ssl_cert_file: /var/lib/postgresql/server.crt
ssl_key_file: /var/lib/postgresql/server.key
ssl_ca_file: /var/lib/postgresql/ca.crt
```

### Row-Level Security

```sql
-- Enable RLS on sensitive tables
ALTER TABLE sgx.sealed_data_items ENABLE ROW LEVEL SECURITY;

-- Create policies
CREATE POLICY service_isolation ON sgx.sealed_data_items
    FOR ALL
    USING (service_name = current_setting('app.current_service'));
```

### Audit Logging

```sql
-- Audit trigger for sensitive operations
CREATE TRIGGER audit_sealed_data_changes
    AFTER INSERT OR UPDATE OR DELETE ON sgx.sealed_data_items
    FOR EACH ROW EXECUTE FUNCTION audit_changes();
```

## Performance Optimization

### Indexing Strategy

```sql
-- Composite indexes for common queries
CREATE INDEX idx_sealed_data_service_expiry 
    ON sgx.sealed_data_items(service_name, expires_at)
    WHERE expires_at IS NOT NULL;

-- Partial indexes for filtered queries
CREATE INDEX idx_active_computations 
    ON compute.computations(blockchain_type)
    WHERE is_active = true;

-- GiST indexes for JSONB queries
CREATE INDEX idx_metadata_gin 
    ON core.services USING gin(metadata);
```

### Connection Pooling

```csharp
// Configure connection pooling
services.AddDbContext<NeoServiceLayerDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.CommandTimeout(30);
    });
    
    // Connection pool settings
    options.UseConnectionPooling(true);
    options.SetMaxPoolSize(100);
    options.SetMinPoolSize(10);
});
```

### Query Optimization

```csharp
// Use projection for read-only queries
var results = await _context.Computations
    .Where(c => c.IsActive)
    .Select(c => new ComputationDto
    {
        Id = c.Id,
        Name = c.Name
    })
    .AsNoTracking()
    .ToListAsync();

// Batch operations
await _context.BulkInsertAsync(entities);
await _context.BulkUpdateAsync(entities);
```

## Monitoring & Health Checks

### Health Check Implementation

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly NeoServiceLayerDbContext _context;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Check response time
            var stopwatch = Stopwatch.StartNew();
            await _context.Database.ExecuteSqlRawAsync(
                "SELECT 1", cancellationToken);
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Slow response: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            return HealthCheckResult.Healthy(
                $"Response time: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed", ex);
        }
    }
}
```

### Metrics Collection

```yaml
# Prometheus metrics to monitor
- neo_db_connections_active
- neo_db_connections_idle
- neo_db_query_duration_seconds
- neo_db_transaction_duration_seconds
- neo_db_errors_total
```

## Backup & Recovery

### Automated Backup Strategy

```bash
# Daily backup script
#!/bin/bash
BACKUP_DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="neoservice_backup_${BACKUP_DATE}.sql.gz"

pg_dump -h localhost -U neoservice_app -d neoservice \
    --verbose --no-owner --no-acl --clean --if-exists \
    | gzip > /backups/${BACKUP_FILE}

# Verify backup
gunzip -t /backups/${BACKUP_FILE}

# Upload to cloud storage
aws s3 cp /backups/${BACKUP_FILE} s3://backups/postgresql/
```

### Recovery Procedures

```bash
# Restore from backup
gunzip < backup.sql.gz | psql -h localhost -U neoservice_app -d neoservice

# Point-in-time recovery
pg_basebackup -h localhost -U replicator -D /recovery -Fp -Xs -P
```

## Troubleshooting

### Common Issues

#### 1. Connection Pool Exhaustion
```csharp
// Solution: Increase pool size or optimize query patterns
services.Configure<NpgsqlConnectionStringBuilder>(options =>
{
    options.MaxPoolSize = 200;
    options.ConnectionIdleLifetime = 300;
});
```

#### 2. Slow Queries
```sql
-- Identify slow queries
SELECT query, calls, mean_exec_time
FROM pg_stat_statements
WHERE mean_exec_time > 1000
ORDER BY mean_exec_time DESC;

-- Add missing indexes
CREATE INDEX CONCURRENTLY idx_missing 
    ON table_name(column_name);
```

#### 3. Lock Contention
```sql
-- Find blocking queries
SELECT blocked_locks.pid AS blocked_pid,
       blocking_locks.pid AS blocking_pid,
       blocked_activity.query AS blocked_query,
       blocking_activity.query AS blocking_query
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity 
    ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks 
    ON blocking_locks.locktype = blocked_locks.locktype
WHERE NOT blocked_locks.granted;
```

### Performance Tuning Checklist

- [ ] Analyze query execution plans
- [ ] Create appropriate indexes
- [ ] Configure connection pooling
- [ ] Enable query caching
- [ ] Optimize transaction scope
- [ ] Use batch operations
- [ ] Implement pagination
- [ ] Monitor slow query log
- [ ] Regular VACUUM and ANALYZE
- [ ] Partition large tables

## Additional Resources

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Npgsql Driver](https://www.npgsql.org/)
- [PostgreSQL Performance Tuning](https://wiki.postgresql.org/wiki/Performance_Optimization)

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review application logs: `/app/logs/`
3. Check PostgreSQL logs: `docker logs neo-postgres`
4. Create an issue on GitHub