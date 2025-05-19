using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models.Events;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Interface for the enclave event system.
    /// </summary>
    public interface IEnclaveEventSystem : IDisposable
    {
        /// <summary>
        /// Publishes an event to the enclave.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventData">The data for the event.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishAsync(string eventType, object eventData);

        /// <summary>
        /// Subscribes to events of a specific type.
        /// </summary>
        /// <param name="eventType">The type of events to subscribe to.</param>
        /// <param name="handler">The handler for the events.</param>
        /// <returns>A subscription that can be used to unsubscribe.</returns>
        IEnclaveEventSubscription Subscribe(string eventType, Func<EnclaveEvent, Task> handler);

        /// <summary>
        /// Unsubscribes from events.
        /// </summary>
        /// <param name="subscription">The subscription to unsubscribe.</param>
        /// <returns>True if the subscription was removed, false otherwise.</returns>
        bool Unsubscribe(IEnclaveEventSubscription subscription);

        /// <summary>
        /// Gets all subscriptions.
        /// </summary>
        /// <returns>A list of all subscriptions.</returns>
        IReadOnlyList<IEnclaveEventSubscription> GetSubscriptions();

        /// <summary>
        /// Gets all subscriptions for a specific event type.
        /// </summary>
        /// <param name="eventType">The type of events to get subscriptions for.</param>
        /// <returns>A list of subscriptions for the specified event type.</returns>
        IReadOnlyList<IEnclaveEventSubscription> GetSubscriptions(string eventType);
    }
}
