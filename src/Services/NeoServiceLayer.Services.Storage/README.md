# Storage Service

## Overview

The Storage Service is a secure, enclave-based service for storing and retrieving data. It provides a framework for storing data with encryption, compression, and access control, ensuring that the data is protected and only accessible to authorized parties.

## Features

- **Secure Storage**: Store data within a secure enclave, ensuring that the data is protected.
- **Encryption**: Encrypt data before storage, ensuring that even if the storage is compromised, the data remains secure.
- **Compression**: Compress data to reduce storage requirements.
- **Chunking**: Split large data into chunks for efficient storage and retrieval.
- **Access Control**: Control access to data through access control lists.
- **Metadata**: Maintain metadata about stored data, including creation date, last accessed date, and custom attributes.
- **Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Storage Service is built on the Neo Service Layer framework and uses the Trusted Execution Environment (TEE) to provide secure storage. The service consists of the following components:

- **IStorageService**: The interface that defines the operations supported by the service.
- **StorageService**: The implementation of the service that uses the enclave to perform storage operations.
- **EnclaveManager**: The component that manages the communication with the enclave.

## Usage

### Service Registration

```csharp
// Register the service
services.AddNeoService<IStorageService, StorageService>();

// Register the service with the service registry
serviceProvider.RegisterAllNeoServices();
```

### Storing Data

```csharp
// Store data
byte[] data = Encoding.UTF8.GetBytes("Hello, world!");
var options = new StorageOptions
{
    Encrypt = true,
    Compress = true,
    ChunkSizeBytes = 1024 * 1024, // 1 MB
    AccessControlList = new List<string> { "user1", "user2" }
};

var metadata = await storageService.StoreDataAsync(
    "my-data",
    data,
    options,
    BlockchainType.NeoN3);
```

### Retrieving Data

```csharp
// Retrieve data
byte[] retrievedData = await storageService.RetrieveDataAsync(
    "my-data",
    BlockchainType.NeoN3);
```

### Getting Metadata

```csharp
// Get metadata
var metadata = await storageService.GetMetadataAsync(
    "my-data",
    BlockchainType.NeoN3);
```

### Listing Keys

```csharp
// List keys with a prefix
var keys = await storageService.ListKeysAsync(
    "my-",
    0,
    10,
    BlockchainType.NeoN3);
```

### Deleting Data

```csharp
// Delete data
bool success = await storageService.DeleteDataAsync(
    "my-data",
    BlockchainType.NeoN3);
```

## Security Considerations

- All data is encrypted before storage.
- Encryption keys are managed by the Key Management Service.
- Access to data is controlled through access control lists.
- All operations are logged for audit purposes.

## API Reference

### StoreDataAsync

Stores data.

```csharp
Task<StorageMetadata> StoreDataAsync(
    string key,
    byte[] data,
    StorageOptions options,
    BlockchainType blockchainType);
```

### RetrieveDataAsync

Retrieves data.

```csharp
Task<byte[]> RetrieveDataAsync(
    string key,
    BlockchainType blockchainType);
```

### DeleteDataAsync

Deletes data.

```csharp
Task<bool> DeleteDataAsync(
    string key,
    BlockchainType blockchainType);
```

### GetMetadataAsync

Gets storage metadata.

```csharp
Task<StorageMetadata> GetMetadataAsync(
    string key,
    BlockchainType blockchainType);
```

### ListKeysAsync

Lists storage keys.

```csharp
Task<IEnumerable<StorageMetadata>> ListKeysAsync(
    string prefix,
    int skip,
    int take,
    BlockchainType blockchainType);
```

### UpdateMetadataAsync

Updates storage metadata.

```csharp
Task<bool> UpdateMetadataAsync(
    string key,
    StorageMetadata metadata,
    BlockchainType blockchainType);
```
