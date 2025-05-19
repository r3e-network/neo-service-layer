using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Infrastructure.Data.Entities
{
    /// <summary>
    /// Entity class for attestation proofs.
    /// </summary>
    public class AttestationProofEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the attestation proof.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the attestation report.
        /// </summary>
        [Required]
        public string Report { get; set; }

        /// <summary>
        /// Gets or sets the signature of the attestation report.
        /// </summary>
        [Required]
        public string Signature { get; set; }

        /// <summary>
        /// Gets or sets the MRENCLAVE value.
        /// </summary>
        [Required]
        public string MrEnclave { get; set; }

        /// <summary>
        /// Gets or sets the MRSIGNER value.
        /// </summary>
        [Required]
        public string MrSigner { get; set; }

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        [Required]
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the security version number.
        /// </summary>
        [Required]
        public string SecurityVersion { get; set; }

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        [Required]
        public string Attributes { get; set; }

        /// <summary>
        /// Gets or sets the time when the attestation proof was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the attestation proof expires.
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the attestation proof.
        /// </summary>
        public string MetadataJson { get; set; }

        /// <summary>
        /// Converts the entity to a domain model.
        /// </summary>
        /// <returns>The domain model.</returns>
        public AttestationProof ToDomainModel()
        {
            var attestationProof = new AttestationProof
            {
                Id = Id,
                Report = Report,
                Signature = Signature,
                MrEnclave = MrEnclave,
                MrSigner = MrSigner,
                ProductId = ProductId,
                SecurityVersion = SecurityVersion,
                Attributes = Attributes,
                CreatedAt = CreatedAt,
                ExpiresAt = ExpiresAt
            };

            if (!string.IsNullOrEmpty(MetadataJson))
            {
                attestationProof.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
            }
            else
            {
                attestationProof.Metadata = new Dictionary<string, object>();
            }

            return attestationProof;
        }

        /// <summary>
        /// Creates an entity from a domain model.
        /// </summary>
        /// <param name="attestationProof">The domain model.</param>
        /// <returns>The entity.</returns>
        public static AttestationProofEntity FromDomainModel(AttestationProof attestationProof)
        {
            var entity = new AttestationProofEntity
            {
                Id = attestationProof.Id,
                Report = attestationProof.Report,
                Signature = attestationProof.Signature,
                MrEnclave = attestationProof.MrEnclave,
                MrSigner = attestationProof.MrSigner,
                ProductId = attestationProof.ProductId,
                SecurityVersion = attestationProof.SecurityVersion,
                Attributes = attestationProof.Attributes,
                CreatedAt = attestationProof.CreatedAt,
                ExpiresAt = attestationProof.ExpiresAt
            };

            if (attestationProof.Metadata != null && attestationProof.Metadata.Count > 0)
            {
                entity.MetadataJson = JsonSerializer.Serialize(attestationProof.Metadata);
            }

            return entity;
        }
    }
}
