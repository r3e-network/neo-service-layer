# Neo Service Layer - API Design Specification

## Overview

This document defines the comprehensive API design for the Neo Service Layer, including REST endpoints, GraphQL schema, WebSocket connections, and gRPC services for high-performance scenarios.

## API Design Principles

### 1. Core Principles
- **RESTful Design**: Resource-oriented URLs with proper HTTP verbs
- **Consistency**: Standardized request/response patterns across all endpoints
- **Versioning**: Backward-compatible API evolution
- **Security**: Authentication and authorization for all endpoints
- **Performance**: Optimized for low latency and high throughput
- **Documentation**: Complete OpenAPI specifications

### 2. URL Structure
```
https://api.neo-service-layer.com/{version}/{resource}/{identifier}
```

**Examples:**
- `GET /v1/users/123` - Get user by ID
- `POST /v1/blockchain/transactions` - Create transaction
- `PUT /v1/keys/456/rotate` - Rotate specific key
- `DELETE /v1/sessions/789` - Delete session

### 3. HTTP Status Codes
- **200 OK**: Successful GET, PUT operations
- **201 Created**: Successful POST operations
- **204 No Content**: Successful DELETE operations
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Missing or invalid authentication
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource doesn't exist
- **409 Conflict**: Resource state conflict
- **422 Unprocessable Entity**: Validation errors
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Server-side errors

## Authentication & Authorization

### 4. Authentication Methods

#### 4.1 JWT Bearer Token
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### 4.2 API Key Authentication
```http
X-API-Key: your-api-key-here
```

#### 4.3 Mutual TLS (mTLS)
```http
# Client certificate-based authentication for high-security scenarios
```

### 5. Request/Response Format

#### 5.1 Standard Request Headers
```http
Content-Type: application/json
Accept: application/json
Authorization: Bearer <JWT_TOKEN>
X-Request-ID: <UUID>
X-Client-Version: 1.0.0
User-Agent: NeoServiceLayer-Client/1.0.0
```

#### 5.2 Standard Response Format
```json
{
  "success": true,
  "data": {
    // Response data
  },
  "metadata": {
    "timestamp": "2025-01-23T10:30:00Z",
    "requestId": "123e4567-e89b-12d3-a456-426614174000",
    "version": "v1",
    "pagination": {
      "cursor": "eyJpZCI6MTIzLCJjcmVhdGVkX2F0IjoiMjAyNS0wMS0yM1QxMDozMDowMFoifQ==",
      "hasMore": true,
      "limit": 100,
      "total": 1250
    }
  },
  "errors": []
}
```

#### 5.3 Error Response Format
```json
{
  "success": false,
  "data": null,
  "metadata": {
    "timestamp": "2025-01-23T10:30:00Z",
    "requestId": "123e4567-e89b-12d3-a456-426614174000",
    "version": "v1"
  },
  "errors": [
    {
      "code": "VALIDATION_FAILED",
      "message": "Invalid input data provided",
      "field": "email",
      "details": {
        "expected": "Valid email format",
        "received": "invalid-email"
      }
    }
  ]
}
```

## REST API Endpoints

### 6. Authentication Endpoints

#### 6.1 User Authentication
```http
POST /v1/auth/login
Content-Type: application/json

{
  "username": "user@example.com",
  "password": "secure-password",
  "mfaCode": "123456"
}

Response:
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "username": "user@example.com",
      "roles": ["user", "trader"]
    }
  }
}
```

#### 6.2 Token Refresh
```http
POST /v1/auth/refresh
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### 6.3 Logout
```http
POST /v1/auth/logout
Authorization: Bearer <JWT_TOKEN>
```

### 7. User Management Endpoints

#### 7.1 Create User
```http
POST /v1/users
{
  "username": "newuser@example.com",
  "password": "secure-password",
  "profile": {
    "firstName": "John",
    "lastName": "Doe",
    "preferredLanguage": "en"
  },
  "roles": ["user"]
}
```

#### 7.2 Get User Profile
```http
GET /v1/users/{userId}
Authorization: Bearer <JWT_TOKEN>

Response:
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "user@example.com",
    "profile": {
      "firstName": "John",
      "lastName": "Doe",
      "preferredLanguage": "en"
    },
    "roles": ["user", "trader"],
    "createdAt": "2025-01-20T10:30:00Z",
    "lastLoginAt": "2025-01-23T09:15:00Z",
    "mfaEnabled": true
  }
}
```

#### 7.3 Update User Profile
```http
PUT /v1/users/{userId}
{
  "profile": {
    "firstName": "Jane",
    "lastName": "Smith",
    "preferredLanguage": "es"
  }
}
```

#### 7.4 List Users (Admin Only)
```http
GET /v1/users?cursor=eyJpZCI6MTIz&limit=50&role=trader&status=active
```

### 8. Key Management Endpoints

#### 8.1 Create Key Pair
```http
POST /v1/keys
{
  "keyType": "secp256k1",
  "purpose": "signing",
  "metadata": {
    "description": "Primary signing key",
    "expiresAt": "2026-01-23T10:30:00Z"
  }
}

Response:
{
  "success": true,
  "data": {
    "keyId": "key_123e4567-e89b-12d3-a456-426614174000",
    "publicKey": "026f7a2b8f...",
    "keyType": "secp256k1",
    "purpose": "signing",
    "status": "active",
    "createdAt": "2025-01-23T10:30:00Z",
    "expiresAt": "2026-01-23T10:30:00Z"
  }
}
```

#### 8.2 List User Keys
```http
GET /v1/keys?status=active&keyType=secp256k1
```

#### 8.3 Rotate Key
```http
PUT /v1/keys/{keyId}/rotate
{
  "newKeyType": "secp256k1",
  "migrateData": true
}
```

#### 8.4 Revoke Key
```http
DELETE /v1/keys/{keyId}
{
  "reason": "compromised",
  "migrateToKey": "key_456"
}
```

### 9. Blockchain Endpoints

#### 9.1 Create Transaction
```http
POST /v1/blockchain/transactions
{
  "network": "neo-n3",
  "fromAddress": "NXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXxx",
  "toAddress": "NYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYyy",
  "amount": "100.0",
  "asset": "GAS",
  "metadata": {
    "memo": "Payment for services"
  }
}
```

#### 9.2 Get Transaction Status
```http
GET /v1/blockchain/transactions/{txId}

Response:
{
  "success": true,
  "data": {
    "txId": "0x123abc...",
    "network": "neo-n3",
    "status": "confirmed",
    "blockNumber": 1234567,
    "blockHash": "0x456def...",
    "gasUsed": "0.001",
    "confirmations": 12,
    "fromAddress": "NXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXxx",
    "toAddress": "NYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYyy",
    "amount": "100.0",
    "asset": "GAS",
    "createdAt": "2025-01-23T10:30:00Z",
    "confirmedAt": "2025-01-23T10:31:00Z"
  }
}
```

#### 9.3 List Transactions
```http
GET /v1/blockchain/transactions?network=neo-n3&address=NXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXxx&status=confirmed&cursor=eyJpZCI6MTIz&limit=20
```

### 10. Smart Contract Endpoints

#### 10.1 Deploy Contract
```http
POST /v1/contracts
{
  "network": "neo-n3",
  "contractCode": "0x...",
  "manifest": {
    "name": "MyContract",
    "abi": [...],
    "permissions": [...]
  },
  "gasLimit": "10000000"
}
```

#### 10.2 Invoke Contract
```http
POST /v1/contracts/{contractHash}/invoke
{
  "network": "neo-n3",
  "method": "transfer",
  "parameters": [
    {"type": "Hash160", "value": "0x..."},
    {"type": "Hash160", "value": "0x..."},
    {"type": "Integer", "value": "1000"}
  ],
  "gasLimit": "100000"
}
```

#### 10.3 Query Contract State
```http
GET /v1/contracts/{contractHash}/state?network=neo-n3&key=balance&address=NXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXxx
```

### 11. Oracle Endpoints

#### 11.1 Create Oracle Request
```http
POST /v1/oracle/requests
{
  "dataSource": "https://api.coinbase.com/v2/exchange-rates?currency=NEO",
  "jsonPath": "$.data.rates.USD",
  "frequency": "60",
  "callback": {
    "url": "https://webhook.example.com/price-update",
    "headers": {
      "Authorization": "Bearer token"
    }
  }
}
```

#### 11.2 Get Oracle Data
```http
GET /v1/oracle/data/{requestId}

Response:
{
  "success": true,
  "data": {
    "requestId": "oracle_123",
    "value": "15.42",
    "dataType": "number",
    "timestamp": "2025-01-23T10:30:00Z",
    "confidence": 0.95,
    "sources": [
      {
        "name": "coinbase",
        "value": "15.42",
        "timestamp": "2025-01-23T10:30:00Z"
      }
    ]
  }
}
```

### 12. Storage Endpoints

#### 12.1 Store Data
```http
POST /v1/storage/data
{
  "key": "user-preferences",
  "data": {
    "theme": "dark",
    "language": "en",
    "notifications": true
  },
  "encryption": true,
  "ttl": 3600
}
```

#### 12.2 Retrieve Data
```http
GET /v1/storage/data/user-preferences

Response:
{
  "success": true,
  "data": {
    "key": "user-preferences",
    "data": {
      "theme": "dark",
      "language": "en",
      "notifications": true
    },
    "encrypted": true,
    "createdAt": "2025-01-23T10:30:00Z",
    "expiresAt": "2025-01-23T11:30:00Z"
  }
}
```

### 13. Voting Endpoints

#### 13.1 Create Proposal
```http
POST /v1/voting/proposals
{
  "title": "Upgrade Protocol to v2.0",
  "description": "Proposal to upgrade the protocol with new features",
  "options": ["Yes", "No", "Abstain"],
  "votingPeriod": "168h",
  "minimumQuorum": 0.1,
  "metadata": {
    "category": "protocol-upgrade",
    "impact": "high"
  }
}
```

#### 13.2 Cast Vote
```http
POST /v1/voting/proposals/{proposalId}/votes
{
  "option": "Yes",
  "weight": "1000.0",
  "reason": "This upgrade is necessary for scalability"
}
```

#### 13.3 Get Voting Results
```http
GET /v1/voting/proposals/{proposalId}/results

Response:
{
  "success": true,
  "data": {
    "proposalId": "prop_123",
    "status": "active",
    "results": {
      "Yes": {
        "votes": 15420,
        "weight": "1542000.0",
        "percentage": 67.8
      },
      "No": {
        "votes": 7234,
        "weight": "723400.0",
        "percentage": 31.8
      },
      "Abstain": {
        "votes": 89,
        "weight": "8900.0",
        "percentage": 0.4
      }
    },
    "totalVotes": 22743,
    "totalWeight": "2274300.0",
    "quorumReached": true,
    "endsAt": "2025-01-30T10:30:00Z"
  }
}
```

## GraphQL API

### 14. GraphQL Schema

#### 14.1 Type Definitions
```graphql
scalar DateTime
scalar JSON
scalar BigInt

# User Types
type User {
  id: ID!
  username: String!
  profile: UserProfile!
  roles: [Role!]!
  keys: [Key!]!
  transactions: TransactionConnection!
  createdAt: DateTime!
  updatedAt: DateTime!
}

type UserProfile {
  firstName: String
  lastName: String
  email: String!
  preferredLanguage: String!
  mfaEnabled: Boolean!
}

# Blockchain Types
type Transaction {
  id: ID!
  network: BlockchainNetwork!
  txHash: String!
  fromAddress: String!
  toAddress: String
  amount: BigInt!
  asset: String!
  status: TransactionStatus!
  blockNumber: BigInt
  blockHash: String
  gasUsed: BigInt
  confirmations: Int!
  createdAt: DateTime!
  confirmedAt: DateTime
}

# Key Management Types
type Key {
  id: ID!
  publicKey: String!
  keyType: KeyType!
  purpose: KeyPurpose!
  status: KeyStatus!
  createdAt: DateTime!
  expiresAt: DateTime
}

# Smart Contract Types
type SmartContract {
  id: ID!
  network: BlockchainNetwork!
  contractHash: String!
  name: String!
  manifest: JSON!
  deployedAt: DateTime!
  invocations: [ContractInvocation!]!
}

# Enums
enum BlockchainNetwork {
  NEO_N3
  NEO_X
}

enum TransactionStatus {
  PENDING
  CONFIRMED
  FAILED
}

enum KeyType {
  SECP256K1
  ED25519
  RSA
}

enum KeyPurpose {
  SIGNING
  ENCRYPTION
  AUTHENTICATION
}

enum KeyStatus {
  ACTIVE
  EXPIRED
  REVOKED
  COMPROMISED
}

# Connection Types for Pagination
type TransactionConnection {
  edges: [TransactionEdge!]!
  pageInfo: PageInfo!
  totalCount: Int!
}

type TransactionEdge {
  node: Transaction!
  cursor: String!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

#### 14.2 Query Operations
```graphql
type Query {
  # User Queries
  me: User
  user(id: ID!): User
  users(
    filter: UserFilter
    first: Int
    after: String
    orderBy: UserOrderBy
  ): UserConnection!
  
  # Blockchain Queries
  transaction(id: ID!): Transaction
  transactions(
    filter: TransactionFilter
    first: Int
    after: String
    orderBy: TransactionOrderBy
  ): TransactionConnection!
  
  # Key Management Queries
  key(id: ID!): Key
  keys(
    filter: KeyFilter
    first: Int
    after: String
  ): KeyConnection!
  
  # Smart Contract Queries
  contract(id: ID!): SmartContract
  contracts(
    network: BlockchainNetwork!
    filter: ContractFilter
    first: Int
    after: String
  ): SmartContractConnection!
  
  # Oracle Queries
  oracleData(requestId: ID!): OracleData
  oracleRequests(
    filter: OracleRequestFilter
    first: Int
    after: String
  ): OracleRequestConnection!
}
```

#### 14.3 Mutation Operations
```graphql
type Mutation {
  # Authentication
  login(input: LoginInput!): AuthPayload!
  logout: Boolean!
  refreshToken(input: RefreshTokenInput!): AuthPayload!
  
  # User Management
  createUser(input: CreateUserInput!): CreateUserPayload!
  updateUser(id: ID!, input: UpdateUserInput!): UpdateUserPayload!
  deleteUser(id: ID!): DeleteUserPayload!
  
  # Key Management
  createKey(input: CreateKeyInput!): CreateKeyPayload!
  rotateKey(id: ID!, input: RotateKeyInput!): RotateKeyPayload!
  revokeKey(id: ID!, input: RevokeKeyInput!): RevokeKeyPayload!
  
  # Blockchain Operations
  createTransaction(input: CreateTransactionInput!): CreateTransactionPayload!
  
  # Smart Contract Operations
  deployContract(input: DeployContractInput!): DeployContractPayload!
  invokeContract(input: InvokeContractInput!): InvokeContractPayload!
  
  # Oracle Operations
  createOracleRequest(input: CreateOracleRequestInput!): CreateOracleRequestPayload!
}
```

#### 14.4 Subscription Operations
```graphql
type Subscription {
  # Real-time Updates
  transactionUpdated(userId: ID): Transaction!
  blockchainEvents(network: BlockchainNetwork!): BlockchainEvent!
  oracleDataUpdated(requestId: ID!): OracleData!
  
  # Notifications
  userNotifications(userId: ID!): Notification!
  systemAlerts: SystemAlert!
  
  # Key Management
  keyStatusChanged(userId: ID): Key!
}
```

## WebSocket API

### 15. Real-time Communication

#### 15.1 Connection Protocol
```javascript
const ws = new WebSocket('wss://api.neo-service-layer.com/v1/ws');

// Authentication
ws.send(JSON.stringify({
  type: 'auth',
  token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
}));

// Subscribe to events
ws.send(JSON.stringify({
  type: 'subscribe',
  channel: 'transactions',
  filters: {
    network: 'neo-n3',
    address: 'NXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXxx'
  }
}));
```

#### 15.2 Message Format
```json
{
  "id": "msg_123",
  "type": "event",
  "channel": "transactions",
  "timestamp": "2025-01-23T10:30:00Z",
  "data": {
    "txHash": "0x123abc...",
    "status": "confirmed",
    "blockNumber": 1234567
  }
}
```

#### 15.3 Available Channels
- `transactions`: Transaction status updates
- `blocks`: New block notifications
- `contracts`: Smart contract events
- `oracle`: Oracle data updates
- `system`: System notifications
- `user`: User-specific events

## Rate Limiting

### 16. Rate Limiting Strategy

#### 16.1 Rate Limit Tiers
```json
{
  "free": {
    "requests_per_minute": 100,
    "requests_per_hour": 5000,
    "requests_per_day": 50000
  },
  "pro": {
    "requests_per_minute": 1000,
    "requests_per_hour": 50000,
    "requests_per_day": 1000000
  },
  "enterprise": {
    "requests_per_minute": 10000,
    "requests_per_hour": 500000,
    "requests_per_day": 10000000
  }
}
```

#### 16.2 Rate Limit Headers
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1643894400
X-RateLimit-Retry-After: 60
```

## Error Codes

### 17. Standard Error Codes

#### 17.1 Authentication Errors (AUTH_)
- `AUTH_TOKEN_MISSING`: Authentication token not provided
- `AUTH_TOKEN_INVALID`: Invalid or malformed token
- `AUTH_TOKEN_EXPIRED`: Token has expired
- `AUTH_CREDENTIALS_INVALID`: Invalid username/password
- `AUTH_MFA_REQUIRED`: Multi-factor authentication required
- `AUTH_MFA_INVALID`: Invalid MFA code

#### 17.2 Authorization Errors (AUTHZ_)
- `AUTHZ_INSUFFICIENT_PERMISSIONS`: User lacks required permissions
- `AUTHZ_RESOURCE_ACCESS_DENIED`: Access to specific resource denied
- `AUTHZ_RATE_LIMIT_EXCEEDED`: Rate limit exceeded

#### 17.3 Validation Errors (VALIDATION_)
- `VALIDATION_FAILED`: General validation failure
- `VALIDATION_REQUIRED_FIELD`: Required field missing
- `VALIDATION_INVALID_FORMAT`: Invalid field format
- `VALIDATION_CONSTRAINT_VIOLATION`: Business rule violation

#### 17.4 Blockchain Errors (BLOCKCHAIN_)
- `BLOCKCHAIN_NETWORK_UNAVAILABLE`: Blockchain network unreachable
- `BLOCKCHAIN_INSUFFICIENT_FUNDS`: Insufficient balance for operation
- `BLOCKCHAIN_TRANSACTION_FAILED`: Transaction execution failed
- `BLOCKCHAIN_CONTRACT_ERROR`: Smart contract execution error

#### 17.5 System Errors (SYSTEM_)
- `SYSTEM_INTERNAL_ERROR`: Internal server error
- `SYSTEM_SERVICE_UNAVAILABLE`: Service temporarily unavailable
- `SYSTEM_MAINTENANCE_MODE`: System under maintenance

## Versioning Strategy

### 18. API Versioning

#### 18.1 URL Versioning (Primary)
```
/v1/users/123
/v2/users/123
```

#### 18.2 Header Versioning (Alternative)
```http
API-Version: v1
Accept: application/vnd.neo-service-layer.v1+json
```

#### 18.3 Version Lifecycle
- **v1**: Current stable version
- **v2**: Next version in development
- **Deprecation**: 12-month notice period
- **Sunset**: Complete version removal

This comprehensive API design specification provides a complete foundation for building robust, scalable, and secure APIs for the Neo Service Layer platform.