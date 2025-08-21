using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Core.Domain;

namespace NeoServiceLayer.Core.Persistence
{
    /// <summary>
    /// Entity Framework Unit of Work with domain event publishing
    /// </summary>
    public class EntityFrameworkUnitOfWorkWithEvents : EntityFrameworkUnitOfWork, IUnitOfWorkWithEvents
    {
        private readonly IDomainEventPublisher _eventPublisher;

        public EntityFrameworkUnitOfWorkWithEvents(
            DbContext context,
            IDomainEventPublisher eventPublisher) 
            : base(context)
        {
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        /// <summary>
        /// Saves changes and publishes domain events
        /// </summary>
        public async Task<int> SaveChangesWithEventsAsync(CancellationToken cancellationToken = default)
        {
            // Collect domain events before saving
            var aggregateRoots = _context.ChangeTracker.Entries<AggregateRoot<object>>()
                .Where(e => e.Entity.HasDomainEvents)
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = aggregateRoots
                .SelectMany(a => a.DomainEvents)
                .ToList();

            // Save changes first
            var result = await SaveChangesAsync(cancellationToken);

            // Then publish domain events
            foreach (var domainEvent in domainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
            }

            // Clear events after publishing
            foreach (var aggregateRoot in aggregateRoots)
            {
                aggregateRoot.ClearDomainEvents();
            }

            return result;
        }
    }
}