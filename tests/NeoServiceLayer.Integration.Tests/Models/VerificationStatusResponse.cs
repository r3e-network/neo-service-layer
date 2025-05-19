using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Integration.Tests.Models
{
    /// <summary>
    /// Represents a response containing the status of a verification.
    /// </summary>
    public class VerificationStatusResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationStatusResponse"/> class.
        /// </summary>
        public VerificationStatusResponse()
        {
            VerificationId = string.Empty;
            Status = string.Empty;
            Reason = string.Empty;
            CreatedAt = DateTime.UtcNow;
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the verification ID.
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
    }
}
