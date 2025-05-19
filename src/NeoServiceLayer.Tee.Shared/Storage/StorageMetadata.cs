using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Shared.Storage
{
    /// <summary>
    /// Metadata for stored data.
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
        /// Gets or sets the last modified time of the data.
        /// </summary>
        public DateTime LastModifiedTime { get; set; }

        /// <summary>
        /// Gets or sets the last access time of the data.
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data is chunked.
        /// </summary>
        public bool IsChunked { get; set; }

        /// <summary>
        /// Gets or sets the number of chunks if the data is chunked.
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// Gets or sets the size of each chunk in bytes if the data is chunked.
        /// </summary>
        public int ChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the hash of the data for integrity verification.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data is encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Gets or sets the encryption algorithm used if the data is encrypted.
        /// </summary>
        public string EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the key ID used for encryption if the data is encrypted.
        /// </summary>
        public string EncryptionKeyId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data is compressed.
        /// </summary>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Gets or sets the compression algorithm used if the data is compressed.
        /// </summary>
        public string CompressionAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the original size of the data before compression if the data is compressed.
        /// </summary>
        public long OriginalSize { get; set; }

        /// <summary>
        /// Gets or sets the content type of the data.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets custom metadata for the data.
        /// </summary>
        public Dictionary<string, string> CustomMetadata { get; set; } = new Dictionary<string, string>();
    }
}
