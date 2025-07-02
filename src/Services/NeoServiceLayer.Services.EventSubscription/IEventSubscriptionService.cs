using NeoServiceLayer.Core;
using NeoServiceLayer.Services.EventSubscription.Models;

namespace NeoServiceLayer.Services.EventSubscription;

/// <summary>
/// Interface for the Event Subscription service.
/// </summary>
public interface IEventSubscriptionService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Creates a subscription.
    /// </summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscription ID.</returns>
    Task<string> CreateSubscriptionAsync(EventSubscription subscription, BlockchainType blockchainType);

    /// <summary>
    /// Gets a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscription.</returns>
    Task<EventSubscription> GetSubscriptionAsync(string subscriptionId, BlockchainType blockchainType);

    /// <summary>
    /// Updates a subscription.
    /// </summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the subscription was updated successfully, false otherwise.</returns>
    Task<bool> UpdateSubscriptionAsync(EventSubscription subscription, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the subscription was deleted successfully, false otherwise.</returns>
    Task<bool> DeleteSubscriptionAsync(string subscriptionId, BlockchainType blockchainType);

    /// <summary>
    /// Lists subscriptions.
    /// </summary>
    /// <param name="skip">The number of subscriptions to skip.</param>
    /// <param name="take">The number of subscriptions to take.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of subscriptions.</returns>
    Task<IEnumerable<EventSubscription>> ListSubscriptionsAsync(int skip, int take, BlockchainType blockchainType);

    /// <summary>
    /// Lists events for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="skip">The number of events to skip.</param>
    /// <param name="take">The number of events to take.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of events.</returns>
    Task<IEnumerable<EventData>> ListEventsAsync(string subscriptionId, int skip, int take, BlockchainType blockchainType);

    /// <summary>
    /// Acknowledges an event.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="eventId">The event ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the event was acknowledged successfully, false otherwise.</returns>
    Task<bool> AcknowledgeEventAsync(string subscriptionId, string eventId, BlockchainType blockchainType);

    /// <summary>
    /// Triggers a test event.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="eventData">The event data.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The event ID.</returns>
    Task<string> TriggerTestEventAsync(string subscriptionId, EventData eventData, BlockchainType blockchainType);
}

/// <summary>
/// Event subscription.
/// </summary>
public class EventSubscription
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event filter.
    /// </summary>
    public string EventFilter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback authentication header.
    /// </summary>
    public string CallbackAuthHeader { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the subscription is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the last triggered date.
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Gets or sets the retry policy.
    /// </summary>
    public RetryPolicy RetryPolicy { get; set; } = new();

    /// <summary>
    /// Gets or sets the custom metadata.
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the event filters.
    /// </summary>
    public List<EventFilter> Filters { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the webhook configuration.
    /// </summary>
    public WebhookConfig? WebhookConfig { get; set; }
    
    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the user ID who owns this subscription.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Retry policy.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial retry delay in seconds.
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the retry backoff factor.
    /// </summary>
    public double RetryBackoffFactor { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the maximum retry delay in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 60;
}

/// <summary>
/// Event data.
/// </summary>
public class EventData
{
    /// <summary>
    /// Gets or sets the event ID.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the event has been acknowledged.
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Gets or sets the acknowledgement timestamp.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery attempts.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the last delivery attempt timestamp.
    /// </summary>
    public DateTime? LastDeliveryAttemptAt { get; set; }

    /// <summary>
    /// Gets or sets the next delivery attempt timestamp.
    /// </summary>
    public DateTime? NextDeliveryAttemptAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public string DeliveryStatus { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the last delivery status code.
    /// </summary>
    public int? LastDeliveryStatusCode { get; set; }

    /// <summary>
    /// Gets or sets the last delivery error message.
    /// </summary>
    public string? LastDeliveryErrorMessage { get; set; }
}
