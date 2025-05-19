using System;

namespace NeoServiceLayer.Shared.Models
{
    /// <summary>
    /// Represents a secure key.
    /// </summary>
    public class SecureKey
    {
        /// <summary>
        /// Gets or sets the ID of the key.
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public KeyType KeyType { get; set; }

        /// <summary>
        /// Gets or sets the time when the key was created.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the time when the key was last used.
        /// </summary>
        public DateTime LastUsedTime { get; set; }
    }

    /// <summary>
    /// Represents the type of a key.
    /// </summary>
    public enum KeyType
    {
        /// <summary>
        /// AES-256 key.
        /// </summary>
        Aes256,

        /// <summary>
        /// RSA-2048 key.
        /// </summary>
        Rsa2048,

        /// <summary>
        /// RSA-4096 key.
        /// </summary>
        Rsa4096,

        /// <summary>
        /// ECDSA P-256 key.
        /// </summary>
        EcdsaP256,

        /// <summary>
        /// ECDSA P-384 key.
        /// </summary>
        EcdsaP384
    }

    /// <summary>
    /// Represents the type of hash algorithm to use for cryptographic operations.
    /// </summary>
    public enum HashAlgorithmType
    {
        /// <summary>
        /// SHA-256 hash algorithm.
        /// </summary>
        Sha256,

        /// <summary>
        /// SHA-384 hash algorithm.
        /// </summary>
        Sha384,

        /// <summary>
        /// SHA-512 hash algorithm.
        /// </summary>
        Sha512
    }
}
