namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Options for the RocksDB storage provider.
    /// </summary>
    public class RocksDBStorageOptions
    {
        /// <summary>
        /// Gets or sets the directory where the RocksDB files are stored.
        /// </summary>
        public string StorageDirectory { get; set; } = "rocksdb_storage";

        /// <summary>
        /// Gets or sets the maximum size of the write buffer in bytes.
        /// </summary>
        public long WriteBufferSize { get; set; } = 64 * 1024 * 1024; // 64 MB

        /// <summary>
        /// Gets or sets the maximum number of write buffers.
        /// </summary>
        public int MaxWriteBuffers { get; set; } = 3;

        /// <summary>
        /// Gets or sets the size of the block cache in bytes.
        /// </summary>
        public long BlockCacheSize { get; set; } = 8 * 1024 * 1024; // 8 MB

        /// <summary>
        /// Gets or sets the size of the block in bytes.
        /// </summary>
        public int BlockSize { get; set; } = 4 * 1024; // 4 KB

        /// <summary>
        /// Gets or sets whether to enable compression.
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable statistics.
        /// </summary>
        public bool EnableStatistics { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable automatic compaction.
        /// </summary>
        public bool EnableAutoCompaction { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of open files.
        /// </summary>
        public int MaxOpenFiles { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to use a separate database for metadata.
        /// </summary>
        public bool UseSeparateMetadataDb { get; set; } = false;
    }
}
