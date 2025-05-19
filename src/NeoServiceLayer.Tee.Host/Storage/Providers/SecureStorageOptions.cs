using System;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// Options for the secure storage provider.
    /// </summary>
    public class SecureStorageOptions
    {
        /// <summary>
        /// Gets or sets the directory where files are stored.
        /// </summary>
        public string StorageDirectory { get; set; } = "secure_storage";

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
        /// Gets or sets a value indicating whether to use journaling for crash recovery.
        /// </summary>
        public bool EnableJournaling { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory where journal files are stored.
        /// </summary>
        public string JournalDirectory { get; set; } = "secure_journal";

        /// <summary>
        /// Gets or sets a value indicating whether to compress data.
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets the compression level.
        /// </summary>
        public int CompressionLevel { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether to use persistence.
        /// </summary>
        public bool EnablePersistence { get; set; } = true;

        /// <summary>
        /// Gets or sets the encryption algorithm to use.
        /// </summary>
        public string EncryptionAlgorithm { get; set; } = "AES-GCM";

        /// <summary>
        /// Gets or sets the key size in bits.
        /// </summary>
        public int KeySize { get; set; } = 256;

        /// <summary>
        /// Gets or sets the key rotation interval in days.
        /// </summary>
        public int KeyRotationIntervalDays { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically rotate keys.
        /// </summary>
        public bool AutoRotateKeys { get; set; } = true;

        /// <summary>
        /// Gets or sets the underlying storage provider type.
        /// </summary>
        public StorageProviderType UnderlyingProviderType { get; set; } = StorageProviderType.File;

        /// <summary>
        /// Gets or sets the options for the underlying storage provider.
        /// </summary>
        public object UnderlyingProviderOptions { get; set; }
    }
}
