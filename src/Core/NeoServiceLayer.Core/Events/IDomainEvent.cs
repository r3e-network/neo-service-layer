using System;

namespace NeoServiceLayer.Core.Events
{
    /// <summary>
    /// Base interface for all domain events in the Neo Service Layer.
    /// Domain events represent something that has happened in the business domain
    /// and are immutable facts about the system state.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique identifier for the event
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// UTC timestamp when the event occurred
        /// </summary>
        DateTime OccurredAt { get; }

        /// <summary>
        /// Identifier of the aggregate root that generated this event
        /// </summary>
        string AggregateId { get; }

        /// <summary>
        /// Type name of the aggregate that generated this event
        /// </summary>
        string AggregateType { get; }

        /// <summary>
        /// Version of the aggregate when this event was generated
        /// Used for optimistic concurrency control
        /// </summary>
        long AggregateVersion { get; }

        /// <summary>
        /// Event type identifier for event routing and handling
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Causation ID - tracks the command or event that caused this event
        /// </summary>
        Guid? CausationId { get; }

        /// <summary>
        /// Correlation ID - tracks related events across aggregate boundaries
        /// </summary>
        Guid? CorrelationId { get; }

        /// <summary>
        /// User or system that initiated the action leading to this event
        /// </summary>
        string InitiatedBy { get; }

        /// <summary>
        /// Additional metadata associated with the event
        /// </summary>
        IDictionary&lt;string, object&gt; Metadata { get; }
}
}
