# Neo Service Layer - Key Management Service

## Overview

The Key Management Service provides secure key generation, storage, and management using Trusted Execution Environments (TEEs). It enables secure signing and encryption operations for blockchain applications on the Neo N3 and NeoX blockchains.

## Features

- **Secure Key Generation**: Generate cryptographic keys within SGX enclaves, ensuring that the keys are never exposed outside the enclave.
- **Secure Key Storage**: Store keys securely within the enclave, with optional export capabilities for backup purposes.
- **Key Signing**: Sign data using keys stored within the enclave, without exposing the keys.
- **Key Verification**: Verify signatures using keys stored within the enclave.
- **Key Rotation**: Rotate keys periodically to enhance security.
- **Key Revocation**: Revoke compromised keys to prevent their use.
- **Multiple Key Types**: Support for various key types, including RSA, ECDSA, and EdDSA.
- **Multiple Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Key Management Service consists of the following components:

1. **Key Management Service API**: Provides RESTful API endpoints for key management operations.
2. **Key Management Service Implementation**: Implements the key management service logic, including key generation, storage, and signing.
3. **Enclave Integration**: Integrates with the TEE for secure key operations.
4. **Blockchain Integration**: Integrates with the blockchain for transaction submission and event monitoring.

## Key Generation Process

The key generation process follows these steps:

1. **Request Validation**: The service validates the request parameters, including the blockchain type, key type, and key usage.
2. **Enclave Initialization**: If not already initialized, the service initializes the enclave.
3. **Key Generation**: The service generates a key pair within the enclave.
4. **Key Storage**: The service stores the key pair securely within the enclave.
5. **Response**: The service returns the key ID and public key to the client.

## Signing Process

The signing process follows these steps:

1. **Request Validation**: The service validates the request parameters, including the blockchain type, key ID, data, and algorithm.
2. **Enclave Initialization**: If not already initialized, the service initializes the enclave.
3. **Key Retrieval**: The service retrieves the key from the enclave.
4. **Data Signing**: The service signs the data using the key within the enclave.
5. **Response**: The service returns the signature to the client.

## Verification Process

The verification process follows these steps:

1. **Request Validation**: The service validates the request parameters, including the blockchain type, key ID, data, signature, and algorithm.
2. **Enclave Initialization**: If not already initialized, the service initializes the enclave.
3. **Key Retrieval**: The service retrieves the key from the enclave.
4. **Signature Verification**: The service verifies the signature using the key within the enclave.
5. **Response**: The service returns the verification result to the client.

## API Endpoints

### Generate Key

Generates a new key.

**URL**: `/api/v1/keys/generate`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "keyType": "secp256r1",
  "keyUsage": "signing",
  "exportable": false
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "keyId": "key-id",
    "publicKey": "public-key",
    "keyType": "secp256r1",
    "keyUsage": "signing",
    "exportable": false,
    "createdAt": "2023-01-01T00:00:00Z"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Sign Data

Signs data using a key.

**URL**: `/api/v1/keys/sign`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "keyId": "key-id",
  "data": "data-to-sign",
  "algorithm": "ECDSA"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "signature": "signature",
    "keyId": "key-id",
    "algorithm": "ECDSA",
    "timestamp": "2023-01-01T00:00:00Z"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Verify Signature

Verifies a signature.

**URL**: `/api/v1/keys/verify`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "keyId": "key-id",
  "data": "data-to-verify",
  "signature": "signature",
  "algorithm": "ECDSA"
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

### Export Key

Exports a key.

**URL**: `/api/v1/keys/export`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "keyId": "key-id",
  "format": "PEM"
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "keyId": "key-id",
    "publicKey": "public-key",
    "privateKey": "private-key",
    "format": "PEM"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Import Key

Imports a key.

**URL**: `/api/v1/keys/import`

**Method**: `POST`

**Request Body**:

```json
{
  "blockchain": "neo-n3",
  "publicKey": "public-key",
  "privateKey": "private-key",
  "keyType": "secp256r1",
  "keyUsage": "signing",
  "format": "PEM",
  "exportable": false
}
```

**Response**:

```json
{
  "success": true,
  "data": {
    "keyId": "key-id",
    "publicKey": "public-key",
    "keyType": "secp256r1",
    "keyUsage": "signing",
    "exportable": false,
    "createdAt": "2023-01-01T00:00:00Z"
  },
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

namespace KeyManagementExample
{
    [DisplayName("KeyManagementExample")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "Key Management Example")]
    public class KeyManagementExample : SmartContract
    {
        [DisplayName("SignatureVerified")]
        public static event Action<bool> OnSignatureVerified;

        public static bool VerifySignature(string keyId, string data, string signature)
        {
            // Verify the signature using the Key Management Service
            bool isValid = VerifySignatureWithKeyManagementService(keyId, data, signature);
            
            // Emit event
            OnSignatureVerified(isValid);
            
            return isValid;
        }

        private static bool VerifySignatureWithKeyManagementService(string keyId, string data, string signature)
        {
            // Call the Key Management Service to verify the signature
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

interface IKeyManagementConsumer {
    function verifySignature(string calldata keyId, string calldata data, string calldata signature) external view returns (bool);
}

contract KeyManagementExample {
    address private keyManagementContract;
    
    event SignatureVerified(bool isValid);
    
    constructor(address _keyManagementContract) {
        keyManagementContract = _keyManagementContract;
    }
    
    function verifySignature(string calldata keyId, string calldata data, string calldata signature) external returns (bool) {
        // Verify the signature using the Key Management Service
        bool isValid = IKeyManagementConsumer(keyManagementContract).verifySignature(keyId, data, signature);
        
        // Emit event
        emit SignatureVerified(isValid);
        
        return isValid;
    }
}
```

## Security Considerations

- **Enclave Security**: The security of the key management process depends on the security of the enclave. The enclave must be properly attested and verified.
- **Key Storage**: Keys must be stored securely within the enclave, with appropriate access controls.
- **Key Export**: Key export should be limited to authorized users and should be performed securely.
- **Key Rotation**: Keys should be rotated periodically to enhance security.
- **Key Revocation**: Compromised keys should be revoked immediately to prevent their use.

## Performance Considerations

- **Enclave Initialization**: Enclave initialization can be time-consuming. The service should initialize the enclave once and reuse it for multiple requests.
- **Key Caching**: Consider caching frequently used keys within the enclave to improve performance.
- **Batch Processing**: For applications that require multiple key operations, use batch processing to reduce the number of enclave transitions.

## References

- [Neo Service Layer Architecture](../architecture/README.md)
- [Neo Service Layer API](../api/README.md)
- [Neo Service Layer Workflows](../workflows/README.md)
