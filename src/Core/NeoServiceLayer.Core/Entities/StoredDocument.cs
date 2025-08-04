using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a stored document in the database.
    /// </summary>
    public class StoredDocument
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the document name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the document type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the document content.
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the content hash.
        /// </summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the document size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets whether the document is encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Gets or sets the metadata as JSON.
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets or sets when the document was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the document was last accessed.
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }
    }
}
