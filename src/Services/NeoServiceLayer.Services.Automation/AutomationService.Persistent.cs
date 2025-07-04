using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;

namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Persistent storage extensions for AutomationService.
/// </summary>
public partial class AutomationService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private const string JOB_PREFIX = "automation:job:";
    private const string EXECUTION_PREFIX = "automation:execution:";
    private const string SCHEDULE_PREFIX = "automation:schedule:";
    private const string OWNER_INDEX_PREFIX = "automation:index:owner:";
    private const string TRIGGER_INDEX_PREFIX = "automation:index:trigger:";
    private const string STATS_KEY = "automation:statistics";

    /// <summary>
    /// Loads persistent jobs from storage.
    /// </summary>
    private async Task LoadPersistentJobsAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("Persistent storage not available for automation service");
            return;
        }

        try
        {
            Logger.LogInformation("Loading persistent automation jobs...");

            // Load jobs
            var jobKeys = await _persistentStorage.ListKeysAsync(JOB_PREFIX);
            foreach (var key in jobKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var job = JsonSerializer.Deserialize<AutomationJob>(data);
                    if (job != null && job.Status != AutomationJobStatus.Completed)
                    {
                        lock (_jobsLock)
                        {
                            _jobs[job.Id] = job;

                            // Initialize execution history
                            if (!_executionHistory.ContainsKey(job.Id))
                            {
                                _executionHistory[job.Id] = new List<AutomationExecution>();
                            }
                        }
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} automation jobs from persistent storage", _jobs.Count);

            // Load execution history
            await LoadExecutionHistoryAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent automation jobs");
        }
    }

    /// <summary>
    /// Persists an automation job to storage.
    /// </summary>
    private async Task PersistJobAsync(AutomationJob job)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{JOB_PREFIX}{job.Id}";
            var data = JsonSerializer.SerializeToUtf8Bytes(job);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = job.ExpiresAt.HasValue ? job.ExpiresAt.Value - DateTime.UtcNow : null,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "AutomationJob",
                    ["JobId"] = job.Id,
                    ["Name"] = job.Name,
                    ["Owner"] = job.OwnerAddress,
                    ["Status"] = job.Status.ToString(),
                    ["CreatedAt"] = job.CreatedAt.ToString("O")
                }
            });

            // Update indexes
            await UpdateJobIndexesAsync(job);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting automation job {JobId}", job.Id);
        }
    }

    /// <summary>
    /// Removes a job from persistent storage.
    /// </summary>
    private async Task RemovePersistedJobAsync(string jobId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{JOB_PREFIX}{jobId}";
            await _persistentStorage.DeleteAsync(key);

            // Remove from indexes
            await RemoveFromJobIndexesAsync(jobId);

            // Clean up execution history
            await CleanupExecutionHistoryAsync(jobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing persisted job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Updates job indexes for efficient queries.
    /// </summary>
    private async Task UpdateJobIndexesAsync(AutomationJob job)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Index by owner
            var ownerIndexKey = $"{OWNER_INDEX_PREFIX}{job.OwnerAddress}:{job.Id}";
            var indexData = JsonSerializer.SerializeToUtf8Bytes(new JobIndex
            {
                JobId = job.Id,
                IndexedAt = DateTime.UtcNow
            });

            await _persistentStorage.StoreAsync(ownerIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by trigger type
            var triggerIndexKey = $"{TRIGGER_INDEX_PREFIX}{job.Trigger.Type}:{job.Id}";
            await _persistentStorage.StoreAsync(triggerIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating job indexes for {JobId}", job.Id);
        }
    }

    /// <summary>
    /// Removes job from all indexes.
    /// </summary>
    private async Task RemoveFromJobIndexesAsync(string jobId)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Remove from owner index
            var ownerIndexKeys = await _persistentStorage.ListKeysAsync(OWNER_INDEX_PREFIX);
            var keysToDelete = ownerIndexKeys.Where(k => k.EndsWith($":{jobId}")).ToList();

            foreach (var key in keysToDelete)
            {
                await _persistentStorage.DeleteAsync(key);
            }

            // Remove from trigger index
            var triggerIndexKeys = await _persistentStorage.ListKeysAsync(TRIGGER_INDEX_PREFIX);
            keysToDelete = triggerIndexKeys.Where(k => k.EndsWith($":{jobId}")).ToList();

            foreach (var key in keysToDelete)
            {
                await _persistentStorage.DeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing job indexes for {JobId}", jobId);
        }
    }

    /// <summary>
    /// Persists execution history entry.
    /// </summary>
    private async Task PersistExecutionAsync(string jobId, AutomationExecution execution)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{EXECUTION_PREFIX}{jobId}:{execution.Id}";
            var data = JsonSerializer.SerializeToUtf8Bytes(execution);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30), // Keep execution history for 30 days
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "AutomationExecution",
                    ["JobId"] = jobId,
                    ["ExecutionId"] = execution.Id,
                    ["Status"] = execution.Status.ToString(),
                    ["ExecutedAt"] = execution.ExecutedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting execution {ExecutionId} for job {JobId}", execution.Id, jobId);
        }
    }

    /// <summary>
    /// Loads execution history from storage.
    /// </summary>
    private async Task LoadExecutionHistoryAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var executionKeys = await _persistentStorage.ListKeysAsync(EXECUTION_PREFIX);

            foreach (var key in executionKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var execution = JsonSerializer.Deserialize<AutomationExecution>(data);
                    if (execution != null)
                    {
                        // Extract job ID from key
                        var parts = key.Replace(EXECUTION_PREFIX, "").Split(':');
                        if (parts.Length >= 1)
                        {
                            var jobId = parts[0];
                            lock (_jobsLock)
                            {
                                if (!_executionHistory.ContainsKey(jobId))
                                {
                                    _executionHistory[jobId] = new List<AutomationExecution>();
                                }
                                _executionHistory[jobId].Add(execution);
                            }
                        }
                    }
                }
            }

            // Sort execution history by date
            lock (_jobsLock)
            {
                foreach (var kvp in _executionHistory)
                {
                    kvp.Value.Sort((a, b) => b.ExecutedAt.CompareTo(a.ExecutedAt));

                    // Keep only last 100 entries per job
                    if (kvp.Value.Count > 100)
                    {
                        kvp.Value.RemoveRange(100, kvp.Value.Count - 100);
                    }
                }
            }

            Logger.LogInformation("Loaded execution history for {Count} jobs", _executionHistory.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading execution history");
        }
    }

    /// <summary>
    /// Cleans up execution history for a job.
    /// </summary>
    private async Task CleanupExecutionHistoryAsync(string jobId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var executionKeys = await _persistentStorage.ListKeysAsync($"{EXECUTION_PREFIX}{jobId}:");

            foreach (var key in executionKeys)
            {
                await _persistentStorage.DeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cleaning up execution history for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Persists service statistics.
    /// </summary>
    private async Task PersistStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var stats = new AutomationStatistics
            {
                TotalJobs = _jobs.Count,
                ActiveJobs = _jobs.Values.Count(j => j.Status == AutomationJobStatus.Active),
                PausedJobs = _jobs.Values.Count(j => j.Status == AutomationJobStatus.Paused),
                FailedJobs = _jobs.Values.Count(j => j.Status == AutomationJobStatus.Failed),
                JobsByTriggerType = _jobs.Values
                    .GroupBy(j => j.Trigger.Type)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                TotalExecutions = _executionHistory.Values.Sum(h => h.Count),
                SuccessfulExecutions = _executionHistory.Values
                    .SelectMany(h => h)
                    .Count(e => e.Status == AutomationExecutionStatus.Completed),
                LastUpdated = DateTime.UtcNow
            };

            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(STATS_KEY, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Statistics",
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting automation statistics");
        }
    }

    /// <summary>
    /// Performs periodic cleanup of old execution data.
    /// </summary>
    private async Task CleanupOldExecutionsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var executionKeys = await _persistentStorage.ListKeysAsync(EXECUTION_PREFIX);

            foreach (var key in executionKeys)
            {
                var metadata = await _persistentStorage.GetMetadataAsync(key);
                if (metadata != null && metadata.CustomMetadata.TryGetValue("ExecutedAt", out var executedAtStr))
                {
                    if (DateTime.TryParse(executedAtStr, out var executedAt) && executedAt < cutoffDate)
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
            }

            Logger.LogInformation("Completed cleanup of old execution data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during execution data cleanup");
        }
    }

    /// <summary>
    /// Gets jobs by owner from persistent storage.
    /// </summary>
    private async Task<List<AutomationJob>> GetJobsByOwnerFromStorageAsync(string ownerAddress)
    {
        if (_persistentStorage == null) return new List<AutomationJob>();

        var jobs = new List<AutomationJob>();

        try
        {
            var indexKeys = await _persistentStorage.ListKeysAsync($"{OWNER_INDEX_PREFIX}{ownerAddress}:");

            foreach (var indexKey in indexKeys)
            {
                // Extract job ID from index key
                var jobId = indexKey.Split(':').LastOrDefault();
                if (!string.IsNullOrEmpty(jobId))
                {
                    var jobKey = $"{JOB_PREFIX}{jobId}";
                    var data = await _persistentStorage.RetrieveAsync(jobKey);
                    if (data != null)
                    {
                        var job = JsonSerializer.Deserialize<AutomationJob>(data);
                        if (job != null)
                        {
                            jobs.Add(job);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting jobs by owner {Owner} from storage", ownerAddress);
        }

        return jobs;
    }
}

/// <summary>
/// Job index entry.
/// </summary>
internal class JobIndex
{
    public string JobId { get; set; } = string.Empty;
    public DateTime IndexedAt { get; set; }
}

/// <summary>
/// Automation statistics.
/// </summary>
internal class AutomationStatistics
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int PausedJobs { get; set; }
    public int FailedJobs { get; set; }
    public Dictionary<string, int> JobsByTriggerType { get; set; } = new();
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public DateTime LastUpdated { get; set; }
}
