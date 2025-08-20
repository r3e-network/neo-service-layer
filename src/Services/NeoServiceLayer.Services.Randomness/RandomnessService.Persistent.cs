using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Randomness;

public partial class RandomnessService
{
    private IPersistentStorageProvider? _persistentStorage;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string RESULT_PREFIX = "randomness:result:";
    private const string HISTORY_PREFIX = "randomness:history:";
    private const string SEED_PREFIX = "randomness:seed:";
    private const string INDEX_PREFIX = "randomness:index:";
    private const string STATS_PREFIX = "randomness:stats:";
    private const string AUDIT_PREFIX = "randomness:audit:";

    /// <summary>
    /// Initializes persistent storage for the randomness service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _persistentStorage = _serviceProvider?.GetService(typeof(IPersistentStorageProvider)) as IPersistentStorageProvider;

            if (_persistentStorage != null)
            {
                await _persistentStorage.InitializeAsync();
                Logger.LogInformation("Persistent storage initialized for RandomnessService");

                // Restore randomness data from storage
                await RestoreRandomnessDataFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistRandomnessDataAsync(),
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
                Logger.LogWarning("Persistent storage provider not available for RandomnessService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize persistent storage for RandomnessService");
        }
    }

    /// <summary>
    /// Persists a verifiable random result to storage.
    /// </summary>
    private async Task PersistRandomResultAsync(VerifiableRandomResult result)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{RESULT_PREFIX}{result.RequestId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(result);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep results for 90 days
            });

            // Store in history
            await PersistToHistoryAsync(result);

            // Update index
            await UpdateRandomnessIndexAsync(result.BlockchainType, result.RequestId);

            Logger.LogDebug("Persisted random result {RequestId} to storage", result.RequestId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist random result {RequestId}", result.RequestId);
        }
    }

    /// <summary>
    /// Persists result to history for audit trail.
    /// </summary>
    private async Task PersistToHistoryAsync(VerifiableRandomResult result)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{HISTORY_PREFIX}{result.Timestamp.Ticks}:{result.RequestId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(result);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365) // Keep history for 1 year
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist result to history {RequestId}", result.RequestId);
        }
    }

    /// <summary>
    /// Persists seed information to storage.
    /// </summary>
    private async Task PersistSeedAsync(string seedId, SeedInfo seedInfo)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SEED_PREFIX}{seedId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(seedInfo);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = false,
                TimeToLive = TimeSpan.FromDays(30) // Keep seeds for 30 days
            });

            Logger.LogDebug("Persisted seed {SeedId} to storage", seedId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist seed {SeedId}", seedId);
        }
    }

    /// <summary>
    /// Persists audit log entry to storage.
    /// </summary>
    private async Task PersistAuditLogAsync(RandomnessAuditEntry auditEntry)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{AUDIT_PREFIX}{auditEntry.AuditId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(auditEntry);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(2555) // Keep audit logs for 7 years
            });

            Logger.LogDebug("Persisted audit log entry {AuditId} to storage", auditEntry.AuditId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist audit log entry {AuditId}", auditEntry.AuditId);
        }
    }

    /// <summary>
    /// Updates randomness index in storage.
    /// </summary>
    private async Task UpdateRandomnessIndexAsync(BlockchainType blockchainType, string requestId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}blockchain:{blockchainType}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var requestIds = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            requestIds.Add(requestId);

            var data = JsonSerializer.SerializeToUtf8Bytes(requestIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update randomness index for {BlockchainType}", blockchainType);
        }
    }

    /// <summary>
    /// Restores randomness data from persistent storage.
    /// </summary>
    private async Task RestoreRandomnessDataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Restoring randomness data from persistent storage");

            // Restore recent results
            await RestoreRecentResultsFromStorageAsync();

            // Restore statistics
            await RestoreStatisticsAsync();

            Logger.LogInformation("Randomness data restored from storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore randomness data from storage");
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
                        var result = JsonSerializer.Deserialize<VerifiableRandomResult>(data);
                        if (result != null && result.Timestamp >= cutoffDate)
                        {
                            _results[result.RequestId] = result;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore result from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} recent random results from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore results from storage");
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
                var stats = JsonSerializer.Deserialize<RandomnessServiceStatistics>(data);
                if (stats != null)
                {
                    _requestCount = stats.TotalRequests;
                    _successCount = stats.SuccessfulRequests;
                    _failureCount = stats.FailedRequests;

                    Logger.LogInformation("Restored randomness service statistics from storage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore statistics from storage");
        }
    }

    /// <summary>
    /// Persists all current randomness data to storage.
    /// </summary>
    private async Task PersistRandomnessDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Persist recent results
            foreach (var result in _results.Values)
            {
                await PersistRandomResultAsync(result);
            }

            // Persist statistics
            await PersistServiceStatisticsAsync();

            Logger.LogDebug("Persisted randomness data to storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist randomness data");
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
            var stats = new RandomnessServiceStatistics
            {
                TotalRequests = _requestCount,
                SuccessfulRequests = _successCount,
                FailedRequests = _failureCount,
                CachedResults = _results.Count,
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
            Logger.LogInformation("Starting cleanup of expired randomness data");

            // Clean up old results (older than 90 days)
            var resultKeys = await _persistentStorage.ListKeysAsync($"{RESULT_PREFIX}*");
            var cleanedCount = 0;

            foreach (var key in resultKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var result = JsonSerializer.Deserialize<VerifiableRandomResult>(data);
                        if (result != null && result.Timestamp < DateTime.UtcNow.AddDays(-90))
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

            // Clean up old seeds (older than 30 days)
            var seedKeys = await _persistentStorage.ListKeysAsync($"{SEED_PREFIX}*");
            foreach (var key in seedKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var seedInfo = JsonSerializer.Deserialize<SeedInfo>(data);
                        if (seedInfo != null && seedInfo.CreatedAt < DateTime.UtcNow.AddDays(-30))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup seed key {Key}", key);
                }
            }

            Logger.LogInformation("Cleaned up {Count} expired randomness data entries", cleanedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data");
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
/// Seed information for randomness generation.
/// </summary>
internal class SeedInfo
{
    public string SeedId { get; set; } = string.Empty;
    public string SeedValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Randomness audit entry.
/// </summary>
internal class RandomnessAuditEntry
{
    public string AuditId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Statistics for randomness service.
/// </summary>
internal class RandomnessServiceStatistics
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int CachedResults { get; set; }
    public DateTime LastUpdated { get; set; }
}
