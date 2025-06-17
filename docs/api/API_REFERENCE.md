# Neo Service Layer - API Reference

## Overview

The Neo Service Layer provides a comprehensive REST API for blockchain operations, AI services, key management, and advanced features. This document provides detailed information about all available endpoints, request/response formats, and usage examples.

## Base URL

```
Production: https://api.neo-service-layer.com
Development: http://localhost:5000
```

## Authentication

All API endpoints require JWT Bearer token authentication unless otherwise specified.

```http
Authorization: Bearer <your-jwt-token>
```

### Obtaining a Token

```http
POST /auth/login
Content-Type: application/json

{
  "username": "your-username",
  "password": "your-password"
}
```

## API Versioning

The API uses URL versioning with the format `/api/v{version}/`. Current version is `v1`.

## Response Format

All API responses follow a consistent format:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { /* response data */ },
  "timestamp": "2024-01-15T10:30:00Z",
  "requestId": "req-123456"
}
```

### Error Response Format

```json
{
  "success": false,
  "message": "Error description",
  "error": {
    "code": "ERROR_CODE",
    "details": "Detailed error information"
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "requestId": "req-123456"
}
```

## Rate Limiting

API endpoints are rate-limited to ensure fair usage:

- **General endpoints**: 1000 requests per minute
- **Key Management**: 100 requests per minute
- **AI Services**: 200 requests per minute

Rate limit headers are included in responses:

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642248600
```

## Pagination

List endpoints support pagination with the following parameters:

- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

Paginated responses include metadata:

```json
{
  "success": true,
  "data": [/* items */],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

---

# Key Management API

## Generate Key

Creates a new cryptographic key for the specified blockchain.

```http
POST /api/v1/keymanagement/keys/{blockchainType}
```

### Parameters

- `blockchainType` (path): Blockchain type (`NeoN3` or `NeoX`)

### Request Body

```json
{
  "keyId": "my-key-001",
  "keyType": "Secp256k1",
  "keyUsage": "Sign,Verify",
  "exportable": false,
  "description": "Key for transaction signing"
}
```

### Response

```json
{
  "success": true,
  "data": {
    "keyId": "my-key-001",
    "keyType": "Secp256k1",
    "keyUsage": "Sign,Verify",
    "publicKeyHex": "0x03a1b2c3d4e5f6...",
    "address": "NX1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0",
    "exportable": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "description": "Key for transaction signing"
  }
}
```

## List Keys

Retrieves a paginated list of keys for the specified blockchain.

```http
GET /api/v1/keymanagement/keys/{blockchainType}?page=1&pageSize=20
```

### Query Parameters

- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20, max: 100)
- `keyType` (optional): Filter by key type
- `keyUsage` (optional): Filter by key usage

### Response

```json
{
  "success": true,
  "data": [
    {
      "keyId": "my-key-001",
      "keyType": "Secp256k1",
      "keyUsage": "Sign,Verify",
      "publicKeyHex": "0x03a1b2c3d4e5f6...",
      "address": "NX1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 5,
    "totalPages": 1
  }
}
```

## Get Key

Retrieves details of a specific key.

```http
GET /api/v1/keymanagement/keys/{keyId}/{blockchainType}
```

## Sign Data

Signs data using the specified key.

```http
POST /api/v1/keymanagement/keys/{keyId}/sign/{blockchainType}
```

### Request Body

```json
{
  "data": "0x1234567890abcdef",
  "algorithm": "ECDSA"
}
```

### Response

```json
{
  "success": true,
  "data": {
    "signature": "0xabcdef1234567890...",
    "algorithm": "ECDSA",
    "keyId": "my-key-001",
    "signedAt": "2024-01-15T10:30:00Z"
  }
}
```

---

# Pattern Recognition API

## Fraud Detection

Analyzes transaction data for potential fraud patterns.

```http
POST /api/v1/patternrecognition/fraud-detection/{blockchainType}
```

### Request Body

```json
{
  "transactionData": {
    "amount": 1000,
    "fromAddress": "NX1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0",
    "toAddress": "NX9z8y7x6w5v4u3t2s1r0q9p8o7n6m5l4k3j2i1h0",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "sensitivity": "Standard",
  "includeHistoricalData": true
}
```

### Response

```json
{
  "success": true,
  "data": {
    "detectionId": "fraud-det-001",
    "riskScore": 0.75,
    "isFraudulent": true,
    "confidence": 0.85,
    "detectedPatterns": [
      {
        "patternId": "unusual-amount",
        "name": "Unusual Transaction Amount",
        "severity": "High",
        "description": "Transaction amount significantly higher than user's typical pattern"
      }
    ],
    "detectedAt": "2024-01-15T10:30:00Z"
  }
}
```

## Pattern Analysis

Performs general pattern analysis on provided data.

```http
POST /api/v1/patternrecognition/pattern-analysis/{blockchainType}
```

### Request Body

```json
{
  "data": {
    "transactions": [
      {"amount": 100, "timestamp": "2024-01-15T10:00:00Z"},
      {"amount": 200, "timestamp": "2024-01-15T10:15:00Z"}
    ]
  },
  "analysisType": "General",
  "minimumConfidence": 0.7
}
```

## Behavior Analysis

Analyzes user behavior patterns for anomaly detection.

```http
POST /api/v1/patternrecognition/behavior-analysis/{blockchainType}
```

---

# Prediction API

## Make Prediction

Generates predictions using AI models.

```http
POST /api/v1/prediction/predict/{blockchainType}
```

### Request Body

```json
{
  "modelId": "price-prediction-v1",
  "inputData": {
    "price": 100.50,
    "volume": 1000000,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "parameters": {
    "horizon": 24,
    "confidence_level": 0.95
  }
}
```

### Response

```json
{
  "success": true,
  "data": {
    "predictionId": "pred-001",
    "modelId": "price-prediction-v1",
    "predictedValue": 105.75,
    "confidence": 0.87,
    "predictionInterval": {
      "lower": 98.20,
      "upper": 113.30
    },
    "predictedAt": "2024-01-15T10:30:00Z",
    "metadata": {
      "model_version": "1.2.3",
      "features_used": ["price", "volume", "timestamp"]
    }
  }
}
```

## Sentiment Analysis

Analyzes sentiment of text data.

```http
POST /api/v1/prediction/sentiment-analysis/{blockchainType}
```

### Request Body

```json
{
  "text": "Neo blockchain is showing great potential for the future!",
  "source": "social_media",
  "language": "en",
  "context": {
    "platform": "twitter",
    "user_followers": 1000
  }
}
```

### Response

```json
{
  "success": true,
  "data": {
    "sentiment": {
      "overall": "Positive",
      "positive": 0.85,
      "negative": 0.10,
      "neutral": 0.05,
      "compound": 0.75
    },
    "confidence": 0.92,
    "emotions": {
      "joy": 0.8,
      "trust": 0.7,
      "anticipation": 0.6
    },
    "keyPhrases": ["Neo blockchain", "great potential", "future"],
    "analyzedAt": "2024-01-15T10:30:00Z"
  }
}
```

## Market Forecast

Generates market forecasts for specified assets.

```http
POST /api/v1/prediction/market-forecast/{blockchainType}
```

---

# Health and Monitoring

## Health Check

Returns the overall health status of the service.

```http
GET /health
```

### Response

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "Database is accessible",
      "duration": "00:00:00.0123456"
    },
    "redis": {
      "status": "Healthy",
      "description": "Redis is accessible",
      "duration": "00:00:00.0098765"
    },
    "blockchain": {
      "status": "Healthy",
      "description": "Blockchain connectivity: Neo N3: Block height 1234567; Neo X: Block height 987654",
      "duration": "00:00:00.0567890"
    }
  }
}
```

## Ready Check

Returns readiness status for load balancer health checks.

```http
GET /health/ready
```

## Live Check

Returns liveness status for container orchestration.

```http
GET /health/live
```

---

# Error Codes

## Common Error Codes

| Code | Description |
|------|-------------|
| `INVALID_REQUEST` | Request validation failed |
| `UNAUTHORIZED` | Authentication required |
| `FORBIDDEN` | Insufficient permissions |
| `NOT_FOUND` | Resource not found |
| `RATE_LIMITED` | Rate limit exceeded |
| `INTERNAL_ERROR` | Internal server error |

## Service-Specific Error Codes

### Key Management

| Code | Description |
|------|-------------|
| `KEY_NOT_FOUND` | Specified key does not exist |
| `KEY_ALREADY_EXISTS` | Key with the same ID already exists |
| `INVALID_KEY_TYPE` | Unsupported key type |
| `SIGNING_FAILED` | Key signing operation failed |
| `KEY_EXPORT_DENIED` | Key is not exportable |

### Pattern Recognition

| Code | Description |
|------|-------------|
| `MODEL_NOT_FOUND` | AI model not found |
| `INSUFFICIENT_DATA` | Not enough data for analysis |
| `ANALYSIS_FAILED` | Pattern analysis failed |
| `INVALID_SENSITIVITY` | Invalid sensitivity level |

### Prediction

| Code | Description |
|------|-------------|
| `PREDICTION_FAILED` | Prediction generation failed |
| `MODEL_UNAVAILABLE` | AI model is not available |
| `INVALID_INPUT_DATA` | Input data format is invalid |
| `CONFIDENCE_TOO_LOW` | Prediction confidence below threshold |

---

# SDK and Client Libraries

## Official SDKs

- **JavaScript/TypeScript**: `@neo-service-layer/js-sdk`
- **Python**: `neo-service-layer-python`
- **C#**: `NeoServiceLayer.Client`
- **Go**: `github.com/neo-service-layer/go-client`

## Example Usage (JavaScript)

```javascript
import { NeoServiceLayerClient } from '@neo-service-layer/js-sdk';

const client = new NeoServiceLayerClient({
  baseUrl: 'https://api.neo-service-layer.com',
  apiKey: 'your-api-key'
});

// Generate a key
const key = await client.keyManagement.generateKey('NeoN3', {
  keyId: 'my-key-001',
  keyType: 'Secp256k1',
  keyUsage: 'Sign,Verify'
});

// Detect fraud
const fraudResult = await client.patternRecognition.detectFraud('NeoN3', {
  transactionData: {
    amount: 1000,
    fromAddress: 'NX...',
    toAddress: 'NX...'
  }
});
```

---

# Webhooks

## Webhook Events

The service can send webhook notifications for various events:

- `key.generated` - New key created
- `fraud.detected` - Fraud pattern detected
- `prediction.completed` - Prediction generated
- `system.alert` - System alert triggered

## Webhook Configuration

Configure webhooks in your application settings:

```json
{
  "webhooks": {
    "url": "https://your-app.com/webhooks/neo-service-layer",
    "secret": "your-webhook-secret",
    "events": ["fraud.detected", "system.alert"]
  }
}
```

## Webhook Payload

```json
{
  "event": "fraud.detected",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "detectionId": "fraud-det-001",
    "riskScore": 0.95,
    "transactionId": "tx-123456"
  },
  "signature": "sha256=..."
}
```

---

# Best Practices

## Security

1. **Always use HTTPS** in production
2. **Rotate API keys** regularly
3. **Implement proper rate limiting** on your client
4. **Validate webhook signatures** to ensure authenticity
5. **Use least privilege** principle for API access

## Performance

1. **Implement caching** for frequently accessed data
2. **Use pagination** for large result sets
3. **Batch operations** when possible
4. **Monitor rate limits** and implement backoff strategies
5. **Use appropriate timeouts** for API calls

## Error Handling

1. **Implement retry logic** with exponential backoff
2. **Handle rate limiting** gracefully
3. **Log errors** with sufficient context
4. **Provide meaningful error messages** to users
5. **Monitor error rates** and set up alerts

---

# Support

## Documentation

- **API Reference**: This document
- **Developer Guide**: `/docs/development/`
- **Architecture Overview**: `/docs/architecture/`
- **Deployment Guide**: `/docs/deployment/`

## Community

- **GitHub**: https://github.com/neo-service-layer
- **Discord**: https://discord.gg/neo-service-layer
- **Stack Overflow**: Tag `neo-service-layer`

## Enterprise Support

For enterprise support, contact: enterprise@neo-service-layer.com 