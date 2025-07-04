using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.EventSubscription.Models;

/// <summary>
/// Statistics for event subscription service.
/// </summary>
public class EventSubscriptionStatistics
{
    /// <summary>
    /// Gets or sets the total number of subscriptions.
    /// </summary>
    public int TotalSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the total number of events.
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Gets or sets the number of delivered events.
    /// </summary>
    public int DeliveredEvents { get; set; }

    /// <summary>
    /// Gets or sets the number of failed events.
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// Gets or sets the total request count.
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the successful request count.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the failed request count.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the last request timestamp.
    /// </summary>
    public DateTime LastRequestTime { get; set; }

    /// <summary>
    /// Gets or sets the success rate (0.0 to 1.0).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event filter configuration.
/// </summary>
public class EventFilter
{
    /// <summary>
    /// Gets or sets the filter identifier.
    /// </summary>
    public string FilterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter expression.
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter type.
    /// </summary>
    public string FilterType { get; set; } = "Address";

    /// <summary>
    /// Gets or sets whether the filter is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Webhook configuration.
/// </summary>
public class WebhookConfig
{
    /// <summary>
    /// Gets or sets the webhook identifier.
    /// </summary>
    public string WebhookId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Gets or sets the authentication headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the retry count.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether the webhook is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification delivery record.
/// </summary>
public class NotificationDelivery
{
    /// <summary>
    /// Gets or sets the delivery identifier.
    /// </summary>
    public string DeliveryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the delivery timestamp.
    /// </summary>
    public DateTime DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the retry count.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the error message if delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the response status code.
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response body.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Gets or sets the timestamp for the delivery.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event statistics for a subscription.
/// </summary>
public class EventStatistics
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of events.
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Gets or sets the number of processed events.
    /// </summary>
    public int ProcessedEvents { get; set; }

    /// <summary>
    /// Gets or sets the number of failed events.
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// Gets or sets the last event timestamp.
    /// </summary>
    public DateTime? LastEventAt { get; set; }

    /// <summary>
    /// Gets or sets the statistics generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Delivery status enumeration.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Delivery is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Delivery was successful.
    /// </summary>
    Success,

    /// <summary>
    /// Delivery failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Delivery is being retried.
    /// </summary>
    Retrying
}
