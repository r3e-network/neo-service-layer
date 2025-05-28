# Neo Service Layer API Endpoints

## Overview

This document provides a comprehensive list of API endpoints available in the Neo Service Layer. Each endpoint is described with its HTTP method, URL, request parameters, and response format.

## Base URL

All API endpoints are relative to the base URL:

```
https://api.neoservicelayer.org/api/v1
```

## Authentication

All API endpoints require authentication. See the [Authentication](README.md#authentication) section for details.

## Common Parameters

All API endpoints support the following common parameters:

- **blockchain**: The blockchain type to use (e.g., `neo-n3`, `neo-x`).
- **format**: The response format (e.g., `json`, `xml`).
- **version**: The API version to use (e.g., `v1`).

## Common Response Format

All API responses follow a common format. See the [Common Response Format](README.md#common-response-format) section for details.

## Randomness Service Endpoints

### Generate Random Number

Generates a random number.

**URL**: `/randomness/generate`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "min": 1,
  "max": 100,
  "seed": "optional-seed"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "value": 42,
    "proof": "cryptographic-proof",
    "timestamp": "2023-01-01T00:00:00Z"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Verify Random Number

Verifies a random number.

**URL**: `/randomness/verify`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "value": 42,
  "proof": "cryptographic-proof",
  "timestamp": "2023-01-01T00:00:00Z"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "valid": true
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Oracle Service Endpoints

### Fetch Data

Fetches data from an external source.

**URL**: `/oracle/fetch`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "url": "https://api.example.com/data",
  "path": "$.data.value"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "value": "42.5",
    "proof": "cryptographic-proof",
    "timestamp": "2023-01-01T00:00:00Z",
    "source": "https://api.example.com/data"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Verify Data

Verifies data from an external source.

**URL**: `/oracle/verify`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "value": "42.5",
  "proof": "cryptographic-proof",
  "timestamp": "2023-01-01T00:00:00Z",
  "source": "https://api.example.com/data"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "valid": true
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Key Management Service Endpoints

### Generate Key

Generates a new key.

**URL**: `/keys/generate`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "keyType": "secp256r1",
  "keyUsage": "signing",
  "exportable": false
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "keyId": "key-id",
    "publicKey": "public-key",
    "keyType": "secp256r1",
    "keyUsage": "signing",
    "exportable": false,
    "createdAt": "2023-01-01T00:00:00Z"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Sign Data

Signs data using a key.

**URL**: `/keys/sign`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "keyId": "key-id",
  "data": "data-to-sign",
  "algorithm": "ECDSA"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "signature": "signature",
    "keyId": "key-id",
    "algorithm": "ECDSA",
    "timestamp": "2023-01-01T00:00:00Z"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Verify Signature

Verifies a signature.

**URL**: `/keys/verify`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "keyId": "key-id",
  "data": "data-to-verify",
  "signature": "signature",
  "algorithm": "ECDSA"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "valid": true
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Compute Service Endpoints

### Register Computation

Registers a computation.

**URL**: `/compute/register`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "computationId": "computation-id",
  "computationCode": "function compute(input) { return input * 2; }",
  "computationType": "JavaScript",
  "description": "A simple computation that doubles the input"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "computationId": "computation-id",
    "computationType": "JavaScript",
    "description": "A simple computation that doubles the input",
    "createdAt": "2023-01-01T00:00:00Z"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Execute Computation

Executes a computation.

**URL**: `/compute/execute`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "computationId": "computation-id",
  "parameters": {
    "input": "42"
  }
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "computationId": "computation-id",
    "resultId": "result-id",
    "resultData": "84",
    "executionTimeMs": 50,
    "timestamp": "2023-01-01T00:00:00Z",
    "proof": "cryptographic-proof"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Verify Computation Result

Verifies a computation result.

**URL**: `/compute/verify`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "computationId": "computation-id",
  "resultId": "result-id",
  "resultData": "84",
  "proof": "cryptographic-proof"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "valid": true
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Storage Service Endpoints

### Store Data

Stores data.

**URL**: `/storage/store`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "key": "my-data",
  "data": "base64-encoded-data",
  "options": {
    "encrypt": true,
    "compress": true,
    "chunkSizeBytes": 1048576,
    "accessControlList": ["user1", "user2"]
  }
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "key": "my-data",
    "size": 1024,
    "chunks": 1,
    "encrypted": true,
    "compressed": true,
    "createdAt": "2023-01-01T00:00:00Z",
    "accessControlList": ["user1", "user2"]
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Retrieve Data

Retrieves data.

**URL**: `/storage/retrieve`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "key": "my-data"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "key": "my-data",
    "data": "base64-encoded-data",
    "size": 1024,
    "chunks": 1,
    "encrypted": true,
    "compressed": true,
    "createdAt": "2023-01-01T00:00:00Z",
    "accessControlList": ["user1", "user2"]
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Compliance Service Endpoints

### Verify Address

Verifies an address against compliance rules.

**URL**: `/compliance/verify-address`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "address": "address"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "passed": true,
    "riskScore": 0,
    "violations": []
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Verify Transaction

Verifies a transaction against compliance rules.

**URL**: `/compliance/verify-transaction`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "transactionData": "transaction-data"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "passed": true,
    "riskScore": 0,
    "violations": []
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Event Subscription Service Endpoints

### Create Subscription

Creates a subscription.

**URL**: `/events/subscriptions`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "name": "New Block Subscription",
  "description": "Subscribe to new blocks",
  "eventType": "Block",
  "eventFilter": "",
  "callbackUrl": "https://example.com/callback",
  "callbackAuthHeader": "Bearer token123",
  "retryPolicy": {
    "maxRetries": 3,
    "initialRetryDelaySeconds": 5,
    "retryBackoffFactor": 2.0,
    "maxRetryDelaySeconds": 60
  }
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "subscriptionId": "subscription-id",
    "name": "New Block Subscription",
    "description": "Subscribe to new blocks",
    "eventType": "Block",
    "eventFilter": "",
    "callbackUrl": "https://example.com/callback",
    "callbackAuthHeader": "Bearer token123",
    "enabled": true,
    "createdAt": "2023-01-01T00:00:00Z",
    "lastModifiedAt": "2023-01-01T00:00:00Z",
    "retryPolicy": {
      "maxRetries": 3,
      "initialRetryDelaySeconds": 5,
      "retryBackoffFactor": 2.0,
      "maxRetryDelaySeconds": 60
    }
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Get Events

Gets events for a subscription.

**URL**: `/events/subscriptions/{subscriptionId}/events`

**Method**: `GET`

**Query Parameters**:

- **blockchain**: The blockchain type (e.g., `neo-n3`, `neo-x`).
- **skip**: The number of events to skip (default: 0).
- **take**: The number of events to take (default: 10, max: 100).

**Response**:

```json
{
  "success": true,
  "data": [
    {
      "eventId": "event-id",
      "subscriptionId": "subscription-id",
      "eventType": "Block",
      "data": "Event data",
      "timestamp": "2023-01-01T00:00:00Z",
      "acknowledged": false,
      "deliveryAttempts": 0,
      "deliveryStatus": "Pending"
    }
  ],
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z",
    "pagination": {
      "page": 1,
      "per_page": 10,
      "total_pages": 1,
      "total_items": 1
    }
  }
}
```

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer Services](../services/README.md)
- [Neo Service Layer Workflows](../workflows/README.md)
