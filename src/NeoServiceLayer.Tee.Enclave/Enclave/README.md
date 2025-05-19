# Neo Service Layer Enclave with Occlum LibOS

This directory contains the enclave implementation for the Neo Service Layer using Occlum LibOS. Occlum is a memory-safe, multi-process library OS (LibOS) for Intel SGX. It enables legacy applications to run securely in SGX enclaves without any modification.

## Overview

The Neo Service Layer Enclave provides a secure environment for executing JavaScript code within an SGX enclave using Occlum LibOS. It includes the following features:

- Secure JavaScript execution using Node.js
- User secret management
- Secure random number generation
- Remote attestation
- Persistent storage
- Gas accounting
- Compliance verification

## Architecture

The enclave implementation consists of the following components:

- **OcclumIntegration**: Provides integration with Occlum LibOS for running JavaScript code in SGX enclaves
- **OcclumEnclave**: Main enclave class that provides a C++ interface to the enclave functionality
- **JavaScriptEngine**: Executes JavaScript code using Node.js
- **SecretManager**: Manages user secrets
- **StorageManager**: Manages persistent storage
- **KeyManager**: Manages cryptographic keys
- **EventTriggerManager**: Manages event triggers
- **RemoteAttestationManager**: Manages remote attestation
- **BackupManager**: Manages backups
- **GasAccountingManager**: Manages gas accounting
- **RandomnessService**: Provides secure random number generation
- **ComplianceService**: Verifies compliance of JavaScript code

## Building

To build the enclave, you need to have Occlum installed. You can build the enclave using CMake:

```bash
mkdir build
cd build
cmake ..
make
```

This will build the enclave library and create an Occlum instance.

## Running

To run the enclave, you can use the following command:

```bash
cd build
make run_enclave
```

This will run the enclave in Occlum.

## Docker

You can also build and run the enclave in a Docker container:

```bash
cd build
make docker
make docker_run
```

## Testing

To test the enclave, you can use the provided JavaScript test file:

```bash
cd build/occlum_instance
occlum run /bin/node /bin/enclave_main.js
```

## API

The enclave provides a C++ API that can be used by the host application. The API is defined in the `OcclumEnclave.h` file.

### JavaScript Execution

```cpp
// Create a JavaScript context
uint64_t create_js_context();

// Destroy a JavaScript context
bool destroy_js_context(uint64_t context_id);

// Execute JavaScript code
std::string execute_js_code(uint64_t context_id, const std::string& code, const std::string& input,
                           const std::string& user_id, const std::string& function_id);

// Execute JavaScript code (legacy interface)
std::string execute_javascript(const std::string& code, const std::string& input, const std::string& secrets,
                              const std::string& function_id, const std::string& user_id, uint64_t& gas_used);
```

### User Secrets

```cpp
// Store a user secret
bool store_user_secret(const std::string& user_id, const std::string& secret_name, const std::string& secret_value);

// Get a user secret
std::string get_user_secret(const std::string& user_id, const std::string& secret_name);

// Delete a user secret
bool delete_user_secret(const std::string& user_id, const std::string& secret_name);

// List user secrets
std::vector<std::string> list_user_secrets(const std::string& user_id);
```

### Random Number Generation

```cpp
// Generate a random number
int generate_random_number(int min, int max);

// Generate random bytes
std::vector<uint8_t> generate_random_bytes(size_t length);

// Generate a random UUID
std::string generate_uuid();
```

### Attestation

```cpp
// Generate attestation evidence
std::vector<uint8_t> generate_attestation();

// Verify attestation evidence
bool verify_attestation(const std::vector<uint8_t>& evidence, const std::vector<uint8_t>& endorsements);
```

### Compliance

```cpp
// Verify compliance of JavaScript code
std::string verify_compliance(const std::string& code, const std::string& user_id,
                             const std::string& function_id, const std::string& compliance_rules);
```

### Storage

```cpp
// Initialize the persistent storage system
bool initialize_storage(const std::string& storage_path);
```

### Occlum Operations

```cpp
// Initialize Occlum
bool occlum_init(const std::string& instance_dir, const std::string& log_level);

// Execute a command in Occlum
int occlum_exec(const std::string& path, const std::vector<std::string>& argv, const std::vector<std::string>& env);
```

## JavaScript Bindings

The enclave also provides JavaScript bindings that can be used to interact with the enclave from JavaScript code. The bindings are defined in the `libneoserviceenclave.js` file.
