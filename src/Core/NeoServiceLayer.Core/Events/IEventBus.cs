using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;


namespace NeoServiceLayer.Core.Events
{
    /// <summary>
    /// Event bus for publishing and subscribing to domain events
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes a single domain event
        /// </summary>
        /// <param name="domainEvent">The event to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes multiple domain events as a batch
        /// </summary>
        /// <param name="domainEvents">The events to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task PublishBatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to events of a specific type
        /// </summary>
        /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
        /// <param name="handler">Event handler</param>
        /// <returns>Subscription that can be disposed to unsubscribe</returns>
        IEventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : class, IDomainEvent;

        /// <summary>
        /// Subscribes to all events with a pattern filter
        /// </summary>
        /// <param name="eventTypePattern">Event type pattern (supports wildcards)</param>
        /// <param name="handler">Generic event handler</param>
        /// <returns>Subscription that can be disposed to unsubscribe</returns>
        IEventSubscription Subscribe(string eventTypePattern, IEventHandler<IDomainEvent> handler);
    }

    /// <summary>
    /// Represents an event subscription that can be disposed
    /// </summary>
    public interface IEventSubscription : IDisposable
    {
        /// <summary>
        /// Unique identifier for the subscription
        /// </summary>
        Guid SubscriptionId { get; }

        /// <summary>
        /// Whether the subscription is active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Event type pattern this subscription handles
        /// </summary>
        string EventTypePattern { get; }

        /// <summary>
        /// When the subscription was created
        /// </summary>
        DateTime CreatedAt { get; }
    }
}
