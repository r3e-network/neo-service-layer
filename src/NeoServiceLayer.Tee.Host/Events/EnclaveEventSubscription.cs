using System;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models.Events;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Represents a subscription to enclave events.
    /// </summary>
    public class EnclaveEventSubscription : IEnclaveEventSubscription
    {
        private readonly IEnclaveEventSystem _eventSystem;
        private bool _disposed;

        /// <summary>
        /// Gets the ID of the subscription.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the type of events the subscription is for.
        /// </summary>
        public string EventType { get; }

        /// <summary>
        /// Gets the handler for the events.
        /// </summary>
        public Func<EnclaveEvent, Task> Handler { get; }

        /// <summary>
        /// Gets the creation time of the subscription.
        /// </summary>
        public DateTime CreationTime { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveEventSubscription"/> class.
        /// </summary>
        /// <param name="eventSystem">The event system the subscription is for.</param>
        /// <param name="eventType">The type of events the subscription is for.</param>
        /// <param name="handler">The handler for the events.</param>
        public EnclaveEventSubscription(IEnclaveEventSystem eventSystem, string eventType, Func<EnclaveEvent, Task> handler)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Id = Guid.NewGuid().ToString();
            CreationTime = DateTime.UtcNow;
            _disposed = false;
        }

        /// <summary>
        /// Disposes the subscription.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the subscription.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from the event system
                    _eventSystem.Unsubscribe(this);
                }

                _disposed = true;
            }
        }
    }
}
