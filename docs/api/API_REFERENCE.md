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

## Error Responses

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

## Common HTTP Status Codes

- `200 OK` - Request successful
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid request parameters
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

## Rate Limiting

API requests are rate limited to prevent abuse:

- **Standard tier**: 100 requests per minute
- **Premium tier**: 1000 requests per minute

Rate limit headers are included in responses:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642248600
```

## Pagination

List endpoints support pagination using query parameters:

```http
GET /api/v1/resource?page=2&pageSize=50
```

Paginated responses include metadata:

```json
{
  "data": [...],
  "pagination": {
    "page": 2,
    "pageSize": 50,
    "totalCount": 387,
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

Retrieves all keys for a blockchain type.

```http
GET /api/v1/keymanagement/keys/{blockchainType}
```

### Parameters

- `blockchainType` (path): Blockchain type (`NeoN3` or `NeoX`)

### Response

```json
{
  "success": true,
  "data": [
    {
      "keyId": "my-key-001",
      "keyType": "Secp256k1",
      "publicKeyHex": "0x03a1b2c3d4e5f6...",
      "address": "NX1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

## Sign Data

Signs data using a specified key.

```http
POST /api/v1/keymanagement/keys/{blockchainType}/{keyId}/sign
```

### Parameters

- `blockchainType` (path): Blockchain type
- `keyId` (path): Key identifier

### Request Body

```json
{
  "data": "0x1234567890abcdef"
}
```

### Response

```json
{
  "success": true,
  "data": {
    "signature": "0x9a8b7c6d5e4f3a2b1c0d...",
    "publicKey": "0x03a1b2c3d4e5f6...",
    "signedAt": "2024-01-15T10:30:00Z"
  }
}
```

---

# Oracle Service API

## Get Price Feed

Retrieves current price data for specified assets.

```http
GET /api/v1/oracle/prices/{assetPair}
```

### Parameters

- `assetPair` (path): Asset pair (e.g., `NEO-USD`, `GAS-USD`)

### Response

```json
{
  "success": true,
  "data": {
    "assetPair": "NEO-USD",
    "price": 12.34,
    "timestamp": "2024-01-15T10:30:00Z",
    "sources": ["binance", "coinbase", "kraken"],
    "aggregationMethod": "median"
  }
}
```

## Submit External Data

Submits external data to the oracle service.

```http
POST /api/v1/oracle/data
```

### Request Body

```json
{
  "dataType": "weather",
  "source": "openweathermap",
  "data": {
    "city": "New York",
    "temperature": 22.5,
    "humidity": 65
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

# Social Recovery API

## Enroll as Guardian

Enrolls a new guardian in the social recovery network.

```http
POST /api/social-recovery/guardians/enroll
```

### Request Body

```json
{
  "address": "0x1234567890abcdef...",
  "stakeAmount": "10000000000",
  "blockchain": "neo-n3"
}
```

### Response

```json
{
  "success": true,
  "guardian": {
    "address": "0x1234567890abcdef...",
    "reputationScore": 100,
    "successfulRecoveries": 0,
    "failedAttempts": 0,
    "stakedAmount": "10000000000",
    "isActive": true,
    "totalEndorsements": 0,
    "trustScore": 25.0
  },
  "message": "Successfully enrolled as guardian"
}
```

## Initiate Recovery

Initiates an account recovery request.

```http
POST /api/social-recovery/recovery/initiate
```

### Request Body

```json
{
  "accountAddress": "0xabc...",
  "newOwner": "0xdef...",
  "strategyId": "standard",
  "isEmergency": false,
  "recoveryFee": "100000000",
  "authFactors": [
    {
      "factorType": "email",
      "factorHash": "hash...",
      "proof": "cHJvb2Y="
    }
  ],
  "blockchain": "neo-n3"
}
```

### Response

```json
{
  "success": true,
  "recoveryRequest": {
    "recoveryId": "0x123...",
    "accountAddress": "0xabc...",
    "newOwner": "0xdef...",
    "strategyId": "standard",
    "requiredConfirmations": 3,
    "currentConfirmations": 1,
    "initiatedAt": "2024-01-15T10:30:00Z",
    "expiresAt": "2024-01-22T10:30:00Z",
    "isEmergency": false,
    "recoveryFee": "100000000",
    "status": "Pending"
  },
  "message": "Recovery initiated successfully"
}
```

## Confirm Recovery

Confirms a recovery request as a guardian.

```http
POST /api/social-recovery/recovery/{recoveryId}/confirm?blockchain=neo-n3
```

### Parameters

- `recoveryId` (path): Recovery request identifier
- `blockchain` (query): Blockchain type (default: neo-n3)

### Response

```json
{
  "success": true,
  "message": "Recovery confirmed successfully"
}
```

## Get Guardian Info

Retrieves information about a guardian.

```http
GET /api/social-recovery/guardians/{address}?blockchain=neo-n3
```

### Parameters

- `address` (path): Guardian address
- `blockchain` (query): Blockchain type

### Response

```json
{
  "address": "0x1234...",
  "reputationScore": 2500,
  "successfulRecoveries": 15,
  "failedAttempts": 1,
  "stakedAmount": "50000000000",
  "isActive": true,
  "totalEndorsements": 25,
  "trustScore": 85.5
}
```

## Get Recovery Info

Retrieves information about a recovery request.

```http
GET /api/social-recovery/recovery/{recoveryId}?blockchain=neo-n3
```

### Response

```json
{
  "recoveryId": "0x123...",
  "accountAddress": "0xabc...",
  "newOwner": "0xdef...",
  "currentConfirmations": 2,
  "requiredConfirmations": 3,
  "expiresAt": "2024-01-22T10:30:00Z",
  "isExecuted": false,
  "isEmergency": false,
  "recoveryFee": "100000000",
  "progress": 66.67
}
```

## Get Recovery Strategies

Retrieves available recovery strategies.

```http
GET /api/social-recovery/strategies?blockchain=neo-n3
```

### Response

```json
[
  {
    "strategyId": "standard",
    "name": "Standard Guardian Recovery",
    "description": "Standard recovery with multiple guardian confirmations and 7-day timeout",
    "minGuardians": 3,
    "timeoutPeriod": "7.00:00:00",
    "requiresReputation": true,
    "minReputationRequired": 100,
    "allowsEmergency": false,
    "requiresAttestation": false
  },
  {
    "strategyId": "emergency",
    "name": "Emergency Recovery",
    "description": "Fast-track recovery for urgent situations",
    "minGuardians": 5,
    "timeoutPeriod": "1.00:00:00",
    "requiresReputation": true,
    "minReputationRequired": 500,
    "allowsEmergency": true,
    "requiresAttestation": false
  }
]
```

## Establish Trust

Establishes a trust relationship with another guardian.

```http
POST /api/social-recovery/trust/establish
```

### Request Body

```json
{
  "trustee": "0x789...",
  "trustLevel": 80,
  "blockchain": "neo-n3"
}
```

### Response

```json
{
  "success": true,
  "message": "Trust established successfully"
}
```

## Add Authentication Factor

Adds a multi-factor authentication factor to an account.

```http
POST /api/social-recovery/auth/factor
```

### Request Body

```json
{
  "factorType": "email",
  "factorHash": "0xabc123...",
  "blockchain": "neo-n3"
}
```

### Response

```json
{
  "success": true,
  "message": "Authentication factor added successfully"
}
```

## Configure Account Recovery

Configures recovery preferences for an account.

```http
POST /api/social-recovery/accounts/{accountAddress}/configure
```

### Request Body

```json
{
  "preferredStrategy": "standard",
  "recoveryThreshold": "3",
  "allowNetworkGuardians": true,
  "minGuardianReputation": "500",
  "blockchain": "neo-n3"
}
```

### Response

```json
{
  "success": true,
  "message": "Recovery configuration updated successfully"
}
```

## Get Network Statistics

Retrieves social recovery network statistics.

```http
GET /api/social-recovery/stats?blockchain=neo-n3
```

### Response

```json
{
  "totalGuardians": 142,
  "totalRecoveries": 1523,
  "successfulRecoveries": 1498,
  "totalStaked": "15420000000000",
  "averageReputationScore": 2834.5,
  "successRate": 98.36
}
```

---

# Pattern Recognition API

## Detect Fraud

Analyzes transaction patterns to detect potential fraud.

```http
POST /api/v1/ai/pattern-recognition/fraud-detection/{blockchainType}
```

### Parameters

- `blockchainType` (path): Blockchain type

### Request Body

```json
{
  "transactionData": {
    "fromAddress": "NX...",
    "toAddress": "NX...",
    "amount": 1000,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "historicalTransactions": [...]
}
```

### Response

```json
{
  "success": true,
  "data": {
    "isFraudulent": false,
    "riskScore": 0.15,
    "patterns": ["normal_transfer"],
    "recommendations": []
  }
}
```

---

# Prediction Service API

## Create Prediction

Generates predictions based on historical data.

```http
POST /api/v1/ai/prediction/{blockchainType}
```

### Request Body

```json
{
  "predictionType": "price",
  "asset": "NEO",
  "timeframe": "24h",
  "historicalData": [...]
}
```

### Response

```json
{
  "success": true,
  "data": {
    "prediction": 15.67,
    "confidence": 0.85,
    "range": {
      "min": 14.50,
      "max": 16.90
    }
  }
}
```

---

# WebSocket API

## Connection

```javascript
const ws = new WebSocket('wss://api.neo-service-layer.com/ws');

ws.on('open', () => {
  // Authenticate
  ws.send(JSON.stringify({
    type: 'auth',
    token: 'your-jwt-token'
  }));
  
  // Subscribe to events
  ws.send(JSON.stringify({
    type: 'subscribe',
    channels: ['prices', 'fraud-alerts']
  }));
});

ws.on('message', (data) => {
  const message = JSON.parse(data);
  console.log('Received:', message);
});
```

## Event Types

- `price-update` - Real-time price updates
- `fraud-alert` - Fraud detection alerts
- `key-event` - Key management events
- `system-status` - System health updates

---

# SDK Usage

## JavaScript/TypeScript

```bash
npm install @neo-service-layer/sdk
```

```typescript
import { NeoServiceLayerClient } from '@neo-service-layer/sdk';

const client = new NeoServiceLayerClient({
  apiKey: 'your-api-key',
  baseUrl: 'https://api.neo-service-layer.com'
});

// Generate key
const key = await client.keyManagement.generateKey('NeoN3', {
  keyId: 'my-key',
  keyType: 'Secp256k1'
});

// Get oracle data
const price = await client.oracle.getPrice('NEO-USD');

// Detect fraud
const fraudResult = await client.patternRecognition.detectFraud('NeoN3', {
  transactionData: {
    amount: 1000,
    fromAddress: 'NX...',
    toAddress: 'NX...'
  }
});

// Social Recovery operations
const guardian = await client.socialRecovery.enrollGuardian({
  address: '0x123...',
  stakeAmount: '10000000000',
  blockchain: 'neo-n3'
});

const recovery = await client.socialRecovery.initiateRecovery({
  accountAddress: '0xabc...',
  newOwner: '0xdef...',
  strategyId: 'standard',
  recoveryFee: '100000000'
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
- `guardian.enrolled` - New guardian enrolled
- `recovery.initiated` - Recovery request initiated
- `recovery.confirmed` - Recovery confirmation received
- `recovery.executed` - Recovery successfully executed

## Webhook Configuration

Configure webhooks in your application settings:

```json
{
  "webhooks": {
    "url": "https://your-app.com/webhooks/neo-service-layer",
    "secret": "your-webhook-secret",
    "events": ["fraud.detected", "system.alert", "recovery.executed"]
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