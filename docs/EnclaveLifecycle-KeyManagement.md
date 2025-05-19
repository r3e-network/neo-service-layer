# Enclave Lifecycle and Key Management Guide

This document provides a comprehensive guide to the enclave lifecycle management and secure key management features in the Neo Confidential Serverless Layer (NCSL).

## Table of Contents

1. [Overview](#overview)
2. [Enclave Lifecycle Management](#enclave-lifecycle-management)
3. [Secure Key Management](#secure-key-management)
4. [JavaScript Execution](#javascript-execution)
5. [Integration with OpenEnclave SDK](#integration-with-openenclave-sdk)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)
8. [References](#references)

## Overview

The Neo Confidential Serverless Layer (NCSL) provides a secure execution environment for JavaScript functions using Intel SGX and the OpenEnclave SDK. The enclave lifecycle management and secure key management features provide a robust and secure way to manage enclaves and cryptographic keys.

## Enclave Lifecycle Management

The enclave lifecycle management features provide a way to create, initialize, and terminate enclaves. The `EnclaveLifecycleManager` class is the main entry point for managing enclaves.

### Creating an Enclave

```csharp
// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Create an enclave interface factory
var enclaveInterfaceFactory = new DefaultEnclaveInterfaceFactory(loggerFactory);

// Create an enclave lifecycle manager
var lifecycleManager = new EnclaveLifecycleManager(
    loggerFactory.CreateLogger<EnclaveLifecycleManager>(),
    enclaveInterfaceFactory);

// Create an enclave
var enclaveId = "my-enclave";
var enclavePath = "path/to/enclave.signed.so";
var simulationMode = true; // Use simulation mode for development and testing
var enclaveInterface = await lifecycleManager.CreateEnclaveAsync(enclaveId, enclavePath, simulationMode);
```

### Getting an Existing Enclave

```csharp
// Get an existing enclave
var enclaveId = "my-enclave";
var enclaveInterface = await lifecycleManager.GetEnclaveAsync(enclaveId);

if (enclaveInterface != null)
{
    // Use the enclave
    var result = await enclaveInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
}
else
{
    // Enclave not found
    Console.WriteLine($"Enclave {enclaveId} not found");
}
```

### Terminating an Enclave

```csharp
// Terminate an enclave
var enclaveId = "my-enclave";
var terminated = await lifecycleManager.TerminateEnclaveAsync(enclaveId);

if (terminated)
{
    Console.WriteLine($"Enclave {enclaveId} terminated successfully");
}
else
{
    Console.WriteLine($"Enclave {enclaveId} not found for termination");
}
```

### Terminating All Enclaves

```csharp
// Terminate all enclaves
var count = await lifecycleManager.TerminateAllEnclavesAsync();
Console.WriteLine($"Terminated {count} enclaves");
```

## Secure Key Management

The secure key management features provide a way to generate, store, and use cryptographic keys securely within an enclave. The `SecureKeyManager` class is the main entry point for managing keys.

### Generating a Key

```csharp
// Create a secure key manager
var keyManager = new SecureKeyManager(
    loggerFactory.CreateLogger<SecureKeyManager>(),
    enclaveInterface);

// Generate a key
var keyId = "my-key";
var keyType = KeyType.Rsa2048;
var key = await keyManager.GenerateKeyAsync(keyId, keyType);

Console.WriteLine($"Generated key {key.KeyId} of type {key.KeyType}");
```

### Signing Data with a Key

```csharp
// Sign data with a key
var keyId = "my-key";
var data = Encoding.UTF8.GetBytes("Hello, world!");
var hashAlgorithm = HashAlgorithmType.Sha256;
var signature = await keyManager.SignDataAsync(keyId, data, hashAlgorithm);

Console.WriteLine($"Signed data with key {keyId}, signature length: {signature.Length}");
```

### Verifying a Signature

```csharp
// Verify a signature
var keyId = "my-key";
var data = Encoding.UTF8.GetBytes("Hello, world!");
var hashAlgorithm = HashAlgorithmType.Sha256;
var isValid = await keyManager.VerifySignatureAsync(keyId, data, signature, hashAlgorithm);

if (isValid)
{
    Console.WriteLine("Signature is valid");
}
else
{
    Console.WriteLine("Signature is invalid");
}
```

### Deleting a Key

```csharp
// Delete a key
var keyId = "my-key";
var deleted = await keyManager.DeleteKeyAsync(keyId);

if (deleted)
{
    Console.WriteLine($"Key {keyId} deleted successfully");
}
else
{
    Console.WriteLine($"Key {keyId} not found for deletion");
}
```

## JavaScript Execution

The JavaScript execution features provide a way to execute JavaScript code securely within an enclave. The `JavaScriptExecutor` class is the main entry point for executing JavaScript code.

### Executing JavaScript Code

```csharp
// Create a JavaScript executor
var jsExecutor = new JavaScriptExecutor(
    loggerFactory.CreateLogger<JavaScriptExecutor>(),
    enclaveInterface);

// Execute JavaScript code
var code = "function add(a, b) { return a + b; } add(1, 2);";
var input = "{}";
var secrets = "{}";
var functionId = "add";
var userId = "user123";
var result = await jsExecutor.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);

if (result.Success)
{
    Console.WriteLine($"JavaScript execution successful: {result.Output}");
    Console.WriteLine($"Execution time: {result.ExecutionTimeMs}ms, Gas used: {result.GasUsed}");
}
else
{
    Console.WriteLine($"JavaScript execution failed: {result.Error}");
}
```

### Creating and Destroying JavaScript Contexts

```csharp
// Create a JavaScript context
var contextId = await jsExecutor.CreateJavaScriptContextAsync();
Console.WriteLine($"Created JavaScript context {contextId}");

// Destroy the JavaScript context
await jsExecutor.DestroyJavaScriptContextAsync(contextId);
Console.WriteLine($"Destroyed JavaScript context {contextId}");
```

## Integration with OpenEnclave SDK

The enclave lifecycle management, secure key management, and JavaScript execution features are integrated with the OpenEnclave SDK. The `OpenEnclaveInterface` class provides a high-level interface for interacting with the OpenEnclave SDK.

### Creating an OpenEnclave Interface

```csharp
// Create an OpenEnclave interface
var logger = loggerFactory.CreateLogger<OpenEnclaveInterface>();
var enclavePath = "path/to/enclave.signed.so";
var enclaveInterface = new OpenEnclaveInterface(logger, enclavePath);

// Use the enclave interface
var result = await enclaveInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);
```

### Getting Enclave Measurements

```csharp
// Get enclave measurements
var mrEnclave = enclaveInterface.MrEnclave;
var mrSigner = enclaveInterface.MrSigner;
var productId = enclaveInterface.ProductId;
var securityVersion = enclaveInterface.SecurityVersion;
var attributes = enclaveInterface.Attributes;

Console.WriteLine($"MRENCLAVE: {Convert.ToBase64String(mrEnclave)}");
Console.WriteLine($"MRSIGNER: {Convert.ToBase64String(mrSigner)}");
Console.WriteLine($"Product ID: {productId}");
Console.WriteLine($"Security Version: {securityVersion}");
Console.WriteLine($"Attributes: {attributes}");
```

### Sealing and Unsealing Data

```csharp
// Seal data
var data = Encoding.UTF8.GetBytes("Hello, world!");
var sealedData = enclaveInterface.SealData(data);
Console.WriteLine($"Sealed data length: {sealedData.Length}");

// Unseal data
var unsealedData = enclaveInterface.UnsealData(sealedData);
var unsealedString = Encoding.UTF8.GetString(unsealedData);
Console.WriteLine($"Unsealed data: {unsealedString}");
```

## Best Practices

1. **Use Simulation Mode for Development and Testing**: Use simulation mode for development and testing to avoid the need for SGX hardware.

2. **Limit Concurrent Enclave Creations**: Limit the number of concurrent enclave creations to avoid resource exhaustion.

3. **Terminate Enclaves When Done**: Terminate enclaves when they are no longer needed to free up resources.

4. **Use Strong Key Types**: Use strong key types like RSA-2048 or ECDSA P-256 for cryptographic operations.

5. **Verify Signatures**: Always verify signatures before trusting data.

6. **Handle Errors Gracefully**: Handle errors gracefully and provide meaningful error messages.

7. **Log Important Events**: Log important events like enclave creation, termination, and key generation.

## Troubleshooting

1. **Enclave Creation Fails**: If enclave creation fails, check that the enclave file exists and is properly signed.

2. **JavaScript Execution Fails**: If JavaScript execution fails, check the error message for details.

3. **Key Generation Fails**: If key generation fails, check that the enclave is properly initialized.

4. **Signature Verification Fails**: If signature verification fails, check that the correct key is being used.

5. **Enclave Termination Fails**: If enclave termination fails, check that the enclave ID is correct.

## References

- [OpenEnclave SDK GitHub Repository](https://github.com/openenclave/openenclave)
- [OpenEnclave SDK Documentation](https://github.com/openenclave/openenclave/tree/master/docs)
- [Intel SGX Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
