using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Generic backup request.
/// </summary>
public class BackupRequest
{
    public string BackupId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public bool IncludeMetadata { get; set; } = true;
    public bool CompressData { get; set; } = true;
    public bool EncryptData { get; set; } = true;
    public Dictionary<string, object> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the compression type.
    /// </summary>
    public string CompressionType { get; set; } = "gzip";

    /// <summary>
    /// Gets or sets whether encryption is enabled.
    /// </summary>
    public bool EncryptionEnabled
    {
        get => EncryptData;
        set => EncryptData = value;
    }

    /// <summary>
    /// Gets or sets the encryption key.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the storage location.
    /// </summary>
    public string StorageLocation
    {
        get => DestinationPath;
        set => DestinationPath = value;
    }
}

/// <summary>
/// Restore request.
/// </summary>
public class RestoreRequest
{
    public string BackupId { get; set; } = string.Empty;
    public string RestorePath { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; } = false;
    public bool ValidateIntegrity { get; set; } = true;
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Backup schedule request.
/// </summary>
public class BackupScheduleRequest
{
    public string ScheduleId { get; set; } = string.Empty;
    public BackupRequest BackupRequest { get; set; } = new();
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Backup schedule result.
/// </summary>
public class BackupScheduleResult
{
    public string ScheduleId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Backup list request.
/// </summary>
public class BackupListRequest
{
    public string? DataType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; } = 1;
    public BackupStatus? Status { get; set; }
    public int? Limit { get; set; }
}

/// <summary>
/// Backup info.
/// </summary>
public class BackupInfo
{
    public string BackupId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public BackupStatus Status { get; set; }
    public string? StorageLocation { get; set; }
}

/// <summary>
/// Backup service statistics.
/// </summary>
public class BackupServiceStatistics
{
    public int ActiveJobs { get; set; }
    public int TotalSchedules { get; set; }
    public int EnabledSchedules { get; set; }
    public DateTime? LastBackupTime { get; set; }
    public DateTime? NextScheduledBackup { get; set; }
}

/// <summary>
/// Backup job information.
/// </summary>
public class BackupJob
{
    public string BackupId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the job ID (alias for BackupId).
    /// </summary>
    public string JobId { get => BackupId; set => BackupId = value; }
    public BackupRequest Request { get; set; } = new();
    public BackupStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StorageLocation { get; set; }
    public string? ErrorMessage { get; set; }
    public BlockchainType BlockchainType { get; set; }
    
    /// <summary>
    /// Gets or sets when the job was created.
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets when the job was completed (alias for CompletedAt).
    /// </summary>
    public DateTime? CompletedTime { get => CompletedAt; set => CompletedAt = value; }
}

/// <summary>
/// Backup schedule information.
/// </summary>
public class BackupSchedule
{
    public string ScheduleId { get; set; } = string.Empty;
    public BackupRequest BackupRequest { get; set; } = new();
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime NextRunTime { get; set; }
    public DateTime? LastRunTime { get; set; }
    public string? LastRunResult { get; set; }
    public BlockchainType BlockchainType { get; set; }
}

/// <summary>
