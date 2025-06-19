# Neo Service Layer API Reference

## Overview

The Neo Service Layer Web Application exposes a comprehensive RESTful API that provides access to all 20+ services in the ecosystem. This document provides complete API documentation for all endpoints.

## 🌐 Base Information

- **Base URL**: `http://localhost:5000/api` (Development)
- **Authentication**: JWT Bearer tokens required
- **Content Type**: `application/json`
- **API Version**: v1

## 🔐 Authentication

### **Get Demo Token**
```http
POST /api/auth/demo-token
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-01-01T12:00:00Z"
}
```

**Usage:**
```javascript
const response = await fetch('/api/auth/demo-token', { method: 'POST' });
const { token } = await response.json();

// Use token in subsequent requests
const serviceResponse = await fetch('/api/service/endpoint', {
    headers: { 'Authorization': `Bearer ${token}` }
});
```

## 🏢 Core Services

### **Key Management Service**

#### **Generate Key**
```http
POST /api/keymanagement/generate/{blockchainType}
Authorization: Bearer {token}
Content-Type: application/json
```

**Path Parameters:**
- `blockchainType`: `NeoN3` | `NeoX`

**Request Body:**
```json
{
  "keyId": "unique-key-identifier",
  "keyType": "ECDSA" | "Ed25519" | "RSA2048",
  "keyUsage": "Signing" | "Encryption" | "KeyExchange",
  "exportable": false,
  "description": "Key description"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "keyId": "unique-key-identifier",
    "keyType": "ECDSA",
    "keyUsage": "Signing",
    "publicKey": "04a1b2c3d4e5f6...",
    "created": "2024-01-01T12:00:00Z",
    "status": "Active",
    "blockchain": "NeoN3",
    "enclave": "SGX Protected",
    "fingerprint": "a1b2c3d4e5f6..."
  },
  "message": "Key generated successfully"
}
```

#### **List Keys**
```http
GET /api/keymanagement/list/{blockchainType}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "keys": [
      {
        "keyId": "key_001",
        "keyType": "ECDSA",
        "keyUsage": "Signing",
        "created": "2024-01-01T12:00:00Z",
        "status": "Active",
        "publicKey": "04a1b2c3d4e5f6..."
      }
    ],
    "total": 1,
    "blockchain": "NeoN3",
    "timestamp": "2024-01-01T12:00:00Z"
  },
  "message": "Keys retrieved successfully"
}
```

### **Randomness Service**

#### **Generate Random Data**
```http
POST /api/randomness/generate
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "format": "hex" | "decimal" | "binary" | "base64",
  "byteCount": 32,
  "randomType": "secure",
  "seed": "optional-seed-value"
}
```

**Response:**
```json
{
  "success": true,
  "data": "a1b2c3d4e5f6789abcdef0123456789",
  "format": "hex",
  "byteCount": 32,
  "randomType": "secure",
  "timestamp": "2024-01-01T12:00:00Z",
  "entropySource": "SGX-Enclave-RNG"
}
```

#### **Health Check**
```http
GET /api/randomness/health
```

**Response:**
```json
{
  "status": "healthy",
  "service": "Randomness Service",
  "timestamp": "2024-01-01T12:00:00Z",
  "enclave_status": "active",
  "entropy_quality": 0.999,
  "numbers_generated": 15420,
  "uptime": "15d"
}
```

### **Oracle Service**

#### **Create Data Feed**
```http
POST /api/oracle/feeds
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Price Feed BTC/USD",
  "description": "Bitcoin price feed from multiple sources",
  "dataSource": "https://api.example.com/btc-usd",
  "updateInterval": 300
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "feedId": "feed_12345",
    "name": "Price Feed BTC/USD",
    "description": "Bitcoin price feed from multiple sources",
    "dataSource": "https://api.example.com/btc-usd",
    "updateInterval": 300,
    "status": "Active",
    "created": "2024-01-01T12:00:00Z",
    "lastUpdate": "2024-01-01T12:00:00Z"
  },
  "message": "Data feed created successfully"
}
```

#### **List Data Feeds**
```http
GET /api/oracle/feeds
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "feedId": "feed_12345",
      "name": "Price Feed BTC/USD",
      "status": "Active",
      "lastValue": "45000.00",
      "lastUpdate": "2024-01-01T12:00:00Z"
    }
  ],
  "message": "Data feeds retrieved successfully"
}
```

### **Voting Service**

#### **Create Proposal**
```http
POST /api/voting/proposals
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "title": "Upgrade Protocol Version",
  "description": "Proposal to upgrade the protocol to version 2.0",
  "options": ["Yes", "No", "Abstain"],
  "votingPeriod": 168,
  "requiredQuorum": 10
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "proposalId": "prop_12345",
    "title": "Upgrade Protocol Version",
    "description": "Proposal to upgrade the protocol to version 2.0",
    "options": ["Yes", "No", "Abstain"],
    "votingPeriod": 168,
    "requiredQuorum": 10,
    "status": "Active",
    "created": "2024-01-01T12:00:00Z",
    "endTime": "2024-01-08T12:00:00Z",
    "totalVotes": 0
  },
  "message": "Proposal created successfully"
}
```

#### **List Proposals**
```http
GET /api/voting/proposals
Authorization: Bearer {token}
```

**Query Parameters:**
- `status` (optional): Filter by proposal status

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "proposalId": "prop_12345",
      "title": "Upgrade Protocol Version",
      "status": "Active",
      "totalVotes": 150,
      "endTime": "2024-01-08T12:00:00Z"
    }
  ],
  "message": "Proposals retrieved successfully"
}
```

## 💾 Storage & Data Services

### **Storage Service**

#### **Store Data**
```http
POST /api/storage/store
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "dataId": "unique-data-identifier",
  "data": "Base64 encoded data content",
  "metadata": {
    "type": "document",
    "timestamp": "2024-01-01T12:00:00Z",
    "tags": ["important", "encrypted"]
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "dataId": "unique-data-identifier",
    "size": 1024,
    "stored": "2024-01-01T12:00:00Z",
    "encrypted": true,
    "checksum": "sha256:a1b2c3d4..."
  },
  "message": "Data stored successfully"
}
```

#### **List Stored Data**
```http
GET /api/storage/list
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "dataId": "data_001",
      "size": 1024,
      "stored": "2024-01-01T12:00:00Z",
      "metadata": {
        "type": "document",
        "tags": ["important"]
      }
    }
  ],
  "message": "Data list retrieved successfully"
}
```

### **Backup Service**

#### **Create Backup**
```http
POST /api/backup/backups
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "backupType": "Full" | "Incremental" | "Differential",
  "description": "Weekly full backup",
  "dataTypes": ["keys", "configurations", "user-data"]
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "backupId": "backup_12345",
    "backupType": "Full",
    "description": "Weekly full backup",
    "created": "2024-01-01T12:00:00Z",
    "status": "Completed",
    "size": "150MB",
    "dataTypes": ["keys", "configurations", "user-data"]
  },
  "message": "Backup created successfully"
}
```

### **Configuration Service**

#### **Get Configuration**
```http
GET /api/configuration/settings
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "settings": {
      "system.timeout": "30000",
      "logging.level": "INFO",
      "security.enabled": "true"
    },
    "lastModified": "2024-01-01T12:00:00Z"
  },
  "message": "Configuration retrieved successfully"
}
```

#### **Update Configuration**
```http
PUT /api/configuration/settings
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "key": "system.timeout",
  "value": "45000",
  "description": "Increased timeout for better performance"
}
```

## 🔒 Security Services

### **Zero Knowledge Service**

#### **Create Proof**
```http
POST /api/zeroknowledge/proofs
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "circuitId": "verification-circuit",
  "inputs": {
    "value": 42,
    "secret": "private-input"
  },
  "proofType": "zk-SNARKs"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "proofId": "proof_12345",
    "proof": "0x1a2b3c4d...",
    "publicInputs": {
      "value": 42
    },
    "created": "2024-01-01T12:00:00Z",
    "verified": true
  },
  "message": "Proof created successfully"
}
```

#### **Verify Proof**
```http
POST /api/zeroknowledge/verify
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "proof": "0x1a2b3c4d...",
  "publicInputs": {
    "value": 42
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "valid": true,
    "verified": "2024-01-01T12:00:00Z",
    "verificationTime": "125ms"
  },
  "message": "Proof verified successfully"
}
```

### **Abstract Account Service**

#### **Create Account**
```http
POST /api/abstractaccount/accounts
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "accountType": "MultiSig" | "Social" | "Timelock",
  "owners": ["0x1234567890abcdef"],
  "threshold": 1,
  "name": "My Smart Account"
}
```

### **Compliance Service**

#### **Run Compliance Check**
```http
POST /api/compliance/check
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "checkType": "AML" | "KYC" | "Sanctions",
  "address": "0x1234567890abcdef",
  "amount": "1000.0"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "checkId": "check_12345",
    "checkType": "AML",
    "address": "0x1234567890abcdef",
    "result": "PASS" | "FAIL" | "WARNING",
    "riskScore": 0.15,
    "details": "Low risk transaction",
    "timestamp": "2024-01-01T12:00:00Z"
  },
  "message": "Compliance check completed"
}
```

### **Proof of Reserve Service**

#### **Generate Proof**
```http
POST /api/proofofreserve/generate
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "asset": "NEO" | "GAS" | "BTC" | "ETH",
  "reserveAmount": "10000",
  "includePrivateData": false
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "proofId": "reserve_proof_12345",
    "asset": "NEO",
    "reserveAmount": "10000",
    "proof": "0xa1b2c3d4...",
    "merkleRoot": "0x5e6f7a8b...",
    "generated": "2024-01-01T12:00:00Z",
    "validUntil": "2024-01-02T12:00:00Z"
  },
  "message": "Proof of reserve generated successfully"
}
```

## ⚙️ Operations Services

### **Automation Service**

#### **Create Job**
```http
POST /api/automation/jobs
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Daily Health Check",
  "description": "Automated daily system health verification",
  "trigger": {
    "type": "Scheduled",
    "interval": "0 9 * * *"
  },
  "action": {
    "type": "RestApi",
    "endpoint": "/api/health/comprehensive"
  }
}
```

### **Monitoring Service**

#### **Get Metrics**
```http
GET /api/monitoring/metrics
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "cpu": {
      "usage": 45.2,
      "cores": 8
    },
    "memory": {
      "used": "4.2GB",
      "total": "16GB",
      "percentage": 26.25
    },
    "services": {
      "total": 20,
      "healthy": 19,
      "warning": 1,
      "error": 0
    },
    "timestamp": "2024-01-01T12:00:00Z"
  },
  "message": "Metrics retrieved successfully"
}
```

### **Health Service**

#### **Health Check**
```http
GET /api/health/check
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "overall": "Healthy",
    "services": {
      "database": "Healthy",
      "redis": "Healthy",
      "sgx_enclave": "Healthy"
    },
    "timestamp": "2024-01-01T12:00:00Z",
    "uptime": "15d 4h 32m"
  },
  "message": "Health check completed"
}
```

### **Notification Service**

#### **Send Notification**
```http
POST /api/notification/send
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "channel": "email" | "sms" | "webhook" | "push",
  "recipient": "user@example.com",
  "subject": "System Alert",
  "message": "Your attention is required",
  "priority": "high" | "medium" | "low"
}
```

## 🌐 Infrastructure Services

### **Cross-Chain Service**

#### **Initiate Bridge**
```http
POST /api/crosschain/bridge
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "fromChain": "NeoN3" | "NeoX" | "Ethereum" | "Bitcoin",
  "toChain": "NeoN3" | "NeoX" | "Ethereum" | "Bitcoin",
  "asset": "NEO" | "GAS" | "ETH" | "BTC",
  "amount": "10.0",
  "recipient": "0x1234567890abcdef"
}
```

### **Compute Service**

#### **Execute Computation**
```http
POST /api/compute/execute
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "computationType": "SecureFunction" | "SmartContract" | "MLModel",
  "code": "function calculate(x, y) { return x * y + 42; }",
  "inputs": {
    "x": 10,
    "y": 5
  }
}
```

### **Event Subscription Service**

#### **Create Subscription**
```http
POST /api/eventsubscription/subscriptions
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "eventType": "TokenTransfer" | "BlockCreated" | "ContractInvoked",
  "filters": {
    "contractHash": "0x1234567890abcdef"
  },
  "webhook": "https://your-webhook.example.com/events",
  "isActive": true
}
```

## 🚨 Error Responses

### **Common Error Codes**

#### **400 Bad Request**
```json
{
  "success": false,
  "error": "Invalid request parameters",
  "details": "The 'amount' field must be greater than 0"
}
```

#### **401 Unauthorized**
```json
{
  "success": false,
  "error": "Authentication required",
  "details": "Please provide a valid JWT token"
}
```

#### **403 Forbidden**
```json
{
  "success": false,
  "error": "Access denied",
  "details": "Insufficient permissions for this operation"
}
```

#### **404 Not Found**
```json
{
  "success": false,
  "error": "Resource not found",
  "details": "The requested resource does not exist"
}
```

#### **500 Internal Server Error**
```json
{
  "success": false,
  "error": "Internal server error",
  "details": "An unexpected error occurred"
}
```

## 📊 Standard Response Format

All API responses follow a consistent format:

### **Success Response**
```json
{
  "success": true,
  "data": { /* service-specific data */ },
  "message": "Operation completed successfully",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### **Error Response**
```json
{
  "success": false,
  "error": "Error type",
  "details": "Detailed error message",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## 🔧 Request/Response Headers

### **Standard Request Headers**
```http
Authorization: Bearer {jwt-token}
Content-Type: application/json
Accept: application/json
User-Agent: Neo-Service-Layer-Client/1.0
```

### **Standard Response Headers**
```http
Content-Type: application/json
X-Request-ID: {unique-request-id}
X-Rate-Limit-Remaining: 95
X-Rate-Limit-Reset: 1609459200
```

## 📚 OpenAPI/Swagger

Interactive API documentation is available at:
- **Development**: `http://localhost:5000/swagger`
- **Swagger JSON**: `http://localhost:5000/swagger/v1/swagger.json`

## 🔗 Related Documentation

- [Web Application Guide](WEB_APPLICATION_GUIDE.md) - Main web app documentation
- [Service Integration](SERVICE_INTEGRATION.md) - Service integration patterns
- [Authentication & Security](AUTHENTICATION.md) - Security implementation

---

This API provides comprehensive access to all Neo Service Layer capabilities through a consistent, secure, and well-documented interface.