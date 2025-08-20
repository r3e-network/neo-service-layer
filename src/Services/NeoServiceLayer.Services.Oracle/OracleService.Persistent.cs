using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Oracle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Oracle;

public partial class OracleService
{
    private IPersistentStorageProvider? _persistentStorage;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string DATA_SOURCE_PREFIX = "oracle:datasource:";
    private const string FEED_DATA_PREFIX = "oracle:feed:";
    private const string SUBSCRIPTION_PREFIX = "oracle:subscription:";
    private const string CACHE_PREFIX = "oracle:cache:";
    private const string AGGREGATION_PREFIX = "oracle:aggregation:";
    private const string INDEX_PREFIX = "oracle:index:";
    private const string STATS_PREFIX = "oracle:stats:";

    /// <summary>
    /// Initializes persistent storage for the oracle service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _persistentStorage = _serviceProvider?.GetService(typeof(IPersistentStorageProvider)) as IPersistentStorageProvider;

            if (_persistentStorage != null)
            {
                await _persistentStorage.InitializeAsync();
                Logger.LogInformation("Persistent storage initialized for OracleService");

                // Restore oracle data from storage
                await RestoreOracleDataFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistOracleDataAsync(),
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
                Logger.LogWarning("Persistent storage provider not available for OracleService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize persistent storage for OracleService");
        }
    }

    /// <summary>
    /// Persists a data source to storage.
    /// </summary>
    private async Task PersistDataSourceAsync(DataSource dataSource)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{DATA_SOURCE_PREFIX}{dataSource.Id}";
            var data = JsonSerializer.SerializeToUtf8Bytes(dataSource);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365) // Keep data sources for 1 year
            });

            // Update index
            await UpdateDataSourceIndexAsync(dataSource.Type.ToString(), dataSource.Id);

            Logger.LogDebug("Persisted data source {DataSourceId} to storage", dataSource.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist data source {DataSourceId}", dataSource.Id);
        }
    }

    /// <summary>
    /// Persists oracle feed data to storage.
    /// </summary>
    private async Task PersistFeedDataAsync(string feedId, OracleFeedData feedData)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{FEED_DATA_PREFIX}{feedId}:{feedData.Timestamp.Ticks}";
            var data = JsonSerializer.SerializeToUtf8Bytes(feedData);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30) // Keep feed data for 30 days
            });

            // Store latest feed value in cache
            await CacheFeedValueAsync(feedId, feedData);

            Logger.LogDebug("Persisted feed data for {FeedId} to storage", feedId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist feed data for {FeedId}", feedId);
        }
    }

    /// <summary>
    /// Caches the latest feed value.
    /// </summary>
    private async Task CacheFeedValueAsync(string feedId, OracleFeedData feedData)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{CACHE_PREFIX}{feedId}:latest";
            var data = JsonSerializer.SerializeToUtf8Bytes(feedData);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = false,
                TimeToLive = TimeSpan.FromMinutes(5) // Short TTL for cache
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cache feed value for {FeedId}", feedId);
        }
    }

    /// <summary>
    /// Persists a subscription to storage.
    /// </summary>
    private async Task PersistSubscriptionAsync(OracleSubscription subscription)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SUBSCRIPTION_PREFIX}{subscription.Id}";
            var data = JsonSerializer.SerializeToUtf8Bytes(subscription);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep subscriptions for 90 days
            });

            // Update subscription index
            await UpdateSubscriptionIndexAsync(subscription.SubscriberId, subscription.Id);

            Logger.LogDebug("Persisted subscription {SubscriptionId} to storage", subscription.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist subscription {SubscriptionId}", subscription.Id);
        }
    }

    /// <summary>
    /// Persists aggregated data to storage.
    /// </summary>
    private async Task PersistAggregatedDataAsync(string feedId, AggregatedOracleData aggregatedData)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{AGGREGATION_PREFIX}{feedId}:{aggregatedData.Period}:{aggregatedData.Timestamp.Ticks}";
            var data = JsonSerializer.SerializeToUtf8Bytes(aggregatedData);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365) // Keep aggregated data for 1 year
            });

            Logger.LogDebug("Persisted aggregated data for {FeedId} to storage", feedId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist aggregated data for {FeedId}", feedId);
        }
    }

    /// <summary>
    /// Updates data source type index in storage.
    /// </summary>
    private async Task UpdateDataSourceIndexAsync(string sourceType, string dataSourceId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}source_type:{sourceType}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var sourceIds = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            sourceIds.Add(dataSourceId);

            var data = JsonSerializer.SerializeToUtf8Bytes(sourceIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update data source type index for {SourceType}", sourceType);
        }
    }

    /// <summary>
    /// Updates subscription index in storage.
    /// </summary>
    private async Task UpdateSubscriptionIndexAsync(string userId, string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{INDEX_PREFIX}user:{userId}";
            var existingData = await _persistentStorage.RetrieveAsync(key);

            var subscriptionIds = existingData != null
                ? JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>()
                : new HashSet<string>();

            subscriptionIds.Add(subscriptionId);

            var data = JsonSerializer.SerializeToUtf8Bytes(subscriptionIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update subscription index for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Restores oracle data from persistent storage.
    /// </summary>
    private async Task RestoreOracleDataFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Restoring oracle data from persistent storage");

            // Restore data sources
            await RestoreDataSourcesFromStorageAsync();

            // Restore subscriptions
            await RestoreSubscriptionsFromStorageAsync();

            // Restore cached feed values
            await RestoreCachedFeedValuesAsync();

            // Restore statistics
            await RestoreStatisticsAsync();

            Logger.LogInformation("Oracle data restored from storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore oracle data from storage");
        }
    }

    /// <summary>
    /// Restores data sources from storage.
    /// </summary>
    private async Task RestoreDataSourcesFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var dataSourceKeys = await _persistentStorage.ListKeysAsync($"{DATA_SOURCE_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in dataSourceKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var dataSource = JsonSerializer.Deserialize<DataSource>(data);
                        if (dataSource != null)
                        {
                            _dataSources.Add(dataSource);
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore data source from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} data sources from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore data sources from storage");
        }
    }

    /// <summary>
    /// Restores subscriptions from storage.
    /// </summary>
    private async Task RestoreSubscriptionsFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var subscriptionKeys = await _persistentStorage.ListKeysAsync($"{SUBSCRIPTION_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in subscriptionKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var subscription = JsonSerializer.Deserialize<OracleSubscription>(data);
                        if (subscription != null && subscription.IsActive)
                        {
                            _subscriptions[subscription.Id] = subscription;
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore subscription from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} active subscriptions from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore subscriptions from storage");
        }
    }

    /// <summary>
    /// Restores cached feed values from storage.
    /// </summary>
    private async Task RestoreCachedFeedValuesAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var cacheKeys = await _persistentStorage.ListKeysAsync($"{CACHE_PREFIX}*:latest");
            var restoredCount = 0;

            foreach (var key in cacheKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);
                    if (data != null)
                    {
                        var feedData = JsonSerializer.Deserialize<OracleFeedData>(data);
                        if (feedData != null)
                        {
                            // Cache is restored implicitly through the storage provider
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore cached feed value from key {Key}", key);
                }
            }

            Logger.LogInformation("Restored {Count} cached feed values", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore cached feed values from storage");
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
                var stats = JsonSerializer.Deserialize<OracleServiceStatistics>(data);
                if (stats != null)
                {
                    _requestCount = stats.TotalRequests;
                    _successCount = stats.SuccessfulRequests;
                    _failureCount = stats.FailedRequests;

                    Logger.LogInformation("Restored oracle statistics from storage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore statistics from storage");
        }
    }

    /// <summary>
    /// Persists all current oracle data to storage.
    /// </summary>
    private async Task PersistOracleDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Persist data sources
            foreach (var dataSource in _dataSources)
            {
                await PersistDataSourceAsync(dataSource);
            }

            // Persist subscriptions
            foreach (var subscription in _subscriptions.Values)
            {
                await PersistSubscriptionAsync(subscription);
            }

            // Persist statistics
            await PersistServiceStatisticsAsync();

            Logger.LogDebug("Persisted oracle data to storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist oracle data");
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
            var stats = new OracleServiceStatistics
            {
                TotalRequests = _requestCount,
                SuccessfulRequests = _successCount,
                FailedRequests = _failureCount,
                ActiveDataSources = _dataSources.Count,
                ActiveSubscriptions = _subscriptions.Count,
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
            Logger.LogInformation("Starting cleanup of expired oracle data");

            // Clean up old feed data (older than 30 days)
            var feedKeys = await _persistentStorage.ListKeysAsync($"{FEED_DATA_PREFIX}*");
            var cleanedCount = 0;

            foreach (var key in feedKeys)
            {
                try
                {
                    // Extract timestamp from key
                    var parts = key.Split(':');
                    if (parts.Length >= 3 && long.TryParse(parts[^1], out var ticks))
                    {
                        var timestamp = new DateTime(ticks);
                        if (timestamp < DateTime.UtcNow.AddDays(-30))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup feed data key {Key}", key);
                }
            }

            // Clean up expired subscriptions
            var subscriptionKeys = await _persistentStorage.ListKeysAsync($"{SUBSCRIPTION_PREFIX}*");
            foreach (var key in subscriptionKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var subscription = JsonSerializer.Deserialize<OracleSubscription>(data);
                        if (subscription != null && subscription.ExpiresAt < DateTime.UtcNow)
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup subscription key {Key}", key);
                }
            }

            Logger.LogInformation("Cleaned up {Count} expired oracle data entries", cleanedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data");
        }
    }

    /// <summary>
    /// Removes a data source from persistent storage.
    /// </summary>
    private async Task RemoveDataSourceFromStorageAsync(string dataSourceId)
    {
        if (_persistentStorage == null) return;

        try
        {
            await _persistentStorage.DeleteAsync($"{DATA_SOURCE_PREFIX}{dataSourceId}");

            Logger.LogDebug("Removed data source {DataSourceId} from storage", dataSourceId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove data source {DataSourceId} from storage", dataSourceId);
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
/// Oracle feed data model.
/// </summary>
internal class OracleFeedData
{
    public string FeedId { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Aggregated oracle data model.
/// </summary>
internal class AggregatedOracleData
{
    public string FeedId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty; // hourly, daily, weekly
    public DateTime Timestamp { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Average { get; set; }
    public int Count { get; set; }
}


/// <summary>
/// Statistics for oracle service.
/// </summary>
internal class OracleServiceStatistics
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int ActiveDataSources { get; set; }
    public int ActiveSubscriptions { get; set; }
    public DateTime LastUpdated { get; set; }
}
