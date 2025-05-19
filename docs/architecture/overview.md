# Neo Service Layer Architecture Overview

## Introduction

Neo Service Layer is a confidential computing platform designed to execute JavaScript code securely within Intel SGX enclaves using the Occlum LibOS. This document provides an overview of the system architecture, components, and their interactions.

## System Architecture

The Neo Service Layer is structured as a layered application with the following key components:

### Core Components

1. **NeoServiceLayer.Api**
   - Public-facing REST API for interacting with the system
   - Handles authentication, authorization, and request routing
   - Exposes endpoints for JavaScript execution, events, and blockchain interactions

2. **NeoServiceLayer.Core**
   - Contains core domain models and business logic
   - Defines service interfaces and domain entities
   - Implements business rules and validation

3. **NeoServiceLayer.Infrastructure**
   - Provides implementations of core interfaces
   - Handles data storage, external services integration
   - Implements persistence mechanisms

4. **NeoServiceLayer.Common**
   - Shared utilities, models, and interfaces
   - Common code used across all layers
   - Helper functions and extension methods

### TEE Components

1. **NeoServiceLayer.Tee.Host**
   - Host-side code for interacting with the TEE
   - Manages enclave lifecycle
   - Handles communication between untrusted and trusted environments

2. **NeoServiceLayer.Tee.Enclave**
   - Enclave-side code that runs in the trusted environment
   - Executes JavaScript securely
   - Implements cryptographic operations in the trusted environment

## Key Abstractions

### Interfaces

- **IOcclumInterface**: Primary interface for interacting with the Occlum LibOS
- **ITeeInterface**: Generic interface for Trusted Execution Environments
- **IStorageManager**: Interface for persistent storage
- **IAttestationProvider**: Interface for remote attestation

### Services

- **JavaScriptExecutionService**: Executes JavaScript code within the enclave
- **EventTriggerService**: Manages event-based triggers and reactions
- **BlockchainService**: Interacts with blockchain networks
- **RandomnessService**: Provides verifiable random numbers

## Data Flow

1. Requests arrive via the API
2. Controllers validate and route requests to appropriate services
3. Services orchestrate business logic and enclave operations
4. Enclave performs secure operations
5. Results are returned through the service and API layers

## Security Architecture

1. **Attestation**: All enclaves are attested before use
2. **Secure Storage**: Sensitive data is sealed to the enclave
3. **Isolation**: JavaScript execution happens within the enclave boundary
4. **Verification**: Results can be cryptographically verified

## Next Steps

See the following documents for more detailed information:
- [Security Model](./security-model.md)
- [Data Flow](./data-flow.md)
- [Dependency Graph](./dependency-graph.md) 