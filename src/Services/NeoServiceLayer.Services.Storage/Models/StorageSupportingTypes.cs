namespace NeoServiceLayer.Services.Storage.Models;

/// <summary>
/// Represents a date and time range.
/// </summary>
public class DateTimeRange
{
    /// <summary>
    /// Gets or sets the start date and time.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets the end date and time.
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Gets whether the range is valid (start is before or equal to end).
    /// </summary>
    public bool IsValid => Start <= End;

    /// <summary>
    /// Gets the duration of the range.
    /// </summary>
    public TimeSpan Duration => End - Start;
}

/// <summary>
/// Represents a size range in bytes.
/// </summary>
public class SizeRange
{
    /// <summary>
    /// Gets or sets the minimum size in bytes.
    /// </summary>
    public long MinSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum size in bytes.
    /// </summary>
    public long MaxSize { get; set; }

    /// <summary>
    /// Gets whether the range is valid (min is less than or equal to max).
    /// </summary>
    public bool IsValid => MinSize <= MaxSize;
}

/// <summary>
/// Enumeration for sort directions.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending sort order.
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Descending sort order.
    /// </summary>
    Descending = 1
}

/// <summary>
/// Enumeration for statistics grouping options.
/// </summary>
public enum StatisticsGrouping
{
    /// <summary>
    /// All statistics combined.
    /// </summary>
    All = 0,

    /// <summary>
    /// Statistics grouped by content type.
    /// </summary>
    ByContentType = 1,

    /// <summary>
    /// Statistics grouped by creation date.
    /// </summary>
    ByCreationDate = 2,

    /// <summary>
    /// Statistics grouped by size range.
    /// </summary>
    BySizeRange = 3,

    /// <summary>
    /// Statistics grouped by storage class.
    /// </summary>
    ByStorageClass = 4,

    /// <summary>
    /// Statistics grouped by encryption status.
    /// </summary>
    ByEncryptionStatus = 5
}

/// <summary>
/// Enumeration for transaction isolation levels.
/// </summary>
public enum TransactionIsolationLevel
{
    /// <summary>
    /// Read uncommitted isolation level.
    /// </summary>
    ReadUncommitted = 0,

    /// <summary>
    /// Read committed isolation level.
    /// </summary>
    ReadCommitted = 1,

    /// <summary>
    /// Repeatable read isolation level.
    /// </summary>
    RepeatableRead = 2,

    /// <summary>
    /// Serializable isolation level.
    /// </summary>
    Serializable = 3
}

/// <summary>
/// Enumeration for storage access levels.
/// </summary>
public enum StorageAccessLevel
{
    /// <summary>
    /// Read-only access.
    /// </summary>
    ReadOnly = 0,

    /// <summary>
    /// Read and write access.
    /// </summary>
    ReadWrite = 1,

    /// <summary>
    /// Full access including delete.
    /// </summary>
    FullAccess = 2,

    /// <summary>
    /// Administrative access.
    /// </summary>
    Administrative = 3
}

/// <summary>
/// Enumeration for storage data types.
/// </summary>
public enum StorageDataType
{
    /// <summary>
    /// Binary data.
    /// </summary>
    Binary = 0,

    /// <summary>
    /// Text data.
    /// </summary>
    Text = 1,

    /// <summary>
    /// Image data.
    /// </summary>
    Image = 2,

    /// <summary>
    /// Video data.
    /// </summary>
    Video = 3,

    /// <summary>
    /// Audio data.
    /// </summary>
    Audio = 4,

    /// <summary>
    /// Document data.
    /// </summary>
    Document = 5,

    /// <summary>
    /// Archive data.
    /// </summary>
    Archive = 6,

    /// <summary>
    /// Configuration data.
    /// </summary>
    Configuration = 7,

    /// <summary>
    /// Blockchain data.
    /// </summary>
    Blockchain = 8,

    /// <summary>
    /// Other data type.
    /// </summary>
    Other = 99
}

/// <summary>
/// Represents storage access control entry.
/// </summary>
public class StorageAccessControlEntry
{
    /// <summary>
    /// Gets or sets the principal (user, group, or service).
    /// </summary>
    public string Principal { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access level.
    /// </summary>
    public StorageAccessLevel AccessLevel { get; set; }

    /// <summary>
    /// Gets or sets whether this is a grant or deny entry.
    /// </summary>
    public bool IsGrant { get; set; } = true;

    /// <summary>
    /// Gets or sets when this access control entry expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets additional permissions or restrictions.
    /// </summary>
    public Dictionary<string, string> ExtendedPermissions { get; set; } = new();
}

/// <summary>
/// Represents extended storage metadata.
/// </summary>
public class ExtendedStorageMetadata : StorageMetadata
{
    /// <summary>
    /// Gets or sets the data type.
    /// </summary>
    public StorageDataType DataType { get; set; }

    /// <summary>
    /// Gets or sets the access control entries.
    /// </summary>
    public List<StorageAccessControlEntry> AccessControlEntries { get; set; } = new();

    /// <summary>
    /// Gets or sets the backup information.
    /// </summary>
    public StorageBackupInfo? BackupInfo { get; set; }

    /// <summary>
    /// Gets or sets the audit trail.
    /// </summary>
    public List<StorageAuditEntry> AuditTrail { get; set; } = new();

    /// <summary>
    /// Gets or sets version information.
    /// </summary>
    public StorageVersionInfo? VersionInfo { get; set; }
}

/// <summary>
/// Represents storage backup information.
/// </summary>
public class StorageBackupInfo
{
    /// <summary>
    /// Gets or sets whether the data is backed up.
    /// </summary>
    public bool IsBackedUp { get; set; }

    /// <summary>
    /// Gets or sets the last backup timestamp.
    /// </summary>
    public DateTime? LastBackupAt { get; set; }

    /// <summary>
    /// Gets or sets the backup location.
    /// </summary>
    public string? BackupLocation { get; set; }

    /// <summary>
    /// Gets or sets the backup frequency.
    /// </summary>
    public TimeSpan? BackupFrequency { get; set; }

    /// <summary>
    /// Gets or sets the backup retention period.
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; }
}

/// <summary>
/// Represents a storage audit entry.
/// </summary>
public class StorageAuditEntry
{
    /// <summary>
    /// Gets or sets the audit entry identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation performed.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who performed the operation.
    /// </summary>
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the operation was performed.
    /// </summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>
    /// Gets or sets additional details about the operation.
    /// </summary>
    public Dictionary<string, string> Details { get; set; } = new();

    /// <summary>
    /// Gets or sets the operation result.
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Gets or sets the IP address or source of the operation.
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// Represents storage version information.
/// </summary>
public class StorageVersionInfo
{
    /// <summary>
    /// Gets or sets the current version number.
    /// </summary>
    public int CurrentVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the total number of versions.
    /// </summary>
    public int TotalVersions { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether versioning is enabled.
    /// </summary>
    public bool VersioningEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of versions to keep.
    /// </summary>
    public int? MaxVersions { get; set; }

    /// <summary>
    /// Gets or sets the version history.
    /// </summary>
    public List<StorageVersionEntry> VersionHistory { get; set; } = new();
}

/// <summary>
/// Represents a storage version entry.
/// </summary>
public class StorageVersionEntry
{
    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets when this version was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets who created this version.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of this version in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the content hash of this version.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the changes in this version.
    /// </summary>
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Gets or sets whether this version is the current active version.
    /// </summary>
    public bool IsCurrent { get; set; }
}
