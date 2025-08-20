using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Backup.Models;

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
/// Create backup request.
/// </summary>
public class CreateBackupRequest
{
    public string BackupName { get; set; } = string.Empty;
    public BackupType BackupType { get; set; } = BackupType.Full;
    public BackupDataSource[] DataSources { get; set; } = Array.Empty<BackupDataSource>();
    public string DataType { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public bool IncludeMetadata { get; set; } = true;
    public bool CompressData { get; set; } = true;
    public bool EncryptData { get; set; } = true;
    public bool VerifyAfterCreation { get; set; } = true;
    public string? UserId { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
    public CompressionSettings Compression { get; set; } = new();
    public EncryptionSettings Encryption { get; set; } = new();
    public BackupDestination Destination { get; set; } = new();
}

/// <summary>
/// Compression settings for backup operations.
/// </summary>
public class CompressionSettings
{
    public bool Enabled { get; set; } = true;
    public string Algorithm { get; set; } = "gzip";
    public int Level { get; set; } = 6;
}

/// <summary>
/// Encryption settings for backup operations.
/// </summary>
public class EncryptionSettings
{
    public bool Enabled { get; set; } = true;
    public string Algorithm { get; set; } = "AES256";
    public string? KeyId { get; set; }
}

/// <summary>
/// Backup result.
/// </summary>
public class BackupResult
{
    public string BackupId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public BackupStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public long BackupSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public string? BackupLocation { get; set; }
    public string? BackupChecksum { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the checksum (alias for BackupChecksum).
    /// </summary>
    public string? Checksum
    {
        get => BackupChecksum;
        set => BackupChecksum = value;
    }

    /// <summary>
    /// Gets or sets the storage location (alias for BackupLocation).
    /// </summary>
    public string? StorageLocation
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
    /// Gets or sets the created at timestamp (alias for StartTime).
    /// </summary>
    public DateTime CreatedAt
    {
        get => StartTime;
        set => StartTime = value;
    }

    /// <summary>
    /// Gets or sets the completed at timestamp (alias for CompletionTime).
    /// </summary>
    public DateTime? CompletedAt
    {
        get => CompletionTime;
        set => CompletionTime = value;
    }
}

/// <summary>
/// Restore backup request.
/// </summary>
public class RestoreBackupRequest
{
    public string BackupId { get; set; } = string.Empty;
    public string RestorePath { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; } = false;
    public bool ValidateIntegrity { get; set; } = true;
    public string? UserId { get; set; }
    public RestoreOptions Options { get; set; } = new();
    public RestoreDestination? Destination { get; set; }
}

/// <summary>
/// Restore options configuration.
/// </summary>
public class RestoreOptions
{
    public RestoreMode Mode { get; set; } = RestoreMode.Full;
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}

/// <summary>
/// Restore destination information.
/// </summary>
public class RestoreDestination
{
    public string? DestinationPath { get; set; }
    public string? StorageType { get; set; }
}

/// <summary>
/// Restore result.
/// </summary>
public class RestoreResult
{
    public string RestoreId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public long RestoredSizeBytes { get; set; }
    public string? RestorePath { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int ItemsRestored { get; set; }
    public long DataSizeRestored { get; set; }
    public RestoreStatus Status { get; set; }
}

/// <summary>
/// Restore status enumeration.
/// </summary>
public enum RestoreStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Backup status request.
/// </summary>
public class BackupStatusRequest
{
    public string BackupId { get; set; } = string.Empty;
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Backup status result.
/// </summary>
public class BackupStatusResult
{
    public string BackupId { get; set; } = string.Empty;
    public BackupStatus Status { get; set; }
    public BackupProgress Progress { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public DateTime CheckedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// List backups request.
/// </summary>
public class ListBackupsRequest
{
    public BackupFilterCriteria? FilterCriteria { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; } = 1;
}

/// <summary>
/// Backup list result.
/// </summary>
public class BackupListResult
{
    public BackupEntry[] Backups { get; set; } = Array.Empty<BackupEntry>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Delete backup request.
/// </summary>
public class DeleteBackupRequest
{
    public string BackupId { get; set; } = string.Empty;
    public bool PermanentDelete { get; set; } = false;
    public string? UserId { get; set; }
}

/// <summary>
/// Backup deletion result.
/// </summary>
public class BackupDeletionResult
{
    public string BackupId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime DeletedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Validate backup request.
/// </summary>
public class ValidateBackupRequest
{
    public string BackupId { get; set; } = string.Empty;
    public bool CheckIntegrity { get; set; } = true;
    public bool VerifyChecksum { get; set; } = true;
}

/// <summary>
/// Backup validation result.
/// </summary>
public class BackupValidationResult
{
    public string BackupId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public ValidationCheck[] ValidationChecks { get; set; } = Array.Empty<ValidationCheck>();
    public DateTime ValidatedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Create scheduled backup request.
/// </summary>
public class CreateScheduledBackupRequest
{
    public string ScheduleName { get; set; } = string.Empty;
    public string BackupName { get; set; } = string.Empty;
    public BackupDataSource[] DataSources { get; set; } = Array.Empty<BackupDataSource>();
    public BackupDestination Destination { get; set; } = new();
    public BackupScheduleConfiguration Schedule { get; set; } = new();
    public CompressionSettings Compression { get; set; } = new();
    public EncryptionSettings Encryption { get; set; } = new();
    public string? UserId { get; set; }
}

/// <summary>
/// Scheduled backup result.
/// </summary>
public class ScheduledBackupResult
{
    public string ScheduleId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime NextRunTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup statistics request.
/// </summary>
public class BackupStatisticsRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DataType { get; set; }
    public TimeSpan TimeRange { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Backup statistics result.
/// </summary>
public class BackupStatisticsResult
{
    public OverallStatistics Statistics { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Export backup request.
/// </summary>
public class ExportBackupRequest
{
    public string BackupId { get; set; } = string.Empty;
    public string ExportPath { get; set; } = string.Empty;
    public BackupExportFormat ExportFormat { get; set; } = BackupExportFormat.Zip;
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// Backup export format enumeration.
/// </summary>
public enum BackupExportFormat
{
    Zip,
    Tar,
    Raw
}

/// <summary>
/// Backup export result.
/// </summary>
public class BackupExportResult
{
    public string BackupId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ExportPath { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExportSizeBytes { get; set; }
    public DateTime ExportedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Import backup request.
/// </summary>
public class ImportBackupRequest
{
    public string ImportPath { get; set; } = string.Empty;
    public string? BackupName { get; set; }
    public bool ValidateOnImport { get; set; } = true;
    public string? UserId { get; set; }
    public BackupExportFormat ImportFormat { get; set; } = BackupExportFormat.Zip;
}

/// <summary>
/// Backup import result.
/// </summary>
public class BackupImportResult
{
    public string BackupId { get; set; } = string.Empty;
    public string ImportId { get; set; } = string.Empty;
    public string? ImportPath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long ImportedSizeBytes { get; set; }
    public DateTime ImportedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

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
    public string? UserId { get; set; }
}

/// <summary>
/// Restore mode enumeration.
/// </summary>
public enum RestoreMode
{
    Full,
    Partial,
    Incremental
}

/// <summary>
/// Backup type enumeration.
/// </summary>
public enum BackupType
{
    Full,
    Incremental,
    Differential
}

/// <summary>
/// Data source type enumeration.
/// </summary>
public enum DataSourceType
{
    Blockchain,
    Database,
    FileSystem,
    Service
}

/// <summary>
/// Backup data source information.
/// </summary>
public class BackupDataSource
{
    public DataSourceType SourceType { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
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
    public string? UserId { get; set; }
    public long? BackupSizeBytes { get; set; }
    public long? CompressedSizeBytes { get; set; }

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
/// Overall backup statistics.
/// </summary>
public class OverallStatistics
{
    public int TotalBackups { get; set; }
    public int SuccessfulBackups { get; set; }
    public int FailedBackups { get; set; }
    public double SuccessRate { get; set; }
    public long TotalBackupSizeBytes { get; set; }
    public TimeSpan AverageBackupDuration { get; set; }
}

/// <summary>
/// Backup progress information.
/// </summary>
public class BackupProgress
{
    public double PercentageCompleted { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public long ProcessedBytes { get; set; }
    public long TotalBytes { get; set; }
}

/// <summary>
/// Backup filter criteria.
/// </summary>
public class BackupFilterCriteria
{
    public string? UserId { get; set; }
    public bool IncludeExpired { get; set; } = true;
    public BackupStatus? Status { get; set; }
}

/// <summary>
/// Backup entry for listing operations.
/// </summary>
public class BackupEntry
{
    public string BackupId { get; set; } = string.Empty;
    public string BackupName { get; set; } = string.Empty;
    public BackupType BackupType { get; set; }
    public BackupStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public long BackupSizeBytes { get; set; }
    public string BackupLocation { get; set; } = string.Empty;
}

/// <summary>
/// Validation check result.
/// </summary>
public class ValidationCheck
{
    public string CheckName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Backup destination configuration.
/// </summary>
public class BackupDestination
{
    public string DestinationPath { get; set; } = string.Empty;
    public string? StorageType { get; set; }
}

/// <summary>
/// Backup schedule configuration.
/// </summary>
public class BackupScheduleConfiguration
{
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? TimeZone { get; set; }
}
