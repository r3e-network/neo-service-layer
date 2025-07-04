using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Configuration.Models;

namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Persistent storage extensions for ConfigurationService.
/// </summary>
public partial class ConfigurationService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private const string CONFIG_PREFIX = "config:entry:";
    private const string SUBSCRIPTION_PREFIX = "config:subscription:";
    private const string HISTORY_PREFIX = "config:history:";
    private const string INDEX_PREFIX = "config:index:";
    private const string STATS_KEY = "config:statistics";
    private const string AUDIT_PREFIX = "config:audit:";

    /// <summary>
    /// Loads persistent configurations from storage.
    /// </summary>
    private async Task LoadPersistentConfigurationsAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("Persistent storage not available for configuration service");
            return;
        }

        try
        {
            Logger.LogInformation("Loading persistent configurations...");

            // Load configurations
            var configKeys = await _persistentStorage.ListKeysAsync(CONFIG_PREFIX);
            foreach (var key in configKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var entry = JsonSerializer.Deserialize<ConfigurationEntry>(data);
                    if (entry != null)
                    {
                        lock (_configLock)
                        {
                            _configurations[entry.Key] = entry;
                        }
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} configurations from persistent storage", _configurations.Count);

            // Load subscriptions
            await LoadPersistentSubscriptionsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent configurations");
        }
    }

    /// <summary>
    /// Persists a configuration entry to storage.
    /// </summary>
    private async Task PersistConfigurationEntryAsync(ConfigurationEntry entry)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{CONFIG_PREFIX}{entry.Key}";
            var data = JsonSerializer.SerializeToUtf8Bytes(entry);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = entry.EncryptValue,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "ConfigurationEntry",
                    ["Key"] = entry.Key,
                    ["ValueType"] = entry.ValueType.ToString(),
                    ["Version"] = entry.Version.ToString(),
                    ["UpdatedAt"] = entry.UpdatedAt.ToString("O"),
                    ["BlockchainType"] = entry.BlockchainType.ToString()
                }
            });

            // Update indexes
            await UpdateConfigurationIndexesAsync(entry);

            // Add to history
            await AddConfigurationHistoryAsync(entry);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting configuration {Key}", entry.Key);
        }
    }

    /// <summary>
    /// Removes a configuration from persistent storage.
    /// </summary>
    private async Task RemovePersistedConfigurationAsync(string configKey)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{CONFIG_PREFIX}{configKey}";
            await _persistentStorage.DeleteAsync(key);

            // Remove from indexes
            await RemoveFromConfigurationIndexesAsync(configKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing persisted configuration {Key}", configKey);
        }
    }

    /// <summary>
    /// Updates configuration indexes for efficient queries.
    /// </summary>
    private async Task UpdateConfigurationIndexesAsync(ConfigurationEntry entry)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Index by value type
            var typeIndexKey = $"{INDEX_PREFIX}type:{entry.ValueType}:{entry.Key}";
            var indexData = JsonSerializer.SerializeToUtf8Bytes(new ConfigurationIndex
            {
                Key = entry.Key,
                IndexedAt = DateTime.UtcNow
            });

            await _persistentStorage.StoreAsync(typeIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by blockchain type
            var blockchainIndexKey = $"{INDEX_PREFIX}blockchain:{entry.BlockchainType}:{entry.Key}";
            await _persistentStorage.StoreAsync(blockchainIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by namespace (first part of key before '.')
            var namespacePart = entry.Key.Split('.').FirstOrDefault() ?? "global";
            var namespaceIndexKey = $"{INDEX_PREFIX}namespace:{namespacePart}:{entry.Key}";
            await _persistentStorage.StoreAsync(namespaceIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating configuration indexes for {Key}", entry.Key);
        }
    }

    /// <summary>
    /// Removes configuration from all indexes.
    /// </summary>
    private async Task RemoveFromConfigurationIndexesAsync(string configKey)
    {
        if (_persistentStorage == null) return;

        try
        {
            var indexKeys = await _persistentStorage.ListKeysAsync(INDEX_PREFIX);
            var keysToDelete = indexKeys.Where(k => k.EndsWith($":{configKey}")).ToList();

            foreach (var key in keysToDelete)
            {
                await _persistentStorage.DeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing configuration indexes for {Key}", configKey);
        }
    }

    /// <summary>
    /// Adds configuration change to history.
    /// </summary>
    private async Task AddConfigurationHistoryAsync(ConfigurationEntry entry)
    {
        if (_persistentStorage == null) return;

        try
        {
            var historyKey = $"{HISTORY_PREFIX}{entry.Key}:{DateTime.UtcNow.Ticks}";
            var historyEntry = new ConfigurationHistoryEntry
            {
                Key = entry.Key,
                Value = entry.Value?.ToString() ?? string.Empty,
                ValueType = entry.ValueType,
                Version = entry.Version,
                ChangedAt = entry.UpdatedAt,
                ChangedBy = entry.UpdatedBy ?? "system"
            };

            var data = JsonSerializer.SerializeToUtf8Bytes(historyEntry);

            await _persistentStorage.StoreAsync(historyKey, data, new StorageOptions
            {
                Encrypt = entry.EncryptValue,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90), // Keep history for 90 days
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "ConfigurationHistory",
                    ["Key"] = entry.Key,
                    ["Version"] = entry.Version.ToString(),
                    ["ChangedAt"] = entry.UpdatedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding configuration history for {Key}", entry.Key);
        }
    }

    /// <summary>
    /// Loads persistent subscriptions from storage.
    /// </summary>
    private async Task LoadPersistentSubscriptionsAsync()
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
                    var subscription = JsonSerializer.Deserialize<ConfigurationSubscription>(data);
                    if (subscription != null && subscription.IsActive)
                    {
                        lock (_configLock)
                        {
                            _subscriptions[subscription.SubscriptionId] = subscription;
                        }
                    }
                }
            }

            Logger.LogInformation("Loaded {Count} active subscriptions from persistent storage", _subscriptions.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent subscriptions");
        }
    }

    /// <summary>
    /// Persists a subscription to storage.
    /// </summary>
    private async Task PersistSubscriptionAsync(ConfigurationSubscription subscription)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SUBSCRIPTION_PREFIX}{subscription.SubscriptionId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(subscription);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "ConfigurationSubscription",
                    ["SubscriptionId"] = subscription.SubscriptionId,
                    ["ConfigurationKey"] = subscription.ConfigurationKey,
                    ["CallbackUrl"] = subscription.CallbackUrl,
                    ["IsActive"] = subscription.IsActive.ToString(),
                    ["CreatedAt"] = subscription.CreatedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting subscription {SubscriptionId}", subscription.SubscriptionId);
        }
    }

    /// <summary>
    /// Removes a subscription from persistent storage.
    /// </summary>
    private async Task RemovePersistedSubscriptionAsync(string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SUBSCRIPTION_PREFIX}{subscriptionId}";
            await _persistentStorage.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing persisted subscription {SubscriptionId}", subscriptionId);
        }
    }

    /// <summary>
    /// Persists configuration audit entry.
    /// </summary>
    private async Task PersistAuditEntryAsync(ConfigurationAuditEntry auditEntry)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{AUDIT_PREFIX}{DateTime.UtcNow.Ticks}:{auditEntry.ConfigurationKey}";
            var data = JsonSerializer.SerializeToUtf8Bytes(auditEntry);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365), // Keep audit logs for 1 year
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "ConfigurationAudit",
                    ["Action"] = auditEntry.Action,
                    ["ConfigurationKey"] = auditEntry.ConfigurationKey,
                    ["UserId"] = auditEntry.UserId ?? "system",
                    ["Timestamp"] = auditEntry.Timestamp.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting audit entry for {Key}", auditEntry.ConfigurationKey);
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
            var stats = GetStatistics();
            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(STATS_KEY, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Statistics",
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O"),
                    ["TotalConfigurations"] = stats.TotalConfigurations.ToString(),
                    ["ActiveSubscriptions"] = stats.ActiveSubscriptions.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting configuration statistics");
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
            // Clean up old history entries (older than 90 days)
            var historyKeys = await _persistentStorage.ListKeysAsync(HISTORY_PREFIX);
            var cutoffDate = DateTime.UtcNow.AddDays(-90);

            foreach (var key in historyKeys)
            {
                var metadata = await _persistentStorage.GetMetadataAsync(key);
                if (metadata != null && metadata.CustomMetadata.TryGetValue("ChangedAt", out var changedAtStr))
                {
                    if (DateTime.TryParse(changedAtStr, out var changedAt) && changedAt < cutoffDate)
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
                    if (!bool.TryParse(isActiveStr, out var isActive) || !isActive)
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
            }

            Logger.LogInformation("Completed cleanup of old configuration data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during configuration data cleanup");
        }
    }

    /// <summary>
    /// Retrieves configuration history for a specific key.
    /// </summary>
    private async Task<List<ConfigurationHistoryEntry>> GetConfigurationHistoryAsync(string configKey, int limit = 10)
    {
        if (_persistentStorage == null) return new List<ConfigurationHistoryEntry>();

        var history = new List<ConfigurationHistoryEntry>();

        try
        {
            var historyKeys = await _persistentStorage.ListKeysAsync($"{HISTORY_PREFIX}{configKey}:");
            var sortedKeys = historyKeys.OrderByDescending(k => k).Take(limit);

            foreach (var key in sortedKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var entry = JsonSerializer.Deserialize<ConfigurationHistoryEntry>(data);
                    if (entry != null)
                    {
                        history.Add(entry);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving configuration history for {Key}", configKey);
        }

        return history;
    }
}

/// <summary>
/// Configuration index entry.
/// </summary>
internal class ConfigurationIndex
{
    public string Key { get; set; } = string.Empty;
    public DateTime IndexedAt { get; set; }
}

/// <summary>
/// Configuration history entry.
/// </summary>
internal class ConfigurationHistoryEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public ConfigurationValueType ValueType { get; set; }
    public int Version { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

/// <summary>
/// Configuration audit entry.
/// </summary>
internal class ConfigurationAuditEntry
{
    public string Action { get; set; } = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}
