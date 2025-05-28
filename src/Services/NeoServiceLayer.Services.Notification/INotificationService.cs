using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Interface for the Notification Service that provides multi-channel notification capabilities.
/// </summary>
public interface INotificationService : IService
{
    /// <summary>
    /// Sends a notification through the specified channel.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification result.</returns>
    Task<NotificationResult> SendNotificationAsync(SendNotificationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Sends notifications to multiple channels simultaneously.
    /// </summary>
    /// <param name="request">The multi-channel notification request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The multi-channel notification result.</returns>
    Task<MultiChannelNotificationResult> SendMultiChannelNotificationAsync(MultiChannelNotificationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Registers a new notification channel.
    /// </summary>
    /// <param name="request">The channel registration request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The channel registration result.</returns>
    Task<ChannelRegistrationResult> RegisterChannelAsync(RegisterChannelRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a notification.
    /// </summary>
    /// <param name="request">The notification status request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification status.</returns>
    Task<NotificationStatusResult> GetNotificationStatusAsync(NotificationStatusRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a notification template.
    /// </summary>
    /// <param name="request">The template creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The template creation result.</returns>
    Task<TemplateResult> CreateTemplateAsync(CreateTemplateRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Sends a notification using a template.
    /// </summary>
    /// <param name="request">The template notification request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification result.</returns>
    Task<NotificationResult> SendTemplateNotificationAsync(SendTemplateNotificationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets notification history.
    /// </summary>
    /// <param name="request">The notification history request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The notification history.</returns>
    Task<NotificationHistoryResult> GetNotificationHistoryAsync(NotificationHistoryRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Subscribes to notifications for specific events.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The subscription result.</returns>
    Task<SubscriptionResult> SubscribeToNotificationsAsync(SubscribeToNotificationsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Unsubscribes from notifications.
    /// </summary>
    /// <param name="request">The unsubscription request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The unsubscription result.</returns>
    Task<SubscriptionResult> UnsubscribeFromNotificationsAsync(UnsubscribeFromNotificationsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets available notification channels.
    /// </summary>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The available channels.</returns>
    Task<AvailableChannelsResult> GetAvailableChannelsAsync(BlockchainType blockchainType);
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
