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

/// <summary>
/// Notification request.
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

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
/// Bulk notification request.
/// </summary>
public class BulkNotificationRequest
{
    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of recipients.
    /// </summary>
    public List<string> Recipients { get; set; } = new();

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
/// Broadcast notification request.
/// </summary>
public class BroadcastNotificationRequest
{
    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the message to broadcast.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target audience.
    /// </summary>
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

/// <summary>
/// Notification subscription request.
/// </summary>
public class NotificationSubscriptionRequest
{
    /// <summary>
    /// Gets or sets the subscriber address.
    /// </summary>
    public string SubscriberAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification types to subscribe to.
    /// </summary>
    public List<string> NotificationTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the preferred channel.
    /// </summary>
    public NotificationChannel PreferredChannel { get; set; }
}

/// <summary>
/// Create notification template request.
/// </summary>
public class CreateNotificationTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Update notification template request.
/// </summary>
public class UpdateNotificationTemplateRequest
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string? Body { get; set; }
}

/// <summary>
/// Create notification channel request.
/// </summary>
public class CreateNotificationChannelRequest
{
    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel type.
    /// </summary>
    public NotificationChannel Type { get; set; }

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Update notification channel request.
/// </summary>
public class UpdateNotificationChannelRequest
{
    /// <summary>
    /// Gets or sets the channel ID.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// Unsubscribe notification request.
/// </summary>
public class UnsubscribeNotificationRequest
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for unsubscribing.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Delete notification template request.
/// </summary>
public class DeleteNotificationTemplateRequest
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for deletion.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Get notification templates request.
/// </summary>
public class GetNotificationTemplatesRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    public string? SearchQuery { get; set; }
}

/// <summary>
/// Get notification history request.
/// </summary>
public class GetNotificationHistoryRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the recipient filter.
    /// </summary>
    public string? Recipient { get; set; }
}

/// <summary>
/// Get notification status request.
/// </summary>
public class GetNotificationStatusRequest
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;
}

/// <summary>
/// Delete notification channel request.
/// </summary>
public class DeleteNotificationChannelRequest
{
    /// <summary>
    /// Gets or sets the channel ID.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for deletion.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Get notification channels request.
/// </summary>
public class GetNotificationChannelsRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;
}

/// <summary>
/// Notification statistics request.
/// </summary>
public class NotificationStatisticsRequest
{
    /// <summary>
    /// Gets or sets the start date for statistics.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for statistics.
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Notification subscription.
/// </summary>
public class NotificationSubscription
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification types.
    /// </summary>
    public List<string> NotificationTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the preferred channel.
    /// </summary>
    public NotificationChannel PreferredChannel { get; set; }

    /// <summary>
    /// Gets or sets when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Available channels result.
/// </summary>
public class AvailableChannelsResult
{
    /// <summary>
    /// Gets or sets the available channels.
    /// </summary>
    public ChannelInfo[] Channels { get; set; } = Array.Empty<ChannelInfo>();

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Channel information.
/// </summary>
public class ChannelInfo
{
    /// <summary>
    /// Gets or sets the channel ID.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel type.
    /// </summary>
    public NotificationChannel ChannelType { get; set; }

    /// <summary>
    /// Gets or sets whether the channel is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the channel configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Subscription result.
/// </summary>
public class SubscriptionResult
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Subscribe request.
/// </summary>
public class SubscribeRequest
{
    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification types to subscribe to.
    /// </summary>
    public List<string> NotificationTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the preferred channel.
    /// </summary>
    public NotificationChannel PreferredChannel { get; set; }
}

/// <summary>
/// Unsubscribe request.
/// </summary>
public class UnsubscribeRequest
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for unsubscribing.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Unsubscribe result.
/// </summary>
public class UnsubscribeResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Notification template.
/// </summary>
public class NotificationTemplate
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the template was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the template was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create template request.
/// </summary>
public class CreateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Update template request.
/// </summary>
public class UpdateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the template subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the template body.
    /// </summary>
    public string? Body { get; set; }
}

/// <summary>
/// Notification history.
/// </summary>
public class NotificationHistory
{
    /// <summary>
    /// Gets or sets the notifications.
    /// </summary>
    public NotificationResult[] Notifications { get; set; } = Array.Empty<NotificationResult>();

    /// <summary>
    /// Gets or sets the total count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }
}

/// <summary>
/// Get history request.
/// </summary>
public class GetHistoryRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the recipient filter.
    /// </summary>
    public string? Recipient { get; set; }

    /// <summary>
    /// Gets or sets the start date filter.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter.
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Broadcast request.
/// </summary>
public class BroadcastRequest
{
    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the message to broadcast.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target audience.
    /// </summary>
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

/// <summary>
/// Broadcast result.
/// </summary>
public class BroadcastResult
{
    /// <summary>
    /// Gets or sets the broadcast ID.
    /// </summary>
    public string BroadcastId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of notifications sent.
    /// </summary>
    public int NotificationsSent { get; set; }

    /// <summary>
    /// Gets or sets the number of notifications failed.
    /// </summary>
    public int NotificationsFailed { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
