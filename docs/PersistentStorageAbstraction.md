# Persistent Storage Abstraction

This document provides a comprehensive guide to the persistent storage abstraction in the Neo Confidential Serverless Layer (NCSL).

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Storage Providers](#storage-providers)
4. [Transactions](#transactions)
5. [Encryption and Compression](#encryption-and-compression)
6. [Usage Examples](#usage-examples)
7. [Best Practices](#best-practices)
8. [Performance Considerations](#performance-considerations)

## Overview

The persistent storage abstraction provides a robust, fault-tolerant storage solution for the Neo Confidential Serverless Layer. It is designed to work with OpenEnclave and Occlum LibOS, providing durability, security, and flexibility.

Key features include:
- Multiple storage backends (file-based, RocksDB, LevelDB)
- Chunking for large data
- Transparent encryption and compression
- Transaction support
- Metadata management
- Fault recovery mechanisms

## Architecture

The persistent storage abstraction consists of the following components:

### Core Components

- **IPersistentStorageProvider**: Interface that defines the contract for all storage providers
- **BasePersistentStorageProvider**: Abstract base class that implements common functionality
- **StorageMetadata**: Class that stores metadata about each stored item
- **PersistentStorageFactory**: Factory for creating storage providers
- **PersistentStorageManager**: Manager for handling multiple storage instances
- **StorageTransaction**: Class for atomic operations
- **StorageUtility**: Utility class for encryption and compression

### Data Flow

1. Application requests data storage/retrieval through a storage provider
2. Provider handles chunking, encryption, and compression as needed
3. Data is stored in the underlying storage system
4. Metadata is maintained for each stored item
5. Transactions ensure atomicity of operations
6. Recovery mechanisms handle failures

## Storage Providers

The abstraction includes several storage providers, each optimized for different use cases:

### OcclumFileStorageProvider

A file-based storage provider optimized for Occlum LibOS. It uses a directory structure to organize data and metadata, with journaling for crash recovery.

**Key features:**
- Simple file-based storage
- Journaling for crash recovery
- Directory structure for organization
- Optimized for Occlum LibOS

**Configuration options:**
- `StorageDirectory`: Directory where files are stored
- `MaxFileSizeBytes`: Maximum size of a file
- `EnableJournaling`: Whether to enable journaling
- `EnableAutoCompaction`: Whether to enable automatic compaction

### RocksDBStorageProvider

A storage provider using RocksDB, a high-performance embedded database. It provides excellent performance for high-throughput workloads.

**Key features:**
- High performance
- Column families for organization
- Optimized for write-heavy workloads
- Automatic compaction

**Configuration options:**
- `StorageDirectory`: Directory where RocksDB files are stored
- `WriteBufferSize`: Size of the write buffer
- `BlockCacheSize`: Size of the block cache
- `EnableCompression`: Whether to enable compression

### LevelDBStorageProvider

A storage provider using LevelDB (via RocksDB's LevelDB compatibility mode). It provides a simpler alternative to RocksDB with good performance.

**Key features:**
- Simple key-value storage
- Good performance
- Smaller footprint than RocksDB
- Automatic compaction

**Configuration options:**
- `StorageDirectory`: Directory where LevelDB files are stored
- `WriteBufferSize`: Size of the write buffer
- `BlockCacheSize`: Size of the block cache
- `EnableCompression`: Whether to enable compression

## Transactions

The storage abstraction includes transaction support for atomic operations. Transactions ensure that either all operations succeed or none do, maintaining data consistency.

**Key features:**
- Atomic operations
- Automatic rollback on failure
- Support for multiple operations in a single transaction
- Isolation between transactions

**Usage:**
```csharp
// Create a transaction
using (var transaction = new StorageTransaction(logger, provider))
{
    // Add operations to the transaction
    await transaction.WriteAsync("key1", data1);
    await transaction.DeleteAsync("key2");

    // Commit the transaction
    await transaction.CommitAsync();
}
```

## Encryption and Compression

The storage abstraction includes support for encryption and compression to protect data and reduce storage requirements.

### Encryption

Data is encrypted using OpenEnclave's sealing functionality, which provides hardware-backed encryption tied to the enclave identity.

**Key features:**
- Hardware-backed encryption
- Tied to enclave identity
- Transparent to application code

### Compression

Data can be compressed using GZip compression to reduce storage requirements.

**Key features:**
- GZip compression
- Configurable compression level
- Transparent to application code

## Usage Examples

### Basic Usage

```csharp
// Create a storage provider
var provider = new OcclumFileStorageProvider(logger, new OcclumFileStorageOptions
{
    StorageDirectory = "storage"
});

// Initialize the provider
await provider.InitializeAsync();

// Store data
await provider.WriteAsync("key", data);

// Retrieve data
byte[] retrievedData = await provider.ReadAsync("key");

// Delete data
await provider.DeleteAsync("key");
```

### Using the Storage Manager

```csharp
// Create a storage factory
var factory = new PersistentStorageFactory(loggerFactory);

// Create a storage manager
var manager = new PersistentStorageManager(logger, factory);

// Create a storage provider
var provider = await manager.CreateProviderAsync("main", PersistentStorageProviderType.RocksDB, new RocksDBStorageOptions
{
    StorageDirectory = "rocksdb_storage"
});

// Use the provider
await provider.WriteAsync("key", data);
```

### Using Transactions

```csharp
// Create a transaction
using (var transaction = new StorageTransaction(logger, provider))
{
    // Add operations to the transaction
    await transaction.WriteAsync("key1", data1);
    await transaction.WriteAsync("key2", data2);
    await transaction.DeleteAsync("key3");

    // Commit the transaction
    await transaction.CommitAsync();
}
```

### Using Encryption and Compression

```csharp
// Create a storage utility
var utility = new StorageUtility(logger, enclaveInterface);

// Encrypt and compress data
byte[] encryptedCompressedData = utility.EncryptAndCompress(data);

// Store the encrypted and compressed data
await provider.WriteAsync("key", encryptedCompressedData);

// Retrieve and decrypt/decompress the data
byte[] retrievedData = await provider.ReadAsync("key");
byte[] decryptedDecompressedData = utility.DecryptAndDecompress(retrievedData);
```

## Best Practices

1. **Choose the Right Provider**: Select the storage provider that best matches your workload characteristics.
2. **Use Transactions for Atomic Operations**: Use transactions when multiple operations need to be atomic.
3. **Enable Encryption for Sensitive Data**: Always encrypt sensitive data using the storage utility.
4. **Use Chunking for Large Data**: Break large data into chunks to avoid memory pressure.
5. **Regularly Compact Storage**: Call `CompactAsync()` periodically to reclaim space.
6. **Handle Exceptions Properly**: Catch and handle storage exceptions to ensure data consistency.
7. **Dispose Providers When Done**: Always dispose storage providers to release resources.
8. **Use Metadata for Organization**: Store metadata to help organize and search data.
9. **Monitor Storage Usage**: Keep track of storage usage to avoid running out of space.
10. **Test Recovery Scenarios**: Test failure scenarios to ensure recovery mechanisms work.

## Performance Considerations

1. **Caching**: Use in-memory caching for frequently accessed data.
2. **Batch Operations**: Batch multiple operations together for better performance.
3. **Compression Trade-offs**: Compression reduces storage requirements but increases CPU usage.
4. **Provider Selection**: Different providers have different performance characteristics:
   - OcclumFileStorageProvider: Good for simple workloads with moderate performance requirements
   - RocksDBStorageProvider: Excellent for high-throughput workloads with many writes
   - LevelDBStorageProvider: Good balance of performance and simplicity
5. **Chunking**: Choose appropriate chunk sizes based on your workload.
6. **Transaction Overhead**: Transactions add overhead, so use them judiciously.
7. **Encryption Overhead**: Encryption adds CPU overhead, so use it only for sensitive data.
8. **Compaction**: Regular compaction improves read performance but temporarily increases I/O.
9. **Flush Frequency**: Adjust flush frequency based on durability requirements.
10. **Concurrency**: The storage providers are thread-safe, but high concurrency may lead to contention.
