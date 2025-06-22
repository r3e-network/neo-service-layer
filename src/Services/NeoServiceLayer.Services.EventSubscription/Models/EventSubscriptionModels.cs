namespace NeoServiceLayer.Services.EventSubscription;

/// <summary>
/// Event subscription service statistics.
/// </summary>
public class EventSubscriptionStatistics
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TotalEvents { get; set; }
    public int RequestCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastRequestTime { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Event statistics for a subscription.
/// </summary>
public class EventStatistics
{
    public string SubscriptionId { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int EventsToday { get; set; }
    public int EventsThisWeek { get; set; }
    public int EventsThisMonth { get; set; }
    public DateTime FirstEventTime { get; set; }
    public DateTime LastEventTime { get; set; }
    public Dictionary<string, int> EventTypeDistribution { get; set; } = new();
    public double AverageEventsPerDay { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
