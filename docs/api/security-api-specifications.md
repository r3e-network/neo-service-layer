# Security API Specifications

## Overview

This document provides comprehensive OpenAPI/Swagger specifications for all security-related APIs in the Neo Service Layer platform. These APIs implement enterprise-grade security controls including input validation, encryption, rate limiting, and threat detection.

## Base Configuration

```yaml
openapi: 3.0.3
info:
  title: Neo Service Layer Security API
  description: |
    Comprehensive security API providing input validation, encryption, 
    authentication, and threat detection services. All critical security 
    vulnerabilities have been addressed in this implementation.
  version: "2.0.0"
  license:
    name: MIT
    url: https://opensource.org/licenses/MIT
  contact:
    name: Neo Security Team
    email: security@neo.org

servers:
  - url: https://api.neo-service-layer.com/v2
    description: Production server
  - url: https://staging.neo-service-layer.com/v2
    description: Staging server
  - url: http://localhost:5000
    description: Development server

security:
  - BearerAuth: []
  - ApiKeyAuth: []

components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: JWT token for authenticated requests
    ApiKeyAuth:
      type: apiKey
      in: header
      name: X-API-Key
      description: API key for service authentication
```

## Security Service API

### Input Validation API

#### POST /api/security/validate

Validates input data against multiple security threats including SQL injection, XSS, and code injection.

```yaml
paths:
  /api/security/validate:
    post:
      tags:
        - Security Validation
      summary: Validate input for security threats
      description: |
        Performs comprehensive security validation of input data against:
        - SQL injection attacks (25+ patterns)
        - Cross-site scripting (XSS) attacks (18+ patterns)
        - Code injection attempts (12+ patterns)
        - Path traversal attacks
        - Input size validation
        - Format validation
      operationId: validateInput
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ValidationRequest'
            examples:
              safe_input:
                summary: Safe input example
                value:
                  input: "Hello, world! This is safe text."
                  options:
                    checkSqlInjection: true
                    checkXss: true
                    checkCodeInjection: true
                    checkPathTraversal: true
                    maxInputSize: 1048576
              sql_injection_attempt:
                summary: SQL injection attempt (will be blocked)
                value:
                  input: "'; DROP TABLE users; --"
                  options:
                    checkSqlInjection: true
                    checkXss: false
                    checkCodeInjection: false
              xss_attempt:
                summary: XSS attempt (will be blocked)
                value:
                  input: "<script>alert('xss')</script>"
                  options:
                    checkSqlInjection: false
                    checkXss: true
                    checkCodeInjection: false
      responses:
        '200':
          description: Validation completed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SecurityValidationResult'
              examples:
                safe_result:
                  summary: Safe input validation result
                  value:
                    isValid: true
                    hasSecurityThreats: false
                    threatTypes: []
                    riskScore: 0.0
                    securityLevel: "Safe"
                    validationErrors: []
                    processingTime: "25ms"
                threat_detected:
                  summary: Threat detected validation result
                  value:
                    isValid: false
                    hasSecurityThreats: true
                    threatTypes: ["SqlInjection"]
                    riskScore: 0.95
                    securityLevel: "Critical"
                    validationErrors: ["Input contains potentially dangerous SQL patterns"]
                    processingTime: "18ms"
        '400':
          description: Invalid request parameters
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
        '429':
          description: Rate limit exceeded
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/RateLimitErrorResponse'
        '500':
          description: Internal server error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'

components:
  schemas:
    ValidationRequest:
      type: object
      required:
        - input
        - options
      properties:
        input:
          type: object
          description: Input data to validate (string, object, or array)
          example: "SELECT * FROM users WHERE id = 1"
        options:
          $ref: '#/components/schemas/SecurityValidationOptions'
      example:
        input: "Normal user input text"
        options:
          checkSqlInjection: true
          checkXss: true
          checkCodeInjection: true
          checkPathTraversal: false
          maxInputSize: 1048576

    SecurityValidationOptions:
      type: object
      properties:
        checkSqlInjection:
          type: boolean
          description: Enable SQL injection detection
          default: true
        checkXss:
          type: boolean
          description: Enable XSS attack detection
          default: true
        checkCodeInjection:
          type: boolean
          description: Enable code injection detection
          default: true
        checkPathTraversal:
          type: boolean
          description: Enable path traversal detection
          default: true
        maxInputSize:
          type: integer
          description: Maximum input size in bytes
          minimum: 1
          maximum: 10485760
          default: 1048576
        strictMode:
          type: boolean
          description: Enable strict validation mode
          default: false

    SecurityValidationResult:
      type: object
      properties:
        isValid:
          type: boolean
          description: Whether the input is considered safe
        hasSecurityThreats:
          type: boolean
          description: Whether any security threats were detected
        threatTypes:
          type: array
          items:
            type: string
            enum: [SqlInjection, XssAttack, CodeInjection, PathTraversal]
          description: List of detected threat types
        riskScore:
          type: number
          format: float
          minimum: 0.0
          maximum: 1.0
          description: Risk assessment score (0.0 = safe, 1.0 = critical)
        securityLevel:
          type: string
          enum: [Safe, Low, Medium, High, Critical, Unknown]
          description: Overall security level assessment
        validationErrors:
          type: array
          items:
            type: string
          description: Detailed validation error messages
        processingTime:
          type: string
          description: Validation processing time
          example: "25ms"
        errorMessage:
          type: string
          description: Error message if validation failed
          nullable: true
      required:
        - isValid
        - hasSecurityThreats
        - riskScore
        - securityLevel
```

### Input Sanitization API

#### POST /api/security/sanitize

Sanitizes input data to remove or encode potentially dangerous content.

```yaml
  /api/security/sanitize:
    post:
      tags:
        - Security Sanitization
      summary: Sanitize input data
      description: |
        Sanitizes input data by:
        - HTML encoding dangerous characters
        - JavaScript encoding special characters
        - SQL parameter encoding
        - Removing dangerous characters
        - Truncating to maximum length
      operationId: sanitizeInput
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/SanitizationRequest'
      responses:
        '200':
          description: Input sanitized successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SanitizationResult'
        '400':
          description: Invalid request parameters
        '500':
          description: Internal server error

components:
  schemas:
    SanitizationRequest:
      type: object
      required:
        - input
        - options
      properties:
        input:
          type: string
          description: Input string to sanitize
          maxLength: 10485760
        options:
          $ref: '#/components/schemas/SanitizationOptions'

    SanitizationOptions:
      type: object
      properties:
        encodeHtml:
          type: boolean
          description: Apply HTML encoding
          default: true
        encodeJavaScript:
          type: boolean
          description: Apply JavaScript encoding
          default: true
        encodeSqlParameters:
          type: boolean
          description: Apply SQL parameter encoding
          default: true
        removeDangerousChars:
          type: boolean
          description: Remove dangerous characters
          default: false
        maxLength:
          type: integer
          description: Maximum output length (0 = no limit)
          minimum: 0
          maximum: 10485760
          default: 0

    SanitizationResult:
      type: object
      properties:
        success:
          type: boolean
          description: Whether sanitization was successful
        sanitizedInput:
          type: string
          description: Sanitized input string
        originalLength:
          type: integer
          description: Original input length
        sanitizedLength:
          type: integer
          description: Sanitized input length
        appliedTransformations:
          type: array
          items:
            type: string
          description: List of applied sanitization transformations
        processingTime:
          type: string
          description: Sanitization processing time
      required:
        - success
        - sanitizedInput
```

### Encryption API

#### POST /api/security/encrypt

Encrypts data using AES-256-GCM authenticated encryption.

```yaml
  /api/security/encrypt:
    post:
      tags:
        - Security Encryption
      summary: Encrypt sensitive data
      description: |
        Encrypts data using AES-256-GCM authenticated encryption with:
        - 256-bit encryption keys
        - Random 96-bit nonces
        - 128-bit authentication tags
        - Integrity hash verification
      operationId: encryptData
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/EncryptionRequest'
      responses:
        '200':
          description: Data encrypted successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/EncryptionResult'
        '400':
          description: Invalid request parameters
        '500':
          description: Encryption failed

components:
  schemas:
    EncryptionRequest:
      type: object
      required:
        - data
        - options
      properties:
        data:
          type: string
          format: base64
          description: Base64-encoded data to encrypt
        options:
          $ref: '#/components/schemas/EncryptionOptions'

    EncryptionOptions:
      type: object
      properties:
        keyId:
          type: string
          description: Encryption key identifier (optional - generates new if not provided)
          pattern: '^[a-zA-Z0-9\-_]+$'
        keySize:
          type: integer
          description: Encryption key size in bits
          enum: [256]
          default: 256
        algorithm:
          type: string
          description: Encryption algorithm
          enum: ["AES-256-GCM"]
          default: "AES-256-GCM"

    EncryptionResult:
      type: object
      properties:
        success:
          type: boolean
          description: Whether encryption was successful
        encryptedData:
          type: string
          format: base64
          description: Base64-encoded encrypted data (nonce + ciphertext + tag)
        keyId:
          type: string
          description: Encryption key identifier used
        algorithm:
          type: string
          description: Encryption algorithm used
        integrityHash:
          type: string
          description: SHA-256 integrity hash of encrypted data
        timestamp:
          type: string
          format: date-time
          description: Encryption timestamp
        errorMessage:
          type: string
          description: Error message if encryption failed
          nullable: true
      required:
        - success
```

#### POST /api/security/decrypt

Decrypts data encrypted with the encryption API.

```yaml
  /api/security/decrypt:
    post:
      tags:
        - Security Encryption
      summary: Decrypt encrypted data
      description: |
        Decrypts data using AES-256-GCM with authentication verification.
        Validates data integrity and authenticity before returning plaintext.
      operationId: decryptData
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/DecryptionRequest'
      responses:
        '200':
          description: Data decrypted successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DecryptionResult'
        '400':
          description: Invalid request or corrupted data
        '404':
          description: Encryption key not found
        '500':
          description: Decryption failed

components:
  schemas:
    DecryptionRequest:
      type: object
      required:
        - encryptedData
        - keyId
      properties:
        encryptedData:
          type: string
          format: base64
          description: Base64-encoded encrypted data
        keyId:
          type: string
          description: Encryption key identifier
        integrityHash:
          type: string
          description: Expected integrity hash for verification
          nullable: true

    DecryptionResult:
      type: object
      properties:
        success:
          type: boolean
          description: Whether decryption was successful
        decryptedData:
          type: string
          format: base64
          description: Base64-encoded decrypted data
        algorithm:
          type: string
          description: Decryption algorithm used
        timestamp:
          type: string
          format: date-time
          description: Decryption timestamp
        integrityVerified:
          type: boolean
          description: Whether data integrity was verified
        errorMessage:
          type: string
          description: Error message if decryption failed
          nullable: true
      required:
        - success
```

### Hash Computation API

#### POST /api/security/hash

Computes secure cryptographic hashes of data.

```yaml
  /api/security/hash:
    post:
      tags:
        - Security Hashing
      summary: Compute secure hash
      description: |
        Computes cryptographically secure hash using SHA-256, SHA-384, or SHA-512.
        Suitable for data integrity verification and digital signatures.
      operationId: computeHash
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/HashRequest'
      responses:
        '200':
          description: Hash computed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/HashResult'
        '400':
          description: Invalid request parameters
        '500':
          description: Hash computation failed

components:
  schemas:
    HashRequest:
      type: object
      required:
        - data
        - algorithm
      properties:
        data:
          type: string
          format: base64
          description: Base64-encoded data to hash
        algorithm:
          type: string
          enum: [SHA256, SHA384, SHA512]
          description: Hash algorithm to use
          default: SHA256

    HashResult:
      type: object
      properties:
        success:
          type: boolean
          description: Whether hash computation was successful
        hash:
          type: string
          description: Base64-encoded hash value
        algorithm:
          type: string
          description: Hash algorithm used
        inputSize:
          type: integer
          description: Size of input data in bytes
        processingTime:
          type: string
          description: Hash computation time
      required:
        - success
        - hash
        - algorithm
```

### Rate Limiting API

#### POST /api/security/rate-limit/check

Checks and enforces rate limiting for requests.

```yaml
  /api/security/rate-limit/check:
    post:
      tags:
        - Security Rate Limiting
      summary: Check rate limit for identifier
      description: |
        Checks current rate limit status and enforces limits using sliding window algorithm.
        Provides detailed information about request counts, remaining requests, and reset times.
      operationId: checkRateLimit
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/RateLimitRequest'
      responses:
        '200':
          description: Rate limit check completed
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/RateLimitResult'
        '400':
          description: Invalid request parameters
        '429':
          description: Rate limit exceeded
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/RateLimitErrorResponse'

components:
  schemas:
    RateLimitRequest:
      type: object
      required:
        - identifier
        - maxRequests
        - timeWindow
      properties:
        identifier:
          type: string
          description: Unique identifier for rate limiting (e.g., IP, user ID)
          pattern: '^[a-zA-Z0-9\-_\.@]+$'
        maxRequests:
          type: integer
          description: Maximum requests allowed in time window
          minimum: 1
          maximum: 10000
        timeWindow:
          type: string
          description: Time window duration (e.g., "1m", "1h", "1d")
          pattern: '^[0-9]+[smhd]$'

    RateLimitResult:
      type: object
      properties:
        isAllowed:
          type: boolean
          description: Whether the request is allowed
        requestCount:
          type: integer
          description: Current request count in window
        remainingRequests:
          type: integer
          description: Remaining requests in current window
        resetTime:
          type: string
          format: date-time
          description: When the rate limit window resets
        retryAfter:
          type: string
          description: Time to wait before retrying (if blocked)
        windowStart:
          type: string
          format: date-time
          description: Start time of current window
      required:
        - isAllowed
        - requestCount
        - remainingRequests
        - resetTime

    RateLimitErrorResponse:
      type: object
      properties:
        error:
          type: string
          description: Error message
          example: "Rate limit exceeded"
        rateLimitInfo:
          $ref: '#/components/schemas/RateLimitResult'
      required:
        - error
        - rateLimitInfo
```

### Security Policy API

#### GET /api/security/policies/{resourceType}

Retrieves security policy for a resource type.

```yaml
  /api/security/policies/{resourceType}:
    get:
      tags:
        - Security Policies
      summary: Get security policy
      description: |
        Retrieves the security policy configuration for a specific resource type.
        Policies define validation rules, encryption requirements, and access controls.
      operationId: getSecurityPolicy
      parameters:
        - name: resourceType
          in: path
          required: true
          schema:
            type: string
            pattern: '^[a-zA-Z0-9\-_]+$'
          description: Resource type identifier
          example: "User"
      responses:
        '200':
          description: Security policy retrieved successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SecurityPolicy'
        '404':
          description: Policy not found
        '500':
          description: Internal server error

components:
  schemas:
    SecurityPolicy:
      type: object
      properties:
        resourceType:
          type: string
          description: Resource type this policy applies to
        requiresAuthentication:
          type: boolean
          description: Whether authentication is required
        requiresEncryption:
          type: boolean
          description: Whether encryption is required
        maxInputSize:
          type: integer
          description: Maximum input size in bytes
        rateLimitRequests:
          type: integer
          description: Rate limit requests per window
        rateLimitWindow:
          type: string
          description: Rate limit time window
        validateInput:
          type: boolean
          description: Whether input validation is required
        logSecurityEvents:
          type: boolean
          description: Whether to log security events
        createdAt:
          type: string
          format: date-time
          description: Policy creation timestamp
        updatedAt:
          type: string
          format: date-time
          description: Policy last update timestamp
          nullable: true
      required:
        - resourceType
        - requiresAuthentication
        - requiresEncryption
        - maxInputSize
        - rateLimitRequests
        - rateLimitWindow
        - validateInput
        - logSecurityEvents
        - createdAt
```

## Authentication API

### JWT Token Management

#### POST /api/auth/token

Generates JWT authentication tokens.

```yaml
  /api/auth/token:
    post:
      tags:
        - Authentication
      summary: Generate JWT token
      description: |
        Generates a JWT authentication token for API access.
        Tokens include user claims, permissions, and expiration time.
      operationId: generateToken
      security: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/TokenRequest'
      responses:
        '200':
          description: Token generated successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TokenResponse'
        '401':
          description: Invalid credentials
        '429':
          description: Rate limit exceeded

components:
  schemas:
    TokenRequest:
      type: object
      required:
        - username
        - password
      properties:
        username:
          type: string
          description: Username or email
          maxLength: 256
        password:
          type: string
          description: User password
          maxLength: 256
        scope:
          type: string
          description: Requested token scope
          default: "api:read api:write"
        expiresIn:
          type: integer
          description: Token expiration time in seconds
          minimum: 300
          maximum: 86400
          default: 3600

    TokenResponse:
      type: object
      properties:
        access_token:
          type: string
          description: JWT access token
        token_type:
          type: string
          description: Token type
          example: "Bearer"
        expires_in:
          type: integer
          description: Token expiration time in seconds
        scope:
          type: string
          description: Granted token scope
        issued_at:
          type: string
          format: date-time
          description: Token issue timestamp
      required:
        - access_token
        - token_type
        - expires_in
```

## Error Handling

### Common Error Responses

```yaml
components:
  schemas:
    ErrorResponse:
      type: object
      properties:
        error:
          type: string
          description: Error message
        code:
          type: string
          description: Error code
        details:
          type: object
          description: Additional error details
          nullable: true
        timestamp:
          type: string
          format: date-time
          description: Error timestamp
        requestId:
          type: string
          description: Request correlation ID
      required:
        - error
        - code
        - timestamp
        - requestId

    ValidationErrorResponse:
      allOf:
        - $ref: '#/components/schemas/ErrorResponse'
        - type: object
          properties:
            validationErrors:
              type: array
              items:
                type: object
                properties:
                  field:
                    type: string
                    description: Field name with error
                  message:
                    type: string
                    description: Validation error message
                required:
                  - field
                  - message
```

## Performance Specifications

### Response Time Targets

| Endpoint | Target Latency | Max Latency |
|----------|----------------|-------------|
| `/validate` | < 50ms | 200ms |
| `/sanitize` | < 30ms | 100ms |
| `/encrypt` | < 100ms | 500ms |
| `/decrypt` | < 100ms | 500ms |
| `/hash` | < 20ms | 100ms |
| `/rate-limit/check` | < 10ms | 50ms |
| `/policies` | < 20ms | 100ms |
| `/auth/token` | < 200ms | 1000ms |

### Throughput Targets

| Operation | Target TPS | Max TPS |
|-----------|------------|---------|
| Input Validation | 5,000 | 10,000 |
| Input Sanitization | 8,000 | 15,000 |
| Encryption | 1,000 | 2,000 |
| Decryption | 1,000 | 2,000 |
| Hash Computation | 10,000 | 20,000 |
| Rate Limit Check | 10,000 | 25,000 |
| Policy Retrieval | 5,000 | 10,000 |
| Token Generation | 500 | 1,000 |

## Security Headers

All API responses include security headers:

```http
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
X-Request-ID: <correlation-id>
X-Rate-Limit-Remaining: <count>
X-Rate-Limit-Reset: <timestamp>
```

## Monitoring and Observability

### Metrics

All endpoints expose the following metrics:
- Request count and rate
- Response time percentiles
- Error rates by status code
- Security threat detection counts
- Resource utilization metrics

### Health Checks

```yaml
  /api/security/health:
    get:
      tags:
        - Health
      summary: Security service health check
      description: Returns health status of security service components
      operationId: getSecurityHealth
      responses:
        '200':
          description: Service is healthy
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/HealthResponse'

components:
  schemas:
    HealthResponse:
      type: object
      properties:
        status:
          type: string
          enum: [Healthy, Degraded, Unhealthy]
        timestamp:
          type: string
          format: date-time
        components:
          type: object
          properties:
            cryptographicProviders:
              type: string
              enum: [Healthy, Unhealthy]
            encryptionKeys:
              type: string
              enum: [Healthy, Degraded, Unhealthy]
            rateLimiting:
              type: string
              enum: [Healthy, Unhealthy]
        version:
          type: string
          example: "2.0.0"
      required:
        - status
        - timestamp
        - components
        - version
```

This comprehensive API specification provides detailed documentation for all security-related endpoints, including request/response schemas, authentication methods, error handling, and performance requirements. The APIs implement enterprise-grade security controls while maintaining high performance and usability.