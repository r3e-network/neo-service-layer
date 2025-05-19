using System;
using System.IO;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// Options for the file storage provider.
    /// </summary>
    public class FileStorageOptions
    {
        /// <summary>
        /// Gets or sets the path where files are stored.
        /// </summary>
        public string StoragePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage");

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
        public string JournalDirectory { get; set; } = "journal";

        /// <summary>
        /// Gets or sets a value indicating whether to compress data.
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets the compression level.
        /// </summary>
        public int CompressionLevel { get; set; } = 5;
    }
}
