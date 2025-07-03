# Secrets Management Service

## Overview

The Secrets Management Service provides secure storage, retrieval, and management of sensitive data such as API keys, passwords, certificates, and cryptographic keys. All secrets are stored within the Intel SGX enclave with hardware-level protection and encryption at rest.

## Features

- **Hardware-Protected Storage**: Secrets stored within SGX enclave memory
- **Encryption at Rest**: AES-256-GCM encryption for all stored secrets
- **Access Control**: Fine-grained role-based access control (RBAC)
- **Audit Logging**: Complete audit trail of all secret access
- **Secret Rotation**: Automated secret rotation with version history
- **Dynamic Secrets**: Generate temporary credentials on-demand
- **Secret Sharing**: Shamir's Secret Sharing for multi-party access
- **HSM Integration**: Support for Hardware Security Modules

## API Reference

### Create Secret

Stores a new secret in the secure vault.

**Endpoint**: `POST /api/v1/secrets/create/{blockchainType}`

**Request Body**:
```json
{
  "name": "api-key-production",
  "value": "sensitive-value-here",
  "type": "API_KEY",
  "metadata": {
    "environment": "production",
    "service": "external-api",
    "expiresAt": "2025-12-31T23:59:59Z"
  },
  "accessPolicy": {
    "roles": ["admin", "service-account"],
    "ipWhitelist": ["192.168.1.0/24"],
    "requiresMFA": true
  }
}
```

**Response**:
```json
{
  "success": true,
  "secretId": "sec_123abc...",
  "version": 1,
  "createdAt": "2025-01-01T00:00:00Z",
  "fingerprint": "SHA256:abcd1234..."
}
```

### Retrieve Secret

Retrieves a secret value with audit logging.

**Endpoint**: `GET /api/v1/secrets/{secretId}/retrieve/{blockchainType}`

**Headers**:
```
Authorization: Bearer {token}
X-MFA-Token: 123456 (if required)
```

**Response**:
```json
{
  "success": true,
  "secretId": "sec_123abc...",
  "value": "decrypted-secret-value",
  "version": 1,
  "metadata": {
    "environment": "production",
    "service": "external-api"
  },
  "accessedAt": "2025-01-01T00:00:00Z"
}
```

### Update Secret

Updates an existing secret, creating a new version.

**Endpoint**: `PUT /api/v1/secrets/{secretId}/update/{blockchainType}`

**Request Body**:
```json
{
  "value": "new-sensitive-value",
  "metadata": {
    "updatedBy": "admin-user",
    "reason": "Regular rotation"
  }
}
```

### Delete Secret

Soft deletes a secret (recoverable for 30 days).

**Endpoint**: `DELETE /api/v1/secrets/{secretId}/{blockchainType}`

### List Secrets

Lists all accessible secrets (without values).

**Endpoint**: `GET /api/v1/secrets/list/{blockchainType}`

**Response**:
```json
{
  "secrets": [
    {
      "secretId": "sec_123abc...",
      "name": "api-key-production",
      "type": "API_KEY",
      "createdAt": "2025-01-01T00:00:00Z",
      "lastAccessed": "2025-01-15T10:30:00Z",
      "version": 2
    }
  ],
  "total": 25,
  "page": 1
}
```

### Rotate Secret

Triggers automatic rotation of a secret.

**Endpoint**: `POST /api/v1/secrets/{secretId}/rotate/{blockchainType}`

### Share Secret

Creates a Shamir's Secret Sharing scheme.

**Endpoint**: `POST /api/v1/secrets/{secretId}/share/{blockchainType}`

**Request Body**:
```json
{
  "totalShares": 5,
  "threshold": 3,
  "shareholders": [
    "user1@example.com",
    "user2@example.com",
    "user3@example.com",
    "user4@example.com",
    "user5@example.com"
  ]
}
```

## Configuration

Add to your `appsettings.json`:

```json
{
  "SecretsManagementService": {
    "Enabled": true,
    "EncryptionAlgorithm": "AES-256-GCM",
    "KeyDerivationFunction": "PBKDF2",
    "KeyDerivationIterations": 100000,
    "SecretRetentionDays": 30,
    "MaxSecretSize": 65536,
    "AuditRetentionDays": 365,
    "RotationPolicy": {
      "Enabled": true,
      "DefaultRotationDays": 90,
      "NotificationDays": 7
    },
    "AccessControl": {
      "RequireMFA": true,
      "SessionTimeout": 900,
      "MaxFailedAttempts": 3
    }
  }
}
```

## Secret Types

The service supports various secret types:

1. **API Keys**: External service API keys
2. **Passwords**: User or service passwords
3. **Certificates**: SSL/TLS certificates and keys
4. **SSH Keys**: SSH private/public key pairs
5. **Database Credentials**: Connection strings and passwords
6. **Encryption Keys**: Symmetric and asymmetric keys
7. **Tokens**: JWT, OAuth, and other tokens
8. **Configuration**: Sensitive configuration values

## Security Features

### Encryption

- **Master Key**: Derived from SGX sealing key
- **Per-Secret Encryption**: Each secret encrypted with unique key
- **Key Wrapping**: Keys wrapped using envelope encryption
- **Perfect Forward Secrecy**: Historical versions protected independently

### Access Control

- **RBAC**: Role-based access control
- **MFA Enforcement**: Optional multi-factor authentication
- **IP Whitelisting**: Restrict access by IP address
- **Time-based Access**: Temporary access windows

### Audit Trail

Every operation is logged with:
- User/Service identity
- Operation type
- Timestamp
- IP address
- Success/Failure status
- Access context

## Usage Examples

### Store API Key

```csharp
var client = new SecretsManagementServiceClient(apiKey);

var secret = new CreateSecretRequest
{
    Name = "stripe-api-key",
    Value = "sk_live_...",
    Type = SecretType.ApiKey,
    Metadata = new Dictionary<string, string>
    {
        ["environment"] = "production",
        ["service"] = "payment-processor"
    }
};

var result = await client.CreateSecretAsync(secret, BlockchainType.NeoN3);
Console.WriteLine($"Secret stored with ID: {result.SecretId}");
```

### Retrieve with Caching

```csharp
// Client handles caching automatically
var secret = await client.GetSecretAsync("sec_123abc", BlockchainType.NeoN3);

// Use the secret
var apiClient = new StripeClient(secret.Value);
```

### Implement Rotation

```csharp
// Set up automatic rotation
await client.ConfigureRotationAsync("sec_123abc", new RotationConfig
{
    RotationDays = 30,
    NotificationEmail = "admin@example.com",
    AutoRotate = true
});
```

## Best Practices

1. **Least Privilege**: Grant minimum necessary access
2. **Regular Rotation**: Rotate secrets every 30-90 days
3. **Avoid Hardcoding**: Never hardcode secrets in source code
4. **Use Secret Types**: Properly categorize secrets
5. **Monitor Access**: Review audit logs regularly
6. **Backup Strategy**: Implement secret backup procedures
7. **Emergency Access**: Plan for break-glass scenarios

## Performance Considerations

- Secret creation: ~100ms
- Secret retrieval: ~50ms (cached: <10ms)
- Secret rotation: ~200ms
- Audit query: ~100ms for last 24 hours

## Limitations

- Maximum secret size: 64KB
- Maximum secrets per account: 10,000
- Audit retention: 365 days
- Version history: Last 50 versions
- Concurrent operations: 100 per second

## Disaster Recovery

The service includes disaster recovery features:

1. **Automatic Backups**: Encrypted backups every 6 hours
2. **Point-in-Time Recovery**: Restore to any point within 30 days
3. **Geographic Redundancy**: Replicated across regions
4. **Recovery Time Objective (RTO)**: < 1 hour
5. **Recovery Point Objective (RPO)**: < 6 hours

## Related Services

- [Key Management Service](key-management-service.md) - For cryptographic operations
- [Configuration Service](configuration-service.md) - For non-sensitive configuration
- [Backup Service](backup-service.md) - For secret backup strategies
- [Monitoring Service](monitoring-service.md) - For access monitoring