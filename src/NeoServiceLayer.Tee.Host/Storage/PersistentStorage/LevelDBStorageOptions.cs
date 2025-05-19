namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Options for the LevelDB storage provider.
    /// </summary>
    public class LevelDBStorageOptions
    {
        /// <summary>
        /// Gets or sets the directory where the LevelDB files are stored.
        /// </summary>
        public string StorageDirectory { get; set; } = "leveldb_storage";

        /// <summary>
        /// Gets or sets the maximum size of the write buffer in bytes.
        /// </summary>
        public long WriteBufferSize { get; set; } = 4 * 1024 * 1024; // 4 MB

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
        /// Gets or sets whether to enable automatic compaction.
        /// </summary>
        public bool EnableAutoCompaction { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of open files.
        /// </summary>
        public int MaxOpenFiles { get; set; } = 1000;
    }
}
