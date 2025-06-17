using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage.Models;

namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Interface for the Storage service.
/// </summary>
public interface IStorageService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Stores data.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="data">The data.</param>
    /// <param name="options">The storage options.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The storage metadata.</returns>
    Task<StorageMetadata> StoreDataAsync(string key, byte[] data, StorageOptions options, BlockchainType blockchainType);

    /// <summary>
    /// Retrieves data.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data.</returns>
    Task<byte[]> RetrieveDataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Deletes data.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the data was deleted, false otherwise.</returns>
    Task<bool> DeleteDataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Gets storage metadata.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The storage metadata.</returns>
    Task<StorageMetadata> GetMetadataAsync(string key, BlockchainType blockchainType);

    /// <summary>
    /// Lists storage keys.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <param name="skip">The number of keys to skip.</param>
    /// <param name="take">The number of keys to take.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of storage metadata.</returns>
    Task<IEnumerable<StorageMetadata>> ListKeysAsync(string prefix, int skip, int take, BlockchainType blockchainType);

    /// <summary>
    /// Updates storage metadata.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="metadata">The new metadata.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the metadata was updated, false otherwise.</returns>
    Task<bool> UpdateMetadataAsync(string key, StorageMetadata metadata, BlockchainType blockchainType);

    /// <summary>
    /// Begins a storage transaction.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The transaction ID.</returns>
    Task<string> BeginTransactionAsync(BlockchainType blockchainType);

    /// <summary>
    /// Commits a storage transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the transaction was committed, false otherwise.</returns>
    Task<bool> CommitTransactionAsync(string transactionId, BlockchainType blockchainType);

    /// <summary>
    /// Rolls back a storage transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the transaction was rolled back, false otherwise.</returns>
    Task<bool> RollbackTransactionAsync(string transactionId, BlockchainType blockchainType);
}

/// <summary>
/// Storage options.
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the data should be encrypted.
    /// </summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the data should be compressed.
    /// </summary>
    public bool Compress { get; set; } = true;

    /// <summary>
    /// Gets or sets the encryption key ID (if null, a default key will be used).
    /// </summary>
    public string? EncryptionKeyId { get; set; }

    /// <summary>
    /// Gets or sets the expiration time (if null, the data will not expire).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the access control list.
    /// </summary>
    public List<string> AccessControlList { get; set; } = new();

    /// <summary>
    /// Gets or sets the chunk size in bytes (0 means no chunking).
    /// </summary>
    public int ChunkSizeBytes { get; set; } = 0;

    /// <summary>
    /// Gets or sets the chunk size (alias for ChunkSizeBytes for compatibility).
    /// </summary>
    public int ChunkSize
    {
        get => ChunkSizeBytes;
        set => ChunkSizeBytes = value;
    }

    /// <summary>
    /// Gets or sets the encryption algorithm.
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Gets or sets the compression algorithm.
    /// </summary>
    public string CompressionAlgorithm { get; set; } = "GZIP";

    /// <summary>
    /// Gets or sets the replication factor.
    /// </summary>
    public int ReplicationFactor { get; set; } = 1;

    /// <summary>
    /// Gets or sets the storage class.
    /// </summary>
    public string StorageClass { get; set; } = "Standard";

    /// <summary>
    /// Gets or sets the custom metadata.
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Storage metadata.
/// </summary>
public class StorageMetadata
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the data is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the data is compressed.
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// Gets or sets the encryption key ID.
    /// </summary>
    public string? EncryptionKeyId { get; set; }

    /// <summary>
    /// Gets or sets the encryption algorithm used.
    /// </summary>
    public string? EncryptionAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified time.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the last accessed time.
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the access control list.
    /// </summary>
    public List<string> AccessControlList { get; set; } = new();

    /// <summary>
    /// Gets or sets the chunk count.
    /// </summary>
    public int ChunkCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the chunk size in bytes.
    /// </summary>
    public int ChunkSizeBytes { get; set; } = 0;

    /// <summary>
    /// Gets or sets the replication factor.
    /// </summary>
    public int ReplicationFactor { get; set; } = 1;

    /// <summary>
    /// Gets or sets the storage class.
    /// </summary>
    public string StorageClass { get; set; } = "Standard";

    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom metadata.
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}
