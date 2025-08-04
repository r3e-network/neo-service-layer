using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a rate limit rule in the database.
    /// </summary>
    public class RateLimitRule
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the rule name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the endpoint pattern.
        /// </summary>
        public string EndpointPattern { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the requests per minute limit.
        /// </summary>
        public int RequestsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets the requests per hour limit.
        /// </summary>
        public int RequestsPerHour { get; set; }

        /// <summary>
        /// Gets or sets whether the rule is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets when the rule was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the rule was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
