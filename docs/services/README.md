# Neo Service Layer Services

This directory contains documentation for the various services provided by the Neo Service Layer. All services leverage Intel SGX with Occlum LibOS enclaves to ensure security, privacy, and verifiability.

## Core Services

### [Randomness Service](randomness-service.md)

The Randomness Service provides secure, verifiable random number generation using Intel SGX with Occlum LibOS enclaves. It generates cryptographically secure random numbers that can be verified by third parties.

**Key Features:**
- Secure random number generation within Intel SGX with Occlum LibOS enclaves
- Verifiable randomness with cryptographic proofs
- Support for multiple blockchain types (Neo N3, NeoX)
- Generation of random numbers, bytes, and strings
- Custom seed support and batch generation

### [Oracle Service](oracle-service.md)

The Oracle Service provides secure, verifiable data feeds from external sources to blockchain smart contracts, including comprehensive price feed capabilities. It leverages Intel SGX with Occlum LibOS enclaves to ensure the integrity and confidentiality of data.

**Key Features:**
- Secure data feeds from external sources
- Decentralized price and market data aggregation
- Verifiable data with cryptographic proofs
- Support for multiple blockchain types (Neo N3, NeoX)
- Data source management and subscriptions
- Real-time price feeds for cryptocurrencies and traditional assets
- Request batching and custom data transformations
- High availability with redundant data sources

### [Key Management Service](key-management-service.md)

The Key Management Service provides secure key generation, storage, and management using Intel SGX with Occlum LibOS enclaves. It enables secure signing and encryption operations for blockchain applications.

**Key Features:**
- Secure key generation and storage within Intel SGX with Occlum LibOS enclaves
- Support for multiple key types and algorithms
- Key rotation and revocation
- Threshold signatures and multi-party computation
- Hardware security module (HSM) integration

### [Compute Service](compute-service.md)

The Compute Service provides secure, verifiable computation and JavaScript execution within Intel SGX with Occlum LibOS enclaves. It enables secure off-chain computation with access to user secrets.

**Key Features:**
- Secure JavaScript execution within Intel SGX with Occlum LibOS enclaves
- User secret management and access control
- Verifiable computation results with cryptographic proofs
- Blockchain integration for smart contract interactions
- Gas accounting and resource management

### [Storage Service](storage-service.md)

The Storage Service provides secure, encrypted data storage with compression, chunking, and access control. It supports multiple storage providers and transaction support.

**Key Features:**
- Encrypted data storage with compression and chunking
- Multiple storage provider implementations
- Access control and versioning support
- Transaction support for data consistency
- Backup and recovery mechanisms

### [Compliance Service](compliance-service.md)

The Compliance Service provides regulatory compliance verification for transactions, addresses, and contracts. It helps ensure adherence to various regulatory frameworks.

**Key Features:**
- Regulatory compliance verification
- Risk scoring and violation reporting
- Support for multiple compliance frameworks
- Real-time transaction monitoring
- Audit trail and reporting

### [Event Subscription Service](event-subscription-service.md)

The Event Subscription Service enables applications to subscribe to blockchain events and trigger actions based on those events. It provides reliable event delivery and processing.

**Key Features:**
- Blockchain event monitoring and filtering
- Reliable event delivery with at-least-once semantics
- Event transformation and enrichment
- Webhook and callback integration
- Retry mechanisms and error handling

### [Automation Service](automation-service.md)

The Automation Service provides reliable, decentralized smart contract automation, similar to Chainlink Automation (formerly Keepers). It enables automated execution of smart contract functions.

**Key Features:**
- Smart contract automation based on conditions
- Time-based and condition-based triggers
- High reliability with redundancy and failover
- Gas optimization for automated transactions
- Custom logic support for complex automation

### [Cross-Chain Service](cross-chain-service.md)

The Cross-Chain Service provides secure cross-chain interoperability, similar to Chainlink CCIP. It enables seamless communication and asset transfers between different blockchains.

**Key Features:**
- Cross-chain messaging and token transfers
- Smart contract calls across chains
- Message verification with cryptographic proofs
- Support for multiple blockchains
- Programmable transfers with smart contract execution

### [Proof of Reserve Service](proof-of-reserve-service.md)

The Proof of Reserve Service provides cryptographic verification of asset backing for tokenized assets and stablecoins, similar to Chainlink Proof of Reserve.

**Key Features:**
- Asset verification and reserve monitoring
- Real-time monitoring of reserve levels
- Multi-asset support (fiat, crypto, commodities)
- Cryptographic proofs of reserve adequacy
- Automated alerts and audit trails

## Advanced Infrastructure Services

### [Zero-Knowledge Service](zero-knowledge-service.md)

The Zero-Knowledge Service provides privacy-preserving computation and verification capabilities using zk-SNARKs, zk-STARKs, and other zero-knowledge proof systems.

**Key Features:**
- zk-SNARK and zk-STARK proof generation and verification
- Private set intersection and confidential voting
- Selective disclosure and range proofs
- Privacy-preserving transactions and computations
- Circuit compilation and verification

### [AI Inference Service](ai-inference-service.md)

The AI Inference Service provides secure artificial intelligence and machine learning capabilities for smart contracts, enabling intelligent automation and decision-making.

**Key Features:**
- Secure AI model inference within Intel SGX with Occlum LibOS enclaves
- Prediction markets and sentiment analysis
- Pattern recognition and anomaly detection
- Natural language processing and computer vision
- Model verification and integrity protection

### [Prediction Service](prediction-service.md)

The Prediction Service provides AI-powered prediction and forecasting capabilities for smart contracts, enabling market forecasting, sentiment analysis, and trend prediction.

**Key Features:**
- Market prediction and price forecasting
- Sentiment analysis from social media and news
- Time series forecasting and trend detection
- Risk prediction and probability assessment
- Model verification and confidence intervals

### [Pattern Recognition Service](pattern-recognition-service.md)

The Pattern Recognition Service provides AI-powered pattern detection and classification capabilities, enabling fraud detection, anomaly detection, and behavioral analysis.

**Key Features:**
- Fraud detection and transaction monitoring
- Anomaly detection and outlier identification
- Behavioral analysis and user classification
- Risk pattern recognition in financial data
- Real-time detection and model verification

### [Fair Ordering Service](fair-ordering-service.md)

The Fair Ordering Service provides protection against unfair transaction ordering and MEV attacks, ensuring transaction fairness across both Neo N3 and NeoX blockchains.

**Key Features:**
- Fair transaction ordering across both chains
- MEV protection for NeoX (EVM-compatible)
- Front-running and sandwich attack prevention
- Private transaction pool and batch processing
- Fairness guarantees and cryptographic proofs

## Service Framework

The Neo Service Layer is built on a modular service framework that provides common functionality for all services:

### Service Lifecycle Management

- Service registration and discovery
- Service initialization and startup
- Service health monitoring and metrics
- Service shutdown and cleanup

### Dependency Management

- Service dependency declaration and resolution
- Dependency validation and health checking
- Dependency injection and configuration

### Configuration Management

- Configuration loading and validation
- Environment-specific configuration
- Dynamic configuration updates

### Logging and Monitoring

- Structured logging with correlation IDs
- Metrics collection and reporting
- Health checks and status reporting
- Alerting and notification

### Security

- Authentication and authorization
- Secure communication with TLS
- Enclave attestation and verification
- Secure storage and key management

## Developing New Services

To develop a new service for the Neo Service Layer, follow these steps:

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

For detailed instructions, see [Service Framework](../architecture/service-framework.md).
