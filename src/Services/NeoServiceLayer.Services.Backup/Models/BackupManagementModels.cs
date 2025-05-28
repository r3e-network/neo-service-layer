namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Sort order enumeration.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Ascending order.
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending order.
    /// </summary>
    Descending
}

/// <summary>
/// Backup status request.
/// </summary>
public class BackupStatusRequest
{
    /// <summary>
    /// Gets or sets the backup ID.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include detailed progress information.
    /// </summary>
    public bool IncludeProgress { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup status result.
/// </summary>
public class BackupStatusResult
{
    /// <summary>
    /// Gets or sets the backup ID.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup status.
    /// </summary>
    public BackupStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the progress information.
    /// </summary>
    public BackupProgress Progress { get; set; } = new();

    /// <summary>
    /// Gets or sets the backup details.
    /// </summary>
    public BackupDetails Details { get; set; } = new();

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
/// Backup progress information.
/// </summary>
public class BackupProgress
{
    /// <summary>
    /// Gets or sets the percentage completed.
    /// </summary>
    public double PercentageCompleted { get; set; }

    /// <summary>
    /// Gets or sets the current operation.
    /// </summary>
    public string CurrentOperation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of items processed.
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the bytes processed.
    /// </summary>
    public long BytesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total bytes to process.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Gets or sets the processing speed in bytes per second.
    /// </summary>
    public double ProcessingSpeedBytesPerSecond { get; set; }
}

/// <summary>
/// Backup details.
/// </summary>
public class BackupDetails
{
    /// <summary>
    /// Gets or sets the backup name.
    /// </summary>
    public string BackupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup type.
    /// </summary>
    public BackupType BackupType { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the completion time.
    /// </summary>
    public DateTime? CompletionTime { get; set; }

    /// <summary>
    /// Gets or sets the backup size in bytes.
    /// </summary>
    public long BackupSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the compressed size in bytes.
    /// </summary>
    public long CompressedSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the compression ratio.
    /// </summary>
    public double CompressionRatio { get; set; }

    /// <summary>
    /// Gets or sets the backup location.
    /// </summary>
    public string BackupLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data sources included.
    /// </summary>
    public BackupDataSource[] DataSources { get; set; } = Array.Empty<BackupDataSource>();

    /// <summary>
    /// Gets or sets additional details.
    /// </summary>
    public Dictionary<string, object> AdditionalDetails { get; set; } = new();
}

/// <summary>
/// List backups request.
/// </summary>
public class ListBackupsRequest
{
    /// <summary>
    /// Gets or sets the backup type filter.
    /// </summary>
    public BackupType? BackupType { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public BackupStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the start date filter.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public SortOrder SortOrder { get; set; } = SortOrder.Descending;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup list result.
/// </summary>
public class BackupListResult
{
    /// <summary>
    /// Gets or sets the backup entries.
    /// </summary>
    public BackupEntry[] Backups { get; set; } = Array.Empty<BackupEntry>();

    /// <summary>
    /// Gets or sets the total count of backups.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets whether there are more results.
    /// </summary>
    public bool HasMore { get; set; }

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
/// Backup entry.
/// </summary>
public class BackupEntry
{
    /// <summary>
    /// Gets or sets the backup ID.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup name.
    /// </summary>
    public string BackupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup type.
    /// </summary>
    public BackupType BackupType { get; set; }

    /// <summary>
    /// Gets or sets the backup status.
    /// </summary>
    public BackupStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the completion time.
    /// </summary>
    public DateTime? CompletionTime { get; set; }

    /// <summary>
    /// Gets or sets the backup size in bytes.
    /// </summary>
    public long BackupSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the backup location.
    /// </summary>
    public string BackupLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Delete backup request.
/// </summary>
public class DeleteBackupRequest
{
    /// <summary>
    /// Gets or sets the backup ID to delete.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to force deletion even if backup is in use.
    /// </summary>
    public bool ForceDelete { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to delete associated metadata.
    /// </summary>
    public bool DeleteMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup deletion result.
/// </summary>
public class BackupDeletionResult
{
    /// <summary>
    /// Gets or sets the backup ID that was deleted.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the deletion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the deletion failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime DeletionTime { get; set; }

    /// <summary>
    /// Gets or sets the amount of space freed in bytes.
    /// </summary>
    public long SpaceFreedBytes { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
