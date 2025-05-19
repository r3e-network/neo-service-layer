namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Options for the Occlum file storage provider.
    /// </summary>
    public class OcclumFileStorageOptions
    {
        /// <summary>
        /// Gets or sets the directory where the storage files are stored.
        /// </summary>
        public string StorageDirectory { get; set; } = "occlum_storage";

        /// <summary>
        /// Gets or sets the maximum size of a file in bytes.
        /// </summary>
        public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Gets or sets whether to enable journaling for crash recovery.
        /// </summary>
        public bool EnableJournaling { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of journal entries to keep.
        /// </summary>
        public int MaxJournalEntries { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to enable automatic compaction.
        /// </summary>
        public bool EnableAutoCompaction { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold for automatic compaction (number of operations).
        /// </summary>
        public int AutoCompactionThreshold { get; set; } = 1000;
    }
}
