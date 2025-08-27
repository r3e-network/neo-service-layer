# Service Boundaries and API Specifications

## Overview

This document defines the precise boundaries, interfaces, and API specifications for each microservice in the Neo Service Layer architecture. Each service is designed with clear responsibilities and well-defined APIs to ensure loose coupling and high cohesion.

## Service Boundary Principles

### Domain-Driven Design (DDD)
- **Bounded Context**: Each service represents a distinct business domain
- **Ubiquitous Language**: Consistent terminology within each service
- **Aggregate Boundaries**: Services own complete business entities
- **Anti-Corruption Layer**: Clear interfaces prevent domain contamination

### Service Design Rules
1. **Single Responsibility**: One business capability per service
2. **Data Ownership**: Each service owns its data exclusively
3. **Autonomous**: Can be developed, deployed, and scaled independently
4. **Resilient**: Graceful degradation and fault tolerance
5. **Observable**: Comprehensive logging, metrics, and tracing

## Service Definitions

## 1. Authentication Service (`neo-auth-service`)

### Bounded Context
User identity and access management, session handling, multi-factor authentication.

### Data Ownership
- User profiles and credentials
- Authentication sessions
- MFA secrets and backup codes
- Login attempt history
- Password reset tokens

### API Specification

#### REST Endpoints
```yaml
paths:
  /api/auth/login:
    post:
      summary: Authenticate user
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                username: string
                password: string
                mfaCode: string (optional)
      responses:
        200:
          description: Authentication successful
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AuthenticationResult'
        401:
          description: Invalid credentials
        429:
          description: Rate limit exceeded

  /api/auth/refresh:
    post:
      summary: Refresh access token
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                refreshToken: string
      responses:
        200:
          description: Token refreshed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TokenPair'

  /api/auth/logout:
    post:
      summary: Logout user
      security:
        - bearerAuth: []
      responses:
        200:
          description: Logout successful

  /api/auth/register:
    post:
      summary: Register new user
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/UserRegistrationRequest'
      responses:
        201:
          description: User registered successfully
        400:
          description: Validation error
        409:
          description: User already exists

  /api/auth/mfa/setup:
    post:
      summary: Setup multi-factor authentication
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                type: 
                  type: string
                  enum: [totp, sms, email]
      responses:
        200:
          description: MFA setup successful
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/MfaSetupResult'

  /api/auth/sessions:
    get:
      summary: Get active user sessions
      security:
        - bearerAuth: []
      responses:
        200:
          description: Active sessions retrieved
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/SessionInfo'
```

#### gRPC Service Definition
```protobuf
syntax = "proto3";
package neo.auth.v1;

service AuthenticationService {
  rpc Authenticate(AuthenticateRequest) returns (AuthenticateResponse);
  rpc ValidateToken(ValidateTokenRequest) returns (ValidateTokenResponse);
  rpc RefreshToken(RefreshTokenRequest) returns (RefreshTokenResponse);
  rpc RevokeToken(RevokeTokenRequest) returns (RevokeTokenResponse);
}

message AuthenticateRequest {
  string username = 1;
  string password = 2;
  optional string mfa_code = 3;
}

message AuthenticateResponse {
  bool success = 1;
  string access_token = 2;
  string refresh_token = 3;
  int64 expires_at = 4;
  repeated string roles = 5;
  optional string error_message = 6;
}
```

### Events Published
```yaml
AuthenticationSucceeded:
  properties:
    userId: string
    timestamp: datetime
    ipAddress: string
    userAgent: string

AuthenticationFailed:
  properties:
    username: string
    timestamp: datetime
    ipAddress: string
    reason: string

PasswordChanged:
  properties:
    userId: string
    timestamp: datetime

AccountLocked:
  properties:
    userId: string
    reason: string
    timestamp: datetime
```

## 2. Oracle Service (`neo-oracle-service`)

### Bounded Context
External data integration, data source management, data validation and caching.

### Data Ownership
- Data source configurations
- Oracle subscriptions
- Cached data responses
- Data validation rules
- Request/response logs

### API Specification

#### REST Endpoints
```yaml
paths:
  /api/oracle/data:
    post:
      summary: Fetch data from external source
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/OracleRequest'
      responses:
        200:
          description: Data fetched successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/OracleResponse'
        400:
          description: Invalid request
        503:
          description: Data source unavailable

  /api/oracle/batch:
    post:
      summary: Batch data fetch
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                requests:
                  type: array
                  items:
                    $ref: '#/components/schemas/OracleRequest'
      responses:
        200:
          description: Batch processed
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/OracleResponse'

  /api/oracle/subscribe:
    post:
      summary: Subscribe to data feed
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/SubscriptionRequest'
      responses:
        201:
          description: Subscription created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SubscriptionResult'

  /api/oracle/datasources:
    get:
      summary: List available data sources
      security:
        - bearerAuth: []
      parameters:
        - name: page
          in: query
          schema:
            type: integer
            default: 1
        - name: limit
          in: query
          schema:
            type: integer
            default: 20
      responses:
        200:
          description: Data sources listed
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DataSourcesResult'

    post:
      summary: Create new data source
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateDataSourceRequest'
      responses:
        201:
          description: Data source created
```

#### WebSocket API
```yaml
websocket:
  /ws/oracle/feed/{subscriptionId}:
    summary: Real-time data feed
    security:
      - bearerAuth: []
    messages:
      DataUpdate:
        payload:
          type: object
          properties:
            subscriptionId: string
            timestamp: datetime
            data: object
            source: string
      
      SubscriptionStatus:
        payload:
          type: object
          properties:
            subscriptionId: string
            status: string
            message: string
```

### Events Published
```yaml
DataFetched:
  properties:
    requestId: string
    dataSource: string
    timestamp: datetime
    responseTime: number
    success: boolean

DataSourceCreated:
  properties:
    dataSourceId: string
    name: string
    url: string
    createdBy: string
    timestamp: datetime

SubscriptionCreated:
  properties:
    subscriptionId: string
    userId: string
    dataSource: string
    filters: object
    timestamp: datetime
```

## 3. Compute Service (`neo-compute-service`)

### Bounded Context
Secure computation within Intel SGX enclaves, multi-party computation, verifiable computing.

### Data Ownership
- Computation jobs and results
- Enclave configurations
- Sealed data within enclaves
- Computation logs and metrics

### API Specification

#### REST Endpoints
```yaml
paths:
  /api/compute/jobs:
    post:
      summary: Submit computation job
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ComputeJobRequest'
      responses:
        202:
          description: Job submitted
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ComputeJobResponse'
        400:
          description: Invalid job specification

    get:
      summary: List computation jobs
      security:
        - bearerAuth: []
      parameters:
        - name: status
          in: query
          schema:
            type: string
            enum: [pending, running, completed, failed]
        - name: userId
          in: query
          schema:
            type: string
      responses:
        200:
          description: Jobs listed
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/ComputeJob'

  /api/compute/jobs/{jobId}:
    get:
      summary: Get job status and results
      security:
        - bearerAuth: []
      parameters:
        - name: jobId
          in: path
          required: true
          schema:
            type: string
      responses:
        200:
          description: Job details retrieved
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ComputeJobDetails'
        404:
          description: Job not found

  /api/compute/enclaves:
    get:
      summary: List available enclaves
      security:
        - bearerAuth: []
      responses:
        200:
          description: Enclaves listed
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/EnclaveInfo'

  /api/compute/attestation:
    post:
      summary: Verify enclave attestation
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/AttestationRequest'
      responses:
        200:
          description: Attestation verified
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AttestationResult'
```

#### gRPC Service Definition
```protobuf
syntax = "proto3";
package neo.compute.v1;

service ComputeService {
  rpc SubmitJob(SubmitJobRequest) returns (SubmitJobResponse);
  rpc GetJobStatus(GetJobStatusRequest) returns (GetJobStatusResponse);
  rpc GetJobResults(GetJobResultsRequest) returns (GetJobResultsResponse);
  rpc CancelJob(CancelJobRequest) returns (CancelJobResponse);
  rpc ListEnclaves(ListEnclavesRequest) returns (ListEnclavesResponse);
}

message SubmitJobRequest {
  string job_type = 1;
  bytes input_data = 2;
  map<string, string> parameters = 3;
  string enclave_id = 4;
  repeated string participants = 5; // For MPC
}

message SubmitJobResponse {
  string job_id = 1;
  JobStatus status = 2;
  string message = 3;
}

enum JobStatus {
  JOB_STATUS_UNSPECIFIED = 0;
  JOB_STATUS_PENDING = 1;
  JOB_STATUS_RUNNING = 2;
  JOB_STATUS_COMPLETED = 3;
  JOB_STATUS_FAILED = 4;
  JOB_STATUS_CANCELLED = 5;
}
```

### Events Published
```yaml
JobSubmitted:
  properties:
    jobId: string
    userId: string
    jobType: string
    enclaveId: string
    timestamp: datetime

JobCompleted:
  properties:
    jobId: string
    duration: number
    success: boolean
    resultSize: number
    timestamp: datetime

EnclaveStarted:
  properties:
    enclaveId: string
    attestationHash: string
    timestamp: datetime
```

## 4. Storage Service (`neo-storage-service`)

### Bounded Context
Encrypted data storage, access control, versioning, and data lifecycle management.

### Data Ownership
- Encrypted data objects
- Access control policies
- Data versions and history
- Storage metadata

### API Specification

#### REST Endpoints
```yaml
paths:
  /api/storage/objects:
    post:
      summary: Store encrypted object
      security:
        - bearerAuth: []
      requestBody:
        required: true
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
                    name: string
                    contentType: string
                    accessLevel: string
                    tags: 
                      type: array
                      items:
                        type: string
      responses:
        201:
          description: Object stored successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/StorageObject'

    get:
      summary: List stored objects
      security:
        - bearerAuth: []
      parameters:
        - name: tags
          in: query
          schema:
            type: array
            items:
              type: string
        - name: accessLevel
          in: query
          schema:
            type: string
        - name: page
          in: query
          schema:
            type: integer
            default: 1
      responses:
        200:
          description: Objects listed
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/StorageObjectList'

  /api/storage/objects/{objectId}:
    get:
      summary: Retrieve stored object
      security:
        - bearerAuth: []
      parameters:
        - name: objectId
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
          description: Object retrieved
          content:
            application/octet-stream:
              schema:
                type: string
                format: binary
        403:
          description: Access denied
        404:
          description: Object not found

    put:
      summary: Update stored object
      security:
        - bearerAuth: []
      parameters:
        - name: objectId
          in: path
          required: true
          schema:
            type: string
      requestBody:
        required: true
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
      responses:
        200:
          description: Object updated successfully

    delete:
      summary: Delete stored object
      security:
        - bearerAuth: []
      parameters:
        - name: objectId
          in: path
          required: true
          schema:
            type: string
      responses:
        204:
          description: Object deleted successfully
        403:
          description: Access denied
        404:
          description: Object not found

  /api/storage/objects/{objectId}/access:
    post:
      summary: Grant object access
      security:
        - bearerAuth: []
      parameters:
        - name: objectId
          in: path
          required: true
          schema:
            type: string
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/GrantAccessRequest'
      responses:
        200:
          description: Access granted

  /api/storage/objects/{objectId}/versions:
    get:
      summary: List object versions
      security:
        - bearerAuth: []
      parameters:
        - name: objectId
          in: path
          required: true
          schema:
            type: string
      responses:
        200:
          description: Versions listed
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/ObjectVersion'
```

### Events Published
```yaml
ObjectStored:
  properties:
    objectId: string
    userId: string
    size: number
    contentType: string
    timestamp: datetime

ObjectAccessed:
  properties:
    objectId: string
    userId: string
    accessType: string
    timestamp: datetime
    ipAddress: string

ObjectDeleted:
  properties:
    objectId: string
    userId: string
    timestamp: datetime
```

## Cross-Service Communication Patterns

### Service-to-Service Authentication
All internal service communications use mTLS with service-specific certificates:

```yaml
# Example: Oracle service calling Authentication service
internal_auth:
  method: mTLS
  certificate_authority: neo-internal-ca
  service_certificate: oracle-service.crt
  service_key: oracle-service.key
  verification:
    - subject: CN=neo-auth-service
    - issuer: neo-internal-ca
```

### Event Schema Registry
Centralized schema registry for event definitions:

```yaml
schema_registry:
  url: http://schema-registry:8081
  subjects:
    - authentication-events-v1
    - oracle-events-v1
    - compute-events-v1
    - storage-events-v1
  compatibility: BACKWARD
```

### Circuit Breaker Configuration
```yaml
circuit_breakers:
  auth_service:
    failure_threshold: 5
    timeout: 30s
    reset_timeout: 60s
    
  oracle_service:
    failure_threshold: 3
    timeout: 10s
    reset_timeout: 30s
```

## API Gateway Configuration

### Route Definitions
```yaml
routes:
  - name: authentication
    paths:
      - /api/auth/*
    service: neo-auth-service:8080
    rate_limit:
      requests_per_minute: 60
      burst: 10
    auth_required: false
    
  - name: oracle
    paths:
      - /api/oracle/*
    service: neo-oracle-service:8080
    rate_limit:
      requests_per_minute: 100
      burst: 20
    auth_required: true
    
  - name: compute
    paths:
      - /api/compute/*
    service: neo-compute-service:8080
    rate_limit:
      requests_per_minute: 30
      burst: 5
    auth_required: true
    role_required: compute_user
    
  - name: storage
    paths:
      - /api/storage/*
    service: neo-storage-service:8080
    rate_limit:
      requests_per_minute: 200
      burst: 50
    auth_required: true
```

### Security Policies
```yaml
security:
  cors:
    allowed_origins:
      - https://app.neo-service-layer.com
      - https://admin.neo-service-layer.com
    allowed_methods: [GET, POST, PUT, DELETE, OPTIONS]
    allowed_headers: [Authorization, Content-Type, X-Correlation-ID]
    
  rate_limiting:
    global:
      requests_per_second: 1000
      burst: 100
    per_ip:
      requests_per_minute: 100
      burst: 20
      
  authentication:
    jwt:
      secret_key_source: vault://secrets/jwt-key
      algorithms: [RS256]
      audience: neo-service-layer
      issuer: neo-auth-service
```

## Data Contracts

### Inter-Service Data Models
```yaml
# Shared data models used across services
shared_models:
  User:
    properties:
      id: string (uuid)
      username: string
      email: string
      roles: array[string]
      created_at: datetime
      updated_at: datetime
      
  AccessToken:
    properties:
      token: string (jwt)
      user_id: string (uuid)
      expires_at: datetime
      scopes: array[string]
      
  JobRequest:
    properties:
      id: string (uuid)
      user_id: string (uuid)
      type: string
      parameters: object
      created_at: datetime
```

This comprehensive service boundary definition ensures that each microservice has clear responsibilities, well-defined APIs, and proper isolation while maintaining the ability to work together as a cohesive system.