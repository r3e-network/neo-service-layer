using System;
using System.Collections.Generic;
using System.Linq;
using NeoServiceLayer.Core.Events;

namespace NeoServiceLayer.Core.Aggregates
{
    /// <summary>
    /// Base class for aggregate roots in domain-driven design
    /// </summary>
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _uncommittedEvents = new();
        private readonly Dictionary<Type, Action<IDomainEvent>> _eventHandlers = new();

        /// <summary>
        /// Unique identifier for the aggregate
        /// </summary>
        public string Id { get; protected set; } = string.Empty;

        /// <summary>
        /// Current version of the aggregate (for optimistic concurrency)
        /// </summary>
        public long Version { get; protected set; }

        /// <summary>
        /// Timestamp when the aggregate was created
        /// </summary>
        public DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// Timestamp when the aggregate was last modified
        /// </summary>
        public DateTime? LastModifiedAt { get; protected set; }

        /// <summary>
        /// User or system that created the aggregate
        /// </summary>
        public string CreatedBy { get; protected set; } = string.Empty;

        /// <summary>
        /// User or system that last modified the aggregate
        /// </summary>
        public string? LastModifiedBy { get; protected set; }

        /// <summary>
        /// Gets the uncommitted events
        /// </summary>
        public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

        protected AggregateRoot()
        {
            RegisterEventHandlers();
        }

        /// <summary>
        /// Registers event handlers for applying events to the aggregate
        /// </summary>
        protected abstract void RegisterEventHandlers();

        /// <summary>
        /// Registers an event handler for a specific event type
        /// </summary>
        protected void RegisterHandler<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
        {
            _eventHandlers[typeof(TEvent)] = e => handler((TEvent)e);
        }

        /// <summary>
        /// Applies an event to the aggregate and adds it to uncommitted events
        /// </summary>
        protected void RaiseEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            ApplyEvent(domainEvent);
            _uncommittedEvents.Add(domainEvent);
            Version++;
            LastModifiedAt = domainEvent.OccurredAt;
            LastModifiedBy = domainEvent.InitiatedBy;
        }

        /// <summary>
        /// Applies an event to the aggregate without adding it to uncommitted events
        /// Used when replaying events from the event store
        /// </summary>
        public void ApplyEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            var eventType = domainEvent.GetType();
            if (_eventHandlers.TryGetValue(eventType, out var handler))
            {
                handler(domainEvent);
            }
            else
            {
                // Try to find a handler for base types
                foreach (var registeredType in _eventHandlers.Keys)
                {
                    if (registeredType.IsAssignableFrom(eventType))
                    {
                        _eventHandlers[registeredType](domainEvent);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Loads the aggregate from a history of events
        /// </summary>
        public void LoadFromHistory(IEnumerable<IDomainEvent> events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            foreach (var domainEvent in events.OrderBy(e => e.AggregateVersion))
            {
                ApplyEvent(domainEvent);
                Version = domainEvent.AggregateVersion;

                if (!CreatedAt.Equals(default(DateTime)))
                {
                    CreatedAt = domainEvent.OccurredAt;
                    CreatedBy = domainEvent.InitiatedBy;
                }

                LastModifiedAt = domainEvent.OccurredAt;
                LastModifiedBy = domainEvent.InitiatedBy;
            }
        }

        /// <summary>
        /// Marks all events as committed
        /// </summary>
        public void MarkEventsAsCommitted()
        {
            _uncommittedEvents.Clear();
        }

        /// <summary>
        /// Creates a snapshot of the aggregate's current state
        /// </summary>
        public virtual AggregateSnapshot CreateSnapshot()
        {
            return new AggregateSnapshot(
                Id,
                GetType().Name,
                Version,
                DateTime.UtcNow,
                GetSnapshotData());
        }

        /// <summary>
        /// Restores the aggregate from a snapshot
        /// </summary>
        public virtual void RestoreFromSnapshot(AggregateSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            Id = snapshot.AggregateId;
            Version = snapshot.Version;
            RestoreSnapshotData(snapshot.Data);
        }

        /// <summary>
        /// Gets the data to include in a snapshot
        /// Override this to include aggregate-specific state
        /// </summary>
        protected virtual object GetSnapshotData()
        {
            return new
            {
                Id,
                Version,
                CreatedAt,
                LastModifiedAt,
                CreatedBy,
                LastModifiedBy
            };
        }

        /// <summary>
        /// Restores aggregate state from snapshot data
        /// Override this to restore aggregate-specific state
        /// </summary>
        protected virtual void RestoreSnapshotData(object data)
        {
            // Base implementation does nothing
            // Derived classes should override to restore their specific state
        }

        /// <summary>
        /// Validates the aggregate's invariants
        /// </summary>
        protected abstract void ValidateInvariants();

        /// <summary>
        /// Ensures the aggregate is in a valid state
        /// </summary>
        protected void EnsureValidState()
        {
            ValidateInvariants();
        }
    }

    /// <summary>
    /// Represents a snapshot of an aggregate's state
    /// </summary>
    public class AggregateSnapshot
    {
        public AggregateSnapshot(
            string aggregateId,
            string aggregateType,
            long version,
            DateTime createdAt,
            object data)
        {
            AggregateId = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
            AggregateType = aggregateType ?? throw new ArgumentNullException(nameof(aggregateType));
            Version = version;
            CreatedAt = createdAt;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public string AggregateId { get; }
        public string AggregateType { get; }
        public long Version { get; }
        public DateTime CreatedAt { get; }
        public object Data { get; }
    }
}
