# Neo Service Layer - Enclave Integration

## Overview

The Neo Service Layer uses Occlum LibOS enclaves for critical operations to ensure security and privacy. This document describes the enclave integration architecture and how to use it.

## Core Components

### Enclave Interface

The enclave interface is defined in `enclave_interface.h` and provides the following functions:

```c
int enclave_init();
int enclave_destroy();
int enclave_execute_js(const char* function_code, size_t function_code_size, const char* args, size_t args_size, char* result, size_t result_size, size_t* actual_result_size);
int enclave_get_data(const char* data_source, size_t data_source_size, const char* data_path, size_t data_path_size, char* result, size_t result_size, size_t* actual_result_size);
int enclave_generate_random(int min, int max, int* result);
int enclave_encrypt(const char* data, size_t data_size, const char* key, size_t key_size, char* result, size_t result_size, size_t* actual_result_size);
int enclave_decrypt(const char* data, size_t data_size, const char* key, size_t key_size, char* result, size_t result_size, size_t* actual_result_size);
int enclave_sign(const char* data, size_t data_size, const char* key, size_t key_size, char* result, size_t result_size, size_t* actual_result_size);
int enclave_verify(const char* data, size_t data_size, const char* signature, size_t signature_size, const char* key, size_t key_size, int* result);
```

### Enclave Implementation

The enclave implementation is written in C++ and runs within Occlum LibOS enclaves. It provides the implementation of the enclave interface functions.

### Enclave Wrapper

The `EnclaveWrapper` class provides a C# wrapper around the enclave interface, making it easy to call enclave functions from C# code:

```csharp
public class EnclaveWrapper : IDisposable
{
    public bool Initialize();
    public string ExecuteJavaScript(string functionCode, string args);
    public string GetData(string dataSource, string dataPath);
    public int GenerateRandom(int min, int max);
    public byte[] Encrypt(byte[] data, byte[] key);
    public byte[] Decrypt(byte[] data, byte[] key);
    public byte[] Sign(byte[] data, byte[] key);
    public bool Verify(byte[] data, byte[] signature, byte[] key);
    public void Dispose();
}
```

### Enclave Manager

The `IEnclaveManager` interface and its implementation `EnclaveManager` provide a higher-level API for interacting with the enclave:

```csharp
public interface IEnclaveManager
{
    Task<bool> InitializeEnclaveAsync();
    Task<bool> DestroyEnclaveAsync();
    Task<string> ExecuteJavaScriptAsync(string functionCode, string args);
    Task<string> GetDataAsync(string dataSource, string dataPath);
    Task<int> GenerateRandomAsync(int min, int max);
    Task<byte[]> EncryptAsync(byte[] data, byte[] key);
    Task<byte[]> DecryptAsync(byte[] data, byte[] key);
    Task<byte[]> SignAsync(byte[] data, byte[] key);
    Task<bool> VerifyAsync(byte[] data, byte[] signature, byte[] key);
}
```

### Enclave Host Service

The `EnclaveHostService` is a hosted service that manages the lifecycle of the enclave:

```csharp
public class EnclaveHostService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken);
}
```

## Building and Running the Enclave

### Building the Enclave

The enclave is built using CMake and the Occlum LibOS SDK:

```bash
# Build the enclave
cd src/Tee/NeoServiceLayer.Tee.Enclave/Enclave
mkdir build
cd build
cmake ..
make

# Create an Occlum instance
occlum init
mkdir -p image/lib
mkdir -p image/bin
cp lib/libneo_service_layer_enclave.so image/lib/

# Build the Occlum instance
occlum build
occlum sign
```

### Running the Enclave Host

The enclave host is a .NET application that loads the enclave and provides a service for interacting with it:

```bash
dotnet run --project src/Tee/NeoServiceLayer.Tee.Host/NeoServiceLayer.Tee.Host.csproj
```

## Using Enclave Integration

### Registering Enclave Services

To use enclave integration in your application, you need to register the enclave services with the dependency injection system:

```csharp
services.AddSingleton<IEnclaveManager, EnclaveManager>();
services.AddHostedService<EnclaveHostService>();
```

### Using Enclave Services

Once registered, you can inject the `IEnclaveManager` into your services and use it to interact with the enclave:

```csharp
public class MyService
{
    private readonly IEnclaveManager _enclaveManager;

    public MyService(IEnclaveManager enclaveManager)
    {
        _enclaveManager = enclaveManager;
    }

    public async Task DoSomethingAsync()
    {
        // Execute a JavaScript function in the enclave
        var result = await _enclaveManager.ExecuteJavaScriptAsync("function add(a, b) { return a + b; }", "{\"a\": 1, \"b\": 2}");

        // Get data from an external source in the enclave
        var data = await _enclaveManager.GetDataAsync("https://example.com/api", "data.value");

        // Generate a random number in the enclave
        var random = await _enclaveManager.GenerateRandomAsync(1, 100);

        // Encrypt data in the enclave
        var key = Encoding.UTF8.GetBytes("my-secret-key");
        var plaintext = Encoding.UTF8.GetBytes("my-secret-data");
        var ciphertext = await _enclaveManager.EncryptAsync(plaintext, key);

        // Decrypt data in the enclave
        var decrypted = await _enclaveManager.DecryptAsync(ciphertext, key);

        // Sign data in the enclave
        var signature = await _enclaveManager.SignAsync(plaintext, key);

        // Verify a signature in the enclave
        var isValid = await _enclaveManager.VerifyAsync(plaintext, signature, key);
    }
}
```

### Implementing Enclave-Aware Services

Services that require enclave operations can implement the `IEnclaveService` interface and inherit from the `EnclaveServiceBase` class:

```csharp
public class MyEnclaveService : EnclaveServiceBase
{
    private readonly IEnclaveManager _enclaveManager;

    public MyEnclaveService(
        ILogger<MyEnclaveService> logger,
        IEnclaveManager enclaveManager)
        : base("MyEnclaveService", "My enclave service", "1.0.0", logger)
    {
        _enclaveManager = enclaveManager;
    }

    public async Task<string> ProcessDataAsync(string data)
    {
        // Process data in the enclave
        var result = await _enclaveManager.ExecuteJavaScriptAsync(
            "function processData(data) { return data.toUpperCase(); }",
            $"{{\"data\": \"{data}\"}}");
        return result;
    }

    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        // Initialize the enclave
        return await _enclaveManager.InitializeEnclaveAsync();
    }

    protected override async Task<bool> OnInitializeAsync()
    {
        // Initialize the service
        return true;
    }

    protected override async Task<bool> OnStartAsync()
    {
        // Start the service
        return true;
    }

    protected override async Task<bool> OnStopAsync()
    {
        // Stop the service
        return true;
    }

    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check the health of the service
        return ServiceHealth.Healthy;
    }
}
```

## Security Considerations

### Enclave Security

Occlum LibOS enclaves provide the following security guarantees:

- **Confidentiality**: Code and data inside the enclave are encrypted in memory and cannot be read by the host OS or other processes.
- **Integrity**: Code and data inside the enclave cannot be modified by the host OS or other processes.
- **Attestation**: The enclave can prove its identity and the integrity of its code to remote parties.

### Data Protection

When using enclaves, consider the following data protection measures:

- **Minimize Data Transfer**: Only transfer necessary data into and out of the enclave.
- **Encrypt Sensitive Data**: Encrypt sensitive data before transferring it into the enclave.
- **Verify Results**: Verify the integrity of results returned from the enclave.

## Conclusion

The Neo Service Layer provides a secure and reliable way to execute critical operations within Occlum LibOS enclaves. By using the enclave integration components, you can easily add enclave support to your services and create secure, privacy-preserving applications.
