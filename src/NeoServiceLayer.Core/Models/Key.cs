using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents a cryptographic key.
    /// </summary>
    public class Key
    {
        /// <summary>
        /// Gets or sets the unique identifier of the key.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who owns the key.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public KeyType Type { get; set; }

        /// <summary>
        /// Gets or sets the algorithm used for the key.
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the time when the key was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the key expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the key was revoked.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the status of the key.
        /// </summary>
        public KeyStatus Status { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the key.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Creates a new instance of the Key class.
        /// </summary>
        public Key()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddDays(90);
            Status = KeyStatus.Active;
            Metadata = new Dictionary<string, object>();
        }
    }

    // Using the existing KeyType enum from the codebase

    /// <summary>
    /// Represents the status of a key.
    /// </summary>
    public enum KeyStatus
    {
        /// <summary>
        /// The key is active.
        /// </summary>
        Active,

        /// <summary>
        /// The key is revoked.
        /// </summary>
        Revoked,

        /// <summary>
        /// The key is expired.
        /// </summary>
        Expired
    }
}
