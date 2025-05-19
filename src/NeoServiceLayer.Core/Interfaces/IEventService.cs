using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for the event service.
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Creates a new subscription.
        /// </summary>
        /// <param name="subscription">The subscription to create.</param>
        /// <returns>The created subscription.</returns>
        Task<Subscription> CreateSubscriptionAsync(Subscription subscription);

        /// <summary>
        /// Gets a subscription by ID.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription to get.</param>
        /// <returns>The subscription with the specified ID.</returns>
        Task<Subscription> GetSubscriptionAsync(string subscriptionId);

        /// <summary>
        /// Gets all subscriptions for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="eventType">Optional event type filter.</param>
        /// <returns>A list of subscriptions for the user.</returns>
        Task<IEnumerable<Subscription>> GetSubscriptionsAsync(string userId, EventType? eventType = null);

        /// <summary>
        /// Updates a subscription.
        /// </summary>
        /// <param name="subscription">The subscription to update.</param>
        /// <returns>The updated subscription.</returns>
        Task<Subscription> UpdateSubscriptionAsync(Subscription subscription);

        /// <summary>
        /// Deletes a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription to delete.</param>
        /// <returns>True if the subscription was deleted, false otherwise.</returns>
        Task<bool> DeleteSubscriptionAsync(string subscriptionId);

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="event">The event to publish.</param>
        /// <returns>The published event.</returns>
        Task<Event> PublishEventAsync(Event @event);

        /// <summary>
        /// Gets an event by ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to get.</param>
        /// <returns>The event with the specified ID.</returns>
        Task<Event> GetEventAsync(string eventId);

        /// <summary>
        /// Gets all events for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="eventType">Optional event type filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of events for the user.</returns>
        Task<(IEnumerable<Event> Events, int TotalCount)> GetEventsAsync(string userId, EventType? eventType = null, int page = 1, int pageSize = 10);

        /// <summary>
        /// Processes an event and sends it to the specified callback URL.
        /// </summary>
        /// <param name="event">The event to process.</param>
        /// <param name="callbackUrl">The callback URL to send the event to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task ProcessEventAsync(Event @event, string callbackUrl);
    }
}
