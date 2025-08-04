using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a notification log entry in the database.
    /// </summary>
    public class NotificationLog
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the notification type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipient.
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets when the notification was sent.
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// Gets or sets when the notification was delivered.
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string? Metadata { get; set; }
    }
}
