using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Text.Json;

namespace NeoServiceLayer.Services.EventSubscription;

/// <summary>
/// Event processing operations for the Event Subscription Service.
/// </summary>
public partial class EventSubscriptionService
{
    /// <inheritdoc/>
    public async Task<IEnumerable<EventData>> GetEventsAsync(string subscriptionId, int skip, int take, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscriptionId));
        }

        try
        {
            IncrementRequestCounters();

            // Check if the subscription exists
            if (!_subscriptionCache.ContainsKey(subscriptionId))
            {
                throw new ArgumentException($"Subscription with ID {subscriptionId} does not exist.");
            }

            // Get events from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getEvents('{subscriptionId}', {skip}, {take}, '{blockchainType}')");

            // Parse the result
            var events = JsonSerializer.Deserialize<List<EventData>>(result) ??
                throw new InvalidOperationException("Failed to deserialize events.");

            // Update the event cache
            lock (_eventCache)
            {
                if (!_eventCache.ContainsKey(subscriptionId))
                {
                    _eventCache[subscriptionId] = new List<EventData>();
                }

                // Add new events to cache (avoiding duplicates)
                foreach (var eventData in events)
                {
                    if (!_eventCache[subscriptionId].Any(e => e.EventId == eventData.EventId))
                    {
                        _eventCache[subscriptionId].Add(eventData);
                    }
                }

                // Keep only recent events (last 1000 events per subscription)
                if (_eventCache[subscriptionId].Count > 1000)
                {
                    _eventCache[subscriptionId] = _eventCache[subscriptionId]
                        .OrderByDescending(e => e.Timestamp)
                        .Take(1000)
                        .ToList();
                }
            }

            RecordSuccess();
            return events;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error getting events for subscription {SubscriptionId} on blockchain {BlockchainType}",
                subscriptionId, blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Processes a new event for all matching subscriptions.
    /// </summary>
    /// <param name="eventData">The event data to process.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Number of subscriptions that matched the event.</returns>
    public async Task<int> ProcessEventAsync(EventData eventData, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        try
        {
            IncrementRequestCounters();

            // Process event in the enclave
            string eventJson = JsonSerializer.Serialize(eventData);
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"processEvent({eventJson}, '{blockchainType}')");

            // Parse the result
            var matchedSubscriptions = JsonSerializer.Deserialize<List<string>>(result) ??
                throw new InvalidOperationException("Failed to deserialize matched subscriptions.");

            // Update event cache for matched subscriptions
            lock (_eventCache)
            {
                foreach (var subscriptionId in matchedSubscriptions)
                {
                    if (!_eventCache.ContainsKey(subscriptionId))
                    {
                        _eventCache[subscriptionId] = new List<EventData>();
                    }

                    _eventCache[subscriptionId].Add(eventData);

                    // Keep only recent events
                    if (_eventCache[subscriptionId].Count > 1000)
                    {
                        _eventCache[subscriptionId] = _eventCache[subscriptionId]
                            .OrderByDescending(e => e.Timestamp)
                            .Take(1000)
                            .ToList();
                    }
                }
            }

            RecordSuccess();
            Logger.LogDebug("Processed event {EventId} for {MatchCount} subscriptions",
                eventData.EventId, matchedSubscriptions.Count);

            return matchedSubscriptions.Count;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error processing event {EventId} for blockchain {BlockchainType}",
                eventData.EventId, blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Gets events by type for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="eventType">The event type to filter by.</param>
    /// <param name="skip">Number of events to skip.</param>
    /// <param name="take">Number of events to take.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Filtered events.</returns>
    public async Task<IEnumerable<EventData>> GetEventsByTypeAsync(string subscriptionId, string eventType, int skip, int take, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscriptionId));
        }

        if (string.IsNullOrEmpty(eventType))
        {
            throw new ArgumentException("Event type cannot be null or empty.", nameof(eventType));
        }

        try
        {
            IncrementRequestCounters();

            // Get filtered events from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getEventsByType('{subscriptionId}', '{eventType}', {skip}, {take}, '{blockchainType}')");

            // Parse the result
            var events = JsonSerializer.Deserialize<List<EventData>>(result) ??
                throw new InvalidOperationException("Failed to deserialize filtered events.");

            RecordSuccess();
            return events;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error getting events by type {EventType} for subscription {SubscriptionId}",
                eventType, subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Gets events within a time range for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Events within the time range.</returns>
    public async Task<IEnumerable<EventData>> GetEventsInTimeRangeAsync(string subscriptionId, DateTime startTime, DateTime endTime, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscriptionId));
        }

        if (startTime >= endTime)
        {
            throw new ArgumentException("Start time must be before end time.");
        }

        try
        {
            IncrementRequestCounters();

            // Get events in time range from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getEventsInTimeRange('{subscriptionId}', '{startTime:O}', '{endTime:O}', '{blockchainType}')");

            // Parse the result
            var events = JsonSerializer.Deserialize<List<EventData>>(result) ??
                throw new InvalidOperationException("Failed to deserialize time-filtered events.");

            RecordSuccess();
            return events;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error getting events in time range for subscription {SubscriptionId}",
                subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Clears old events from the cache and storage.
    /// </summary>
    /// <param name="olderThan">Remove events older than this time.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Number of events cleaned up.</returns>
    public async Task<int> CleanupOldEventsAsync(DateTime olderThan, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        try
        {
            IncrementRequestCounters();

            // Cleanup old events in the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"cleanupOldEvents('{olderThan:O}', '{blockchainType}')");

            // Parse the result
            var cleanedCount = JsonSerializer.Deserialize<int>(result);

            // Cleanup event cache
            var cacheCleanedCount = 0;
            lock (_eventCache)
            {
                foreach (var subscriptionId in _eventCache.Keys.ToList())
                {
                    var originalCount = _eventCache[subscriptionId].Count;
                    _eventCache[subscriptionId] = _eventCache[subscriptionId]
                        .Where(e => e.Timestamp >= olderThan)
                        .ToList();
                    cacheCleanedCount += originalCount - _eventCache[subscriptionId].Count;
                }
            }

            RecordSuccess();
            Logger.LogInformation("Cleaned up {CleanedCount} old events from storage and {CacheCleanedCount} from cache",
                cleanedCount, cacheCleanedCount);

            return cleanedCount;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error cleaning up old events for blockchain {BlockchainType}",
                blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Gets event statistics for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Event statistics.</returns>
    public async Task<EventStatistics> GetEventStatisticsAsync(string subscriptionId, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscriptionId));
        }

        try
        {
            IncrementRequestCounters();

            // Get event statistics from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getEventStatistics('{subscriptionId}', '{blockchainType}')");

            // Parse the result
            var statistics = JsonSerializer.Deserialize<EventStatistics>(result) ??
                throw new InvalidOperationException("Failed to deserialize event statistics.");

            RecordSuccess();
            return statistics;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error getting event statistics for subscription {SubscriptionId}",
                subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Gets cached events for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>Cached events or empty list if not found.</returns>
    public List<EventData> GetCachedEvents(string subscriptionId)
    {
        if (string.IsNullOrEmpty(subscriptionId))
        {
            return new List<EventData>();
        }

        lock (_eventCache)
        {
            if (_eventCache.TryGetValue(subscriptionId, out var events))
            {
                return new List<EventData>(events);
            }
            return new List<EventData>();
        }
    }

    /// <summary>
    /// Clears event cache for a specific subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    public void ClearEventCache(string subscriptionId)
    {
        if (string.IsNullOrEmpty(subscriptionId))
        {
            return;
        }

        lock (_eventCache)
        {
            _eventCache.Remove(subscriptionId);
        }

        Logger.LogDebug("Cleared event cache for subscription {SubscriptionId}", subscriptionId);
    }
}
