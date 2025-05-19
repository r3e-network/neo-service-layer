using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Infrastructure.Data.Entities
{
    /// <summary>
    /// Entity class for TEE accounts.
    /// </summary>
    public class TeeAccountEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the account.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the account.
        /// </summary>
        [Required]
        public AccountType Type { get; set; }

        /// <summary>
        /// Gets or sets the public key of the account.
        /// </summary>
        [Required]
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the address of the account.
        /// </summary>
        [Required]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who owns the account.
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the time when the account was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the account was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the account.
        /// </summary>
        public string MetadataJson { get; set; }

        /// <summary>
        /// Gets or sets whether the account is exportable.
        /// </summary>
        [Required]
        public bool IsExportable { get; set; }

        /// <summary>
        /// Gets or sets the attestation proof associated with the account.
        /// </summary>
        public string AttestationProof { get; set; }

        /// <summary>
        /// Converts the entity to a domain model.
        /// </summary>
        /// <returns>The domain model.</returns>
        public TeeAccount ToDomainModel()
        {
            var teeAccount = new TeeAccount
            {
                Id = Id,
                Name = Name,
                Type = Type,
                PublicKey = PublicKey,
                Address = Address,
                UserId = UserId,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                IsExportable = IsExportable,
                AttestationProof = AttestationProof
            };

            if (!string.IsNullOrEmpty(MetadataJson))
            {
                teeAccount.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
            }
            else
            {
                teeAccount.Metadata = new Dictionary<string, object>();
            }

            return teeAccount;
        }

        /// <summary>
        /// Creates an entity from a domain model.
        /// </summary>
        /// <param name="teeAccount">The domain model.</param>
        /// <returns>The entity.</returns>
        public static TeeAccountEntity FromDomainModel(TeeAccount teeAccount)
        {
            var entity = new TeeAccountEntity
            {
                Id = teeAccount.Id,
                Name = teeAccount.Name,
                Type = teeAccount.Type,
                PublicKey = teeAccount.PublicKey,
                Address = teeAccount.Address,
                UserId = teeAccount.UserId,
                CreatedAt = teeAccount.CreatedAt,
                UpdatedAt = teeAccount.UpdatedAt,
                IsExportable = teeAccount.IsExportable,
                AttestationProof = teeAccount.AttestationProof
            };

            if (teeAccount.Metadata != null && teeAccount.Metadata.Count > 0)
            {
                entity.MetadataJson = JsonSerializer.Serialize(teeAccount.Metadata);
            }

            return entity;
        }
    }
}
