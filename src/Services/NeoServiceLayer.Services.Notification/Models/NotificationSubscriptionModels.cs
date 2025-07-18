﻿namespace NeoServiceLayer.Services.Notification.Models;

/// <summary>
/// Simple subscription request alias.
/// </summary>
public class SubscribeRequest : SubscribeToNotificationsRequest
{
    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the event types to subscribe to.
    /// </summary>
    public string[] EventTypes { get; set; } = Array.Empty<string>();
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
    /// Gets or sets the notification channels to subscribe to.
    /// </summary>
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Gets or sets the notification categories to subscribe to.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the subscription preferences.
    /// </summary>
    public SubscriptionPreferences Preferences { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Subscription preferences.
/// </summary>
public class SubscriptionPreferences
{
    /// <summary>
    /// Gets or sets the preferred delivery times.
    /// </summary>
    public TimeSpan[] PreferredDeliveryTimes { get; set; } = Array.Empty<TimeSpan>();

    /// <summary>
    /// Gets or sets the time zone.
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets the frequency limit.
    /// </summary>
    public FrequencyLimit FrequencyLimit { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to batch notifications.
    /// </summary>
    public bool BatchNotifications { get; set; } = false;

    /// <summary>
    /// Gets or sets the batch interval.
    /// </summary>
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets additional preferences.
    /// </summary>
    public Dictionary<string, object> AdditionalPreferences { get; set; } = new();

    /// <summary>
    /// Implicit conversion to Dictionary for compatibility.
    /// </summary>
    public static implicit operator Dictionary<string, object>(SubscriptionPreferences preferences)
    {
        var dict = new Dictionary<string, object>
        {
            ["PreferredDeliveryTimes"] = preferences.PreferredDeliveryTimes,
            ["TimeZone"] = preferences.TimeZone,
            ["FrequencyLimit"] = preferences.FrequencyLimit,
            ["BatchNotifications"] = preferences.BatchNotifications,
            ["BatchInterval"] = preferences.BatchInterval
        };

        foreach (var kvp in preferences.AdditionalPreferences)
        {
            dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }
}

/// <summary>
/// Frequency limit.
/// </summary>
public class FrequencyLimit
{
    /// <summary>
    /// Gets or sets the maximum notifications per hour.
    /// </summary>
    public int MaxPerHour { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum notifications per day.
    /// </summary>
    public int MaxPerDay { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether to enforce limits.
    /// </summary>
    public bool EnforceLimits { get; set; } = true;
}

/// <summary>
/// Notification subscription result.
/// </summary>
public class NotificationSubscriptionResult
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the subscription was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the subscription failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the subscription timestamp.
    /// </summary>
    public DateTime SubscribedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Unsubscribe request.
/// </summary>
public class UnsubscribeRequest
{
    /// <summary>
    /// Gets or sets the subscription ID to cancel.
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

/// <summary>
/// Unsubscribe result.
/// </summary>
public class UnsubscribeResult
{
    /// <summary>
    /// Gets or sets the subscription ID that was cancelled.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the unsubscribe was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the unsubscribe failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the unsubscribe timestamp.
    /// </summary>
    public DateTime UnsubscribedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
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
    /// Gets or sets the subscriber identifier.
    /// </summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification channels.
    /// </summary>
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Gets or sets the notification categories.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the subscription preferences.
    /// </summary>
    public SubscriptionPreferences Preferences { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the event types to subscribe to.
    /// </summary>
    public string[] EventTypes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Unsubscribe from notifications request.
/// </summary>
public class UnsubscribeFromNotificationsRequest
{
    /// <summary>
    /// Gets or sets the subscriber identifier.
    /// </summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories to unsubscribe from (empty means all).
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the reason for unsubscribing.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
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
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the active subscriptions count.
    /// </summary>
    public int ActiveSubscriptionsCount { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the subscription that was created.
    /// </summary>
    public NotificationSubscription? Subscription { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
