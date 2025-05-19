using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents a request for randomness.
    /// </summary>
    public class RandomnessRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the randomness request.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who requested the randomness.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes of randomness requested.
        /// </summary>
        public int NumBytes { get; set; }

        /// <summary>
        /// Gets or sets the purpose of the randomness request.
        /// </summary>
        public string Purpose { get; set; }

        /// <summary>
        /// Gets or sets the status of the randomness request.
        /// </summary>
        public RandomnessStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the time when the request was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the request was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the randomness in hexadecimal format.
        /// </summary>
        public string RandomnessHex { get; set; }

        /// <summary>
        /// Gets or sets the seed used to generate the randomness.
        /// </summary>
        public string Seed { get; set; }

        /// <summary>
        /// Gets or sets the proof of the randomness.
        /// </summary>
        public string Proof { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the randomness request.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Creates a new instance of the RandomnessRequest class.
        /// </summary>
        public RandomnessRequest()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            Status = RandomnessStatus.Pending;
            Metadata = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Represents the status of a randomness request.
    /// </summary>
    public enum RandomnessStatus
    {
        /// <summary>
        /// The request is pending.
        /// </summary>
        Pending,

        /// <summary>
        /// The request is being processed.
        /// </summary>
        Processing,

        /// <summary>
        /// The request has been completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The request has failed.
        /// </summary>
        Failed
    }
}
