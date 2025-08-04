using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a notification template in the database.
    /// </summary>
    public class NotificationTemplate
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the template name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the template type (email, sms, push).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subject template.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the body template.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the template variables as JSON.
        /// </summary>
        public string? Variables { get; set; }

        /// <summary>
        /// Gets or sets whether the template is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets when the template was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the template was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
