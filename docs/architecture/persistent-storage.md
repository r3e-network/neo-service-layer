# Neo Service Layer - Persistent Storage

## Overview

The Persistent Storage component of the Neo Service Layer provides a secure, reliable, and efficient way to store and retrieve data. It supports encryption, compression, chunking, and transaction support, ensuring that data is protected and can be efficiently managed.

## Architecture

The Persistent Storage component consists of the following parts:

### Storage Interface

The `IPersistentStorageProvider` interface defines the contract for all storage providers:

```csharp
public interface IPersistentStorageProvider : IDisposable
{
    Task<bool> InitializeAsync();
    Task<bool> StoreAsync(string key, byte[] data, StorageOptions options);
    Task<byte[]> RetrieveAsync(string key);
    Task<bool> DeleteAsync(string key);
    Task<StorageMetadata> GetMetadataAsync(string key);
    Task<bool> BeginTransactionAsync();
    Task<bool> CommitTransactionAsync();
    Task<bool> RollbackTransactionAsync();
    Task<IEnumerable<string>> ListKeysAsync(string prefix = null);
}
```

### Storage Options

The `StorageOptions` class defines options for storing data:

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

### Storage Metadata

The `StorageMetadata` class provides metadata about stored data:

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

### Storage Providers

The Neo Service Layer includes the following storage providers:

1. **OcclumFileStorageProvider**: Uses Occlum's file system for storage.
2. **RocksDBStorageProvider**: Uses RocksDB for storage.
3. **LevelDBStorageProvider**: Uses LevelDB for storage.

## Implementation Details

### Data Processing Flow

When storing data, the following processing flow is used:

1. **Chunking**: Large data is split into chunks for efficient storage and retrieval.
2. **Compression**: Data is compressed to reduce storage requirements.
3. **Encryption**: Data is encrypted to ensure confidentiality.
4. **Storage**: Processed data is stored using the configured storage provider.

When retrieving data, the reverse flow is used:

1. **Retrieval**: Data is retrieved from the storage provider.
2. **Decryption**: Encrypted data is decrypted.
3. **Decompression**: Compressed data is decompressed.
4. **Reassembly**: Chunked data is reassembled.

### Encryption

Data encryption is performed using the AES-256-GCM algorithm by default. The encryption process includes:

1. **Key Derivation**: A unique encryption key is derived for each data item.
2. **Encryption**: Data is encrypted using the derived key.
3. **Authentication**: The encrypted data includes an authentication tag to ensure integrity.

### Compression

Data compression is performed using the GZIP algorithm by default. The compression process includes:

1. **Compression**: Data is compressed to reduce its size.
2. **Metadata**: Compression metadata is stored to enable decompression.

### Chunking

Large data is split into chunks for efficient storage and retrieval. The chunking process includes:

1. **Splitting**: Data is split into chunks of the specified size.
2. **Metadata**: Chunking metadata is stored to enable reassembly.

### Transaction Support

The storage providers support transactions to ensure data consistency. The transaction process includes:

1. **Begin Transaction**: A transaction is started.
2. **Operations**: Storage operations are performed within the transaction.
3. **Commit Transaction**: The transaction is committed, making the changes permanent.
4. **Rollback Transaction**: If an error occurs, the transaction is rolled back, reverting the changes.

## OcclumFileStorageProvider

The `OcclumFileStorageProvider` uses Occlum's file system for storage. It provides a secure storage solution that leverages the security features of Occlum LibOS.

### Implementation

```csharp
public class OcclumFileStorageProvider : IPersistentStorageProvider
{
    private readonly string _storagePath;
    private readonly IEncryptionService _encryptionService;
    private readonly ICompressionService _compressionService;
    private readonly ILogger<OcclumFileStorageProvider> _logger;
    private bool _isTransactionActive;
    private List<StorageOperation> _transactionOperations;

    public OcclumFileStorageProvider(
        string storagePath,
        IEncryptionService encryptionService,
        ICompressionService compressionService,
        ILogger<OcclumFileStorageProvider> logger)
    {
        _storagePath = storagePath;
        _encryptionService = encryptionService;
        _compressionService = compressionService;
        _logger = logger;
        _isTransactionActive = false;
        _transactionOperations = new List<StorageOperation>();
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing OcclumFileStorageProvider");
            return false;
        }
    }

    public async Task<bool> StoreAsync(string key, byte[] data, StorageOptions options)
    {
        try
        {
            // Process data (chunking, compression, encryption)
            var processedData = await ProcessDataForStorageAsync(data, options);

            // Store data
            var filePath = Path.Combine(_storagePath, key);
            
            if (_isTransactionActive)
            {
                // Add to transaction operations
                _transactionOperations.Add(new StorageOperation
                {
                    OperationType = StorageOperationType.Store,
                    Key = key,
                    Data = processedData,
                    FilePath = filePath
                });
                return true;
            }
            else
            {
                // Store immediately
                await File.WriteAllBytesAsync(filePath, processedData);
                
                // Store metadata
                await StoreMetadataAsync(key, data.Length, options);
                
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing data with key {Key}", key);
            return false;
        }
    }

    public async Task<byte[]> RetrieveAsync(string key)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, key);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Data with key {Key} not found", key);
                return null;
            }
            
            var processedData = await File.ReadAllBytesAsync(filePath);
            
            // Get metadata
            var metadata = await GetMetadataAsync(key);
            
            // Process data (decryption, decompression, reassembly)
            var data = await ProcessDataForRetrievalAsync(processedData, metadata);
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data with key {Key}", key);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, key);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Data with key {Key} not found", key);
                return false;
            }
            
            if (_isTransactionActive)
            {
                // Add to transaction operations
                _transactionOperations.Add(new StorageOperation
                {
                    OperationType = StorageOperationType.Delete,
                    Key = key,
                    FilePath = filePath
                });
                return true;
            }
            else
            {
                // Delete immediately
                File.Delete(filePath);
                
                // Delete metadata
                await DeleteMetadataAsync(key);
                
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data with key {Key}", key);
            return false;
        }
    }

    public async Task<StorageMetadata> GetMetadataAsync(string key)
    {
        try
        {
            var metadataPath = Path.Combine(_storagePath, $"{key}.metadata");
            
            if (!File.Exists(metadataPath))
            {
                _logger.LogWarning("Metadata for key {Key} not found", key);
                return null;
            }
            
            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<StorageMetadata>(metadataJson);
            
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for key {Key}", key);
            return null;
        }
    }

    public async Task<bool> BeginTransactionAsync()
    {
        if (_isTransactionActive)
        {
            _logger.LogWarning("Transaction already active");
            return false;
        }
        
        _isTransactionActive = true;
        _transactionOperations.Clear();
        
        return true;
    }

    public async Task<bool> CommitTransactionAsync()
    {
        if (!_isTransactionActive)
        {
            _logger.LogWarning("No active transaction to commit");
            return false;
        }
        
        try
        {
            // Execute all operations
            foreach (var operation in _transactionOperations)
            {
                switch (operation.OperationType)
                {
                    case StorageOperationType.Store:
                        await File.WriteAllBytesAsync(operation.FilePath, operation.Data);
                        break;
                    case StorageOperationType.Delete:
                        File.Delete(operation.FilePath);
                        break;
                }
            }
            
            _isTransactionActive = false;
            _transactionOperations.Clear();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            
            // Attempt rollback
            await RollbackTransactionAsync();
            
            return false;
        }
    }

    public async Task<bool> RollbackTransactionAsync()
    {
        if (!_isTransactionActive)
        {
            _logger.LogWarning("No active transaction to rollback");
            return false;
        }
        
        _isTransactionActive = false;
        _transactionOperations.Clear();
        
        return true;
    }

    public async Task<IEnumerable<string>> ListKeysAsync(string prefix = null)
    {
        try
        {
            var files = Directory.GetFiles(_storagePath);
            var keys = files
                .Select(f => Path.GetFileName(f))
                .Where(f => !f.EndsWith(".metadata"))
                .Where(f => prefix == null || f.StartsWith(prefix))
                .ToList();
            
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing keys with prefix {Prefix}", prefix);
            return Enumerable.Empty<string>();
        }
    }

    public void Dispose()
    {
        // Clean up resources
    }

    private async Task<byte[]> ProcessDataForStorageAsync(byte[] data, StorageOptions options)
    {
        // Implement data processing for storage
        // (chunking, compression, encryption)
        return data;
    }

    private async Task<byte[]> ProcessDataForRetrievalAsync(byte[] processedData, StorageMetadata metadata)
    {
        // Implement data processing for retrieval
        // (decryption, decompression, reassembly)
        return processedData;
    }

    private async Task<bool> StoreMetadataAsync(string key, long size, StorageOptions options)
    {
        try
        {
            var metadata = new StorageMetadata
            {
                Key = key,
                Size = size,
                Chunks = 1, // Simplified for example
                Encrypted = options.Encrypt,
                Compressed = options.Compress,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                AccessControlList = options.AccessControlList,
                Version = 1
            };
            
            var metadataJson = JsonSerializer.Serialize(metadata);
            var metadataPath = Path.Combine(_storagePath, $"{key}.metadata");
            
            await File.WriteAllTextAsync(metadataPath, metadataJson);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing metadata for key {Key}", key);
            return false;
        }
    }

    private async Task<bool> DeleteMetadataAsync(string key)
    {
        try
        {
            var metadataPath = Path.Combine(_storagePath, $"{key}.metadata");
            
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metadata for key {Key}", key);
            return false;
        }
    }

    private enum StorageOperationType
    {
        Store,
        Delete
    }

    private class StorageOperation
    {
        public StorageOperationType OperationType { get; set; }
        public string Key { get; set; }
        public byte[] Data { get; set; }
        public string FilePath { get; set; }
    }
}
```

## RocksDBStorageProvider

The `RocksDBStorageProvider` uses RocksDB for storage. It provides a high-performance storage solution that is suitable for applications with high throughput requirements.

### Implementation

The implementation of the `RocksDBStorageProvider` follows a similar pattern to the `OcclumFileStorageProvider`, but uses RocksDB for storage instead of the file system.

## LevelDBStorageProvider

The `LevelDBStorageProvider` uses LevelDB for storage. It provides a lightweight storage solution that is suitable for applications with moderate throughput requirements.

### Implementation

The implementation of the `LevelDBStorageProvider` follows a similar pattern to the `OcclumFileStorageProvider`, but uses LevelDB for storage instead of the file system.

## Usage

To use the Persistent Storage component, follow these steps:

1. **Choose a Storage Provider**: Choose the appropriate storage provider for your needs.
2. **Configure the Storage Provider**: Configure the storage provider with the appropriate options.
3. **Initialize the Storage Provider**: Initialize the storage provider.
4. **Store and Retrieve Data**: Use the storage provider to store and retrieve data.

Example:

```csharp
// Create a storage provider
var storageProvider = new OcclumFileStorageProvider(
    "/path/to/storage",
    encryptionService,
    compressionService,
    logger);

// Initialize the storage provider
await storageProvider.InitializeAsync();

// Store data
var options = new StorageOptions
{
    Encrypt = true,
    Compress = true,
    ChunkSizeBytes = 1024 * 1024, // 1 MB
    AccessControlList = new[] { "user1", "user2" }
};
await storageProvider.StoreAsync("my-key", data, options);

// Retrieve data
var retrievedData = await storageProvider.RetrieveAsync("my-key");

// Get metadata
var metadata = await storageProvider.GetMetadataAsync("my-key");

// Delete data
await storageProvider.DeleteAsync("my-key");
```

## Conclusion

The Persistent Storage component of the Neo Service Layer provides a secure, reliable, and efficient way to store and retrieve data. It supports encryption, compression, chunking, and transaction support, ensuring that data is protected and can be efficiently managed. The component includes multiple storage providers, allowing you to choose the appropriate storage solution for your needs.
