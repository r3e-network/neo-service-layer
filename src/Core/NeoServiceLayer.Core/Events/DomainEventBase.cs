using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Events
{
    /// <summary>
    /// Base implementation for domain events providing common properties
    /// and ensuring immutability after creation.
    /// </summary>
    public abstract class DomainEventBase : IDomainEvent
    {
        /// <summary>
        /// Initializes a new domain event with required properties
        /// </summary>
        /// <param name="aggregateId">Identifier of the aggregate root</param>
        /// <param name="aggregateType">Type name of the aggregate</param>
        /// <param name="aggregateVersion">Version of the aggregate</param>
        /// <param name="initiatedBy">User or system that initiated the action</param>
        /// <param name="causationId">ID of the command or event that caused this event</param>
        /// <param name="correlationId">Correlation ID for tracking related events</param>
        protected DomainEventBase(
            string aggregateId,
            string aggregateType,
            long aggregateVersion,
            string initiatedBy,
            Guid? causationId = null,
            Guid? correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));
            
            if (string.IsNullOrWhiteSpace(aggregateType))
                throw new ArgumentException("Aggregate type cannot be null or empty", nameof(aggregateType));
            
            if (string.IsNullOrWhiteSpace(initiatedBy))
                throw new ArgumentException("Initiated by cannot be null or empty", nameof(initiatedBy));
            
            EventId = Guid.NewGuid();
            OccurredAt = DateTime.UtcNow;
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            EventType = GetType().Name;
            InitiatedBy = initiatedBy;
            CausationId = causationId;
            CorrelationId = correlationId ?? Guid.NewGuid();
            Metadata = new Dictionary&lt;string, object&gt;();
        }

        public Guid EventId { get; }
        
        public DateTime OccurredAt { get; }
        
        public string AggregateId { get; }
        
        public string AggregateType { get; }
        
        public long AggregateVersion { get; }
        
        public string EventType { get; }
        
        public Guid? CausationId { get; }
        
        public Guid? CorrelationId { get; }
        
        public string InitiatedBy { get; }
        
        public IDictionary&lt;string, object&gt; Metadata { get; }

        /// <summary>
        /// Adds metadata to the event. Can only be called during construction.
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        protected void AddMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
            
            Metadata[key] = value;
        }
        
        /// <summary>
        /// Returns a string representation of the event for logging and debugging
        /// </summary>
        public override string ToString()
        {
            return $"{EventType} - AggregateId: {AggregateId}, Version: {AggregateVersion}, EventId: {EventId}, OccurredAt: {OccurredAt:O}";
        }
    }
}