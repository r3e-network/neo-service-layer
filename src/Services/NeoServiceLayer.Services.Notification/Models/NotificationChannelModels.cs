using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Notification.Models;

/// <summary>
/// Multi-channel notification request.
/// </summary>
public class MultiChannelNotificationRequest
{
    /// <summary>
    /// Gets or sets the notification channels to use.
    /// </summary>
    public List<NotificationChannel> Channels { get; set; } = new();

    /// <summary>
    /// Gets or sets the recipients for each channel.
    /// </summary>
    public Dictionary<NotificationChannel, List<string>> Recipients { get; set; } = new();

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
    /// Gets or sets whether to stop on first failure.
    /// </summary>
    public bool StopOnFirstFailure { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Multi-channel notification result.
/// </summary>
public class MultiChannelNotificationResult
{
    /// <summary>
    /// Gets or sets the batch notification ID.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the individual notification results.
    /// </summary>
    public List<NotificationResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets whether all notifications were successful.
    /// </summary>
    public bool AllSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the number of successful notifications.
    /// </summary>
    public int SuccessfulCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed notifications.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Register channel request.
/// </summary>
public class RegisterChannelRequest
{
    /// <summary>
    /// Gets or sets the channel type.
    /// </summary>
    public NotificationChannel ChannelType { get; set; }

    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the channel is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the channel description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Channel registration result.
/// </summary>
public class ChannelRegistrationResult
{
    /// <summary>
    /// Gets or sets the channel ID.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the registration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if registration failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the registration timestamp.
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Subscribe to notifications request.
/// </summary>
public class SubscribeToNotificationsRequest
{
    /// <summary>
    /// Gets or sets the subscriber identifier.
    /// </summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification types to subscribe to.
    /// </summary>
    public List<string> NotificationTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the preferred channels.
    /// </summary>
    public List<NotificationChannel> Channels { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Unsubscribe from notifications request.
/// </summary>
public class UnsubscribeFromNotificationsRequest
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for unsubscribing.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
