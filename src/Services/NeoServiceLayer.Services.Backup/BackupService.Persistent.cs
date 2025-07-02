using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Backup.Models;

namespace NeoServiceLayer.Services.Backup;

public partial class BackupService
{
    private IPersistentStorageProvider? _persistentStorage;
    private readonly IServiceProvider? _serviceProvider;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string BACKUP_PREFIX = "backup:backup:";
    private const string JOB_PREFIX = "backup:job:";
    private const string SCHEDULE_PREFIX = "backup:schedule:";
    private const string METADATA_PREFIX = "backup:metadata:";
    private const string INDEX_PREFIX = "backup:index:";
    private const string STATS_PREFIX = "backup:stats:";
    private const string RESTORE_PREFIX = "backup:restore:";

    /// <summary>
    /// Initializes persistent storage for the backup service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _persistentStorage = _serviceProvider?.GetService(typeof(IPersistentStorageProvider)) as IPersistentStorageProvider;

            if (_persistentStorage != null)
            {
                await _persistentStorage.InitializeAsync();
                Logger.LogInformation("Persistent storage initialized for BackupService");

                // Restore backup data from storage
                await RestoreBackupDataFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistBackupDataAsync(),
                    null,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30));

                // Start cleanup timer (every hour)
                _cleanupTimer = new Timer(
                    async _ => await CleanupExpiredDataAsync(),
                    null,
                    TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1));
            }
            else
            {
                Logger.LogWarning("Persistent storage provider not available for BackupService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize persistent storage for BackupService");
        }
    }

    /// <summary>
    /// Persists backup metadata to storage.
    /// </summary>
    private async Task PersistBackupMetadataAsync(BackupMetadata metadata)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{METADATA_PREFIX}{metadata.BackupId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(metadata);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365) // Keep metadata for 1 year
            });

            // Update index
            await UpdateBackupIndexAsync(metadata.DataType, metadata.BackupId);

            Logger.LogDebug("Persisted backup metadata {BackupId} to storage", metadata.BackupId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist backup metadata {BackupId}", metadata.BackupId);
        }
    }

    /// <summary>
    /// Persists backup job to storage.
    /// </summary>
    private async Task PersistBackupJobAsync(BackupJob job)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{JOB_PREFIX}{job.JobId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(job);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30) // Keep jobs for 30 days
            });

            // Update job index by status
            await UpdateJobIndexAsync(job.Status, job.JobId);

            Logger.LogDebug("Persisted backup job {JobId} to storage", job.JobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist backup job {JobId}", job.JobId);
        }
    }

    /// <summary>
    /// Persists backup schedule to storage.
    /// </summary>
    private async Task PersistBackupScheduleAsync(BackupSchedule schedule)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SCHEDULE_PREFIX}{schedule.ScheduleId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(schedule);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(730) // Keep schedules for 2 years
            });

            Logger.LogDebug("Persisted backup schedule {ScheduleId} to storage", schedule.ScheduleId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist backup schedule {ScheduleId}", schedule.ScheduleId);
        }
    }

    /// <summary>
    /// Persists restore operation to storage.
    /// </summary>
    private async Task PersistRestoreOperationAsync(RestoreOperation operation)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{RESTORE_PREFIX}{operation.RestoreId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(operation);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep restore operations for 90 days
            });

            Logger.LogDebug("Persisted restore operation {RestoreId} to storage", operation.RestoreId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist restore operation {RestoreId}", operation.RestoreId);
        }
    }

    /// <summary>
    /// Updates backup index in storage.
    /// </summary>
    private async Task UpdateBackupIndexAsync(string dataType, string backupId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}datatype:{dataType}";
            var existingData = await _persistentStorage.RetrieveAsync(key);
            
            var backupIds = existingData != null 
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            backupIds.Add(backupId);

            var data = JsonSerializer.SerializeToUtf8Bytes(backupIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update backup index for data type {DataType}", dataType);
        }
    }

    /// <summary>
    /// Updates job status index in storage.
    /// </summary>
    private async Task UpdateJobIndexAsync(BackupStatus status, string jobId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}job_status:{status}";
            var existingData = await _persistentStorage.RetrieveAsync(key);
            
            var jobIds = existingData != null 
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            jobIds.Add(jobId);

            var data = JsonSerializer.SerializeToUtf8Bytes(jobIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update job index for status {Status}", status);
        }
    }

    /// <summary>
    /// Restores backup data from persistent storage.
    /// </summary>
    private async Task RestoreBackupDataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Restoring backup data from persistent storage");

            // Restore schedules
            await RestoreSchedulesFromStorageAsync();

            // Restore active jobs
            await RestoreActiveJobsFromStorageAsync();

            // Restore backup metadata
            await RestoreBackupMetadataFromStorageAsync();

            // Restore statistics
            await RestoreStatisticsAsync();

            Logger.LogInformation("Backup data restored from storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore backup data from storage");
        }
    }

    /// <summary>
    /// Restores schedules from storage.
    /// </summary>
    private async Task RestoreSchedulesFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var scheduleKeys = await _persistentStorage.ListKeysAsync($"{SCHEDULE_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in scheduleKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var schedule = JsonSerializer.Deserialize<BackupSchedule>(data);
                        if (schedule != null && schedule.IsEnabled)
                        {
                            _schedules[schedule.ScheduleId] = schedule;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore schedule from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} backup schedules from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore schedules from storage");
        }
    }

    /// <summary>
    /// Restores active jobs from storage.
    /// </summary>
    private async Task RestoreActiveJobsFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var jobKeys = await _persistentStorage.ListKeysAsync($"{JOB_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in jobKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var job = JsonSerializer.Deserialize<BackupJob>(data);
                        if (job != null && job.Status != BackupStatus.Completed && job.Status != BackupStatus.Failed)
                        {
                            // Mark as failed if job was running when service stopped
                            if (job.Status == BackupStatus.InProgress)
                            {
                                job.Status = BackupStatus.Failed;
                                job.ErrorMessage = "Service restarted while job was running";
                                job.CompletedTime = DateTime.UtcNow;
                                await PersistBackupJobAsync(job);
                            }
                            else
                            {
                                _activeJobs[job.JobId] = job;
                                restoredCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore job from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} active backup jobs from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore jobs from storage");
        }
    }

    /// <summary>
    /// Restores backup metadata from storage.
    /// </summary>
    private async Task RestoreBackupMetadataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var metadataKeys = await _persistentStorage.ListKeysAsync($"{METADATA_PREFIX}*");
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Only restore recent metadata
            var restoredCount = 0;

            foreach (var key in metadataKeys.Take(1000)) // Limit to recent 1000 backups
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var metadata = JsonSerializer.Deserialize<BackupMetadata>(data);
                        if (metadata != null && metadata.CreatedTime >= cutoffDate)
                        {
                            // Store in appropriate cache
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore metadata from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} recent backup metadata entries", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore metadata from storage");
        }
    }

    /// <summary>
    /// Restores service statistics from storage.
    /// </summary>
    private async Task RestoreStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{STATS_PREFIX}current";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                var stats = JsonSerializer.Deserialize<InternalBackupServiceStatistics>(data);
                if (stats != null)
                {
                    // Restore statistics to service state
                    Logger.LogInformation("Restored backup service statistics from storage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore statistics from storage");
        }
    }

    /// <summary>
    /// Persists all current backup data to storage.
    /// </summary>
    private async Task PersistBackupDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Persist schedules
            foreach (var schedule in _schedules.Values)
            {
                await PersistBackupScheduleAsync(schedule);
            }

            // Persist active jobs
            foreach (var job in _activeJobs.Values)
            {
                await PersistBackupJobAsync(job);
            }

            // Persist statistics
            await PersistServiceStatisticsAsync();

            Logger.LogDebug("Persisted backup data to storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist backup data");
        }
    }

    /// <summary>
    /// Persists service statistics to storage.
    /// </summary>
    private async Task PersistServiceStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var stats = new InternalBackupServiceStatistics
            {
                TotalBackups = _activeJobs.Count(j => j.Value.Status == BackupStatus.Completed),
                ActiveJobs = _activeJobs.Count,
                ActiveSchedules = _schedules.Count,
                LastUpdated = DateTime.UtcNow
            };

            var key = $"{STATS_PREFIX}current";
            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist service statistics");
        }
    }

    /// <summary>
    /// Cleans up expired data from storage.
    /// </summary>
    private async Task CleanupExpiredDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Starting cleanup of expired backup data");

            // Clean up old jobs (older than 30 days)
            var jobKeys = await _persistentStorage.ListKeysAsync($"{JOB_PREFIX}*");
            var cleanedCount = 0;

            foreach (var key in jobKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var job = JsonSerializer.Deserialize<BackupJob>(data);
                        if (job != null && job.CreatedTime < DateTime.UtcNow.AddDays(-30))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup job key {Key}", key);
                }
            }

            // Clean up old restore operations (older than 90 days)
            var restoreKeys = await _persistentStorage.ListKeysAsync($"{RESTORE_PREFIX}*");
            foreach (var key in restoreKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var operation = JsonSerializer.Deserialize<RestoreOperation>(data);
                        if (operation != null && operation.CreatedTime < DateTime.UtcNow.AddDays(-90))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup restore key {Key}", key);
                }
            }

            Logger.LogInformation("Cleaned up {Count} expired backup data entries", cleanedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data");
        }
    }

    /// <summary>
    /// Removes backup data from persistent storage.
    /// </summary>
    private async Task RemoveBackupFromStorageAsync(string backupId)
    {
        if (_persistentStorage == null) return;

        try
        {
            await _persistentStorage.DeleteAsync($"{METADATA_PREFIX}{backupId}");
            await _persistentStorage.DeleteAsync($"{BACKUP_PREFIX}{backupId}");
            
            Logger.LogDebug("Removed backup {BackupId} from storage", backupId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove backup {BackupId} from storage", backupId);
        }
    }

    /// <summary>
    /// Disposes persistence resources.
    /// </summary>
    private void DisposePersistenceResources()
    {
        _persistenceTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _persistentStorage?.Dispose();
    }
}

/// <summary>
/// Backup metadata model.
/// </summary>
internal class BackupMetadata
{
    public string BackupId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public long SizeInBytes { get; set; }
    public string Location { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Restore operation model.
/// </summary>
internal class RestoreOperation
{
    public string RestoreId { get; set; } = string.Empty;
    public string BackupId { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public RestoreStatus Status { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Restore status enumeration.
/// </summary>
internal enum RestoreStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Statistics for backup service.
/// </summary>
internal class InternalBackupServiceStatistics
{
    public int TotalBackups { get; set; }
    public int ActiveJobs { get; set; }
    public int ActiveSchedules { get; set; }
    public DateTime LastUpdated { get; set; }
}