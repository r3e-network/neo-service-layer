# Neo Service Layer - Proof of Reserve Service

## Overview

The Proof of Reserve Service provides cryptographic verification of asset backing for tokenized assets, stablecoins, and other financial instruments on the Neo N3 and NeoX blockchains. It leverages Intel SGX with Occlum LibOS enclaves to ensure the integrity and confidentiality of reserve verification processes, similar to Chainlink Proof of Reserve but optimized for the Neo ecosystem.

## Features

- **Asset Verification**: Verify that tokenized assets are fully backed by reserves
- **Real-Time Monitoring**: Continuous monitoring of reserve levels and ratios
- **Multi-Asset Support**: Support for various asset types including fiat, crypto, and commodities
- **Cryptographic Proofs**: Generate verifiable proofs of reserve adequacy
- **Automated Alerts**: Automatic alerts when reserves fall below thresholds
- **Audit Trail**: Complete audit trail of all verification activities
- **Multi-Blockchain Support**: Supports both Neo N3 and NeoX blockchains

## Architecture

The Proof of Reserve Service consists of the following components:

### Service Layer

- **IProofOfReserveService**: Interface defining the Proof of Reserve service operations
- **ProofOfReserveService**: Implementation of the service, inheriting from EnclaveBlockchainServiceBase

### Enclave Layer

- **Reserve Verifier**: C++ code running within Intel SGX with Occlum LibOS enclaves to securely verify reserves
- **Cryptographic Prover**: Generates cryptographic proofs of reserve adequacy
- **Data Aggregator**: Aggregates reserve data from multiple sources securely

### Blockchain Integration

- **Neo N3 Integration**: Integration with Neo N3 blockchain for proof publication
- **NeoX Integration**: Integration with NeoX blockchain (EVM-compatible) for proof publication

## Verification Process

1. **Data Collection**: Securely collect reserve data from custodians and exchanges
2. **Enclave Processing**: Process and verify data within Intel SGX enclaves
3. **Proof Generation**: Generate cryptographic proofs of reserve adequacy
4. **Blockchain Publication**: Publish proofs to the blockchain for transparency
5. **Continuous Monitoring**: Monitor reserves in real-time for changes

## API Reference

### IProofOfReserveService Interface

```csharp
public interface IProofOfReserveService : IEnclaveService, IBlockchainService
{
    Task<string> RegisterAssetAsync(AssetRegistration registration, BlockchainType blockchainType);
    Task<bool> UpdateReserveDataAsync(string assetId, ReserveData data, BlockchainType blockchainType);
    Task<ProofOfReserve> GenerateProofAsync(string assetId, BlockchainType blockchainType);
    Task<bool> VerifyProofAsync(string proofId, BlockchainType blockchainType);
    Task<ReserveStatus> GetReserveStatusAsync(string assetId, BlockchainType blockchainType);
    Task<IEnumerable<AssetInfo>> GetRegisteredAssetsAsync(BlockchainType blockchainType);
    Task<ReserveHistory> GetReserveHistoryAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType);
    Task<bool> SetAlertThresholdAsync(string assetId, decimal threshold, BlockchainType blockchainType);
    Task<IEnumerable<ReserveAlert>> GetActiveAlertsAsync(BlockchainType blockchainType);
    Task<AuditReport> GenerateAuditReportAsync(string assetId, DateTime from, DateTime to, BlockchainType blockchainType);
}
```

#### Methods

- **RegisterAssetAsync**: Registers a new asset for reserve verification
  - Parameters:
    - `registration`: Asset registration details
    - `blockchainType`: The blockchain type
  - Returns: Asset ID for tracking

- **UpdateReserveDataAsync**: Updates reserve data for an asset
  - Parameters:
    - `assetId`: The ID of the asset
    - `data`: Updated reserve data
    - `blockchainType`: The blockchain type
  - Returns: True if the update was successful

- **GenerateProofAsync**: Generates a proof of reserve for an asset
  - Parameters:
    - `assetId`: The ID of the asset
    - `blockchainType`: The blockchain type
  - Returns: Proof of reserve data

- **VerifyProofAsync**: Verifies a proof of reserve
  - Parameters:
    - `proofId`: The ID of the proof to verify
    - `blockchainType`: The blockchain type
  - Returns: True if the proof is valid

- **GetReserveStatusAsync**: Gets the current reserve status for an asset
  - Parameters:
    - `assetId`: The ID of the asset
    - `blockchainType`: The blockchain type
  - Returns: Current reserve status

- **GetRegisteredAssetsAsync**: Gets all registered assets
  - Parameters:
    - `blockchainType`: The blockchain type
  - Returns: Collection of registered assets

- **GetReserveHistoryAsync**: Gets reserve history for an asset
  - Parameters:
    - `assetId`: The ID of the asset
    - `from`: Start date for history
    - `to`: End date for history
    - `blockchainType`: The blockchain type
  - Returns: Reserve history data

- **SetAlertThresholdAsync**: Sets an alert threshold for an asset
  - Parameters:
    - `assetId`: The ID of the asset
    - `threshold`: Alert threshold percentage
    - `blockchainType`: The blockchain type
  - Returns: True if the threshold was set successfully

- **GetActiveAlertsAsync**: Gets all active reserve alerts
  - Parameters:
    - `blockchainType`: The blockchain type
  - Returns: Collection of active alerts

- **GenerateAuditReportAsync**: Generates an audit report for an asset
  - Parameters:
    - `assetId`: The ID of the asset
    - `from`: Start date for report
    - `to`: End date for report
    - `blockchainType`: The blockchain type
  - Returns: Audit report data

### Data Models

#### AssetRegistration Class

```csharp
public class AssetRegistration
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string TokenAddress { get; set; }
    public AssetType Type { get; set; }
    public string Issuer { get; set; }
    public string[] CustodianAddresses { get; set; }
    public ReserveConfig ReserveConfig { get; set; }
    public string Description { get; set; }
}
```

#### ReserveData Class

```csharp
public class ReserveData
{
    public string AssetId { get; set; }
    public decimal TotalSupply { get; set; }
    public ReserveHolding[] Holdings { get; set; }
    public DateTime Timestamp { get; set; }
    public string DataSource { get; set; }
    public string Signature { get; set; }
}
```

#### ReserveHolding Class

```csharp
public class ReserveHolding
{
    public string AssetType { get; set; }
    public decimal Amount { get; set; }
    public string Address { get; set; }
    public string Custodian { get; set; }
    public decimal Value { get; set; }
    public string Currency { get; set; }
}
```

#### ProofOfReserve Class

```csharp
public class ProofOfReserve
{
    public string ProofId { get; set; }
    public string AssetId { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal TotalReserves { get; set; }
    public decimal ReserveRatio { get; set; }
    public bool IsFullyBacked { get; set; }
    public DateTime Timestamp { get; set; }
    public string MerkleRoot { get; set; }
    public string Signature { get; set; }
    public string BlockchainProof { get; set; }
}
```

#### ReserveStatus Class

```csharp
public class ReserveStatus
{
    public string AssetId { get; set; }
    public decimal CurrentSupply { get; set; }
    public decimal CurrentReserves { get; set; }
    public decimal ReserveRatio { get; set; }
    public ReserveHealth Health { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime LastProofGenerated { get; set; }
    public bool AlertsActive { get; set; }
}
```

#### ReserveAlert Class

```csharp
public class ReserveAlert
{
    public string AlertId { get; set; }
    public string AssetId { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Acknowledged { get; set; }
    public decimal CurrentRatio { get; set; }
    public decimal ThresholdRatio { get; set; }
}
```

#### Enums

```csharp
public enum AssetType
{
    Stablecoin,
    TokenizedAsset,
    WrappedToken,
    SyntheticAsset,
    CommodityToken
}

public enum ReserveHealth
{
    Healthy,
    Warning,
    Critical,
    Undercollateralized
}

public enum AlertType
{
    LowReserves,
    RatioThreshold,
    DataStale,
    VerificationFailed,
    CustodianIssue
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical,
    Emergency
}
```

## Smart Contract Integration

### Neo N3

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace ProofOfReserveConsumer
{
    [Contract("0x0123456789abcdef0123456789abcdef")]
    public class ProofOfReserveConsumer : SmartContract
    {
        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 ProofOfReserveContractAddress = default;

        // Get reserve status for an asset
        public static object GetReserveStatus(string assetId)
        {
            var result = Contract.Call(ProofOfReserveContractAddress, "getReserveStatus", CallFlags.All, new object[] { assetId });
            return result;
        }

        // Verify if an asset is fully backed
        public static bool IsAssetFullyBacked(string assetId)
        {
            var status = Contract.Call(ProofOfReserveContractAddress, "getReserveStatus", CallFlags.All, new object[] { assetId });
            // Parse status and check if fully backed
            return true; // Simplified for example
        }

        // Get the latest proof for an asset
        public static object GetLatestProof(string assetId)
        {
            var result = Contract.Call(ProofOfReserveContractAddress, "getLatestProof", CallFlags.All, new object[] { assetId });
            return result;
        }

        // Register for reserve alerts
        public static bool RegisterForAlerts(string assetId, decimal threshold)
        {
            var result = (bool)Contract.Call(ProofOfReserveContractAddress, "setAlertThreshold", CallFlags.All, 
                new object[] { assetId, threshold });
            return result;
        }
    }
}
```

### NeoX (EVM)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IProofOfReserveConsumer {
    function getReserveStatus(string calldata assetId) external view returns (
        uint256 currentSupply,
        uint256 currentReserves,
        uint256 reserveRatio,
        bool isFullyBacked
    );
    function getLatestProof(string calldata assetId) external view returns (
        string memory proofId,
        uint256 timestamp,
        string memory merkleRoot,
        string memory signature
    );
    function setAlertThreshold(string calldata assetId, uint256 threshold) external returns (bool);
}

contract ProofOfReserveConsumer {
    address private proofOfReserveContract;
    
    event ReserveAlert(string assetId, uint256 currentRatio, uint256 threshold);
    
    constructor(address _proofOfReserveContract) {
        proofOfReserveContract = _proofOfReserveContract;
    }
    
    // Get reserve status for an asset
    function getReserveStatus(string calldata assetId) external view returns (
        uint256 currentSupply,
        uint256 currentReserves,
        uint256 reserveRatio,
        bool isFullyBacked
    ) {
        return IProofOfReserveConsumer(proofOfReserveContract).getReserveStatus(assetId);
    }
    
    // Verify if an asset is fully backed
    function isAssetFullyBacked(string calldata assetId) external view returns (bool) {
        (, , , bool isFullyBacked) = IProofOfReserveConsumer(proofOfReserveContract).getReserveStatus(assetId);
        return isFullyBacked;
    }
    
    // Get the latest proof for an asset
    function getLatestProof(string calldata assetId) external view returns (
        string memory proofId,
        uint256 timestamp,
        string memory merkleRoot,
        string memory signature
    ) {
        return IProofOfReserveConsumer(proofOfReserveContract).getLatestProof(assetId);
    }
    
    // Register for reserve alerts
    function registerForAlerts(string calldata assetId, uint256 threshold) external returns (bool) {
        return IProofOfReserveConsumer(proofOfReserveContract).setAlertThreshold(assetId, threshold);
    }
}
```

## Supported Asset Types

### Stablecoins
- **Fiat-Backed**: USD, EUR, GBP backed stablecoins
- **Crypto-Backed**: Cryptocurrency collateralized stablecoins
- **Algorithmic**: Algorithm-based stablecoins with reserve mechanisms

### Tokenized Assets
- **Real Estate**: Property-backed tokens
- **Commodities**: Gold, silver, oil-backed tokens
- **Securities**: Stock and bond tokens
- **Art and Collectibles**: NFT-backed tokens

### Wrapped Tokens
- **Cross-Chain**: Wrapped Bitcoin, Ethereum on Neo
- **Layer 2**: Tokens bridged from other networks
- **Synthetic**: Synthetic representations of assets

## Use Cases

### Financial Services
- **Stablecoin Issuers**: Prove backing for issued stablecoins
- **Asset Managers**: Verify backing for tokenized funds
- **Banks**: Demonstrate reserve adequacy for digital currencies

### DeFi Protocols
- **Lending Platforms**: Verify collateral backing
- **Yield Farming**: Prove reserve adequacy for yield tokens
- **Insurance**: Verify backing for insurance pools

### Regulatory Compliance
- **Audit Requirements**: Meet regulatory audit requirements
- **Transparency**: Provide transparency to regulators and users
- **Risk Management**: Monitor and manage reserve risks

## Security Considerations

- **Enclave Security**: All reserve verification occurs within secure Intel SGX with Occlum LibOS enclaves
- **Data Integrity**: Cryptographic verification of all reserve data
- **Privacy Protection**: Sensitive custodian data protected within enclaves
- **Audit Trail**: Complete audit trail of all verification activities
- **Multi-Source Verification**: Verification from multiple independent sources

## Deployment

The Proof of Reserve Service is deployed as part of the Neo Service Layer:

- **Service Layer**: Deployed as a .NET service with high availability
- **Enclave Layer**: Deployed within Intel SGX with Occlum LibOS enclaves
- **Data Sources**: Integration with custodians, exchanges, and data providers
- **Smart Contracts**: Deployed on Neo N3 and NeoX blockchains

## Conclusion

The Proof of Reserve Service provides transparent and verifiable proof of asset backing for the Neo ecosystem. By leveraging Intel SGX with Occlum LibOS enclaves and providing comprehensive verification capabilities, it enables trust and transparency in tokenized assets, stablecoins, and other financial instruments on both Neo N3 and NeoX blockchains.
