# Neo Service Layer - Storage Service

## Overview

The Storage Service provides secure, scalable, and high-performance data storage capabilities for the Neo Service Layer ecosystem. It offers encrypted storage, versioning, access control, and seamless integration with Neo blockchain for data integrity verification.

## Features

- **Encrypted Storage**: AES-256 encryption for data at rest
- **Versioning**: Automatic version tracking with rollback capabilities
- **Access Control**: Fine-grained permissions with role-based access
- **Data Integrity**: Blockchain-anchored checksums for tamper detection
- **Multi-tenancy**: Isolated storage spaces for different applications
- **Compression**: Automatic compression for efficient storage
- **Caching**: Integrated caching layer for performance
- **Audit Trail**: Complete audit logging of all operations

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Storage Service                     │
├─────────────────────────────────────────────────────┤
│  API Gateway Layer                                  │
│  ├── REST API Endpoints                             │
│  ├── GraphQL Interface                              │
│  └── gRPC Service                                   │
├─────────────────────────────────────────────────────┤
│  Business Logic Layer                               │
│  ├── Access Control Engine                          │
│  ├── Encryption/Decryption Service                  │
│  ├── Version Management                             │
│  └── Data Validation                                │
├─────────────────────────────────────────────────────┤
│  Storage Engine                                     │
│  ├── Primary Storage (PostgreSQL)                   │
│  ├── Object Storage (S3-compatible)                 │
│  ├── Cache Layer (Redis)                            │
│  └── Search Index (Elasticsearch)                   │
├─────────────────────────────────────────────────────┤
│  Blockchain Integration                             │
│  ├── Data Integrity Anchoring                       │
│  ├── Smart Contract Integration                     │
│  └── Event Streaming                                │
└─────────────────────────────────────────────────────┘
```

## Configuration

### Environment Variables

```bash
# Service Configuration
SERVICE_NAME=storage-service
SERVICE_PORT=8080
LOG_LEVEL=Information
MAX_REQUEST_SIZE=104857600  # 100MB

# Database Configuration
DB_CONNECTION_STRING=Host=postgres-storage;Database=neo_storage;Username=storage_user;Password=${DB_PASSWORD}
DB_POOL_SIZE=50
DB_COMMAND_TIMEOUT=30

# Object Storage Configuration
S3_ENDPOINT=https://s3.amazonaws.com
S3_BUCKET=neo-storage-prod
S3_ACCESS_KEY=${S3_ACCESS_KEY}
S3_SECRET_KEY=${S3_SECRET_KEY}
S3_REGION=us-east-1

# Redis Cache Configuration
REDIS_CONNECTION=redis-storage:6379,password=${REDIS_PASSWORD}
CACHE_EXPIRATION_MINUTES=60
CACHE_SLIDING_EXPIRATION=true

# Encryption Configuration
ENCRYPTION_ENABLED=true
ENCRYPTION_ALGORITHM=AES256
KEY_DERIVATION_ITERATIONS=100000

# Blockchain Configuration
NEO_RPC_ENDPOINT=https://mainnet1-seed.neo.org:10332
ANCHORING_ENABLED=true
ANCHORING_INTERVAL_MINUTES=60

# Security Configuration
ENABLE_AUTH=true
JWT_SECRET=${JWT_SECRET}
REQUIRE_HTTPS=true
ALLOWED_ORIGINS=https://app.neo-service-layer.com

# Performance Configuration
COMPRESSION_ENABLED=true
COMPRESSION_LEVEL=6
PARALLEL_UPLOAD_THREADS=4
CHUNK_SIZE_BYTES=1048576  # 1MB
```

### Configuration File (appsettings.json)

```json
{
  "StorageService": {
    "MaxFileSize": 104857600,
    "AllowedFileTypes": [".json", ".xml", ".csv", ".txt", ".pdf"],
    "StorageQuotas": {
      "DefaultUserQuota": 1073741824,
      "PremiumUserQuota": 10737418240
    },
    "Versioning": {
      "MaxVersionsPerFile": 100,
      "RetentionDays": 90
    },
    "Replication": {
      "Enabled": true,
      "ReplicationFactor": 3,
      "CrossRegionBackup": true
    }
  }
}
```

## API Endpoints

### Base URL
```
https://api.neo-service-layer.com/v1/storage
```

### Storage Operations

#### Upload File
```http
POST /storage/files
Content-Type: multipart/form-data
Authorization: Bearer <token>

{
  "file": <binary>,
  "metadata": {
    "name": "document.pdf",
    "description": "Important document",
    "tags": ["contract", "2025"],
    "encryption": true
  }
}
```

**Response:**
```json
{
  "fileId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "document.pdf",
  "size": 1048576,
  "contentType": "application/pdf",
  "checksum": "sha256:abcd1234...",
  "encrypted": true,
  "version": 1,
  "createdAt": "2025-01-10T10:00:00Z",
  "blockchainTxId": "0x1234abcd...",
  "urls": {
    "download": "https://api.neo-service-layer.com/v1/storage/files/550e8400-e29b-41d4-a716-446655440000",
    "metadata": "https://api.neo-service-layer.com/v1/storage/files/550e8400-e29b-41d4-a716-446655440000/metadata"
  }
}
```

#### Download File
```http
GET /storage/files/{fileId}
Authorization: Bearer <token>
Accept: application/octet-stream
```

#### Get File Metadata
```http
GET /storage/files/{fileId}/metadata
Authorization: Bearer <token>
```

**Response:**
```json
{
  "fileId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "document.pdf",
  "size": 1048576,
  "contentType": "application/pdf",
  "checksum": "sha256:abcd1234...",
  "encrypted": true,
  "compression": "gzip",
  "versions": [
    {
      "version": 1,
      "size": 1048576,
      "checksum": "sha256:abcd1234...",
      "createdAt": "2025-01-10T10:00:00Z",
      "createdBy": "user123"
    }
  ],
  "permissions": {
    "owner": "user123",
    "readers": ["user456", "group:admins"],
    "writers": ["user123"]
  },
  "blockchain": {
    "anchored": true,
    "txId": "0x1234abcd...",
    "blockHeight": 1234567,
    "timestamp": "2025-01-10T10:05:00Z"
  }
}
```

#### Update File
```http
PUT /storage/files/{fileId}
Content-Type: multipart/form-data
Authorization: Bearer <token>

{
  "file": <binary>,
  "createNewVersion": true,
  "metadata": {
    "description": "Updated document"
  }
}
```

#### Delete File
```http
DELETE /storage/files/{fileId}
Authorization: Bearer <token>
```

### Directory Operations

#### Create Directory
```http
POST /storage/directories
Content-Type: application/json
Authorization: Bearer <token>

{
  "path": "/documents/contracts",
  "metadata": {
    "description": "Contract storage"
  }
}
```

#### List Directory Contents
```http
GET /storage/directories/{path}?page=1&size=50
Authorization: Bearer <token>
```

**Response:**
```json
{
  "path": "/documents/contracts",
  "items": [
    {
      "type": "file",
      "name": "contract-2025.pdf",
      "fileId": "550e8400-e29b-41d4-a716-446655440000",
      "size": 1048576,
      "modifiedAt": "2025-01-10T10:00:00Z"
    },
    {
      "type": "directory",
      "name": "archive",
      "path": "/documents/contracts/archive",
      "itemCount": 42
    }
  ],
  "pagination": {
    "page": 1,
    "size": 50,
    "total": 2
  }
}
```

### Access Control

#### Grant Permission
```http
POST /storage/files/{fileId}/permissions
Content-Type: application/json
Authorization: Bearer <token>

{
  "principal": "user456",
  "permission": "read",
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

#### List Permissions
```http
GET /storage/files/{fileId}/permissions
Authorization: Bearer <token>
```

### Search Operations

#### Search Files
```http
POST /storage/search
Content-Type: application/json
Authorization: Bearer <token>

{
  "query": "contract",
  "filters": {
    "contentType": ["application/pdf"],
    "dateRange": {
      "from": "2025-01-01",
      "to": "2025-12-31"
    },
    "tags": ["important"]
  },
  "sort": {
    "field": "modifiedAt",
    "order": "desc"
  },
  "pagination": {
    "page": 1,
    "size": 20
  }
}
```

## Security

### Encryption

All files are encrypted using AES-256-GCM with unique keys per file:

```csharp
public class FileEncryption
{
    public EncryptedFile Encrypt(Stream fileStream, byte[] key)
    {
        using var aes = new AesGcm(key);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        
        var plaintext = fileStream.ToArray();
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        
        return new EncryptedFile
        {
            Ciphertext = ciphertext,
            Nonce = nonce,
            Tag = tag
        };
    }
}
```

### Access Control

Fine-grained permissions system:

```csharp
public enum StoragePermission
{
    Read = 1,
    Write = 2,
    Delete = 4,
    Share = 8,
    Admin = 16
}

public class AccessControl
{
    public bool HasPermission(string userId, string fileId, StoragePermission permission)
    {
        var userPermissions = GetUserPermissions(userId, fileId);
        return (userPermissions & permission) == permission;
    }
}
```

### Blockchain Integration

Files are anchored to the Neo blockchain for integrity:

```csharp
public class BlockchainAnchor
{
    public async Task<string> AnchorFile(string fileId, string checksum)
    {
        var contract = new StorageContract();
        var result = await contract.AnchorData(fileId, checksum);
        return result.Transaction.Hash.ToString();
    }
}
```

## Performance

### Caching Strategy

Multi-level caching for optimal performance:

1. **Memory Cache**: Hot data cached in-process
2. **Redis Cache**: Distributed cache for metadata
3. **CDN**: Static file distribution

### Optimization Techniques

- **Chunked Uploads**: Large files uploaded in chunks
- **Parallel Processing**: Multi-threaded upload/download
- **Compression**: Automatic compression for text files
- **Deduplication**: Content-based deduplication

## Monitoring

### Metrics

```prometheus
# Storage usage
storage_used_bytes{tenant="default"} 1234567890
storage_quota_bytes{tenant="default"} 10737418240

# Operation metrics
storage_operations_total{operation="upload",status="success"} 12345
storage_operation_duration_seconds{operation="upload",quantile="0.95"} 1.23

# Cache metrics
storage_cache_hits_total 98765
storage_cache_misses_total 1234
```

### Health Checks

```http
GET /health/live
GET /health/ready
GET /health/storage
```

## Development

### Local Development

```bash
# Start dependencies
docker-compose up -d postgres redis minio

# Run the service
dotnet run --project src/Services/NeoServiceLayer.Services.Storage

# Run tests
dotnet test tests/NeoServiceLayer.Services.Storage.Tests
```

### SDK Examples

#### C# SDK
```csharp
var client = new StorageClient(apiKey);

// Upload file
var file = await client.UploadFileAsync(
    fileStream, 
    "document.pdf",
    new FileMetadata { Encrypted = true }
);

// Download file
var stream = await client.DownloadFileAsync(file.FileId);

// Search files
var results = await client.SearchFilesAsync(
    query: "contract",
    tags: new[] { "important" }
);
```

#### JavaScript SDK
```javascript
const client = new StorageClient({ apiKey });

// Upload file
const file = await client.uploadFile(fileBlob, {
  name: 'document.pdf',
  encrypted: true
});

// Download file
const blob = await client.downloadFile(file.fileId);

// Search files
const results = await client.searchFiles({
  query: 'contract',
  tags: ['important']
});
```

## Troubleshooting

### Common Issues

1. **Upload Failures**
   - Check file size limits
   - Verify network connectivity
   - Ensure proper authentication

2. **Permission Denied**
   - Verify JWT token is valid
   - Check file permissions
   - Review access control settings

3. **Slow Performance**
   - Check cache hit rates
   - Review database query performance
   - Monitor network latency

### Debugging

Enable debug logging:
```bash
LOG_LEVEL=Debug dotnet run
```

View detailed metrics:
```bash
curl http://localhost:8080/metrics
```

## License

This service is part of the Neo Service Layer and is licensed under the MIT License.