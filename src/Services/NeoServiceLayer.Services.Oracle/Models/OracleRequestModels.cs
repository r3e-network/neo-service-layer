using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Oracle.Models;

/// <summary>
/// Request model for Oracle subscription operations.
/// </summary>
public class OracleSubscriptionRequest
{
    /// <summary>
    /// Gets or sets the data source identifier to subscribe to.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string SubscriptionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update interval for the subscription.
    /// </summary>
    [Range(1, 86400)] // 1 second to 24 hours in seconds
    public int UpdateIntervalSeconds { get; set; } = 300; // 5 minutes default

    /// <summary>
    /// Gets or sets the webhook URL to call when data is updated.
    /// </summary>
    [Url]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Gets or sets the subscription expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the data filtering criteria.
    /// </summary>
    public Dictionary<string, string> Filters { get; set; } = new();

    /// <summary>
    /// Gets or sets subscription parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include historical data.
    /// </summary>
    public bool IncludeHistoricalData { get; set; } = false;

    /// <summary>
    /// Gets or sets the priority level.
    /// </summary>
    [Range(1, 10)]
    public int Priority { get; set; } = 5;
}

/// <summary>
/// Request model for Oracle unsubscribe operations.
/// </summary>
public class OracleUnsubscribeRequest
{
    /// <summary>
    /// Gets or sets the subscription identifier to cancel.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for unsubscribing.
    /// </summary>
    [StringLength(200)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets whether to immediately stop data delivery.
    /// </summary>
    public bool ImmediateStop { get; set; } = true;
}

/// <summary>
/// Request model for Oracle data retrieval.
/// </summary>
public class OracleDataRequest
{
    /// <summary>
    /// Gets or sets the data source identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific data path to retrieve.
    /// </summary>
    public string? DataPath { get; set; }

    /// <summary>
    /// Gets or sets query parameters for the data request.
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to fetch the latest data.
    /// </summary>
    public bool FetchLatest { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum age of acceptable data.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the data request.
    /// </summary>
    [Range(1000, 60000)]
    public int TimeoutMs { get; set; } = 10000;
}

/// <summary>
/// Request model for creating Oracle data sources.
/// </summary>
public class CreateDataSourceRequest
{
    /// <summary>
    /// Gets or sets the data source name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source description.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source URL.
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source type.
    /// </summary>
    [Required]
    public DataSourceType SourceType { get; set; }

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public AuthenticationConfig? Authentication { get; set; }

    /// <summary>
    /// Gets or sets the update frequency.
    /// </summary>
    [Range(1, 86400)]
    public int UpdateFrequencySeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the data format.
    /// </summary>
    public string DataFormat { get; set; } = "JSON";

    /// <summary>
    /// Gets or sets the data extraction path.
    /// </summary>
    public string? ExtractionPath { get; set; }

    /// <summary>
    /// Gets or sets validation rules.
    /// </summary>
    public List<ValidationRule> ValidationRules { get; set; } = new();

    /// <summary>
    /// Gets or sets custom headers for requests.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the data source is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Request model for updating Oracle data sources.
/// </summary>
public class UpdateDataSourceRequest
{
    /// <summary>
    /// Gets or sets the data source identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated name.
    /// </summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the updated description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated URL.
    /// </summary>
    [Url]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the updated authentication configuration.
    /// </summary>
    public AuthenticationConfig? Authentication { get; set; }

    /// <summary>
    /// Gets or sets the updated update frequency.
    /// </summary>
    [Range(1, 86400)]
    public int? UpdateFrequencySeconds { get; set; }

    /// <summary>
    /// Gets or sets the updated data format.
    /// </summary>
    public string? DataFormat { get; set; }

    /// <summary>
    /// Gets or sets the updated extraction path.
    /// </summary>
    public string? ExtractionPath { get; set; }

    /// <summary>
    /// Gets or sets updated validation rules.
    /// </summary>
    public List<ValidationRule>? ValidationRules { get; set; }

    /// <summary>
    /// Gets or sets updated custom headers.
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>
    /// Gets or sets whether the data source is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets updated tags.
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request model for deleting Oracle data sources.
/// </summary>
public class DeleteDataSourceRequest
{
    /// <summary>
    /// Gets or sets the data source identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string DataSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for deletion.
    /// </summary>
    [StringLength(200)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets whether to perform a soft delete.
    /// </summary>
    public bool SoftDelete { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to force delete even if subscriptions exist.
    /// </summary>
    public bool Force { get; set; } = false;
}

/// <summary>
/// Request model for listing Oracle subscriptions.
/// </summary>
public class ListSubscriptionsRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the data source filter.
    /// </summary>
    public string? DataSourceId { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public SubscriptionStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the creation date range filter.
    /// </summary>
    public DateRange? CreatedRange { get; set; }

    /// <summary>
    /// Gets or sets the search query for subscription names.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortField { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public string SortDirection { get; set; } = "DESC";
}

/// <summary>
/// Request model for listing Oracle data sources.
/// </summary>
public class ListDataSourcesRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the source type filter.
    /// </summary>
    public DataSourceType? SourceType { get; set; }

    /// <summary>
    /// Gets or sets the enabled status filter.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the tag filter.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the search query for data source names.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortField { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public string SortDirection { get; set; } = "DESC";
}

/// <summary>
/// Request model for batch Oracle operations.
/// </summary>
public class BatchOracleRequest
{
    /// <summary>
    /// Gets or sets the list of individual Oracle requests.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public List<OracleDataRequest> Requests { get; set; } = new();

    /// <summary>
    /// Gets or sets the batch name.
    /// </summary>
    [StringLength(100)]
    public string? BatchName { get; set; }

    /// <summary>
    /// Gets or sets whether the batch should be processed atomically.
    /// </summary>
    public bool Atomic { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum parallel requests.
    /// </summary>
    [Range(1, 10)]
    public int MaxParallelRequests { get; set; } = 3;

    /// <summary>
    /// Gets or sets the overall timeout for the batch.
    /// </summary>
    [Range(5000, 300000)]
    public int BatchTimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets whether to continue on individual request failures.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;
}

/// <summary>
/// Request model for getting Oracle subscription status.
/// </summary>
public class OracleStatusRequest
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include detailed metrics.
    /// </summary>
    public bool IncludeMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include recent activity.
    /// </summary>
    public bool IncludeActivity { get; set; } = false;

    /// <summary>
    /// Gets or sets the activity time range in hours.
    /// </summary>
    [Range(1, 168)] // 1 hour to 1 week
    public int ActivityRangeHours { get; set; } = 24;
}
