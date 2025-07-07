using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Persistent storage extensions for NotificationService.
/// </summary>
public partial class NotificationService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private const string SUBSCRIPTION_PREFIX = "notification:subscription:";
    private const string TEMPLATE_PREFIX = "notification:template:";
    private const string HISTORY_PREFIX = "notification:history:";
    private const string CHANNEL_PREFIX = "notification:channel:";
    private const string METRICS_KEY = "notification:metrics";

    /// <summary>
    /// Loads persistent data from storage on service initialization.
    /// </summary>
    private async Task LoadPersistentDataAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("Persistent storage not available, using in-memory storage only");
            return;
        }

        try
        {
            Logger.LogInformation("Loading persistent notification data...");

            // Load subscriptions
            var subscriptionKeys = await _persistentStorage.ListKeysAsync(SUBSCRIPTION_PREFIX);
            foreach (var key in subscriptionKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var subscription = JsonSerializer.Deserialize<NotificationSubscription>(data);
                    if (subscription != null)
                    {
                        _subscriptions[subscription.Id] = subscription;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} subscriptions from persistent storage", _subscriptions.Count);

            // Load templates
            var templateKeys = await _persistentStorage.ListKeysAsync(TEMPLATE_PREFIX);
            foreach (var key in templateKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var template = JsonSerializer.Deserialize<InternalNotificationTemplate>(data);
                    if (template != null)
                    {
                        _templates[template.TemplateId] = template;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} templates from persistent storage", _templates.Count);

            // Load registered channels
            var channelKeys = await _persistentStorage.ListKeysAsync(CHANNEL_PREFIX);
            foreach (var key in channelKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var channel = JsonSerializer.Deserialize<ChannelInfo>(data);
                    if (channel != null)
                    {
                        _registeredChannels[channel.ChannelName] = channel;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} channels from persistent storage", _registeredChannels.Count);

            // Load metrics
            var metricsData = await _persistentStorage.RetrieveAsync(METRICS_KEY);
            if (metricsData != null)
            {
                var metrics = JsonSerializer.Deserialize<NotificationMetrics>(metricsData);
                if (metrics != null)
                {
                    _totalNotificationsSent = metrics.TotalSent;
                    _totalNotificationsFailed = metrics.TotalFailed;
                    _lastProcessingTime = metrics.LastProcessingTime;
                }
                Logger.LogInformation("Loaded metrics: {Sent} sent, {Failed} failed", _totalNotificationsSent, _totalNotificationsFailed);
            }

            // Load recent history (last 1000 entries)
            var historyKeys = (await _persistentStorage.ListKeysAsync(HISTORY_PREFIX, 1000)).ToList();
            foreach (var key in historyKeys.OrderByDescending(k => k))
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var result = JsonSerializer.Deserialize<NotificationResult>(data);
                    if (result != null)
                    {
                        _notificationHistory[result.NotificationId] = result;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} history entries from persistent storage", _notificationHistory.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent notification data");
            // Continue with in-memory data only
        }
    }

    /// <summary>
    /// Persists a subscription to storage.
    /// </summary>
    private async Task PersistSubscriptionAsync(NotificationSubscription subscription)
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
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Subscription",
                    ["UserId"] = subscription.SubscriberId,
                    ["EventType"] = subscription.EventType,
                    ["CreatedAt"] = subscription.CreatedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting subscription {Id}", subscription.Id);
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
            Logger.LogError(ex, "Error removing persisted subscription {Id}", subscriptionId);
        }
    }

    /// <summary>
    /// Persists a template to storage.
    /// </summary>
    private async Task PersistTemplateAsync(InternalNotificationTemplate template)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{TEMPLATE_PREFIX}{template.TemplateId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(template);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Template",
                    ["Name"] = template.TemplateName,
                    ["CreatedAt"] = template.CreatedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting template {Id}", template.TemplateId);
        }
    }

    /// <summary>
    /// Persists notification history entry to storage.
    /// </summary>
    private async Task PersistHistoryAsync(NotificationResult result)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Use timestamp in key for chronological ordering
            var key = $"{HISTORY_PREFIX}{result.SentAt.Ticks}:{result.NotificationId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(result);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false, // History doesn't need encryption
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30), // Keep history for 30 days
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "History",
                    ["Channel"] = result.Channel.ToString(),
                    ["Success"] = result.Success.ToString()
                }
            });

            // Clean up old in-memory history if too large
            if (_notificationHistory.Count > 1000)
            {
                var oldestKeys = _notificationHistory
                    .OrderBy(kvp => kvp.Value.SentAt)
                    .Take(100)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var oldKey in oldestKeys)
                {
                    _notificationHistory.TryRemove(oldKey, out _);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting notification history {Id}", result.NotificationId);
        }
    }

    /// <summary>
    /// Persists metrics to storage.
    /// </summary>
    private async Task PersistMetricsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var metrics = new NotificationMetrics
            {
                TotalSent = _totalNotificationsSent,
                TotalFailed = _totalNotificationsFailed,
                LastProcessingTime = _lastProcessingTime,
                UpdatedAt = DateTime.UtcNow
            };

            var data = JsonSerializer.SerializeToUtf8Bytes(metrics);

            await _persistentStorage.StoreAsync(METRICS_KEY, data, new StorageOptions
            {
                Encrypt = false,
                Compress = false,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Metrics",
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting notification metrics");
        }
    }

    /// <summary>
    /// Persists channel information to storage.
    /// </summary>
    private async Task PersistChannelAsync(ChannelInfo channel)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{CHANNEL_PREFIX}{channel.ChannelName}";
            var data = JsonSerializer.SerializeToUtf8Bytes(channel);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Channel",
                    ["ChannelType"] = channel.ChannelType.ToString(),
                    ["Enabled"] = channel.IsEnabled.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting channel {Name}", channel.ChannelName);
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
            // Clean up old history entries (older than 30 days)
            var historyKeys = await _persistentStorage.ListKeysAsync(HISTORY_PREFIX);
            var cutoffTicks = DateTime.UtcNow.AddDays(-30).Ticks;

            foreach (var key in historyKeys)
            {
                // Extract timestamp from key
                var parts = key.Split(':');
                if (parts.Length >= 2 && long.TryParse(parts[1], out var ticks))
                {
                    if (ticks < cutoffTicks)
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
            }

            Logger.LogInformation("Completed cleanup of old notification data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during notification data cleanup");
        }
    }
}

/// <summary>
/// Notification metrics for persistence.
/// </summary>
internal class NotificationMetrics
{
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public DateTime LastProcessingTime { get; set; }
    public DateTime UpdatedAt { get; set; }
}
