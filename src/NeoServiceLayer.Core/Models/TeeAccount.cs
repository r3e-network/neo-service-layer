using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents a TEE-managed account in the Neo Service Layer.
    /// </summary>
    public class TeeAccount
    {
        /// <summary>
        /// Gets or sets the unique identifier of the account.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the account.
        /// </summary>
        public AccountType Type { get; set; }

        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        public KeyType KeyType { get; set; }

        /// <summary>
        /// Gets or sets the public key of the account.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the address of the account.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who owns the account.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the time when the account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the account was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the account.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets whether the account is exportable.
        /// </summary>
        public bool IsExportable { get; set; }

        /// <summary>
        /// Gets or sets the attestation proof associated with the account.
        /// </summary>
        public string AttestationProof { get; set; }

        /// <summary>
        /// Creates a new instance of the TeeAccount class.
        /// </summary>
        public TeeAccount()
        {
            Id = Guid.NewGuid().ToString();
            Metadata = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
            IsExportable = false;
        }
    }


}
