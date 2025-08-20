# Neo Service Layer - API Design Specification

## 1. API Design Principles

### Core Principles
- **RESTful**: Follow REST architectural constraints
- **Versioned**: Support multiple API versions simultaneously
- **Consistent**: Uniform interface across all services
- **Secure**: Authentication and authorization on all endpoints
- **Observable**: Comprehensive logging and metrics
- **Documented**: OpenAPI/Swagger specifications

## 2. API Structure

### 2.1 Base URL Structure
```
https://api.neo-service.io/v{version}/{service}/{resource}
```

Examples:
- `https://api.neo-service.io/v1/auth/users`
- `https://api.neo-service.io/v1/compute/jobs`
- `https://api.neo-service.io/v1/storage/files`

### 2.2 API Versioning Strategy

```http
# URL Path Versioning (Primary)
GET /v1/users/123

# Header Versioning (Alternative)
GET /users/123
X-API-Version: 1

# Query Parameter Versioning (Fallback)
GET /users/123?version=1
```

## 3. Authentication & Authorization

### 3.1 Authentication Flow

```yaml
openapi: 3.0.0
paths:
  /v1/auth/login:
    post:
      summary: Authenticate user
      requestBody:
        content:
          application/json:
            schema:
              type: object
              required:
                - username
                - password
              properties:
                username:
                  type: string
                  example: "user@example.com"
                password:
                  type: string
                  format: password
                  example: "SecurePassword123!"
                mfa_code:
                  type: string
                  example: "123456"
      responses:
        200:
          description: Successful authentication
          content:
            application/json:
              schema:
                type: object
                properties:
                  access_token:
                    type: string
                    example: "eyJhbGciOiJSUzI1NiIs..."
                  refresh_token:
                    type: string
                    example: "eyJhbGciOiJSUzI1NiIs..."
                  token_type:
                    type: string
                    example: "Bearer"
                  expires_in:
                    type: integer
                    example: 3600
```

### 3.2 Authorization Headers

```http
# Bearer Token Authentication
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...

# API Key Authentication (for service-to-service)
X-API-Key: YOUR_API_KEY_HERE

# Request Signature (for webhooks)
X-Signature: sha256=3f4e8b2a5c6d7e8f9a0b1c2d3e4f5g6h
```

## 4. Service APIs

### 4.1 Compute Service API

```typescript
// TypeScript SDK Example
interface ComputeServiceAPI {
  // Job Management
  createJob(request: CreateJobRequest): Promise<Job>;
  getJob(jobId: string): Promise<Job>;
  listJobs(filter?: JobFilter): Promise<PagedResult<Job>>;
  cancelJob(jobId: string): Promise<void>;
  
  // Enclave Operations
  executeInEnclave(request: EnclaveExecutionRequest): Promise<EnclaveResult>;
  getEnclaveStatus(): Promise<EnclaveStatus>;
  attestEnclave(): Promise<AttestationReport>;
}

interface CreateJobRequest {
  name: string;
  type: JobType;
  parameters: Map<string, any>;
  priority?: Priority;
  scheduling?: SchedulingOptions;
  resources?: ResourceRequirements;
}

interface Job {
  id: string;
  name: string;
  status: JobStatus;
  progress: number;
  result?: any;
  error?: Error;
  created_at: Date;
  updated_at: Date;
  completed_at?: Date;
}
```

### 4.2 Storage Service API

```yaml
openapi: 3.0.0
paths:
  /v1/storage/files:
    post:
      summary: Upload file
      requestBody:
        content:
          multipart/form-data:
            schema:
              type: object
              properties:
                file:
                  type: string
                  format: binary
                metadata:
                  type: object
                  properties:
                    name:
                      type: string
                    description:
                      type: string
                    tags:
                      type: array
                      items:
                        type: string
                encryption:
                  type: object
                  properties:
                    enabled:
                      type: boolean
                    key_id:
                      type: string
      responses:
        201:
          description: File uploaded successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/File'
                
  /v1/storage/files/{fileId}:
    get:
      summary: Download file
      parameters:
        - name: fileId
          in: path
          required: true
          schema:
            type: string
        - name: version
          in: query
          schema:
            type: string
      responses:
        200:
          description: File content
          content:
            application/octet-stream:
              schema:
                type: string
                format: binary
```

### 4.3 Oracle Service API

```csharp
// C# Client Example
public interface IOracleServiceClient
{
    // Data Feed Management
    Task<DataFeed> CreateDataFeedAsync(CreateDataFeedRequest request);
    Task<DataFeed> GetDataFeedAsync(string feedId);
    Task<IEnumerable<DataFeed>> ListDataFeedsAsync(DataFeedFilter filter = null);
    Task<DataFeed> UpdateDataFeedAsync(string feedId, UpdateDataFeedRequest request);
    Task DeleteDataFeedAsync(string feedId);
    
    // Price Feeds
    Task<PriceData> GetPriceAsync(string symbol);
    Task<IEnumerable<PriceData>> GetPricesAsync(params string[] symbols);
    Task<PriceHistory> GetPriceHistoryAsync(string symbol, TimeRange range);
    
    // Custom Oracle Requests
    Task<OracleResponse> RequestDataAsync(OracleRequest request);
    Task<string> SubscribeToFeedAsync(string feedId, WebhookConfig webhook);
    Task UnsubscribeFromFeedAsync(string subscriptionId);
}

public class CreateDataFeedRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DataSourceType SourceType { get; set; }
    public string SourceUrl { get; set; }
    public TimeSpan UpdateInterval { get; set; }
    public Dictionary<string, object> Configuration { get; set; }
}
```

### 4.4 Blockchain Service API

```graphql
# GraphQL API for Blockchain Operations
type Query {
  # Account queries
  account(address: String!): Account
  accounts(filter: AccountFilter): [Account!]!
  
  # Transaction queries
  transaction(hash: String!): Transaction
  transactions(
    filter: TransactionFilter
    first: Int = 10
    after: String
  ): TransactionConnection!
  
  # Block queries
  block(height: Int, hash: String): Block
  blocks(
    first: Int = 10
    after: String
  ): BlockConnection!
  
  # Smart contract queries
  contract(address: String!): SmartContract
  contractState(address: String!, key: String!): ContractState
}

type Mutation {
  # Transaction operations
  sendTransaction(input: SendTransactionInput!): Transaction!
  
  # Smart contract operations
  deployContract(input: DeployContractInput!): SmartContract!
  invokeContract(input: InvokeContractInput!): InvocationResult!
  
  # Cross-chain operations
  initiateCrossChainTransfer(
    input: CrossChainTransferInput!
  ): CrossChainTransfer!
}

type Subscription {
  # Real-time updates
  newBlocks: Block!
  newTransactions(filter: TransactionFilter): Transaction!
  contractEvents(address: String!): ContractEvent!
}

input SendTransactionInput {
  from: String!
  to: String!
  amount: BigInt!
  asset: String!
  data: String
  gasPrice: BigInt
  gasLimit: BigInt
}
```

## 5. Common Patterns

### 5.1 Pagination

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "page_size": 20,
    "total_pages": 10,
    "total_items": 200,
    "has_next": true,
    "has_previous": false
  },
  "links": {
    "self": "/v1/resource?page=1&page_size=20",
    "next": "/v1/resource?page=2&page_size=20",
    "last": "/v1/resource?page=10&page_size=20"
  }
}
```

### 5.2 Filtering & Sorting

```http
# Filtering
GET /v1/transactions?status=completed&amount_gte=100&created_after=2024-01-01

# Sorting
GET /v1/transactions?sort=-created_at,amount

# Field selection
GET /v1/users/123?fields=id,name,email,profile.avatar
```

### 5.3 Batch Operations

```json
POST /v1/batch
{
  "operations": [
    {
      "method": "GET",
      "path": "/v1/users/123",
      "id": "get-user"
    },
    {
      "method": "POST",
      "path": "/v1/transactions",
      "body": {
        "from": "{get-user.wallet_address}",
        "to": "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
        "amount": "100"
      },
      "id": "create-transaction"
    }
  ]
}
```

## 6. Error Handling

### 6.1 Error Response Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed for the request",
    "details": [
      {
        "field": "email",
        "code": "INVALID_FORMAT",
        "message": "Email format is invalid"
      },
      {
        "field": "password",
        "code": "TOO_WEAK",
        "message": "Password does not meet security requirements"
      }
    ],
    "request_id": "req_1234567890",
    "documentation_url": "https://docs.neo-service.io/errors/VALIDATION_ERROR"
  }
}
```

### 6.2 HTTP Status Codes

| Status Code | Meaning | Usage |
|------------|---------|-------|
| 200 | OK | Successful GET, PUT |
| 201 | Created | Successful POST |
| 202 | Accepted | Async operation started |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Invalid request format |
| 401 | Unauthorized | Missing/invalid auth |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Resource conflict |
| 422 | Unprocessable Entity | Validation error |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service down/maintenance |

## 7. WebSocket APIs

### 7.1 Real-time Event Streaming

```javascript
// WebSocket connection for real-time updates
const ws = new WebSocket('wss://api.neo-service.io/v1/stream');

// Authentication
ws.send(JSON.stringify({
  type: 'auth',
  token: 'Bearer eyJhbGciOiJSUzI1NiIs...'
}));

// Subscribe to events
ws.send(JSON.stringify({
  type: 'subscribe',
  channels: [
    'transactions:0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb',
    'blocks:new',
    'oracle:ETH/USD'
  ]
}));

// Handle messages
ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  switch(message.type) {
    case 'transaction':
      handleTransaction(message.data);
      break;
    case 'block':
      handleNewBlock(message.data);
      break;
    case 'price':
      handlePriceUpdate(message.data);
      break;
  }
};
```

### 7.2 Command Execution Protocol

```json
// Request
{
  "id": "cmd_123",
  "method": "compute.execute",
  "params": {
    "operation": "matrix_multiply",
    "input": [[1,2],[3,4]],
    "enclave": true
  }
}

// Response
{
  "id": "cmd_123",
  "result": {
    "output": [[7,10],[15,22]],
    "computation_time": 12.5,
    "attestation": "SGX_ATTESTATION_REPORT..."
  }
}
```

## 8. Rate Limiting

### 8.1 Rate Limit Headers

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1640995200
X-RateLimit-Retry-After: 60
```

### 8.2 Rate Limit Tiers

| Tier | Requests/Hour | Burst | Use Case |
|------|--------------|-------|----------|
| Free | 1,000 | 20/sec | Development |
| Basic | 10,000 | 50/sec | Small apps |
| Pro | 100,000 | 200/sec | Production |
| Enterprise | Unlimited | Custom | High-volume |

## 9. API Documentation

### 9.1 OpenAPI Specification

```yaml
openapi: 3.0.0
info:
  title: Neo Service Layer API
  version: 1.0.0
  description: Enterprise blockchain service platform API
  contact:
    name: API Support
    email: api@neo-service.io
  license:
    name: MIT
servers:
  - url: https://api.neo-service.io/v1
    description: Production
  - url: https://staging-api.neo-service.io/v1
    description: Staging
  - url: http://localhost:8080/v1
    description: Development
security:
  - bearerAuth: []
  - apiKey: []
components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
    apiKey:
      type: apiKey
      in: header
      name: X-API-Key
```

### 9.2 SDK Generation

```bash
# Generate TypeScript SDK
openapi-generator generate \
  -i openapi.yaml \
  -g typescript-axios \
  -o ./sdk/typescript

# Generate Python SDK
openapi-generator generate \
  -i openapi.yaml \
  -g python \
  -o ./sdk/python

# Generate C# SDK
openapi-generator generate \
  -i openapi.yaml \
  -g csharp-netcore \
  -o ./sdk/csharp
```

## 10. Testing & Monitoring

### 10.1 API Testing Strategy

```python
# Python test example using pytest
import pytest
import requests
from datetime import datetime

class TestComputeAPI:
    base_url = "https://api.neo-service.io/v1"
    
    @pytest.fixture
    def auth_headers(self):
        response = requests.post(
            f"{self.base_url}/auth/login",
            json={"username": "test@example.com", "password": "test123"}
        )
        token = response.json()["access_token"]
        return {"Authorization": f"Bearer {token}"}
    
    def test_create_compute_job(self, auth_headers):
        response = requests.post(
            f"{self.base_url}/compute/jobs",
            headers=auth_headers,
            json={
                "name": "Test Job",
                "type": "BATCH_PROCESSING",
                "parameters": {"input": [1, 2, 3]}
            }
        )
        assert response.status_code == 201
        assert "id" in response.json()
```

### 10.2 API Monitoring Metrics

```yaml
# Prometheus metrics for API monitoring
metrics:
  - name: http_requests_total
    type: counter
    labels: [method, endpoint, status]
    
  - name: http_request_duration_seconds
    type: histogram
    labels: [method, endpoint]
    buckets: [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10]
    
  - name: api_errors_total
    type: counter
    labels: [endpoint, error_code]
    
  - name: rate_limit_exceeded_total
    type: counter
    labels: [client_id, endpoint]
```

## Conclusion

This API design specification provides a comprehensive framework for building consistent, secure, and scalable APIs for the Neo Service Layer platform. The design emphasizes RESTful principles, comprehensive documentation, robust error handling, and support for both synchronous and asynchronous communication patterns. Following these specifications ensures a unified developer experience across all services while maintaining the flexibility to evolve the API over time.