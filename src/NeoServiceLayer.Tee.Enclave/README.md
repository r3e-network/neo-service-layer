# Neo Service Layer: Occlum-based Confidential Computing

## Overview

Neo Service Layer is a secure JavaScript execution environment built on Intel SGX with Occlum LibOS. It provides a confidential computing platform for executing JavaScript code in secure enclaves with persistent storage, gas accounting, and user secrets management.

This implementation uses Occlum LibOS exclusively, with no OpenEnclave dependencies, to provide a production-ready solution for secure code execution.

## Architecture

The Neo Service Layer follows a layered architecture design:

1. **Core Components**:
   - `JavaScriptEngine`: Executes JavaScript code securely within the enclave
   - `OcclumInterface`: Provides access to Occlum LibOS functionality
   - `GasAccountingManager`: Tracks resource usage during code execution
   - `UserSecretManager`: Securely manages user secrets
   - `PersistentStorageService`: Provides secure, persistent storage

2. **Security Features**:
   - Intel SGX hardware-based memory encryption
   - JavaScript sandbox with restricted access
   - Encrypted storage with AES-256
   - Memory scrubbing to prevent data leakage
   - Gas limitations to prevent resource exhaustion

3. **Execution Flow**:
   ```
   Client Request → Enclave Initialization → JS Code Verification 
   → Secure Execution → Gas Accounting → Result Verification → Response
   ```

## API Reference

### JavaScript Engine

```csharp
public class JavaScriptEngine : IDisposable
{
    // Initializes a new instance of the JavaScriptEngine
    public JavaScriptEngine(ILogger<JavaScriptEngine> logger,
        GasAccountingManager gasAccountingManager,
        IOcclumInterface occlumInterface);

    // Executes JavaScript code within the Occlum enclave
    public virtual async Task<(JsonDocument Result, long GasUsed)> ExecuteAsync(
        string code, JavaScriptExecutionContext context);

    // Properly disposes of resources
    public void Dispose();
}
```

### Occlum Interface

```csharp
public interface IOcclumInterface
{
    // Initializes the Occlum environment
    Task<bool> InitializeAsync();

    // Executes a command in the Occlum instance
    Task<string> ExecuteCommandAsync(string command, string[] args);

    // Gets the instance ID of the Occlum instance
    string GetInstanceId();

    // Records execution metrics for a JavaScript function
    Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed);

    // Records execution failure for a JavaScript function
    Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage);

    // Verifies the integrity of the Occlum instance
    Task<bool> VerifyIntegrityAsync();

    // Generates a random value using the Occlum random source
    Task<byte[]> GenerateRandomBytesAsync(int size);

    // Terminates the Occlum instance
    Task TerminateAsync();
}
```

### Persistent Storage

```csharp
public interface IPersistentStorageService
{
    // Initializes the storage service
    Task InitializeAsync(PersistentStorageOptions options);

    // Reads data from storage
    Task<byte[]> ReadAsync(string key);

    // Writes data to storage
    Task WriteAsync(string key, byte[] data);

    // Deletes data from storage
    Task DeleteAsync(string key);

    // Checks if a key exists in storage
    Task<bool> ExistsAsync(string key);

    // Lists all keys in storage with a specified prefix
    Task<List<string>> ListKeysAsync(string prefix);

    // Gets the size of the data for a specified key
    Task<long> GetSizeAsync(string key);

    // Opens a stream for reading from storage
    Task<Stream> OpenReadStreamAsync(string key);

    // Opens a stream for writing to storage
    Task<Stream> OpenWriteStreamAsync(string key);

    // Flushes any pending changes to storage
    Task FlushAsync();
}
```

### Gas Accounting

```csharp
public class GasAccountingManager
{
    // Initializes a new instance of the GasAccountingManager
    public GasAccountingManager(ILogger<GasAccountingManager> logger,
        IOptions<GasAccountingOptions> options);

    // Uses gas for an operation
    public void UseGas(long amount);

    // Gets the amount of gas used so far
    public long GetGasUsed();

    // Resets the gas accounting
    public void ResetGasUsed();
}
```

## Usage Examples

### Basic JavaScript Execution

```csharp
// Initialize the services
var occlumInterface = serviceProvider.GetRequiredService<IOcclumInterface>();
var gasAccountingManager = serviceProvider.GetRequiredService<GasAccountingManager>();
var javascriptEngine = serviceProvider.GetRequiredService<JavaScriptEngine>();

// Create execution context
var context = new JavaScriptExecutionContext
{
    FunctionId = "example_function",
    UserId = "user123",
    Input = JsonDocument.Parse("{\"value\": 42}"),
    Secrets = new Dictionary<string, string> { { "API_KEY", "secret_key_value" } }
};

// Execute JavaScript code
string code = @"
function main(input) {
    // Access input values
    const value = input.value;
    
    // Use provided API
    const apiKey = getSecret('API_KEY');
    
    // Log securely
    log('Processing input: ' + value);
    
    // Return result
    return {
        result: value * 2,
        processed: true
    };
}
";

var (result, gasUsed) = await javascriptEngine.ExecuteAsync(code, context);
Console.WriteLine($"Result: {result}");
Console.WriteLine($"Gas used: {gasUsed}");
```

### Storing and Retrieving User Secrets

```csharp
// Initialize the secret manager
var secretManager = serviceProvider.GetRequiredService<UserSecretManager>();

// Store a secret
await secretManager.StoreSecretAsync("user123", "DATABASE_PASSWORD", "very-secure-password");

// Retrieve the secret
string password = await secretManager.GetSecretAsync("user123", "DATABASE_PASSWORD");

// Get multiple secrets
var secrets = await secretManager.GetSecretsAsync("user123", 
    new[] { "API_KEY", "DATABASE_PASSWORD" });

// List all secret names for a user
var secretNames = await secretManager.ListSecretNamesAsync("user123");
```

### Persistent Storage Operations

```csharp
// Initialize storage
var storageService = serviceProvider.GetRequiredService<IPersistentStorageService>();
var options = new PersistentStorageOptions
{
    StoragePath = "/data/storage",
    EnableEncryption = true,
    EnableCompression = true,
    CreateIfNotExists = true
};
await storageService.InitializeAsync(options);

// Store data
byte[] data = Encoding.UTF8.GetBytes("Hello, secure world!");
await storageService.WriteAsync("example_key", data);

// Retrieve data
byte[] retrievedData = await storageService.ReadAsync("example_key");
string retrievedString = Encoding.UTF8.GetString(retrievedData);

// Check if key exists
bool exists = await storageService.ExistsAsync("example_key");

// List keys with a prefix
var keys = await storageService.ListKeysAsync("user_");

// Delete a key
await storageService.DeleteAsync("example_key");
```

## Security Considerations

1. **Memory Security**:
   - All sensitive data is securely handled within the SGX enclave
   - Memory is explicitly cleared after use
   - Secrets are encrypted in memory and securely erased

2. **JavaScript Sandbox**:
   - Core JavaScript objects are frozen to prevent tampering
   - Dangerous global objects are disabled
   - Resource limiting via gas accounting prevents DoS attacks

3. **Storage Security**:
   - All persistent data is encrypted with AES-256
   - Each enclave instance has its own encryption key
   - Keys are securely generated using hardware random number generation

4. **Input Validation**:
   - All inputs are validated before processing
   - JSON inputs are strictly validated against schemas
   - Code input is verified for integrity

## Installation and Setup

### Prerequisites

- Intel SGX-enabled hardware
- Occlum LibOS (version 0.29.5 or later)
- Node.js (version 16.15.0 or later)
- .NET 6.0 SDK
- CMake 3.10+

### Building the Enclave

1. Clone the repository:
   ```
   git clone https://github.com/neo-project/neo-service-layer.git
   cd neo-service-layer/src/NeoServiceLayer.Tee.Enclave
   ```

2. Build the project:
   ```
   ./build_occlum.ps1
   ```

3. Running the enclave:
   ```
   # Production mode (requires SGX)
   ./run_enclave.sh

   # Simulation mode (no SGX required)
   ./run_enclave_simulation.sh
   ```

## Testing

Comprehensive tests are included to verify:
- JavaScript engine functionality
- Persistent storage operations
- Secret management
- Gas accounting accuracy
- Occlum integration

Run tests with:
```
./test_enclave.sh
```

## Troubleshooting

Common issues and solutions:

1. **Occlum initialization fails**:
   - Verify SGX is enabled in BIOS
   - Check Occlum installation with `occlum --version`
   - Ensure SGX driver is installed with `ls /dev/sgx*`

2. **JavaScript execution errors**:
   - Check for syntax errors in JS code
   - Verify gas limit is sufficient
   - Check log output for sandbox violations

3. **Storage failures**:
   - Verify permissions on storage directory
   - Check disk space availability
   - Ensure encryption key is properly generated

## License

Copyright (c) Neo Project 2022
Licensed under the [MIT License](LICENSE)
