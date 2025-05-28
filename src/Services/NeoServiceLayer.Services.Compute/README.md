# Compute Service

## Overview

The Compute Service is a secure, enclave-based service for executing computations in a trusted environment. It provides a framework for registering, executing, and verifying computations, ensuring that the results are trustworthy and tamper-proof.

## Features

- **Secure Computation**: Execute computations within a secure enclave, ensuring that the code and data are protected.
- **Computation Registration**: Register computations that can be executed later.
- **Verifiable Results**: Generate cryptographic proofs for computation results, allowing third parties to verify the authenticity of the results.
- **Computation Metadata**: Maintain metadata about computations, including execution statistics.
- **Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Compute Service is built on the Neo Service Layer framework and uses the Trusted Execution Environment (TEE) to provide secure computation. The service consists of the following components:

- **IComputeService**: The interface that defines the operations supported by the service.
- **ComputeService**: The implementation of the service that uses the enclave to perform computations.
- **EnclaveManager**: The component that manages the communication with the enclave.

## Usage

### Service Registration

```csharp
// Register the service
services.AddNeoService<IComputeService, ComputeService>();

// Register the service with the service registry
serviceProvider.RegisterAllNeoServices();
```

### Registering a Computation

```csharp
// Register a new computation
bool success = await computeService.RegisterComputationAsync(
    "my-computation",
    "function compute(input) { return input * 2; }",
    "JavaScript",
    "A simple computation that doubles the input",
    BlockchainType.NeoN3);
```

### Executing a Computation

```csharp
// Execute a computation
var parameters = new Dictionary<string, string>
{
    { "input", "42" }
};

var result = await computeService.ExecuteComputationAsync(
    "my-computation",
    parameters,
    BlockchainType.NeoN3);
```

### Verifying a Computation Result

```csharp
// Verify a computation result
bool isValid = await computeService.VerifyComputationResultAsync(
    result,
    BlockchainType.NeoN3);
```

### Getting Computation Metadata

```csharp
// Get computation metadata
var metadata = await computeService.GetComputationMetadataAsync(
    "my-computation",
    BlockchainType.NeoN3);
```

## Security Considerations

- All computations are executed within the secure enclave.
- Computation code and data are protected from tampering.
- Results are cryptographically signed to ensure authenticity.
- All operations are logged for audit purposes.

## API Reference

### RegisterComputationAsync

Registers a computation.

```csharp
Task<bool> RegisterComputationAsync(
    string computationId,
    string computationCode,
    string computationType,
    string description,
    BlockchainType blockchainType);
```

### UnregisterComputationAsync

Unregisters a computation.

```csharp
Task<bool> UnregisterComputationAsync(
    string computationId,
    BlockchainType blockchainType);
```

### ExecuteComputationAsync

Executes a computation.

```csharp
Task<ComputationResult> ExecuteComputationAsync(
    string computationId,
    IDictionary<string, string> parameters,
    BlockchainType blockchainType);
```

### GetComputationStatusAsync

Gets the status of a computation.

```csharp
Task<ComputationStatus> GetComputationStatusAsync(
    string computationId,
    BlockchainType blockchainType);
```

### ListComputationsAsync

Lists registered computations.

```csharp
Task<IEnumerable<ComputationMetadata>> ListComputationsAsync(
    int skip,
    int take,
    BlockchainType blockchainType);
```

### GetComputationMetadataAsync

Gets computation metadata.

```csharp
Task<ComputationMetadata> GetComputationMetadataAsync(
    string computationId,
    BlockchainType blockchainType);
```

### VerifyComputationResultAsync

Verifies a computation result.

```csharp
Task<bool> VerifyComputationResultAsync(
    ComputationResult result,
    BlockchainType blockchainType);
```
