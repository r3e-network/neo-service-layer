using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for services that support persistent storage with standardized patterns.
/// </summary>
public abstract class PersistentServiceBase : ServiceBase
{
    /// <summary>
    /// The persistent storage provider, may be null if not configured.
    /// </summary>
    protected readonly IPersistentStorageProvider? PersistentStorage;

    /// <summary>
    /// Timer for periodic persistence operations.
    /// </summary>
    protected Timer? PersistenceTimer;

    /// <summary>
    /// Timer for cleanup operations.
    /// </summary>
    protected Timer? CleanupTimer;

    /// <summary>
    /// Service start time for uptime calculation.
    /// </summary>
    protected DateTime StartedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Service prefix for storage keys.
    /// </summary>
    protected abstract string ServicePrefix { get; }

    /// <summary>
    /// Default retention period for historical data.
    /// </summary>
    protected virtual TimeSpan DefaultRetentionPeriod => TimeSpan.FromDays(90);

    /// <summary>
    /// Default persistence interval.
    /// </summary>
    protected virtual TimeSpan DefaultPersistenceInterval => TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default cleanup interval.
    /// </summary>
    protected virtual TimeSpan DefaultCleanupInterval => TimeSpan.FromHours(24);

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentServiceBase"/> class.
    /// </summary>
    protected PersistentServiceBase(
        string serviceName,
        string description,
        string version,
        ILogger logger,
        IPersistentStorageProvider? persistentStorage = null)
        : base(serviceName, description, version, logger)
    {
        PersistentStorage = persistentStorage;

        if (PersistentStorage != null)
        {
            InitializePersistenceTimers();
        }
    }

    /// <summary>
    /// Initializes persistence and cleanup timers.
    /// </summary>
    private void InitializePersistenceTimers()
    {
        PersistenceTimer = new Timer(
            async _ => await ExecutePersistenceOperationAsync(),
            null,
            DefaultPersistenceInterval,
            DefaultPersistenceInterval);

        CleanupTimer = new Timer(
            async _ => await ExecuteCleanupOperationAsync(),
            null,
            DefaultCleanupInterval,
            DefaultCleanupInterval);
    }

    /// <summary>
    /// Executes periodic persistence operations.
    /// </summary>
    private async Task ExecutePersistenceOperationAsync()
    {
        try
        {
            await OnPeriodicPersistenceAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during periodic persistence operation for {ServiceName}", Name);
        }
    }

    /// <summary>
    /// Executes periodic cleanup operations.
    /// </summary>
    private async Task ExecuteCleanupOperationAsync()
    {
        try
        {
            await OnPeriodicCleanupAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during periodic cleanup operation for {ServiceName}", Name);
        }
    }

    /// <summary>
    /// Called periodically to persist service data.
    /// </summary>
    protected virtual async Task OnPeriodicPersistenceAsync()
    {
        if (PersistentStorage != null)
        {
            await PersistServiceStatisticsAsync();
        }
    }

    /// <summary>
    /// Called periodically to clean up expired data.
    /// </summary>
    protected virtual async Task OnPeriodicCleanupAsync()
    {
        if (PersistentStorage != null)
        {
            await CleanupExpiredDataAsync();

            // Validate storage integrity periodically
            var validationResult = await PersistentStorage.ValidateStorageAsync(Logger);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Storage integrity issues detected for {ServiceName}: {Errors}",
                    Name, string.Join(", ", validationResult.Errors));
            }
        }
    }

    /// <summary>
    /// Persists service statistics.
    /// </summary>
    protected virtual async Task PersistServiceStatisticsAsync()
    {
        if (PersistentStorage == null) return;

        try
        {
            var stats = GetServiceStatistics();
            var key = StorageKeyPatterns.CreateStatsKey($"{ServicePrefix}:", "current");

            await PersistentStorage.StoreObjectAsync(key, stats, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            }, Logger, "service statistics");

            // Also store historical stats
            var historyKey = StorageKeyPatterns.CreateStatsKey($"{ServicePrefix}:", "history", DateTime.UtcNow);
            await PersistentStorage.StoreObjectAsync(historyKey, stats, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = DefaultRetentionPeriod
            }, Logger, "historical statistics");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to persist service statistics for {ServiceName}", Name);
        }
    }

    /// <summary>
    /// Cleans up expired data using standardized patterns.
    /// </summary>
    protected virtual async Task CleanupExpiredDataAsync()
    {
        if (PersistentStorage == null) return;

        try
        {
            // Clean up expired historical statistics
            var statsDeletedCount = await PersistentStorage.CleanupExpiredKeysAsync(
                $"{ServicePrefix}:stats:history:*",
                DefaultRetentionPeriod,
                StorageKeyPatterns.ExtractTimestamp,
                Logger);

            // Allow derived classes to perform additional cleanup
            var additionalDeletedCount = await OnCleanupExpiredDataAsync();

            var totalDeleted = statsDeletedCount + additionalDeletedCount;

            // Compact storage if significant cleanup occurred
            if (totalDeleted > 100)
            {
                await PersistentStorage.CompactServiceStorageAsync(Logger);
            }

            if (totalDeleted > 0)
            {
                Logger.LogInformation("Cleanup completed for {ServiceName}: {TotalDeleted} items removed",
                    Name, totalDeleted);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during cleanup for {ServiceName}", Name);
        }
    }

    /// <summary>
    /// Override to provide custom cleanup logic for service-specific data.
    /// </summary>
    /// <returns>Number of items cleaned up.</returns>
    protected virtual async Task<int> OnCleanupExpiredDataAsync()
    {
        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// Gets service statistics for persistence.
    /// </summary>
    protected virtual object GetServiceStatistics()
    {
        return new
        {
            ServiceName = Name,
            IsRunning,
            Health = "Healthy", // Simplified health status
            Timestamp = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - StartedAt
        };
    }

    /// <summary>
    /// Loads persistent data on service initialization.
    /// </summary>
    protected virtual async Task LoadPersistentDataAsync()
    {
        if (PersistentStorage == null) return;

        try
        {
            Logger.LogInformation("Loading persistent data for {ServiceName}", Name);
            await OnLoadPersistentDataAsync();
            Logger.LogInformation("Successfully loaded persistent data for {ServiceName}", Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent data for {ServiceName}", Name);
        }
    }

    /// <summary>
    /// Override to implement service-specific data loading.
    /// </summary>
    protected virtual async Task OnLoadPersistentDataAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a backup of service data.
    /// </summary>
    public virtual async Task<bool> BackupServiceDataAsync(string backupPath)
    {
        if (PersistentStorage == null)
        {
            Logger.LogWarning("No persistent storage configured for {ServiceName}, cannot create backup", Name);
            return false;
        }

        return await PersistentStorage.BackupServiceDataAsync(ServicePrefix, backupPath, Logger);
    }

    /// <summary>
    /// Restores service data from backup.
    /// </summary>
    public virtual async Task<bool> RestoreServiceDataAsync(string backupPath)
    {
        if (PersistentStorage == null)
        {
            Logger.LogWarning("No persistent storage configured for {ServiceName}, cannot restore backup", Name);
            return false;
        }

        var success = await PersistentStorage.RestoreServiceDataAsync(backupPath, Logger);

        if (success)
        {
            // Reload data after restore
            await LoadPersistentDataAsync();
        }

        return success;
    }

    /// <summary>
    /// Validates storage integrity for this service.
    /// </summary>
    public virtual async Task<bool> ValidateStorageIntegrityAsync()
    {
        if (PersistentStorage == null)
        {
            return true; // No storage to validate
        }

        var result = await PersistentStorage.ValidateStorageAsync(Logger);
        return result.IsValid;
    }

    /// <summary>
    /// Gets storage statistics for this service.
    /// </summary>
    public virtual async Task<StorageStatistics> GetStorageStatisticsAsync()
    {
        if (PersistentStorage == null)
        {
            return new StorageStatistics();
        }

        return await PersistentStorage.GetStorageStatisticsAsync(ServicePrefix, Logger);
    }

    /// <summary>
    /// Override to load persistent data during initialization.
    /// </summary>
    protected override async Task<bool> OnInitializeAsync()
    {
        StartedAt = DateTime.UtcNow;

        if (PersistentStorage != null)
        {
            await LoadPersistentDataAsync();
        }

        return true;
    }

    /// <summary>
    /// Override to persist final state on shutdown.
    /// </summary>
    protected override async Task<bool> OnStopAsync()
    {
        if (PersistentStorage != null)
        {
            await PersistServiceStatisticsAsync();
            await OnFinalPersistenceAsync();
        }

        PersistenceTimer?.Dispose();
        CleanupTimer?.Dispose();

        return true;
    }

    /// <summary>
    /// Called during shutdown to perform final persistence operations.
    /// </summary>
    protected virtual async Task OnFinalPersistenceAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes an operation with transaction support if available.
    /// </summary>
    protected async Task<T> ExecuteWithTransactionAsync<T>(Func<Task<T>> operation)
    {
        if (PersistentStorage?.SupportsTransactions == true)
        {
            return await PersistentStorage.ExecuteTransactionAsync(async _ => await operation(), Logger);
        }
        else
        {
            return await operation();
        }
    }

    /// <summary>
    /// Stores an object with standard patterns.
    /// </summary>
    protected async Task<bool> StoreObjectAsync<T>(string key, T obj, StorageOptions? options = null, string? operationName = null)
    {
        if (PersistentStorage == null) return false;

        return await PersistentStorage.StoreObjectAsync(key, obj, options, Logger, operationName);
    }

    /// <summary>
    /// Retrieves an object with standard patterns.
    /// </summary>
    protected async Task<T?> RetrieveObjectAsync<T>(string key, string? operationName = null) where T : class
    {
        if (PersistentStorage == null) return null;

        return await PersistentStorage.RetrieveObjectAsync<T>(key, Logger, operationName);
    }

    /// <summary>
    /// Updates an index with standard patterns.
    /// </summary>
    protected async Task<bool> UpdateIndexAsync(string indexKey, string itemId, bool add = true)
    {
        if (PersistentStorage == null) return false;

        return await PersistentStorage.UpdateIndexAsync(indexKey, itemId, add, Logger);
    }
}
