using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a key vault entry in the database.
    /// </summary>
    public class KeyVaultEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the key identifier.
        /// </summary>
        public string KeyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key type.
        /// </summary>
        public string KeyType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the encrypted key data.
        /// </summary>
        public byte[] EncryptedKeyData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the key metadata as JSON.
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets or sets when the key was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the key expires.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets when the key was last used.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }
    }
}
