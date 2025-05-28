namespace NeoServiceLayer.Services.Notification.Models;

/// <summary>
/// Notification channel enumeration.
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Email notification.
    /// </summary>
    Email,

    /// <summary>
    /// SMS notification.
    /// </summary>
    SMS,

    /// <summary>
    /// Push notification.
    /// </summary>
    Push,

    /// <summary>
    /// Webhook notification.
    /// </summary>
    Webhook,

    /// <summary>
    /// Slack notification.
    /// </summary>
    Slack,

    /// <summary>
    /// Discord notification.
    /// </summary>
    Discord,

    /// <summary>
    /// Telegram notification.
    /// </summary>
    Telegram,

    /// <summary>
    /// In-app notification.
    /// </summary>
    InApp
}

/// <summary>
/// Notification priority enumeration.
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority.
    /// </summary>
    Critical
}

/// <summary>
/// Delivery status enumeration.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Notification is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Notification is being sent.
    /// </summary>
    Sending,

    /// <summary>
    /// Notification was delivered successfully.
    /// </summary>
    Delivered,

    /// <summary>
    /// Notification delivery failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Notification was scheduled for later delivery.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Notification was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Notification attachment.
/// </summary>
public class NotificationAttachment
{
    /// <summary>
    /// Gets or sets the attachment name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attachment content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attachment data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the attachment URL (alternative to Data).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the attachment size in bytes.
    /// </summary>
    public long Size { get; set; }
}

/// <summary>
/// Send notification request.
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification priority.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Gets or sets the notification category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional attachments.
    /// </summary>
    public NotificationAttachment[] Attachments { get; set; } = Array.Empty<NotificationAttachment>();

    /// <summary>
    /// Gets or sets the scheduled delivery time.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification result.
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the notification was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message if the notification failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the sent timestamp.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery timestamp.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the channel used for delivery.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
