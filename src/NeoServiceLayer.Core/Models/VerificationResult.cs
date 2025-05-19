using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents the result of a verification process.
    /// </summary>
    public class VerificationResult
    {
        /// <summary>
        /// Gets or sets the unique identifier of the verification result.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the verification was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the reason for the verification result.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the verification details.
        /// </summary>
        public Dictionary<string, object> Details { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was performed.
        /// </summary>
        public DateTime VerifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Creates a new instance of the VerificationResult class.
        /// </summary>
        public VerificationResult()
        {
            Id = Guid.NewGuid().ToString();
            Details = new Dictionary<string, object>();
            VerifiedAt = DateTime.UtcNow;
            ExpiresAt = VerifiedAt.AddDays(1);
        }
    }
}
