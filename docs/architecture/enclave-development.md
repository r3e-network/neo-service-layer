# Enclave Development for Neo Service Layer

## Overview

This document provides a guide for developing enclave code for the Neo Service Layer. It covers the entire process from setting up the development environment to implementing and testing enclave code.

## Prerequisites

Before developing enclave code, ensure you have the following:

- Intel SGX SDK installed
- Occlum LibOS installed
- C++ development environment
- Basic understanding of Trusted Execution Environments (TEEs)

## Setting Up the Development Environment

### Installing Intel SGX SDK

1. Download the Intel SGX SDK from the [Intel SGX SDK website](https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/overview.html).
2. Follow the installation instructions for your operating system.
3. Verify the installation by running the SGX SDK sample applications.

### Installing Occlum LibOS

1. Clone the Occlum repository:

```bash
git clone https://github.com/occlum/occlum.git
```

2. Follow the installation instructions in the Occlum repository.
3. Verify the installation by running the Occlum sample applications.

## Enclave Development Process

### Step 1: Define the Enclave Interface

The first step in developing enclave code is to define the interface between the untrusted host application and the trusted enclave. This is done using the Enclave Definition Language (EDL).

Create an EDL file for your enclave:

```edl
enclave {
    trusted {
        public void ecall_initialize();
        public int ecall_perform_operation([in, string] const char* parameter);
    };

    untrusted {
        void ocall_log([in, string] const char* message);
    };
};
```

This EDL file defines two ECALLs (Enclave Calls) that the host application can make to the enclave:

- `ecall_initialize`: Initializes the enclave.
- `ecall_perform_operation`: Performs an operation with a parameter.

It also defines one OCALL (Outside Call) that the enclave can make to the host application:

- `ocall_log`: Logs a message from the enclave.

### Step 2: Implement the Enclave Code

Next, implement the enclave code in C++:

```cpp
#include "Enclave_t.h"
#include <string>
#include <vector>
#include <sgx_trts.h>

// Global variables
bool g_initialized = false;

// Initialize the enclave
void ecall_initialize()
{
    if (!g_initialized)
    {
        // Perform initialization tasks
        g_initialized = true;
        ocall_log("Enclave initialized.");
    }
}

// Perform an operation
int ecall_perform_operation(const char* parameter)
{
    if (!g_initialized)
    {
        ocall_log("Enclave not initialized.");
        return -1;
    }

    if (parameter == nullptr)
    {
        ocall_log("Parameter is null.");
        return -2;
    }

    // Perform the operation
    std::string param(parameter);
    ocall_log(("Performing operation with parameter: " + param).c_str());

    // Return a result
    return 0;
}
```

### Step 3: Build the Enclave

Build the enclave using the Intel SGX SDK:

```bash
make
```

This will generate the enclave shared object file (`.so`) and the enclave signature file (`.signed.so`).

### Step 4: Integrate with Occlum LibOS

To run the enclave with Occlum LibOS, you need to create an Occlum instance and copy the enclave files into it:

```bash
# Create an Occlum instance
occlum new occlum_instance
cd occlum_instance

# Copy the enclave files
cp ../enclave.signed.so image/lib/

# Copy any dependencies
cp /usr/lib/x86_64-linux-gnu/libsgx*.so image/lib/

# Build the Occlum instance
occlum build
```

### Step 5: Run the Enclave

Run the enclave with Occlum LibOS:

```bash
occlum run /bin/enclave_host
```

## Integrating with Neo Service Layer

### Step 1: Create a Host Application

Create a host application that communicates with the enclave:

```csharp
using NeoServiceLayer.Tee.Host.Services;
using System.Runtime.InteropServices;

namespace NeoServiceLayer.Services.YourService;

public class EnclaveHost : IEnclaveManager
{
    private IntPtr _enclaveId;
    private bool _initialized;

    public async Task<bool> InitializeAsync()
    {
        if (_initialized)
        {
            return true;
        }

        try
        {
            // Initialize the enclave
            var result = NativeMethods.sgx_create_enclave("enclave.signed.so", 0, out _, out _, out _enclaveId, out _);
            if (result != 0)
            {
                return false;
            }

            // Call the enclave initialization function
            result = NativeMethods.ecall_initialize(_enclaveId);
            if (result != 0)
            {
                return false;
            }

            _initialized = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> ExecuteJavaScriptAsync(string script)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Enclave not initialized.");
        }

        try
        {
            // Call the enclave function
            var result = NativeMethods.ecall_perform_operation(_enclaveId, script);
            if (result != 0)
            {
                throw new InvalidOperationException($"Enclave operation failed with error code {result}.");
            }

            // Get the result from the enclave
            // This is a simplified example
            return "result";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Enclave operation failed.", ex);
        }
    }

    public void Dispose()
    {
        if (_enclaveId != IntPtr.Zero)
        {
            NativeMethods.sgx_destroy_enclave(_enclaveId);
            _enclaveId = IntPtr.Zero;
        }
    }

    private static class NativeMethods
    {
        [DllImport("sgx_urts")]
        public static extern int sgx_create_enclave(
            [MarshalAs(UnmanagedType.LPStr)] string filename,
            int debug,
            out IntPtr token,
            out int updated,
            out IntPtr enclave_id,
            out int misc_attr);

        [DllImport("sgx_urts")]
        public static extern int sgx_destroy_enclave(IntPtr enclave_id);

        [DllImport("enclave_bridge")]
        public static extern int ecall_initialize(IntPtr enclave_id);

        [DllImport("enclave_bridge")]
        public static extern int ecall_perform_operation(
            IntPtr enclave_id,
            [MarshalAs(UnmanagedType.LPStr)] string parameter);
    }
}
```

### Step 2: Register the Enclave Manager

Register the enclave manager with the dependency injection system:

```csharp
services.AddSingleton<IEnclaveManager, EnclaveHost>();
```

### Step 3: Use the Enclave Manager in Your Service

Use the enclave manager in your service:

```csharp
public class YourService : EnclaveBlockchainServiceBase, IYourService
{
    private readonly IEnclaveManager _enclaveManager;

    public YourService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<YourService> logger)
        : base("YourService", "Your Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _enclaveManager = enclaveManager;
    }

    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        return await _enclaveManager.InitializeAsync();
    }

    public async Task<string> PerformOperationAsync(string parameter, BlockchainType blockchainType)
    {
        return await _enclaveManager.ExecuteJavaScriptAsync($"performOperation('{parameter}', '{blockchainType}')");
    }
}
```

## Testing Enclave Code

### Unit Testing

Unit testing enclave code can be challenging due to the secure nature of enclaves. However, you can use the SGX SDK's simulation mode for testing:

```bash
# Build the enclave in simulation mode
make SGX_MODE=SIM
```

This allows you to run the enclave in a simulated environment without requiring SGX hardware.

### Integration Testing

For integration testing, you can use the Occlum LibOS's simulation mode:

```bash
# Build the Occlum instance in simulation mode
occlum build --sgx-mode sim
```

This allows you to run the enclave with Occlum LibOS in a simulated environment.

## Best Practices

### Security

- Minimize the enclave's attack surface by keeping the enclave code small and focused.
- Validate all inputs to the enclave to prevent buffer overflows and other security vulnerabilities.
- Use secure coding practices to prevent side-channel attacks.
- Avoid storing sensitive data in untrusted memory.

### Performance

- Minimize the number of ECALLs and OCALLs, as they involve expensive context switches.
- Use batching to reduce the number of enclave transitions.
- Optimize enclave code for performance, as enclave execution is slower than normal execution.

### Reliability

- Handle errors gracefully to prevent enclave crashes.
- Implement proper cleanup to prevent resource leaks.
- Use defensive programming techniques to ensure enclave stability.

## Conclusion

Developing enclave code for the Neo Service Layer requires a good understanding of Trusted Execution Environments (TEEs) and secure coding practices. By following the guidelines in this document, you can develop secure, reliable, and performant enclave code for your services.

## References

- [Intel SGX Documentation](https://www.intel.com/content/www/us/en/developer/tools/software-guard-extensions/overview.html)
- [Occlum LibOS Documentation](https://occlum.io/)
- [Neo Service Layer Architecture](README.md)
