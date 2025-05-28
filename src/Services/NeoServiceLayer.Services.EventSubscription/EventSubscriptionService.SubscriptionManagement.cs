using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Text.Json;

namespace NeoServiceLayer.Services.EventSubscription;

/// <summary>
/// Subscription management operations for the Event Subscription Service.
/// </summary>
public partial class EventSubscriptionService
{
    /// <inheritdoc/>
    public async Task<string> CreateSubscriptionAsync(EventSubscription subscription, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (subscription == null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        try
        {
            IncrementRequestCounters();

            // Generate a subscription ID if not provided
            if (string.IsNullOrEmpty(subscription.SubscriptionId))
            {
                subscription.SubscriptionId = Guid.NewGuid().ToString();
            }
            else if (_subscriptionCache.ContainsKey(subscription.SubscriptionId))
            {
                throw new ArgumentException($"Subscription with ID {subscription.SubscriptionId} already exists.");
            }

            // Set creation and modification dates
            subscription.CreatedAt = DateTime.UtcNow;
            subscription.LastModifiedAt = DateTime.UtcNow;

            // Create subscription in the enclave
            string subscriptionJson = JsonSerializer.Serialize(subscription);
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"createSubscription({subscriptionJson}, '{blockchainType}')");

            // Parse the result
            var subscriptionId = JsonSerializer.Deserialize<string>(result) ??
                throw new InvalidOperationException("Failed to deserialize subscription ID.");

            // Update the cache
            lock (_subscriptionCache)
            {
                _subscriptionCache[subscriptionId] = subscription;
            }

            // Initialize event cache for this subscription
            lock (_eventCache)
            {
                _eventCache[subscriptionId] = new List<EventData>();
            }

            RecordSuccess();
            return subscriptionId;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error creating subscription for blockchain {BlockchainType}",
                blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<EventSubscription> GetSubscriptionAsync(string subscriptionId, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscriptionId));
        }

        try
        {
            IncrementRequestCounters();

            // Check the cache first
            lock (_subscriptionCache)
            {
                if (_subscriptionCache.TryGetValue(subscriptionId, out var cachedSubscription))
                {
                    RecordSuccess();
                    return cachedSubscription;
                }
            }

            // Get subscription from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getSubscription('{subscriptionId}', '{blockchainType}')");

            // Parse the result
            var subscription = JsonSerializer.Deserialize<EventSubscription>(result) ??
                throw new InvalidOperationException("Failed to deserialize subscription.");

            // Update the cache
            lock (_subscriptionCache)
            {
                _subscriptionCache[subscriptionId] = subscription;
            }

            RecordSuccess();
            return subscription;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error getting subscription {SubscriptionId} for blockchain {BlockchainType}",
                subscriptionId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateSubscriptionAsync(EventSubscription subscription, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (subscription == null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        if (string.IsNullOrEmpty(subscription.SubscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscription));
        }

        try
        {
            IncrementRequestCounters();

            // Check if the subscription exists
            if (!_subscriptionCache.TryGetValue(subscription.SubscriptionId, out var existingSubscription))
            {
                throw new ArgumentException($"Subscription with ID {subscription.SubscriptionId} does not exist.");
            }

            // Preserve creation date
            subscription.CreatedAt = existingSubscription.CreatedAt;
            subscription.LastModifiedAt = DateTime.UtcNow;

            // Update subscription in the enclave
            string subscriptionJson = JsonSerializer.Serialize(subscription);
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"updateSubscription({subscriptionJson}, '{blockchainType}')");
            bool success = JsonSerializer.Deserialize<bool>(result);

            if (success)
            {
                // Update the cache
                lock (_subscriptionCache)
                {
                    _subscriptionCache[subscription.SubscriptionId] = subscription;
                }
            }

            RecordSuccess();
            return success;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error updating subscription {SubscriptionId} for blockchain {BlockchainType}",
                subscription.SubscriptionId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSubscriptionAsync(string subscriptionId, BlockchainType blockchainType)
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

            // Delete subscription from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"deleteSubscription('{subscriptionId}', '{blockchainType}')");
            bool success = JsonSerializer.Deserialize<bool>(result);

            if (success)
            {
                // Remove from caches
                lock (_subscriptionCache)
                {
                    _subscriptionCache.Remove(subscriptionId);
                }

                lock (_eventCache)
                {
                    _eventCache.Remove(subscriptionId);
                }
            }

            RecordSuccess();
            return success;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error deleting subscription {SubscriptionId} for blockchain {BlockchainType}",
                subscriptionId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventSubscription>> ListSubscriptionsAsync(int skip, int take, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        try
        {
            IncrementRequestCounters();

            // List subscriptions from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"listSubscriptions({skip}, {take}, '{blockchainType}')");

            // Parse the result
            var subscriptions = JsonSerializer.Deserialize<List<EventSubscription>>(result) ??
                throw new InvalidOperationException("Failed to deserialize subscriptions.");

            // Update the cache
            lock (_subscriptionCache)
            {
                foreach (var subscription in subscriptions)
                {
                    _subscriptionCache[subscription.SubscriptionId] = subscription;
                }
            }

            RecordSuccess();
            return subscriptions;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error listing subscriptions for blockchain {BlockchainType}",
                blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Gets all subscriptions for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>List of user subscriptions.</returns>
    public async Task<IEnumerable<EventSubscription>> GetUserSubscriptionsAsync(string userId, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        try
        {
            IncrementRequestCounters();

            // Get user subscriptions from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getUserSubscriptions('{userId}', '{blockchainType}')");

            // Parse the result
            var subscriptions = JsonSerializer.Deserialize<List<EventSubscription>>(result) ??
                throw new InvalidOperationException("Failed to deserialize user subscriptions.");

            RecordSuccess();
            return subscriptions;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error getting subscriptions for user {UserId} on blockchain {BlockchainType}",
                userId, blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Activates a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if activated successfully.</returns>
    public async Task<bool> ActivateSubscriptionAsync(string subscriptionId, BlockchainType blockchainType)
    {
        return await SetSubscriptionStatusAsync(subscriptionId, true, blockchainType);
    }

    /// <summary>
    /// Deactivates a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if deactivated successfully.</returns>
    public async Task<bool> DeactivateSubscriptionAsync(string subscriptionId, BlockchainType blockchainType)
    {
        return await SetSubscriptionStatusAsync(subscriptionId, false, blockchainType);
    }

    /// <summary>
    /// Sets the subscription status.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="isActive">Whether the subscription should be active.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if status was set successfully.</returns>
    private async Task<bool> SetSubscriptionStatusAsync(string subscriptionId, bool isActive, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscriptionId));
        }

        try
        {
            IncrementRequestCounters();

            // Get the subscription
            var subscription = await GetSubscriptionAsync(subscriptionId, blockchainType);
            subscription.Enabled = isActive;
            subscription.LastModifiedAt = DateTime.UtcNow;

            // Update the subscription
            bool success = await UpdateSubscriptionAsync(subscription, blockchainType);

            if (success)
            {
                Logger.LogInformation("Subscription {SubscriptionId} {Status} successfully",
                    subscriptionId, isActive ? "activated" : "deactivated");
            }

            return success;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error setting subscription {SubscriptionId} status to {IsActive}",
                subscriptionId, isActive);
            throw;
        }
    }
}
