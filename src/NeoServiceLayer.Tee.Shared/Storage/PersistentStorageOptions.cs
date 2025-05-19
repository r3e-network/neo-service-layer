using System;

namespace NeoServiceLayer.Tee.Shared.Storage
{
    /// <summary>
    /// Options for persistent storage.
    /// </summary>
    public class PersistentStorageOptions
    {
        /// <summary>
        /// Gets or sets the storage path.
        /// </summary>
        public string StoragePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage");

        /// <summary>
        /// Gets or sets a value indicating whether to enable encryption.
        /// </summary>
        public bool EnableEncryption { get; set; } = true;

        /// <summary>
        /// Gets or sets the encryption key.
        /// </summary>
        public byte[] EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable compression.
        /// </summary>
        public bool EnableCompression { get; set; } = true;

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
