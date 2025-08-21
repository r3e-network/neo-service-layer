using System.Collections.Generic;
using System.Linq;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Base class for aggregate roots in domain-driven design
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate identifier</typeparam>
    public abstract class AggregateRoot<TId> : Entity<TId> where TId : class
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>
        /// Gets the domain events that have been added to this aggregate
        /// </summary>
        public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Adds a domain event to be published when the aggregate is saved
        /// </summary>
        /// <param name="domainEvent">The domain event to add</param>
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                return;

            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Removes a domain event from the aggregate
        /// </summary>
        /// <param name="domainEvent">The domain event to remove</param>
        protected void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        /// <summary>
        /// Clears all domain events from the aggregate
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// Gets whether the aggregate has any uncommitted domain events
        /// </summary>
        public bool HasDomainEvents => _domainEvents.Any();
    }
}