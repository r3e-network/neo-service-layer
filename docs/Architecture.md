# Neo Service Layer - Architecture Document

## 1. Introduction

The Neo Service Layer (NSL) is a secure, scalable, and verifiable serverless platform that leverages Trusted Execution Environments (TEEs) like Intel SGX through the Open Enclave SDK with Occlum to provide confidential computing capabilities for executing JavaScript functions with user secrets, event triggers, and GAS accounting. This document outlines the architecture, components, and implementation details of the NSL.

## 2. System Architecture Overview

### 2.1 High-Level Architecture

The NSL follows a layered architecture approach:

1. **API Layer**: Provides RESTful endpoints for clients to interact with the NSL
2. **Service Layer**: Contains the business logic and orchestrates the execution of JavaScript functions
3. **TEE Layer**: Executes JavaScript functions in a secure environment using Open Enclave SDK with Occlum
4. **Infrastructure Layer**: Provides supporting services such as database, message queue, and monitoring
5. **Storage Layer**: Provides persistent storage capabilities using Occlum's file system

### 2.2 Core Services

The NSL provides seven core services:

1. **Confidential JavaScript Function Execution**: Executes JavaScript functions securely within a Trusted Execution Environment (TEE)
2. **User Secrets Management**: Securely stores and manages user-specified secrets
3. **Event-Triggered Function Execution**: Allows functions to be triggered by registered events
4. **Randomness Service**: Generates verifiable random numbers for applications
5. **Attestation Service**: Verifies the integrity of the TEE
6. **Key Management Service**: Manages cryptographic keys securely
7. **GAS Accounting Service**: Tracks resource usage for JavaScript function execution

### 2.3 Component Architecture

#### 2.3.1 API Layer
- **API Gateway**: Entry point for all client requests
- **Authentication Service**: Handles API key validation and authentication
- **Rate Limiting**: Controls request rates to prevent abuse

#### 2.3.2 Core Services
- **JavaScript Function Manager**: Manages JavaScript function execution
- **User Secrets Manager**: Securely stores and retrieves user secrets
- **Blockchain Event Listener**: Monitors Neo N3 blockchain events and triggers JavaScript execution
- **Blockchain Callback Service**: Sends execution results back to Neo N3 smart contracts
- **GAS Accounting**: Tracks resource usage and billing

#### 2.3.3 TEE Components
- **TEE Host Process**: Manages the enclave lifecycle
- **TEE Enclave**: Executes JavaScript functions in a secure environment
- **JavaScript Engine**: Executes JavaScript code with access to user secrets
- **Blockchain API**: Allows JavaScript functions to send callback transactions
- **Attestation Generator**: Creates attestation proofs
- **Key Manager**: Manages cryptographic keys securely

#### 2.3.4 Infrastructure Services
- **Database**: Stores task state and metadata
- **Message Queue**: Enables asynchronous processing
- **Monitoring & Logging**: Tracks system health and performance
- **Secure Storage**: Stores sensitive data securely
- **Neo N3 Blockchain Service**: Interacts with the Neo N3 blockchain

## 3. Project Structure

The NSL solution is structured as follows:

```
NeoServiceLayer/
├── src/
│   ├── NeoServiceLayer.Api/                # API project
│   ├── NeoServiceLayer.Core/               # Core domain models and services
│   ├── NeoServiceLayer.Infrastructure/     # Infrastructure services
│   ├── NeoServiceLayer.Tee.Host/           # TEE host process
│   │   ├── Occlum/                         # Occlum integration
│   │   └── Storage/                        # Storage implementations
│   ├── NeoServiceLayer.Tee.Enclave/        # TEE enclave code (.NET and C++)
│   │   └── Enclave/                        # Native enclave code (C++)
│   └── NeoServiceLayer.Shared/             # Shared components
├── tests/
│   ├── NeoServiceLayer.Api.Tests/
│   ├── NeoServiceLayer.Core.Tests/
│   ├── NeoServiceLayer.Infrastructure.Tests/
│   ├── NeoServiceLayer.Tee.Host.Tests/
│   ├── NeoServiceLayer.Tee.Enclave.Tests/
│   ├── NeoServiceLayer.Occlum.Tests/       # Occlum-specific tests
│   └── NeoServiceLayer.Integration.Tests/  # Integration tests
├── docs/
│   ├── Architecture.md
│   ├── API.md
│   ├── Deployment.md
│   ├── Development.md
│   └── persistent-storage.md               # Persistent storage documentation
├── examples/
│   ├── SmartContractExecution/
│   └── JavaScriptFunctions/
└── deployment/
    └── docker/                             # Docker configuration files
```

## 4. Component Details

### 4.1 NeoServiceLayer.Api

The API project provides RESTful endpoints for clients to interact with the NSL:

- **Controllers**:
  - JavaScriptFunctionController: Manage JavaScript functions
  - UserSecretController: Manage user secrets
  - EventTriggerController: Manage event triggers
  - AttestationController: Handle attestation verification
  - KeyManagementController: Manage cryptographic keys
  - RandomnessController: Generate random numbers
  - GasAccountingController: Track resource usage

- **Middleware**:
  - Authentication middleware
  - Rate limiting middleware
  - Exception handling middleware
  - Logging middleware

- **Swagger Documentation**:
  - API documentation
  - Example requests and responses
  - Authentication information

### 4.2 NeoServiceLayer.Core

The Core project contains the domain models and business logic:

- **Models**:
  - JavaScriptFunction: Represents a JavaScript function
  - UserSecret: Represents a user-specified secret
  - EventTrigger: Represents an event trigger
  - GasAccounting: Represents resource usage tracking
  - TeeAccount: Represents a TEE-managed account
  - AttestationProof: Contains attestation information

- **Interfaces**:
  - IJavaScriptExecutionService: JavaScript function execution interface
  - IUserSecretService: User secrets management interface
  - IEventTriggerService: Event trigger management interface
  - IGasAccountingService: Resource usage tracking interface
  - IAttestationService: Attestation verification interface
  - IKeyManagementService: Key management interface
  - IRandomnessService: Random number generation interface

- **Services**:
  - Implementations of the above interfaces
  - Business logic for each service

### 4.3 NeoServiceLayer.Infrastructure

The Infrastructure project handles external service integrations:

- **Services**:
  - JavaScriptExecutionService: Executes JavaScript functions
  - UserSecretService: Manages user secrets
  - EventTriggerService: Manages event triggers
  - GasAccountingService: Tracks resource usage
  - AttestationService: Verifies attestation proofs
  - KeyManagementService: Manages cryptographic keys
  - RandomnessService: Generates random numbers
  - NeoN3BlockchainService: Interacts with the Neo N3 blockchain
  - NeoN3EventListenerService: Listens for events from the Neo N3 blockchain

- **Data Access**:
  - Entity Framework Core DbContext
  - Repository implementations
  - Database migrations

- **Background Services**:
  - JavaScript function execution service
  - Neo N3 blockchain event listener service
  - Blockchain callback service
  - GAS accounting service

### 4.4 NeoServiceLayer.Tee.Host

The TEE Host project manages the enclave lifecycle:

- **Enclave Management**:
  - Enclave initialization
  - Enclave lifecycle management
  - Enclave communication

- **JavaScript Function Processing**:
  - Function validation
  - Function execution
  - Result handling

- **Attestation**:
  - Attestation request handling
  - Attestation verification
  - Attestation report generation

### 4.5 NeoServiceLayer.Tee.Enclave

The TEE Enclave project contains the enclave code:

- **Services**:
  - JavaScriptExecutor: Executes JavaScript functions
  - UserSecretManager: Manages user secrets
  - EventProcessor: Processes events
  - GasAccountingManager: Tracks resource usage
  - BlockchainCallbackManager: Manages blockchain callbacks

- **JavaScript Engine**:
  - QuickJS integration
  - JavaScript API for accessing user secrets via SECRETS global object
  - JavaScript API for blockchain callbacks
  - Sandboxed execution environment

- **Key Management**:
  - Key generation
  - Key storage
  - Signing operations

- **Randomness**:
  - Random number generation
  - Verifiable randomness
  - Entropy collection

### 4.6 NeoServiceLayer.Tee.Enclave/Enclave (Native)

The native enclave code implements the low-level Open Enclave functionality:

- **Core Components**:
  - Neo Service Layer main component
  - QuickJS JavaScript Engine
  - User Secret Manager
  - Event Processor
  - GAS Accounting Manager
  - Blockchain Callback Manager
  - Key Manager
  - Attestation Generator
  - Secure Storage
  - Random Generator

- **JavaScript APIs**:
  - console: For logging
  - storage: For persistent storage
  - crypto: For cryptographic operations
  - gas: For gas accounting
  - SECRETS: Global object for accessing user secrets
  - blockchain: For interacting with the Neo N3 blockchain

- **Occlum Integration**:
  - Occlum instance setup
  - Filesystem configuration
  - Memory management
  - Persistent storage management

- **Open Enclave Bridge**:
  - Native to managed code bridge
  - ECALL/OCALL implementations
  - Serialization/deserialization

## 5. Security Considerations

### 5.1 TEE Security
- **Enclave Protection**: Ensure code and data in the enclave are protected
- **Attestation**: Implement remote attestation to verify enclave integrity
- **Side-Channel Mitigation**: Address potential side-channel attacks
- **Memory Encryption**: Ensure all sensitive data is encrypted in memory
- **Persistent Storage Security**: Ensure data stored in Occlum's file system is encrypted and protected

### 5.2 API Security
- **Authentication**: Implement API key and OAuth authentication
- **Authorization**: Enforce proper access control
- **Rate Limiting**: Prevent abuse through rate limiting
- **Input Validation**: Validate all input to prevent injection attacks

### 5.3 Data Security
- **Encryption**: Encrypt all sensitive data at rest and in transit
- **Key Management**: Securely manage encryption keys
- **Data Minimization**: Only collect and store necessary data
- **Secure Deletion**: Implement secure data deletion procedures
- **Persistent Storage**: Use encrypted and secure persistent storage for sensitive data
- **Compression**: Compress data to reduce storage requirements while maintaining security

### 5.4 Network Security
- **TLS**: Use TLS for all communications
- **Network Segmentation**: Isolate components using network policies
- **Firewall Rules**: Implement strict firewall rules
- **DDoS Protection**: Use Aliyun's DDoS protection services

## 6. Deployment Architecture

The Neo Service Layer will be deployed on Aliyun using Kubernetes for orchestration:

### 6.1 Containerization
- Docker images for each component
- Docker Compose for local development
- Kubernetes manifests for production

### 6.2 Kubernetes Resources
- Deployments for stateless components
- StatefulSets for stateful components
- Services for internal communication
- Ingress for external access
- ConfigMaps and Secrets for configuration

### 6.3 Aliyun Services
- Aliyun Container Service for Kubernetes (ACK)
- Aliyun Database Service (RDS)
- Aliyun Object Storage Service (OSS)
- Aliyun Message Queue Service
- Aliyun Virtual Private Cloud (VPC)

### 6.4 Monitoring and Logging
- Prometheus for metrics collection
- Grafana for visualization
- ELK stack for logging
- Jaeger for distributed tracing
