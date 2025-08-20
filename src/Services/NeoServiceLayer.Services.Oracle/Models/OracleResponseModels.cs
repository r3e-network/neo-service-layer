using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Oracle.Models;

/// <summary>
/// Response model for Oracle subscription operations.
/// </summary>
public class OracleSubscriptionResult
{
    /// <summary>
    /// Gets or sets the subscription identifier.
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
    /// Gets or sets the subscription details.
    /// </summary>
    public OracleSubscription? Subscription { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the estimated next update time.
    /// </summary>
    public DateTime? NextUpdateAt { get; set; }
}

/// <summary>
/// Response model for Oracle data retrieval.
/// </summary>
public class OracleDataResult
{
    /// <summary>
    /// Gets or sets the retrieved data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data timestamp.
    /// </summary>
    public DateTime DataTimestamp { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the data source identifier.
    /// </summary>
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data quality score.
    /// </summary>
    public double QualityScore { get; set; }

    /// <summary>
    /// Gets or sets the cryptographic proof of the data.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the response latency in milliseconds.
    /// </summary>
    public int LatencyMs { get; set; }
}

/// <summary>
/// Response model for Oracle data source operations.
/// </summary>
public class DataSourceResult
{
    /// <summary>
    /// Gets or sets the data source identifier.
    /// </summary>
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the data source details.
    /// </summary>
    public DataSource? DataSource { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response model for listing Oracle subscriptions.
/// </summary>
public class ListSubscriptionsResult
{
    /// <summary>
    /// Gets or sets the list of subscriptions.
    /// </summary>
    public List<OracleSubscription> Subscriptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of subscriptions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMorePages => (PageNumber * PageSize) < TotalCount;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for listing Oracle data sources.
/// </summary>
public class ListDataSourcesResult
{
    /// <summary>
    /// Gets or sets the list of data sources.
    /// </summary>
    public List<DataSource> DataSources { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of data sources.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMorePages => (PageNumber * PageSize) < TotalCount;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for batch Oracle operations.
/// </summary>
public class BatchOracleResult
{
    /// <summary>
    /// Gets or sets the batch identifier.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the individual request results.
    /// </summary>
    public List<OracleDataResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets whether the entire batch was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the batch operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the batch processing timestamp.
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the total processing time in milliseconds.
    /// </summary>
    public int TotalProcessingTimeMs { get; set; }
}

/// <summary>
/// Response model for Oracle subscription status.
/// </summary>
public class OracleStatusResult
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription status.
    /// </summary>
    public SubscriptionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? LastUpdateAt { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled update.
    /// </summary>
    public DateTime? NextUpdateAt { get; set; }

    /// <summary>
    /// Gets or sets the subscription metrics.
    /// </summary>
    public SubscriptionMetrics? Metrics { get; set; }

    /// <summary>
    /// Gets or sets recent activity.
    /// </summary>
    public List<SubscriptionActivity>? RecentActivity { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the status check timestamp.
    /// </summary>
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Metadata information about a data feed.
/// </summary>
public class DataFeedMetadata
{
    /// <summary>
    /// Gets or sets the feed identifier.
    /// </summary>
    public string FeedId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update frequency in seconds.
    /// </summary>
    public int UpdateFrequencySeconds { get; set; }

    /// <summary>
    /// Gets or sets the data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? LastUpdateAt { get; set; }

    /// <summary>
    /// Gets or sets the update frequency.
    /// </summary>
    public TimeSpan UpdateFrequency { get; set; }

    /// <summary>
    /// Gets or sets whether the feed is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the reliability score (0-1).
    /// </summary>
    public double ReliabilityScore { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();
}

// Note: OracleRequest, OracleResponse, and OracleBatchResponse are defined in OracleModels.cs
