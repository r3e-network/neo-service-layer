namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Enumeration of storage provider types.
    /// </summary>
    public enum StorageProviderType
    {
        /// <summary>
        /// In-memory storage provider.
        /// </summary>
        Memory = 0,

        /// <summary>
        /// File-based storage provider.
        /// </summary>
        File = 1,

        /// <summary>
        /// LevelDB storage provider.
        /// </summary>
        LevelDB = 2,

        /// <summary>
        /// RocksDB storage provider.
        /// </summary>
        RocksDB = 3,

        /// <summary>
        /// SQLite storage provider.
        /// </summary>
        SQLite = 4,

        /// <summary>
        /// Occlum file storage provider.
        /// </summary>
        OcclumFile = 5,

        /// <summary>
        /// Custom storage provider.
        /// </summary>
        Custom = 99
    }
}
