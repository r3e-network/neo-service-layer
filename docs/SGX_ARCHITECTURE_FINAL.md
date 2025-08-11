# Neo Service Layer - Final SGX Architecture

## Overview

The Neo Service Layer has been fully integrated with Intel SGX (Software Guard Extensions) to provide hardware-based security and privacy-preserving computation for all services. This document describes the final architecture after complete SGX integration.

## Architecture Components

### 1. SGX Infrastructure

#### Core Components
- **Occlum LibOS**: Provides a memory-safe, multi-process LibOS for Intel SGX
- **Rust-based Enclave Runtime**: High-performance enclave implementation
- **Deno JavaScript Engine**: Secure JavaScript execution within enclaves
- **Sealed Storage**: SGX-based persistent storage with hardware encryption

#### Security Features
- **Remote Attestation**: Cryptographic proof of enclave integrity
- **Sealed Data**: Data encrypted with enclave-specific keys
- **Memory Protection**: Hardware-enforced memory isolation
- **Side-Channel Protection**: Mitigations against timing and cache attacks

### 2. Service Integration Pattern

Every service in the Neo Service Layer now follows this integration pattern:

```csharp
public class ServiceName : EnclaveBlockchainServiceBase
{
    private readonly SGXPersistence _sgxPersistence;
    
    // Service uses SGX for:
    // 1. Privacy-preserving computation (JavaScript in enclave)
    // 2. Secure storage (SGX sealed storage)
    // 3. Cryptographic operations
    // 4. Sensitive data processing
}
```

### 3. Privacy-Preserving JavaScript Templates

Each service has specialized JavaScript templates for privacy-preserving operations:

- **AbstractAccount**: Account abstraction with privacy
- **Voting**: Anonymous voting and tallying
- **SocialRecovery**: Private guardian management
- **KeyManagement**: Secure key derivation and storage
- **ZeroKnowledge**: ZK proof generation and verification
- **SmartContracts**: Private smart contract execution
- **Oracle**: Confidential external data processing
- **Notification**: Privacy-preserving alerts

### 4. SGX Storage Architecture

#### Storage Hierarchy
```
IEnclaveStorageService
    ├── SealDataAsync()      // Encrypt and store
    ├── UnsealDataAsync()    // Retrieve and decrypt
    ├── ListSealedItemsAsync()  // List stored items
    └── DeleteSealedDataAsync() // Secure deletion
```

#### Storage Features
- **Sealing Policies**: MrEnclave or MrSigner based
- **Automatic Key Management**: Hardware-derived keys
- **Service Isolation**: Each service has isolated storage
- **Backup Support**: Re-sealing for backup/restore

### 5. Service Categories with SGX

#### Core Services (4)
- Storage Service (persistent + SGX)
- Health Service (secure monitoring)
- Configuration Service (encrypted config)
- ServiceRegistry (attestation support)

#### Security Services (6)
- KeyManagement (HSM + SGX)
- Authentication (secure tokens)
- NetworkSecurity (encrypted channels)
- Compliance (audit in SGX)
- SecretsManagement (sealed secrets)
- EnclaveStorage (core SGX storage)

#### Blockchain Services (7)
- AbstractAccount (privacy accounts)
- SmartContracts (confidential execution)
- Oracle (secure data feeds)
- CrossChain (private bridges)
- EventSubscription (encrypted events)
- Randomness (secure RNG)
- ProofOfReserve (private proofs)

#### AI Services (2)
- PatternRecognition (private ML)
- Prediction (confidential analytics)

#### Advanced Services (1)
- FairOrdering (MEV protection in SGX)

### 6. Migration Path

Services were migrated in phases:
1. **Phase 1**: Core infrastructure (EnclaveManager, Storage)
2. **Phase 2**: Critical services (AbstractAccount, Voting)
3. **Phase 3**: Security services (KeyManagement, Secrets)
4. **Phase 4**: Remaining services with full SGX integration

### 7. Performance Optimizations

- **Batch Operations**: Minimize enclave transitions
- **Caching**: Secure in-enclave caching
- **Async Processing**: Non-blocking enclave operations
- **Connection Pooling**: Reuse enclave sessions

### 8. Security Guarantees

With full SGX integration, the Neo Service Layer provides:

1. **Confidentiality**: All sensitive data processed in enclaves
2. **Integrity**: Hardware-enforced code and data protection
3. **Attestation**: Cryptographic proof of service authenticity
4. **Privacy**: Zero-knowledge operations where applicable
5. **Secure Storage**: Hardware-encrypted persistent storage

## Deployment

### Requirements
- Intel SGX-enabled hardware
- Occlum LibOS runtime
- SGX driver and SDK
- Docker with SGX support

### Configuration
```yaml
SGX_MODE: HW          # Hardware mode
SGX_ENCLAVE_SIZE: 512MB
SGX_THREAD_COUNT: 32
ENABLE_REMOTE_ATTESTATION: true
```

## Future Enhancements

1. **Multi-Enclave Coordination**: Cross-enclave secure channels
2. **Distributed SGX**: Multi-node enclave clusters
3. **Enhanced Analytics**: Privacy-preserving analytics in SGX
4. **Homomorphic Operations**: Computation on encrypted data
5. **Secure Multi-Party Computation**: Collaborative privacy

## Conclusion

The Neo Service Layer now provides comprehensive privacy-preserving computation and storage through Intel SGX integration. All 26 services leverage hardware-based security for sensitive operations, making it suitable for enterprise deployments requiring the highest levels of security and privacy.