namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Represents a subscription to an oracle feed.
/// </summary>
public class OracleSubscription
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed ID.
    /// </summary>
    public string FeedId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the callback to invoke when the feed is updated.
    /// </summary>
    public Func<string, Task> Callback { get; set; } = _ => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the update interval.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the success count.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the failure count.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the expiration timestamp.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether the subscription is active.
    /// </summary>
    public bool IsActive => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
}
