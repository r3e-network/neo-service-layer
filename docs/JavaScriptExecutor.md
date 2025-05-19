# JavaScript Executor

The JavaScript Executor is a component of the Neo Service Layer that allows executing JavaScript code in the Trusted Execution Environment (TEE). It provides a secure environment for running JavaScript code with access to user secrets, storage, and other features.

## Architecture

The JavaScript Executor consists of the following components:

1. **QuickJS Engine**: A lightweight JavaScript engine that runs in the enclave.
2. **JavaScript API**: A set of APIs that provide access to enclave features from JavaScript.
3. **JavaScript Service**: A .NET service that provides a high-level interface for executing JavaScript code.
4. **TEE Service**: A .NET service that communicates with the enclave to execute JavaScript code.

## JavaScript API

The JavaScript API provides the following features:

### Console API

```javascript
console.log("Hello, World");
console.info("Information message");
console.warn("Warning message");
console.error("Error message");
```

### Storage API

```javascript
// Store a value
storage.set("key", "value");

// Get a value
const value = storage.get("key");

// Remove a value
storage.remove("key");

// Clear all values
storage.clear();
```

### Crypto API

```javascript
// Generate random bytes
const randomBytes = crypto.randomBytes(32);

// Calculate SHA-256 hash
const hash = crypto.sha256("data");

// Sign data
const signature = crypto.sign("data", "key");

// Verify signature
const isValid = crypto.verify("data", "signature", "key");
```

### Gas API

```javascript
// Get available gas
const availableGas = gas.get();

// Use gas
const success = gas.use(100);
```

### Secrets API

```javascript
// Store a secret
SECRETS.set("key", "value");

// Get a secret
const value = SECRETS.get("key");

// Remove a secret
SECRETS.remove("key");
```

### Blockchain API

```javascript
// Send a callback to the blockchain
blockchain.callback("method", "result");
```

## Usage

### Initializing the JavaScript Executor

```csharp
// Get the JavaScript service from DI
var jsService = serviceProvider.GetRequiredService<IJavaScriptService>();

// Initialize the JavaScript executor
await jsService.InitializeJavaScriptExecutorAsync();
```

### Executing JavaScript Code

```csharp
// Execute JavaScript code
string result = await jsService.ExecuteJavaScriptCodeAsync(
    "console.log('Hello, World'); return 42;",
    "example.js");

// result = "42"
```

### Executing a JavaScript Function

```csharp
// Define a function
await jsService.ExecuteJavaScriptCodeAsync(
    "function add(a, b) { return parseInt(a) + parseInt(b); }",
    "define.js");

// Execute the function
string result = await jsService.ExecuteJavaScriptFunctionAsync(
    "add",
    new List<string> { "5", "7" });

// result = "12"
```

### Collecting Garbage

```csharp
// Collect JavaScript garbage
await jsService.CollectJavaScriptGarbageAsync();
```

### Shutting Down the JavaScript Executor

```csharp
// Shutdown the JavaScript executor
await jsService.ShutdownJavaScriptExecutorAsync();
```

## Security Considerations

The JavaScript Executor runs in the Trusted Execution Environment (TEE), which provides the following security guarantees:

1. **Code Integrity**: The JavaScript code is executed in an isolated environment, protected from the host operating system.
2. **Data Confidentiality**: User secrets and other sensitive data are encrypted and can only be accessed within the enclave.
3. **Memory Protection**: The memory used by the JavaScript engine is protected from unauthorized access.
4. **Gas Accounting**: The JavaScript code is subject to gas limits to prevent denial-of-service attacks.

## Performance Considerations

The JavaScript Executor is designed to be lightweight and efficient, but there are some performance considerations to keep in mind:

1. **Memory Usage**: The JavaScript engine has a memory limit of 16 MB.
2. **Stack Size**: The JavaScript engine has a stack size limit of 1 MB.
3. **Gas Usage**: Each operation in the JavaScript code consumes gas, which is limited.
4. **Execution Time**: The JavaScript code is executed synchronously, so long-running operations can block the enclave.

## Error Handling

The JavaScript Executor provides detailed error messages for JavaScript errors:

```csharp
try
{
    string result = await jsService.ExecuteJavaScriptCodeAsync(
        "throw new Error('Something went wrong');",
        "error.js");
}
catch (Exception ex)
{
    // ex.Message = "JavaScript error: Error: Something went wrong"
}
```

## Limitations

The JavaScript Executor has the following limitations:

1. **No DOM**: The JavaScript engine does not provide a DOM or browser APIs.
2. **Limited Standard Library**: The JavaScript engine provides a limited standard library.
3. **No Network Access**: The JavaScript code cannot access the network directly.
4. **No File System Access**: The JavaScript code cannot access the file system directly.
5. **No Process Creation**: The JavaScript code cannot create processes or execute system commands.

## Future Enhancements

The following enhancements are planned for the JavaScript Executor:

1. **Module Support**: Support for loading JavaScript modules.
2. **TypeScript Support**: Support for executing TypeScript code.
3. **WebAssembly Support**: Support for executing WebAssembly code.
4. **Async/Await Support**: Better support for asynchronous operations.
5. **Debugging Support**: Support for debugging JavaScript code.
