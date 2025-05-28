# Neo Service Layer

The Neo Service Layer is a comprehensive platform that leverages Intel SGX with Occlum LibOS to provide secure, privacy-preserving services for the Neo blockchain ecosystem. It supports both Neo N3 and NeoX (EVM-compatible) blockchains.

## Overview

The Neo Service Layer provides oracle services and other critical infrastructure for the Neo blockchain ecosystem. It uses Intel SGX with Occlum LibOS enclaves for critical operations to ensure security and privacy.

## Core Services

The Neo Service Layer consists of the following core services:

1. **Randomness Service**: Generates verifiable random numbers for use in smart contracts and applications using Intel SGX with Occlum LibOS enclaves.
2. **Oracle Service**: Fetches data from external sources and delivers it to smart contracts with cryptographic proofs.
3. **Data Feeds Service**: Provides decentralized, high-quality price and market data similar to Chainlink Data Feeds.
4. **Key Management Service**: Securely manages cryptographic keys for various operations within Intel SGX with Occlum LibOS enclaves.
5. **Compute Service**: Executes JavaScript computations in a secure enclave with access to user secrets.
6. **Storage Service**: Stores and retrieves data with encryption, compression, and access control.
7. **Compliance Service**: Verifies compliance with regulatory requirements for transactions, addresses, and contracts.
8. **Event Subscription Service**: Subscribes to and receives events from the blockchain, enabling automated contract interactions.
9. **Automation Service**: Provides smart contract automation similar to Chainlink Automation (formerly Keepers).
10. **Cross-Chain Service**: Enables secure cross-chain interoperability similar to Chainlink CCIP.
11. **Proof of Reserve Service**: Provides cryptographic verification of asset backing similar to Chainlink Proof of Reserve.

### Advanced Infrastructure Services

12. **Zero-Knowledge Service**: Provides privacy-preserving computation using zk-SNARKs, zk-STARKs, and other zero-knowledge proof systems.
13. **Prediction Service**: Enables AI-powered prediction and forecasting capabilities for smart contracts.
14. **Pattern Recognition Service**: Provides AI-powered fraud detection, anomaly detection, and behavioral analysis.
15. **Fair Ordering Service**: Provides protection against unfair transaction ordering and MEV attacks.

All services are implemented using Intel SGX with Occlum LibOS enclaves for maximum security and privacy.

## Architecture

The Neo Service Layer is built on a modular architecture with the following components:

- **Service Framework**: Provides the foundation for all services, including service registration, configuration, and lifecycle management.
- **Enclave Integration**: Integrates with Intel SGX and Occlum LibOS enclaves for secure execution of critical operations.
- **Blockchain Integration**: Integrates with Neo N3 and NeoX blockchains for transaction submission and event monitoring.
- **Service Implementations**: Individual service implementations that provide specific functionality.
- **API Layer**: Provides RESTful API endpoints for interacting with the services.

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2025 or later (optional)
- Git
- Intel SGX SDK (for enclave development)
- Occlum LibOS (for enclave development)

### Building the Project

1. Clone the repository:

```bash
git clone https://github.com/neo-project/neo-service-layer.git
cd neo-service-layer
```

2. Build the solution:

```bash
dotnet build
```

3. Run the tests:

```bash
dotnet test
```

### Running the Services

1. Run the API:

```bash
dotnet run --project src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj
```

2. Access the API at `http://localhost:5000`.

## Development

### Adding a New Service

To add a new service to the Neo Service Layer, follow these steps:

1. Create a new project for the service:

```bash
dotnet new classlib -n NeoServiceLayer.Services.YourService -o src/Services/NeoServiceLayer.Services.YourService -f net9.0
```

2. Add the necessary references:

```bash
dotnet add src/Services/NeoServiceLayer.Services.YourService/NeoServiceLayer.Services.YourService.csproj reference src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj
```

3. Implement the service interface and implementation.

4. Create tests for the service.

5. Create documentation for the service.

For detailed instructions, see [Service Framework](docs/architecture/service-framework.md).

### Enclave Development

The Neo Service Layer uses Intel SGX with Occlum LibOS enclaves for critical operations. To develop enclave code:

1. Install the Intel SGX SDK.
2. Install the Occlum LibOS SDK.
3. Implement the enclave code in C++.
4. Build the enclave using the Occlum LibOS SDK.

For detailed instructions, see [Enclave Development](docs/architecture/enclave-development.md).

## Documentation

- [Architecture](docs/architecture/README.md)
- [Services](docs/services/README.md)
- [API](docs/api/README.md)
- [Workflows](docs/workflows/README.md)

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
