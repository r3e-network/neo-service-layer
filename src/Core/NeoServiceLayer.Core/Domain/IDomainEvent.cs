using System;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Marker interface for domain events
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Gets the unique identifier for this event
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Gets when the event occurred
        /// </summary>
        DateTime OccurredAt { get; }
    }
}