namespace NeoServiceLayer.Common.Storage
{
    /// <summary>
    /// Defines the types of storage providers available in the system.
    /// </summary>
    public enum StorageProviderType
    {
        /// <summary>
        /// File-based storage provider.
        /// </summary>
        File = 0,

        /// <summary>
        /// RocksDB storage provider.
        /// </summary>
        RocksDB = 1,

        /// <summary>
        /// LevelDB storage provider.
        /// </summary>
        LevelDB = 2,

        /// <summary>
        /// Occlum file storage provider.
        /// </summary>
        OcclumFile = 3,

        /// <summary>
        /// In-memory storage provider (for testing).
        /// </summary>
        InMemory = 4,

        /// <summary>
        /// SQLite storage provider.
        /// </summary>
        SQLite = 5
    }
}
