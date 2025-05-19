# Neo Service Layer

The Neo Service Layer (NSL) is a confidential computing platform that enables secure JavaScript execution in Intel SGX enclaves using the Occlum LibOS. It provides a robust and secure runtime environment for executing sensitive code with strong integrity and confidentiality guarantees.

## Project Structure

The project follows a clean architecture approach with the following key components:

### Core Components

- `NeoServiceLayer.Common` - Common models, interfaces, and utilities shared across the solution
- `NeoServiceLayer.Core` - Domain models, interfaces, and business logic
- `NeoServiceLayer.Infrastructure` - Implementations of core interfaces, data access, and external services integration
- `NeoServiceLayer.Api` - API controllers and endpoints

### TEE Components

- `NeoServiceLayer.Tee.Shared` - Shared interfaces and models for TEE integration
- `NeoServiceLayer.Tee.Host` - Host-side code for TEE interaction
- `NeoServiceLayer.Tee.Enclave` - Enclave-side code that runs in the trusted environment

## Key Features

- Secure JavaScript execution in SGX enclaves
- Remote attestation for enclave verification
- Secure storage with encryption and integrity protection
- Event-driven trigger system for blockchain events
- Verifiable random number generation
- API key-based authentication

## Development Requirements

- .NET 6.0 SDK or higher
- Docker (for containerized testing)
- Intel SGX SDK (for production deployment)
- Occlum SDK (for Occlum LibOS integration)

## Getting Started

1. Clone the repository
2. Build the solution
3. Run tests
4. Start the API

## Documentation

Detailed documentation is available in the `docs` directory:

- Architecture Overview: `docs/architecture/overview.md`
- API Reference: `docs/api`
- Developer Guide: `docs/Development.md`

## Recent Changes

### Code Organization

- Broke down large classes into partial classes for better maintainability
- Moved common functionality to appropriate base classes and interfaces
- Standardized naming conventions and code style using `.editorconfig` and `Directory.Build.props`
- Added comprehensive documentation

### Functional Enhancements

- Implemented trigger registration methods for blockchain events
- Added verifiable random number generation
- Created secure temporary file utilities
- Enhanced security features with proper input validation and error handling

## What is NSL?

The Neo Service Layer allows developers to run JavaScript functions in a secure and confidential environment using Trusted Execution Environments (TEEs) like Intel SGX through Occlum LibOS. This ensures that sensitive code and data remain protected even if the underlying infrastructure is compromised. The platform provides:

- **Confidentiality**: Your code and data are encrypted in memory
- **Integrity**: Any tampering with your code or data is detected and prevented
- **Attestation**: Remote verification of the execution environment's identity and code
- **Secure Secrets**: Store API keys and other sensitive data securely
- **Event-Driven Execution**: Trigger functions based on blockchain or external events
- **Resource Management**: Track and limit resource usage with GAS accounting

## Core Services

The Neo Service Layer provides seven core services:

1. **Confidential JavaScript Function Execution**: Executes JavaScript functions securely within a Trusted Execution Environment (TEE)
2. **User Secrets Management**: Securely stores and manages user-specified secrets
3. **Event-Triggered Function Execution**: Allows functions to be triggered by registered events
4. **Randomness Service**: Generates verifiable random numbers for applications
5. **Attestation Service**: Verifies the integrity of the TEE
6. **Key Management Service**: Manages cryptographic keys securely
7. **GAS Accounting Service**: Tracks resource usage for JavaScript function execution

## Architecture

The Neo Service Layer follows a layered architecture approach:

1. **API Layer**: Provides RESTful endpoints for clients to interact with the NSL
2. **Service Layer**: Contains the business logic and orchestrates the execution of JavaScript functions
3. **TEE Layer**: Executes JavaScript functions in a secure environment using Occlum LibOS
4. **Infrastructure Layer**: Provides supporting services such as database, message queue, and monitoring
5. **Storage Layer**: Provides persistent storage capabilities using Occlum's file system
6. **Blockchain Layer**: Integrates with the Neo N3 blockchain for event monitoring and triggering functions

## Neo N3 Blockchain Integration

The Neo Service Layer integrates with the Neo N3 blockchain through a workflow where:

1. **Neo N3 Smart Contracts**: Smart contracts on the Neo N3 blockchain emit events to request JavaScript execution in the Neo Service Layer
   - Smart contracts emit events with function ID, input data, and user ID
   - Smart contracts include callback methods to receive results from the Neo Service Layer

2. **NeoN3BlockchainService**: Service for interacting with the Neo N3 blockchain
   - Monitoring blockchain for events
   - Retrieving transaction details
   - Sending callback transactions with JavaScript execution results

3. **NeoN3EventListenerService**: Service for listening to Neo N3 blockchain events
   - Subscribing to contract events
   - Detecting JavaScript execution requests
   - Triggering JavaScript functions based on events

4. **JavaScript Functions**: JavaScript functions executed in the secure enclave
   - Access user secrets securely stored in the enclave
   - Perform confidential computations
   - Send callback transactions to the Neo N3 blockchain with results

This architecture provides a powerful combination of blockchain transparency and confidential computing, allowing sensitive operations to be performed securely while still integrating with the public blockchain.

## Project Structure

```
NeoServiceLayer/
├── src/
│   ├── NeoServiceLayer.Api/                # API project
│   ├── NeoServiceLayer.Core/               # Core domain models and services
│   ├── NeoServiceLayer.Infrastructure/     # Infrastructure services
│   ├── NeoServiceLayer.Tee.Host/           # TEE host process
│   │   ├── Occlum/                         # Occlum integration
│   │   └── Storage/                        # Storage implementations
│   ├── NeoServiceLayer.Tee.Enclave/        # TEE enclave code (.NET and C++)
│   │   └── Enclave/                        # Native enclave code (C++)
│   └── NeoServiceLayer.Shared/             # Shared components
├── tests/
│   ├── NeoServiceLayer.Api.Tests/
│   ├── NeoServiceLayer.Core.Tests/
│   ├── NeoServiceLayer.Infrastructure.Tests/
│   ├── NeoServiceLayer.Tee.Host.Tests/
│   ├── NeoServiceLayer.Tee.Enclave.Tests/
│   ├── NeoServiceLayer.Occlum.Tests/       # Occlum-specific tests
│   └── NeoServiceLayer.Integration.Tests/  # Integration tests
├── docs/
│   ├── Architecture.md
│   ├── API.md
│   ├── Deployment.md
│   ├── Development.md
│   └── persistent-storage.md               # Persistent storage documentation
├── examples/
│   ├── SmartContractExecution/
│   │   ├── ConfidentialToken/
│   │   └── ConfidentialDataProcessing/
│   └── JavaScriptFunctions/
└── deployment/
    └── docker/                             # Docker configuration files
```

## Getting Started

### Prerequisites

- Windows 10/11 or Ubuntu 20.04+
- .NET 9.0 SDK
- Docker Desktop
- Visual Studio 2022 or Visual Studio Code
- Git
- Intel SGX SDK
- Occlum LibOS
- Neo N3 Wallet (for interacting with the Neo N3 blockchain)
- Neo-CLI or Neo-GUI (for deploying smart contracts)

### Building the Solution

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run only Occlum-related tests
dotnet test --filter "Category=Occlum"

# Run only Storage-related tests
dotnet test --filter "Category=Storage"

# Run tests with simulation mode
$env:OCCLUM_SIMULATION=1  # Windows PowerShell
export OCCLUM_SIMULATION=1  # Linux/macOS
dotnet test
```

#### Test Categories

The tests are organized into the following categories:

**Occlum Tests** (Category=Occlum):
- **OcclumTests**: Tests the basic Occlum functionality
- **OcclumJavaScriptExecutionTests**: Tests JavaScript execution in the Occlum enclave
- **OcclumUserSecretTests**: Tests secure storage and retrieval of user secrets in the Occlum enclave
- **OcclumGasAccountingTests**: Tests GAS accounting in the Occlum enclave
- **OcclumEventTriggerTests**: Tests event trigger processing in the Occlum enclave

**Storage Tests** (Category=Storage):
- **OcclumFileStorageTests**: Tests the Occlum file storage provider
- **OcclumIntegrationTests**: Tests the integration between Occlum and the application

**Additional Storage Tests**:
- **PersistentStorageTests**: Tests the persistent storage functionality
- **StorageProviderTests**: Tests the storage provider implementations
- **StorageTransactionTests**: Tests the storage transaction functionality

All tests can run in simulation mode without requiring SGX hardware, making them suitable for CI/CD pipelines.

### Running the API

```bash
cd src/NeoServiceLayer.Api
dotnet run
```

### Running with Docker Compose

```bash
# For standard deployment
docker-compose up -d

# For Occlum deployment
docker-compose -f docker/occlum/docker-compose.yml up -d
```

This will start all the required services, including:
- Neo Confidential Serverless Layer API
- TEE Host with Occlum support
- Neo N3 Node
- Database
- Message Queue
- Persistent Storage
- Monitoring (Prometheus, Grafana)
- Logging (Elasticsearch, Kibana)
- Tracing (Jaeger)

### Running in Simulation Mode

For development and testing without SGX hardware, you can use the simulation mode:

```bash
# Set Occlum simulation mode
export OCCLUM_SIMULATION=1  # Linux/macOS
$env:OCCLUM_SIMULATION=1    # Windows PowerShell

# Run the application
dotnet run --project src/NCSL.Api

# Or with Docker for Occlum
docker-compose -f docker/occlum/docker-compose.yml -e OCCLUM_SIMULATION=1 up -d
```

### Using the Neo Service Layer API

The Neo Service Layer provides a RESTful API for managing JavaScript functions, user secrets, and event triggers:

```bash
# Create a JavaScript function
curl -X POST http://localhost:5000/api/v1/functions \
  -H "Content-Type: application/json" \
  -d '{"functionId": "token-swap", "code": "function main(input) { /* function code */ }", "description": "A function that handles token swaps"}'

# Store a user secret
curl -X POST http://localhost:5000/api/v1/secrets \
  -H "Content-Type: application/json" \
  -d '{"userId": "user123", "name": "api_key", "value": "sk_test_abcdefghijklmnopqrstuvwxyz", "description": "API key for external service"}'

# Create an event trigger for a Neo N3 smart contract
curl -X POST http://localhost:5000/api/v1/triggers \
  -H "Content-Type: application/json" \
  -d '{"functionId": "token-swap", "eventType": "ExecuteJavaScript", "filters": {"contractHash": "0x1234567890abcdef", "eventName": "ExecuteJavaScript"}}'

# Get execution results
curl -X GET http://localhost:5000/api/v1/executions/{executionId} \
  -H "Content-Type: application/json"
```

### Interacting with Neo N3 Smart Contracts

To trigger JavaScript execution from a Neo N3 smart contract:

```csharp
// In your Neo N3 smart contract
[DisplayName("ExecuteJavaScript")]
public static event Action<string, string, string, string> OnExecuteJavaScript;

public static void RequestExecution(string functionId, string input, string userId)
{
    string requestId = $"{Runtime.CallingScriptHash}_{Runtime.Time}";
    OnExecuteJavaScript(functionId, input, userId, requestId);
}
```

The Neo Service Layer will detect this event, execute the JavaScript function in the secure enclave, and send the result back to the blockchain via a callback transaction.

## Documentation

- [Architecture](docs/Architecture.md)
- [API](docs/API.md)
- [Deployment](docs/Deployment.md)
- [Development](docs/Development.md)
- [Persistent Storage](docs/persistent-storage.md)

## Contributing

Please read [Development.md](docs/Development.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Neo Foundation
- Occlum Team
- Intel SGX Team
- JavaScript Engine Contributors

## Security Considerations

When deploying the Neo Confidential Serverless Layer in production:

1. **Hardware Requirements**: Use genuine Intel CPUs with SGX support
2. **Production Mode**: Always use hardware mode (not simulation mode) in production
3. **Remote Attestation**: Implement remote attestation to verify the Occlum enclave's identity
4. **Secure Key Management**: Use a hardware security module (HSM) for key management
5. **Regular Updates**: Keep the Intel SGX SDK, Occlum, and all dependencies up to date
6. **Security Audits**: Conduct regular security audits of your JavaScript functions
7. **Input Validation**: Validate all inputs to JavaScript functions to prevent injection attacks
8. **Resource Limits**: Set appropriate GAS limits to prevent resource exhaustion attacks
9. **Persistent Storage Security**: Ensure data stored in Occlum's file system is encrypted and protected
10. **Secure Deletion**: Implement secure data deletion procedures for sensitive data
