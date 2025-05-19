using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents an attestation proof in the Neo Service Layer.
    /// </summary>
    public class AttestationProof
    {
        /// <summary>
        /// Gets or sets the unique identifier of the attestation proof.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the attestation report.
        /// </summary>
        public string Report { get; set; }

        /// <summary>
        /// Gets or sets the signature of the attestation report.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Gets or sets the MRENCLAVE value.
        /// </summary>
        public string MrEnclave { get; set; }

        /// <summary>
        /// Gets or sets the MRSIGNER value.
        /// </summary>
        public string MrSigner { get; set; }

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the security version number.
        /// </summary>
        public string SecurityVersion { get; set; }

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        public string Attributes { get; set; }

        /// <summary>
        /// Gets or sets the time when the attestation proof was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the attestation proof expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the attestation proof.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Creates a new instance of the AttestationProof class.
        /// </summary>
        public AttestationProof()
        {
            Id = Guid.NewGuid().ToString();
            Metadata = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = CreatedAt.AddHours(24);
        }
    }
}
