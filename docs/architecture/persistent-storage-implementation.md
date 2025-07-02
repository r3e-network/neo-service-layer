# Persistent Storage Implementation Guide

## Overview

The Neo Service Layer has been enhanced with comprehensive persistent storage capabilities to ensure all service data is durable and survives restarts, crashes, and deployments. This document details the implementation and configuration of persistent storage across all services.

## Architecture

### Storage Layers

1. **Primary Persistent Storage** - OcclumFileStorageProvider
   - File-based storage within Intel SGX enclaves
   - Encrypted and compressed data storage
   - Transaction support for atomic operations
   - Automatic backup and recovery

2. **Distributed Cache** - Redis (Optional)
   - High-performance caching layer
   - Session management
   - Rate limiting and temporary data
   - Pub/Sub for real-time events

3. **In-Memory Cache** - Service-level caching
   - Hot data for performance
   - Synchronized with persistent storage
   - Automatic cache warming on startup

### Data Flow

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Application   │────▶│  Service Layer   │────▶│   In-Memory     │
│    Request      │     │                  │     │     Cache       │
└─────────────────┘     └────────┬─────────┘     └────────┬────────┘
                                 │                         │
                                 ▼                         ▼
                        ┌──────────────────┐     ┌─────────────────┐
                        │  Redis Cache     │◀────│   Persistent    │
                        │  (Distributed)   │     │    Storage      │
                        └──────────────────┘     │  (Occlum/File)  │
                                                 └─────────────────┘
```

## Implementation Details

### Service Updates

#### 1. NotificationService
- **Persistent Data**: Subscriptions, templates, notification history, channel configurations
- **Storage Keys**:
  - `notification:subscription:{id}` - User subscriptions
  - `notification:template:{id}` - Notification templates
  - `notification:history:{timestamp}:{id}` - Historical records
  - `notification:channel:{name}` - Channel configurations
- **Retention**: History retained for 30 days

#### 2. MonitoringService
- **Persistent Data**: Metrics, alerts, health status, monitoring sessions
- **Storage Keys**:
  - `monitoring:metrics:{service}:{timestamp}` - Time-series metrics
  - `monitoring:alert:{id}` - Active and resolved alerts
  - `monitoring:health:{service}:{timestamp}` - Health history
  - `monitoring:aggregate:{period}:{timestamp}` - Aggregated data
- **Retention**: Metrics for 7 days, aggregates for 90 days

#### 3. StorageService
- **Persistent Data**: Metadata, indexes, statistics
- **Storage Keys**:
  - `storage:metadata:{key}` - Storage metadata
  - `storage:index:{type}:{value}:{key}` - Query indexes
  - `storage:statistics` - Service statistics
- **Features**: Indexed queries by owner, class, date

### Configuration

#### appsettings.PersistentStorage.json
```json
{
  "Storage": {
    "Provider": "OcclumFile",
    "Path": "/secure_storage",
    "EnablePersistence": true,
    "PersistenceOptions": {
      "AutoSave": true,
      "AutoSaveInterval": 300,
      "CompactionInterval": 3600,
      "MaxFileSize": 1073741824,
      "EnableCompression": true,
      "EnableEncryption": true
    }
  },
  "Redis": {
    "Configuration": "localhost:6379",
    "EnableCaching": true
  },
  "Services": {
    "Notification": {
      "UsePersistentStorage": true,
      "HistoryRetentionDays": 30
    },
    "Monitoring": {
      "UsePersistentStorage": true,
      "MetricsRetentionDays": 7
    }
  }
}
```

### Startup Configuration

```csharp
// Program.cs
builder.Services.AddPersistentStorageServices(builder.Configuration);
builder.Services.ConfigureServicesWithPersistentStorage(builder.Configuration);
```

## Key Features

### 1. Automatic Data Recovery
- Services automatically load persisted data on startup
- Graceful handling of missing or corrupted data
- Fallback to in-memory operation if storage unavailable

### 2. Data Integrity
- Transactional operations for critical updates
- Checksum validation for stored data
- Automatic corruption detection and recovery

### 3. Performance Optimization
- Asynchronous persistence operations
- Batch writes for efficiency
- Intelligent caching strategies
- Compression for storage efficiency

### 4. Security
- All sensitive data encrypted at rest
- Access control via service boundaries
- Audit logging for compliance

### 5. Maintenance
- Automatic cleanup of expired data
- Storage compaction to reclaim space
- Health checks for storage availability
- Metrics for monitoring storage usage

## Migration from In-Memory

### Before (In-Memory Only)
```csharp
private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();

public async Task Subscribe(Subscription subscription)
{
    _subscriptions[subscription.Id] = subscription;
}
```

### After (With Persistent Storage)
```csharp
private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
private readonly IPersistentStorageProvider? _persistentStorage;

public async Task Subscribe(Subscription subscription)
{
    _subscriptions[subscription.Id] = subscription;
    await PersistSubscriptionAsync(subscription); // Persist to storage
}

private async Task LoadPersistentDataAsync()
{
    // Load from storage on startup
    var keys = await _persistentStorage.ListKeysAsync(SUBSCRIPTION_PREFIX);
    foreach (var key in keys)
    {
        var data = await _persistentStorage.RetrieveAsync(key);
        var subscription = JsonSerializer.Deserialize<Subscription>(data);
        _subscriptions[subscription.Id] = subscription;
    }
}
```

## Benefits

1. **Data Durability**: No data loss on service restarts or crashes
2. **Scalability**: Services can be scaled horizontally with shared storage
3. **Disaster Recovery**: Automatic backups and point-in-time recovery
4. **Compliance**: Audit trails and data retention policies
5. **Performance**: Strategic caching reduces storage access

## Monitoring

### Health Checks
```bash
# Check storage health
curl http://localhost:5000/health

# Response includes storage status
{
  "status": "Healthy",
  "results": {
    "persistent-storage": {
      "status": "Healthy",
      "description": "Persistent storage is healthy. Keys: 1234, Size: 567890 bytes"
    }
  }
}
```

### Metrics
- Storage size and key count
- Read/write operations per second
- Cache hit/miss ratios
- Compression ratios
- Error rates

## Best Practices

1. **Key Naming**: Use consistent prefixes for easy management
2. **TTL Usage**: Set appropriate TTL for temporary data
3. **Batch Operations**: Group related writes for efficiency
4. **Error Handling**: Always have fallback for storage failures
5. **Testing**: Test with storage unavailable scenarios

## Future Enhancements

1. **Multi-Region Replication**: Geo-distributed storage
2. **S3 Integration**: Cloud storage backend option
3. **Blockchain Storage**: Store metadata on-chain
4. **Advanced Indexing**: Full-text search capabilities
5. **Data Migration Tools**: Automated migration utilities