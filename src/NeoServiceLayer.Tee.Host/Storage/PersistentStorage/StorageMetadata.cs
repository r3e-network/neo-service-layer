using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Represents metadata for data in storage.
    /// </summary>
    public class StorageMetadata
    {
        /// <summary>
        /// Gets or sets the key for the data.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the size of the data in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the creation time of the data.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the last modification time of the data.
        /// </summary>
        public DateTime LastModifiedTime { get; set; }

        /// <summary>
        /// Gets or sets the last access time of the data.
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// Gets or sets the content type of the data.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the encryption algorithm used for the data.
        /// </summary>
        public string EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the compression algorithm used for the data.
        /// </summary>
        public string CompressionAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the hash algorithm used for the data.
        /// </summary>
        public string HashAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the hash of the data.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets whether the data is chunked.
        /// </summary>
        public bool IsChunked { get; set; }

        /// <summary>
        /// Gets or sets the chunk size in bytes.
        /// </summary>
        public int ChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the number of chunks.
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// Gets or sets the tags for the data.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the custom metadata for the data.
        /// </summary>
        public Dictionary<string, string> CustomMetadata { get; set; } = new Dictionary<string, string>();
    }
}
