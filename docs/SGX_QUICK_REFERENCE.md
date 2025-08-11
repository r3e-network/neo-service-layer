# SGX Integration Quick Reference

## Quick Start Checklist

- [ ] Create `{ServiceName}.EnclaveOperations.cs` partial class
- [ ] Add JavaScript template to `PrivacyComputingJavaScriptTemplates`
- [ ] Implement privacy wrapper methods
- [ ] Update service methods to call privacy wrappers
- [ ] Add privacy metadata to responses
- [ ] Create tests for SGX operations
- [ ] Update service documentation

## Code Templates

### 1. Enclave Operations File Template

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Core.SGX;

namespace NeoServiceLayer.Services.{ServiceName};

public partial class {ServiceName}Service
{
    private async Task<Privacy{Operation}Result> Process{Operation}WithPrivacyAsync(
        {RequestType} request)
    {
        if (_enclaveManager == null)
        {
            return new Privacy{Operation}Result
            {
                Success = true,
                DataHash = HashData(request.ToString())
            };
        }

        var jsParams = new
        {
            operation = "{operation}",
            data = request,
            metadata = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
        };

        string paramsJson = JsonSerializer.Serialize(jsParams);
        string result = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.{ServiceName}Operations,
            paramsJson);

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);
        // Parse and return result
    }

    private string HashData(string data)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
```

### 2. JavaScript Template

```javascript
public const string {ServiceName}Operations = @"
function process{ServiceName}Operation(params) {
    const { operation, data, metadata } = JSON.parse(params);
    
    switch (operation) {
        case '{operation1}':
            return process{Operation1}(data, metadata);
        case '{operation2}':
            return process{Operation2}(data, metadata);
        default:
            return JSON.stringify({
                success: false,
                error: 'Invalid operation: ' + operation
            });
    }
}

function hash(data) {
    const crypto = require('crypto');
    return crypto.createHash('sha256').update(JSON.stringify(data)).digest('hex');
}

function process{Operation1}(data, metadata) {
    // Privacy-preserving logic here
    const dataHash = hash(data);
    
    return JSON.stringify({
        success: true,
        result: {
            dataHash: dataHash,
            timestamp: metadata.timestamp,
            // Additional privacy-preserving results
        }
    });
}

process{ServiceName}Operation(params);
";
```

### 3. Service Method Update Template

```csharp
public async Task<ServiceResult> PerformOperationAsync(Request request)
{
    // Add privacy-preserving operations
    var privacyResult = await ProcessOperationWithPrivacyAsync(request);
    
    Logger.LogDebug("Privacy operation completed: DataHash={Hash}", 
        privacyResult.DataHash);
    
    // Original business logic
    var result = await ExecuteBusinessLogicAsync(request);
    
    // Enrich with privacy metadata
    result.Metadata = new Dictionary<string, object>
    {
        ["privacy_proof_id"] = privacyResult.ProofId,
        ["data_hash"] = privacyResult.DataHash,
        ["enclave_verified"] = _enclaveManager?.IsInitialized ?? false
    };
    
    return result;
}
```

### 4. Test Template

```csharp
[Fact]
public async Task {ServiceName}_Should_UsePrivacyPreservingOperations()
{
    // Arrange
    var service = _serviceProvider.GetRequiredService<I{ServiceName}Service>();
    var request = new {RequestType} { /* test data */ };
    
    // Act
    var result = await service.PerformOperationAsync(request);
    
    // Assert
    result.Should().NotBeNull();
    result.Metadata.Should().ContainKey("privacy_proof_id");
    result.Metadata.Should().ContainKey("data_hash");
    result.Metadata["enclave_verified"].Should().BeOneOf(true, false);
}
```

## Common Privacy Patterns

### 1. Data Hashing
```csharp
var sensitiveDataHash = HashData(sensitiveData);
// Never store or transmit sensitiveData, only sensitiveDataHash
```

### 2. Anonymous Identifiers
```csharp
var userHash = HashData(userId);
var anonymousId = userHash.Substring(0, 16);
```

### 3. Zero-Knowledge Proofs
```csharp
var proof = new
{
    commitment = HashData(secretValue),
    challenge = GenerateChallenge(),
    response = ComputeResponse(secretValue, challenge)
};
```

### 4. Nullifiers (Prevent Double Actions)
```csharp
var nullifier = HashData($"{userId}:{actionId}:{nonce}");
// Store nullifier to prevent replay
```

## Privacy Metadata Keys

Standard metadata keys to include in responses:

- `privacy_proof_id` - Unique identifier for the privacy operation
- `data_hash` - Hash of the processed data
- `enclave_verified` - Whether operation was performed in SGX
- `timestamp` - Unix timestamp of the operation
- `nullifier` - For operations that should not be repeated
- `commitment` - For zero-knowledge proofs
- `source_proof` - For data origin verification

## Common Issues & Solutions

### Issue: Enclave not available
```csharp
if (_enclaveManager == null)
{
    // Provide fallback
    return GenerateMockResult();
}
```

### Issue: JavaScript execution fails
```csharp
try
{
    string result = await _enclaveManager.ExecuteJavaScriptAsync(template, params);
}
catch (Exception ex)
{
    Logger.LogError(ex, "JavaScript execution failed");
    // Handle gracefully
}
```

### Issue: Result parsing errors
```csharp
if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
{
    throw new InvalidOperationException($"Operation failed: {resultJson}");
}
```

## Performance Tips

1. **Batch Operations**: Process multiple items in one enclave call
2. **Cache Results**: Cache non-sensitive enclave results
3. **Async All The Way**: Always use async/await
4. **Minimize Data**: Send only essential data to enclave

## Security Reminders

- ✅ Always hash sensitive data before logging
- ✅ Validate all inputs before enclave processing
- ✅ Use constant-time operations for crypto
- ✅ Clear sensitive variables after use
- ✅ Never log private keys or secrets
- ❌ Don't expose raw user data in responses
- ❌ Don't skip enclave result validation
- ❌ Don't use weak hashing algorithms

## Testing Checklist

- [ ] Unit test privacy wrapper methods
- [ ] Test fallback behavior when enclave unavailable
- [ ] Verify privacy metadata in responses
- [ ] Test concurrent enclave operations
- [ ] Validate JavaScript template execution
- [ ] Check error handling paths

## Useful Commands

```bash
# Run SGX-specific tests
dotnet test --filter "FullyQualifiedName~SGX"

# Check enclave logs
docker logs neo-service-layer-enclave

# Verify SGX support
oesgx info

# Test JavaScript template
deno eval "$(cat template.js)"
```