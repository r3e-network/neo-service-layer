using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Interface for persistent storage operations.
    /// </summary>
    public interface IPersistentStorageService
    {
        /// <summary>
        /// Initializes the storage service.
        /// </summary>
        /// <param name="options">The storage options.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync(PersistentStorageOptions options);

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>The data read from storage, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string key);

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteAsync(string key, byte[] data);

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string key);

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Lists all keys in storage with a specified prefix.
        /// </summary>
        /// <param name="prefix">The key prefix.</param>
        /// <returns>A list of keys.</returns>
        Task<List<string>> ListKeysAsync(string prefix);

        /// <summary>
        /// Gets the size of the data for a specified key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        Task<long> GetSizeAsync(string key);

        /// <summary>
        /// Opens a stream for reading from storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A stream for reading from storage, or null if the key does not exist.</returns>
        Task<Stream> OpenReadStreamAsync(string key);

        /// <summary>
        /// Opens a stream for writing to storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A stream for writing to storage.</returns>
        Task<Stream> OpenWriteStreamAsync(string key);

        /// <summary>
        /// Flushes any pending changes to storage.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task FlushAsync();
    }

    /// <summary>
    /// Options for persistent storage.
    /// </summary>
    public class PersistentStorageOptions
    {
        /// <summary>
        /// Gets or sets the storage path.
        /// </summary>
        public string StoragePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable encryption.
        /// </summary>
        public bool EnableEncryption { get; set; }

        /// <summary>
        /// Gets or sets the encryption key.
        /// </summary>
        public byte[] EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable compression.
        /// </summary>
        public bool EnableCompression { get; set; }

        /// <summary>
        /// Gets or sets the compression level (1-9).
        /// </summary>
        public int CompressionLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create the storage directory if it does not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; }

        /// <summary>
        /// Gets or sets the maximum chunk size for large data.
        /// </summary>
        public int MaxChunkSize { get; set; } = 4 * 1024 * 1024; // 4MB by default

        /// <summary>
        /// Gets or sets a value indicating whether to enable caching.
        /// </summary>
        public bool EnableCaching { get; set; }

        /// <summary>
        /// Gets or sets the cache size in bytes.
        /// </summary>
        public long CacheSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB by default

        /// <summary>
        /// Gets or sets a value indicating whether to enable auto-flush.
        /// </summary>
        public bool EnableAutoFlush { get; set; }

        /// <summary>
        /// Gets or sets the auto-flush interval in milliseconds.
        /// </summary>
        public int AutoFlushIntervalMs { get; set; } = 5000; // 5 seconds by default
    }
} 