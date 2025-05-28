using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Configuration.Models;

namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Subscription and notification operations for the Configuration Service.
/// </summary>
public partial class ConfigurationService
{
    public async Task<ConfigurationSubscriptionResult> SubscribeToChangesAsync(SubscribeToChangesRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var subscriptionId = Guid.NewGuid().ToString();

        try
        {
            Logger.LogInformation("Creating configuration subscription {SubscriptionId} on {Blockchain}", subscriptionId, blockchainType);

            await Task.Delay(1); // Simulate async subscription creation
            var subscription = new Models.ConfigurationSubscription
            {
                SubscriptionId = subscriptionId,
                ConfigurationKey = request.KeyPattern,
                SubscriberEndpoint = request.CallbackUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            lock (_configLock)
            {
                _subscriptions[subscriptionId] = subscription;
            }

            return new ConfigurationSubscriptionResult
            {
                SubscriptionId = subscriptionId,
                Success = true,
                CreatedAt = subscription.CreatedAt
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create configuration subscription {SubscriptionId}", subscriptionId);
            return new ConfigurationSubscriptionResult
            {
                SubscriptionId = subscriptionId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Unsubscribes from configuration changes.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID to remove.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Unsubscription result.</returns>
    public async Task<ConfigurationUnsubscribeResult> UnsubscribeFromChangesAsync(string subscriptionId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogInformation("Removing configuration subscription {SubscriptionId} on {Blockchain}", subscriptionId, blockchainType);

            await Task.Delay(1); // Simulate async subscription removal
            bool removed;
            lock (_configLock)
            {
                removed = _subscriptions.Remove(subscriptionId);
            }

            if (removed)
            {
                return new ConfigurationUnsubscribeResult
                {
                    SubscriptionId = subscriptionId,
                    Success = true,
                    RemovedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new ConfigurationUnsubscribeResult
                {
                    SubscriptionId = subscriptionId,
                    Success = false,
                    ErrorMessage = "Subscription not found"
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove configuration subscription {SubscriptionId}", subscriptionId);
            return new ConfigurationUnsubscribeResult
            {
                SubscriptionId = subscriptionId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets all active subscriptions.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>List of active subscriptions.</returns>
    public async Task<ConfigurationSubscriptionListResult> GetSubscriptionsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            await Task.Delay(1); // Simulate async subscription retrieval
            List<Models.ConfigurationSubscription> activeSubscriptions;

            lock (_configLock)
            {
                activeSubscriptions = _subscriptions.Values.Where(s => s.IsActive).ToList();
            }

            return new ConfigurationSubscriptionListResult
            {
                Subscriptions = activeSubscriptions.ToArray(),
                TotalCount = activeSubscriptions.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get configuration subscriptions");
            return new ConfigurationSubscriptionListResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Notifies subscribers of configuration changes.
    /// </summary>
    /// <param name="key">The configuration key that changed.</param>
    /// <param name="entry">The configuration entry.</param>
    private async Task NotifySubscribersAsync(string key, ConfigurationEntry entry)
    {
        try
        {
            List<Models.ConfigurationSubscription> matchingSubscriptions;

            lock (_configLock)
            {
                matchingSubscriptions = _subscriptions.Values
                    .Where(s => s.IsActive && MatchesPattern(key, s.ConfigurationKey))
                    .ToList();
            }

            foreach (var subscription in matchingSubscriptions)
            {
                try
                {
                    await SendNotificationAsync(subscription, key, entry, "Updated");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to notify subscription {SubscriptionId} about configuration change {Key}",
                        subscription.SubscriptionId, key);
                }
            }

            if (matchingSubscriptions.Count > 0)
            {
                Logger.LogDebug("Notified {SubscriptionCount} subscribers about configuration change {Key}",
                    matchingSubscriptions.Count, key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to notify subscribers about configuration change {Key}", key);
        }
    }

    /// <summary>
    /// Notifies subscribers of configuration deletion.
    /// </summary>
    /// <param name="key">The configuration key that was deleted.</param>
    private async Task NotifySubscribersOfDeletionAsync(string key)
    {
        try
        {
            List<Models.ConfigurationSubscription> matchingSubscriptions;

            lock (_configLock)
            {
                matchingSubscriptions = _subscriptions.Values
                    .Where(s => s.IsActive && MatchesPattern(key, s.ConfigurationKey))
                    .ToList();
            }

            foreach (var subscription in matchingSubscriptions)
            {
                try
                {
                    await SendNotificationAsync(subscription, key, null, "Deleted");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to notify subscription {SubscriptionId} about configuration deletion {Key}",
                        subscription.SubscriptionId, key);
                }
            }

            if (matchingSubscriptions.Count > 0)
            {
                Logger.LogDebug("Notified {SubscriptionCount} subscribers about configuration deletion {Key}",
                    matchingSubscriptions.Count, key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to notify subscribers about configuration deletion {Key}", key);
        }
    }

    /// <summary>
    /// Sends notification to a specific subscription.
    /// </summary>
    /// <param name="subscription">The subscription to notify.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="entry">The configuration entry (null for deletions).</param>
    /// <param name="changeType">The type of change.</param>
    private async Task SendNotificationAsync(Models.ConfigurationSubscription subscription, string key, ConfigurationEntry? entry, string changeType)
    {
        try
        {
            var notification = new
            {
                SubscriptionId = subscription.SubscriptionId,
                Key = key,
                ChangeType = changeType,
                Timestamp = DateTime.UtcNow,
                Value = entry?.Value,
                Version = entry?.Version
            };

            // In production, this would send HTTP POST to the callback URL
            await Task.Delay(10); // Simulate network call

            Logger.LogDebug("Sent notification to subscription {SubscriptionId} for configuration {Key}",
                subscription.SubscriptionId, key);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send notification to subscription {SubscriptionId}",
                subscription.SubscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Checks if a key matches a subscription pattern.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="pattern">The subscription pattern.</param>
    /// <returns>True if the key matches the pattern.</returns>
    private bool MatchesPattern(string key, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern == "*")
            return true;

        // Simple pattern matching - in production, this could use regex or glob patterns
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return key.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets subscription statistics.
    /// </summary>
    /// <returns>Subscription statistics.</returns>
    public SubscriptionStatistics GetSubscriptionStatistics()
    {
        lock (_configLock)
        {
            var totalSubscriptions = _subscriptions.Count;
            var activeSubscriptions = _subscriptions.Values.Count(s => s.IsActive);

            return new SubscriptionStatistics
            {
                TotalSubscriptions = totalSubscriptions,
                ActiveSubscriptions = activeSubscriptions,
                InactiveSubscriptions = totalSubscriptions - activeSubscriptions,
                OldestSubscription = _subscriptions.Values.Any()
                    ? _subscriptions.Values.Min(s => s.CreatedAt)
                    : (DateTime?)null,
                NewestSubscription = _subscriptions.Values.Any()
                    ? _subscriptions.Values.Max(s => s.CreatedAt)
                    : (DateTime?)null
            };
        }
    }
}
