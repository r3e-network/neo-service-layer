# SGX Standard Interface Guide

This guide explains how to use the standard SGX computing and storage interface in Neo Service Layer services.

## 🏗️ Overview

The standard SGX interface provides unified methods for:
- **Secure JavaScript execution** within SGX enclaves
- **Privacy-preserving storage** with automatic encryption
- **Batch operations** for multiple SGX computations
- **Session management** for related operations
- **Pre-built templates** for common use cases

## 🚀 Quick Start

### 1. Inherit from SGXComputingServiceBase

```csharp
using NeoServiceLayer.ServiceFramework.SGX;

[ServicePermissions("myservice")]
public class MyService : SGXComputingServiceBase, IMyService
{
    public MyService(
        ILogger<MyService> logger,
        IEnclaveManager enclaveManager,
        IServiceProvider serviceProvider,
        IEnclaveStorageService? enclaveStorage = null)
        : base("MyService", "Description", "1.0.0",
               logger, supportedBlockchains, enclaveManager, serviceProvider, enclaveStorage)
    {
        AddCapability<IMyService>();
        AddCapability<ISGXComputingService>();
    }
}
```

### 2. Use Standard SGX Methods

```csharp
// Secure JavaScript execution
var context = new SGXExecutionContext
{
    JavaScriptCode = @"
        function processData(params) {
            // Privacy-preserving computation
            return { success: true, result: params.data * 2 };
        }
        return processData(params);
    ",
    Parameters = new Dictionary<string, object> { ["data"] = 42 }
};

var result = await ExecuteSecureComputingAsync(context, BlockchainType.NeoN3);

// Secure data storage
var storageContext = new SGXStorageContext
{
    Key = "user-data:123",
    Data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userData)),
    ContentType = "application/json"
};

var storageResult = await StoreSecureDataAsync(storageContext, BlockchainType.NeoN3);

// Retrieve secure data
var retrievalResult = await RetrieveSecureDataAsync("user-data:123", BlockchainType.NeoN3);
```

## 📚 Core Interface Methods

### ISGXComputingService Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| `ExecuteSecureComputingAsync` | Execute JavaScript in SGX enclave | `SGXExecutionResult` |
| `StoreSecureDataAsync` | Store encrypted data in SGX | `SGXStorageResult` |
| `RetrieveSecureDataAsync` | Retrieve and decrypt SGX data | `SGXRetrievalResult` |
| `DeleteSecureDataAsync` | Securely delete SGX data | `SGXDeletionResult` |
| `ExecutePrivacyComputationAsync` | Privacy-preserving computation | `SGXComputationResult` |
| `ListStorageKeysAsync` | List accessible storage keys | `SGXKeyListResult` |
| `GetStorageMetadataAsync` | Get data metadata only | `SGXMetadataResult` |
| `ExecuteBatchOperationsAsync` | Execute multiple operations | `SGXBatchResult` |
| `CreateSecureSessionAsync` | Create operation session | `SGXSessionResult` |
| `ExecuteInSessionAsync` | Execute within session | `SGXExecutionResult` |
| `CloseSecureSessionAsync` | Close session | `SGXSessionResult` |

## 🎯 Pre-built Templates

The framework includes ready-to-use JavaScript templates:

### Available Templates

```csharp
// Get template by name
var template = SGXComputingTemplates.GetTemplate("secure_voting");
var context = SGXComputingTemplates.CreateTemplateContext("secure_encryption", parameters);

// Available templates:
// - secure_encryption     - AES-256-GCM encryption within enclave
// - secure_aggregation    - Privacy-preserving data aggregation  
// - multi_party_computation - Secure multi-party computation
// - neo_transaction       - Neo blockchain transaction processing
// - secure_voting         - Anonymous voting with audit trails
// - secure_random         - Cryptographically secure random generation
// - secure_validation     - Data validation and sanitization
```

### Using Templates

```csharp
// Secure voting example
var votingContext = SGXComputingHelpers.CreateVotingContext(
    votes: votesList,
    candidates: candidatesList,
    method: "simple_majority",
    anonymize: true
);

var votingResult = await ExecuteSecureComputingAsync(votingContext, blockchainType);

// Secure aggregation example  
var aggregationContext = SGXComputingHelpers.CreateAnalyticsContext(
    inputKeys: new List<string> { "data1", "data2", "data3" },
    outputKey: "aggregated-result",
    analyticsType: "aggregate"
);

var computationResult = await ExecutePrivacyComputationAsync(aggregationContext, blockchainType);
```

## 🔧 Helper Utilities

### SGXComputingHelpers Class

```csharp
// Create secure storage with compression
var storageContext = SGXComputingHelpers.CreateSecureStorageContext(
    key: "sensitive-data",
    data: myDataObject,
    enableCompression: true,
    metadata: additionalMetadata
);

// Create temporary session storage
var tempContext = SGXComputingHelpers.CreateTempStorageContext(
    sessionId: "session-123",
    key: "temp-data",
    data: temporaryData,
    expirationMinutes: 30
);

// Create multi-party computation context
var mpcContext = SGXComputingHelpers.CreateMPCContext(
    parties: new List<string> { "party1", "party2", "party3" },
    computation: "sum",
    threshold: 2,
    outputKey: "mpc-result"
);
```

### Validation and Utilities

```csharp
// Validate execution context
var (isValid, errors) = SGXComputingHelpers.ValidateExecutionContext(executionContext);

// Sanitize storage keys
var safeKey = SGXComputingHelpers.SanitizeStorageKey(userProvidedKey);

// Estimate computation complexity
var complexity = SGXComputingHelpers.EstimateComplexity(jsCode);
```

## 🔐 Security Features

### Automatic Permission Checking

```csharp
// Permissions are automatically checked based on operation:
// - sgx:compute:execute    - For JavaScript execution
// - sgx:storage:write      - For data storage
// - sgx:storage:read       - For data retrieval  
// - sgx:privacy:compute    - For privacy computations
// - sgx:batch:execute      - For batch operations
// - sgx:sessions:create    - For session creation
```

### Privacy Levels

```csharp
var computationContext = new SGXComputationContext
{
    // Set privacy level for the computation
    PrivacyLevel = SGXPrivacyLevel.Maximum, // Low, Medium, High, Maximum
    
    // Other settings...
};
```

### Storage Policies

```csharp
var storageContext = new SGXStorageContext
{
    Policy = new SGXStoragePolicy
    {
        SealingType = SGXSealingPolicyType.MrSigner, // MrSigner, MrEnclave, Both
        ExpiresAt = DateTime.UtcNow.AddDays(30),
        AllowSharing = false,
        Replication = new SGXReplicationPolicy 
        {
            Enabled = true,
            ReplicaCount = 2
        }
    }
};
```

## 📊 Batch Operations

### Computation Batch

```csharp
var batchContext = SGXComputingHelpers.CreateComputationBatch(
    operations: new List<(string, Dictionary<string, object>)>
    {
        (SGXComputingTemplates.GetTemplate("secure_encryption"), encryptParams),
        (SGXComputingTemplates.GetTemplate("secure_validation"), validateParams),
        (SGXComputingTemplates.GetTemplate("secure_aggregation"), aggregateParams)
    },
    isAtomic: true, // All succeed or all fail
    maxExecutionTimeMs: 300000
);

var batchResult = await ExecuteBatchOperationsAsync(batchContext, blockchainType);
```

### Storage Batch

```csharp
var storageOperations = new List<SGXStorageContext>
{
    SGXComputingHelpers.CreateSecureStorageContext("key1", data1),
    SGXComputingHelpers.CreateSecureStorageContext("key2", data2),
    SGXComputingHelpers.CreateSecureStorageContext("key3", data3)
};

var storageBatch = SGXComputingHelpers.CreateStorageBatch(storageOperations, isAtomic: true);
var batchResult = await ExecuteBatchOperationsAsync(storageBatch, blockchainType);
```

## 🔄 Session Management

### Create and Use Sessions

```csharp
// Create session for related operations
var sessionContext = SGXComputingHelpers.CreatePrivacySession(
    sessionName: "user-workflow",
    privacyLevel: SGXPrivacyLevel.High,
    timeoutMinutes: 60
);

var sessionResult = await CreateSecureSessionAsync(sessionContext, blockchainType);
var sessionId = sessionResult.SessionId;

// Execute operations in session
var operationContext = new SGXOperationContext
{
    OperationType = SGXOperationType.Computation,
    JavaScriptCode = myJavaScriptCode,
    Parameters = myParameters
};

var execResult = await ExecuteInSessionAsync(sessionId, operationContext, blockchainType);

// Close session when done
await CloseSecureSessionAsync(sessionId, blockchainType);
```

## 📈 Real-world Examples

### Example 1: Secure Voting Service

See the updated `VotingService` implementation for a complete example:

```csharp
// From VotingService.cs - demonstrates:
// 1. Inheriting from SGXComputingServiceBase
// 2. Using secure JavaScript execution for vote processing
// 3. Storing voting strategies in SGX
// 4. Privacy-preserving computation for vote tallying
// 5. Batch operations for multiple votes
```

### Example 2: Privacy-Preserving Analytics

```csharp
public class AnalyticsService : SGXComputingServiceBase, IAnalyticsService
{
    public async Task<AnalyticsResult> ComputePrivateAnalyticsAsync(
        List<string> dataKeys, 
        string analysisType,
        BlockchainType blockchainType)
    {
        // Create computation context
        var computationContext = SGXComputingHelpers.CreateAnalyticsContext(
            inputKeys: dataKeys,
            outputKey: $"analytics-result-{Guid.NewGuid()}",
            analyticsType: analysisType,
            parameters: new Dictionary<string, object>
            {
                ["preservePrivacy"] = true,
                ["aggregationLevel"] = "high"
            }
        );
        
        // Execute privacy-preserving computation
        var result = await ExecutePrivacyComputationAsync(computationContext, blockchainType);
        
        return new AnalyticsResult
        {
            Success = result.Success,
            ResultKey = result.OutputKeys.FirstOrDefault(),
            PrivacyAttestation = result.PrivacyAttestation
        };
    }
}
```

## ⚠️ Best Practices

### 1. Always Check Results

```csharp
var result = await ExecuteSecureComputingAsync(context, blockchainType);
if (!result.Success)
{
    Logger.LogError("SGX execution failed: {Error}", result.ErrorMessage);
    throw new InvalidOperationException($"SGX computation failed: {result.ErrorMessage}");
}
```

### 2. Use Appropriate Privacy Levels

```csharp
// For highly sensitive operations
computationContext.PrivacyLevel = SGXPrivacyLevel.Maximum;

// For general secure operations  
computationContext.PrivacyLevel = SGXPrivacyLevel.High;

// For less sensitive operations
computationContext.PrivacyLevel = SGXPrivacyLevel.Medium;
```

### 3. Set Reasonable Timeouts

```csharp
// Simple computations: 30 seconds
context.TimeoutMs = 30000;

// Complex analytics: 5 minutes
context.TimeoutMs = 300000;

// Multi-party computations: 10 minutes
context.TimeoutMs = 600000;
```

### 4. Handle Storage Key Collisions

```csharp
// Use service-specific prefixes (done automatically)
var key = GenerateStorageKey("user-data:123"); // Becomes "VotingService:user-data:123"

// Include timestamps for uniqueness
var uniqueKey = $"result:{DateTime.UtcNow:yyyyMMddHHmmss}:{Guid.NewGuid()}";
```

### 5. Monitor Performance

```csharp
var result = await ExecuteSecureComputingAsync(context, blockchainType);

Logger.LogInformation("SGX execution completed in {ExecutionTimeMs}ms using {MemoryBytes} bytes",
    result.Metrics.ExecutionTimeMs,
    result.Metrics.MemoryUsedBytes);
```

## 🔍 Troubleshooting

### Common Issues

1. **Permission Denied**: Ensure service has required SGX permissions
2. **Timeout Errors**: Increase timeout for complex operations
3. **Storage Key Conflicts**: Use unique, service-prefixed keys
4. **JavaScript Errors**: Validate JS code before execution
5. **Memory Errors**: Limit data size and complexity

### Debug Mode

```csharp
var context = new SGXExecutionContext
{
    JavaScriptCode = myCode,
    Parameters = myParams,
    EnableDebug = true // Enables detailed debug information
};

var result = await ExecuteSecureComputingAsync(context, blockchainType);

// Check debug information
if (result.DebugInfo.Any())
{
    Logger.LogDebug("SGX Debug Info: {@DebugInfo}", result.DebugInfo);
}
```

## 📚 Additional Resources

- [Permission Integration Guide](PERMISSION_INTEGRATION_GUIDE.md) - For permission setup
- [SGX Architecture Overview](../src/Tee/README.md) - Technical architecture details
- [JavaScript Templates](../src/ServiceFramework/NeoServiceLayer.ServiceFramework/SGX/SGXComputingTemplates.cs) - All available templates
- [Helper Utilities](../src/ServiceFramework/NeoServiceLayer.ServiceFramework/SGX/SGXComputingHelpers.cs) - Utility methods

## 🤝 Contributing

When adding new services with SGX capabilities:

1. Always inherit from `SGXComputingServiceBase`
2. Add `ISGXComputingService` capability
3. Use helper methods for common patterns
4. Follow permission naming conventions: `sgx:{category}:{action}`
5. Include integration tests for SGX operations
6. Document any custom JavaScript templates

The standard SGX interface ensures consistent, secure, and efficient SGX operations across all Neo Service Layer services.