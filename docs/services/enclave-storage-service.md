# Enclave Storage Service

## Overview

The Enclave Storage Service provides secure persistent storage capabilities within the Intel SGX enclave environment. It enables encrypted data persistence with hardware-level protection, ensuring that sensitive data remains protected even when written to disk.

## Features

- **Hardware-Protected Storage**: Data encrypted with SGX sealing keys
- **Persistent Encryption**: AES-256-GCM encryption at rest
- **Secure Key Derivation**: Keys derived from enclave measurements
- **Data Integrity**: HMAC-based integrity verification
- **Versioning Support**: Multiple versions of sealed data
- **Backup and Recovery**: Secure backup with re-sealing capabilities
- **Access Control**: Enclave-based access restrictions
- **Storage Quotas**: Configurable storage limits per service

## API Reference

### Store Sealed Data

Seals and stores data within the enclave.

**Endpoint**: `POST /api/v1/enclave/storage/seal/{blockchainType}`

**Request Body**:
```json
{
  "key": "user-secrets-v1",
  "data": "base64_encoded_sensitive_data",
  "metadata": {
    "service": "key-management",
    "version": 1,
    "created": "2025-01-01T00:00:00Z"
  },
  "policy": {
    "sealingPolicy": "MRENCLAVE",
    "expirationHours": 8760
  }
}
```

**Response**:
```json
{
  "success": true,
  "storageId": "seal_123abc...",
  "sealedSize": 1024,
  "fingerprint": "sha256:abcd1234...",
  "expiresAt": "2026-01-01T00:00:00Z"
}
```

### Unseal Data

Retrieves and unseals previously stored data.

**Endpoint**: `GET /api/v1/enclave/storage/unseal/{key}/{blockchainType}`

**Response**:
```json
{
  "success": true,
  "data": "base64_encoded_original_data",
  "metadata": {
    "service": "key-management",
    "version": 1,
    "created": "2025-01-01T00:00:00Z",
    "lastAccessed": "2025-01-15T10:30:00Z"
  },
  "sealed": true,
  "remainingReads": 100
}
```

### List Sealed Items

Lists all sealed data items for a service.

**Endpoint**: `GET /api/v1/enclave/storage/list/{blockchainType}`

**Query Parameters**:
- `service`: Filter by service name
- `prefix`: Key prefix filter

**Response**:
```json
{
  "items": [
    {
      "key": "user-secrets-v1",
      "size": 1024,
      "created": "2025-01-01T00:00:00Z",
      "lastAccessed": "2025-01-15T10:30:00Z",
      "expiresAt": "2026-01-01T00:00:00Z",
      "service": "key-management"
    }
  ],
  "totalSize": 5120,
  "itemCount": 5
}
```

### Delete Sealed Data

Securely deletes sealed data.

**Endpoint**: `DELETE /api/v1/enclave/storage/{key}/{blockchainType}`

**Response**:
```json
{
  "success": true,
  "deleted": true,
  "shredded": true,
  "timestamp": "2025-01-01T00:00:00Z"
}
```

## Configuration

Add to your `appsettings.json`:

```json
{
  "EnclaveStorageService": {
    "Enabled": true,
    "StoragePath": "/opt/neo/enclave/sealed",
    "MaxStorageSize": 1073741824,
    "DefaultSealingPolicy": "MRENCLAVE",
    "Encryption": {
      "Algorithm": "AES-256-GCM",
      "KeyDerivation": "HKDF-SHA256",
      "SaltLength": 32
    },
    "Quotas": {
      "MaxItemSize": 1048576,
      "MaxItemsPerService": 1000,
      "MaxTotalSize": 104857600
    },
    "Backup": {
      "Enabled": true,
      "BackupPath": "/opt/neo/enclave/backup",
      "RetentionDays": 30
    }
  }
}
```

## Sealing Policies

### MRENCLAVE Policy
- Data sealed to specific enclave measurement
- Only same enclave code can unseal
- Most secure but less flexible

### MRSIGNER Policy
- Data sealed to enclave signer identity
- Any enclave from same signer can unseal
- Allows enclave updates while maintaining access

## Usage Examples

### Store Sensitive Configuration

```csharp
var client = new EnclaveStorageServiceClient(apiKey);

var sealRequest = new SealDataRequest
{
    Key = "database-credentials",
    Data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(credentials)),
    Policy = new SealingPolicy
    {
        Type = SealingPolicyType.MrEnclave,
        ExpirationHours = 24 * 30 // 30 days
    }
};

var result = await client.SealDataAsync(sealRequest, BlockchainType.NeoN3);
Console.WriteLine($"Credentials sealed: {result.StorageId}");
```

### Retrieve Sealed Data

```csharp
var unsealResult = await client.UnsealDataAsync("database-credentials", BlockchainType.NeoN3);
if (unsealResult.Success)
{
    var credentials = JsonSerializer.Deserialize<DatabaseCredentials>(
        Encoding.UTF8.GetString(unsealResult.Data)
    );
    // Use credentials
}
```

### Implement Key Rotation

```csharp
// Retrieve old key
var oldKeyData = await client.UnsealDataAsync("encryption-key-v1", BlockchainType.NeoN3);

// Generate new key
var newKey = GenerateNewEncryptionKey();

// Store new key with version
await client.SealDataAsync(new SealDataRequest
{
    Key = "encryption-key-v2",
    Data = newKey,
    Metadata = new Dictionary<string, object>
    {
        ["version"] = 2,
        ["rotatedFrom"] = "encryption-key-v1",
        ["rotatedAt"] = DateTime.UtcNow
    }
}, BlockchainType.NeoN3);
```

## Security Architecture

```
┌─────────────────────────────────────────┐
│         SGX Enclave Boundary            │
│  ┌─────────────────────────────────┐   │
│  │   Enclave Storage Service       │   │
│  │  ┌──────────┐  ┌─────────────┐ │   │
│  │  │ Sealing  │  │   Access    │ │   │
│  │  │  Engine  │  │  Control    │ │   │
│  │  └──────────┘  └─────────────┘ │   │
│  │  ┌──────────────────────────┐  │   │
│  │  │  Encrypted Storage API   │  │   │
│  │  └──────────────────────────┘  │   │
│  └─────────────────────────────────┘   │
│              ↓ Sealed Data ↓            │
└─────────────────────────────────────────┘
                    ↓
        Host Filesystem (Encrypted)
```

## Best Practices

1. **Sealing Policy**: Choose appropriate policy based on update requirements
2. **Key Management**: Implement proper key lifecycle management
3. **Backup Strategy**: Regular backups with secure re-sealing
4. **Access Patterns**: Minimize unseal operations for performance
5. **Data Classification**: Only seal truly sensitive data

## Performance Considerations

- Seal operation: ~50ms for 1KB data
- Unseal operation: ~30ms for 1KB data
- Maximum item size: 1MB
- Concurrent operations: Limited by enclave memory
- Storage overhead: ~20% for encryption metadata

## Limitations

- Maximum sealed item size: 1MB
- Total storage per service: 100MB
- Concurrent seal operations: 10
- Key length limit: 256 characters
- Metadata size limit: 4KB

## Data Recovery

### Backup Process
1. Enumerate all sealed items
2. Unseal each item in enclave
3. Re-seal with backup key
4. Store in backup location

### Recovery Process
1. Load backup data
2. Unseal with backup key
3. Re-seal with production key
4. Restore to primary storage

## Related Services

- [Storage Service](storage-service.md) - For general data storage
- [Key Management Service](key-management-service.md) - For key lifecycle
- [Backup Service](backup-service.md) - For comprehensive backup strategies