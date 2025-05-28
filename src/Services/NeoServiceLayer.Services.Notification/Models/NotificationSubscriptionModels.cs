namespace NeoServiceLayer.Services.Notification.Models;

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
    /// Gets or sets the event types to subscribe to.
    /// </summary>
    public string[] EventTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the preferred notification channels.
    /// </summary>
    public NotificationChannel[] PreferredChannels { get; set; } = Array.Empty<NotificationChannel>();

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
    /// Gets or sets the subscription filters.
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();

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
    /// Gets or sets the subscriber identifier.
    /// </summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event types to unsubscribe from (empty means all).
    /// </summary>
    public string[] EventTypes { get; set; } = Array.Empty<string>();

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
    /// Gets or sets the count of active subscriptions.
    /// </summary>
    public int ActiveSubscriptionsCount { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification subscription internal model.
/// </summary>
public class NotificationSubscription
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscriber ID.
    /// </summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event types.
    /// </summary>
    public string[] EventTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the preferred channels.
    /// </summary>
    public NotificationChannel[] PreferredChannels { get; set; } = Array.Empty<NotificationChannel>();

    /// <summary>
    /// Gets or sets the subscription preferences.
    /// </summary>
    public SubscriptionPreferences Preferences { get; set; } = new();

    /// <summary>
    /// Gets or sets the filters.
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

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
