# Persistent Storage in Neo Service Layer

This document describes the persistent storage system in the Neo Service Layer, which provides a secure and reliable way to store data within the Trusted Execution Environment (TEE).

## Overview

The persistent storage system in Neo Service Layer is designed to provide a secure, reliable, and efficient way to store data within the Trusted Execution Environment (TEE). It uses Occlum's file system capabilities to store data persistently, with additional features like encryption, compression, and transaction support.

## Architecture

The persistent storage system consists of the following components:

1. **IPersistentStorageProvider**: Interface for storage providers that defines the core storage operations.
2. **OcclumFileStorageProvider**: Implementation of the storage provider that uses Occlum's file system.
3. **IPersistentStorageService**: High-level service that provides additional functionality on top of the storage provider.
4. **PersistentStorageService**: Implementation of the storage service.

### Storage Provider Interface

The `IPersistentStorageProvider` interface defines the following operations:

- **Read**: Read data from storage.
- **Write**: Write data to storage.
- **Delete**: Delete data from storage.
- **Exists**: Check if a key exists in storage.
- **ListKeys**: List all keys in storage with a specified prefix.
- **GetSize**: Get the size of the data for a specified key.
- **BeginTransaction**: Begin a transaction.
- **OpenReadStream**: Open a stream for reading from storage.
- **OpenWriteStream**: Open a stream for writing to storage.
- **Flush**: Flush any pending changes to storage.

### Storage Service Interface

The `IPersistentStorageService` interface extends the storage provider interface with additional functionality:

- **ReadJson**: Read data from storage and deserialize it to a specified type.
- **WriteJson**: Serialize an object to JSON and write it to storage.

## Features

### Encryption

The persistent storage system supports encryption of data using AES-256. Encryption is enabled by default and can be configured in the `appsettings.json` file.

```json
"Storage": {
  "EnableEncryption": true,
  "EncryptionKey": "YourEncryptionKey"
}
```

### Compression

The persistent storage system supports compression of data using GZip. Compression is enabled by default and can be configured in the `appsettings.json` file.

```json
"Storage": {
  "EnableCompression": true,
  "CompressionLevel": 6
}
```

### Transactions

The persistent storage system supports transactions, allowing multiple operations to be performed atomically. Transactions are implemented using the `IStorageTransaction` interface, which provides the following operations:

- **Read**: Read data from storage within the transaction.
- **Write**: Write data to storage within the transaction.
- **Delete**: Delete data from storage within the transaction.
- **Commit**: Commit the transaction.
- **Rollback**: Roll back the transaction.

### Caching

The persistent storage system supports caching of data in memory to improve performance. Caching is enabled by default and can be configured in the `appsettings.json` file.

```json
"Storage": {
  "EnableCaching": true,
  "CacheSizeBytes": 52428800
}
```

### Auto-Flush

The persistent storage system supports automatic flushing of data to disk at regular intervals. Auto-flush is enabled by default and can be configured in the `appsettings.json` file.

```json
"Storage": {
  "EnableAutoFlush": true,
  "AutoFlushIntervalMs": 5000
}
```

## Configuration

The persistent storage system can be configured in the `appsettings.json` file:

```json
"Tee": {
  "Storage": {
    "Provider": "OcclumFileStorage",
    "StoragePath": "/occlum_instance/data",
    "EnableEncryption": true,
    "EncryptionKey": "YourEncryptionKey",
    "EnableCompression": true,
    "CompressionLevel": 6,
    "MaxChunkSize": 4194304,
    "EnableCaching": true,
    "CacheSizeBytes": 52428800,
    "EnableAutoFlush": true,
    "AutoFlushIntervalMs": 5000
  }
}
```

## Usage

### Basic Usage

```csharp
// Get the storage service
var storageService = serviceProvider.GetRequiredService<IPersistentStorageService>();

// Initialize the storage service
var options = new PersistentStorageOptions
{
    StoragePath = "/occlum_instance/data",
    EnableEncryption = true,
    EncryptionKey = Encoding.UTF8.GetBytes("YourEncryptionKey"),
    EnableCompression = true,
    CompressionLevel = 6,
    CreateIfNotExists = true
};
await storageService.InitializeAsync(options);

// Write data
await storageService.WriteAsync("key", Encoding.UTF8.GetBytes("value"));

// Read data
var data = await storageService.ReadAsync("key");
var value = Encoding.UTF8.GetString(data);

// Delete data
await storageService.DeleteAsync("key");
```

### JSON Serialization

```csharp
// Write an object as JSON
var user = new User { Id = 1, Name = "John Doe" };
await storageService.WriteJsonAsync("user:1", user);

// Read an object from JSON
var user = await storageService.ReadJsonAsync<User>("user:1");
```

### Transactions

```csharp
// Begin a transaction
using (var transaction = storageService.BeginTransaction())
{
    // Perform operations within the transaction
    await transaction.WriteAsync("key1", Encoding.UTF8.GetBytes("value1"));
    await transaction.WriteAsync("key2", Encoding.UTF8.GetBytes("value2"));

    // Commit the transaction
    await transaction.CommitAsync();
}
```

## Security Considerations

### Encryption

All data stored in the persistent storage system is encrypted using AES-256. The encryption key is stored in the `appsettings.json` file and should be kept secure.

### Occlum Security

The persistent storage system uses Occlum's file system, which provides additional security through the Trusted Execution Environment (TEE). Occlum ensures that data is protected from unauthorized access, even if the host system is compromised.

## Performance Considerations

### Compression

Compression can improve storage efficiency but may impact performance. The compression level can be configured to balance between compression ratio and performance.

### Caching

Caching can improve read performance by keeping frequently accessed data in memory. The cache size can be configured based on the available memory and the expected data size.

### Chunking

Large data is automatically chunked into smaller pieces to improve performance and reduce memory usage. The maximum chunk size can be configured in the `appsettings.json` file.

## Troubleshooting

### Common Issues

1. **Storage initialization fails**: Ensure that the storage path exists and is accessible by the application.
2. **Encryption key is invalid**: Ensure that the encryption key is at least 32 bytes long.
3. **Transaction fails**: Ensure that the transaction is committed or rolled back before it is disposed.

### Logging

The persistent storage system logs all operations at the debug level. Enable debug logging to see detailed information about storage operations.

```json
"Logging": {
  "LogLevel": {
    "NeoServiceLayer.Infrastructure.Services.PersistentStorageService": "Debug"
  }
}
```

## References

- [Occlum Documentation](https://occlum.io/occlum/latest/index.html)
- [AES Encryption](https://en.wikipedia.org/wiki/Advanced_Encryption_Standard)
- [GZip Compression](https://en.wikipedia.org/wiki/Gzip)
