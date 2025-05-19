using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockEventService : IEventService
    {
        private readonly ILogger<MockEventService> _logger;
        private readonly List<Event> _events = new List<Event>();
        private readonly List<Subscription> _subscriptions = new List<Subscription>();

        public MockEventService(ILogger<MockEventService> logger)
        {
            _logger = logger;

            // Add some sample events
            _events.Add(new Event
            {
                Id = Guid.NewGuid().ToString(),
                Type = EventType.BlockchainEvent,
                Source = "test",
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new object[] { "address1", "address2", 100 } }
                },
                OccurredAt = DateTime.UtcNow.AddMinutes(-10)
            });

            _events.Add(new Event
            {
                Id = Guid.NewGuid().ToString(),
                Type = EventType.TaskEvent,
                Source = "test",
                Data = new Dictionary<string, object>
                {
                    { "dataId", "data123" },
                    { "operation", "encrypt" },
                    { "result", "success" }
                },
                OccurredAt = DateTime.UtcNow.AddMinutes(-5)
            });
        }

        public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
        {
            _logger.LogInformation("Creating subscription for event type {EventType}", subscription.EventType);

            subscription.Id = Guid.NewGuid().ToString();
            subscription.CreatedAt = DateTime.UtcNow;

            _subscriptions.Add(subscription);

            return subscription;
        }

        public async Task<Subscription> GetSubscriptionAsync(string subscriptionId)
        {
            _logger.LogInformation("Getting subscription {SubscriptionId}", subscriptionId);

            var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            return subscription;
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync(string userId, EventType? eventType = null)
        {
            _logger.LogInformation("Getting subscriptions for user {UserId} with type {EventType}", userId, eventType);

            var query = _subscriptions.Where(s => s.UserId == userId);

            if (eventType.HasValue)
            {
                query = query.Where(s => s.EventType == eventType.Value);
            }

            return query.AsEnumerable();
        }

        public async Task<Subscription> UpdateSubscriptionAsync(Subscription subscription)
        {
            _logger.LogInformation("Updating subscription {SubscriptionId}", subscription.Id);

            var existingSubscription = _subscriptions.FirstOrDefault(s => s.Id == subscription.Id);
            if (existingSubscription != null)
            {
                var index = _subscriptions.IndexOf(existingSubscription);
                _subscriptions[index] = subscription;
                return subscription;
            }

            return null;
        }

        public async Task<bool> DeleteSubscriptionAsync(string subscriptionId)
        {
            _logger.LogInformation("Deleting subscription {SubscriptionId}", subscriptionId);

            var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription != null)
            {
                _subscriptions.Remove(subscription);
                return true;
            }

            return false;
        }

        public async Task<Event> PublishEventAsync(Event @event)
        {
            _logger.LogInformation("Publishing event of type {EventType}", @event.Type);

            @event.Id = Guid.NewGuid().ToString();
            if (@event.OccurredAt == default)
            {
                @event.OccurredAt = DateTime.UtcNow;
            }

            _events.Add(@event);

            // Process subscriptions
            var matchingSubscriptions = _subscriptions
                .Where(s => s.EventType == @event.Type && s.Status == SubscriptionStatus.Active)
                .ToList();

            foreach (var subscription in matchingSubscriptions)
            {
                _logger.LogInformation("Processing event for subscription {SubscriptionId}", subscription.Id);

                // Send the event to the callback URL
                using (var httpClient = new HttpClient())
                {
                    // Add authentication if required
                    if (!string.IsNullOrEmpty(subscription.ApiKey))
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {subscription.ApiKey}");
                    }

                    // Prepare the payload
                    var payload = new
                    {
                        subscription_id = subscription.Id,
                        event_type = eventType,
                        event_data = eventData,
                        timestamp = DateTime.UtcNow
                    };

                    // Send the webhook
                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    try
                    {
                        var response = await httpClient.PostAsync(subscription.CallbackUrl, content);

                        // Log the result
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Successfully delivered event to {CallbackUrl}", subscription.CallbackUrl);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deliver event to {CallbackUrl}: {StatusCode}",
                                subscription.CallbackUrl, response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error delivering event to {CallbackUrl}", subscription.CallbackUrl);
                    }
                }
            }

            return @event;
        }

        public async Task<Event> GetEventAsync(string eventId)
        {
            _logger.LogInformation("Getting event {EventId}", eventId);

            var @event = _events.FirstOrDefault(e => e.Id == eventId);
            return @event;
        }

        public async Task<(IEnumerable<Event> Events, int TotalCount)> GetEventsAsync(string userId, EventType? eventType = null, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Getting events for user {UserId} with type {EventType}, page {Page}, pageSize {PageSize}", userId, eventType, page, pageSize);

            var query = _events.Where(e => e.Source == userId);

            if (eventType.HasValue)
            {
                query = query.Where(e => e.Type == eventType.Value);
            }

            var totalCount = query.Count();
            var events = query
                .OrderByDescending(e => e.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (events.AsEnumerable(), totalCount);
        }

        public async System.Threading.Tasks.Task ProcessEventAsync(Event @event, string callbackUrl)
        {
            _logger.LogInformation("Processing event {EventId} for callback URL {CallbackUrl}", @event.Id, callbackUrl);

            // Send the event to the callback URL
            using (var httpClient = new HttpClient())
            {
                // Add authentication if required
                if (@event.ApiKey != null)
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {@event.ApiKey}");
                }

                // Prepare the payload
                var payload = new
                {
                    event_id = @event.Id,
                    event_type = @event.Type,
                    event_data = @event.Data,
                    timestamp = DateTime.UtcNow
                };

                // Send the webhook
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                try
                {
                    var response = await httpClient.PostAsync(callbackUrl, content);

                    // Log the result
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully delivered event {EventId} to {CallbackUrl}",
                            @event.Id, callbackUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deliver event {EventId} to {CallbackUrl}: {StatusCode}",
                            @event.Id, callbackUrl, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error delivering event {EventId} to {CallbackUrl}",
                        @event.Id, callbackUrl);
                }
            }
        }

        // The following methods are not part of the IEventService interface
        // but are kept for backward compatibility with existing tests

        public async Task<Event> CreateEventAsync(Event @event)
        {
            _logger.LogInformation("Creating event of type {EventType}", @event.Type);

            @event.Id = Guid.NewGuid().ToString();
            if (@event.OccurredAt == default)
            {
                @event.OccurredAt = DateTime.UtcNow;
            }

            _events.Add(@event);

            return @event;
        }

        public async Task<bool> DeleteEventAsync(string eventId)
        {
            _logger.LogInformation("Deleting event {EventId}", eventId);

            var @event = _events.FirstOrDefault(e => e.Id == eventId);
            if (@event != null)
            {
                _events.Remove(@event);
                return true;
            }

            return false;
        }
    }
}
