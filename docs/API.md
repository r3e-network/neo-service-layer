# Neo Service Layer - API Documentation

## 1. Introduction

This document describes the RESTful API endpoints provided by the Neo Service Layer (NSL). The API allows clients to interact with the NSL to execute JavaScript functions securely, manage user secrets, set up event triggers, track GAS usage, manage keys, generate random numbers, and verify attestations.

## 2. API Overview

### 2.1 Base URL

```
https://api.neoconfidentialserverless.io/v1
```

### 2.2 Authentication

All API requests require authentication using an API key. The API key should be included in the `Authorization` header of each request:

```
Authorization: Bearer <api_key>
```

### 2.3 Response Format

All API responses are in JSON format and include a standard structure:

```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```

In case of an error:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "error_code",
    "message": "Error message"
  }
}
```

### 2.4 Rate Limiting

The API enforces rate limiting to prevent abuse. Rate limit information is included in the response headers:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1620000000
```

## 3. API Endpoints

### 3.1 JavaScript Function Management

#### 3.1.1 Create JavaScript Function

```
POST /functions
```

Create a new JavaScript function.

**Request Body:**

```json
{
  "name": "myFunction",
  "code": "function myFunction(input) { return input.value * 2; }",
  "description": "A function that doubles the input value",
  "required_secrets": ["API_KEY", "DATABASE_PASSWORD"],
  "gas_limit": 1000000
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "function_id": "func_123456",
    "name": "myFunction",
    "status": "active",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.1.2 Get JavaScript Function

```
GET /functions/{function_id}
```

Get information about a specific JavaScript function.

**Response:**

```json
{
  "success": true,
  "data": {
    "function_id": "func_123456",
    "name": "myFunction",
    "description": "A function that doubles the input value",
    "code": "function myFunction(input) { return input.value * 2; }",
    "required_secrets": ["API_KEY", "DATABASE_PASSWORD"],
    "gas_limit": 1000000,
    "status": "active",
    "created_at": "2023-05-01T12:00:00Z",
    "updated_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.1.3 List JavaScript Functions

```
GET /functions
```

List all JavaScript functions for the authenticated user.

**Query Parameters:**

- `status`: Filter by status (active, inactive)
- `page`: Page number (default: 1)
- `limit`: Number of items per page (default: 10)

**Response:**

```json
{
  "success": true,
  "data": {
    "functions": [
      {
        "function_id": "func_123456",
        "name": "myFunction",
        "description": "A function that doubles the input value",
        "status": "active",
        "created_at": "2023-05-01T12:00:00Z",
        "updated_at": "2023-05-01T12:00:00Z"
      },
      ...
    ],
    "pagination": {
      "total": 100,
      "page": 1,
      "limit": 10,
      "pages": 10
    }
  },
  "error": null
}
```

### 3.2 Attestation

#### 3.2.1 Get Attestation

```
GET /attestation
```

Get the current attestation report for the TEE.

**Response:**

```json
{
  "success": true,
  "data": {
    "attestation_report": "base64_encoded_report",
    "signature": "base64_encoded_signature",
    "timestamp": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.2.2 Verify Attestation

```
POST /attestation/verify
```

Verify an attestation report.

**Request Body:**

```json
{
  "attestation_report": "base64_encoded_report",
  "signature": "base64_encoded_signature"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "valid": true,
    "enclave_identity": {
      "mrenclave": "mrenclave_hash",
      "mrsigner": "mrsigner_hash"
    },
    "timestamp": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

### 3.3 Key Management

#### 3.3.1 Create Key

```
POST /keys
```

Create a new key in the TEE.

**Request Body:**

```json
{
  "key_type": "secp256r1",
  "key_name": "my_key",
  "exportable": false
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "key_id": "key_123456",
    "key_type": "secp256r1",
    "key_name": "my_key",
    "public_key": "base64_encoded_public_key",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.3.2 Get Key

```
GET /keys/{key_id}
```

Get information about a specific key.

**Response:**

```json
{
  "success": true,
  "data": {
    "key_id": "key_123456",
    "key_type": "secp256r1",
    "key_name": "my_key",
    "public_key": "base64_encoded_public_key",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.3.3 List Keys

```
GET /keys
```

List all keys for the authenticated user.

**Response:**

```json
{
  "success": true,
  "data": {
    "keys": [
      {
        "key_id": "key_123456",
        "key_type": "secp256r1",
        "key_name": "my_key",
        "created_at": "2023-05-01T12:00:00Z"
      },
      ...
    ]
  },
  "error": null
}
```

#### 3.3.4 Sign Data

```
POST /keys/{key_id}/sign
```

Sign data using a key in the TEE.

**Request Body:**

```json
{
  "data": "base64_encoded_data",
  "hash_algorithm": "sha256"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "signature": "base64_encoded_signature",
    "timestamp": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

### 3.4 Randomness

#### 3.4.1 Generate Random Numbers

```
POST /randomness
```

Generate random numbers in the TEE.

**Request Body:**

```json
{
  "count": 10,
  "min": 1,
  "max": 100,
  "seed": "optional_seed"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "random_numbers": [42, 17, 89, ...],
    "proof": "base64_encoded_proof",
    "timestamp": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

### 3.5 Compliance & Identity Verification

#### 3.5.1 Verify Identity

```
POST /compliance/verify
```

Verify an identity in the TEE.

**Request Body:**

```json
{
  "identity_data": "encrypted_identity_data",
  "verification_type": "kyc"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "verification_id": "verification_123456",
    "status": "pending",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.5.2 Get Verification Result

```
GET /compliance/verify/{verification_id}
```

Get the result of an identity verification.

**Response:**

```json
{
  "success": true,
  "data": {
    "verification_id": "verification_123456",
    "status": "completed",
    "result": {
      "verified": true,
      "score": 0.95
    },
    "created_at": "2023-05-01T12:00:00Z",
    "completed_at": "2023-05-01T12:01:00Z"
  },
  "error": null
}
```

### 3.6 Event Subscription

#### 3.6.1 Create Subscription

```
POST /events/subscriptions
```

Create a new event subscription.

**Request Body:**

```json
{
  "event_type": "blockchain_event",
  "event_filter": {
    "contract_hash": "0x1234567890abcdef",
    "event_name": "Transfer"
  },
  "callback_url": "https://example.com/callback"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "subscription_id": "subscription_123456",
    "status": "active",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.6.2 Get Subscription

```
GET /events/subscriptions/{subscription_id}
```

Get information about a specific subscription.

**Response:**

```json
{
  "success": true,
  "data": {
    "subscription_id": "subscription_123456",
    "event_type": "blockchain_event",
    "event_filter": {
      "contract_hash": "0x1234567890abcdef",
      "event_name": "Transfer"
    },
    "callback_url": "https://example.com/callback",
    "status": "active",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.6.3 List Subscriptions

```
GET /events/subscriptions
```

List all subscriptions for the authenticated user.

**Response:**

```json
{
  "success": true,
  "data": {
    "subscriptions": [
      {
        "subscription_id": "subscription_123456",
        "event_type": "blockchain_event",
        "status": "active",
        "created_at": "2023-05-01T12:00:00Z"
      },
      ...
    ]
  },
  "error": null
}
```

#### 3.6.4 Delete Subscription

```
DELETE /events/subscriptions/{subscription_id}
```

Delete an event subscription.

**Response:**

```json
{
  "success": true,
  "data": {
    "deleted": true
  },
  "error": null
}
```

### 3.7 User Secrets Management

#### 3.7.1 Create User Secret

```
POST /secrets
```

Create a new user secret.

**Request Body:**

```json
{
  "name": "API_KEY",
  "value": "sk_test_abcdefghijklmnopqrstuvwxyz",
  "description": "API key for external service"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "secret_id": "secret_123456",
    "name": "API_KEY",
    "description": "API key for external service",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.7.2 Get User Secret

```
GET /secrets/{secret_id}
```

Get information about a specific user secret (value is not returned).

**Response:**

```json
{
  "success": true,
  "data": {
    "secret_id": "secret_123456",
    "name": "API_KEY",
    "description": "API key for external service",
    "created_at": "2023-05-01T12:00:00Z",
    "updated_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

#### 3.7.3 List User Secrets

```
GET /secrets
```

List all user secrets for the authenticated user.

**Response:**

```json
{
  "success": true,
  "data": {
    "secrets": [
      {
        "secret_id": "secret_123456",
        "name": "API_KEY",
        "description": "API key for external service",
        "created_at": "2023-05-01T12:00:00Z",
        "updated_at": "2023-05-01T12:00:00Z"
      },
      ...
    ]
  },
  "error": null
}
```

### 3.8 Event Triggers

#### 3.8.1 Create Event Trigger

```
POST /triggers
```

Create a new event trigger for a JavaScript function.

**Request Body:**

```json
{
  "function_id": "func_123456",
  "event_type": "block_added",
  "filters": {
    "contract_hash": "0x1234567890abcdef"
  },
  "input_mapping": {
    "block_height": "$.height",
    "timestamp": "$.timestamp"
  }
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "trigger_id": "trigger_123456",
    "function_id": "func_123456",
    "event_type": "block_added",
    "status": "active",
    "created_at": "2023-05-01T12:00:00Z"
  },
  "error": null
}
```

### 3.9 GAS Accounting

#### 3.9.1 Get GAS Usage

```
GET /gas/usage
```

Get GAS usage information for the authenticated user.

**Response:**

```json
{
  "success": true,
  "data": {
    "total_gas_used": 1234567,
    "current_period_gas_used": 123456,
    "gas_limit": 10000000,
    "period_start": "2023-05-01T00:00:00Z",
    "period_end": "2023-05-31T23:59:59Z"
  },
  "error": null
}
```

#### 3.9.2 Get Function GAS Usage

```
GET /functions/{function_id}/gas
```

Get GAS usage information for a specific JavaScript function.

**Response:**

```json
{
  "success": true,
  "data": {
    "function_id": "func_123456",
    "total_gas_used": 123456,
    "average_gas_per_execution": 1234,
    "execution_count": 100,
    "last_execution_gas": 1200
  },
  "error": null
}
```

## 4. Error Codes

| Code | Description |
|------|-------------|
| `authentication_error` | Invalid or missing API key |
| `authorization_error` | Insufficient permissions |
| `validation_error` | Invalid request parameters |
| `resource_not_found` | Requested resource not found |
| `rate_limit_exceeded` | Rate limit exceeded |
| `internal_error` | Internal server error |
| `tee_error` | Error in the TEE |
| `attestation_error` | Error in attestation |

## 5. Webhooks

The NCSL can send webhook notifications for various events:

### 5.1 Task Completion

```json
{
  "event_type": "task.completed",
  "task_id": "task_123456",
  "status": "completed",
  "result": { ... },
  "timestamp": "2023-05-01T12:01:00Z"
}
```

### 5.2 Event Notification

```json
{
  "event_type": "blockchain_event",
  "subscription_id": "subscription_123456",
  "event_data": { ... },
  "timestamp": "2023-05-01T12:01:00Z"
}
```


