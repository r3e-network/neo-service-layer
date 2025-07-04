using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.EventSubscription.Models;

namespace NeoServiceLayer.Services.EventSubscription;

public partial class EventSubscriptionService
{
    private IPersistentStorageProvider? _persistentStorage;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    // Storage key prefixes
    private const string SUBSCRIPTION_PREFIX = "subscription:";
    private const string FILTER_PREFIX = "filter:";
    private const string WEBHOOK_PREFIX = "webhook:";
    private const string HISTORY_PREFIX = "history:";
    private const string INDEX_PREFIX = "index:";
    private const string STATS_PREFIX = "stats:";
    private const string USER_INDEX_PREFIX = "user_index:";
    private const string EVENT_INDEX_PREFIX = "event_index:";

    /// <summary>
    /// Initializes persistent storage for the event subscription service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _persistentStorage = _serviceProvider?.GetService(typeof(IPersistentStorageProvider)) as IPersistentStorageProvider;

            if (_persistentStorage != null)
            {
                await _persistentStorage.InitializeAsync();
                Logger.LogInformation("Persistent storage initialized for EventSubscriptionService");

                // Restore subscriptions from storage
                await RestoreSubscriptionsFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistSubscriptionsAsync(),
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
                Logger.LogWarning("Persistent storage provider not available for EventSubscriptionService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize persistent storage for EventSubscriptionService");
        }
    }

    /// <summary>
    /// Persists a subscription to storage.
    /// </summary>
    private async Task PersistSubscriptionAsync(EventSubscription subscription)
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
                TimeToLive = TimeSpan.FromDays(365) // Keep subscriptions for 1 year
            });


            // Update event type index
            await UpdateEventTypeIndexAsync(subscription.EventType, subscription.SubscriptionId);

            Logger.LogDebug("Persisted subscription {SubscriptionId} to storage", subscription.SubscriptionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist subscription {SubscriptionId}", subscription.SubscriptionId);
        }
    }

    /// <summary>
    /// Persists a filter to storage.
    /// </summary>
    private async Task PersistFilterAsync(string subscriptionId, EventFilter filter)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{FILTER_PREFIX}{subscriptionId}:{filter.FilterId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(filter);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365)
            });

            Logger.LogDebug("Persisted filter {FilterId} for subscription {SubscriptionId}", filter.FilterId, subscriptionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist filter {FilterId}", filter.FilterId);
        }
    }

    /// <summary>
    /// Persists webhook configuration to storage.
    /// </summary>
    private async Task PersistWebhookAsync(string subscriptionId, WebhookConfig webhook)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{WEBHOOK_PREFIX}{subscriptionId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(webhook);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true, // Encrypt webhook configs as they contain sensitive URLs
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365)
            });

            Logger.LogDebug("Persisted webhook config for subscription {SubscriptionId}", subscriptionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist webhook config for subscription {SubscriptionId}", subscriptionId);
        }
    }

    /// <summary>
    /// Persists delivery history to storage.
    /// </summary>
    private async Task PersistDeliveryHistoryAsync(string subscriptionId, NotificationDelivery delivery)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{HISTORY_PREFIX}{subscriptionId}:{delivery.DeliveryId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(delivery);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30) // Keep history for 30 days
            });

            Logger.LogDebug("Persisted delivery history {DeliveryId} for subscription {SubscriptionId}",
                delivery.DeliveryId, subscriptionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist delivery history {DeliveryId}", delivery.DeliveryId);
        }
    }

    /// <summary>
    /// Updates user index in storage.
    /// </summary>
    private async Task UpdateUserIndexAsync(string userId, string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{USER_INDEX_PREFIX}{userId}";
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
            Logger.LogError(ex, "Failed to update user index for {UserId}", userId);
        }
    }

    /// <summary>
    /// Updates event type index in storage.
    /// </summary>
    private async Task UpdateEventTypeIndexAsync(string eventType, string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{EVENT_INDEX_PREFIX}{eventType}";
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
            Logger.LogError(ex, "Failed to update event type index for {EventType}", eventType);
        }
    }

    /// <summary>
    /// Restores subscriptions from persistent storage.
    /// </summary>
    private async Task RestoreSubscriptionsFromStorageAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            Logger.LogInformation("Restoring event subscriptions from persistent storage");

            var subscriptionKeys = await _persistentStorage.ListKeysAsync($"{SUBSCRIPTION_PREFIX}*");
            var restoredCount = 0;

            foreach (var key in subscriptionKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var subscription = JsonSerializer.Deserialize<EventSubscription>(data);
                        if (subscription != null)
                        {
                            _subscriptions[subscription.SubscriptionId] = subscription;

                            // Restore filters
                            await RestoreFiltersForSubscriptionAsync(subscription.SubscriptionId);

                            // Restore webhook config
                            await RestoreWebhookForSubscriptionAsync(subscription.SubscriptionId);

                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore subscription from key {Key}", key);
                }
            }

            // Restore indices
            await RestoreIndicesAsync();

            Logger.LogInformation("Restored {Count} event subscriptions from storage", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore event subscriptions from storage");
        }
    }

    /// <summary>
    /// Restores filters for a subscription.
    /// </summary>
    private async Task RestoreFiltersForSubscriptionAsync(string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var filterKeys = await _persistentStorage.ListKeysAsync($"{FILTER_PREFIX}{subscriptionId}:*");

            foreach (var key in filterKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);

                    if (data != null)
                    {
                        var filter = JsonSerializer.Deserialize<EventFilter>(data);
                        if (filter != null && _subscriptions.TryGetValue(subscriptionId, out var subscription))
                        {
                            subscription.Filters ??= new List<EventFilter>();
                            subscription.Filters.Add(filter);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore filter from key {Key}", key);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore filters for subscription {SubscriptionId}", subscriptionId);
        }
    }

    /// <summary>
    /// Restores webhook configuration for a subscription.
    /// </summary>
    private async Task RestoreWebhookForSubscriptionAsync(string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{WEBHOOK_PREFIX}{subscriptionId}";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                var webhook = JsonSerializer.Deserialize<WebhookConfig>(data);
                if (webhook != null && _subscriptions.TryGetValue(subscriptionId, out var subscription))
                {
                    subscription.WebhookConfig = webhook;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore webhook for subscription {SubscriptionId}", subscriptionId);
        }
    }

    /// <summary>
    /// Restores indices from storage.
    /// </summary>
    private async Task RestoreIndicesAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Restore user index
            var userIndexKeys = await _persistentStorage.ListKeysAsync($"{USER_INDEX_PREFIX}*");
            foreach (var key in userIndexKeys)
            {
                var userId = key.Replace(USER_INDEX_PREFIX, "");
                _userSubscriptionIndex[userId] = new HashSet<string>();

                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var subscriptionIds = JsonSerializer.Deserialize<HashSet<string>>(data);
                    if (subscriptionIds != null)
                    {
                        _userSubscriptionIndex[userId] = subscriptionIds;
                    }
                }
            }

            // Restore event type index
            var eventIndexKeys = await _persistentStorage.ListKeysAsync($"{EVENT_INDEX_PREFIX}*");
            foreach (var key in eventIndexKeys)
            {
                var eventType = key.Replace(EVENT_INDEX_PREFIX, "");
                _eventTypeIndex[eventType] = new HashSet<string>();

                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var subscriptionIds = JsonSerializer.Deserialize<HashSet<string>>(data);
                    if (subscriptionIds != null)
                    {
                        _eventTypeIndex[eventType] = subscriptionIds;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore indices from storage");
        }
    }

    /// <summary>
    /// Persists all current subscriptions to storage.
    /// </summary>
    private async Task PersistSubscriptionsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            foreach (var subscription in _subscriptions.Values)
            {
                await PersistSubscriptionAsync(subscription);
            }

            await PersistServiceStatisticsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist subscriptions");
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
            var stats = new PersistentEventSubscriptionStatistics
            {
                TotalSubscriptions = _subscriptions.Count,
                ActiveSubscriptions = _subscriptions.Values.Count(s => s.IsActive),
                TotalEventsProcessed = _totalEventsProcessed,
                TotalNotificationsSent = _totalNotificationsSent,
                TotalErrors = _totalErrors,
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
            Logger.LogInformation("Starting cleanup of expired event subscription data");

            // Clean up old delivery history (older than 30 days)
            var historyKeys = await _persistentStorage.ListKeysAsync($"{HISTORY_PREFIX}*");
            var cleanedCount = 0;

            foreach (var key in historyKeys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);
                    if (data != null)
                    {
                        var delivery = JsonSerializer.Deserialize<NotificationDelivery>(data);
                        if (delivery != null && delivery.Timestamp < DateTime.UtcNow.AddDays(-30))
                        {
                            await _persistentStorage.DeleteAsync(key);
                            cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to cleanup history key {Key}", key);
                }
            }

            Logger.LogInformation("Cleaned up {Count} expired delivery history entries", cleanedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data");
        }
    }

    /// <summary>
    /// Removes a subscription from persistent storage.
    /// </summary>
    private async Task RemoveSubscriptionFromStorageAsync(string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Remove subscription
            await _persistentStorage.DeleteAsync($"{SUBSCRIPTION_PREFIX}{subscriptionId}");

            // Remove filters
            var filterKeys = await _persistentStorage.ListKeysAsync($"{FILTER_PREFIX}{subscriptionId}:*");
            foreach (var key in filterKeys)
            {
                await _persistentStorage.DeleteAsync(key);
            }

            // Remove webhook config
            await _persistentStorage.DeleteAsync($"{WEBHOOK_PREFIX}{subscriptionId}");

            // Remove from indices
            if (_subscriptions.TryGetValue(subscriptionId, out var subscription))
            {
                await RemoveFromUserIndexAsync(subscription.UserId, subscriptionId);
                await RemoveFromEventTypeIndexAsync(subscription.EventType, subscriptionId);
            }

            Logger.LogDebug("Removed subscription {SubscriptionId} from storage", subscriptionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove subscription {SubscriptionId} from storage", subscriptionId);
        }
    }

    /// <summary>
    /// Removes subscription from user index.
    /// </summary>
    private async Task RemoveFromUserIndexAsync(string userId, string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{USER_INDEX_PREFIX}{userId}";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                var subscriptionIds = JsonSerializer.Deserialize<HashSet<string>>(data) ?? new HashSet<string>();
                subscriptionIds.Remove(subscriptionId);

                if (subscriptionIds.Count > 0)
                {
                    var updatedData = JsonSerializer.SerializeToUtf8Bytes(subscriptionIds);
                    await _persistentStorage.StoreAsync(key, updatedData, new StorageOptions());
                }
                else
                {
                    await _persistentStorage.DeleteAsync(key);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove subscription from user index");
        }
    }

    /// <summary>
    /// Removes subscription from event type index.
    /// </summary>
    private async Task RemoveFromEventTypeIndexAsync(string eventType, string subscriptionId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{EVENT_INDEX_PREFIX}{eventType}";
            var data = await _persistentStorage.RetrieveAsync(key);

            if (data != null)
            {
                var subscriptionIds = JsonSerializer.Deserialize<HashSet<string>>(data) ?? new HashSet<string>();
                subscriptionIds.Remove(subscriptionId);

                if (subscriptionIds.Count > 0)
                {
                    var updatedData = JsonSerializer.SerializeToUtf8Bytes(subscriptionIds);
                    await _persistentStorage.StoreAsync(key, updatedData, new StorageOptions());
                }
                else
                {
                    await _persistentStorage.DeleteAsync(key);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove subscription from event type index");
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
/// Statistics for event subscription service persistence.
/// </summary>
internal class PersistentEventSubscriptionStatistics
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public long TotalEventsProcessed { get; set; }
    public long TotalNotificationsSent { get; set; }
    public long TotalErrors { get; set; }
    public DateTime LastUpdated { get; set; }
}
