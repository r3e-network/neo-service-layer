# Smart Contracts API Reference

This document provides comprehensive documentation for the Neo Service Layer Smart Contracts API, supporting both Neo N3 and Neo X networks.

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Neo N3 Smart Contracts](#neo-n3-smart-contracts)
4. [Neo X Smart Contracts](#neo-x-smart-contracts)
5. [Cross-Chain Operations](#cross-chain-operations)
6. [Error Handling](#error-handling)
7. [Rate Limiting](#rate-limiting)
8. [SDK Usage](#sdk-usage)

## Overview

The Smart Contracts API provides secure, enclave-protected access to smart contract operations across Neo N3 and Neo X networks. All contract interactions are executed within Intel SGX enclaves for maximum security.

### Base URL
```
https://api.neoservicelayer.io/api/v1/smart-contracts
```

### Supported Networks
- **Neo N3**: Native Neo blockchain (C# smart contracts)
- **Neo X**: EVM-compatible Neo sidechain (Solidity smart contracts)

## Authentication

All endpoints require JWT bearer token authentication with appropriate role-based permissions.

```http
Authorization: Bearer <jwt-token>
```

### Required Roles
- **Admin**: Full access to all operations
- **KeyManager**: Deploy and manage contracts
- **KeyUser**: Invoke and call contract methods
- **ServiceUser**: Read-only access to contract information

## Neo N3 Smart Contracts

### Deploy Contract

Deploys a smart contract to the Neo N3 network.

```http
POST /neo-n3/deploy
```

**Headers:**
```http
Authorization: Bearer <jwt-token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "contractCode": "base64-encoded-nef-and-manifest",
  "constructorParameters": [
    "parameter1",
    "parameter2"
  ],
  "name": "MyToken",
  "version": "1.0.0",
  "author": "Developer Name",
  "description": "My NEP-17 token contract",
  "gasLimit": 10000000
}
```

**Response:**
```json
{
  "contractHash": "0x1234567890abcdef1234567890abcdef12345678",
  "transactionHash": "0xabcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
  "blockNumber": 1234567,
  "gasConsumed": 8500000,
  "isSuccess": true,
  "contractManifest": "{\"name\":\"MyToken\",\"groups\":[],\"features\":{},\"supportedStandards\":[\"NEP-17\"],...}"
}
```

### Invoke Contract Method

Executes a state-changing method on a Neo N3 contract.

```http
POST /neo-n3/{contractHash}/invoke
```

**Path Parameters:**
- `contractHash` (string): The contract script hash

**Request Body:**
```json
{
  "method": "transfer",
  "parameters": [
    "NPTmAHDxAz6Ky4yt6AeKzaw2oYGxbtrhhB",
    "NTmpbW7Vt8i7LvS4cmx1SJLFLXmkFHyAGm",
    100000000
  ],
  "gasLimit": 1000000,
  "value": 0,
  "waitForConfirmation": true
}
```

**Response:**
```json
{
  "transactionHash": "0xdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abc",
  "blockNumber": 1234568,
  "gasConsumed": 900000,
  "returnValue": true,
  "isSuccess": true,
  "executionState": "HALT",
  "events": [
    {
      "name": "Transfer",
      "contractHash": "0x1234567890abcdef1234567890abcdef12345678",
      "parameters": [
        "NPTmAHDxAz6Ky4yt6AeKzaw2oYGxbtrhhB",
        "NTmpbW7Vt8i7LvS4cmx1SJLFLXmkFHyAGm",
        100000000
      ],
      "blockNumber": 1234568,
      "transactionHash": "0xdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abc"
    }
  ]
}
```

### Call Contract Method (Read-Only)

Executes a read-only method on a Neo N3 contract without creating a transaction.

```http
GET /neo-n3/{contractHash}/call/{method}?parameters=[\"param1\",\"param2\"]
```

**Path Parameters:**
- `contractHash` (string): The contract script hash
- `method` (string): The method name to call

**Query Parameters:**
- `parameters` (string, optional): JSON array of method parameters

**Response:**
```json
{
  "result": "MyToken"
}
```

### Get Contract Metadata

Retrieves metadata information about a Neo N3 contract.

```http
GET /neo-n3/{contractHash}/metadata
```

**Response:**
```json
{
  "contractHash": "0x1234567890abcdef1234567890abcdef12345678",
  "name": "MyToken",
  "version": "1.0.0",
  "author": "Developer Name",
  "description": "My NEP-17 token contract",
  "manifest": "{...}",
  "deployedBlockNumber": 1234567,
  "deploymentTxHash": "0xabcdef...",
  "deployedAt": "2024-01-15T10:30:00Z",
  "methods": [
    {
      "name": "symbol",
      "parameters": [],
      "returnType": "String",
      "isSafe": true,
      "isPayable": false
    },
    {
      "name": "transfer",
      "parameters": [
        {
          "name": "from",
          "type": "Hash160"
        },
        {
          "name": "to",
          "type": "Hash160"
        },
        {
          "name": "amount",
          "type": "Integer"
        }
      ],
      "returnType": "Boolean",
      "isSafe": false,
      "isPayable": false
    }
  ],
  "isActive": true
}
```

### List Deployed Contracts

Lists all contracts deployed by the current account on Neo N3.

```http
GET /neo-n3/contracts
```

**Response:**
```json
{
  "contracts": [
    {
      "contractHash": "0x1234567890abcdef1234567890abcdef12345678",
      "name": "MyToken",
      "version": "1.0.0",
      "deployedAt": "2024-01-15T10:30:00Z",
      "isActive": true
    }
  ]
}
```

### Get Contract Events

Retrieves events emitted by a Neo N3 contract within a block range.

```http
GET /neo-n3/{contractHash}/events?eventName=Transfer&fromBlock=1234000&toBlock=1235000
```

**Query Parameters:**
- `eventName` (string, optional): Filter by specific event name
- `fromBlock` (integer, optional): Starting block number
- `toBlock` (integer, optional): Ending block number

**Response:**
```json
{
  "events": [
    {
      "name": "Transfer",
      "contractHash": "0x1234567890abcdef1234567890abcdef12345678",
      "parameters": [
        "NPTmAHDxAz6Ky4yt6AeKzaw2oYGxbtrhhB",
        "NTmpbW7Vt8i7LvS4cmx1SJLFLXmkFHyAGm",
        100000000
      ],
      "blockNumber": 1234568,
      "transactionHash": "0xdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abc"
    }
  ]
}
```

### Estimate Gas

Estimates the gas cost for invoking a Neo N3 contract method.

```http
GET /neo-n3/{contractHash}/estimate-gas/{method}?parameters=[\"param1\",\"param2\"]
```

**Response:**
```json
{
  "gasEstimate": 900000
}
```

## Neo X Smart Contracts

### Deploy Contract

Deploys a smart contract to the Neo X (EVM) network.

```http
POST /neo-x/deploy
```

**Request Body:**
```json
{
  "contractCode": "base64-encoded-bytecode-and-abi",
  "constructorParameters": [
    "Initial Supply",
    "TOKEN",
    18
  ],
  "name": "MyERC20Token",
  "version": "1.0.0",
  "author": "Developer Name",
  "description": "My ERC20 token contract",
  "gasLimit": 2000000
}
```

**Response:**
```json
{
  "contractHash": "0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234",
  "transactionHash": "0x9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
  "blockNumber": 123456,
  "gasConsumed": 1800000,
  "isSuccess": true
}
```

### Invoke Contract Method

Executes a method on a Neo X contract.

```http
POST /neo-x/{contractAddress}/invoke
```

**Request Body:**
```json
{
  "method": "transfer",
  "parameters": [
    "0x8ba1f109551bD432803012645Hac136c30F0B97c",
    1000000000000000000
  ],
  "gasLimit": 100000,
  "value": 0,
  "waitForConfirmation": true
}
```

**Response:**
```json
{
  "transactionHash": "0x2f8a1234567890abcdef1234567890abcdef1234567890abcdef1234567890def",
  "blockNumber": 123457,
  "gasConsumed": 65000,
  "returnValue": true,
  "isSuccess": true,
  "executionState": "SUCCESS",
  "events": [
    {
      "name": "Transfer",
      "contractHash": "0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234",
      "parameters": [
        "0xfrom_address",
        "0x8ba1f109551bD432803012645Hac136c30F0B97c",
        "1000000000000000000"
      ],
      "blockNumber": 123457,
      "transactionHash": "0x2f8a1234567890abcdef1234567890abcdef1234567890abcdef1234567890def"
    }
  ]
}
```

### Call Contract Method (Read-Only)

Executes a view/pure method on a Neo X contract.

```http
GET /neo-x/{contractAddress}/call/{method}?parameters=[\"0x8ba1f109551bD432803012645Hac136c30F0B97c\"]
```

**Response:**
```json
{
  "result": "1000000000000000000"
}
```

## Cross-Chain Operations

### Execute Cross-Chain Transaction

Executes a transaction that spans between Neo N3 and Neo X networks.

```http
POST /cross-chain/execute
```

**Request Body:**
```json
{
  "sourceBlockchain": "NeoN3",
  "targetBlockchain": "NeoX",
  "sourceContract": "0x1234567890abcdef1234567890abcdef12345678",
  "targetContract": "0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234",
  "method": "mint",
  "parameters": [
    "0x8ba1f109551bD432803012645Hac136c30F0B97c",
    1000000000000000000
  ],
  "value": 0.1,
  "gasLimit": 2000000
}
```

**Response:**
```json
{
  "sourceTransactionHash": "0xabc1234567890def1234567890abc1234567890def1234567890abc1234567890",
  "targetTransactionHash": "0xdef1234567890abc1234567890def1234567890abc1234567890def1234567890",
  "isSuccess": true,
  "sourceBlockNumber": 1234568,
  "targetBlockNumber": 123458,
  "sourceGasConsumed": 1000000,
  "targetGasConsumed": 150000,
  "bridgeFee": 0.001,
  "completedAt": "2024-01-15T11:00:00Z"
}
```

### Get Cross-Chain Transaction Status

Retrieves the status of a cross-chain transaction.

```http
GET /cross-chain/status/{sourceTransactionHash}
```

**Response:**
```json
{
  "sourceTransactionHash": "0xabc1234567890def1234567890abc1234567890def1234567890abc1234567890",
  "targetTransactionHash": "0xdef1234567890abc1234567890def1234567890abc1234567890def1234567890",
  "isSuccess": true,
  "status": "completed",
  "sourceBlockNumber": 1234568,
  "targetBlockNumber": 123458,
  "bridgeFee": 0.001,
  "completedAt": "2024-01-15T11:00:00Z"
}
```

### Get Bridge Configuration

Gets the bridge configuration for a specific chain pair.

```http
GET /cross-chain/bridge-config/{sourceChain}/{targetChain}
```

**Response:**
```json
{
  "sourceBridgeAddress": "0x1234567890abcdef1234567890abcdef12345678",
  "targetBridgeAddress": "0x742d35cc6049b2c0c2a3d6fd9e42e5d7b8e3f234",
  "minConfirmations": 6,
  "operators": [
    "0xoperator1...",
    "0xoperator2...",
    "0xoperator3..."
  ],
  "signatureThreshold": 2,
  "feePercentage": 0.001
}
```

## Error Handling

All API endpoints return structured error responses following RFC 7807 (Problem Details for HTTP APIs).

### Error Response Format

```json
{
  "type": "https://docs.neoservicelayer.io/errors/contract-deployment-failed",
  "title": "Contract Deployment Failed",
  "status": 400,
  "detail": "The contract bytecode is invalid or malformed",
  "instance": "/api/v1/smart-contracts/neo-n3/deploy",
  "timestamp": "2024-01-15T10:30:00Z",
  "traceId": "abc123def456"
}
```

### Common Error Codes

| Status Code | Error Type | Description |
|-------------|------------|-------------|
| 400 | `validation-error` | Invalid request parameters |
| 401 | `authentication-required` | Missing or invalid JWT token |
| 403 | `insufficient-permissions` | User lacks required role |
| 404 | `contract-not-found` | Contract does not exist |
| 409 | `contract-already-exists` | Contract with same hash exists |
| 429 | `rate-limit-exceeded` | Too many requests |
| 500 | `internal-server-error` | Unexpected server error |
| 502 | `blockchain-unavailable` | Cannot connect to blockchain node |
| 503 | `service-unavailable` | Service temporarily unavailable |

### Blockchain-Specific Errors

**Neo N3 Errors:**
- `insufficient-gas` - Not enough GAS for transaction
- `invalid-script-hash` - Contract hash format invalid
- `vm-fault` - Contract execution failed

**Neo X Errors:**
- `gas-estimation-failed` - Cannot estimate transaction gas
- `invalid-contract-address` - Contract address format invalid
- `revert` - Transaction reverted with reason

## Rate Limiting

API requests are rate-limited to ensure fair usage and system stability.

### Rate Limits

| Endpoint Category | Requests per Minute | Burst Limit |
|-------------------|---------------------|-------------|
| Read Operations | 1000 | 100 |
| Write Operations | 100 | 20 |
| Deploy Operations | 10 | 2 |
| Cross-Chain Operations | 20 | 5 |

### Rate Limit Headers

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642329600
```

### Rate Limit Exceeded Response

```json
{
  "type": "https://docs.neoservicelayer.io/errors/rate-limit-exceeded",
  "title": "Rate Limit Exceeded",
  "status": 429,
  "detail": "API rate limit of 1000 requests per minute exceeded",
  "retryAfter": 60
}
```

## SDK Usage

### JavaScript/TypeScript SDK

```typescript
import { NeoServiceLayerClient } from '@neo/service-layer-sdk';

const client = new NeoServiceLayerClient({
  baseUrl: 'https://api.neoservicelayer.io',
  apiKey: 'your-jwt-token'
});

// Deploy Neo N3 contract
const deployResult = await client.neoN3.deployContract({
  contractCode: 'base64-encoded-code',
  name: 'MyToken',
  constructorParameters: ['Initial Supply', 'TOKEN']
});

// Invoke contract method
const invokeResult = await client.neoN3.invokeContract(
  '0x1234...', 
  'transfer', 
  ['from', 'to', 100]
);

// Call read-only method
const balance = await client.neoN3.callContract(
  '0x1234...', 
  'balanceOf', 
  ['0xuser...']
);

// Cross-chain transaction
const crossChainResult = await client.crossChain.execute({
  sourceBlockchain: 'NeoN3',
  targetBlockchain: 'NeoX',
  sourceContract: '0x1234...',
  targetContract: '0x5678...',
  method: 'mint',
  parameters: ['0xuser...', 1000]
});
```

### Python SDK

```python
from neo_service_layer import NeoServiceLayerClient

client = NeoServiceLayerClient(
    base_url='https://api.neoservicelayer.io',
    api_key='your-jwt-token'
)

# Deploy contract
deploy_result = client.neo_n3.deploy_contract(
    contract_code='base64-encoded-code',
    name='MyToken',
    constructor_parameters=['Initial Supply', 'TOKEN']
)

# Invoke method
invoke_result = client.neo_n3.invoke_contract(
    contract_hash='0x1234...',
    method='transfer',
    parameters=['from', 'to', 100]
)

# Cross-chain transaction
cross_chain_result = client.cross_chain.execute({
    'sourceBlockchain': 'NeoN3',
    'targetBlockchain': 'NeoX',
    'sourceContract': '0x1234...',
    'targetContract': '0x5678...',
    'method': 'mint',
    'parameters': ['0xuser...', 1000]
})
```

### C# SDK

```csharp
using Neo.ServiceLayer.Sdk;

var client = new NeoServiceLayerClient(new NeoServiceLayerOptions
{
    BaseUrl = "https://api.neoservicelayer.io",
    ApiKey = "your-jwt-token"
});

// Deploy contract
var deployResult = await client.NeoN3.DeployContractAsync(new DeployContractRequest
{
    ContractCode = "base64-encoded-code",
    Name = "MyToken",
    ConstructorParameters = new object[] { "Initial Supply", "TOKEN" }
});

// Invoke method
var invokeResult = await client.NeoN3.InvokeContractAsync(
    "0x1234...", 
    new InvokeContractRequest
    {
        Method = "transfer",
        Parameters = new object[] { "from", "to", 100 }
    }
);

// Cross-chain transaction
var crossChainResult = await client.CrossChain.ExecuteAsync(new CrossChainTransactionRequest
{
    SourceBlockchain = "NeoN3",
    TargetBlockchain = "NeoX",
    SourceContract = "0x1234...",
    TargetContract = "0x5678...",
    Method = "mint",
    Parameters = new object[] { "0xuser...", 1000 }
});
```

## Webhooks

The API supports webhooks for real-time notifications of contract events and transaction status updates.

### Webhook Configuration

```http
POST /api/v1/webhooks
```

**Request Body:**
```json
{
  "url": "https://your-domain.com/webhooks/neo-service-layer",
  "events": [
    "contract.deployed",
    "contract.invoked",
    "transaction.confirmed",
    "cross-chain.completed"
  ],
  "secret": "your-webhook-secret"
}
```

### Webhook Payload

```json
{
  "id": "webhook_1234567890",
  "event": "contract.deployed",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "contractHash": "0x1234567890abcdef1234567890abcdef12345678",
    "transactionHash": "0xabcdef...",
    "blockNumber": 1234567,
    "blockchain": "NeoN3"
  }
}
```

## Support

For additional support:
- **Documentation**: https://docs.neoservicelayer.io
- **GitHub**: https://github.com/neo/service-layer
- **Discord**: https://discord.gg/neo-service-layer
- **Email**: api-support@neoservicelayer.io