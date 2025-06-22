namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Schedule type enumeration.
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// One-time schedule.
    /// </summary>
    OneTime,

    /// <summary>
    /// Interval-based schedule.
    /// </summary>
    Interval,

    /// <summary>
    /// Cron-based schedule.
    /// </summary>
    Cron,

    /// <summary>
    /// Daily schedule.
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly schedule.
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly schedule.
    /// </summary>
    Monthly
}

/// <summary>
/// Create scheduled backup request.
/// </summary>
public class CreateScheduledBackupRequest
{
    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string ScheduleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup name.
    /// </summary>
    public string BackupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data sources to backup.
    /// </summary>
    public BackupDataSource[] DataSources { get; set; } = Array.Empty<BackupDataSource>();

    /// <summary>
    /// Gets or sets the backup destination.
    /// </summary>
    public BackupDestination Destination { get; set; } = new();

    /// <summary>
    /// Gets or sets the backup configuration.
    /// </summary>
    public CreateBackupRequest BackupConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets the schedule configuration.
    /// </summary>
    public ScheduleConfiguration Schedule { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Schedule configuration.
/// </summary>
public class ScheduleConfiguration
{
    /// <summary>
    /// Gets or sets the schedule type.
    /// </summary>
    public ScheduleType ScheduleType { get; set; }

    /// <summary>
    /// Gets or sets the cron expression for cron-based schedules.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval for interval-based schedules.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the time zone.
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets additional schedule options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Scheduled backup result.
/// </summary>
public class ScheduledBackupResult
{
    /// <summary>
    /// Gets or sets the schedule ID.
    /// </summary>
    public string ScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the schedule creation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the schedule creation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the next execution time.
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the next run time (alias for NextExecutionTime).
    /// </summary>
    public DateTime? NextRunTime
    {
        get => NextExecutionTime;
        set => NextExecutionTime = value;
    }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup statistics request.
/// </summary>
public class BackupStatisticsRequest
{
    /// <summary>
    /// Gets or sets the time range for statistics.
    /// </summary>
    public TimeSpan TimeRange { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets whether to include detailed statistics.
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup statistics result.
/// </summary>
public class BackupStatisticsResult
{
    /// <summary>
    /// Gets or sets the overall statistics.
    /// </summary>
    public OverallStatistics Overall { get; set; } = new();

    /// <summary>
    /// Gets or sets the backup type statistics.
    /// </summary>
    public BackupTypeStatistics[] BackupTypes { get; set; } = Array.Empty<BackupTypeStatistics>();

    /// <summary>
    /// Gets or sets the storage statistics.
    /// </summary>
    public StorageStatistics Storage { get; set; } = new();

    /// <summary>
    /// Gets or sets the statistics (alias for Overall).
    /// </summary>
    public OverallStatistics Statistics
    {
        get => Overall;
        set => Overall = value;
    }

    /// <summary>
    /// Gets or sets when the statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

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
/// Overall backup statistics.
/// </summary>
public class OverallStatistics
{
    /// <summary>
    /// Gets or sets the total number of backups.
    /// </summary>
    public int TotalBackups { get; set; }

    /// <summary>
    /// Gets or sets the number of successful backups.
    /// </summary>
    public int SuccessfulBackups { get; set; }

    /// <summary>
    /// Gets or sets the number of failed backups.
    /// </summary>
    public int FailedBackups { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the total backup size in bytes.
    /// </summary>
    public long TotalBackupSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the average backup duration.
    /// </summary>
    public TimeSpan AverageBackupDuration { get; set; }

    /// <summary>
    /// Gets or sets additional overall statistics.
    /// </summary>
    public Dictionary<string, object> AdditionalStatistics { get; set; } = new();
}

/// <summary>
/// Backup type statistics.
/// </summary>
public class BackupTypeStatistics
{
    /// <summary>
    /// Gets or sets the backup type.
    /// </summary>
    public BackupType BackupType { get; set; }

    /// <summary>
    /// Gets or sets the count of this backup type.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the average duration.
    /// </summary>
    public TimeSpan AverageDuration { get; set; }

    /// <summary>
    /// Gets or sets the success rate.
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// Storage statistics.
/// </summary>
public class StorageStatistics
{
    /// <summary>
    /// Gets or sets the total storage used in bytes.
    /// </summary>
    public long TotalStorageUsedBytes { get; set; }

    /// <summary>
    /// Gets or sets the available storage in bytes.
    /// </summary>
    public long AvailableStorageBytes { get; set; }

    /// <summary>
    /// Gets or sets the storage utilization percentage.
    /// </summary>
    public double StorageUtilizationPercent { get; set; }

    /// <summary>
    /// Gets or sets the compression ratio.
    /// </summary>
    public double CompressionRatio { get; set; }

    /// <summary>
    /// Gets or sets the deduplication savings in bytes.
    /// </summary>
    public long DeduplicationSavingsBytes { get; set; }
}
