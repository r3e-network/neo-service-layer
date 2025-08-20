using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.ProofOfReserve.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Persistent storage extensions for ProofOfReserveService.
/// </summary>
public partial class ProofOfReserveService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private const string ASSET_PREFIX = "por:asset:";
    private const string SNAPSHOT_PREFIX = "por:snapshot:";
    private const string ALERT_PREFIX = "por:alert:";
    private const string SUBSCRIPTION_PREFIX = "por:subscription:";
    private const string VERIFICATION_PREFIX = "por:verification:";
    private const string INDEX_PREFIX = "por:index:";
    private const string STATS_KEY = "por:statistics";

    /// <summary>
    /// Loads persistent monitored assets from storage.
    /// </summary>
    private async Task LoadPersistentAssetsAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("Persistent storage not available for proof of reserve service");
            return;
        }

        try
        {
            Logger.LogInformation("Loading persistent monitored assets...");

            // Load monitored assets
            var assetKeys = await _persistentStorage.ListKeysAsync(ASSET_PREFIX);
            foreach (var key in assetKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var asset = JsonSerializer.Deserialize<MonitoredAsset>(data);
                    if (asset != null && asset.IsActive)
                    {
                        lock (_assetsLock)
                        {
                            _monitoredAssets[asset.AssetId] = asset;
                        }
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} monitored assets from persistent storage", _monitoredAssets.Count);

            // Load reserve history
            await LoadReserveHistoryAsync();

            // Load alert configurations
            await LoadAlertConfigurationsAsync();

            // Load subscriptions
            await LoadSubscriptionsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent monitored assets");
        }
    }

    /// <summary>
    /// Persists a monitored asset to storage.
    /// </summary>
    private async Task PersistMonitoredAssetAsync(MonitoredAsset asset)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{ASSET_PREFIX}{asset.AssetId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(asset);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "MonitoredAsset",
                    ["AssetId"] = asset.AssetId,
                    ["AssetName"] = asset.AssetName,
                    ["AssetType"] = asset.AssetType,
                    ["BlockchainType"] = asset.BlockchainType.ToString(),
                    ["IsActive"] = asset.IsActive.ToString(),
                    ["CreatedAt"] = asset.CreatedAt.ToString("O")
                }
            });

            // Update indexes
            await UpdateAssetIndexesAsync(asset);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting monitored asset {AssetId}", asset.AssetId);
        }
    }

    /// <summary>
    /// Removes a monitored asset from persistent storage.
    /// </summary>
    private async Task RemovePersistedAssetAsync(string assetId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{ASSET_PREFIX}{assetId}";
            await _persistentStorage.DeleteAsync(key);

            // Remove from indexes
            await RemoveFromAssetIndexesAsync(assetId);

            // Clean up related data
            await CleanupAssetDataAsync(assetId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing persisted asset {AssetId}", assetId);
        }
    }

    /// <summary>
    /// Updates asset indexes for efficient queries.
    /// </summary>
    private async Task UpdateAssetIndexesAsync(MonitoredAsset asset)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Index by asset type
            var typeIndexKey = $"{INDEX_PREFIX}type:{asset.AssetType}:{asset.AssetId}";
            var indexData = JsonSerializer.SerializeToUtf8Bytes(new AssetIndex
            {
                AssetId = asset.AssetId,
                IndexedAt = DateTime.UtcNow
            });

            await _persistentStorage.StoreAsync(typeIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by blockchain
            var blockchainIndexKey = $"{INDEX_PREFIX}blockchain:{asset.BlockchainType}:{asset.AssetId}";
            await _persistentStorage.StoreAsync(blockchainIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by status
            var statusIndexKey = $"{INDEX_PREFIX}status:{(asset.IsActive ? "active" : "inactive")}:{asset.AssetId}";
            await _persistentStorage.StoreAsync(statusIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating asset indexes for {AssetId}", asset.AssetId);
        }
    }

    /// <summary>
    /// Removes asset from all indexes.
    /// </summary>
    private async Task RemoveFromAssetIndexesAsync(string assetId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var indexKeys = await _persistentStorage.ListKeysAsync(INDEX_PREFIX);
            var keysToDelete = indexKeys.Where(k => k.EndsWith($":{assetId}")).ToList();

            foreach (var key in keysToDelete)
            {
                await _persistentStorage.DeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing asset indexes for {AssetId}", assetId);
        }
    }

    /// <summary>
    /// Persists a reserve snapshot to storage.
    /// </summary>
    private async Task PersistSnapshotAsync(string assetId, ReserveSnapshot snapshot)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SNAPSHOT_PREFIX}{assetId}:{snapshot.SnapshotId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(snapshot);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90), // Keep snapshots for 90 days
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "ReserveSnapshot",
                    ["AssetId"] = assetId,
                    ["SnapshotId"] = snapshot.SnapshotId,
                    ["Timestamp"] = snapshot.Timestamp.ToString("O"),
                    ["TotalReserves"] = snapshot.TotalReserves.ToString(),
                    ["VerificationStatus"] = snapshot.VerificationStatus.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting snapshot {SnapshotId} for asset {AssetId}", snapshot.SnapshotId, assetId);
        }
    }

    /// <summary>
    /// Loads reserve history from storage.
    /// </summary>
    private async Task LoadReserveHistoryAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var snapshotKeys = await _persistentStorage.ListKeysAsync(SNAPSHOT_PREFIX);

            foreach (var key in snapshotKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var snapshot = JsonSerializer.Deserialize<ReserveSnapshot>(data);
                    if (snapshot != null)
                    {
                        // Extract asset ID from key
                        var parts = key.Replace(SNAPSHOT_PREFIX, "").Split(':');
                        if (parts.Length >= 1)
                        {
                            var assetId = parts[0];
                            lock (_assetsLock)
                            {
                                if (!_reserveHistory.ContainsKey(assetId))
                                {
                                    _reserveHistory[assetId] = new List<ReserveSnapshot>();
                                }
                                _reserveHistory[assetId].Add(snapshot);
                            }
                        }
                    }
                }
            }

            // Sort history by timestamp and limit to recent entries
            lock (_assetsLock)
            {
                foreach (var kvp in _reserveHistory)
                {
                    kvp.Value.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

                    // Keep only last 100 snapshots per asset
                    if (kvp.Value.Count > 100)
                    {
                        kvp.Value.RemoveRange(100, kvp.Value.Count - 100);
                    }
                }
            }

            Logger.LogInformation("Loaded reserve history for {Count} assets", _reserveHistory.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading reserve history");
        }
    }

    /// <summary>
    /// Loads alert configurations from storage.
    /// </summary>
    private async Task LoadAlertConfigurationsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var alertKeys = await _persistentStorage.ListKeysAsync(ALERT_PREFIX);

            foreach (var key in alertKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var alertConfig = JsonSerializer.Deserialize<Core.ReserveAlertConfig>(data);
                    if (alertConfig != null)
                    {
                        lock (_assetsLock)
                        {
                            _alertConfigs[alertConfig.AssetId] = alertConfig;
                        }
                    }
                }
            }

            Logger.LogInformation("Loaded {Count} alert configurations", _alertConfigs.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading alert configurations");
        }
    }

    /// <summary>
    /// Persists an alert configuration to storage.
    /// </summary>
    private async Task PersistAlertConfigAsync(Core.ReserveAlertConfig alertConfig)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{ALERT_PREFIX}{alertConfig.AssetId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(alertConfig);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "ReserveAlertConfig",
                    ["AssetId"] = alertConfig.AssetId,
                    ["ThresholdPercentage"] = alertConfig.ThresholdPercentage.ToString(),
                    ["IsEnabled"] = alertConfig.IsEnabled.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting alert config for asset {AssetId}", alertConfig.AssetId);
        }
    }

    /// <summary>
    /// Loads subscriptions from storage.
    /// </summary>
    private async Task LoadSubscriptionsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var subscriptionKeys = await _persistentStorage.ListKeysAsync(SUBSCRIPTION_PREFIX);

            foreach (var key in subscriptionKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var subscription = JsonSerializer.Deserialize<ReserveSubscription>(data);
                    if (subscription != null && subscription.IsActive)
                    {
                        // Extract asset ID from key
                        var parts = key.Replace(SUBSCRIPTION_PREFIX, "").Split(':');
                        if (parts.Length >= 1)
                        {
                            var assetId = parts[0];
                            lock (_assetsLock)
                            {
                                if (!_subscriptions.ContainsKey(assetId))
                                {
                                    _subscriptions[assetId] = new List<ReserveSubscription>();
                                }
                                _subscriptions[assetId].Add(subscription);
                            }
                        }
                    }
                }
            }

            Logger.LogInformation("Loaded subscriptions for {Count} assets", _subscriptions.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading subscriptions");
        }
    }

    /// <summary>
    /// Persists a subscription to storage.
    /// </summary>
    private async Task PersistSubscriptionAsync(string assetId, ReserveSubscription subscription)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SUBSCRIPTION_PREFIX}{assetId}:{subscription.SubscriptionId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(subscription);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "ReserveSubscription",
                    ["AssetId"] = assetId,
                    ["SubscriptionId"] = subscription.SubscriptionId,
                    ["CallbackUrl"] = subscription.CallbackUrl,
                    ["IsActive"] = subscription.IsActive.ToString(),
                    ["CreatedAt"] = subscription.CreatedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting subscription {SubscriptionId} for asset {AssetId}", subscription.SubscriptionId, assetId);
        }
    }

    /// <summary>
    /// Persists verification result.
    /// </summary>
    private async Task PersistVerificationResultAsync(string assetId, VerificationRecord verification)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{VERIFICATION_PREFIX}{assetId}:{verification.VerificationId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(verification);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365), // Keep verification records for 1 year
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "VerificationRecord",
                    ["AssetId"] = assetId,
                    ["VerificationId"] = verification.VerificationId,
                    ["Timestamp"] = verification.Timestamp.ToString("O"),
                    ["Result"] = verification.Result.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting verification {VerificationId} for asset {AssetId}", verification.VerificationId, assetId);
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
            var stats = new ProofOfReserveStatistics
            {
                TotalMonitoredAssets = _monitoredAssets.Count,
                ActiveAssets = _monitoredAssets.Values.Count(a => a.IsActive),
                TotalSnapshots = _reserveHistory.Values.Sum(h => h.Count),
                TotalAlerts = _alertConfigs.Count,
                ActiveAlerts = _alertConfigs.Values.Count(a => a.IsEnabled),
                TotalSubscriptions = _subscriptions.Values.Sum(s => s.Count),
                ActiveSubscriptions = _subscriptions.Values.SelectMany(s => s).Count(s => s.IsActive),
                LastUpdated = DateTime.UtcNow
            };

            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(STATS_KEY, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "Statistics",
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting proof of reserve statistics");
        }
    }

    /// <summary>
    /// Cleans up asset-related data from storage.
    /// </summary>
    private async Task CleanupAssetDataAsync(string assetId)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Clean up snapshots
            var snapshotKeys = await _persistentStorage.ListKeysAsync($"{SNAPSHOT_PREFIX}{assetId}:");
            foreach (var key in snapshotKeys)
            {
                await _persistentStorage.DeleteAsync(key);
            }

            // Clean up alert config
            await _persistentStorage.DeleteAsync($"{ALERT_PREFIX}{assetId}");

            // Clean up subscriptions
            var subscriptionKeys = await _persistentStorage.ListKeysAsync($"{SUBSCRIPTION_PREFIX}{assetId}:");
            foreach (var key in subscriptionKeys)
            {
                await _persistentStorage.DeleteAsync(key);
            }

            // Clean up verifications
            var verificationKeys = await _persistentStorage.ListKeysAsync($"{VERIFICATION_PREFIX}{assetId}:");
            foreach (var key in verificationKeys)
            {
                await _persistentStorage.DeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cleaning up data for asset {AssetId}", assetId);
        }
    }

    /// <summary>
    /// Performs periodic cleanup of old data.
    /// </summary>
    private async Task CleanupOldDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Clean up old snapshots (older than 90 days)
            var snapshotKeys = await _persistentStorage.ListKeysAsync(SNAPSHOT_PREFIX);
            var cutoffDate = DateTime.UtcNow.AddDays(-90);

            foreach (var key in snapshotKeys)
            {
                var metadata = await _persistentStorage.GetMetadataAsync(key);
                if (metadata != null && metadata.CustomMetadata.TryGetValue("Timestamp", out var timestampStr))
                {
                    if (DateTime.TryParse(timestampStr?.ToString(), out var timestamp) && timestamp < cutoffDate)
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
            }

            // Clean up inactive subscriptions
            var subscriptionKeys = await _persistentStorage.ListKeysAsync(SUBSCRIPTION_PREFIX);

            foreach (var key in subscriptionKeys)
            {
                var metadata = await _persistentStorage.GetMetadataAsync(key);
                if (metadata != null && metadata.CustomMetadata.TryGetValue("IsActive", out var isActiveStr))
                {
                    if (!bool.TryParse(isActiveStr?.ToString(), out var isActive) || !isActive)
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
            }

            Logger.LogInformation("Completed cleanup of old proof of reserve data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during proof of reserve data cleanup");
        }
    }
}

/// <summary>
/// Asset index entry.
/// </summary>
internal class AssetIndex
{
    public string AssetId { get; set; } = string.Empty;
    public DateTime IndexedAt { get; set; }
}

/// <summary>
/// Proof of reserve statistics.
/// </summary>
internal class ProofOfReserveStatistics
{
    public int TotalMonitoredAssets { get; set; }
    public int ActiveAssets { get; set; }
    public int TotalSnapshots { get; set; }
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Verification record.
/// </summary>
internal class VerificationRecord
{
    public string VerificationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public VerificationResult Result { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
}

public enum VerificationResult
{
    Passed,
    Failed,
    Warning,
    Error
}
