using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Storage
{
    /// <summary>
    /// Interface for persistent storage providers.
    /// </summary>
    public interface IPersistentStorageProvider : IDisposable
    {
        /// <summary>
        /// Gets the name of the storage provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the storage provider is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets a value indicating whether the storage provider supports transactions.
        /// </summary>
        bool SupportsTransactions { get; }

        /// <summary>
        /// Gets a value indicating whether the storage provider supports encryption.
        /// </summary>
        bool SupportsEncryption { get; }

        /// <summary>
        /// Gets a value indicating whether the storage provider supports compression.
        /// </summary>
        bool SupportsCompression { get; }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <param name="options">The storage options.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync(PersistentStorageOptions options);

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The data as a byte array, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key to write.</param>
        /// <param name="data">The data to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the key was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all keys in storage with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to filter keys by.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of keys.</returns>
        Task<IEnumerable<string>> ListKeysAsync(string prefix = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the size of the data for the specified key.
        /// </summary>
        /// <param name="key">The key to get the size for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        Task<long> GetSizeAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>A transaction object.</returns>
        /// <exception cref="NotSupportedException">Thrown if transactions are not supported.</exception>
        IStorageTransaction BeginTransaction();

        /// <summary>
        /// Opens a stream for reading from storage.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A stream for reading, or null if the key does not exist.</returns>
        Task<Stream> OpenReadStreamAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a stream for writing to storage.
        /// </summary>
        /// <param name="key">The key to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A stream for writing.</returns>
        Task<Stream> OpenWriteStreamAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes any pending changes to storage.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for storage transactions.
    /// </summary>
    public interface IStorageTransaction : IDisposable
    {
        /// <summary>
        /// Reads data from storage within the transaction.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The data as a byte array, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes data to storage within the transaction.
        /// </summary>
        /// <param name="key">The key to write.</param>
        /// <param name="data">The data to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes data from storage within the transaction.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the key was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);
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
        /// Gets or sets the compression level.
        /// </summary>
        public int CompressionLevel { get; set; } = 6;

        /// <summary>
        /// Gets or sets the maximum size of a chunk in bytes.
        /// </summary>
        public int MaxChunkSize { get; set; } = 4 * 1024 * 1024; // 4 MB

        /// <summary>
        /// Gets or sets a value indicating whether to create the storage directory if it does not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable auto-flush.
        /// </summary>
        public bool EnableAutoFlush { get; set; } = true;

        /// <summary>
        /// Gets or sets the auto-flush interval in milliseconds.
        /// </summary>
        public int AutoFlushIntervalMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets a value indicating whether to enable caching.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the cache size in bytes.
        /// </summary>
        public long CacheSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 MB

        /// <summary>
        /// Gets or sets a value indicating whether to enable logging.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public string LogLevel { get; set; } = "Information";
    }
}
