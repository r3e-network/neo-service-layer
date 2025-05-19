# Neo Service Layer (NSL) - User Guide

## Introduction

The Neo Service Layer (NSL) is a secure computing environment that uses Trusted Execution Environments (TEEs) to run JavaScript functions in a confidential manner. This guide provides instructions on how to use the NSL, including its features for User Secrets, Event Triggers, GAS Accounting, Provably Fair Randomness, and Compliance Verification.

**Important**: The NSL does not execute smart contracts directly. Instead, it works with Neo N3 smart contracts in the following way:

1. Neo N3 smart contracts emit events to request JavaScript execution in the enclave
2. The NSL detects these events and executes the corresponding JavaScript functions
3. JavaScript functions can access user secrets securely stored in the enclave
4. JavaScript functions can send callback transactions to the Neo N3 blockchain with results

This architecture combines the transparency and auditability of blockchain smart contracts with the confidentiality and privacy of secure enclaves.

For a visual representation of the architecture and workflows, see:
- [NSL Architecture Diagram](diagrams/NSL_Architecture.md)
- [JavaScript Execution Workflow](diagrams/JavaScript_Execution_Workflow.md)
- [Event Trigger Workflow](diagrams/Event_Trigger_Workflow.md)
- [Randomness Service Workflow](diagrams/Randomness_Service_Workflow.md)
- [Compliance Service Workflow](diagrams/Compliance_Service_Workflow.md)

## Getting Started

### Prerequisites

- .NET 9.0 or later
- OpenEnclave SDK 0.17.0 or later
- Visual Studio 2022 or later (for Windows)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/neo-project/neo-service-layer.git
   cd neo-service-layer
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the tests:
   ```bash
   dotnet test
   ```

## JavaScript Execution

The NCSL allows you to execute JavaScript functions securely within a TEE. Here's how to use this feature:

```csharp
// Initialize the enclave host
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var enclaveHost = new TeeEnclaveHost(loggerFactory, simulationMode: true);
var enclaveInterface = enclaveHost.GetEnclaveInterface();

// Execute a JavaScript function
string jsCode = @"
    function main(input) {
        return { result: 'Hello, ' + input.name };
    }
";
string input = @"{ ""name"": ""World"" }";
string secrets = @"{}";
string functionId = "test-function";
string userId = "test-user";

var result = await enclaveInterface.ExecuteJavaScriptAsync(
    jsCode, input, secrets, functionId, userId);

Console.WriteLine(result.Result); // Output: {"result":"Hello, World"}
```

## User Secrets

The NCSL provides a secure way to store and retrieve user secrets. Here's how to use this feature:

```csharp
// Store a secret
bool storeResult = await enclaveInterface.StoreUserSecretAsync(
    "user123", "api-key", "my-secret-api-key");

// Retrieve a secret
string secret = await enclaveInterface.GetUserSecretAsync(
    "user123", "api-key");

// Delete a secret
bool deleteResult = await enclaveInterface.DeleteUserSecretAsync(
    "user123", "api-key");
```

## Event Triggers

The NCSL allows you to register event triggers that execute JavaScript functions in response to specific events. Here's how to use this feature:

### Blockchain Event Triggers

```csharp
// Register a blockchain event trigger
string eventType = "blockchain";
string functionId = "transfer-handler";
string userId = "user123";
string condition = @"{
    ""event_type"": ""transfer"",
    ""contract_address"": ""0x1234567890abcdef""
}";

string triggerId = await enclaveInterface.RegisterTriggerAsync(
    eventType, functionId, userId, condition);

// Process a blockchain event
string eventData = @"{
    ""type"": ""transfer"",
    ""contract"": ""0x1234567890abcdef"",
    ""from"": ""0xabcdef1234567890"",
    ""to"": ""0x0987654321fedcba"",
    ""amount"": 100
}";

int processedCount = await enclaveInterface.ProcessBlockchainEventAsync(eventData);
```

### Scheduled Event Triggers

```csharp
// Register a scheduled event trigger
string eventType = "schedule";
string functionId = "daily-report";
string userId = "user123";
string condition = @"{
    ""interval_seconds"": 86400
}";

string triggerId = await enclaveInterface.RegisterTriggerAsync(
    eventType, functionId, userId, condition);

// Process scheduled triggers
ulong currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
int processedCount = await enclaveInterface.ProcessScheduledTriggersAsync(currentTime);
```

### Managing Triggers

```csharp
// Get triggers for an event type
string[] triggers = await enclaveInterface.GetTriggersForEventAsync("blockchain");

// Get trigger information
string triggerInfo = await enclaveInterface.GetTriggerInfoAsync(triggerId);

// Unregister a trigger
bool unregisterResult = await enclaveInterface.UnregisterTriggerAsync(triggerId);
```

## Provably Fair Randomness

The NCSL provides a provably fair randomness service that allows you to generate random numbers that can be verified by external parties. Here's how to use this feature:

```csharp
// Generate a random number
ulong min = 1;
ulong max = 100;
string userId = "user123";
string requestId = Guid.NewGuid().ToString();

ulong randomNumber = await enclaveInterface.GenerateRandomNumberAsync(
    min, max, userId, requestId);

// Get the proof for the random number
string proof = await enclaveInterface.GetRandomNumberProofAsync(
    randomNumber, min, max, userId, requestId);

// Verify the random number
bool isValid = await enclaveInterface.VerifyRandomNumberAsync(
    randomNumber, min, max, userId, requestId, proof);

// Generate a random seed
string seed = await enclaveInterface.GenerateSeedAsync(userId, requestId);
```

## Compliance Verification

The NCSL provides a compliance verification service that allows you to verify JavaScript code for compliance with regulatory requirements. Here's how to use this feature:

```csharp
// Verify code compliance
string code = @"
    function main(input) {
        let result = 0;
        for (let i = 0; i < 100; i++) {
            result += i;
        }
        return { result: result };
    }
";
string userId = "user123";
string functionId = "test-function";
string complianceRules = @"{
    ""jurisdiction"": ""global"",
    ""prohibited_apis"": [""eval"", ""Function"", ""setTimeout"", ""setInterval"", ""XMLHttpRequest"", ""fetch""],
    ""prohibited_data"": [""password"", ""credit_card"", ""ssn"", ""passport""],
    ""allow_network_access"": false,
    ""max_gas"": 1000000
}";

string result = await enclaveInterface.VerifyComplianceAsync(
    code, userId, functionId, complianceRules);

// Get compliance status
string status = await enclaveInterface.GetComplianceStatusAsync(
    functionId, "global");

// Get compliance rules
string rules = await enclaveInterface.GetComplianceRulesAsync("global");

// Set compliance rules
bool setResult = await enclaveInterface.SetComplianceRulesAsync(
    "global", complianceRules);

// Verify identity
string identityData = @"{
    ""name"": ""John Doe"",
    ""email"": ""john.doe@example.com"",
    ""address"": ""123 Main St, Anytown, USA"",
    ""phone"": ""+1-555-123-4567""
}";

string verificationResult = await enclaveInterface.VerifyIdentityAsync(
    userId, identityData, "US");

// Get identity status
string identityStatus = await enclaveInterface.GetIdentityStatusAsync(
    userId, "US");
```

## GAS Accounting

The NCSL tracks the computational resources used by JavaScript functions using a gas accounting system. Here's how to use this feature:

```csharp
// Reset gas used
await enclaveInterface.ResetGasUsedAsync();

// Get gas used
ulong gasUsed = await enclaveInterface.GetGasUsedAsync();

// Execute a JavaScript function and get gas used
var result = await enclaveInterface.ExecuteJavaScriptAsync(
    jsCode, input, secrets, functionId, userId);

ulong gasUsedAfterExecution = result.GasUsed;
```

## Persistent Storage

The NCSL provides a persistent storage system for storing data securely. Here's how to use this feature:

```csharp
// Store data
string key = "test-key";
byte[] data = Encoding.UTF8.GetBytes("This is test data");

bool storeResult = await enclaveInterface.StorePersistentDataAsync(key, data);

// Retrieve data
byte[] retrievedData = await enclaveInterface.RetrievePersistentDataAsync(key);

// Check if key exists
bool keyExists = await enclaveInterface.PersistentDataExistsAsync(key);

// Remove data
bool removeResult = await enclaveInterface.RemovePersistentDataAsync(key);

// List keys
string[] keys = await enclaveInterface.ListPersistentDataAsync();
```

## Conclusion

The Neo Service Layer (NSL) provides a secure computing environment for executing JavaScript functions. By using the features described in this guide, you can build secure, confidential applications that protect user data and provide provably fair randomness and compliance verification.

For more detailed information, see the [Enclave Documentation](NeoServiceLayer.Tee.Enclave.md).
