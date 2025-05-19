using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models
{
    /// <summary>
    /// Represents the result of a verification process.
    /// </summary>
    public class VerificationResult
    {
        /// <summary>
        /// Gets or sets the unique identifier of the verification.
        /// </summary>
        public string VerificationId { get; set; }

        /// <summary>
        /// Gets or sets the status of the verification.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the verification was successful.
        /// </summary>
        public bool? Verified { get; set; }

        /// <summary>
        /// Gets or sets the verification score.
        /// </summary>
        public double? Score { get; set; }

        /// <summary>
        /// Gets or sets the reason for the verification result.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was processed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the verification.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the type of verification.
        /// </summary>
        public string VerificationType { get; set; }

        /// <summary>
        /// Gets or sets the encrypted identity data.
        /// </summary>
        public string IdentityData { get; set; }
    }
}
