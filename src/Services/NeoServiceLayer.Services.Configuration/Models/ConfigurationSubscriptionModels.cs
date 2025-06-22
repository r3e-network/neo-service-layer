namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Configuration operation enumeration.
/// </summary>
public enum ConfigurationOperation
{
    /// <summary>
    /// Configuration was created.
    /// </summary>
    Created,

    /// <summary>
    /// Configuration was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// Configuration was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// Configuration was imported.
    /// </summary>
    Imported,

    /// <summary>
    /// Configuration was exported.
    /// </summary>
    Exported,

    /// <summary>
    /// Configuration was restored from backup.
    /// </summary>
    Restored
}

/// <summary>
/// Subscribe to changes request.
/// </summary>
public class SubscribeToChangesRequest
{
    /// <summary>
    /// Gets or sets the subscriber identifier.
    /// </summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration keys to monitor.
    /// </summary>
    public string[] ConfigurationKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the key patterns to monitor.
    /// </summary>
    public string[] KeyPatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the scopes to monitor.
    /// </summary>
    public ConfigurationScope[] Scopes { get; set; } = Array.Empty<ConfigurationScope>();

    /// <summary>
    /// Gets or sets the environments to monitor.
    /// </summary>
    public string[] Environments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the services to monitor.
    /// </summary>
    public string[] ServiceNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the notification preferences.
    /// </summary>
    public ChangeNotificationPreferences NotificationPreferences { get; set; } = new();

    /// <summary>
    /// Gets or sets the key pattern to monitor (alias for KeyPatterns).
    /// </summary>
    public string KeyPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback URL for notifications.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Change notification preferences.
/// </summary>
public class ChangeNotificationPreferences
{
    /// <summary>
    /// Gets or sets the notification channels.
    /// </summary>
    public string[] NotificationChannels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to notify on value changes.
    /// </summary>
    public bool NotifyOnValueChange { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to notify on key creation.
    /// </summary>
    public bool NotifyOnKeyCreation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to notify on key deletion.
    /// </summary>
    public bool NotifyOnKeyDeletion { get; set; } = true;

    /// <summary>
    /// Gets or sets the notification delay.
    /// </summary>
    public TimeSpan NotificationDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets whether to batch notifications.
    /// </summary>
    public bool BatchNotifications { get; set; } = false;

    /// <summary>
    /// Gets or sets the batch interval.
    /// </summary>
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Configuration subscription result.
/// </summary>
public class ConfigurationSubscriptionResult
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
    /// Gets or sets the subscription timestamp.
    /// </summary>
    public DateTime SubscribedAt { get; set; }

    /// <summary>
    /// Gets or sets the created at timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of keys being monitored.
    /// </summary>
    public int MonitoredKeysCount { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
