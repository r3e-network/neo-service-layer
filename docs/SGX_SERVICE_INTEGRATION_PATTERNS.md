# SGX Service Integration Patterns

## Overview

This document describes the patterns and best practices for integrating Intel SGX (Software Guard Extensions) with Neo Service Layer services to provide privacy-preserving computation and secure data storage.

## Architecture

### Core Components

1. **Enclave Manager** (`IEnclaveManager`)
   - Central interface for all SGX operations
   - Manages enclave lifecycle
   - Provides JavaScript execution environment (Deno)
   - Handles secure data operations

2. **Privacy Computing JavaScript Templates**
   - Service-specific JavaScript code executed in SGX
   - Implements privacy-preserving algorithms
   - Located in `NeoServiceLayer.Services.Core.SGX.PrivacyComputingJavaScriptTemplates`

3. **Service Enclave Operations**
   - Partial classes extending service functionality
   - Named as `{ServiceName}.EnclaveOperations.cs`
   - Contains privacy-preserving implementations

## Integration Patterns

### Pattern 1: Privacy-Preserving Operation Wrapper

Each service implements a wrapper pattern for sensitive operations:

```csharp
private async Task<PrivacyResult> ProcessWithPrivacyAsync(RequestData request)
{
    if (_enclaveManager == null)
    {
        // Fallback for non-SGX environments
        return GenerateMockPrivacyResult(request);
    }

    var jsParams = new
    {
        operation = "process",
        data = request,
        metadata = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
    };

    string paramsJson = JsonSerializer.Serialize(jsParams);
    
    string result = await _enclaveManager.ExecuteJavaScriptAsync(
        PrivacyComputingJavaScriptTemplates.ServiceOperations,
        paramsJson);

    return ParsePrivacyResult(result);
}
```

### Pattern 2: Dual-Mode Operation

Services support both SGX and non-SGX modes:

```csharp
public async Task<Result> PerformOperationAsync(Request request)
{
    // Privacy-preserving operations (always attempted)
    var privacyResult = await ProcessWithPrivacyAsync(request);
    
    // Core business logic
    var businessResult = await ExecuteBusinessLogicAsync(request);
    
    // Combine results with privacy metadata
    return new Result
    {
        Data = businessResult,
        Metadata = new Dictionary<string, object>
        {
            ["privacy_proof"] = privacyResult.Proof,
            ["data_hash"] = privacyResult.DataHash,
            ["enclave_verified"] = _enclaveManager?.IsInitialized ?? false
        }
    };
}
```

### Pattern 3: JavaScript Template Structure

Each service has a standardized JavaScript template:

```javascript
function processServiceOperation(params) {
    const { operation, data, metadata } = JSON.parse(params);
    
    switch (operation) {
        case 'process':
            return processData(data, metadata);
        case 'validate':
            return validateData(data, metadata);
        case 'batch':
            return processBatch(data, metadata);
        default:
            return JSON.stringify({
                success: false,
                error: 'Invalid operation'
            });
    }
}

function hash(data) {
    // SHA-256 hashing implementation
    return sha256(JSON.stringify(data));
}

// Service-specific privacy-preserving functions
function processData(data, metadata) {
    // Implementation
    return JSON.stringify({
        success: true,
        result: {
            // Privacy-preserving result
        }
    });
}
```

## Service-Specific Implementations

### 1. Abstract Account Service

**Privacy Features:**
- Witness validation without exposing private keys
- Transaction anonymization
- Multi-signature privacy

**Key Methods:**
- `ProcessAbstractAccountTransactionAsync`
- `ValidateWitnessesWithPrivacyAsync`

### 2. Voting Service

**Privacy Features:**
- Anonymous voting with zero-knowledge proofs
- Vote nullifiers to prevent double voting
- Homomorphic vote aggregation

**Key Methods:**
- `ExecutePrivacyPreservingVotingAsync`
- `GenerateVoteProofAsync`

### 3. Social Recovery Service

**Privacy Features:**
- Guardian identity hashing
- Anonymous approval collection
- Recovery proof generation

**Key Methods:**
- `ProcessGuardianApprovalAsync`
- `ValidateRecoveryWithPrivacyAsync`

### 4. Key Management Service

**Privacy Features:**
- Key derivation in enclave
- Access control without key exposure
- Secure key rotation

**Key Methods:**
- `CreateKeyWithPrivacyAsync`
- `SignDataWithPrivacyAsync`
- `RotateKeyWithPrivacyAsync`

### 5. Zero Knowledge Service

**Privacy Features:**
- True zero-knowledge proof generation
- Witness protection
- Secure computation verification

**Key Methods:**
- `GenerateProofInEnclaveAsync`
- `VerifyProofInEnclaveAsync`
- `ExecutePrivacyComputationAsync`

### 6. Smart Contract Service

**Privacy Features:**
- Contract code validation
- Deployer anonymization
- Invocation privacy

**Key Methods:**
- `DeployContractWithPrivacyAsync`
- `InvokeContractWithPrivacyAsync`

### 7. Oracle Service

**Privacy Features:**
- Data source reputation validation
- Request anonymization
- Batch processing privacy

**Key Methods:**
- `FetchDataWithPrivacyAsync`
- `ValidateDataSourceReputationAsync`
- `ProcessBatchRequestsWithPrivacyAsync`

### 8. Notification Service

**Privacy Features:**
- Recipient anonymization
- Delivery proof generation
- Content privacy

**Key Methods:**
- `ProcessNotificationWithPrivacyAsync`
- `ValidateRecipientWithPrivacyAsync`
- `AnonymizeNotificationContentAsync`

## Best Practices

### 1. Error Handling

Always provide fallback mechanisms for non-SGX environments:

```csharp
if (_enclaveManager == null)
{
    // Provide degraded but functional service
    return GenerateMockResult();
}
```

### 2. Data Minimization

Only send necessary data to the enclave:

```csharp
var enclaveData = new
{
    // Only essential fields
    id = request.Id,
    hash = HashSensitiveData(request.SensitiveField),
    // Avoid sending raw sensitive data
};
```

### 3. Result Verification

Always verify enclave results:

```csharp
var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
{
    throw new InvalidOperationException("Enclave operation failed");
}
```

### 4. Consistent Hashing

Use SHA-256 for all hashing operations:

```csharp
private string HashData(string data)
{
    var hash = System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(data));
    return Convert.ToBase64String(hash);
}
```

### 5. Metadata Enrichment

Always include privacy metadata in responses:

```csharp
result.Metadata = new Dictionary<string, object>
{
    ["privacy_proof_id"] = privacyResult.ProofId,
    ["data_hash"] = privacyResult.DataHash,
    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    ["enclave_verified"] = true
};
```

## Testing

### Unit Tests

1. Test privacy-preserving operations with mock enclave
2. Verify fallback behavior
3. Test JavaScript template syntax

### Integration Tests

1. Test service interactions with real/mock enclave
2. Verify privacy metadata propagation
3. Test concurrent SGX operations

Example test:

```csharp
[Fact]
public async Task Service_Should_UsePrivacyPreservingOperations()
{
    // Arrange
    var service = _serviceProvider.GetRequiredService<IService>();
    
    // Act
    var result = await service.PerformOperationAsync(request);
    
    // Assert
    result.Metadata.Should().ContainKey("privacy_proof_id");
    result.Metadata.Should().ContainKey("data_hash");
}
```

## Security Considerations

1. **Enclave Attestation**: Verify enclave integrity before sensitive operations
2. **Side-Channel Protection**: Use constant-time operations where possible
3. **Memory Safety**: Clear sensitive data after use
4. **Input Validation**: Validate all inputs before enclave processing
5. **Output Sanitization**: Ensure enclave outputs don't leak sensitive information

## Performance Optimization

1. **Batch Operations**: Group multiple operations when possible
2. **Caching**: Cache non-sensitive enclave results
3. **Async Processing**: Use async/await for all enclave operations
4. **Resource Management**: Properly dispose enclave resources

## Migration Guide

To add SGX support to a new service:

1. Create `{ServiceName}.EnclaveOperations.cs` partial class
2. Add JavaScript template to `PrivacyComputingJavaScriptTemplates`
3. Implement privacy-preserving wrappers for sensitive operations
4. Update service methods to use privacy wrappers
5. Add privacy metadata to responses
6. Create unit and integration tests
7. Document service-specific privacy features

## Troubleshooting

### Common Issues

1. **Enclave Not Initialized**
   - Check enclave manager configuration
   - Verify SGX hardware support
   - Check Occlum runtime installation

2. **JavaScript Execution Errors**
   - Validate JavaScript syntax
   - Check parameter serialization
   - Verify Deno runtime compatibility

3. **Performance Degradation**
   - Monitor enclave memory usage
   - Optimize JavaScript code
   - Consider batch processing

### Debug Logging

Enable detailed logging for troubleshooting:

```csharp
Logger.LogDebug("Privacy operation: {Operation}, DataHash: {Hash}", 
    operation, dataHash);
```

## Future Enhancements

1. **Homomorphic Encryption**: Add support for computations on encrypted data
2. **Secure Multi-Party Computation**: Enable collaborative privacy-preserving operations
3. **Hardware Security Module Integration**: Support additional hardware security options
4. **Advanced Zero-Knowledge Protocols**: Implement zk-SNARKs and zk-STARKs
5. **Federated Learning**: Enable privacy-preserving machine learning

## References

- [Intel SGX Developer Guide](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
- [Occlum Documentation](https://occlum.io/docs/)
- [Deno Security Model](https://deno.land/manual/runtime/permission_apis)
- [Zero-Knowledge Proof Protocols](https://zkp.science/)
- [Privacy-Preserving Computation Patterns](https://eprint.iacr.org/)