using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Services.Oracle.Models;

namespace NeoServiceLayer.Services.Oracle.Models;

/// <summary>
/// Represents a subscription to oracle data feeds.
/// </summary>
public class OracleSubscription
{
    /// <summary>
    /// Gets or sets the unique identifier for the subscription.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source identifier.
    /// </summary>
    [Required]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed identifier.
    /// </summary>
    [Required]
    public string FeedId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscriber identifier.
    /// </summary>
    [Required]
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription type.
    /// </summary>
    public SubscriptionType Type { get; set; }

    /// <summary>
    /// Gets or sets the data filters.
    /// </summary>
    public List<DataFilter> Filters { get; set; } = new();

    /// <summary>
    /// Gets or sets the update frequency in seconds.
    /// </summary>
    public int UpdateFrequencySeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the callback URL for notifications.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets or sets the webhook secret for validation.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the subscription was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the data was last updated from the source.
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets when the subscription expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the last time data was delivered.
    /// </summary>
    public DateTime? LastDeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery count.
    /// </summary>
    public long DeliveryCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the success count.
    /// </summary>
    public int SuccessCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the failure count.
    /// </summary>
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the update interval.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets additional parameters for the subscription.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the callback function for data delivery.
    /// </summary>
    public Func<string, Task>? Callback { get; set; }

    /// <summary>
    /// Gets or sets additional subscription metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the subscription configuration.
    /// </summary>
    public SubscriptionConfig Config { get; set; } = new();
}

/// <summary>
/// Represents a data filter for subscriptions.
/// </summary>
public class DataFilter
{
    /// <summary>
    /// Gets or sets the field name to filter on.
    /// </summary>
    [Required]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter operator.
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;

    /// <summary>
    /// Gets or sets the filter value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets whether the filter is case sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Represents subscription configuration.
/// </summary>
public class SubscriptionConfig
{
    /// <summary>
    /// Gets or sets whether to include historical data.
    /// </summary>
    public bool IncludeHistoricalData { get; set; } = false;

    /// <summary>
    /// Gets or sets the batch size for data delivery.
    /// </summary>
    public int BatchSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the delivery timeout in seconds.
    /// </summary>
    public int DeliveryTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to compress data.
    /// </summary>
    public bool CompressData { get; set; } = false;

    /// <summary>
    /// Gets or sets the data format.
    /// </summary>
    public DataFormat Format { get; set; } = DataFormat.Json;

    /// <summary>
    /// Gets or sets custom configuration parameters.
    /// </summary>
    public Dictionary<string, object> CustomParams { get; set; } = new();
}

/// <summary>
/// Represents subscription types.
/// </summary>
public enum SubscriptionType
{
    /// <summary>
    /// Real-time push notifications.
    /// </summary>
    Push,

    /// <summary>
    /// Polling-based subscription.
    /// </summary>
    Poll,

    /// <summary>
    /// Event-driven subscription.
    /// </summary>
    Event,

    /// <summary>
    /// Scheduled batch delivery.
    /// </summary>
    Batch
}

// Note: Additional enums may be defined in OracleSupportingTypes.cs as needed 