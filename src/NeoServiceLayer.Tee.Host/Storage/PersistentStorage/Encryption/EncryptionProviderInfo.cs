using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Encryption
{
    /// <summary>
    /// Information about an encryption provider.
    /// </summary>
    public class EncryptionProviderInfo
    {
        /// <summary>
        /// Gets or sets the name of the encryption provider.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the encryption provider.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the algorithm used by the encryption provider.
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the key size in bits.
        /// </summary>
        public int KeySizeBits { get; set; }

        /// <summary>
        /// Gets or sets the block size in bits.
        /// </summary>
        public int BlockSizeBits { get; set; }

        /// <summary>
        /// Gets or sets the initialization vector (IV) size in bits.
        /// </summary>
        public int IVSizeBits { get; set; }

        /// <summary>
        /// Gets or sets whether the encryption provider supports authenticated encryption.
        /// </summary>
        public bool SupportsAuthenticatedEncryption { get; set; }

        /// <summary>
        /// Gets or sets whether the encryption provider supports key rotation.
        /// </summary>
        public bool SupportsKeyRotation { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the current key was created.
        /// </summary>
        public DateTime? CurrentKeyCreationTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the key was last rotated.
        /// </summary>
        public DateTime? LastKeyRotationTime { get; set; }

        /// <summary>
        /// Gets or sets additional properties of the encryption provider.
        /// </summary>
        public Dictionary<string, string> AdditionalProperties { get; set; } = new Dictionary<string, string>();
    }
}
