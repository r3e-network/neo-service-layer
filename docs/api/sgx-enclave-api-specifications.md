# SGX Enclave API Specifications

## Overview

This document provides comprehensive OpenAPI specifications for Intel SGX enclave operations in the Neo Service Layer platform. These APIs enable secure execution of JavaScript code within Intel SGX trusted execution environments with hardware-backed attestation and data sealing.

## Base Configuration

```yaml
openapi: 3.0.3
info:
  title: Neo Service Layer SGX Enclave API
  description: |
    Intel SGX enclave API providing secure execution, attestation, and data sealing
    services. All operations run within hardware-protected trusted execution environments
    with comprehensive security controls and performance monitoring.
  version: "2.0.0"
  license:
    name: MIT
    url: https://opensource.org/licenses/MIT
  contact:
    name: Neo SGX Team
    email: sgx@neo.org

servers:
  - url: https://api.neo-service-layer.com/v2
    description: Production server (Hardware SGX)
  - url: https://staging.neo-service-layer.com/v2
    description: Staging server (Simulation mode)
  - url: http://localhost:5000
    description: Development server (Simulation mode)

security:
  - BearerAuth: []
  - ApiKeyAuth: []

components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
    ApiKeyAuth:
      type: apiKey
      in: header
      name: X-API-Key
```

## SGX Enclave Core API

### Code Execution API

#### POST /api/enclave/execute

Executes JavaScript code securely within an SGX enclave.

```yaml
paths:
  /api/enclave/execute:
    post:
      tags:
        - SGX Execution
      summary: Execute code in SGX enclave
      description: |
        Securely executes JavaScript code within an Intel SGX enclave with:
        - Hardware-backed isolation and protection
        - Encrypted data processing
        - Execution time and memory limits
        - Comprehensive input validation
        - Detailed execution metrics
      operationId: executeCode
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ExecutionRequest'
            examples:
              simple_calculation:
                summary: Simple mathematical calculation
                value:
                  script: "Math.sqrt(data.number)"
                  data: { "number": 16 }
                  options:
                    maxExecutionTime: 5000
                    maxMemoryMB: 64
                    enableMetrics: true
              data_processing:
                summary: Complex data processing
                value:
                  script: |
                    const result = data.values.map(x => x * 2).reduce((a, b) => a + b, 0);
                    return { processedSum: result, timestamp: Date.now() };
                  data: { "values": [1, 2, 3, 4, 5] }
                  options:
                    maxExecutionTime: 10000
                    maxMemoryMB: 128
                    enableMetrics: true
                    enableProfiling: true
      responses:
        '200':
          description: Code executed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ExecutionResult'
              examples:
                success_result:
                  summary: Successful execution result
                  value:
                    success: true
                    result: 4
                    executionTime: 1250
                    memoryUsage: 2048
                    attestationValid: true
                    metrics:
                      cpuTime: 800
                      memoryPeak: 2048
                      ioOperations: 0
                    securityValidation:
                      inputValidated: true
                      outputValidated: true
                      threats: []
        '400':
          description: Invalid request or script validation failed
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ExecutionErrorResponse'
        '413':
          description: Script or data too large
        '429':
          description: Rate limit exceeded
        '500':
          description: Enclave execution failed

components:
  schemas:
    ExecutionRequest:
      type: object
      required:
        - script
        - data
      properties:
        script:
          type: string
          description: JavaScript code to execute in enclave
          maxLength: 1048576
          example: "Math.sqrt(data.number)"
        data:
          type: object
          description: Input data for the script (JSON serializable)
          example: { "number": 16 }
        options:
          $ref: '#/components/schemas/ExecutionOptions'
        metadata:
          type: object
          description: Optional metadata for tracking and debugging
          properties:
            requestId:
              type: string
              description: Client-provided request identifier
            tags:
              type: array
              items:
                type: string
              description: Tags for categorization
            priority:
              type: string
              enum: [Low, Normal, High, Critical]
              default: Normal

    ExecutionOptions:
      type: object
      properties:
        maxExecutionTime:
          type: integer
          description: Maximum execution time in milliseconds
          minimum: 100
          maximum: 300000
          default: 30000
        maxMemoryMB:
          type: integer
          description: Maximum memory usage in megabytes
          minimum: 1
          maximum: 512
          default: 64
        enableMetrics:
          type: boolean
          description: Enable detailed execution metrics
          default: true
        enableProfiling:
          type: boolean
          description: Enable code profiling information
          default: false
        strictMode:
          type: boolean
          description: Enable strict JavaScript execution mode
          default: true
        allowNetworking:
          type: boolean
          description: Allow network operations (restricted)
          default: false
        allowFileSystem:
          type: boolean
          description: Allow file system operations (restricted)
          default: false

    ExecutionResult:
      type: object
      properties:
        success:
          type: boolean
          description: Whether execution completed successfully
        result:
          description: Execution result (any JSON serializable value)
          nullable: true
        executionTime:
          type: integer
          description: Actual execution time in milliseconds
        memoryUsage:
          type: integer
          description: Peak memory usage in bytes
        attestationValid:
          type: boolean
          description: Whether SGX attestation is valid
        enclaveId:
          type: string
          description: Unique enclave instance identifier
        metrics:
          $ref: '#/components/schemas/ExecutionMetrics'
          nullable: true
        securityValidation:
          $ref: '#/components/schemas/SecurityValidationInfo'
        errorMessage:
          type: string
          description: Error message if execution failed
          nullable: true
        errorType:
          type: string
          enum: [ScriptError, TimeoutError, MemoryError, SecurityError, SystemError]
          description: Type of error that occurred
          nullable: true
      required:
        - success
        - executionTime
        - memoryUsage
        - attestationValid

    ExecutionMetrics:
      type: object
      properties:
        cpuTime:
          type: integer
          description: CPU time used in milliseconds
        memoryPeak:
          type: integer
          description: Peak memory usage in bytes
        memoryAverage:
          type: integer
          description: Average memory usage in bytes
        ioOperations:
          type: integer
          description: Number of I/O operations performed
        functionCalls:
          type: integer
          description: Number of function calls made
        loopIterations:
          type: integer
          description: Total loop iterations executed
        gcCollections:
          type: integer
          description: Number of garbage collections triggered

    SecurityValidationInfo:
      type: object
      properties:
        inputValidated:
          type: boolean
          description: Whether input data was validated
        outputValidated:
          type: boolean
          description: Whether output data was validated
        threats:
          type: array
          items:
            type: string
          description: List of security threats detected
        riskScore:
          type: number
          format: float
          description: Overall security risk score (0.0-1.0)
        validationTime:
          type: integer
          description: Time spent on security validation in milliseconds

    ExecutionErrorResponse:
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
          properties:
            scriptLine:
              type: integer
              description: Line number where error occurred
              nullable: true
            scriptColumn:
              type: integer
              description: Column number where error occurred
              nullable: true
            stackTrace:
              type: string
              description: JavaScript stack trace
              nullable: true
        executionInfo:
          type: object
          description: Execution information at time of error
          properties:
            executionTime:
              type: integer
              description: Time elapsed before error
            memoryUsage:
              type: integer
              description: Memory usage when error occurred
      required:
        - error
        - code
```

### Attestation API

#### GET /api/enclave/attestation

Retrieves SGX enclave attestation information.

```yaml
  /api/enclave/attestation:
    get:
      tags:
        - SGX Attestation
      summary: Get enclave attestation
      description: |
        Retrieves Intel SGX remote attestation information including:
        - Enclave measurement (MRENCLAVE)
        - Signer measurement (MRSIGNER)
        - Enclave attributes and configuration
        - Quote and signature verification
        - Platform security version (CPUSVN/ISVSVN)
      operationId: getAttestation
      parameters:
        - name: includeQuote
          in: query
          description: Include full attestation quote
          schema:
            type: boolean
            default: false
        - name: verifySignature
          in: query
          description: Verify attestation signature
          schema:
            type: boolean
            default: true
      responses:
        '200':
          description: Attestation retrieved successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AttestationResponse'
        '503':
          description: SGX not available or attestation failed

components:
  schemas:
    AttestationResponse:
      type: object
      properties:
        success:
          type: boolean
          description: Whether attestation was successful
        sgxMode:
          type: string
          enum: [Hardware, Simulation, NotSupported]
          description: SGX operation mode
        enclaveInfo:
          $ref: '#/components/schemas/EnclaveInfo'
        attestationInfo:
          $ref: '#/components/schemas/AttestationInfo'
          nullable: true
        platformInfo:
          $ref: '#/components/schemas/PlatformInfo'
        timestamp:
          type: string
          format: date-time
          description: Attestation timestamp
      required:
        - success
        - sgxMode
        - enclaveInfo
        - timestamp

    EnclaveInfo:
      type: object
      properties:
        enclaveId:
          type: string
          description: Unique enclave identifier
        mrEnclave:
          type: string
          description: Enclave measurement (hex)
          pattern: '^[0-9a-fA-F]{64}$'
        mrSigner:
          type: string
          description: Signer measurement (hex)
          pattern: '^[0-9a-fA-F]{64}$'
        isvProdId:
          type: integer
          description: ISV product ID
        isvSvn:
          type: integer
          description: ISV security version number
        attributes:
          type: object
          description: Enclave attributes
          properties:
            debug:
              type: boolean
              description: Debug mode enabled
            mode64Bit:
              type: boolean
              description: 64-bit mode enabled
            provisionKey:
              type: boolean
              description: Provision key available
            einittokenKey:
              type: boolean
              description: EINIT token key available
        configId:
          type: string
          description: Configuration ID (hex)
          nullable: true

    AttestationInfo:
      type: object
      properties:
        quoteStatus:
          type: string
          enum: [OK, SignatureInvalid, GroupRevoked, SignatureRevoked, KeyRevoked, SigrlVersionMismatch, GroupOutOfDate, ConfigurationNeeded]
          description: Quote verification status
        quote:
          type: string
          description: Base64-encoded attestation quote
          nullable: true
        signature:
          type: string
          description: Attestation signature
          nullable: true
        certificates:
          type: array
          items:
            type: string
          description: Certificate chain for verification
        revocationReason:
          type: string
          description: Reason for revocation (if applicable)
          nullable: true
        advisoryUrls:
          type: array
          items:
            type: string
          description: URLs for security advisories

    PlatformInfo:
      type: object
      properties:
        cpuSvn:
          type: string
          description: CPU security version number (hex)
        platformInstanceId:
          type: string
          description: Platform instance ID
          nullable: true
        gid:
          type: integer
          description: Platform group ID
        ppid:
          type: string
          description: Platform provisioning ID (hex)
          nullable: true
        epid:
          type: object
          description: EPID information
          nullable: true
```

### Data Sealing API

#### POST /api/enclave/seal

Seals data using SGX data sealing for persistent storage.

```yaml
  /api/enclave/seal:
    post:
      tags:
        - SGX Data Sealing
      summary: Seal data for secure storage
      description: |
        Seals data using Intel SGX data sealing mechanisms:
        - Hardware-bound encryption keys
        - Policy-based access control (MRENCLAVE/MRSIGNER)
        - Replay protection with monotonic counters
        - Tamper-evident storage format
      operationId: sealData
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/SealRequest'
      responses:
        '200':
          description: Data sealed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SealResult'
        '400':
          description: Invalid request
        '413':
          description: Data too large
        '500':
          description: Sealing failed

components:
  schemas:
    SealRequest:
      type: object
      required:
        - data
        - policy
      properties:
        data:
          type: string
          format: base64
          description: Base64-encoded data to seal
          maxLength: 104857600  # 100MB
        policy:
          $ref: '#/components/schemas/SealingPolicy'
        metadata:
          type: object
          description: Optional metadata
          properties:
            description:
              type: string
              maxLength: 256
            tags:
              type: array
              items:
                type: string
            expirationTime:
              type: string
              format: date-time
              description: Optional expiration time

    SealingPolicy:
      type: object
      required:
        - policyType
      properties:
        policyType:
          type: string
          enum: [MRENCLAVE, MRSIGNER, HYBRID]
          description: Sealing policy type
        requireSameEnclave:
          type: boolean
          description: Require same enclave for unsealing
          default: true
        requireSameSigner:
          type: boolean
          description: Require same signer for unsealing
          default: false
        minimumSvn:
          type: integer
          description: Minimum security version number
          minimum: 0
        additionalData:
          type: string
          description: Additional authentication data
          maxLength: 1024
          nullable: true

    SealResult:
      type: object
      properties:
        success:
          type: boolean
          description: Whether sealing was successful
        sealedData:
          type: string
          description: Base64-encoded sealed data blob
        keyId:
          type: string
          description: Unique identifier for sealed data
        sealingInfo:
          $ref: '#/components/schemas/SealingInfo'
        timestamp:
          type: string
          format: date-time
          description: Sealing timestamp
        errorMessage:
          type: string
          description: Error message if sealing failed
          nullable: true
      required:
        - success
        - timestamp

    SealingInfo:
      type: object
      properties:
        policyUsed:
          $ref: '#/components/schemas/SealingPolicy'
        dataSize:
          type: integer
          description: Original data size in bytes
        sealedSize:
          type: integer
          description: Sealed data size in bytes
        compressionRatio:
          type: number
          format: float
          description: Data compression ratio
        enclaveInfo:
          type: object
          properties:
            mrEnclave:
              type: string
              description: Enclave measurement used
            mrSigner:
              type: string
              description: Signer measurement used
            isvSvn:
              type: integer
              description: ISV security version used
```

#### POST /api/enclave/unseal

Unseals previously sealed data.

```yaml
  /api/enclave/unseal:
    post:
      tags:
        - SGX Data Sealing
      summary: Unseal previously sealed data
      description: |
        Unseals data that was previously sealed using SGX data sealing.
        Validates policy compliance and enclave identity before unsealing.
      operationId: unsealData
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/UnsealRequest'
      responses:
        '200':
          description: Data unsealed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UnsealResult'
        '400':
          description: Invalid sealed data
        '403':
          description: Policy validation failed
        '500':
          description: Unsealing failed

components:
  schemas:
    UnsealRequest:
      type: object
      required:
        - sealedData
      properties:
        sealedData:
          type: string
          description: Base64-encoded sealed data blob
        keyId:
          type: string
          description: Sealed data identifier (for verification)
          nullable: true
        additionalData:
          type: string
          description: Additional authentication data (if used during sealing)
          nullable: true

    UnsealResult:
      type: object
      properties:
        success:
          type: boolean
          description: Whether unsealing was successful
        data:
          type: string
          format: base64
          description: Base64-encoded unsealed data
        unsealingInfo:
          $ref: '#/components/schemas/UnsealingInfo'
          nullable: true
        timestamp:
          type: string
          format: date-time
          description: Unsealing timestamp
        errorMessage:
          type: string
          description: Error message if unsealing failed
          nullable: true
        errorType:
          type: string
          enum: [PolicyViolation, IntegrityError, KeyNotFound, SystemError]
          nullable: true
      required:
        - success
        - timestamp

    UnsealingInfo:
      type: object
      properties:
        originalPolicy:
          $ref: '#/components/schemas/SealingPolicy'
        policyValidation:
          type: object
          properties:
            enclaveMatch:
              type: boolean
              description: Whether enclave measurement matches
            signerMatch:
              type: boolean
              description: Whether signer measurement matches
            svnValid:
              type: boolean
              description: Whether security version is valid
            additionalDataValid:
              type: boolean
              description: Whether additional data is valid
        dataIntegrity:
          type: object
          properties:
            checksumValid:
              type: boolean
              description: Whether data checksum is valid
            sizeValid:
              type: boolean
              description: Whether data size is valid
            formatValid:
              type: boolean
              description: Whether data format is valid
        originalSize:
          type: integer
          description: Original data size in bytes
        processingTime:
          type: integer
          description: Unsealing processing time in milliseconds
```

### Status and Health API

#### GET /api/enclave/status

Retrieves SGX enclave status and health information.

```yaml
  /api/enclave/status:
    get:
      tags:
        - SGX Status
      summary: Get enclave status
      description: |
        Retrieves comprehensive status information about SGX enclave operations:
        - Hardware capabilities and configuration
        - Enclave runtime statistics
        - Performance metrics
        - Security status
      operationId: getEnclaveStatus
      responses:
        '200':
          description: Status retrieved successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/EnclaveStatusResponse'
        '500':
          description: Failed to retrieve status

components:
  schemas:
    EnclaveStatusResponse:
      type: object
      properties:
        overall:
          type: string
          enum: [Healthy, Degraded, Unhealthy, NotSupported]
          description: Overall enclave health status
        sgxInfo:
          $ref: '#/components/schemas/SGXInfo'
        runtime:
          $ref: '#/components/schemas/RuntimeInfo'
        performance:
          $ref: '#/components/schemas/PerformanceInfo'
        security:
          $ref: '#/components/schemas/SecurityInfo'
        timestamp:
          type: string
          format: date-time
      required:
        - overall
        - sgxInfo
        - timestamp

    SGXInfo:
      type: object
      properties:
        supported:
          type: boolean
          description: Whether SGX is supported
        mode:
          type: string
          enum: [Hardware, Simulation, NotAvailable]
        version:
          type: string
          description: SGX version
        features:
          type: array
          items:
            type: string
          description: Available SGX features
        limits:
          type: object
          properties:
            maxEnclaves:
              type: integer
            maxEnclaveSize:
              type: integer
            maxEpcSize:
              type: integer

    RuntimeInfo:
      type: object
      properties:
        activeEnclaves:
          type: integer
          description: Number of active enclave instances
        totalExecutions:
          type: integer
          description: Total executions since startup
        uptime:
          type: integer
          description: Runtime uptime in seconds
        memoryUsage:
          type: object
          properties:
            totalAllocated:
              type: integer
              description: Total memory allocated in bytes
            peakUsage:
              type: integer
              description: Peak memory usage in bytes
            currentUsage:
              type: integer
              description: Current memory usage in bytes

    PerformanceInfo:
      type: object
      properties:
        averageExecutionTime:
          type: number
          format: float
          description: Average execution time in milliseconds
        throughput:
          type: number
          format: float
          description: Requests per second
        errorRate:
          type: number
          format: float
          description: Error rate percentage
        latencyPercentiles:
          type: object
          properties:
            p50:
              type: number
              format: float
            p90:
              type: number
              format: float
            p99:
              type: number
              format: float

    SecurityInfo:
      type: object
      properties:
        attestationStatus:
          type: string
          enum: [Valid, Invalid, NotPerformed]
        lastAttestationTime:
          type: string
          format: date-time
          nullable: true
        threatsDetected:
          type: integer
          description: Number of security threats detected
        securityEventsToday:
          type: integer
          description: Security events in last 24 hours
        encryptionStatus:
          type: string
          enum: [Enabled, Disabled, Error]
```

## Error Handling

### Common Error Responses

```yaml
components:
  schemas:
    SGXErrorResponse:
      type: object
      properties:
        error:
          type: string
          description: Error message
        code:
          type: string
          description: SGX-specific error code
        sgxErrorCode:
          type: integer
          description: Native SGX error code
          nullable: true
        details:
          type: object
          description: Additional error details
        timestamp:
          type: string
          format: date-time
        requestId:
          type: string
          description: Request correlation ID
      required:
        - error
        - code
        - timestamp
        - requestId
```

## Performance Specifications

### Response Time Targets

| Endpoint | Target Latency | Max Latency |
|----------|----------------|-------------|
| `/execute` (simple) | < 100ms | 500ms |
| `/execute` (complex) | < 1000ms | 5000ms |
| `/attestation` | < 200ms | 1000ms |
| `/seal` | < 500ms | 2000ms |
| `/unseal` | < 300ms | 1000ms |
| `/status` | < 50ms | 200ms |

### Throughput Targets

| Operation | Target TPS | Notes |
|-----------|------------|-------|
| Simple Code Execution | 100 | Basic calculations |
| Complex Code Execution | 25 | Data processing |
| Data Sealing | 50 | Up to 1MB data |
| Data Unsealing | 75 | Cached keys |
| Status Checks | 500 | Health monitoring |

## Security Considerations

### Input Validation
- All JavaScript code is validated before execution
- Maximum script size: 1MB
- Maximum execution time: 5 minutes
- Memory limits enforced per enclave

### Data Protection
- All data sealed with hardware-bound keys
- Replay protection for sealed data
- Integrity verification on all operations
- Comprehensive audit logging

### Access Control
- JWT-based authentication required
- Role-based authorization
- Rate limiting per user/API key
- Request correlation and tracking

This comprehensive API specification provides complete documentation for all SGX enclave operations, including secure code execution, hardware attestation, data sealing/unsealing, and status monitoring. The APIs leverage Intel SGX hardware security features while maintaining high performance and developer usability.