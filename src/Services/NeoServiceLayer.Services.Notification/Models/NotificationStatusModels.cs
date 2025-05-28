namespace NeoServiceLayer.Services.Notification.Models;

/// <summary>
/// Notification status request.
/// </summary>
public class NotificationStatusRequest
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification status result.
/// </summary>
public class NotificationStatusResult
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the delivery attempts.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the last attempt timestamp.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Gets or sets the next retry timestamp.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery details.
    /// </summary>
    public DeliveryDetails Details { get; set; } = new();

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
/// Delivery details.
/// </summary>
public class DeliveryDetails
{
    /// <summary>
    /// Gets or sets the channel used.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the recipient.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sent timestamp.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gets or sets the delivered timestamp.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery response.
    /// </summary>
    public string? DeliveryResponse { get; set; }

    /// <summary>
    /// Gets or sets additional delivery metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Delivery statistics request.
/// </summary>
public class DeliveryStatisticsRequest
{
    /// <summary>
    /// Gets or sets the time range for statistics.
    /// </summary>
    public TimeSpan TimeRange { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the channel filter.
    /// </summary>
    public NotificationChannel? ChannelFilter { get; set; }

    /// <summary>
    /// Gets or sets whether to include detailed statistics.
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Delivery statistics result.
/// </summary>
public class DeliveryStatisticsResult
{
    /// <summary>
    /// Gets or sets the overall statistics.
    /// </summary>
    public OverallDeliveryStatistics Overall { get; set; } = new();

    /// <summary>
    /// Gets or sets the channel-specific statistics.
    /// </summary>
    public ChannelDeliveryStatistics[] ChannelStatistics { get; set; } = Array.Empty<ChannelDeliveryStatistics>();

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
/// Overall delivery statistics.
/// </summary>
public class OverallDeliveryStatistics
{
    /// <summary>
    /// Gets or sets the total notifications sent.
    /// </summary>
    public int TotalSent { get; set; }

    /// <summary>
    /// Gets or sets the total notifications delivered.
    /// </summary>
    public int TotalDelivered { get; set; }

    /// <summary>
    /// Gets or sets the total notifications failed.
    /// </summary>
    public int TotalFailed { get; set; }

    /// <summary>
    /// Gets or sets the delivery success rate.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average delivery time.
    /// </summary>
    public TimeSpan AverageDeliveryTime { get; set; }

    /// <summary>
    /// Gets or sets additional statistics.
    /// </summary>
    public Dictionary<string, object> AdditionalStatistics { get; set; } = new();
}

/// <summary>
/// Channel delivery statistics.
/// </summary>
public class ChannelDeliveryStatistics
{
    /// <summary>
    /// Gets or sets the channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the number sent via this channel.
    /// </summary>
    public int Sent { get; set; }

    /// <summary>
    /// Gets or sets the number delivered via this channel.
    /// </summary>
    public int Delivered { get; set; }

    /// <summary>
    /// Gets or sets the number failed via this channel.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets the success rate for this channel.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average delivery time for this channel.
    /// </summary>
    public TimeSpan AverageDeliveryTime { get; set; }
}
