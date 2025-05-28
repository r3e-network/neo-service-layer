# Neo Service Layer - Oracle Service

## Overview

The Oracle Service provides secure and verifiable external data to smart contracts on the Neo N3 and NeoX blockchains. It leverages Intel SGX with Occlum LibOS enclaves to ensure the integrity and confidentiality of data processing. This service includes comprehensive price feed capabilities, making it a complete oracle solution for both general data and financial market data.

## Features

- **Confidential Data Retrieval**: Securely retrieves data from external sources within enclaves
- **Price Feed Aggregation**: Decentralized price and market data aggregation from multiple sources
- **Cryptographic Verification**: Provides cryptographic proofs of data authenticity
- **Multi-Blockchain Support**: Supports both Neo N3 and NeoX blockchains
- **Data Source Management**: Allows registration and management of trusted data sources
- **Real-Time Price Feeds**: Continuous price updates for cryptocurrencies and traditional assets
- **Request Batching**: Batch multiple data requests for efficiency
- **Custom Data Transformations**: Apply transformations to raw data before delivery
- **Data Subscriptions**: Subscribe to data feeds for automatic updates
- **High Availability**: Ensures continuous data availability with redundant sources

## Architecture

The Oracle Service consists of the following components:

### Service Layer

- **IOracleService**: Interface defining the Oracle service operations.
- **OracleService**: Implementation of the Oracle service, inheriting from EnclaveBlockchainServiceBase.

### Enclave Layer

- **Enclave Implementation**: C++ code running within Intel SGX with Occlum LibOS enclaves to securely process data.
- **Secure Communication**: Encrypted communication between the service layer and the enclave.

### Blockchain Integration

- **Neo N3 Integration**: Integration with the Neo N3 blockchain.
- **NeoX Integration**: Integration with the NeoX blockchain (EVM-compatible).

## Data Flow

1. **Request Initiation**: A smart contract requests external data through the Oracle service.
2. **Data Retrieval**: The Oracle service securely retrieves data from trusted sources.
3. **Enclave Processing**: Data is processed within Occlum LibOS enclaves to ensure confidentiality.
4. **Verification**: The processed data is cryptographically verified.
5. **Response**: Verified data is provided to the smart contract with cryptographic proofs.

## API Reference

### IOracleService Interface

```csharp
public interface IOracleService : IEnclaveService, IBlockchainService
{
    // General Oracle Methods
    Task<OracleResult> GetDataAsync(OracleRequest request, BlockchainType blockchainType);
    Task<bool> RegisterDataSourceAsync(DataSourceRegistration registration, BlockchainType blockchainType);
    Task<bool> RemoveDataSourceAsync(string dataSourceId, BlockchainType blockchainType);
    Task<IEnumerable<DataSource>> GetDataSourcesAsync(BlockchainType blockchainType);
    Task<string> CreateSubscriptionAsync(SubscriptionRequest request, BlockchainType blockchainType);
    Task<bool> CancelSubscriptionAsync(string subscriptionId, BlockchainType blockchainType);

    // Price Feed Methods
    Task<PriceFeed> GetPriceFeedAsync(string symbol, BlockchainType blockchainType);
    Task<IEnumerable<PriceFeed>> GetPriceFeedsAsync(string[] symbols, BlockchainType blockchainType);
    Task<bool> CreateCustomPriceFeedAsync(CustomPriceFeedRequest request, BlockchainType blockchainType);
    Task<IEnumerable<string>> GetAvailableSymbolsAsync(BlockchainType blockchainType);
    Task<PriceFeedMetadata> GetPriceFeedMetadataAsync(string symbol, BlockchainType blockchainType);

    // Batch Operations
    Task<BatchOracleResult> GetBatchDataAsync(BatchOracleRequest request, BlockchainType blockchainType);
    Task<bool> VerifyDataAsync(string dataId, string proof, BlockchainType blockchainType);
}
```

#### Methods

- **GetDataAsync**: Gets data from an external source.
  - Parameters:
    - `dataSource`: The data source URL.
    - `dataPath`: The path to the data within the source.
    - `blockchainType`: The blockchain type.
  - Returns: The data from the external source.

- **RegisterDataSourceAsync**: Registers a new data source.
  - Parameters:
    - `dataSource`: The data source URL.
    - `description`: The description of the data source.
    - `blockchainType`: The blockchain type.
  - Returns: True if the data source was registered successfully, false otherwise.

- **RemoveDataSourceAsync**: Removes a data source.
  - Parameters:
    - `dataSource`: The data source URL.
    - `blockchainType`: The blockchain type.
  - Returns: True if the data source was removed successfully, false otherwise.

- **GetDataSourcesAsync**: Gets all registered data sources.
  - Parameters:
    - `blockchainType`: The blockchain type.
  - Returns: All registered data sources.

### DataSource Class

```csharp
public class DataSource
{
    public string Url { get; set; }
    public string Description { get; set; }
    public BlockchainType BlockchainType { get; set; }
}
```

#### Properties

- **Url**: The URL of the data source.
- **Description**: The description of the data source.
- **BlockchainType**: The blockchain type.

## Smart Contract Integration

### Neo N3

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace OracleConsumer
{
    [Contract("0x0123456789abcdef0123456789abcdef")]
    public class OracleConsumer : SmartContract
    {
        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 OracleContractAddress = default;

        public static object GetExternalData(string url, string path)
        {
            var result = (string)Contract.Call(OracleContractAddress, "getOracleData", CallFlags.All, new object[] { url, path });
            return result;
        }
    }
}
```

### NeoX (EVM)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IOracleConsumer {
    function getOracleData(string calldata url, string calldata path) external view returns (string memory);
}

contract OracleConsumer {
    address private oracleContract;

    constructor(address _oracleContract) {
        oracleContract = _oracleContract;
    }

    function getExternalData(string calldata url, string calldata path) external view returns (string memory) {
        return IOracleConsumer(oracleContract).getOracleData(url, path);
    }
}
```

## Security Considerations

- **Enclave Security**: All sensitive data processing occurs within secure Occlum LibOS enclaves.
- **Data Integrity**: Cryptographic verification ensures data integrity.
- **Source Validation**: Only registered and trusted data sources are used.
- **Confidentiality**: Data is encrypted during transmission and processing.

## Deployment

The Oracle Service is deployed as part of the Neo Service Layer, with the following components:

- **Service Layer**: Deployed as a .NET service.
- **Enclave Layer**: Deployed within Occlum LibOS enclaves.
- **Smart Contracts**: Deployed on the Neo N3 and NeoX blockchains.

## Conclusion

The Oracle Service provides a secure and reliable way to bring external data into smart contracts on the Neo N3 and NeoX blockchains. By leveraging Confidential Computing with Occlum LibOS enclaves, it ensures the integrity and confidentiality of data processing, enabling new classes of decentralized applications that require external data.
