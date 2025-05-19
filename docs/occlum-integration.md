# Occlum LibOS Integration Guide

This document provides a comprehensive guide to the Occlum LibOS integration in the Neo Confidential Serverless Layer (NCSL).

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Installation](#installation)
4. [Usage](#usage)
5. [Remote Attestation](#remote-attestation)
6. [Simulation Mode](#simulation-mode)
7. [Troubleshooting](#troubleshooting)
8. [References](#references)

## Overview

The Neo Confidential Serverless Layer (NCSL) uses Occlum LibOS to provide a secure execution environment for JavaScript functions. Occlum is a memory-safe, multi-process library OS (LibOS) for Intel SGX that enables applications to run securely in the SGX enclave.

The integration provides the following features:

- Support for running JavaScript functions in a secure environment
- Support for simulation mode for development and testing
- Remote attestation for verifying the integrity of the enclave
- Secure key management and data sealing
- Persistent storage for enclave data

## Architecture

The Occlum LibOS integration is the exclusive enclave implementation for the Neo Service Layer and consists of the following components:

### Core Components

1. **OcclumInterface**: Provides a high-level interface for interacting with Occlum LibOS.
2. **OcclumManager**: Manages the Occlum instance lifecycle.
3. **OcclumJavaScriptExecution**: Executes JavaScript code in the Occlum enclave.
4. **OcclumAttestationProvider**: Provides remote attestation capabilities using Occlum.
5. **OcclumEnclave**: Main enclave class that provides a C++ interface to the enclave functionality.

### Support Components

1. **AttestationProviderFactory**: Factory for creating attestation providers.
2. **MockAttestationProvider**: A mock attestation provider for testing.
3. **OcclumAvailabilityChecker**: Checks if Occlum is available on the system.
4. **OcclumFileStorageProvider**: Provides persistent storage for enclave data.

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

## Installation

### Prerequisites

- Linux (Ubuntu 18.04 or later recommended)
- Docker (for containerized deployment)
- Intel SGX Driver and SDK
- Occlum (version 0.29.5 or later)

### Installing Occlum

1. Install Occlum:

   ```bash
   # Install dependencies
   sudo apt-get update
   sudo apt-get install -y build-essential cmake libssl-dev libcurl4-openssl-dev pkg-config

   # Install Occlum
   wget https://github.com/occlum/occlum/releases/download/0.29.5/occlum-0.29.5-ubuntu20.04-x86_64.tar.gz
   tar -xzf occlum-0.29.5-ubuntu20.04-x86_64.tar.gz
   cd occlum-0.29.5
   ./install.sh
   ```

2. Verify the installation by running `occlum --version`.

### Building the Enclave

1. Clone the Neo Confidential Serverless Layer repository.
2. Navigate to the `src/NeoServiceLayer.Tee.Enclave` directory.
3. Run `make` to build the enclave.
4. The built enclave will be in the `build` directory.

### Creating an Occlum Instance

1. Create an Occlum instance:

   ```bash
   mkdir -p /occlum_instance
   cd /occlum_instance
   occlum init
   mkdir -p image/bin image/lib image/etc image/node_modules image/tmp

   # Copy Node.js and dependencies
   cp $(which node) image/bin/
   cp -r /usr/lib/x86_64-linux-gnu/libnode* image/lib/
   cp /etc/hosts image/etc/
   cp /etc/resolv.conf image/etc/

   # Install required Node.js modules
   cd /tmp
   npm init -y
   npm install fs-extra crypto-js uuid axios
   cp -r node_modules/* /occlum_instance/image/node_modules/

   # Configure Occlum instance
   cd /occlum_instance
   cat > Occlum.json << EOF
   {
     "resource_limits": {
       "user_space_size": "1GB",
       "kernel_space_heap_size": "64MB",
       "kernel_space_stack_size": "1MB",
       "max_num_of_threads": 32
     },
     "process": {
       "default_stack_size": "4MB",
       "default_heap_size": "32MB",
       "default_mmap_size": "500MB"
     },
     "entry_points": ["/bin/node"],
     "env": {
       "LD_LIBRARY_PATH": "/lib:/usr/lib:/usr/local/lib",
       "PATH": "/bin:/usr/bin",
       "OCCLUM": "yes"
     }
   }
   EOF

   # Build the Occlum instance
   occlum build
   ```

## Usage

### Creating an Enclave

```csharp
// Create a logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<OcclumInterface>();

// Create the enclave
var serviceProvider = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .BuildServiceProvider();
var enclaveInterface = new OcclumInterface(logger, "path/to/enclave.so", serviceProvider);

// Use the enclave
var result = await enclaveInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);

// Dispose the enclave when done
enclaveInterface.Dispose();
```

### Running JavaScript Functions with Occlum

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

1. **Occlum instance not found**:
   - Ensure the `InstanceDir` configuration points to a valid Occlum instance.
   - Check if the Occlum instance is properly built.

2. **Node.js not found in Occlum instance**:
   - Ensure Node.js is properly installed in the Occlum instance.
   - Check if the `NodeJsPath` configuration is correct.

3. **Permission issues**:
   - Ensure the application has permission to access the Occlum instance directory.
   - Run the application with appropriate privileges.

4. **Memory or resource limitations**:
   - Adjust the resource limits in the Occlum.json file.
   - Increase the `MaxMemoryMB` configuration.

### Logs

Check the following logs for troubleshooting:

- Occlum logs: `/occlum_instance/log/occlum.log`
- Application logs: Check the application logs for Occlum-related errors.

## References

- [Occlum GitHub Repository](https://github.com/occlum/occlum)
- [Occlum Documentation](https://occlum.io/occlum/latest/index.html)
- [Intel SGX Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
