using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Storage.Models;

/// <summary>
/// Request to store data in the storage system.
/// </summary>
public class StoreDataRequest
{
    /// <summary>
    /// Gets or sets the unique key for the data.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data to store (base64 encoded).
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type of the data.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets whether the data should be encrypted.
    /// </summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the data should be compressed.
    /// </summary>
    public bool Compress { get; set; } = true;

    /// <summary>
    /// Gets or sets the expiration time for the data.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets custom metadata for the data.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets access control settings.
    /// </summary>
    public List<string> AccessControlList { get; set; } = new();
}

/// <summary>
/// Request to retrieve data from the storage system.
/// </summary>
public class RetrieveDataRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the data.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to decrypt the data if it's encrypted.
    /// </summary>
    public bool Decrypt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to decompress the data if it's compressed.
    /// </summary>
    public bool Decompress { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include metadata in the response.
    /// </summary>
    public bool IncludeMetadata { get; set; } = false;
}

/// <summary>
/// Request to delete data from the storage system.
/// </summary>
public class DeleteDataRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the data.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to perform a soft delete (mark as deleted but keep data).
    /// </summary>
    public bool SoftDelete { get; set; } = false;

    /// <summary>
    /// Gets or sets the reason for deletion (for audit purposes).
    /// </summary>
    public string? DeletionReason { get; set; }
}

/// <summary>
/// Request to update existing data in the storage system.
/// </summary>
public class UpdateDataRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the data.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new data content (base64 encoded).
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the updated content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets updated metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets updated expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets updated access control list.
    /// </summary>
    public List<string>? AccessControlList { get; set; }
}

/// <summary>
/// Request to get metadata for stored data.
/// </summary>
public class GetMetadataRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the data.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include extended metadata.
    /// </summary>
    public bool IncludeExtended { get; set; } = false;
}

/// <summary>
/// Request to search for data in the storage system.
/// </summary>
public class SearchDataRequest
{
    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the key prefix to search for.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the content type filter.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets metadata filters.
    /// </summary>
    public Dictionary<string, string> MetadataFilters { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation date range filter.
    /// </summary>
    public DateTimeRange? CreatedRange { get; set; }

    /// <summary>
    /// Gets or sets the size range filter in bytes.
    /// </summary>
    public SizeRange? SizeRange { get; set; }

    /// <summary>
    /// Gets or sets the page size for results.
    /// </summary>
    [Range(1, 1000)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortField { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Request to list data entries.
/// </summary>
public class ListDataRequest
{
    /// <summary>
    /// Gets or sets the key prefix filter.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Range(1, 1000)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to include metadata in the response.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortField { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Request to store multiple data items in a batch operation.
/// </summary>
public class BatchStoreDataRequest
{
    /// <summary>
    /// Gets or sets the list of store data requests.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<StoreDataRequest> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the batch operation should be atomic.
    /// </summary>
    public bool Atomic { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to continue on errors (if not atomic).
    /// </summary>
    public bool ContinueOnError { get; set; } = false;
}

/// <summary>
/// Request to get storage statistics.
/// </summary>
public class StorageStatisticsRequest
{
    /// <summary>
    /// Gets or sets whether to include detailed statistics.
    /// </summary>
    public bool IncludeDetailed { get; set; } = false;

    /// <summary>
    /// Gets or sets the time range for statistics.
    /// </summary>
    public DateTimeRange? TimeRange { get; set; }

    /// <summary>
    /// Gets or sets the data grouping for statistics.
    /// </summary>
    public StatisticsGrouping Grouping { get; set; } = StatisticsGrouping.All;
}

/// <summary>
/// Request to begin a storage transaction.
/// </summary>
public class BeginTransactionRequest
{
    /// <summary>
    /// Gets or sets the transaction name or description.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the isolation level for the transaction.
    /// </summary>
    public TransactionIsolationLevel IsolationLevel { get; set; } = TransactionIsolationLevel.ReadCommitted;

    /// <summary>
    /// Gets or sets the timeout for the transaction in seconds.
    /// </summary>
    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether the transaction should auto-commit on success.
    /// </summary>
    public bool AutoCommit { get; set; } = false;
}

/// <summary>
/// Request to commit a storage transaction.
/// </summary>
public class CommitTransactionRequest
{
    /// <summary>
    /// Gets or sets the transaction identifier.
    /// </summary>
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to force commit even if there are warnings.
    /// </summary>
    public bool ForceCommit { get; set; } = false;
}

/// <summary>
/// Request to rollback a storage transaction.
/// </summary>
public class RollbackTransactionRequest
{
    /// <summary>
    /// Gets or sets the transaction identifier.
    /// </summary>
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for rollback.
    /// </summary>
    public string? Reason { get; set; }
}
