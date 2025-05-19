namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Options for the secure storage.
    /// </summary>
    public class SecureStorageOptions
    {
        /// <summary>
        /// Gets or sets the directory where the secure storage files are stored.
        /// </summary>
        public string StorageDirectory { get; set; } = "secure_storage";

        /// <summary>
        /// Gets or sets whether to enable caching of values in memory.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable persistence of values to disk.
        /// </summary>
        public bool EnablePersistence { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of cached items.
        /// </summary>
        public int MaxCachedItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum size of a value in bytes.
        /// </summary>
        public int MaxValueSizeBytes { get; set; } = 1024 * 1024; // 1 MB

        /// <summary>
        /// Gets or sets whether to compress values before storing them.
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to encrypt values before storing them.
        /// </summary>
        public bool EnableEncryption { get; set; } = true;

        /// <summary>
        /// Gets or sets the encryption key to use for encrypting values.
        /// </summary>
        public string EncryptionKey { get; set; }
    }
}
