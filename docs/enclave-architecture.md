# Neo Service Layer Enclave Architecture

## Overview

The Neo Service Layer (NSL) enclave is a secure execution environment built on Intel SGX technology using Occlum LibOS. It provides a trusted execution environment for running JavaScript functions with access to user secrets, persistent storage, and other secure services.

## Architecture

The NSL enclave architecture consists of several key components:

```
┌─────────────────────────────────────────────────────────────────┐
│                     Neo Service Layer Enclave                    │
│                                                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────┐ │
│  │ JavaScript  │  │   Secret    │  │   Storage   │  │   Gas    │ │
│  │   Engine    │  │  Manager    │  │   Manager   │  │ Accounting│ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └──────────┘ │
│          │               │                │               │      │
│          └───────────────┼────────────────┼───────────────┘      │
│                          │                │                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────┐ │
│  │    Key      │  │ Attestation │  │  Occlum     │  │  Logger  │ │
│  │  Manager    │  │   Service   │  │ Integration │  │          │ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └──────────┘ │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Core Components

1. **JavaScript Engine**: Executes JavaScript code within the enclave.
2. **Secret Manager**: Manages user secrets securely within the enclave.
3. **Storage Manager**: Provides persistent storage for enclave data.
4. **Gas Accounting**: Tracks resource usage during JavaScript execution.
5. **Key Manager**: Manages cryptographic keys for the enclave.
6. **Attestation Service**: Provides attestation for the enclave.
7. **Occlum Integration**: Integrates with Occlum LibOS for SGX support.
8. **Logger**: Provides secure logging capabilities.

## Workflow

### JavaScript Execution Workflow

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│   Host   │     │  Enclave │     │ JavaScript│     │  Secret  │
│Application│────▶│ Interface│────▶│  Engine  │────▶│ Manager  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
                                        │                │
                                        ▼                │
                                  ┌──────────┐           │
                                  │  Storage │           │
                                  │ Manager  │◀──────────┘
                                  └──────────┘
                                        │
                                        ▼
                                  ┌──────────┐
                                  │   Gas    │
                                  │Accounting│
                                  └──────────┘
```

1. The host application calls the enclave interface to execute JavaScript code.
2. The enclave interface validates the request and forwards it to the JavaScript engine.
3. The JavaScript engine initializes the execution environment and loads any required user secrets.
4. The JavaScript engine executes the code, tracking gas usage.
5. The JavaScript engine returns the result to the host application.

### Secret Management Workflow

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│   Host   │     │  Enclave │     │  Secret  │     │   Key    │
│Application│────▶│ Interface│────▶│ Manager  │────▶│ Manager  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
                                        │                │
                                        ▼                │
                                  ┌──────────┐           │
                                  │  Storage │◀──────────┘
                                  │ Manager  │
                                  └──────────┘
```

1. The host application calls the enclave interface to store, retrieve, or delete a user secret.
2. The enclave interface validates the request and forwards it to the secret manager.
3. The secret manager encrypts/decrypts the secret using the key manager.
4. The secret manager stores/retrieves the encrypted secret using the storage manager.
5. The secret manager returns the result to the host application.

## Data Flow

### JavaScript Execution Data Flow

```
┌─────────────┐
│    Input    │
│    JSON     │
└─────────────┘
       │
       ▼
┌─────────────┐    ┌─────────────┐
│  JavaScript │    │    User     │
│    Code     │───▶│   Secrets   │
└─────────────┘    └─────────────┘
       │                 │
       ▼                 ▼
┌─────────────────────────────────┐
│      JavaScript Engine          │
└─────────────────────────────────┘
       │
       ▼
┌─────────────┐
│   Result    │
│    JSON     │
└─────────────┘
```

1. The JavaScript engine receives the input JSON, JavaScript code, and user secrets.
2. The JavaScript engine executes the code with access to the input and secrets.
3. The JavaScript engine returns the result as JSON.

### Secret Management Data Flow

```
┌─────────────┐
│    User     │
│     ID      │
└─────────────┘
       │
       ▼
┌─────────────┐    ┌─────────────┐
│   Secret    │    │  Encryption │
│    Name     │───▶│     Key     │
└─────────────┘    └─────────────┘
       │                 │
       ▼                 ▼
┌─────────────────────────────────┐
│         Secret Manager          │
└─────────────────────────────────┘
       │
       ▼
┌─────────────┐
│  Encrypted  │
│   Secret    │
└─────────────┘
       │
       ▼
┌─────────────┐
│  Persistent │
│   Storage   │
└─────────────┘
```

1. The secret manager receives the user ID, secret name, and secret value.
2. The secret manager encrypts the secret value using the encryption key.
3. The secret manager stores the encrypted secret in persistent storage.
4. For retrieval, the process is reversed.

## Security Considerations

### Enclave Security

1. **Attestation**: The enclave provides attestation to verify its identity and integrity.
2. **Sealing**: Sensitive data is sealed to the enclave to protect it at rest.
3. **Memory Encryption**: All enclave memory is encrypted by the SGX hardware.
4. **Secure Communication**: Communication with the enclave is secured using TLS.

### Secret Management

1. **Encryption**: All secrets are encrypted before being stored.
2. **Access Control**: Secrets are only accessible to the user who created them.
3. **Memory Clearing**: Sensitive data is cleared from memory after use.

### JavaScript Execution

1. **Isolation**: JavaScript execution is isolated from the host system.
2. **Resource Limits**: Gas accounting prevents resource exhaustion.
3. **Input Validation**: All inputs are validated before processing.

## Deployment

### Requirements

1. **Hardware**: Intel CPU with SGX support
2. **Software**: Occlum LibOS, SGX driver
3. **OS**: Linux (Ubuntu 20.04 or later recommended)

### Deployment Options

1. **Bare Metal**: Deploy directly on SGX-enabled hardware.
2. **Cloud**: Deploy on cloud providers with SGX support (e.g., Alibaba Cloud).

## Monitoring and Operations

### Logging

The enclave uses a secure logging system that provides:

1. **Log Levels**: Different log levels for different types of messages.
2. **Log Rotation**: Automatic rotation of log files to prevent disk space issues.
3. **Secure Logging**: Logs are protected from tampering.

### Metrics

The enclave provides metrics for monitoring:

1. **JavaScript Execution**: Number of executions, execution time, gas usage.
2. **Secret Management**: Number of secrets stored, retrieved, deleted.
3. **Storage**: Storage usage, read/write operations.

### Alerts

The enclave can be configured to generate alerts for:

1. **Error Conditions**: JavaScript execution errors, storage errors.
2. **Resource Usage**: High gas usage, storage space running low.
3. **Security Events**: Attestation failures, unauthorized access attempts.

## Troubleshooting

### Common Issues

1. **JavaScript Execution Errors**: Check the JavaScript code for syntax errors.
2. **Storage Errors**: Check disk space and permissions.
3. **Attestation Failures**: Check SGX configuration and update the platform software.

### Debugging

1. **Simulation Mode**: Run the enclave in simulation mode for easier debugging.
2. **Logging**: Increase the log level for more detailed information.
3. **Unit Tests**: Run unit tests to isolate issues.

## Conclusion

The Neo Service Layer enclave provides a secure execution environment for JavaScript functions with access to user secrets and persistent storage. It is built on Intel SGX technology using Occlum LibOS and provides a comprehensive set of security features to protect sensitive data and code execution.
