# Neo Service Layer (NSL) - API Reference

## ITeeInterface

The `ITeeInterface` interface provides methods for interacting with the enclave. This interface is implemented by the `OpenEnclaveInterface` class.

### JavaScript Execution

#### ExecuteJavaScriptAsync

```csharp
Task<JavaScriptExecutionResult> ExecuteJavaScriptAsync(
    string code,
    string input,
    string secrets,
    string functionId,
    string userId);
```

Executes a JavaScript function securely within the enclave.

- **Parameters**:
  - `code`: The JavaScript code to execute.
  - `input`: The input data for the JavaScript function (JSON string).
  - `secrets`: The secrets to make available to the JavaScript function (JSON string).
  - `functionId`: The ID of the function.
  - `userId`: The ID of the user who owns the function.
- **Returns**: A `JavaScriptExecutionResult` object containing the result of the execution.

#### StoreJavaScriptFunctionAsync

```csharp
Task<bool> StoreJavaScriptFunctionAsync(
    string functionId,
    string code,
    string userId);
```

Stores a JavaScript function in the enclave.

- **Parameters**:
  - `functionId`: The ID of the function.
  - `code`: The JavaScript code to store.
  - `userId`: The ID of the user who owns the function.
- **Returns**: `true` if the function was stored successfully, `false` otherwise.

### User Secrets

#### StoreUserSecretAsync

```csharp
Task<bool> StoreUserSecretAsync(
    string userId,
    string secretName,
    string secretValue);
```

Stores a user secret in the enclave.

- **Parameters**:
  - `userId`: The ID of the user who owns the secret.
  - `secretName`: The name of the secret.
  - `secretValue`: The value of the secret.
- **Returns**: `true` if the secret was stored successfully, `false` otherwise.

#### GetUserSecretAsync

```csharp
Task<string> GetUserSecretAsync(
    string userId,
    string secretName);
```

Retrieves a user secret from the enclave.

- **Parameters**:
  - `userId`: The ID of the user who owns the secret.
  - `secretName`: The name of the secret.
- **Returns**: The value of the secret, or an empty string if the secret does not exist.

#### DeleteUserSecretAsync

```csharp
Task<bool> DeleteUserSecretAsync(
    string userId,
    string secretName);
```

Deletes a user secret from the enclave.

- **Parameters**:
  - `userId`: The ID of the user who owns the secret.
  - `secretName`: The name of the secret.
- **Returns**: `true` if the secret was deleted successfully, `false` otherwise.

#### GetUserSecretNamesAsync

```csharp
Task<string[]> GetUserSecretNamesAsync(
    string userId);
```

Retrieves the names of all secrets owned by a user.

- **Parameters**:
  - `userId`: The ID of the user.
- **Returns**: An array of secret names.

### Event Triggers

#### RegisterTriggerAsync

```csharp
Task<string> RegisterTriggerAsync(
    string eventType,
    string functionId,
    string userId,
    string condition);
```

Registers a trigger for a specific event.

- **Parameters**:
  - `eventType`: The type of event to trigger on (e.g., "blockchain", "schedule").
  - `functionId`: The ID of the function to execute.
  - `userId`: The ID of the user who owns the function.
  - `condition`: The condition under which the trigger should fire (JSON string).
- **Returns**: The ID of the registered trigger, or an empty string if registration failed.

#### UnregisterTriggerAsync

```csharp
Task<bool> UnregisterTriggerAsync(
    string triggerId);
```

Unregisters a trigger.

- **Parameters**:
  - `triggerId`: The ID of the trigger to unregister.
- **Returns**: `true` if the trigger was unregistered successfully, `false` otherwise.

#### GetTriggersForEventAsync

```csharp
Task<string[]> GetTriggersForEventAsync(
    string eventType);
```

Gets all triggers for a specific event type.

- **Parameters**:
  - `eventType`: The type of event.
- **Returns**: An array of trigger IDs.

#### GetTriggerInfoAsync

```csharp
Task<string> GetTriggerInfoAsync(
    string triggerId);
```

Gets information about a specific trigger.

- **Parameters**:
  - `triggerId`: The ID of the trigger.
- **Returns**: A JSON string containing information about the trigger, or an empty string if not found.

#### ProcessBlockchainEventAsync

```csharp
Task<int> ProcessBlockchainEventAsync(
    string eventData);
```

Processes a blockchain event.

- **Parameters**:
  - `eventData`: The event data (JSON string).
- **Returns**: The number of triggers executed.

#### ProcessScheduledTriggersAsync

```csharp
Task<int> ProcessScheduledTriggersAsync(
    ulong currentTime);
```

Processes scheduled triggers.

- **Parameters**:
  - `currentTime`: The current time in seconds since the epoch.
- **Returns**: The number of triggers executed.

### Provably Fair Randomness

#### GenerateRandomNumberAsync

```csharp
Task<ulong> GenerateRandomNumberAsync(
    ulong min,
    ulong max,
    string userId,
    string requestId);
```

Generates a random number between min and max (inclusive).

- **Parameters**:
  - `min`: The minimum value.
  - `max`: The maximum value.
  - `userId`: The ID of the user requesting the random number.
  - `requestId`: The ID of the request.
- **Returns**: The generated random number.

#### VerifyRandomNumberAsync

```csharp
Task<bool> VerifyRandomNumberAsync(
    ulong randomNumber,
    ulong min,
    ulong max,
    string userId,
    string requestId,
    string proof);
```

Verifies a random number.

- **Parameters**:
  - `randomNumber`: The random number to verify.
  - `min`: The minimum value.
  - `max`: The maximum value.
  - `userId`: The ID of the user who requested the random number.
  - `requestId`: The ID of the request.
  - `proof`: The proof of the random number.
- **Returns**: `true` if the random number is valid, `false` otherwise.

#### GetRandomNumberProofAsync

```csharp
Task<string> GetRandomNumberProofAsync(
    ulong randomNumber,
    ulong min,
    ulong max,
    string userId,
    string requestId);
```

Gets the proof for a random number.

- **Parameters**:
  - `randomNumber`: The random number.
  - `min`: The minimum value.
  - `max`: The maximum value.
  - `userId`: The ID of the user who requested the random number.
  - `requestId`: The ID of the request.
- **Returns**: The proof of the random number.

#### GenerateSeedAsync

```csharp
Task<string> GenerateSeedAsync(
    string userId,
    string requestId);
```

Generates a random seed.

- **Parameters**:
  - `userId`: The ID of the user requesting the seed.
  - `requestId`: The ID of the request.
- **Returns**: The generated seed.

### Compliance Verification

#### VerifyComplianceAsync

```csharp
Task<string> VerifyComplianceAsync(
    string code,
    string userId,
    string functionId,
    string complianceRules);
```

Verifies a JavaScript function for compliance.

- **Parameters**:
  - `code`: The JavaScript code to verify.
  - `userId`: The ID of the user who owns the function.
  - `functionId`: The ID of the function.
  - `complianceRules`: The compliance rules to check against (JSON string).
- **Returns**: A JSON string containing the verification result.

#### GetComplianceRulesAsync

```csharp
Task<string> GetComplianceRulesAsync(
    string jurisdiction);
```

Gets the compliance rules for a specific jurisdiction.

- **Parameters**:
  - `jurisdiction`: The jurisdiction code (e.g., "US", "EU", "JP").
- **Returns**: A JSON string containing the compliance rules.

#### SetComplianceRulesAsync

```csharp
Task<bool> SetComplianceRulesAsync(
    string jurisdiction,
    string rules);
```

Sets the compliance rules for a specific jurisdiction.

- **Parameters**:
  - `jurisdiction`: The jurisdiction code (e.g., "US", "EU", "JP").
  - `rules`: The compliance rules (JSON string).
- **Returns**: `true` if the rules were set successfully, `false` otherwise.

#### GetComplianceStatusAsync

```csharp
Task<string> GetComplianceStatusAsync(
    string functionId,
    string jurisdiction);
```

Gets the compliance status for a specific function.

- **Parameters**:
  - `functionId`: The ID of the function.
  - `jurisdiction`: The jurisdiction code (e.g., "US", "EU", "JP").
- **Returns**: A JSON string containing the compliance status.

#### VerifyIdentityAsync

```csharp
Task<string> VerifyIdentityAsync(
    string userId,
    string identityData,
    string jurisdiction);
```

Verifies a user's identity.

- **Parameters**:
  - `userId`: The ID of the user.
  - `identityData`: The identity data (JSON string).
  - `jurisdiction`: The jurisdiction code (e.g., "US", "EU", "JP").
- **Returns**: A JSON string containing the verification result.

#### GetIdentityStatusAsync

```csharp
Task<string> GetIdentityStatusAsync(
    string userId,
    string jurisdiction);
```

Gets the identity verification status for a specific user.

- **Parameters**:
  - `userId`: The ID of the user.
  - `jurisdiction`: The jurisdiction code (e.g., "US", "EU", "JP").
- **Returns**: A JSON string containing the identity verification status.
