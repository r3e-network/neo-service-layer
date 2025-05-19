using System;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// Options for the Occlum file storage provider.
    /// </summary>
    public class OcclumFileStorageOptions
    {
        /// <summary>
        /// Gets or sets the path where files are stored.
        /// </summary>
        public string StoragePath { get; set; } = "/occlum_instance/storage";

        /// <summary>
        /// Gets or sets a value indicating whether to create the storage directory if it doesn't exist.
        /// </summary>
        public bool CreateDirectoryIfNotExists { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use Occlum-specific optimizations.
        /// </summary>
        public bool UseOcclumOptimizations { get; set; } = true;

        /// <summary>
        /// Gets or sets the Occlum instance directory.
        /// </summary>
        public string OcclumInstanceDirectory { get; set; } = "/occlum_instance";

        /// <summary>
        /// Gets or sets a value indicating whether to use memory-mapped files.
        /// </summary>
        public bool UseMemoryMappedFiles { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum file size for memory-mapped files.
        /// </summary>
        public long MaxMemoryMappedFileSize { get; set; } = 1024 * 1024 * 10; // 10 MB
    }
}
