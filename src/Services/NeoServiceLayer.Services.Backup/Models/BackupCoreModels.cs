namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Backup type enumeration.
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Full backup of all data.
    /// </summary>
    Full,

    /// <summary>
    /// Incremental backup of changes since last backup.
    /// </summary>
    Incremental,

    /// <summary>
    /// Differential backup of changes since last full backup.
    /// </summary>
    Differential,

    /// <summary>
    /// Snapshot backup at a specific point in time.
    /// </summary>
    Snapshot
}

/// <summary>
/// Backup status enumeration.
/// </summary>
public enum BackupStatus
{
    /// <summary>
    /// Backup is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Backup is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Backup completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Backup failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Backup was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Backup is being verified.
    /// </summary>
    Verifying,

    /// <summary>
    /// Backup verification failed.
    /// </summary>
    VerificationFailed
}

/// <summary>
/// Data source type enumeration.
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// File system data source.
    /// </summary>
    FileSystem,

    /// <summary>
    /// Database data source.
    /// </summary>
    Database,

    /// <summary>
    /// Service data source.
    /// </summary>
    Service,

    /// <summary>
    /// Configuration data source.
    /// </summary>
    Configuration,

    /// <summary>
    /// Blockchain data source.
    /// </summary>
    Blockchain,

    /// <summary>
    /// Enclave data source.
    /// </summary>
    Enclave
}

/// <summary>
/// Destination type enumeration.
/// </summary>
public enum DestinationType
{
    /// <summary>
    /// Local file system destination.
    /// </summary>
    LocalFileSystem,

    /// <summary>
    /// Network file share destination.
    /// </summary>
    NetworkShare,

    /// <summary>
    /// Cloud storage destination.
    /// </summary>
    CloudStorage,

    /// <summary>
    /// Database destination.
    /// </summary>
    Database,

    /// <summary>
    /// Distributed storage destination.
    /// </summary>
    DistributedStorage
}

/// <summary>
/// Backup data source.
/// </summary>
public class BackupDataSource
{
    /// <summary>
    /// Gets or sets the source type.
    /// </summary>
    public DataSourceType SourceType { get; set; }

    /// <summary>
    /// Gets or sets the source identifier.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source path or connection string.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inclusion filters.
    /// </summary>
    public string[] IncludeFilters { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the exclusion filters.
    /// </summary>
    public string[] ExcludeFilters { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional source metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup destination.
/// </summary>
public class BackupDestination
{
    /// <summary>
    /// Gets or sets the destination type.
    /// </summary>
    public DestinationType DestinationType { get; set; }

    /// <summary>
    /// Gets or sets the destination path or connection string.
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the access credentials.
    /// </summary>
    public Dictionary<string, string> Credentials { get; set; } = new();
}

/// <summary>
/// Create backup request.
/// </summary>
public class CreateBackupRequest
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
    /// Gets or sets the data sources to backup.
    /// </summary>
    public BackupDataSource[] DataSources { get; set; } = Array.Empty<BackupDataSource>();

    /// <summary>
    /// Gets or sets the backup destination.
    /// </summary>
    public BackupDestination Destination { get; set; } = new();

    /// <summary>
    /// Gets or sets the compression settings.
    /// </summary>
    public CompressionSettings Compression { get; set; } = new();

    /// <summary>
    /// Gets or sets the encryption settings.
    /// </summary>
    public EncryptionSettings Encryption { get; set; } = new();

    /// <summary>
    /// Gets or sets the retention policy.
    /// </summary>
    public RetentionPolicy RetentionPolicy { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to verify the backup after creation.
    /// </summary>
    public bool VerifyAfterCreation { get; set; } = true;

    /// <summary>
    /// Gets or sets the user ID associated with this backup.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup result.
/// </summary>
public class BackupResult
{
    /// <summary>
    /// Gets or sets the backup ID.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the backup was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the backup status.
    /// </summary>
    public BackupStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message if the backup failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the backup start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the backup completion time.
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
    /// Gets or sets the backup location.
    /// </summary>
    public string BackupLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup checksum.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage location (alias for BackupLocation).
    /// </summary>
    public string StorageLocation
    {
        get => BackupLocation;
        set => BackupLocation = value;
    }

    /// <summary>
    /// Gets or sets the backup size (alias for BackupSizeBytes).
    /// </summary>
    public long BackupSize
    {
        get => BackupSizeBytes;
        set => BackupSizeBytes = value;
    }

    /// <summary>
    /// Gets or sets the creation time (alias for StartTime).
    /// </summary>
    public DateTime CreatedAt
    {
        get => StartTime;
        set => StartTime = value;
    }

    /// <summary>
    /// Gets or sets the completion time (alias for CompletionTime).
    /// </summary>
    public DateTime? CompletedAt
    {
        get => CompletionTime;
        set => CompletionTime = value;
    }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
