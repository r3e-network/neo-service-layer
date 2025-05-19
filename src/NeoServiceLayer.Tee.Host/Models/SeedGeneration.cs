using System;

namespace NeoServiceLayer.Tee.Host.Models
{
    /// <summary>
    /// Represents a record of a seed generation.
    /// </summary>
    public class SeedGeneration
    {
        /// <summary>
        /// Gets or sets the generated seed.
        /// </summary>
        public string Seed { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who requested the seed.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the request.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or sets the UNIX timestamp (in seconds) when the seed was generated.
        /// </summary>
        public long Timestamp { get; set; }
    }
} 