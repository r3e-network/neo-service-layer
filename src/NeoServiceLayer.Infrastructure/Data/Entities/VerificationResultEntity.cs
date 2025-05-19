using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Infrastructure.Data.Entities
{
    /// <summary>
    /// Entity class for verification results.
    /// </summary>
    public class VerificationResultEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the verification result.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status of the verification.
        /// </summary>
        [Required]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the verification type.
        /// </summary>
        [Required]
        public string VerificationType { get; set; }

        /// <summary>
        /// Gets or sets the encrypted identity data.
        /// </summary>
        [Required]
        public string IdentityData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the verification was successful.
        /// </summary>
        public bool? Verified { get; set; }

        /// <summary>
        /// Gets or sets the verification score.
        /// </summary>
        public double? Score { get; set; }

        /// <summary>
        /// Gets or sets the verification reason.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was processed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the verification.
        /// </summary>
        public string MetadataJson { get; set; }

        /// <summary>
        /// Converts the entity to a domain model.
        /// </summary>
        /// <returns>The domain model.</returns>
        public Shared.Models.VerificationResult ToDomainModel()
        {
            var verificationResult = new Shared.Models.VerificationResult
            {
                VerificationId = Id,
                Status = Status,
                Verified = Verified,
                Score = Score,
                Reason = Reason,
                CreatedAt = CreatedAt,
                ProcessedAt = ProcessedAt,
                VerificationType = VerificationType,
                IdentityData = IdentityData
            };

            if (!string.IsNullOrEmpty(MetadataJson))
            {
                verificationResult.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
            }
            else
            {
                verificationResult.Metadata = new Dictionary<string, object>();
            }

            return verificationResult;
        }

        /// <summary>
        /// Creates an entity from a domain model.
        /// </summary>
        /// <param name="verificationResult">The domain model.</param>
        /// <param name="verificationType">The verification type.</param>
        /// <param name="identityData">The encrypted identity data.</param>
        /// <returns>The entity.</returns>
        public static VerificationResultEntity FromDomainModel(Shared.Models.VerificationResult verificationResult, string verificationType, string identityData)
        {
            var entity = new VerificationResultEntity
            {
                Id = verificationResult.VerificationId,
                Status = verificationResult.Status,
                VerificationType = verificationType,
                IdentityData = identityData,
                Verified = verificationResult.Verified,
                Score = verificationResult.Score,
                Reason = verificationResult.Reason,
                CreatedAt = verificationResult.CreatedAt,
                ProcessedAt = verificationResult.ProcessedAt
            };

            if (verificationResult.Metadata != null && verificationResult.Metadata.Count > 0)
            {
                entity.MetadataJson = JsonSerializer.Serialize(verificationResult.Metadata);
            }

            return entity;
        }
    }
}
