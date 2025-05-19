using System;
using System.IO;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// Options for the RocksDB storage provider.
    /// </summary>
    public class RocksDBStorageOptions
    {
        /// <summary>
        /// Gets or sets the path where the database is stored.
        /// </summary>
        public string StoragePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rocksdb");

        /// <summary>
        /// Gets or sets a value indicating whether to create the storage directory if it doesn't exist.
        /// </summary>
        public bool CreateDirectoryIfNotExists { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use a cache.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of items to keep in the cache.
        /// </summary>
        public int MaxCacheItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating whether to use compression.
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Gets or sets the size of the write buffer in bytes.
        /// </summary>
        public long WriteBufferSize { get; set; } = 64 * 1024 * 1024; // 64 MB

        /// <summary>
        /// Gets or sets the number of write buffers.
        /// </summary>
        public int MaxWriteBufferNumber { get; set; } = 3;

        /// <summary>
        /// Gets or sets the size of the block cache in bytes.
        /// </summary>
        public long BlockCacheSize { get; set; } = 8 * 1024 * 1024; // 8 MB

        /// <summary>
        /// Gets or sets the size of the block in bytes.
        /// </summary>
        public int BlockSize { get; set; } = 4 * 1024; // 4 KB

        /// <summary>
        /// Gets or sets the maximum number of open files.
        /// </summary>
        public int MaxOpenFiles { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating whether to create a backup before compaction.
        /// </summary>
        public bool CreateBackupBeforeCompaction { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory where backups are stored.
        /// </summary>
        public string BackupDirectory { get; set; } = "rocksdb_backup";
    }
}
