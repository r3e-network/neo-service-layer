# Key Management Service

## Overview

The Key Management Service (KMS) is a secure, enclave-based service for managing cryptographic keys. It provides a comprehensive set of operations for creating, using, and managing cryptographic keys in a secure manner.

## Features

- **Secure Key Generation**: Generate cryptographic keys of various types (Secp256k1, Ed25519, RSA) within the secure enclave.
- **Key Storage**: Store keys securely within the enclave, with optional exportability.
- **Cryptographic Operations**: Sign data, verify signatures, encrypt data, and decrypt data using the stored keys.
- **Key Metadata**: Maintain metadata about keys, including creation date, last used date, and custom attributes.
- **Access Control**: Control access to keys through access control lists.
- **Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Key Management Service is built on the Neo Service Layer framework and uses the Trusted Execution Environment (TEE) to provide secure key management. The service consists of the following components:

- **IKeyManagementService**: The interface that defines the operations supported by the service.
- **KeyManagementService**: The implementation of the service that uses the enclave to perform cryptographic operations.
- **EnclaveManager**: The component that manages the communication with the enclave.

## Usage

### Service Registration

```csharp
// Register the service
services.AddNeoService<IKeyManagementService, KeyManagementService>();

// Register the service with the service registry
serviceProvider.RegisterAllNeoServices();
```

### Key Generation

```csharp
// Generate a new key
var keyMetadata = await keyManagementService.GenerateKeyAsync(
    "my-key",
    "Secp256k1",
    "Sign,Verify",
    false,
    "My signing key",
    BlockchainType.NeoN3);
```

### Signing Data

```csharp
// Sign data using a key
string dataHex = "48656c6c6f20776f726c64"; // "Hello world" in hex
string signatureHex = await keyManagementService.SignDataAsync(
    "my-key",
    dataHex,
    "ECDSA",
    BlockchainType.NeoN3);
```

### Verifying Signatures

```csharp
// Verify a signature
bool isValid = await keyManagementService.VerifySignatureAsync(
    "my-key",
    dataHex,
    signatureHex,
    "ECDSA",
    BlockchainType.NeoN3);
```

### Encrypting Data

```csharp
// Encrypt data using a key
string encryptedDataHex = await keyManagementService.EncryptDataAsync(
    "my-key",
    dataHex,
    "ECIES",
    BlockchainType.NeoN3);
```

### Decrypting Data

```csharp
// Decrypt data using a key
string decryptedDataHex = await keyManagementService.DecryptDataAsync(
    "my-key",
    encryptedDataHex,
    "ECIES",
    BlockchainType.NeoN3);
```

## Security Considerations

- All cryptographic operations are performed within the secure enclave.
- Private keys never leave the enclave.
- Access to keys is controlled through the service interface.
- All operations are logged for audit purposes.

## API Reference

### GenerateKeyAsync

Generates a new key.

```csharp
Task<KeyMetadata> GenerateKeyAsync(
    string keyId,
    string keyType,
    string keyUsage,
    bool exportable,
    string description,
    BlockchainType blockchainType);
```

### GetKeyMetadataAsync

Gets key metadata.

```csharp
Task<KeyMetadata> GetKeyMetadataAsync(
    string keyId,
    BlockchainType blockchainType);
```

### ListKeysAsync

Lists keys.

```csharp
Task<IEnumerable<KeyMetadata>> ListKeysAsync(
    int skip,
    int take,
    BlockchainType blockchainType);
```

### SignDataAsync

Signs data using a key.

```csharp
Task<string> SignDataAsync(
    string keyId,
    string dataHex,
    string signingAlgorithm,
    BlockchainType blockchainType);
```

### VerifySignatureAsync

Verifies a signature.

```csharp
Task<bool> VerifySignatureAsync(
    string keyIdOrPublicKeyHex,
    string dataHex,
    string signatureHex,
    string signingAlgorithm,
    BlockchainType blockchainType);
```

### EncryptDataAsync

Encrypts data using a key.

```csharp
Task<string> EncryptDataAsync(
    string keyIdOrPublicKeyHex,
    string dataHex,
    string encryptionAlgorithm,
    BlockchainType blockchainType);
```

### DecryptDataAsync

Decrypts data using a key.

```csharp
Task<string> DecryptDataAsync(
    string keyId,
    string encryptedDataHex,
    string encryptionAlgorithm,
    BlockchainType blockchainType);
```

### DeleteKeyAsync

Deletes a key.

```csharp
Task<bool> DeleteKeyAsync(
    string keyId,
    BlockchainType blockchainType);
```
