using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents an audit log entry in the database.
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who performed the action.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the action performed.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity ID.
        /// </summary>
        public string? EntityId { get; set; }

        /// <summary>
        /// Gets or sets the old values as JSON.
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// Gets or sets the new values as JSON.
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets when the action occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
