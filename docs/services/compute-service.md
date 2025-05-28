# Neo Service Layer - Compute Service

## Overview

The Compute Service provides secure, verifiable computation offloading for blockchain applications. It enables computationally intensive tasks to be executed securely off-chain within Trusted Execution Environments (TEEs), with the results being verifiable on-chain.

## Features

- **Secure Computation**: Execute computations within SGX enclaves, ensuring that the code and data are protected.
- **Verifiable Results**: Generate cryptographic proofs for computation results, allowing third parties to verify the correctness of the results.
- **Multiple Blockchain Support**: Support for both Neo N3 and NeoX blockchains.
- **Multiple Language Support**: Support for executing code in various languages, including JavaScript, Python, and WebAssembly.
- **Batch Computation**: Execute multiple computations in a single request.
- **Computation Scheduling**: Schedule computations to be executed at specific times or intervals.
- **Resource Limits**: Set limits on computation resources to prevent abuse.
- **Parallel Execution**: Execute computations in parallel to improve performance.

## Architecture

The Compute Service consists of the following components:

1. **Compute Service API**: Provides RESTful API endpoints for registering, executing, and verifying computations.
2. **Compute Service Implementation**: Implements the compute service logic, including computation registration, execution, and verification.
3. **Enclave Integration**: Integrates with the TEE for secure computation execution.
4. **Blockchain Integration**: Integrates with the blockchain for transaction submission and event monitoring.
5. **Language Runtimes**: Provides runtimes for executing code in various languages.

## Computation Registration Process

The computation registration process follows these steps:

1. **Request Validation**: The service validates the request parameters, including the blockchain type, computation code, and computation type.
2. **Enclave Initialization**: If not already initialized, the service initializes the enclave.
3. **Code Validation**: The service validates the computation code to ensure it meets security requirements.
4. **Code Storage**: The service stores the computation code securely within the enclave.
5. **Response**: The service returns the computation ID to the client.

## Computation Execution Process

The computation execution process follows these steps:

1. **Request Validation**: The service validates the request parameters, including the blockchain type, computation ID, and parameters.
2. **Enclave Initialization**: If not already initialized, the service initializes the enclave.
3. **Code Retrieval**: The service retrieves the computation code from the enclave.
4. **Parameter Validation**: The service validates the computation parameters.
5. **Computation Execution**: The service executes the computation within the enclave.
6. **Result Generation**: The service generates a result and a cryptographic proof for the result.
7. **Response**: The service returns the result and proof to the client.

## Computation Verification Process

The computation verification process follows these steps:

1. **Request Validation**: The service validates the request parameters, including the blockchain type, computation ID, result, and proof.
2. **Enclave Initialization**: If not already initialized, the service initializes the enclave.
3. **Code Retrieval**: The service retrieves the computation code from the enclave.
4. **Proof Verification**: The service verifies the cryptographic proof for the result using the enclave.
5. **Response**: The service returns the verification result to the client.

## API Endpoints

### Register Computation

Registers a computation.

**URL**: `/api/v1/compute/register`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "computationId": "computation-id",
  "computationCode": "function compute(input) { return input * 2; }",
  "computationType": "JavaScript",
  "description": "A simple computation that doubles the input"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "computationId": "computation-id",
    "computationType": "JavaScript",
    "description": "A simple computation that doubles the input",
    "createdAt": "2023-01-01T00:00:00Z"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Execute Computation

Executes a computation.

**URL**: `/api/v1/compute/execute`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "computationId": "computation-id",
  "parameters": {
    "input": 42
  }
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "computationId": "computation-id",
    "resultId": "result-id",
    "resultData": 84,
    "executionTimeMs": 50,
    "timestamp": "2023-01-01T00:00:00Z",
    "proof": "cryptographic-proof"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Verify Computation Result

Verifies a computation result.

**URL**: `/api/v1/compute/verify`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "computationId": "computation-id",
  "resultId": "result-id",
  "resultData": 84,
  "proof": "cryptographic-proof"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "valid": true
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Batch Execute Computation

Executes multiple computations in a single request.

**URL**: `/api/v1/compute/batch-execute`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "computations": [
    {
      "computationId": "computation-id-1",
      "parameters": {
        "input": 42
      }
    },
    {
      "computationId": "computation-id-2",
      "parameters": {
        "input": 24
      }
    }
  ]
}
```

**Response**:

```json
{
  "success": true,
  "data": [
    {
      "computationId": "computation-id-1",
      "resultId": "result-id-1",
      "resultData": 84,
      "executionTimeMs": 50,
      "timestamp": "2023-01-01T00:00:00Z",
      "proof": "cryptographic-proof-1"
    },
    {
      "computationId": "computation-id-2",
      "resultId": "result-id-2",
      "resultData": 48,
      "executionTimeMs": 45,
      "timestamp": "2023-01-01T00:00:00Z",
      "proof": "cryptographic-proof-2"
    }
  ],
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Smart Contract Integration

### Neo N3 Smart Contract

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;

namespace ComputeExample
{
    [DisplayName("ComputeExample")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "Compute Example")]
    public class ComputeExample : SmartContract
    {
        [DisplayName("ComputationResultUsed")]
        public static event Action<int> OnComputationResultUsed;

        public static bool UseComputationResult(string computationId, string resultId, int resultData, byte[] proof)
        {
            // Verify the computation result using the Compute Service
            bool isValid = VerifyComputationResult(computationId, resultId, resultData, proof);
            if (!isValid)
            {
                return false;
            }

            // Use the computation result
            OnComputationResultUsed(resultData);

            // Perform some action based on the computation result
            // ...

            return true;
        }

        private static bool VerifyComputationResult(string computationId, string resultId, int resultData, byte[] proof)
        {
            // Call the Compute Service to verify the computation result
            // This is a simplified example
            return true;
        }
    }
}
```

### NeoX Smart Contract

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IComputeConsumer {
    function verifyComputationResult(string calldata computationId, string calldata resultId, uint256 resultData, bytes calldata proof) external view returns (bool);
}

contract ComputeExample {
    address private computeContract;
    
    event ComputationResultUsed(uint256 resultData);
    
    constructor(address _computeContract) {
        computeContract = _computeContract;
    }
    
    function useComputationResult(string calldata computationId, string calldata resultId, uint256 resultData, bytes calldata proof) external returns (bool) {
        // Verify the computation result using the Compute Service
        bool isValid = IComputeConsumer(computeContract).verifyComputationResult(computationId, resultId, resultData, proof);
        if (!isValid) {
            return false;
        }
        
        // Use the computation result
        emit ComputationResultUsed(resultData);
        
        // Perform some action based on the computation result
        // ...
        
        return true;
    }
}
```

## Security Considerations

- **Enclave Security**: The security of the computation execution depends on the security of the enclave. The enclave must be properly attested and verified.
- **Code Validation**: The computation code must be validated to ensure it meets security requirements.
- **Resource Limits**: Resource limits must be enforced to prevent abuse.
- **Proof Verification**: The cryptographic proof must be properly verified to ensure the correctness of the computation result.
- **Blockchain Integration**: The blockchain integration must be secure to prevent tampering with the computation result.

## Performance Considerations

- **Enclave Initialization**: Enclave initialization can be time-consuming. The service should initialize the enclave once and reuse it for multiple requests.
- **Batch Processing**: For applications that require multiple computations, use batch processing to reduce the number of requests.
- **Parallel Execution**: For independent computations, use parallel execution to improve performance.
- **Resource Allocation**: Allocate resources based on the complexity of the computation to optimize performance.

## References

- [Neo Service Layer Architecture](../architecture/README.md)
- [Neo Service Layer API](../api/README.md)
- [Neo Service Layer Workflows](../workflows/README.md)
