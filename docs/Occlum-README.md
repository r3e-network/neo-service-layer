# Neo Service Layer - Occlum Integration

This document provides information about the Occlum integration in the Neo Service Layer.

## Overview

The Neo Service Layer uses Occlum LibOS to provide a secure execution environment for JavaScript functions. Occlum is a memory-safe, multi-process library OS (LibOS) for Intel SGX that enables applications to run securely in the SGX enclave.

## Prerequisites

- Linux (Ubuntu 20.04 or later recommended)
- Docker (for containerized deployment)
- Intel SGX Driver and SDK
- Occlum (version 0.29.5 or later)

## Building the Enclave

### Using PowerShell Scripts

1. Build the enclave:

   ```powershell
   ./build_occlum.ps1
   ```

2. Run the enclave:

   ```powershell
   ./run_occlum.ps1
   ```

### Using Docker

1. Build the Docker image:

   ```bash
   cd docker/occlum
   docker-compose build
   ```

2. Run the Docker container:

   ```bash
   docker-compose up
   ```

## Configuration

The Neo Service Layer uses the following configuration settings for Occlum:

```json
"Tee": {
  "Type": "Occlum",
  "SimulationMode": true,
  "EnclavePath": "bin/libenclave.so",
  "Debug": true,
  "Occlum": {
    "InstanceDir": "/occlum_instance",
    "LogLevel": "debug",
    "NodeJsPath": "/bin/node",
    "TempDir": "/tmp",
    "EnableDebugMode": true,
    "MaxMemoryMB": 1024,
    "MaxThreads": 32,
    "MaxProcesses": 16,
    "MaxExecutionTimeSeconds": 60
  }
}
```

## Folder Structure

The Occlum integration is organized into the following folders:

- `src/NeoServiceLayer.Tee.Enclave/Enclave/Occlum`: Occlum-specific enclave code
- `src/NeoServiceLayer.Tee.Host/Occlum`: Occlum-specific host code
- `docker/occlum`: Docker files for Occlum

## JavaScript Execution

The Neo Service Layer provides an API endpoint for executing JavaScript functions in the Occlum-based enclave:

```http
POST /api/v1/functions/execute-javascript
Content-Type: application/json

{
  "code": "function main(input) { return { result: input.value * 2 }; }",
  "input": "{ \"value\": 42 }",
  "secrets": "{ \"API_KEY\": \"test_key\" }",
  "functionId": "test_function",
  "userId": "test_user"
}
```

## Remote Attestation

Remote attestation is a process that allows a remote party to verify the integrity of an enclave. Occlum provides remote attestation capabilities through the SGX attestation service.

### Generating an Attestation Report

```csharp
// Create an attestation provider
var attestationProviderFactory = new AttestationProviderFactory(loggerFactory);
var attestationProvider = attestationProviderFactory.CreateAttestationProvider(enclaveInterface.GetEnclaveId());

// Generate an attestation proof
var reportData = Encoding.UTF8.GetBytes("Some data to include in the report");
var attestationProof = await attestationProvider.GenerateAttestationProofAsync(reportData);

// Convert the attestation proof to a string
string proofJson = System.Text.Json.JsonSerializer.Serialize(attestationProof);
```

### Verifying an Attestation Report

```csharp
// Verify an attestation proof
bool isValid = await attestationProvider.VerifyAttestationProofAsync(attestationProof);

if (isValid)
{
    Console.WriteLine("Attestation proof is valid");
}
else
{
    Console.WriteLine("Attestation proof is invalid");
}
```

## Simulation Mode

Occlum supports a simulation mode that allows you to develop and test enclave applications without requiring SGX hardware. To enable simulation mode, set the `OCCLUM_SIMULATION` environment variable to `1` before creating the enclave.

```csharp
// Enable simulation mode
Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");

// Create the enclave
var enclaveInterface = new OcclumInterface(logger, "path/to/enclave.so", serviceProvider);
```

## Troubleshooting

### Common Issues

1. **Occlum instance not found**: Make sure the `InstanceDir` configuration points to a valid Occlum instance.

2. **Node.js not found in Occlum instance**: Ensure Node.js is properly installed in the Occlum instance.

3. **Permission issues**: Ensure the application has permission to access the Occlum instance directory.

4. **Memory or resource limitations**: Adjust the resource limits in the Occlum.json file.

### Logs

Check the following logs for troubleshooting:

- Occlum logs: `/occlum_instance/log/occlum.log`
- Application logs: Check the application logs for Occlum-related errors.

## References

- [Occlum GitHub Repository](https://github.com/occlum/occlum)
- [Occlum Documentation](https://occlum.io/occlum/latest/index.html)
- [Intel SGX Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
