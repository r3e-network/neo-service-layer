# Neo Service Layer Troubleshooting Guide

This guide provides solutions for common issues you might encounter when using the Neo Service Layer (NSL).

## Table of Contents

- [Installation Issues](#installation-issues)
- [Enclave Initialization Issues](#enclave-initialization-issues)
- [JavaScript Execution Issues](#javascript-execution-issues)
- [Storage Issues](#storage-issues)
- [Event Trigger Issues](#event-trigger-issues)
- [Randomness Service Issues](#randomness-service-issues)
- [Compliance Service Issues](#compliance-service-issues)
- [Neo N3 Integration Issues](#neo-n3-integration-issues)
- [Performance Issues](#performance-issues)
- [Debugging Tips](#debugging-tips)

## Installation Issues

### OpenEnclave SDK Not Found

**Symptom**: Error message "OpenEnclave SDK not found" when initializing the enclave.

**Solution**:
1. Ensure that the OpenEnclave SDK is installed on your system.
2. Set the `OPENENCLAVE_PATH` environment variable to the installation path:
   ```powershell
   $env:OPENENCLAVE_PATH = "C:\Program Files\Open Enclave SDK"
   ```
3. Restart your application.

### Missing Dependencies

**Symptom**: Error message about missing DLLs or dependencies.

**Solution**:
1. Install the required dependencies:
   ```powershell
   # For Windows
   choco install openenclave-sdk

   # For Ubuntu
   sudo apt update
   sudo apt install -y openenclave
   ```
2. Ensure that the dependencies are in your system PATH.

## Enclave Initialization Issues

### Enclave Initialization Failed

**Symptom**: Error message "Failed to initialize enclave" or "Enclave initialization failed".

**Solution**:
1. Check if you're running in simulation mode:
   ```csharp
   var enclaveHost = new TeeEnclaveHost(loggerFactory, simulationMode: true);
   ```
2. If you're not using simulation mode, ensure that your system supports Intel SGX or another TEE technology.
3. Check the logs for more specific error messages.

### Enclave Loading Failed

**Symptom**: Error message "Failed to load enclave" or "Enclave loading failed".

**Solution**:
1. Ensure that the enclave binary is in the correct location.
2. Check if the enclave binary is signed correctly.
3. Try rebuilding the enclave using the provided build script:
   ```powershell
   .\build_enclave.ps1
   ```

## JavaScript Execution Issues

### JavaScript Execution Failed

**Symptom**: Error message "JavaScript execution failed" or "Failed to execute JavaScript".

**Solution**:
1. Check your JavaScript code for syntax errors.
2. Ensure that your JavaScript code has a `main` function that takes an `input` parameter.
3. Check if the input data is valid JSON.
4. Try executing a simple JavaScript function to test the enclave:
   ```csharp
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
   ```

### Gas Limit Exceeded

**Symptom**: Error message "Gas limit exceeded" or "Out of gas".

**Solution**:
1. Optimize your JavaScript code to use less gas.
2. Increase the gas limit for the function:
   ```csharp
   // Reset gas used
   await enclaveInterface.ResetGasUsedAsync();

   // Set a higher gas limit
   await enclaveInterface.SetGasLimitAsync(2000000);
   ```

## Storage Issues

### Storage Initialization Failed

**Symptom**: Error message "Failed to initialize storage manager" or "Storage initialization failed".

**Solution**:
1. Ensure that the storage path is valid and accessible.
2. Check if the storage path has the correct permissions.
3. Try using a different storage path:
   ```csharp
   // In your enclave code
   if (!storage_manager_->initialize("new_storage_path")) {
       secure_log("Failed to initialize storage manager");
       return false;
   }
   ```

### Data Not Found

**Symptom**: Error message "Data not found" or "Key not found".

**Solution**:
1. Ensure that the key is correct.
2. Check if the data was stored successfully.
3. Try storing and retrieving a simple value to test the storage:
   ```csharp
   string key = "test-key";
   byte[] data = Encoding.UTF8.GetBytes("test-data");

   bool storeResult = await enclaveInterface.StorePersistentDataAsync(key, data);
   byte[] retrievedData = await enclaveInterface.RetrievePersistentDataAsync(key);
   ```

## Event Trigger Issues

### Trigger Registration Failed

**Symptom**: Error message "Failed to register trigger" or empty trigger ID returned.

**Solution**:
1. Ensure that the event type is valid (e.g., "blockchain", "schedule").
2. Check if the function ID exists.
3. Ensure that the condition is valid JSON.
4. Try registering a simple trigger:
   ```csharp
   string eventType = "blockchain";
   string functionId = "test-function";
   string userId = "test-user";
   string condition = @"{""event_type"": ""test""}";

   string triggerId = await enclaveInterface.RegisterTriggerAsync(
       eventType, functionId, userId, condition);
   ```

### Trigger Not Firing

**Symptom**: Event triggers are not being executed when expected.

**Solution**:
1. Ensure that the event data matches the trigger condition.
2. Check if the trigger is registered correctly.
3. Try processing a simple event:
   ```csharp
   string eventData = @"{""type"": ""test""}";
   int processedCount = await enclaveInterface.ProcessBlockchainEventAsync(eventData);
   ```

## Randomness Service Issues

### Random Number Generation Failed

**Symptom**: Error message "Failed to generate random number" or exception thrown.

**Solution**:
1. Ensure that the randomness service is initialized correctly.
2. Try generating a simple random number:
   ```csharp
   ulong min = 1;
   ulong max = 100;
   string userId = "test-user";
   string requestId = Guid.NewGuid().ToString();

   ulong randomNumber = await enclaveInterface.GenerateRandomNumberAsync(
       min, max, userId, requestId);
   ```

### Random Number Verification Failed

**Symptom**: Random number verification returns `false`.

**Solution**:
1. Ensure that the random number, user ID, and request ID match the ones used to generate the random number.
2. Check if the proof is correct.
3. Try verifying a newly generated random number:
   ```csharp
   ulong randomNumber = await enclaveInterface.GenerateRandomNumberAsync(
       min, max, userId, requestId);

   string proof = await enclaveInterface.GetRandomNumberProofAsync(
       randomNumber, min, max, userId, requestId);

   bool isValid = await enclaveInterface.VerifyRandomNumberAsync(
       randomNumber, min, max, userId, requestId, proof);
   ```

## Compliance Service Issues

### Compliance Verification Failed

**Symptom**: Error message "Failed to verify compliance" or empty result returned.

**Solution**:
1. Ensure that the compliance service is initialized correctly.
2. Check if the compliance rules are valid JSON.
3. Try verifying a simple JavaScript function:
   ```csharp
   string code = @"
       function main(input) {
           return { result: 'Hello, ' + input.name };
       }
   ";
   string userId = "test-user";
   string functionId = "test-function";
   string complianceRules = @"{
       ""jurisdiction"": ""global"",
       ""prohibited_apis"": [""eval"", ""Function""],
       ""prohibited_data"": [""password""],
       ""allow_network_access"": false,
       ""max_gas"": 1000000
   }";

   string result = await enclaveInterface.VerifyComplianceAsync(
       code, userId, functionId, complianceRules);
   ```

### Identity Verification Failed

**Symptom**: Error message "Failed to verify identity" or empty result returned.

**Solution**:
1. Ensure that the compliance service is initialized correctly.
2. Check if the identity data is valid JSON.
3. Ensure that the identity data contains all required fields for the jurisdiction.
4. Try verifying a simple identity:
   ```csharp
   string userId = "test-user";
   string identityData = @"{
       ""name"": ""John Doe"",
       ""email"": ""john.doe@example.com"",
       ""address"": ""123 Main St, Anytown, USA"",
       ""phone"": ""+1-555-123-4567""
   }";
   string jurisdiction = "US";

   string result = await enclaveInterface.VerifyIdentityAsync(
       userId, identityData, jurisdiction);
   ```

## Performance Issues

### Slow JavaScript Execution

**Symptom**: JavaScript execution is taking longer than expected.

**Solution**:
1. Optimize your JavaScript code to use less resources.
2. Break large inputs into smaller chunks.
3. Use batch processing for large datasets.
4. Consider using a more efficient algorithm.

### High Memory Usage

**Symptom**: The application is using more memory than expected.

**Solution**:
1. Optimize your JavaScript code to use less memory.
2. Release resources when they're no longer needed.
3. Use streaming for large datasets.
4. Consider using a more memory-efficient algorithm.

## Debugging Tips

### Enable Verbose Logging

To get more detailed information about what's happening in the enclave, enable verbose logging:

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
```

### Use Simulation Mode

During development and testing, use simulation mode to run the enclave without requiring SGX hardware:

```csharp
var enclaveHost = new TeeEnclaveHost(loggerFactory, simulationMode: true);
```

### Check Enclave Logs

The enclave logs can provide valuable information about what's happening inside the enclave:

```csharp
// In your enclave code
secure_log("Debug message");
```

### Test with Simple Examples

If you're having issues with a complex function, try testing with a simple example first to isolate the problem:

```csharp
string jsCode = @"
    function main(input) {
        return { result: 'Hello, ' + input.name };
    }
";
string input = @"{ ""name"": ""World"" }";
```

### Check for Common JavaScript Errors

Common JavaScript errors include:
- Syntax errors
- Undefined variables
- Type errors
- Missing or invalid JSON

### Verify Enclave Integrity

If you suspect that the enclave has been tampered with, verify its integrity:

```csharp
byte[] mrEnclave = await enclaveInterface.GetMrEnclaveAsync();
byte[] mrSigner = await enclaveInterface.GetMrSignerAsync();

// Compare with expected values
```

## Still Need Help?

If you're still experiencing issues, please:

1. Check the [Neo Service Layer Documentation](index.md) for more information.
2. Look for similar issues in the [GitHub Issues](https://github.com/neo-project/neo-service-layer/issues).
3. Create a new issue with a detailed description of the problem, including steps to reproduce, expected behavior, and actual behavior.
