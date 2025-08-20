using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Notification.Models;

/// <summary>
/// Notification channel enumeration.
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Email notification.
    /// </summary>
    Email = 0,

    /// <summary>
    /// SMS notification.
    /// </summary>
    SMS = 1,

    /// <summary>
    /// Push notification.
    /// </summary>
    Push = 2,

    /// <summary>
    /// Webhook notification.
    /// </summary>
    Webhook = 3,

    /// <summary>
    /// Slack notification.
    /// </summary>
    Slack = 4,

    /// <summary>
    /// Discord notification.
    /// </summary>
    Discord = 5,

    /// <summary>
    /// Telegram notification.
    /// </summary>
    Telegram = 6,

    /// <summary>
    /// In-app notification.
    /// </summary>
    InApp = 7,

    /// <summary>
    /// Blockchain notification.
    /// </summary>
    Blockchain = 8
}

/// <summary>
/// Notification priority enumeration.
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Delivery status enumeration.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Notification is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification is being sent.
    /// </summary>
    Sending = 1,

    /// <summary>
    /// Notification was delivered successfully.
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Notification delivery failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Notification was scheduled for later delivery.
    /// </summary>
    Scheduled = 4,

    /// <summary>
    /// Notification was cancelled.
    /// </summary>
    Cancelled = 5
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
    public byte[]? Data { get; set; }

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
    public List<NotificationAttachment> Attachments { get; set; } = new();

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
    public NotificationChannel? Channel { get; set; }

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string? Recipient { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the message associated with the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the operation.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }
}

/// <summary>
/// Broadcast notification request.
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
    /// Gets or sets the recipients.
    /// </summary>
    public IEnumerable<string> Recipients { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
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
    /// Gets or sets whether the broadcast was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the total number of recipients.
    /// </summary>
    public int TotalRecipients { get; set; }

    /// <summary>
    /// Gets or sets the number of successful deliveries.
    /// </summary>
    public int SuccessfulDeliveries { get; set; }

    /// <summary>
    /// Gets or sets the number of failed deliveries.
    /// </summary>
    public int FailedDeliveries { get; set; }

    /// <summary>
    /// Gets or sets the individual notification results.
    /// </summary>
    public List<NotificationResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the broadcast timestamp.
    /// </summary>
    public DateTime BroadcastAt { get; set; }
}

/// <summary>
/// Notification subscription request.
/// </summary>
public class SubscribeRequest
{
    /// <summary>
    /// Gets or sets the subscriber address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification types to subscribe to.
    /// </summary>
    public List<string> EventTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the preferred channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }
}

/// <summary>
/// Create notification template request.
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
    /// Gets or sets the template channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }
}

/// <summary>
/// Update notification template request.
/// </summary>
public class UpdateTemplateRequest
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
    /// Gets or sets the template channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }
}

/// <summary>
/// Unsubscribe notification request.
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
/// Get notification history request.
/// </summary>
public class GetHistoryRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the recipient filter.
    /// </summary>
    public string? RecipientFilter { get; set; }
}

/// <summary>
/// Notification history.
/// </summary>
public class NotificationHistory
{
    /// <summary>
    /// Gets or sets the total count of notifications.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the list of notifications.
    /// </summary>
    public List<NotificationResult> Notifications { get; set; } = new();

    /// <summary>
    /// Gets or sets when the history was retrieved.
    /// </summary>
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Unsubscribe result.
/// </summary>
public class UnsubscribeResult
{
    /// <summary>
    /// Gets or sets whether the unsubscribe was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the unsubscribe occurred.
    /// </summary>
    public DateTime UnsubscribedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if unsubscribe failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
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
    /// Gets or sets the error message if the operation failed.
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
/// Privacy-preserving notification result.
/// </summary>
public class NotificationPrivacyResult
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery proof.
    /// </summary>
    public DeliveryProof DeliveryProof { get; set; } = new();
}

/// <summary>
/// Delivery proof for privacy-preserving notifications.
/// </summary>
public class DeliveryProof
{
    /// <summary>
    /// Gets or sets the cryptographic proof.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proof timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Internal notification template.
/// </summary>
public class InternalNotificationTemplate
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
    /// Gets or sets the template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template variables.
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// Notification service configuration options.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Gets or sets the enabled notification channels.
    /// </summary>
    public string[] EnabledChannels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the batch size for processing notifications.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum queue size.
    /// </summary>
    public int MaxQueueSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the processing interval in seconds.
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;
}
/// <summary>
/// Request to broadcast notification to multiple recipients.
/// </summary>
public class BroadcastNotificationRequest
{
    /// <summary>
    /// Gets or sets the notification channels.
    /// </summary>
    public List<NotificationChannel> Channels { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the recipient list.
    /// </summary>
    public List<string> Recipients { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the notification subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the notification body.
    /// </summary>
    public string Body { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the notification metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request to create notification channel.
/// </summary>
public class CreateNotificationChannelRequest
{
    /// <summary>
    /// Gets or sets the channel type.
    /// </summary>
    public NotificationChannel ChannelType { get; set; }
    
    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the channel configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
    
    /// <summary>
    /// Gets or sets whether the channel is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Request to update notification channel.
/// </summary>
public class UpdateNotificationChannelRequest
{
    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the channel configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
    
    /// <summary>
    /// Gets or sets whether the channel is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Request to create notification template.
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
    /// Gets or sets the template variables.
    /// </summary>
    public List<string> Variables { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the supported channels.
    /// </summary>
    public List<NotificationChannel> SupportedChannels { get; set; } = new();
}

