using System;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Interface for an enclave event subscription.
    /// </summary>
    public interface IEnclaveEventSubscription : IDisposable
    {
        /// <summary>
        /// Gets the ID of the subscription.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the type of events the subscription is for.
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Gets the creation time of the subscription.
        /// </summary>
        DateTime CreationTime { get; }
    }
}
