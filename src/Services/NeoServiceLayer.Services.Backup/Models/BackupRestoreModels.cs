namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Restore mode enumeration.
/// </summary>
public enum RestoreMode
{
    /// <summary>
    /// Complete restore of all data.
    /// </summary>
    Complete,

    /// <summary>
    /// Partial restore of specific items.
    /// </summary>
    Partial,

    /// <summary>
    /// Point-in-time restore.
    /// </summary>
    PointInTime,

    /// <summary>
    /// Incremental restore.
    /// </summary>
    Incremental
}

/// <summary>
/// Restore status enumeration.
/// </summary>
public enum RestoreStatus
{
    /// <summary>
    /// Restore is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Restore is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Restore completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Restore failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Restore was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Restore is being verified.
    /// </summary>
    Verifying,

    /// <summary>
    /// Restore verification failed.
    /// </summary>
    VerificationFailed
}

/// <summary>
/// Restore backup request.
/// </summary>
public class RestoreBackupRequest
{
    /// <summary>
    /// Gets or sets the backup ID to restore.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the restore destination.
    /// </summary>
    public RestoreDestination Destination { get; set; } = new();

    /// <summary>
    /// Gets or sets the restore options.
    /// </summary>
    public RestoreOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to verify the restore.
    /// </summary>
    public bool VerifyRestore { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Restore destination.
/// </summary>
public class RestoreDestination
{
    /// <summary>
    /// Gets or sets the destination type.
    /// </summary>
    public DestinationType DestinationType { get; set; }

    /// <summary>
    /// Gets or sets the destination path.
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to overwrite existing data.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Gets or sets the restore configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Restore options.
/// </summary>
public class RestoreOptions
{
    /// <summary>
    /// Gets or sets the restore mode.
    /// </summary>
    public RestoreMode Mode { get; set; } = RestoreMode.Complete;

    /// <summary>
    /// Gets or sets the specific items to restore.
    /// </summary>
    public string[] SpecificItems { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the point-in-time to restore to.
    /// </summary>
    public DateTime? PointInTime { get; set; }

    /// <summary>
    /// Gets or sets whether to preserve permissions.
    /// </summary>
    public bool PreservePermissions { get; set; } = true;

    /// <summary>
    /// Gets or sets additional restore options.
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}

/// <summary>
/// Restore result.
/// </summary>
public class RestoreResult
{
    /// <summary>
    /// Gets or sets the restore ID.
    /// </summary>
    public string RestoreId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the restore was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the restore status.
    /// </summary>
    public RestoreStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message if the restore failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the restore start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the restore completion time.
    /// </summary>
    public DateTime? CompletionTime { get; set; }

    /// <summary>
    /// Gets or sets the number of items restored.
    /// </summary>
    public int ItemsRestored { get; set; }

    /// <summary>
    /// Gets or sets the total data size restored in bytes.
    /// </summary>
    public long DataSizeRestored { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
