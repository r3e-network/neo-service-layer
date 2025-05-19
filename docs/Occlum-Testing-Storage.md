# Occlum Integration, Testing Framework, and Secure Storage Guide

This document provides a comprehensive guide to the Occlum integration, testing framework, and secure storage features in the Neo Confidential Serverless Layer (NCSL).

## Table of Contents

1. [Overview](#overview)
2. [Occlum Integration](#occlum-integration)
3. [Testing Framework](#testing-framework)
4. [Secure Storage](#secure-storage)
5. [Persistent Storage](#persistent-storage)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)
8. [References](#references)

## Overview

The Neo Confidential Serverless Layer (NCSL) provides a secure execution environment for JavaScript functions using Intel SGX and the OpenEnclave SDK. The Occlum integration, testing framework, and secure storage features enhance the security, reliability, and usability of the NCSL.

## Occlum Integration

Occlum is a memory-safe, multi-process library OS for Intel SGX. It enables applications to run securely inside SGX enclaves without modification. The `OcclumManager` class provides a high-level interface for managing Occlum instances within an enclave.

### Initializing Occlum

```csharp
// Create an Occlum manager
var occlumManager = new OcclumManager(
    loggerFactory.CreateLogger<OcclumManager>(),
    enclaveInterface,
    new OcclumOptions
    {
        InstanceDir = "/tmp/occlum_instance",
        LogLevel = "info",
        NodeJsPath = "/usr/bin/node",
        TempDir = "/tmp"
    });

// Initialize Occlum
await occlumManager.InitializeAsync();
```

### Executing Commands in Occlum

```csharp
// Execute a command in Occlum
string path = "/bin/ls";
string[] args = new[] { "-la", "/tmp" };
string[] env = new[] { "PATH=/bin:/usr/bin", "HOME=/root" };
int exitCode = await occlumManager.ExecuteCommandAsync(path, args, env);

Console.WriteLine($"Command exited with code {exitCode}");
```

### Executing JavaScript in Occlum

```csharp
// Execute a JavaScript file in Occlum
string scriptPath = "/app/script.js";
string[] args = new[] { "arg1", "arg2" };
string[] env = new[] { "NODE_ENV=production" };
int exitCode = await occlumManager.ExecuteJavaScriptFileAsync(scriptPath, args, env);

Console.WriteLine($"JavaScript file exited with code {exitCode}");

// Execute JavaScript code in Occlum
string code = "console.log('Hello, world!');";
exitCode = await occlumManager.ExecuteJavaScriptCodeAsync(code, args, env);

Console.WriteLine($"JavaScript code exited with code {exitCode}");
```

### Checking Occlum Support

```csharp
// Check if Occlum support is enabled
bool isOcclumSupportEnabled = occlumManager.IsOcclumSupportEnabled();

if (isOcclumSupportEnabled)
{
    Console.WriteLine("Occlum support is enabled");
}
else
{
    Console.WriteLine("Occlum support is not enabled");
}
```

## Testing Framework

The testing framework provides a way to test enclaves and JavaScript code running inside enclaves. The `EnclaveTestRunner` class is the main entry point for running tests.

### Creating a Test

```csharp
// Create a test
var test = new EnclaveTest
{
    Name = "Addition Test",
    Description = "Tests the addition function",
    Code = "function add(a, b) { return a + b; } add(1, 2);",
    Input = "{}",
    Secrets = "{}",
    FunctionId = "add",
    UserId = "test-user",
    EnclavePath = "path/to/enclave.signed.so",
    SimulationMode = true,
    Assertions = new List<EnclaveTestAssertion>
    {
        new EnclaveTestAssertion
        {
            Name = "Output equals 3",
            Type = AssertionType.OutputEquals,
            ExpectedValue = "3"
        },
        new EnclaveTestAssertion
        {
            Name = "Execution time is less than 100ms",
            Type = AssertionType.ExecutionTimeMs,
            ExpectedValue = "100"
        }
    }
};
```

### Running a Test

```csharp
// Create a test runner
var testRunner = new EnclaveTestRunner(
    loggerFactory.CreateLogger<EnclaveTestRunner>(),
    lifecycleManager,
    jsExecutor,
    new EnclaveTestOptions
    {
        DefaultEnclavePath = "path/to/enclave.signed.so",
        DefaultSimulationMode = true,
        DefaultTimeoutMs = 30000,
        ContinueOnFailure = true
    });

// Run a test
var result = await testRunner.RunTestAsync(test);

if (result.Success)
{
    Console.WriteLine($"Test {result.TestName} passed");
    Console.WriteLine($"Output: {result.Output}");
    Console.WriteLine($"Execution time: {result.ExecutionTimeMs}ms");
    Console.WriteLine($"Gas used: {result.GasUsed}");
}
else
{
    Console.WriteLine($"Test {result.TestName} failed");
    Console.WriteLine($"Error: {result.Error}");
}

// Check assertion results
foreach (var assertionResult in result.AssertionResults)
{
    Console.WriteLine($"Assertion {assertionResult.Name}: {(assertionResult.Success ? "Passed" : "Failed")}");
    Console.WriteLine($"Message: {assertionResult.Message}");
}
```

### Running Multiple Tests

```csharp
// Create multiple tests
var tests = new List<EnclaveTest>
{
    new EnclaveTest
    {
        Name = "Addition Test",
        Code = "function add(a, b) { return a + b; } add(1, 2);",
        Assertions = new List<EnclaveTestAssertion>
        {
            new EnclaveTestAssertion
            {
                Name = "Output equals 3",
                Type = AssertionType.OutputEquals,
                ExpectedValue = "3"
            }
        }
    },
    new EnclaveTest
    {
        Name = "Subtraction Test",
        Code = "function subtract(a, b) { return a - b; } subtract(5, 3);",
        Assertions = new List<EnclaveTestAssertion>
        {
            new EnclaveTestAssertion
            {
                Name = "Output equals 2",
                Type = AssertionType.OutputEquals,
                ExpectedValue = "2"
            }
        }
    }
};

// Run multiple tests
var results = await testRunner.RunTestsAsync(tests);

// Print results
foreach (var result in results)
{
    Console.WriteLine($"Test {result.TestName}: {(result.Success ? "Passed" : "Failed")}");
}
```

## Secure Storage

The secure storage features provide a way to store sensitive data securely. The `SecureStorage` class is the main entry point for secure storage operations, now built on top of the persistent storage abstraction.

### Storing a Value

```csharp
// Create a secure storage with default file-based storage provider
var secureStorage = new SecureStorage(
    loggerFactory.CreateLogger<SecureStorage>(),
    enclaveInterface,
    new SecureStorageOptions
    {
        StorageDirectory = "secure_storage",
        EnableCaching = true,
        EnablePersistence = true
    });

// Or create a secure storage with a specific storage provider
var rocksDbOptions = new RocksDBStorageOptions
{
    StorageDirectory = "rocksdb_storage"
};
var rocksDbProvider = new RocksDBStorageProvider(
    loggerFactory.CreateLogger<RocksDBStorageProvider>(),
    rocksDbOptions);

var secureStorage = new SecureStorage(
    loggerFactory.CreateLogger<SecureStorage>(),
    enclaveInterface,
    new SecureStorageOptions
    {
        EnableCaching = true,
        EnablePersistence = true
    },
    rocksDbProvider);

// Initialize the secure storage
await secureStorage.InitializeAsync();

// Store a value
string key = "my-key";
string value = "my-value";
await secureStorage.StoreAsync(key, value);

Console.WriteLine($"Value stored for key {key}");
```

### Retrieving a Value

```csharp
// Retrieve a value
string key = "my-key";
string value = await secureStorage.RetrieveAsync(key);

if (value != null)
{
    Console.WriteLine($"Retrieved value for key {key}: {value}");
}
else
{
    Console.WriteLine($"Key {key} not found");
}
```

### Removing a Value

```csharp
// Remove a value
string key = "my-key";
bool removed = await secureStorage.RemoveAsync(key);

if (removed)
{
    Console.WriteLine($"Value removed for key {key}");
}
else
{
    Console.WriteLine($"Key {key} not found for removal");
}
```

### Checking if a Key Exists

```csharp
// Check if a key exists
string key = "my-key";
bool exists = await secureStorage.ExistsAsync(key);

if (exists)
{
    Console.WriteLine($"Key {key} exists");
}
else
{
    Console.WriteLine($"Key {key} does not exist");
}
```

### Getting All Keys

```csharp
// Get all keys
var keys = await secureStorage.GetAllKeysAsync();

Console.WriteLine($"Found {keys.Count} keys:");
foreach (var key in keys)
{
    Console.WriteLine($"- {key}");
}
```

### Clearing All Values

```csharp
// Clear all values
await secureStorage.ClearAsync();

Console.WriteLine("All values cleared");
```

## Best Practices

1. **Use Simulation Mode for Development and Testing**: Use simulation mode for development and testing to avoid the need for SGX hardware.

2. **Limit Concurrent Enclave Creations**: Limit the number of concurrent enclave creations to avoid resource exhaustion.

3. **Terminate Enclaves When Done**: Terminate enclaves when they are no longer needed to free up resources.

4. **Use Strong Assertions in Tests**: Use strong assertions in tests to verify that the code is working correctly.

5. **Handle Errors Gracefully**: Handle errors gracefully and provide meaningful error messages.

6. **Log Important Events**: Log important events like enclave creation, termination, and test execution.

7. **Use Secure Storage for Sensitive Data**: Use secure storage for sensitive data like keys and secrets.

8. **Enable Caching for Performance**: Enable caching in secure storage for better performance.

9. **Enable Persistence for Durability**: Enable persistence in secure storage for durability.

10. **Use Occlum for Running Unmodified Applications**: Use Occlum for running unmodified applications inside SGX enclaves.

## Troubleshooting

1. **Occlum Initialization Fails**: If Occlum initialization fails, check that the Occlum instance directory exists and is writable.

2. **JavaScript Execution Fails**: If JavaScript execution fails, check the error message for details.

3. **Test Execution Fails**: If test execution fails, check the assertion results for details.

4. **Secure Storage Operations Fail**: If secure storage operations fail, check that the storage directory exists and is writable.

5. **Enclave Creation Fails**: If enclave creation fails, check that the enclave file exists and is properly signed.

## Persistent Storage

The persistent storage abstraction provides a robust, fault-tolerant storage solution for the Neo Confidential Serverless Layer. It is designed to work with OpenEnclave and Occlum LibOS, providing durability, security, and flexibility.

### Available Storage Providers

- **OcclumFileStorageProvider**: A file-based provider optimized for Occlum LibOS
- **RocksDBStorageProvider**: A high-performance key-value store provider
- **LevelDBStorageProvider**: An alternative key-value store provider

### Using a Storage Provider

```csharp
// Create a storage provider
var provider = new OcclumFileStorageProvider(
    loggerFactory.CreateLogger<OcclumFileStorageProvider>(),
    new OcclumFileStorageOptions
    {
        StorageDirectory = "occlum_storage"
    });

// Initialize the provider
await provider.InitializeAsync();

// Write data
byte[] data = Encoding.UTF8.GetBytes("Hello, world!");
await provider.WriteAsync("key", data);

// Read data
byte[] retrievedData = await provider.ReadAsync("key");

// Delete data
await provider.DeleteAsync("key");

// Check if a key exists
bool exists = await provider.ExistsAsync("key");

// Get all keys
var keys = await provider.GetAllKeysAsync();

// Get metadata
var metadata = await provider.GetMetadataAsync("key");

// Flush pending writes
await provider.FlushAsync();

// Compact storage
await provider.CompactAsync();
```

### Using the Storage Factory and Manager

```csharp
// Create a storage factory
var factory = new PersistentStorageFactory(loggerFactory);

// Create a storage manager
var manager = new PersistentStorageManager(
    loggerFactory.CreateLogger<PersistentStorageManager>(),
    factory);

// Create a storage provider
var provider = await manager.CreateProviderAsync(
    "main",
    PersistentStorageProviderType.RocksDB,
    new RocksDBStorageOptions
    {
        StorageDirectory = "rocksdb_storage"
    });

// Use the provider
await provider.WriteAsync("key", data);

// Get a provider by name
var retrievedProvider = manager.GetProvider("main");

// Remove a provider
await manager.RemoveProviderAsync("main");

// Get all providers
var providers = manager.GetAllProviders();
```

### Using Transactions

```csharp
// Create a transaction
using (var transaction = new StorageTransaction(
    loggerFactory.CreateLogger<StorageTransaction>(),
    provider))
{
    // Add operations to the transaction
    await transaction.WriteAsync("key1", data1);
    await transaction.WriteAsync("key2", data2);
    await transaction.DeleteAsync("key3");

    // Commit the transaction
    await transaction.CommitAsync();
}
```

## References

- [Occlum GitHub Repository](https://github.com/occlum/occlum)
- [Occlum Documentation](https://occlum.io/occlum/docs/index.html)
- [OpenEnclave SDK GitHub Repository](https://github.com/openenclave/openenclave)
- [OpenEnclave SDK Documentation](https://github.com/openenclave/openenclave/tree/master/docs)
- [Intel SGX Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
- [RocksDB Documentation](https://rocksdb.org/docs/getting-started.html)
- [LevelDB Documentation](https://github.com/google/leveldb/blob/main/doc/index.md)
