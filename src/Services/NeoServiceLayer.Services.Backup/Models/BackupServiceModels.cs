using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Backup.Models;

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
    public BackupRequest Request { get; set; } = new();
    public BackupStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StorageLocation { get; set; }
    public string? ErrorMessage { get; set; }
    public BlockchainType BlockchainType { get; set; }
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
/// Backup status enumeration.
/// </summary>
public enum BackupStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}
