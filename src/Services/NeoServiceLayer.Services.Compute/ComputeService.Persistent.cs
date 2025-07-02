using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;

namespace NeoServiceLayer.Services.Compute;

public partial class ComputeService
{
    private IPersistentStorageProvider? _persistentStorage;
    private readonly IServiceProvider? _serviceProvider;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string COMPUTATION_PREFIX = "compute:computation:";
    private const string RESULT_PREFIX = "compute:result:";
    private const string METADATA_PREFIX = "compute:metadata:";
    private const string INDEX_PREFIX = "compute:index:";
    private const string STATS_PREFIX = "compute:stats:";
    private const string JOB_PREFIX = "compute:job:";

    /// <summary>
    /// Initializes persistent storage for the compute service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _persistentStorage = _serviceProvider?.GetService(typeof(IPersistentStorageProvider)) as IPersistentStorageProvider;

            if (_persistentStorage != null)
            {
                await _persistentStorage.InitializeAsync();
                Logger.LogInformation("Persistent storage initialized for ComputeService");

                // Restore compute data from storage
                await RestoreComputeDataFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistComputeDataAsync(),
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
                Logger.LogWarning("Persistent storage provider not available for ComputeService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize persistent storage for ComputeService");
        }
    }

    /// <summary>
    /// Persists computation metadata to storage.
    /// </summary>
    private async Task PersistComputationMetadataAsync(ComputationMetadata metadata)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{METADATA_PREFIX}{metadata.ComputationId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(metadata);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep metadata for 90 days
            });

            // Update index
            await UpdateComputationIndexAsync(metadata.ComputationType, metadata.ComputationId);

            Logger.LogDebug("Persisted computation metadata {ComputationId} to storage", metadata.ComputationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist computation metadata {ComputationId}", metadata.ComputationId);
        }
    }

    /// <summary>
    /// Persists computation result to storage.
    /// </summary>
    private async Task PersistComputationResultAsync(string computationId, ComputationResult result)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{RESULT_PREFIX}{computationId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(result);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30) // Keep results for 30 days
            });

            Logger.LogDebug("Persisted computation result {ComputationId} to storage", computationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist computation result {ComputationId}", computationId);
        }
    }

    /// <summary>
    /// Persists computation job to storage.
    /// </summary>
    private async Task PersistComputationJobAsync(ComputationJob job)
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
                TimeToLive = TimeSpan.FromDays(7) // Keep jobs for 7 days
            });

            // Update job index by status
            await UpdateJobIndexAsync(job.Status, job.JobId);

            Logger.LogDebug("Persisted computation job {JobId} to storage", job.JobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist computation job {JobId}", job.JobId);
        }
    }

    /// <summary>
    /// Updates computation type index in storage.
    /// </summary>
    private async Task UpdateComputationIndexAsync(string computationType, string computationId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}type:{computationType}";
            var existingData = await _persistentStorage.RetrieveAsync(key);
            
            var computationIds = existingData != null 
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            computationIds.Add(computationId);

            var data = JsonSerializer.SerializeToUtf8Bytes(computationIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update computation index for type {ComputationType}", computationType);
        }
    }

    /// <summary>
    /// Updates job status index in storage.
    /// </summary>
    private async Task UpdateJobIndexAsync(JobStatus status, string jobId)
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
    /// Restores compute data from persistent storage.
    /// </summary>
    private async Task RestoreComputeDataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Restoring compute data from persistent storage");

            // Restore computation metadata
            await RestoreComputationMetadataFromStorageAsync();

            // Restore recent results
            await RestoreRecentResultsFromStorageAsync();

            // Restore active jobs
            await RestoreActiveJobsFromStorageAsync();

            // Restore statistics
            await RestoreStatisticsAsync();

            Logger.LogInformation("Compute data restored from storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore compute data from storage");
        }
    }

    /// <summary>
    /// Restores computation metadata from storage.
    /// </summary>
    private async Task RestoreComputationMetadataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var metadataKeys = await _persistentStorage.ListKeysAsync($"{METADATA_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in metadataKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var metadata = JsonSerializer.Deserialize<ComputationMetadata>(data);
                        if (metadata != null)
                        {
                            _computationCache[metadata.ComputationId] = metadata;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore computation metadata from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} computation metadata entries from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore computation metadata from storage");
        }
    }

    /// <summary>
    /// Restores recent results from storage.
    /// </summary>
    private async Task RestoreRecentResultsFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var resultKeys = await _persistentStorage.ListKeysAsync($"{RESULT_PREFIX}*");
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Only restore results from last 7 days
            var restoredCount = 0;

            foreach (var key in resultKeys.Take(100)) // Limit to recent 100 results
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var result = JsonSerializer.Deserialize<ComputationResult>(data);
                        if (result != null && result.Timestamp >= cutoffDate)
                        {
                            var computationId = key.Replace(RESULT_PREFIX, "");
                            _resultCache[computationId] = result;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore result from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} recent computation results", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore results from storage");
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
                        var job = JsonSerializer.Deserialize<ComputationJob>(data);
                        if (job != null && job.Status != JobStatus.Completed && job.Status != JobStatus.Failed)
                        {
                            // Mark as failed if job was running when service stopped
                            if (job.Status == JobStatus.Running)
                            {
                                job.Status = JobStatus.Failed;
                                job.ErrorMessage = "Service restarted while job was running";
                                await PersistComputationJobAsync(job);
                            }
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore job from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} active jobs from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore jobs from storage");
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
                var stats = JsonSerializer.Deserialize<ComputeServiceStatistics>(data);
                if (stats != null)
                {
                    _requestCount = stats.TotalRequests;
                    _successCount = stats.SuccessfulRequests;
                    _failureCount = stats.FailedRequests;
                    
                    Logger.LogInformation("Restored compute service statistics from storage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore statistics from storage");
        }
    }

    /// <summary>
    /// Persists all current compute data to storage.
    /// </summary>
    private async Task PersistComputeDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Persist metadata
            foreach (var metadata in _computationCache.Values)
            {
                await PersistComputationMetadataAsync(metadata);
            }

            // Persist statistics
            await PersistServiceStatisticsAsync();

            Logger.LogDebug("Persisted compute data to storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist compute data");
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
            var stats = new ComputeServiceStatistics
            {
                TotalRequests = _requestCount,
                SuccessfulRequests = _successCount,
                FailedRequests = _failureCount,
                ActiveComputations = _computationCache.Count,
                CachedResults = _resultCache.Count,
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
            Logger.LogInformation("Starting cleanup of expired compute data");

            // Clean up old results (older than 30 days)
            var resultKeys = await _persistentStorage.ListKeysAsync($"{RESULT_PREFIX}*");
            var cleanedCount = 0;

            foreach (var key in resultKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var result = JsonSerializer.Deserialize<ComputationResult>(data);
                        if (result != null && result.Timestamp < DateTime.UtcNow.AddDays(-30))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup result key {Key}", key);
                }
            }

            // Clean up old jobs (older than 7 days)
            var jobKeys = await _persistentStorage.ListKeysAsync($"{JOB_PREFIX}*");
            foreach (var key in jobKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var job = JsonSerializer.Deserialize<ComputationJob>(data);
                        if (job != null && job.CreatedAt < DateTime.UtcNow.AddDays(-7))
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

            Logger.LogInformation("Cleaned up {Count} expired compute data entries", cleanedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data");
        }
    }

    /// <summary>
    /// Removes computation data from persistent storage.
    /// </summary>
    private async Task RemoveComputationFromStorageAsync(string computationId)
    {
        if (_persistentStorage == null) return;

        try
        {
            await _persistentStorage.DeleteAsync($"{METADATA_PREFIX}{computationId}");
            await _persistentStorage.DeleteAsync($"{RESULT_PREFIX}{computationId}");
            
            Logger.LogDebug("Removed computation {ComputationId} from storage", computationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove computation {ComputationId} from storage", computationId);
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
/// Computation job model.
/// </summary>
internal class ComputationJob
{
    public string JobId { get; set; } = string.Empty;
    public string ComputationId { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Job status enumeration.
/// </summary>
internal enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Statistics for compute service.
/// </summary>
internal class ComputeServiceStatistics
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int ActiveComputations { get; set; }
    public int CachedResults { get; set; }
    public DateTime LastUpdated { get; set; }
}