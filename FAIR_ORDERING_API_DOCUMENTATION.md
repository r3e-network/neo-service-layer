# Fair Ordering Service API Documentation

## Overview

The Fair Ordering Service provides REST endpoints for MEV protection, fair transaction ordering, and risk analysis. All operations are secured with enclave integration to ensure privacy and protection against front-running and sandwich attacks.

**Base URL**: `/api/v1/fair-ordering`  
**Authentication**: Bearer token required  
**Content-Type**: `application/json`

## Table of Contents

1. [Authentication & Authorization](#authentication--authorization)
2. [Ordering Pool Management](#ordering-pool-management)
3. [Transaction Submission](#transaction-submission)
4. [Risk Analysis](#risk-analysis)
5. [Metrics & Monitoring](#metrics--monitoring)
6. [Error Handling](#error-handling)
7. [Rate Limiting](#rate-limiting)
8. [Examples](#examples)

---

## Authentication & Authorization

### Required Headers
```http
Authorization: Bearer <jwt-token>
Content-Type: application/json
```

### Role-Based Access Control

| Role | Permissions |
|------|-------------|
| `Admin` | Full access to all endpoints |
| `PoolManager` | Create and update ordering pools |
| `ServiceUser` | Submit transactions, view metrics |
| `Trader` | Submit transactions, analyze risks |
| `Analyst` | View metrics, analyze risks |
| `Monitor` | View health status only |

---

## Ordering Pool Management

### Create Ordering Pool

Creates a new ordering pool for fair transaction processing.

**Endpoint**: `POST /pools/{blockchainType}`

**Parameters**:
- `blockchainType` (path): `NeoN3` or `NeoX`

**Request Body**:
```json
{
  "name": "High Performance Pool",
  "description": "Pool optimized for high-frequency trading",
  "orderingAlgorithm": "PriorityFair",
  "batchSize": 100,
  "batchTimeout": "00:00:05",
  "fairnessLevel": "High",
  "mevProtectionEnabled": true,
  "maxSlippage": 0.01,
  "parameters": {
    "priority_fee_threshold": 0.001,
    "front_running_protection": true,
    "sandwich_attack_prevention": true
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": "pool-uuid-here",
  "message": "Ordering pool created successfully",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `PoolManager`

---

### Update Pool Configuration

Updates the configuration of an existing ordering pool.

**Endpoint**: `PUT /pools/{poolId}/{blockchainType}`

**Parameters**:
- `poolId` (path): The pool identifier
- `blockchainType` (path): `NeoN3` or `NeoX`

**Request Body**: Same as create pool

**Response**:
```json
{
  "success": true,
  "data": true,
  "message": "Pool configuration updated successfully",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `PoolManager`

---

### List Ordering Pools

Retrieves all active ordering pools.

**Endpoint**: `GET /pools/{blockchainType}`

**Parameters**:
- `blockchainType` (path): `NeoN3` or `NeoX`

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "pool-uuid",
      "name": "Standard Fair Pool",
      "orderingAlgorithm": "FairQueue",
      "batchSize": 100,
      "fairnessLevel": "Standard",
      "mevProtectionEnabled": true,
      "status": "Active",
      "createdAt": "2025-06-18T09:00:00Z",
      "pendingTransactionCount": 5,
      "processedBatchCount": 127
    }
  ],
  "message": "Ordering pools retrieved successfully",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Analyst`

---

## Transaction Submission

### Submit Fair Transaction

Submits a transaction for fair ordering protection.

**Endpoint**: `POST /transactions/{blockchainType}`

**Parameters**:
- `blockchainType` (path): `NeoN3` or `NeoX`

**Request Body**:
```json
{
  "from": "0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E",
  "to": "0x1234567890abcdef1234567890abcdef12345678",
  "value": 50000.0,
  "data": "0xa9059cbb...",
  "gasLimit": 100000,
  "protectionLevel": "High",
  "maxSlippage": 0.005,
  "executeAfter": "2025-06-18T10:05:00Z",
  "executeBefore": "2025-06-18T10:15:00Z"
}
```

**Response**:
```json
{
  "success": true,
  "data": "transaction-uuid",
  "message": "Transaction submitted for fair ordering",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Trader`

---

### Submit to Ordering Pool

Submits a transaction directly to a specific ordering pool.

**Endpoint**: `POST /submit/{blockchainType}`

**Parameters**:
- `blockchainType` (path): `NeoN3` or `NeoX`

**Request Body**:
```json
{
  "from": "0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E",
  "to": "0x1234567890abcdef1234567890abcdef12345678",
  "value": 1000.0,
  "transactionData": "0xa9059cbb...",
  "gasPrice": 20000000000,
  "gasLimit": 100000,
  "priorityFee": 0.001
}
```

**Response**:
```json
{
  "success": true,
  "data": "submission-uuid",
  "message": "Transaction submitted to ordering pool",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Trader`

---

### Get Ordering Result

Retrieves the ordering result for a specific transaction.

**Endpoint**: `GET /transactions/{transactionId}/result/{blockchainType}`

**Parameters**:
- `transactionId` (path): The transaction identifier
- `blockchainType` (path): `NeoN3` or `NeoX`

**Response**:
```json
{
  "success": true,
  "data": {
    "transactionId": "transaction-uuid",
    "poolId": "pool-uuid",
    "batchId": "batch-uuid",
    "originalPosition": 3,
    "finalPosition": 1,
    "orderingAlgorithm": "PriorityFair",
    "fairnessScore": 0.95,
    "mevProtectionScore": 0.98,
    "success": true,
    "processedAt": "2025-06-18T10:00:30Z"
  },
  "message": "Ordering result retrieved successfully",
  "timestamp": "2025-06-18T10:01:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Trader`

---

## Risk Analysis

### Analyze Fairness Risk

Analyzes the fairness risk of a transaction before submission.

**Endpoint**: `POST /analyze/{blockchainType}`

**Parameters**:
- `blockchainType` (path): `NeoN3` or `NeoX`

**Request Body**:
```json
{
  "from": "0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E",
  "to": "0x1234567890abcdef1234567890abcdef12345678",
  "value": 1000000.0,
  "transactionData": "0xa9059cbb...",
  "gasPrice": 50000000000,
  "gasLimit": 300000,
  "timestamp": "2025-06-18T10:00:00Z",
  "context": {
    "GasPrice": "50000000000",
    "Priority": "High"
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "transactionHash": "0xabc123...",
    "riskLevel": "High",
    "estimatedMEV": 1000.5,
    "detectedRisks": [
      "Large transaction value detected",
      "High gas price detected - potential front-running target"
    ],
    "recommendations": [
      "Consider splitting large transactions",
      "Use fair ordering protection to prevent front-running"
    ],
    "protectionFee": 15.75,
    "analyzedAt": "2025-06-18T10:00:00Z"
  },
  "message": "Fairness analysis completed",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Trader`, `Analyst`

---

### Analyze MEV Risk

Performs detailed MEV risk analysis for a transaction.

**Endpoint**: `POST /mev-analysis/{blockchainType}`

**Parameters**:
- `blockchainType` (path): `NeoN3` or `NeoX`

**Request Body**:
```json
{
  "transactionHash": "0xabc123...",
  "transaction": {
    "id": "tx-uuid",
    "from": "0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E",
    "to": "0x1234567890abcdef1234567890abcdef12345678",
    "value": 100000.0,
    "gasPrice": 25000000000,
    "gasLimit": 200000
  },
  "poolContext": [
    {
      "id": "tx1",
      "value": 50000.0,
      "gasPrice": 30000000000
    }
  ],
  "depth": "Standard",
  "parameters": {
    "include_arbitrage": true,
    "include_sandwich": true
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "analysisId": "analysis-uuid",
    "transactionHash": "0xabc123...",
    "mevRiskScore": 0.75,
    "riskLevel": "High",
    "protectionLevel": "Standard",
    "detectedThreats": [
      "High gas price - front-running target",
      "Pool activity - increased MEV competition"
    ],
    "protectionStrategies": [
      "Use fair ordering protection",
      "Add random delay"
    ],
    "detectedOpportunities": [
      {
        "type": "FrontRunning",
        "potentialProfit": 150.25,
        "riskLevel": 0.8,
        "confidence": 0.95
      }
    ],
    "analyzedAt": "2025-06-18T10:00:00Z"
  },
  "message": "MEV analysis completed",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Trader`, `Analyst`

---

## Metrics & Monitoring

### Get Fairness Metrics

Retrieves fairness metrics for an ordering pool.

**Endpoint**: `GET /pools/{poolId}/metrics/{blockchainType}`

**Parameters**:
- `poolId` (path): The pool identifier
- `blockchainType` (path): `NeoN3` or `NeoX`

**Response**:
```json
{
  "success": true,
  "data": {
    "poolId": "pool-uuid",
    "totalTransactionsProcessed": 5420,
    "averageProcessingTime": "00:00:02.5",
    "fairnessScore": 0.94,
    "mevProtectionEffectiveness": 0.97,
    "orderingAlgorithmEfficiency": 0.89,
    "metricsGeneratedAt": "2025-06-18T10:00:00Z"
  },
  "message": "Fairness metrics retrieved successfully",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Analyst`

---

### Get Health Status

Retrieves health status of the fair ordering service.

**Endpoint**: `GET /health`

**Response**:
```json
{
  "success": true,
  "data": {
    "status": "Healthy",
    "isRunning": true,
    "serviceName": "FairOrdering",
    "version": "1.0.0",
    "checkedAt": "2025-06-18T10:00:00Z"
  },
  "message": "Health status retrieved successfully",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

**Required Roles**: `Admin`, `ServiceUser`, `Monitor`

---

## Error Handling

### Standard Error Response

All API endpoints return errors in a consistent format:

```json
{
  "success": false,
  "message": "Error description",
  "data": null,
  "timestamp": "2025-06-18T10:00:00Z",
  "errors": {
    "field": ["Error message"]
  }
}
```

### HTTP Status Codes

| Code | Description | When Used |
|------|-------------|-----------|
| 200 | OK | Successful operation |
| 400 | Bad Request | Invalid request parameters |
| 401 | Unauthorized | Missing or invalid authentication |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server-side error |

### Common Error Scenarios

#### Invalid Blockchain Type
```json
{
  "success": false,
  "message": "Invalid blockchain type: InvalidChain",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

#### Pool Not Found
```json
{
  "success": false,
  "message": "Pool not found: invalid-pool-id",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

#### Circuit Breaker Open
```json
{
  "success": false,
  "message": "Circuit breaker is open for SubmitFairTransaction_NeoX",
  "timestamp": "2025-06-18T10:00:00Z"
}
```

---

## Rate Limiting

### Limits by Role

| Role | Requests per Minute | Burst Limit |
|------|-------------------|-------------|
| Admin | Unlimited | Unlimited |
| PoolManager | 100 | 150 |
| ServiceUser | 60 | 100 |
| Trader | 120 | 200 |
| Analyst | 60 | 100 |
| Monitor | 30 | 50 |

### Rate Limit Headers

```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1634567890
```

---

## Examples

### Complete Transaction Flow

1. **Analyze Risk**:
```bash
curl -X POST "https://api.neoservice.com/api/v1/fair-ordering/analyze/NeoX" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "from": "0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E",
    "to": "0x1234567890abcdef1234567890abcdef12345678",
    "value": 50000,
    "transactionData": "0xa9059cbb...",
    "gasPrice": 25000000000,
    "gasLimit": 100000
  }'
```

2. **Submit Transaction**:
```bash
curl -X POST "https://api.neoservice.com/api/v1/fair-ordering/transactions/NeoX" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "from": "0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E",
    "to": "0x1234567890abcdef1234567890abcdef12345678",
    "value": 50000,
    "data": "0xa9059cbb...",
    "gasLimit": 100000,
    "protectionLevel": "High"
  }'
```

3. **Check Result**:
```bash
curl -X GET "https://api.neoservice.com/api/v1/fair-ordering/transactions/{transactionId}/result/NeoX" \
  -H "Authorization: Bearer <token>"
```

### Pool Management Flow

1. **Create Pool**:
```bash
curl -X POST "https://api.neoservice.com/api/v1/fair-ordering/pools/NeoX" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "DeFi Optimized Pool",
    "orderingAlgorithm": "MevResistant",
    "batchSize": 50,
    "fairnessLevel": "Maximum",
    "mevProtectionEnabled": true
  }'
```

2. **Get Metrics**:
```bash
curl -X GET "https://api.neoservice.com/api/v1/fair-ordering/pools/{poolId}/metrics/NeoX" \
  -H "Authorization: Bearer <token>"
```

---

## SDK Integration

For easier integration, consider using our official SDKs:

- **JavaScript/TypeScript**: `npm install @neo-service-layer/fair-ordering-sdk`
- **Python**: `pip install neo-service-layer-fair-ordering`
- **C#**: `dotnet add package NeoServiceLayer.FairOrdering.Client`

### JavaScript Example

```javascript
import { FairOrderingClient } from '@neo-service-layer/fair-ordering-sdk';

const client = new FairOrderingClient({
  baseUrl: 'https://api.neoservice.com',
  apiKey: 'your-api-key'
});

// Analyze risk
const riskAnalysis = await client.analyzeFairnessRisk({
  from: '0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E',
  to: '0x1234567890abcdef1234567890abcdef12345678',
  value: 50000,
  blockchainType: 'NeoX'
});

// Submit transaction
const transactionId = await client.submitFairTransaction({
  from: '0x742D35Cc6634C0532925A3b8D4E6E497C8c9CD7E',
  to: '0x1234567890abcdef1234567890abcdef12345678',
  value: 50000,
  protectionLevel: 'High',
  blockchainType: 'NeoX'
});
```

---

## Support

For technical support and questions:

- **Documentation**: [https://docs.neoservice.com/fair-ordering](https://docs.neoservice.com/fair-ordering)
- **API Status**: [https://status.neoservice.com](https://status.neoservice.com)
- **Support Email**: support@neoservice.com
- **GitHub Issues**: [https://github.com/neoservice/fair-ordering/issues](https://github.com/neoservice/fair-ordering/issues)

---

## Changelog

### Version 1.0.0 (2025-06-18)
- Initial API release
- Core fair ordering functionality
- MEV protection and risk analysis
- Pool management capabilities
- Comprehensive resilience patterns