using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Services.Storage.Models;

/// <summary>
/// Sort direction enumeration.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending sort order.
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending sort order.
    /// </summary>
    Descending
}

/// <summary>
/// Date time range for filtering operations.
/// </summary>
public class DateTimeRange
{
    /// <summary>
    /// Gets or sets the start date and time.
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// Gets or sets the end date and time.
    /// </summary>
    public DateTime? End { get; set; }

    /// <summary>
    /// Gets a value indicating whether this range is valid.
    /// </summary>
    public bool IsValid => !Start.HasValue || !End.HasValue || Start.Value <= End.Value;
}

/// <summary>
/// Transaction isolation level enumeration.
/// </summary>
public enum TransactionIsolationLevel
{
    /// <summary>
    /// Read uncommitted isolation level.
    /// </summary>
    ReadUncommitted,

    /// <summary>
    /// Read committed isolation level.
    /// </summary>
    ReadCommitted,

    /// <summary>
    /// Repeatable read isolation level.
    /// </summary>
    RepeatableRead,

    /// <summary>
    /// Serializable isolation level.
    /// </summary>
    Serializable
}

/// <summary>
/// Statistics grouping enumeration.
/// </summary>
public enum StatisticsGrouping
{
    /// <summary>
    /// All statistics combined.
    /// </summary>
    All,

    /// <summary>
    /// Group by hour.
    /// </summary>
    Hour,

    /// <summary>
    /// Group by day.
    /// </summary>
    Day,

    /// <summary>
    /// Group by week.
    /// </summary>
    Week,

    /// <summary>
    /// Group by month.
    /// </summary>
    Month,

    /// <summary>
    /// Group by year.
    /// </summary>
    Year
}

/// <summary>
/// Storage usage information.
/// </summary>
public class StorageUsage
{
    /// <summary>
    /// Gets or sets the total storage used in bytes.
    /// </summary>
    public long TotalBytesUsed { get; set; }

    /// <summary>
    /// Gets or sets the total storage available in bytes.
    /// </summary>
    public long TotalBytesAvailable { get; set; }

    /// <summary>
    /// Gets or sets the number of stored items.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the storage usage by content type.
    /// </summary>
    public Dictionary<string, long> UsageByContentType { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of items stored.
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the storage usage by encryption status.
    /// </summary>
    public Dictionary<string, long> UsageByEncryption { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of compressed items.
    /// </summary>
    public int CompressedItems { get; set; }

    /// <summary>
    /// Gets or sets the number of encrypted items.
    /// </summary>
    public int EncryptedItems { get; set; }

    /// <summary>
    /// Gets or sets the number of chunked items.
    /// </summary>
    public int ChunkedItems { get; set; }

    /// <summary>
    /// Gets or sets the available space in bytes.
    /// </summary>
    public long AvailableSpaceBytes { get; set; }

    /// <summary>
    /// Gets or sets the used space in bytes.
    /// </summary>
    public long UsedSpaceBytes { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the percentage of storage used.
    /// </summary>
    public double UsagePercentage => TotalBytesAvailable > 0 ? (double)TotalBytesUsed / TotalBytesAvailable * 100 : 0;
}

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
}

/// <summary>
/// Represents a size range filter.
/// </summary>
public class SizeRange
{
    /// <summary>
    /// Gets or sets the minimum size in bytes.
    /// </summary>
    public long? MinSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum size in bytes.
    /// </summary>
    public long? MaxSize { get; set; }

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

/// <summary>
/// Storage system statistics.
/// </summary>
public class StorageStatistics
{
    /// <summary>
    /// Gets or sets the total number of stored items.
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the total storage size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the average item size in bytes.
    /// </summary>
    public double AverageItemSize { get; set; }

    /// <summary>
    /// Gets or sets the cache hit ratio.
    /// </summary>
    public double CacheHitRatio { get; set; }

    /// <summary>
    /// Gets or sets the number of operations performed.
    /// </summary>
    public long OperationCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when statistics were collected.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the total number of requests.
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successful operations.
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed operations.
    /// </summary>
    public long FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last request.
    /// </summary>
    public DateTime LastRequestTime { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate as a percentage (0-100).
    /// </summary>
    public double CacheHitRate { get; set; }
}
