namespace NeoServiceLayer.Services.Storage.Models;

/// <summary>
/// Response from storing data.
/// </summary>
public class StoreDataResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the stored data.
    /// </summary>
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key used to store the data.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the size of the stored data in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data was stored.
    /// </summary>
    public DateTime StoredAt { get; set; }

    /// <summary>
    /// Gets or sets the metadata of the stored data.
    /// </summary>
    public StorageMetadata? Metadata { get; set; }
}

/// <summary>
/// Response from retrieving data.
/// </summary>
public class RetrieveDataResult
{
    /// <summary>
    /// Gets or sets the retrieved data (base64 encoded).
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type of the data.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the metadata if requested.
    /// </summary>
    public StorageMetadata? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data was retrieved.
    /// </summary>
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Response from deleting data.
/// </summary>
public class DeleteDataResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data was deleted.
    /// </summary>
    public DateTime DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the deletion was soft or hard.
    /// </summary>
    public bool SoftDelete { get; set; }
}

/// <summary>
/// Response from updating data.
/// </summary>
public class UpdateDataResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated metadata.
    /// </summary>
    public StorageMetadata? Metadata { get; set; }
}

/// <summary>
/// Response from searching data.
/// </summary>
public class SearchDataResult
{
    /// <summary>
    /// Gets or sets the search results.
    /// </summary>
    public List<StorageSearchItem> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of matching items.
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
    /// Gets or sets whether there are more pages available.
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
/// Response from listing data.
/// </summary>
public class ListDataResult
{
    /// <summary>
    /// Gets or sets the data items.
    /// </summary>
    public List<StorageListItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of items.
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
    /// Gets or sets whether there are more pages available.
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
/// Response from batch storing data.
/// </summary>
public class BatchStoreDataResult
{
    /// <summary>
    /// Gets or sets the batch operation identifier.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the results for each item in the batch.
    /// </summary>
    public List<StoreDataResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of successful operations.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed operations.
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
    /// Gets or sets the timestamp when the batch was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Response from getting storage statistics.
/// </summary>
public class StorageStatisticsResult
{
    /// <summary>
    /// Gets or sets the storage statistics.
    /// </summary>
    public StorageStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Response from beginning a transaction.
/// </summary>
public class BeginTransactionResult
{
    /// <summary>
    /// Gets or sets the transaction identifier.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the transaction will expire.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Response from committing a transaction.
/// </summary>
public class CommitTransactionResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was committed.
    /// </summary>
    public DateTime CommittedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of operations that were committed.
    /// </summary>
    public int OperationCount { get; set; }
}

/// <summary>
/// Response from rolling back a transaction.
/// </summary>
public class RollbackTransactionResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was rolled back.
    /// </summary>
    public DateTime RolledBackAt { get; set; }

    /// <summary>
    /// Gets or sets the number of operations that were rolled back.
    /// </summary>
    public int OperationCount { get; set; }
}

/// <summary>
/// Represents a storage search result item.
/// </summary>
public class StorageSearchItem
{
    /// <summary>
    /// Gets or sets the data identifier.
    /// </summary>
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the search relevance score.
    /// </summary>
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Represents a storage list item.
/// </summary>
public class StorageListItem
{
    /// <summary>
    /// Gets or sets the data identifier.
    /// </summary>
    public string DataId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the metadata if requested.
    /// </summary>
    public StorageMetadata? Metadata { get; set; }
} 