# Neo Service Layer - API Documentation

## Table of Contents
- [Overview](#overview)
- [Authentication](#authentication)
- [API Endpoints](#api-endpoints)
- [Request/Response Formats](#requestresponse-formats)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)
- [Webhooks](#webhooks)
- [SDKs and Libraries](#sdks-and-libraries)
- [API Versioning](#api-versioning)
- [Testing](#testing)

## Overview

The Neo Service Layer provides a comprehensive REST API for blockchain services, confidential computing, and distributed applications.

### Base URLs
- **Production**: `https://api.neoservice.io/v1`
- **Staging**: `https://staging-api.neoservice.io/v1`
- **Development**: `http://localhost:8080/v1`

### API Standards
- RESTful design principles
- JSON request/response format
- OAuth 2.0 / JWT authentication
- OpenAPI 3.0 specification
- ISO 8601 date formats
- UTF-8 encoding

## Authentication

### JWT Token Authentication
```http
POST /auth/token
Content-Type: application/json

{
  "username": "user@example.com",
  "password": "SecurePassword123!",
  "grant_type": "password"
}

Response:
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "def50200..."
}
```

### Using the Token
```http
GET /api/resource
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### API Key Authentication
```http
GET /api/resource
X-API-Key: your-api-key-here
```

## API Endpoints

### Core Services

#### Health Check
```http
GET /health

Response:
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2024-01-15T10:30:00Z",
  "services": {
    "database": "healthy",
    "redis": "healthy",
    "sgx": "healthy"
  }
}
```

### Authentication Service

#### Register User
```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}

Response: 201 Created
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}

Response:
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "refresh_token": "def50200...",
  "expires_in": 3600,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "roles": ["user", "developer"]
  }
}
```

#### Refresh Token
```http
POST /auth/refresh
Content-Type: application/json

{
  "refresh_token": "def50200..."
}

Response:
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "expires_in": 3600
}
```

### SGX Confidential Computing

#### Seal Data
```http
POST /sgx/seal
Authorization: Bearer {token}
Content-Type: application/json

{
  "data": "SGVsbG8gV29ybGQh", // Base64 encoded
  "policy": {
    "encryptionAlgorithm": "AES256_GCM",
    "accessControl": ["read", "write"],
    "expiresAt": "2024-12-31T23:59:59Z"
  }
}

Response: 201 Created
{
  "id": "seal_550e8400-e29b-41d4-a716-446655440000",
  "status": "sealed",
  "algorithm": "AES256_GCM",
  "createdAt": "2024-01-15T10:30:00Z",
  "expiresAt": "2024-12-31T23:59:59Z"
}
```

#### Unseal Data
```http
GET /sgx/unseal/{sealId}
Authorization: Bearer {token}

Response:
{
  "id": "seal_550e8400-e29b-41d4-a716-446655440000",
  "data": "SGVsbG8gV29ybGQh",
  "unsealed_at": "2024-01-15T10:35:00Z"
}
```

#### Get Attestation
```http
GET /sgx/attestation
Authorization: Bearer {token}

Response:
{
  "quote": "0x...",
  "mrenclave": "0x...",
  "mrsigner": "0x...",
  "isvprodid": 1,
  "isvsvn": 1,
  "timestamp": "2024-01-15T10:30:00Z",
  "status": "OK"
}
```

### Compute Service

#### Submit Computation
```http
POST /compute/submit
Authorization: Bearer {token}
Content-Type: application/json

{
  "type": "WASM",
  "code": "AGFzbQEAAAAB...", // Base64 encoded WASM
  "inputs": {
    "data": [1, 2, 3, 4, 5]
  },
  "options": {
    "timeout": 30000,
    "maxMemory": "256MB",
    "priority": "high"
  }
}

Response: 202 Accepted
{
  "id": "comp_550e8400-e29b-41d4-a716-446655440000",
  "status": "queued",
  "estimatedTime": 5000,
  "queuePosition": 3
}
```

#### Get Computation Status
```http
GET /compute/status/{computationId}
Authorization: Bearer {token}

Response:
{
  "id": "comp_550e8400-e29b-41d4-a716-446655440000",
  "status": "completed",
  "progress": 100,
  "startedAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:30:05Z",
  "result": {
    "output": [2, 4, 6, 8, 10],
    "gasUsed": 1500000,
    "executionTime": 4500
  }
}
```

### Oracle Service

#### Create Data Feed
```http
POST /oracle/feeds
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "BTC/USD Price",
  "description": "Bitcoin to USD price feed",
  "source": "aggregated",
  "updateFrequency": 60,
  "aggregationMethod": "median",
  "sources": [
    "binance",
    "coinbase",
    "kraken"
  ]
}

Response: 201 Created
{
  "id": "feed_550e8400-e29b-41d4-a716-446655440000",
  "name": "BTC/USD Price",
  "status": "active",
  "currentValue": 45000.50,
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

#### Get Feed Data
```http
GET /oracle/feeds/{feedId}/data
Authorization: Bearer {token}

Response:
{
  "feedId": "feed_550e8400-e29b-41d4-a716-446655440000",
  "value": 45000.50,
  "timestamp": "2024-01-15T10:30:00Z",
  "sources": [
    {"source": "binance", "value": 45001.00},
    {"source": "coinbase", "value": 45000.00},
    {"source": "kraken", "value": 45000.50}
  ],
  "signature": "0x..."
}
```

### Voting Service

#### Create Proposal
```http
POST /voting/proposals
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Upgrade Protocol to v2.0",
  "description": "Proposal to upgrade the protocol...",
  "type": "governance",
  "options": [
    {"id": "yes", "label": "Yes"},
    {"id": "no", "label": "No"},
    {"id": "abstain", "label": "Abstain"}
  ],
  "startTime": "2024-01-20T00:00:00Z",
  "endTime": "2024-01-27T00:00:00Z",
  "quorum": 0.51
}

Response: 201 Created
{
  "id": "prop_550e8400-e29b-41d4-a716-446655440000",
  "title": "Upgrade Protocol to v2.0",
  "status": "pending",
  "createdAt": "2024-01-15T10:30:00Z",
  "startTime": "2024-01-20T00:00:00Z",
  "endTime": "2024-01-27T00:00:00Z"
}
```

#### Cast Vote
```http
POST /voting/proposals/{proposalId}/vote
Authorization: Bearer {token}
Content-Type: application/json

{
  "option": "yes",
  "weight": 1000,
  "proof": "0x..."
}

Response:
{
  "voteId": "vote_550e8400-e29b-41d4-a716-446655440000",
  "proposalId": "prop_550e8400-e29b-41d4-a716-446655440000",
  "voter": "0x...",
  "option": "yes",
  "weight": 1000,
  "timestamp": "2024-01-15T10:30:00Z",
  "txHash": "0x..."
}
```

### Cross-Chain Service

#### Bridge Token
```http
POST /crosschain/bridge
Authorization: Bearer {token}
Content-Type: application/json

{
  "fromChain": "ethereum",
  "toChain": "neo",
  "token": "USDT",
  "amount": "1000.00",
  "fromAddress": "0x...",
  "toAddress": "N...",
  "proof": "0x..."
}

Response: 202 Accepted
{
  "bridgeId": "bridge_550e8400-e29b-41d4-a716-446655440000",
  "status": "pending",
  "estimatedTime": 600,
  "fee": "10.00",
  "fromTxHash": "0x...",
  "toTxHash": null
}
```

## Request/Response Formats

### Standard Request Headers
```http
Content-Type: application/json
Accept: application/json
Authorization: Bearer {token}
X-Request-ID: {uuid}
X-Client-Version: 1.0.0
```

### Standard Response Format
```json
{
  "success": true,
  "data": {
    // Response data
  },
  "meta": {
    "timestamp": "2024-01-15T10:30:00Z",
    "version": "1.0.0",
    "request_id": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### Pagination
```http
GET /api/resources?page=2&limit=20&sort=createdAt:desc

Response:
{
  "data": [...],
  "pagination": {
    "page": 2,
    "limit": 20,
    "total": 150,
    "pages": 8,
    "hasNext": true,
    "hasPrev": true
  }
}
```

### Filtering
```http
GET /api/resources?filter[status]=active&filter[created_after]=2024-01-01
```

## Error Handling

### Error Response Format
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [
      {
        "field": "email",
        "message": "Invalid email format"
      }
    ],
    "timestamp": "2024-01-15T10:30:00Z",
    "request_id": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `AUTHENTICATION_REQUIRED` | 401 | Missing or invalid authentication |
| `PERMISSION_DENIED` | 403 | Insufficient permissions |
| `RESOURCE_NOT_FOUND` | 404 | Resource does not exist |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `INTERNAL_ERROR` | 500 | Internal server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

## Rate Limiting

### Rate Limit Headers
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642248000
```

### Rate Limits by Tier

| Tier | Requests/Hour | Burst | Compute Units/Day |
|------|---------------|-------|-------------------|
| Free | 100 | 10 | 1,000 |
| Basic | 1,000 | 50 | 10,000 |
| Pro | 10,000 | 200 | 100,000 |
| Enterprise | Unlimited | Custom | Unlimited |

## Webhooks

### Webhook Configuration
```http
POST /webhooks
Authorization: Bearer {token}
Content-Type: application/json

{
  "url": "https://your-app.com/webhook",
  "events": [
    "computation.completed",
    "vote.cast",
    "bridge.completed"
  ],
  "secret": "webhook_secret_key"
}
```

### Webhook Payload
```json
{
  "event": "computation.completed",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "computationId": "comp_550e8400-e29b-41d4-a716-446655440000",
    "status": "completed",
    "result": {...}
  },
  "signature": "sha256=..."
}
```

### Webhook Security
Verify webhook signatures using HMAC-SHA256:
```javascript
const crypto = require('crypto');

function verifyWebhook(payload, signature, secret) {
  const hash = crypto
    .createHmac('sha256', secret)
    .update(payload)
    .digest('hex');
  return `sha256=${hash}` === signature;
}
```

## SDKs and Libraries

### Official SDKs
- **JavaScript/TypeScript**: `npm install @neoservice/sdk`
- **Python**: `pip install neoservice-sdk`
- **Go**: `go get github.com/neoservice/go-sdk`
- **Rust**: `cargo add neoservice-sdk`
- **.NET**: `dotnet add package NeoService.SDK`

### SDK Example (JavaScript)
```javascript
const NeoService = require('@neoservice/sdk');

const client = new NeoService({
  apiKey: 'your-api-key',
  environment: 'production'
});

// Seal data
const sealedData = await client.sgx.seal({
  data: Buffer.from('Hello World'),
  policy: {
    encryptionAlgorithm: 'AES256_GCM'
  }
});

// Submit computation
const computation = await client.compute.submit({
  type: 'WASM',
  code: wasmBuffer,
  inputs: { data: [1, 2, 3] }
});

// Get result
const result = await client.compute.waitForResult(computation.id);
```

## API Versioning

### Version Strategy
- Semantic versioning (MAJOR.MINOR.PATCH)
- Breaking changes increment MAJOR version
- New features increment MINOR version
- Bug fixes increment PATCH version

### Version Header
```http
X-API-Version: 1.0.0
```

### Deprecation Policy
- 6 months notice for breaking changes
- Deprecated endpoints return `Deprecation` header
- Migration guides provided

### Version Negotiation
```http
Accept: application/vnd.neoservice.v1+json
```

## Testing

### Test Environment
- **Base URL**: `https://sandbox.neoservice.io/v1`
- **Test Credentials**: Available in developer portal
- **Rate Limits**: Relaxed for testing

### Postman Collection
Import our Postman collection for easy testing:
```
https://api.neoservice.io/postman/collection.json
```

### cURL Examples
```bash
# Health check
curl https://api.neoservice.io/v1/health

# Authenticated request
curl -H "Authorization: Bearer $TOKEN" \
     https://api.neoservice.io/v1/compute/status/comp_123

# POST request
curl -X POST \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer $TOKEN" \
     -d '{"data": "test"}' \
     https://api.neoservice.io/v1/sgx/seal
```

### Integration Testing
```javascript
// Jest example
describe('Neo Service API', () => {
  test('should authenticate successfully', async () => {
    const response = await fetch('/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: 'test@example.com',
        password: 'TestPassword123!'
      })
    });
    
    expect(response.status).toBe(200);
    const data = await response.json();
    expect(data).toHaveProperty('access_token');
  });
});
```

## Support

- **Documentation**: https://docs.neoservice.io
- **API Status**: https://status.neoservice.io
- **Support Email**: support@neoservice.io
- **Discord**: https://discord.gg/neoservice
- **GitHub**: https://github.com/neoservice