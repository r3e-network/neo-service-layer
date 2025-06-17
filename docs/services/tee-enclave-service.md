# TEE Enclave Service

## Overview

The TEE (Trusted Execution Environment) Enclave Service provides secure, privacy-preserving computation capabilities using Intel SGX (Software Guard Extensions) and Occlum LibOS. This service enables confidential computing scenarios while maintaining full integration with the Neo blockchain ecosystem.

## Features

### Core TEE Capabilities
- **Intel SGX Integration**: Hardware-based trusted execution environments
- **Occlum LibOS Support**: Linux application compatibility in SGX enclaves
- **Secure Storage**: Encrypted filesystem with automatic sealing/unsealing
- **Remote Attestation**: Cryptographic proof of enclave integrity
- **Secure Networking**: TLS-secured communication channels
- **JavaScript Execution**: Sandboxed JavaScript runtime for smart contracts

### Security Features
- **Data Encryption**: AES-256-GCM encryption for all sensitive data
- **Key Management**: Hardware-protected key derivation and storage
- **Memory Protection**: Hardware-enforced memory isolation
- **Code Integrity**: Cryptographic verification of enclave code
- **Side-Channel Resistance**: Protected against timing and cache attacks

### Blockchain Integration
- **Abstract Accounts**: Secure wallet operations with guardian support
- **Oracle Services**: Secure external data integration with domain validation
- **Cross-Chain Support**: Multi-blockchain compatibility with secure state management
- **Transaction Privacy**: Confidential transaction processing

## Architecture

### Component Structure

```
NeoServiceLayer.Tee.Enclave/
├── Native/                     # P/Invoke declarations
│   ├── SgxNativeApi.cs        # Intel SGX SDK bindings
│   └── OcclumNativeApi.cs     # Occlum LibOS bindings
├── Enclave/                   # SGX enclave components
│   ├── include/               # C/C++ headers
│   ├── src/                   # Trusted code implementation
│   └── Enclave.edl           # Enclave Definition Language
├── src/                       # Rust enclave implementation
│   ├── lib.rs                # Main Rust library
│   ├── crypto.rs             # Cryptographic operations
│   ├── storage.rs            # Secure storage
│   └── networking.rs         # Secure networking
├── Services/                  # C# service implementations
├── Models/                    # Data models and DTOs
└── Cargo.toml                # Rust dependencies
```

### Deployment Modes

#### Simulation Mode (`SGX_MODE=SIM`)
- **Purpose**: Development and testing without SGX hardware
- **Security**: Software-based simulation (not production-secure)
- **Platform**: Windows and Linux support
- **Use Cases**: CI/CD, local development, testing

#### Hardware Mode (`SGX_MODE=HW`)
- **Purpose**: Production deployment with SGX-enabled hardware
- **Security**: Full hardware-based TEE protection
- **Platform**: SGX-capable processors required
- **Use Cases**: Production environments, sensitive workloads

## API Reference

### Core Interfaces

#### `IEnclaveWrapper`

Primary interface for enclave operations.

```csharp
public interface IEnclaveWrapper : IDisposable
{
    Task<bool> InitializeAsync(EnclaveConfig config);
    Task<EncryptedData> SealDataAsync(byte[] data, string keyId);
    Task<byte[]> UnsealDataAsync(EncryptedData sealedData, string keyId);
    Task<SignatureResult> SignDataAsync(byte[] data, string keyId);
    Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, string publicKey);
    Task<AttestationReport> GetAttestationAsync();
    Task<NetworkResponse> SecureHttpRequestAsync(SecureHttpRequest request);
    Task<JavaScriptResult> ExecuteJavaScriptAsync(string code, JavaScriptContext context);
    Task<AbstractAccountResult> ProcessAbstractAccountAsync(AbstractAccountRequest request);
}
```

#### `IEnclaveStorageService`

Secure storage operations within the enclave.

```csharp
public interface IEnclaveStorageService
{
    Task<StorageResult> StoreSecurelyAsync(string key, byte[] data, StorageOptions options);
    Task<byte[]> RetrieveSecurelyAsync(string key, StorageOptions options);
    Task<bool> DeleteSecurelyAsync(string key);
    Task<string[]> ListKeysAsync(string prefix);
    Task<StorageStatistics> GetStatisticsAsync();
}
```

### Configuration Models

#### `EnclaveConfig`

```csharp
public class EnclaveConfig
{
    public string SGXMode { get; set; } = "SIM";
    public bool EnableDebug { get; set; } = true;
    public string OcclumVersion { get; set; } = "0.29.6";
    public CryptographyConfig Cryptography { get; set; } = new();
    public StorageConfig Storage { get; set; } = new();
    public NetworkConfig Network { get; set; } = new();
    public JavaScriptConfig JavaScript { get; set; } = new();
    public AbstractAccountConfig AbstractAccounts { get; set; } = new();
    public PerformanceConfig Performance { get; set; } = new();
}
```

#### `CryptographyConfig`

```csharp
public class CryptographyConfig
{
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
    public string SigningAlgorithm { get; set; } = "secp256k1";
    public int KeySize { get; set; } = 256;
    public bool EnableHardwareRNG { get; set; } = true;
    public TimeSpan KeyRotationInterval { get; set; } = TimeSpan.FromDays(30);
}
```

## Usage Examples

### Basic Enclave Initialization

```csharp
// Configure the enclave
var config = new EnclaveConfig
{
    SGXMode = "SIM", // Use simulation mode for development
    EnableDebug = true,
    Cryptography = new CryptographyConfig
    {
        EncryptionAlgorithm = "AES-256-GCM",
        SigningAlgorithm = "secp256k1"
    }
};

// Initialize enclave
using var enclave = new ProductionSGXEnclaveWrapper();
await enclave.InitializeAsync(config);
```

### Secure Data Storage

```csharp
// Seal sensitive data
var sensitiveData = Encoding.UTF8.GetBytes("confidential information");
var sealedData = await enclave.SealDataAsync(sensitiveData, "user-data-key");

// Store in secure storage
await storageService.StoreSecurelyAsync("user:123", sealedData.EncryptedData, new StorageOptions
{
    EnableCompression = true,
    EnableIntegrityCheck = true
});

// Retrieve and unseal
var retrievedData = await storageService.RetrieveSecurelyAsync("user:123", new StorageOptions());
var unsealedData = await enclave.UnsealDataAsync(new EncryptedData 
{ 
    EncryptedData = retrievedData 
}, "user-data-key");
```

### Cryptographic Operations

```csharp
// Generate secure signature
var messageToSign = Encoding.UTF8.GetBytes("transaction data");
var signature = await enclave.SignDataAsync(messageToSign, "signing-key");

// Verify signature
var isValid = await enclave.VerifySignatureAsync(
    messageToSign, 
    signature.Signature, 
    signature.PublicKey
);
```

### Secure HTTP Requests

```csharp
// Make secure external API call
var request = new SecureHttpRequest
{
    Url = "https://api.example.com/data",
    Method = "GET",
    Headers = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer " + await enclave.GetSecureTokenAsync()
    },
    DomainValidation = new DomainValidationConfig
    {
        AllowedDomains = new[] { "api.example.com" },
        RequireHttps = true,
        ValidateCertificate = true
    }
};

var response = await enclave.SecureHttpRequestAsync(request);
```

### JavaScript Execution

```csharp
// Execute JavaScript in secure sandbox
var jsCode = @"
    function processData(input) {
        // Secure data processing logic
        return { result: input.value * 2, timestamp: Date.now() };
    }
    processData(input);
";

var context = new JavaScriptContext
{
    Input = new { value = 42 },
    Timeout = TimeSpan.FromSeconds(5),
    MemoryLimit = 64 * 1024 * 1024, // 64 MB
    SecurityConstraints = new JavaScriptSecurityConstraints
    {
        DisallowNetworking = true,
        DisallowFileSystem = true,
        DisallowProcessExecution = true
    }
};

var result = await enclave.ExecuteJavaScriptAsync(jsCode, context);
```

### Abstract Account Operations

```csharp
// Process abstract account transaction
var accountRequest = new AbstractAccountRequest
{
    AccountId = "abstract-account-123",
    Operation = AbstractAccountOperation.ExecuteTransaction,
    TransactionData = transactionBytes,
    GuardianApprovals = guardianSignatures,
    SecurityPolicy = new AbstractAccountSecurityPolicy
    {
        RequiredApprovals = 2,
        TimeoutMinutes = 30,
        AllowedOperations = new[] { "transfer", "stake" }
    }
};

var result = await enclave.ProcessAbstractAccountAsync(accountRequest);
```

## Integration Patterns

### Dependency Injection Setup

```csharp
// Program.cs or Startup.cs
services.AddSingleton<IEnclaveWrapper, ProductionSGXEnclaveWrapper>();
services.AddScoped<IEnclaveStorageService, EnclaveStorageService>();
services.AddScoped<IEnclaveNetworkService, EnclaveNetworkService>();

// Configure enclave settings
services.Configure<EnclaveConfig>(configuration.GetSection("Enclave"));
```

### Background Service Integration

```csharp
public class EnclaveBackgroundService : BackgroundService
{
    private readonly IEnclaveWrapper _enclave;
    private readonly ILogger<EnclaveBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize enclave
        await _enclave.InitializeAsync(enclaveConfig);

        // Perform periodic attestation
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var attestation = await _enclave.GetAttestationAsync();
                _logger.LogInformation("Enclave attestation successful: {Quote}", 
                    Convert.ToBase64String(attestation.Quote));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enclave attestation failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
```

## Performance Considerations

### Optimization Guidelines

1. **Batch Operations**: Group multiple enclave calls to reduce context switching
2. **Memory Management**: Use pooled buffers for large data operations
3. **Caching**: Cache frequently accessed sealed data outside the enclave
4. **Parallel Processing**: Utilize multiple enclave instances for concurrent workloads

### Performance Metrics

| Operation | Simulation Mode | Hardware Mode | Notes |
|-----------|----------------|---------------|-------|
| Enclave Initialization | ~100ms | ~200ms | One-time cost |
| Data Sealing (1KB) | ~1ms | ~2ms | Includes encryption |
| Data Unsealing (1KB) | ~1ms | ~2ms | Includes decryption |
| Signature Generation | ~5ms | ~10ms | secp256k1 |
| Attestation | ~10ms | ~50ms | Network dependent |
| JS Execution (simple) | ~10ms | ~15ms | V8 overhead |

## Security Best Practices

### Development Guidelines

1. **Input Validation**: Always validate data before entering the enclave
2. **Output Sanitization**: Ensure enclave outputs don't leak sensitive information
3. **Error Handling**: Avoid exposing internal state through error messages
4. **Logging**: Never log sensitive data; use structured logging with redaction
5. **Key Rotation**: Implement regular key rotation policies
6. **Attestation**: Verify enclave attestation before processing sensitive operations

### Production Deployment

1. **Hardware Requirements**: Use SGX-capable processors in production
2. **Network Security**: Deploy behind firewalls with strict access controls
3. **Monitoring**: Implement comprehensive monitoring and alerting
4. **Backup**: Secure backup of sealed keys and critical data
5. **Updates**: Establish secure update procedures for enclave code

## Error Handling

### Common Error Types

#### `EnclaveInitializationException`
- **Cause**: SGX SDK not available or misconfigured
- **Resolution**: Install SGX SDK or use simulation mode

#### `SealingException`
- **Cause**: Encryption key derivation failure
- **Resolution**: Check enclave integrity and key availability

#### `AttestationException`
- **Cause**: Enclave attestation failure
- **Resolution**: Verify enclave signature and platform trust

#### `NetworkSecurityException`
- **Cause**: Domain validation or certificate verification failure
- **Resolution**: Update allowed domains and verify SSL certificates

### Error Recovery Patterns

```csharp
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (TransientEnclaveException ex) when (attempt < maxRetries)
        {
            _logger.LogWarning("Enclave operation failed (attempt {Attempt}/{MaxRetries}): {Error}", 
                attempt, maxRetries, ex.Message);
            
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
        }
    }
    
    throw new EnclaveOperationException("Operation failed after maximum retries");
}
```

## Testing

### Unit Testing with Simulation Mode

```csharp
[Test]
public async Task SealAndUnsealData_ShouldPreserveOriginalData()
{
    // Arrange
    var config = TestHelper.CreateSimulationConfig();
    using var enclave = new ProductionSGXEnclaveWrapper();
    await enclave.InitializeAsync(config);
    
    var originalData = Encoding.UTF8.GetBytes("test data");
    
    // Act
    var sealedData = await enclave.SealDataAsync(originalData, "test-key");
    var unsealedData = await enclave.UnsealDataAsync(sealedData, "test-key");
    
    // Assert
    Assert.That(unsealedData, Is.EqualTo(originalData));
}
```

### Integration Testing

```csharp
[Test]
[Category("Integration")]
public async Task CompleteWorkflow_ShouldExecuteSuccessfully()
{
    // Full workflow test including storage, networking, and cryptography
    var result = await TestHelper.ExecuteCompleteWorkflowAsync();
    Assert.That(result.Success, Is.True);
}
```

## Troubleshooting

See [TEE Troubleshooting Guide](../troubleshooting/tee-troubleshooting.md) for detailed troubleshooting information.

## Related Documentation

- [SGX Deployment Guide](../deployment/sgx-deployment-guide.md)
- [Occlum LibOS Guide](../deployment/occlum-libos-guide.md)
- [TEE API Reference](../api/tee-api-reference.md)
- [Security Architecture](../security/tee-security-architecture.md) 