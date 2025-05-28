# Neo Service Layer - Storage Service

## Overview

The Storage Service provides secure, encrypted storage for sensitive data using Trusted Execution Environments (TEEs). It enables blockchain applications to store and retrieve data with encryption, compression, and access control, ensuring that only authorized users can access the data.

## Features

- **Secure Storage**: Store data securely within SGX enclaves, ensuring that the data is protected.
- **Data Encryption**: Encrypt data before storage to ensure confidentiality.
- **Data Compression**: Compress data to reduce storage requirements.
- **Data Chunking**: Split large data into chunks for efficient storage and retrieval.
- **Access Control**: Control access to data using blockchain-based access control lists.
- **Multiple Blockchain Support**: Support for both Neo N3 and NeoX blockchains.
- **Transaction Support**: Support for transactional operations to ensure data consistency.
- **Data Versioning**: Maintain multiple versions of data for audit and recovery purposes.

## Architecture

The Storage Service consists of the following components:

### Service Layer

- **IStorageService**: Interface defining the Storage service operations.
- **StorageService**: Implementation of the Storage service, inheriting from EnclaveBlockchainServiceBase.

### Enclave Layer

- **Enclave Implementation**: C++ code running within Occlum LibOS enclaves to securely process and store data.
- **Secure Communication**: Encrypted communication between the service layer and the enclave.

### Storage Providers

- **IPersistentStorageProvider**: Interface for persistent storage providers.
- **OcclumFileStorageProvider**: Storage provider using Occlum's file system.
- **RocksDBStorageProvider**: Storage provider using RocksDB.
- **LevelDBStorageProvider**: Storage provider using LevelDB.

### Blockchain Integration

- **Neo N3 Integration**: Integration with the Neo N3 blockchain.
- **NeoX Integration**: Integration with the NeoX blockchain (EVM-compatible).

## Data Flow

1. **Data Storage**: The client sends data to be stored, along with storage options.
2. **Data Processing**: The service processes the data (encryption, compression, chunking) within the enclave.
3. **Data Storage**: The processed data is stored using the configured storage provider.
4. **Access Control**: Access control information is stored on the blockchain.
5. **Data Retrieval**: The client requests data retrieval, which is verified against access control lists.
6. **Data Processing**: The retrieved data is processed (decryption, decompression) within the enclave.
7. **Data Return**: The processed data is returned to the client.

## API Reference

### IStorageService Interface

```csharp
public interface IStorageService : IEnclaveService, IBlockchainService
{
    Task<StorageResult> StoreAsync(string key, byte[] data, StorageOptions options, BlockchainType blockchainType);
    Task<byte[]> RetrieveAsync(string key, BlockchainType blockchainType);
    Task<bool> DeleteAsync(string key, BlockchainType blockchainType);
    Task<StorageMetadata> GetMetadataAsync(string key, BlockchainType blockchainType);
    Task<bool> UpdateAccessControlAsync(string key, string[] accessControlList, BlockchainType blockchainType);
    Task<bool> BeginTransactionAsync(BlockchainType blockchainType);
    Task<bool> CommitTransactionAsync(BlockchainType blockchainType);
    Task<bool> RollbackTransactionAsync(BlockchainType blockchainType);
}
```

#### Methods

- **StoreAsync**: Stores data securely.
  - Parameters:
    - `key`: The key to store the data under.
    - `data`: The data to store.
    - `options`: Storage options.
    - `blockchainType`: The blockchain type.
  - Returns: Storage result containing metadata about the stored data.

- **RetrieveAsync**: Retrieves stored data.
  - Parameters:
    - `key`: The key to retrieve the data from.
    - `blockchainType`: The blockchain type.
  - Returns: The retrieved data.

- **DeleteAsync**: Deletes stored data.
  - Parameters:
    - `key`: The key to delete the data from.
    - `blockchainType`: The blockchain type.
  - Returns: True if the data was deleted successfully.

- **GetMetadataAsync**: Gets metadata about stored data.
  - Parameters:
    - `key`: The key to get metadata for.
    - `blockchainType`: The blockchain type.
  - Returns: Metadata about the stored data.

- **UpdateAccessControlAsync**: Updates the access control list for stored data.
  - Parameters:
    - `key`: The key to update access control for.
    - `accessControlList`: The new access control list.
    - `blockchainType`: The blockchain type.
  - Returns: True if the access control list was updated successfully.

- **BeginTransactionAsync**: Begins a transaction.
  - Parameters:
    - `blockchainType`: The blockchain type.
  - Returns: True if the transaction was started successfully.

- **CommitTransactionAsync**: Commits a transaction.
  - Parameters:
    - `blockchainType`: The blockchain type.
  - Returns: True if the transaction was committed successfully.

- **RollbackTransactionAsync**: Rolls back a transaction.
  - Parameters:
    - `blockchainType`: The blockchain type.
  - Returns: True if the transaction was rolled back successfully.

### StorageOptions Class

```csharp
public class StorageOptions
{
    public bool Encrypt { get; set; } = true;
    public bool Compress { get; set; } = true;
    public int ChunkSizeBytes { get; set; } = 1024 * 1024; // 1 MB
    public string[] AccessControlList { get; set; } = Array.Empty<string>();
    public bool VersionData { get; set; } = false;
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
    public string CompressionAlgorithm { get; set; } = "GZIP";
}
```

#### Properties

- **Encrypt**: Whether to encrypt the data.
- **Compress**: Whether to compress the data.
- **ChunkSizeBytes**: The size of each chunk in bytes.
- **AccessControlList**: The list of addresses that can access the data.
- **VersionData**: Whether to version the data.
- **EncryptionAlgorithm**: The encryption algorithm to use.
- **CompressionAlgorithm**: The compression algorithm to use.

### StorageResult Class

```csharp
public class StorageResult
{
    public string Key { get; set; }
    public long Size { get; set; }
    public int Chunks { get; set; }
    public bool Encrypted { get; set; }
    public bool Compressed { get; set; }
    public DateTime CreatedAt { get; set; }
    public string[] AccessControlList { get; set; }
}
```

#### Properties

- **Key**: The key the data is stored under.
- **Size**: The size of the data in bytes.
- **Chunks**: The number of chunks the data is split into.
- **Encrypted**: Whether the data is encrypted.
- **Compressed**: Whether the data is compressed.
- **CreatedAt**: When the data was created.
- **AccessControlList**: The list of addresses that can access the data.

### StorageMetadata Class

```csharp
public class StorageMetadata
{
    public string Key { get; set; }
    public long Size { get; set; }
    public int Chunks { get; set; }
    public bool Encrypted { get; set; }
    public bool Compressed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string[] AccessControlList { get; set; }
    public int Version { get; set; }
}
```

#### Properties

- **Key**: The key the data is stored under.
- **Size**: The size of the data in bytes.
- **Chunks**: The number of chunks the data is split into.
- **Encrypted**: Whether the data is encrypted.
- **Compressed**: Whether the data is compressed.
- **CreatedAt**: When the data was created.
- **LastModifiedAt**: When the data was last modified.
- **AccessControlList**: The list of addresses that can access the data.
- **Version**: The version of the data.

## Smart Contract Integration

### Neo N3 Smart Contract

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;

namespace StorageExample
{
    [DisplayName("StorageExample")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "Storage Example")]
    public class StorageExample : SmartContract
    {
        [DisplayName("DataStored")]
        public static event Action<string, UInt160> OnDataStored;

        [DisplayName("DataRetrieved")]
        public static event Action<string, UInt160> OnDataRetrieved;

        public static bool StoreData(string key, byte[] data, string[] accessControlList)
        {
            // Verify that the caller is authorized
            if (!Runtime.CheckWitness(Runtime.ExecutingScriptHash))
            {
                return false;
            }

            // Store the data using the Storage Service
            // This is a simplified example
            Storage.Put(Storage.CurrentContext, $"data:{key}", data);
            Storage.Put(Storage.CurrentContext, $"acl:{key}", StdLib.Serialize(accessControlList));

            // Emit event
            OnDataStored(key, Runtime.ExecutingScriptHash);

            return true;
        }

        public static byte[] RetrieveData(string key)
        {
            // Verify that the caller is authorized
            if (!Runtime.CheckWitness(Runtime.ExecutingScriptHash))
            {
                return new byte[0];
            }

            // Retrieve the data
            byte[] data = Storage.Get(Storage.CurrentContext, $"data:{key});
            
            // Verify access control
            string[] accessControlList = (string[])StdLib.Deserialize(Storage.Get(Storage.CurrentContext, $"acl:{key}"));
            bool hasAccess = false;
            foreach (string address in accessControlList)
            {
                if (address == Runtime.ExecutingScriptHash.ToString())
                {
                    hasAccess = true;
                    break;
                }
            }

            if (!hasAccess)
            {
                return new byte[0];
            }

            // Emit event
            OnDataRetrieved(key, Runtime.ExecutingScriptHash);

            return data;
        }
    }
}
```

### NeoX Smart Contract

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract StorageExample {
    mapping(string => bytes) private data;
    mapping(string => address[]) private accessControlLists;
    
    event DataStored(string key, address storer);
    event DataRetrieved(string key, address retriever);
    
    function storeData(string calldata key, bytes calldata dataToStore, address[] calldata accessControlList) external returns (bool) {
        // Store the data
        data[key] = dataToStore;
        accessControlLists[key] = accessControlList;
        
        // Emit event
        emit DataStored(key, msg.sender);
        
        return true;
    }
    
    function retrieveData(string calldata key) external view returns (bytes memory) {
        // Verify access control
        bool hasAccess = false;
        address[] memory acl = accessControlLists[key];
        for (uint i = 0; i < acl.length; i++) {
            if (acl[i] == msg.sender) {
                hasAccess = true;
                break;
            }
        }
        
        require(hasAccess, "Access denied");
        
        // Return the data
        return data[key];
    }
}
```

## Security Considerations

- **Enclave Security**: The security of the data storage and retrieval depends on the security of the enclave. The enclave must be properly attested and verified.
- **Encryption**: Data must be encrypted before storage to ensure confidentiality.
- **Access Control**: Access control must be enforced to ensure that only authorized users can access the data.
- **Key Management**: Encryption keys must be managed securely within the enclave.
- **Blockchain Integration**: The blockchain integration must be secure to prevent tampering with access control lists.

## Performance Considerations

- **Enclave Initialization**: Enclave initialization can be time-consuming. The service should initialize the enclave once and reuse it for multiple requests.
- **Data Chunking**: Large data should be split into chunks for efficient storage and retrieval.
- **Compression**: Data should be compressed to reduce storage requirements and improve performance.
- **Caching**: Frequently accessed data should be cached to improve performance.
- **Transaction Support**: Transactions should be used to ensure data consistency for operations that modify multiple data items.

## References

- [Neo Service Layer Architecture](../architecture/README.md)
- [Neo Service Layer API](../api/README.md)
- [Neo Service Layer Workflows](../workflows/README.md)
