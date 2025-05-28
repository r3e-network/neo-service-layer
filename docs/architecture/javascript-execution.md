# Neo Service Layer - JavaScript Execution Environment

## Overview

The JavaScript Execution Environment is a core component of the Neo Service Layer that enables secure execution of JavaScript code within Trusted Execution Environments (TEEs). It leverages Occlum LibOS and the V8 JavaScript engine to provide a secure, isolated environment for executing JavaScript functions with access to user secrets and blockchain integration.

## Architecture

The JavaScript Execution Environment consists of the following components:

### V8 JavaScript Engine

The V8 JavaScript engine is embedded within the Occlum LibOS enclave to provide a high-performance JavaScript execution environment. It is configured with appropriate security settings to prevent malicious code execution.

### JavaScript Runtime

The JavaScript Runtime provides the execution context for JavaScript functions, including:

- **Global Objects**: Predefined global objects that provide access to enclave functionality.
- **Security Sandbox**: A security sandbox that restricts access to sensitive resources.
- **Memory Management**: Memory management to prevent memory leaks and ensure efficient resource usage.

### JavaScript API

The JavaScript API provides a set of functions and objects that JavaScript code can use to interact with the enclave and the blockchain:

- **Context Object**: Provides information about the execution context.
- **Blockchain Object**: Provides access to blockchain operations.
- **Secrets Object**: Provides access to user secrets.
- **Utility Functions**: Provides utility functions for common tasks.

### JavaScript Function Management

The JavaScript Function Management component handles the registration, storage, and execution of JavaScript functions:

- **Function Registration**: Registers JavaScript functions for later execution.
- **Function Storage**: Securely stores registered functions.
- **Function Execution**: Executes registered functions with provided parameters.

## Implementation Details

### V8 Integration

The V8 JavaScript engine is integrated into the Occlum LibOS enclave using the V8 C++ API. The integration includes:

1. **Initialization**: Initialize the V8 engine with appropriate settings.
2. **Context Creation**: Create a V8 context for JavaScript execution.
3. **Global Object Setup**: Set up global objects and functions.
4. **Script Compilation**: Compile JavaScript code into V8 scripts.
5. **Script Execution**: Execute compiled scripts within the V8 context.
6. **Result Extraction**: Extract execution results from the V8 context.
7. **Error Handling**: Handle JavaScript execution errors.
8. **Cleanup**: Clean up V8 resources after execution.

### JavaScript Runtime Environment

The JavaScript Runtime Environment provides the execution context for JavaScript functions. It includes:

1. **Global Objects**: Predefined global objects that provide access to enclave functionality.
2. **Security Sandbox**: A security sandbox that restricts access to sensitive resources.
3. **Memory Management**: Memory management to prevent memory leaks and ensure efficient resource usage.
4. **Error Handling**: Error handling to catch and report JavaScript errors.
5. **Timeout Handling**: Timeout handling to prevent infinite loops or long-running scripts.

### JavaScript API

The JavaScript API provides a set of functions and objects that JavaScript code can use to interact with the enclave and the blockchain:

#### Context Object

The `context` object provides information about the execution context:

```javascript
const context = {
    blockchainType: "neo-n3", // The blockchain type
    functionId: "my-function", // The ID of the function being executed
    caller: "0x1234567890abcdef", // The address of the caller
    timestamp: 1609459200 // The timestamp of the execution
};
```

#### Blockchain Object

The `blockchain` object provides access to blockchain operations:

```javascript
const blockchain = {
    // Call a smart contract method
    callContract: function(contractAddress, method, params) {
        // Implementation
    },
    
    // Get the balance of an address
    getBalance: function(address) {
        // Implementation
    },
    
    // Get storage from a smart contract
    getStorage: function(contractAddress, key) {
        // Implementation
    }
};
```

#### Secrets Object

The `secrets` object provides access to user secrets:

```javascript
const secrets = {
    // Get a secret by ID
    get: function(secretId) {
        // Implementation
    }
};
```

#### Utility Functions

Utility functions provide common functionality:

```javascript
// Log a message
function log(message) {
    // Implementation
}

// Assert that a condition is true
function assert(condition, message) {
    // Implementation
}

// Require that a condition is true, otherwise throw an error
function require(condition, message) {
    // Implementation
}

// Fetch data from an external URL (if allowed)
async function fetch(url, options) {
    // Implementation
}
```

### JavaScript Function Management

The JavaScript Function Management component handles the registration, storage, and execution of JavaScript functions:

#### Function Registration

Functions are registered with a unique ID and stored securely within the enclave:

```cpp
bool RegisterFunction(const std::string& functionId, const std::string& functionCode) {
    // Validate function code
    if (!ValidateFunctionCode(functionCode)) {
        return false;
    }
    
    // Store function code
    functions_[functionId] = functionCode;
    
    // Compile function (optional, can also be done at execution time)
    if (!CompileFunction(functionId)) {
        functions_.erase(functionId);
        return false;
    }
    
    return true;
}
```

#### Function Execution

Registered functions are executed with provided parameters:

```cpp
std::string ExecuteFunction(const std::string& functionId, const std::string& parameters) {
    // Check if function exists
    auto it = functions_.find(functionId);
    if (it == functions_.end()) {
        return "Error: Function not found";
    }
    
    // Create V8 context
    v8::Local<v8::Context> context = CreateContext();
    v8::Context::Scope context_scope(context);
    
    try {
        // Set up execution environment
        SetupExecutionEnvironment(context, functionId, parameters);
        
        // Compile function if not already compiled
        if (!compiled_functions_.count(functionId)) {
            if (!CompileFunction(functionId)) {
                return "Error: Failed to compile function";
            }
        }
        
        // Execute function
        v8::Local<v8::Function> function = compiled_functions_[functionId].Get(isolate_);
        v8::Local<v8::Value> result = ExecuteCompiledFunction(function, parameters);
        
        // Convert result to string
        return ConvertToString(result);
    } catch (const std::exception& e) {
        return std::string("Error: ") + e.what();
    }
}
```

## Security Considerations

### Code Validation

JavaScript code is validated before execution to prevent malicious code:

1. **Static Analysis**: Static analysis is performed to detect potentially malicious code patterns.
2. **Resource Limits**: Resource limits are enforced to prevent denial-of-service attacks.
3. **API Restrictions**: Access to sensitive APIs is restricted based on the function's permissions.

### Sandbox Isolation

The JavaScript execution environment is isolated from the rest of the system:

1. **Memory Isolation**: The V8 engine's memory is isolated from the rest of the enclave.
2. **API Restrictions**: Access to system resources is restricted.
3. **Exception Handling**: Exceptions are caught and handled to prevent crashes.

### Resource Management

Resources are managed to prevent resource exhaustion:

1. **Memory Limits**: Memory usage is limited to prevent memory exhaustion.
2. **CPU Limits**: CPU usage is limited to prevent CPU exhaustion.
3. **Execution Time Limits**: Execution time is limited to prevent infinite loops.

## Example JavaScript Function

Here's an example of a JavaScript function that can be executed within the enclave:

```javascript
function processPayment(params) {
  // Parse parameters
  const { amount, recipient, paymentId } = JSON.parse(params);
  
  // Get payment secret (e.g., API key for payment processor)
  const paymentApiKey = secrets.get('payment-api-key');
  
  // Log the payment attempt
  log(`Processing payment ${paymentId} of ${amount} to ${recipient}`);
  
  // Call payment processor API (if external calls are allowed)
  // const response = await fetch('https://payment-processor.example.com/api/pay', {
  //   method: 'POST',
  //   headers: {
  //     'Content-Type': 'application/json',
  //     'Authorization': `Bearer ${paymentApiKey}`
  //   },
  //   body: JSON.stringify({ amount, recipient, paymentId })
  // });
  
  // For this example, simulate a successful payment
  const paymentSuccessful = true;
  
  // If payment is successful, call blockchain contract to record it
  if (paymentSuccessful) {
    const result = blockchain.callContract(
      '0x1234567890abcdef1234567890abcdef12345678',
      'recordPayment',
      [paymentId, amount, recipient, context.timestamp]
    );
    
    return JSON.stringify({
      success: true,
      paymentId,
      transactionHash: result.transactionHash
    });
  } else {
    return JSON.stringify({
      success: false,
      paymentId,
      error: 'Payment processing failed'
    });
  }
}
```

## Usage

To use the JavaScript Execution Environment, follow these steps:

1. **Register a Function**: Register a JavaScript function with a unique ID.
2. **Store User Secrets**: Store any secrets that the function needs to access.
3. **Execute the Function**: Execute the function with the provided parameters.
4. **Process the Result**: Process the result of the function execution.

Example:

```csharp
// Register a function
await computeService.RegisterFunctionAsync("process-payment", functionCode, BlockchainType.NeoN3);

// Store a secret
await computeService.StoreSecretAsync("payment-api-key", "secret-api-key", "owner-address", BlockchainType.NeoN3);

// Execute the function
string parameters = JsonSerializer.Serialize(new
{
    amount = 100,
    recipient = "0xabcdef1234567890abcdef1234567890abcdef12",
    paymentId = "payment-123"
});
string result = await computeService.ExecuteFunctionAsync("process-payment", parameters, BlockchainType.NeoN3);

// Process the result
var paymentResult = JsonSerializer.Deserialize<PaymentResult>(result);
if (paymentResult.Success)
{
    Console.WriteLine($"Payment {paymentResult.PaymentId} successful with transaction hash {paymentResult.TransactionHash}");
}
else
{
    Console.WriteLine($"Payment {paymentResult.PaymentId} failed: {paymentResult.Error}");
}
```

## Conclusion

The JavaScript Execution Environment is a powerful component of the Neo Service Layer that enables secure execution of JavaScript code within Trusted Execution Environments (TEEs). It provides a secure, isolated environment for executing JavaScript functions with access to user secrets and blockchain integration, enabling a wide range of use cases for blockchain applications.
