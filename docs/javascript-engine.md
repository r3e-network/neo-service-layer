# Neo Service Layer JavaScript Engine

## Overview

The JavaScript Engine is a core component of the Neo Service Layer (NSL) enclave. It provides a secure environment for executing JavaScript code within the enclave, with access to user secrets, persistent storage, and other secure services.

## Architecture

The JavaScript Engine is designed with a modular architecture that allows for different JavaScript engine implementations to be used. The current implementation uses QuickJS as the underlying JavaScript engine.

```
┌─────────────────────────────────────────────────────────────────┐
│                     JavaScript Engine                            │
│                                                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────┐ │
│  │ JavaScript  │  │ JavaScript  │  │ JavaScript  │  │   Gas    │ │
│  │  Interface  │  │   Manager   │  │  Executor   │  │ Accounting│ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └──────────┘ │
│          │               │                │               │      │
│          └───────────────┼────────────────┼───────────────┘      │
│                          │                │                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │   QuickJS   │  │   Error     │  │   Code      │              │
│  │   Adapter   │  │  Handling   │  │  Validation │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Core Components

1. **JavaScript Interface**: Provides the interface for executing JavaScript code.
2. **JavaScript Manager**: Manages JavaScript execution contexts and resources.
3. **JavaScript Executor**: Executes JavaScript code within the enclave.
4. **Gas Accounting**: Tracks resource usage during JavaScript execution.
5. **QuickJS Adapter**: Adapts the QuickJS engine to the JavaScript Engine interface.
6. **Error Handling**: Provides detailed error information for JavaScript execution errors.
7. **Code Validation**: Validates JavaScript code before execution.

## JavaScript Execution

### Execution Flow

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│   Host   │     │ JavaScript│     │ JavaScript│    │ QuickJS  │
│Application│────▶│ Interface│────▶│  Manager  │───▶│ Adapter  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
                                        │                │
                                        ▼                │
                                  ┌──────────┐           │
                                  │   Gas    │           │
                                  │Accounting│◀──────────┘
                                  └──────────┘
                                        │
                                        ▼
                                  ┌──────────┐
                                  │  Result  │
                                  │          │
                                  └──────────┘
```

1. The host application calls the JavaScript interface to execute JavaScript code.
2. The JavaScript interface validates the request and forwards it to the JavaScript manager.
3. The JavaScript manager creates an execution context and forwards the request to the QuickJS adapter.
4. The QuickJS adapter executes the code, tracking gas usage.
5. The QuickJS adapter returns the result to the JavaScript manager.
6. The JavaScript manager returns the result to the JavaScript interface.
7. The JavaScript interface returns the result to the host application.

### Execution Context

The JavaScript execution context includes:

1. **Input**: The input data for the JavaScript code.
2. **Secrets**: User secrets accessible to the JavaScript code.
3. **Function ID**: The ID of the function being executed.
4. **User ID**: The ID of the user executing the function.
5. **Gas Limit**: The maximum amount of gas that can be used during execution.

### Gas Accounting

Gas accounting is used to track resource usage during JavaScript execution. This prevents infinite loops and other resource exhaustion attacks. Gas is consumed for:

1. **CPU Usage**: Instructions executed.
2. **Memory Usage**: Memory allocated.
3. **Storage Usage**: Storage operations.
4. **Network Usage**: Network operations.

## Error Handling

The JavaScript Engine provides detailed error information for JavaScript execution errors. This includes:

1. **Error Message**: A human-readable error message.
2. **Error Type**: The type of error (e.g., SyntaxError, TypeError).
3. **Line Number**: The line number where the error occurred.
4. **Column Number**: The column number where the error occurred.
5. **Stack Trace**: A stack trace showing the call stack at the time of the error.

### Error Types

The JavaScript Engine handles the following types of errors:

1. **Syntax Errors**: Errors in the JavaScript syntax.
2. **Runtime Errors**: Errors that occur during JavaScript execution.
3. **Resource Errors**: Errors due to resource exhaustion (e.g., out of gas).
4. **System Errors**: Errors in the JavaScript Engine itself.

### Error Reporting

Errors are reported in a structured JSON format:

```json
{
  "error": "TypeError: Cannot read property 'foo' of null",
  "type": "TypeError",
  "lineNumber": 42,
  "columnNumber": 10,
  "stack": "TypeError: Cannot read property 'foo' of null\n    at main (input:42:10)\n    at execute (input:1:1)"
}
```

## Code Validation

The JavaScript Engine validates JavaScript code before execution. This includes:

1. **Syntax Validation**: Checking for syntax errors.
2. **Code Size Validation**: Checking that the code size is within limits.
3. **Resource Usage Estimation**: Estimating the resource usage of the code.
4. **Code Hash Validation**: Validating the code hash against a known good hash.

## Security Considerations

### JavaScript Isolation

The JavaScript Engine isolates JavaScript execution from the rest of the enclave. This prevents JavaScript code from accessing enclave memory or other resources directly.

### Input Validation

All inputs to the JavaScript Engine are validated before processing. This prevents injection attacks and other security issues.

### Resource Limits

The JavaScript Engine enforces resource limits on JavaScript execution. This prevents resource exhaustion attacks.

### Memory Clearing

The JavaScript Engine clears sensitive data from memory after use. This prevents sensitive data from being leaked.

## Performance Considerations

### JavaScript Engine Selection

The JavaScript Engine uses QuickJS as the underlying JavaScript engine. QuickJS was chosen for its small footprint, good performance, and compatibility with the SGX environment.

### Caching

The JavaScript Engine caches compiled JavaScript code to improve performance for repeated executions.

### Memory Management

The JavaScript Engine carefully manages memory to minimize memory usage and prevent memory leaks.

## API Reference

### JavaScript Interface

```cpp
class IJavaScriptEngine {
public:
    virtual bool initialize() = 0;
    
    virtual std::string execute(
        const std::string& code,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& function_id,
        const std::string& user_id,
        uint64_t& gas_used) = 0;
    
    virtual bool verify_code_hash(
        const std::string& code,
        const std::string& hash) = 0;
    
    virtual std::string calculate_code_hash(
        const std::string& code) = 0;
    
    virtual void reset_gas_used() = 0;
    
    virtual uint64_t get_gas_used() const = 0;
};
```

### JavaScript Manager

```cpp
class JavaScriptManager {
public:
    JavaScriptManager(
        GasAccounting* gas_accounting,
        SecretManager* secret_manager,
        StorageManager* storage_manager);
    
    ~JavaScriptManager();
    
    bool initialize();
    
    std::string execute_javascript(
        const std::string& code,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& function_id,
        const std::string& user_id,
        uint64_t& gas_used);
    
    bool verify_code_hash(
        const std::string& code,
        const std::string& hash);
    
    std::string calculate_code_hash(
        const std::string& code);
};
```

## Conclusion

The JavaScript Engine is a core component of the Neo Service Layer enclave. It provides a secure environment for executing JavaScript code within the enclave, with access to user secrets, persistent storage, and other secure services. It includes comprehensive error handling, code validation, and security features to ensure the safe execution of JavaScript code.
