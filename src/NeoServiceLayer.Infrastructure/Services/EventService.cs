using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;
using ITeeHostService = NeoServiceLayer.Core.Interfaces.ITeeHostService;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the event service.
    /// </summary>
    public class EventService : IEventService
    {
        private readonly ITeeHostService _teeHostService;
        private readonly ILogger<EventService> _logger;

        // In-memory storage for subscriptions and events (replace with database in production)
        private static readonly Dictionary<string, Subscription> _subscriptions = new();
        private static readonly Dictionary<string, Event> _events = new();

        /// <summary>
        /// Initializes a new instance of the EventService class.
        /// </summary>
        /// <param name="teeHostService">The TEE host service.</param>
        /// <param name="logger">The logger.</param>
        public EventService(ITeeHostService teeHostService, ILogger<EventService> logger)
        {
            _teeHostService = teeHostService ?? throw new ArgumentNullException(nameof(teeHostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            _logger.LogInformation("Creating subscription for event type {EventType}", subscription.EventType);

            if (string.IsNullOrEmpty(subscription.UserId))
            {
                throw new ArgumentException("User ID is required", nameof(subscription));
            }

            if (string.IsNullOrEmpty(subscription.CallbackUrl))
            {
                throw new ArgumentException("Callback URL is required", nameof(subscription));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Event, JsonSerializer.Serialize(new
                {
                    Action = "create_subscription",
                    Subscription = subscription
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);

                // Update subscription with response data
                subscription.Id = result["subscription_id"];

                // Store subscription
                _subscriptions[subscription.Id] = subscription;

                _logger.LogInformation("Subscription {SubscriptionId} created successfully", subscription.Id);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Subscription> GetSubscriptionAsync(string subscriptionId)
        {
            _logger.LogInformation("Getting subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentException("Subscription ID is required", nameof(subscriptionId));
            }

            try
            {
                if (_subscriptions.TryGetValue(subscriptionId, out var subscription))
                {
                    _logger.LogInformation("Subscription {SubscriptionId} retrieved successfully", subscriptionId);
                    return subscription;
                }

                _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync(string userId, EventType? eventType = null)
        {
            _logger.LogInformation("Getting subscriptions for user {UserId} with event type {EventType}", userId, eventType);

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID is required", nameof(userId));
            }

            try
            {
                // Filter subscriptions
                var query = _subscriptions.Values.Where(s => s.UserId == userId);

                if (eventType.HasValue)
                {
                    query = query.Where(s => s.EventType == eventType.Value);
                }

                var subscriptions = query.ToList();

                _logger.LogInformation("Retrieved {Count} subscriptions for user {UserId}", subscriptions.Count, userId);

                return subscriptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Subscription> UpdateSubscriptionAsync(Subscription subscription)
        {
            _logger.LogInformation("Updating subscription {SubscriptionId}", subscription.Id);

            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (string.IsNullOrEmpty(subscription.Id))
            {
                throw new ArgumentException("Subscription ID is required", nameof(subscription));
            }

            try
            {
                // Check if subscription exists
                if (!_subscriptions.ContainsKey(subscription.Id))
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscription.Id);
                    return null;
                }

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Event, JsonSerializer.Serialize(new
                {
                    Action = "update_subscription",
                    Subscription = subscription
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Update subscription
                _subscriptions[subscription.Id] = subscription;

                _logger.LogInformation("Subscription {SubscriptionId} updated successfully", subscription.Id);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscription.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteSubscriptionAsync(string subscriptionId)
        {
            _logger.LogInformation("Deleting subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentException("Subscription ID is required", nameof(subscriptionId));
            }

            try
            {
                // Check if subscription exists
                if (!_subscriptions.ContainsKey(subscriptionId))
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                    return false;
                }

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Event, JsonSerializer.Serialize(new
                {
                    Action = "delete_subscription",
                    SubscriptionId = subscriptionId
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Remove subscription
                _subscriptions.Remove(subscriptionId);

                _logger.LogInformation("Subscription {SubscriptionId} deleted successfully", subscriptionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Event> PublishEventAsync(Event @event)
        {
            _logger.LogInformation("Publishing event of type {EventType}", @event.Type);

            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Event, JsonSerializer.Serialize(new
                {
                    Action = "publish_event",
                    Event = @event
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);

                // Update event with response data
                @event.Id = result["event_id"];
                @event.ProcessedAt = DateTime.UtcNow;

                // Store event
                _events[@event.Id] = @event;

                // Process event asynchronously
                _ = ProcessEventAsync(@event);

                _logger.LogInformation("Event {EventId} published successfully", @event.Id);

                return @event;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Event> GetEventAsync(string eventId)
        {
            _logger.LogInformation("Getting event {EventId}", eventId);

            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("Event ID is required", nameof(eventId));
            }

            try
            {
                if (_events.TryGetValue(eventId, out var @event))
                {
                    _logger.LogInformation("Event {EventId} retrieved successfully", eventId);
                    return @event;
                }

                _logger.LogWarning("Event {EventId} not found", eventId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event {EventId}", eventId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<Event> Events, int TotalCount)> GetEventsAsync(string userId, EventType? eventType = null, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Getting events for user {UserId} with event type {EventType}, page {Page}, pageSize {PageSize}", userId, eventType, page, pageSize);

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID is required", nameof(userId));
            }

            if (page < 1)
            {
                throw new ArgumentException("Page must be greater than 0", nameof(page));
            }

            if (pageSize < 1)
            {
                throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));
            }

            try
            {
                // Filter events
                var query = _events.Values.Where(e => e.Source == userId);

                if (eventType.HasValue)
                {
                    query = query.Where(e => e.Type == eventType.Value);
                }

                // Get total count
                var totalCount = query.Count();

                // Apply pagination
                var events = query
                    .OrderByDescending(e => e.OccurredAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} events for user {UserId}", events.Count, userId);

                return (events, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task ProcessEventAsync(Event @event, string callbackUrl)
        {
            _logger.LogInformation("Processing event {EventId} to callback URL {CallbackUrl}", @event.Id, callbackUrl);

            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            if (string.IsNullOrEmpty(callbackUrl))
            {
                throw new ArgumentException("Callback URL is required", nameof(callbackUrl));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.Event, JsonSerializer.Serialize(new
                {
                    Action = "send_event_to_callback",
                    Event = @event,
                    CallbackUrl = callbackUrl
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                _logger.LogInformation("Event {EventId} sent to callback URL {CallbackUrl} successfully", @event.Id, callbackUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event {EventId} to callback URL {CallbackUrl}", @event.Id, callbackUrl);
                throw;
            }
        }

        private async System.Threading.Tasks.Task ProcessEventAsync(Event @event)
        {
            try
            {
                _logger.LogInformation("Processing event {EventId}", @event.Id);

                // Find matching subscriptions
                var matchingSubscriptions = _subscriptions.Values
                    .Where(s => s.EventType == @event.Type && s.Status == SubscriptionStatus.Active)
                    .ToList();

                _logger.LogInformation("Found {Count} matching subscriptions for event {EventId}", matchingSubscriptions.Count, @event.Id);

                // Process each matching subscription
                foreach (var subscription in matchingSubscriptions)
                {
                    try
                    {
                        // Check if the event matches the subscription filter
                        if (!EventMatchesFilter(@event, subscription.EventFilter))
                        {
                            continue;
                        }

                        // Create a message to send to the TEE
                        var message = TeeMessage.Create(TeeMessageType.Event, JsonSerializer.Serialize(new
                        {
                            Action = "process_event",
                            EventId = @event.Id,
                            SubscriptionId = subscription.Id
                        }));

                        // Send the message to the TEE
                        var response = await _teeHostService.SendMessageAsync(message);

                        _logger.LogInformation("Event {EventId} processed for subscription {SubscriptionId}", @event.Id, subscription.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event {EventId} for subscription {SubscriptionId}", @event.Id, subscription.Id);
                    }
                }

                _logger.LogInformation("Event {EventId} processing completed", @event.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventId}", @event.Id);
            }
        }

        private bool EventMatchesFilter(Event @event, Dictionary<string, object> filter)
        {
            if (filter == null || filter.Count == 0)
            {
                return true;
            }

            foreach (var kvp in filter)
            {
                if (!@event.Data.TryGetValue(kvp.Key, out var value) || !value.Equals(kvp.Value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
