# Occlum LibOS Integration

This document describes the integration of Occlum LibOS into the Neo Service Layer.

## What is Occlum?

Occlum is a memory-safe, multi-process library operating system (LibOS) for Intel SGX. As a LibOS, it enables legacy applications to run on SGX with minimal or even no modifications of source code, thus protecting the confidentiality and integrity of user workloads transparently.

## Why Occlum?

Occlum provides several advantages over other SGX solutions:

1. **Memory Safety**: Occlum is written in Rust, a memory-safe programming language, which helps prevent memory-related vulnerabilities.
2. **Multi-Process Support**: Occlum supports running multiple processes inside a single SGX enclave, which enables legacy applications to run without modification.
3. **File System Support**: Occlum provides a fully-featured file system that is compatible with POSIX, which makes it easy to port existing applications.
4. **Network Support**: Occlum supports networking inside the enclave, which enables applications to communicate with the outside world securely.
5. **Dynamic Loading**: Occlum supports dynamic loading of libraries, which enables applications to load libraries at runtime.

## Architecture

The Neo Service Layer integrates with Occlum through the following components:

1. **OcclumManager**: Manages Occlum instances and provides methods for executing commands and JavaScript code in Occlum.
2. **OcclumInterface**: Implements the ITeeInterface interface and provides methods for interacting with Occlum enclaves.
3. **OcclumJavaScriptExecution**: Executes JavaScript code in Occlum enclaves.
4. **OcclumAttestationProvider**: Provides attestation services for Occlum enclaves.

## Building and Running

### Prerequisites

- Intel SGX SDK
- Occlum LibOS
- Node.js
- .NET 9.0 SDK

### Building the Enclave

To build the enclave, run the following command:

```bash
cd src/NeoServiceLayer.Tee.Enclave
./build_occlum.ps1
```

This will compile the enclave code and create an Occlum instance with the enclave library.

### Running the Enclave

To run the enclave, run the following command:

```bash
cd src/NeoServiceLayer.Tee.Enclave
./run_occlum.ps1
```

This will start the enclave and run the JavaScript entry point.

### Running in Simulation Mode

To run the enclave in simulation mode, set the `OCCLUM_SIMULATION` environment variable to `1`:

```bash
$env:OCCLUM_SIMULATION=1
cd src/NeoServiceLayer.Tee.Enclave
./run_occlum.ps1
```

## JavaScript Execution

The Neo Service Layer supports executing JavaScript code in Occlum enclaves. The JavaScript code is executed using Node.js, which is included in the Occlum instance.

### Example

```javascript
function main(input, secrets, functionId, userId) {
    return {
        message: 'Hello, world!',
        input: input,
        functionId: functionId,
        userId: userId
    };
}
```

This JavaScript function takes input data, secrets, a function ID, and a user ID, and returns a JSON object with a message and the input data.

## Attestation

The Neo Service Layer supports remote attestation for Occlum enclaves. The attestation process verifies the identity and integrity of the enclave to a remote party.

### Attestation Flow

1. The enclave generates an attestation report.
2. The attestation report is sent to the remote party.
3. The remote party verifies the attestation report.
4. If the attestation report is valid, the remote party trusts the enclave.

## Sealing

The Neo Service Layer supports sealing data for Occlum enclaves. Sealing is the process of encrypting data with a key that is derived from the enclave's identity, so that only the same enclave can decrypt the data.

### Sealing Flow

1. The enclave generates a sealing key.
2. The enclave encrypts the data with the sealing key.
3. The encrypted data is stored outside the enclave.
4. When the enclave needs to access the data, it decrypts the data with the sealing key.

## Testing

The Neo Service Layer includes tests for Occlum integration. The tests are located in the `tests/NeoServiceLayer.Occlum.Tests` directory.

### Running Tests

To run the tests, run the following command:

```bash
dotnet test tests/NeoServiceLayer.Occlum.Tests
```

To run the tests in simulation mode, set the `OCCLUM_SIMULATION` environment variable to `1`:

```bash
$env:OCCLUM_SIMULATION=1
dotnet test tests/NeoServiceLayer.Occlum.Tests
```

## Troubleshooting

### Occlum Not Found

If you get an error that Occlum is not found, make sure that Occlum is installed and that the `OCCLUM_PATH` environment variable is set to the Occlum installation directory.

### Enclave Not Found

If you get an error that the enclave is not found, make sure that the enclave is built and that the enclave path is correct.

### JavaScript Execution Errors

If you get an error during JavaScript execution, check the JavaScript code for syntax errors and make sure that the Node.js version in the Occlum instance is compatible with the JavaScript code.

## References

- [Occlum GitHub Repository](https://github.com/occlum/occlum)
- [Occlum Documentation](https://occlum.io/occlum/latest/index.html)
- [Intel SGX Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
