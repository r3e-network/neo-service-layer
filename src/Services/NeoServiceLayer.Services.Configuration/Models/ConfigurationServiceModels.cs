namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Configuration statistics.
/// </summary>
public class ConfigurationStatistics
{
    public int TotalConfigurations { get; set; }
    public int ActiveSubscriptions { get; set; }
    public DateTime LastModified { get; set; }
    public Dictionary<string, int> ConfigurationsByType { get; set; } = new();
}

/// <summary>
/// Batch configuration operation result.
/// </summary>
public class BatchConfigurationResult
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulUpdates { get; set; }
    public int FailedUpdates { get; set; }
    public List<ConfigurationSetResult> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Configuration unsubscribe result.
/// </summary>
public class ConfigurationUnsubscribeResult
{
    public string SubscriptionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime RemovedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Configuration subscription.
/// </summary>
public class ConfigurationSubscription
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public string SubscriberEndpoint { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastNotified { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration subscription list result.
/// </summary>
public class ConfigurationSubscriptionListResult
{
    public ConfigurationSubscription[] Subscriptions { get; set; } = Array.Empty<ConfigurationSubscription>();
    public int TotalCount { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Subscription statistics.
/// </summary>
public class SubscriptionStatistics
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int InactiveSubscriptions { get; set; }
    public DateTime? OldestSubscription { get; set; }
    public DateTime? NewestSubscription { get; set; }
}

/// <summary>
/// Extended configuration import result.
/// </summary>
public partial class ConfigurationImportResult
{
    public int ErrorCount { get; set; }
}

/// <summary>
/// Extended configuration export result.
/// </summary>
public partial class ConfigurationExportResult
{
    // ConfigurationCount is already defined in the main class
}
