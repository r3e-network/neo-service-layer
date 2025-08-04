using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents an API key in the database.
    /// </summary>
    public class ApiKey
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key value.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the permissions as JSON.
        /// </summary>
        public string? Permissions { get; set; }

        /// <summary>
        /// Gets or sets the rate limit per minute.
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 100;

        /// <summary>
        /// Gets or sets when the key was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the key was last used.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets when the key expires.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
