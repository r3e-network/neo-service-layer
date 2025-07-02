namespace NeoServiceLayer.Infrastructure.Persistence;

/// <summary>
/// Interface for persistent storage providers in the Neo Service Layer.
/// Provides secure, encrypted, and compressed storage capabilities.
/// </summary>
public interface IPersistentStorageProvider : IDisposable
{
    /// <summary>
    /// Gets the name of the storage provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets whether the storage provider is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets whether the storage provider supports transactions.
    /// </summary>
    bool SupportsTransactions { get; }

    /// <summary>
    /// Gets whether the storage provider supports compression.
    /// </summary>
    bool SupportsCompression { get; }

    /// <summary>
    /// Gets whether the storage provider supports encryption.
    /// </summary>
    bool SupportsEncryption { get; }

    /// <summary>
    /// Initializes the storage provider.
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise.</returns>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Stores data with the specified key.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="options">Storage options.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> StoreAsync(string key, byte[] data, StorageOptions? options = null);

    /// <summary>
    /// Retrieves data with the specified key.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>The retrieved data or null if not found.</returns>
    Task<byte[]?> RetrieveAsync(string key);

    /// <summary>
    /// Deletes data with the specified key.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// Checks if data exists with the specified key.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>True if data exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets metadata for the specified key.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>The storage metadata or null if not found.</returns>
    Task<StorageMetadata?> GetMetadataAsync(string key);

    /// <summary>
    /// Lists all keys with the specified prefix.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <param name="limit">Maximum number of keys to return.</param>
    /// <returns>The list of keys.</returns>
    Task<IEnumerable<string>> ListKeysAsync(string? prefix = null, int limit = 1000);

    /// <summary>
    /// Gets storage statistics.
    /// </summary>
    /// <returns>The storage statistics.</returns>
    Task<StorageStatistics> GetStatisticsAsync();

    /// <summary>
    /// Begins a transaction if supported.
    /// </summary>
    /// <returns>The transaction or null if not supported.</returns>
    Task<IStorageTransaction?> BeginTransactionAsync();

    /// <summary>
    /// Performs a backup of the storage.
    /// </summary>
    /// <param name="backupPath">The backup destination path.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> BackupAsync(string backupPath);

    /// <summary>
    /// Restores storage from a backup.
    /// </summary>
    /// <param name="backupPath">The backup source path.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> RestoreAsync(string backupPath);

    /// <summary>
    /// Compacts the storage to reclaim space.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> CompactAsync();

    /// <summary>
    /// Validates the integrity of the storage.
    /// </summary>
    /// <returns>The validation result.</returns>
    Task<StorageValidationResult> ValidateIntegrityAsync();
}

/// <summary>
/// Storage options for controlling how data is stored.
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Gets or sets whether to encrypt the data.
    /// </summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to compress the data.
    /// </summary>
    public bool Compress { get; set; } = true;

    /// <summary>
    /// Gets or sets the encryption key to use.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the compression algorithm to use.
    /// </summary>
    public CompressionAlgorithm CompressionAlgorithm { get; set; } = CompressionAlgorithm.GZip;

    /// <summary>
    /// Gets or sets the time-to-live for the data.
    /// </summary>
    public TimeSpan? TimeToLive { get; set; }

    /// <summary>
    /// Gets or sets custom metadata for the storage entry.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Storage metadata information.
/// </summary>
public class StorageMetadata
{
    /// <summary>
    /// Gets or sets the storage key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original data size in bytes.
    /// </summary>
    public long OriginalSize { get; set; }

    /// <summary>
    /// Gets or sets the stored data size in bytes.
    /// </summary>
    public long StoredSize { get; set; }

    /// <summary>
    /// Gets or sets whether the data is compressed.
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// Gets or sets whether the data is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the compression algorithm used.
    /// </summary>
    public CompressionAlgorithm? CompressionAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the last accessed timestamp.
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets the expiration timestamp.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the data checksum.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets custom metadata.
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Storage statistics information.
/// </summary>
public class StorageStatistics
{
    /// <summary>
    /// Gets or sets the total number of keys.
    /// </summary>
    public long TotalKeys { get; set; }

    /// <summary>
    /// Gets or sets the total storage size in bytes.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the total original data size in bytes.
    /// </summary>
    public long TotalOriginalSize { get; set; }

    /// <summary>
    /// Gets or sets the compression ratio.
    /// </summary>
    public double CompressionRatio { get; set; }

    /// <summary>
    /// Gets or sets the available space in bytes.
    /// </summary>
    public long AvailableSpace { get; set; }

    /// <summary>
    /// Gets or sets the number of compressed entries.
    /// </summary>
    public long CompressedEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of encrypted entries.
    /// </summary>
    public long EncryptedEntries { get; set; }

    /// <summary>
    /// Gets or sets the last compaction timestamp.
    /// </summary>
    public DateTime? LastCompaction { get; set; }

    /// <summary>
    /// Gets or sets the last backup timestamp.
    /// </summary>
    public DateTime? LastBackup { get; set; }
}

/// <summary>
/// Storage validation result.
/// </summary>
public class StorageValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of validated entries.
    /// </summary>
    public long ValidatedEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of corrupted entries.
    /// </summary>
    public long CorruptedEntries { get; set; }
}

/// <summary>
/// Compression algorithm enumeration.
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// No compression.
    /// </summary>
    None,

    /// <summary>
    /// LZ4 compression (fast).
    /// </summary>
    LZ4,

    /// <summary>
    /// GZip compression (balanced).
    /// </summary>
    GZip,

    /// <summary>
    /// Brotli compression (high compression).
    /// </summary>
    Brotli,

    /// <summary>
    /// LZMA compression (highest compression).
    /// </summary>
    LZMA
}

/// <summary>
/// Interface for storage transactions.
/// </summary>
public interface IStorageTransaction : IDisposable
{
    /// <summary>
    /// Gets the transaction ID.
    /// </summary>
    string TransactionId { get; }

    /// <summary>
    /// Gets whether the transaction is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Stores data within the transaction.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="options">Storage options.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> StoreAsync(string key, byte[] data, StorageOptions? options = null);

    /// <summary>
    /// Deletes data within the transaction.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> CommitAsync();

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> RollbackAsync();
}
