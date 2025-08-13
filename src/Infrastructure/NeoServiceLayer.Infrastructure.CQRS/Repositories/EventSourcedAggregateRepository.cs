using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Aggregates;
using NeoServiceLayer.Core.Events;

namespace NeoServiceLayer.Infrastructure.CQRS.Repositories
{
    /// <summary>
    /// Repository implementation for event-sourced aggregates
    /// </summary>
    /// <typeparam name="TAggregate">Type of aggregate</typeparam>
    public class EventSourcedAggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
        where TAggregate : AggregateRoot, new()
    {
        private readonly IEventStore _eventStore;
        private readonly IEventBus _eventBus;
        private readonly ILogger<EventSourcedAggregateRepository<TAggregate>> _logger;
        private readonly bool _useSnapshots;
        private readonly int _snapshotFrequency;

        public EventSourcedAggregateRepository(
            IEventStore eventStore,
            IEventBus eventBus,
            ILogger<EventSourcedAggregateRepository<TAggregate>> logger,
            bool useSnapshots = true,
            int snapshotFrequency = 100)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _useSnapshots = useSnapshots;
            _snapshotFrequency = snapshotFrequency;
        }

        public async Task<TAggregate?> GetByIdAsync(string aggregateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            try
            {
                var aggregate = new TAggregate();
                long fromVersion = 0;

                // Try to load from snapshot if enabled
                if (_useSnapshots)
                {
                    var snapshot = await _eventStore.GetLatestSnapshotAsync(aggregateId, cancellationToken);
                    if (snapshot != null)
                    {
                        aggregate.RestoreFromSnapshot(new AggregateSnapshot(
                            snapshot.AggregateId,
                            snapshot.AggregateType,
                            snapshot.Version,
                            snapshot.CreatedAt,
                            snapshot.Data));
                        
                        fromVersion = snapshot.Version;
                        
                        _logger.LogDebug(
                            "Loaded aggregate {AggregateId} from snapshot at version {Version}",
                            aggregateId, fromVersion);
                    }
                }

                // Load events after snapshot
                var events = await _eventStore.GetEventsAsync(aggregateId, fromVersion, cancellationToken);
                var eventList = events.ToList();
                
                if (!eventList.Any() && fromVersion == 0)
                {
                    _logger.LogDebug("Aggregate {AggregateId} not found", aggregateId);
                    return null;
                }

                // Apply events to aggregate
                aggregate.LoadFromHistory(eventList);
                aggregate.Id = aggregateId;

                _logger.LogDebug(
                    "Loaded aggregate {AggregateId} with {EventCount} events, version {Version}",
                    aggregateId, eventList.Count, aggregate.Version);

                return aggregate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load aggregate {AggregateId}", aggregateId);
                throw;
            }
        }

        public async Task<TAggregate?> GetByIdAsync(
            string aggregateId, 
            long version, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            if (version < 0)
                throw new ArgumentException("Version cannot be negative", nameof(version));

            try
            {
                var aggregate = new TAggregate();

                // Load events up to specified version
                var events = await _eventStore.GetEventsAsync(aggregateId, 0, cancellationToken);
                var eventList = events.Where(e => e.AggregateVersion <= version).ToList();
                
                if (!eventList.Any())
                {
                    _logger.LogDebug(
                        "Aggregate {AggregateId} not found at version {Version}",
                        aggregateId, version);
                    return null;
                }

                // Apply events to aggregate
                aggregate.LoadFromHistory(eventList);
                aggregate.Id = aggregateId;

                _logger.LogDebug(
                    "Loaded aggregate {AggregateId} at version {Version}",
                    aggregateId, aggregate.Version);

                return aggregate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to load aggregate {AggregateId} at version {Version}",
                    aggregateId, version);
                throw;
            }
        }

        public async Task SaveAsync(
            TAggregate aggregate, 
            long? expectedVersion = null, 
            CancellationToken cancellationToken = default)
        {
            if (aggregate == null)
                throw new ArgumentNullException(nameof(aggregate));

            if (string.IsNullOrWhiteSpace(aggregate.Id))
                throw new InvalidOperationException("Aggregate ID cannot be null or empty");

            var uncommittedEvents = aggregate.UncommittedEvents.ToList();
            
            if (!uncommittedEvents.Any())
            {
                _logger.LogDebug("No uncommitted events to save for aggregate {AggregateId}", aggregate.Id);
                return;
            }

            try
            {
                // Use expected version if provided, otherwise use current version minus uncommitted events
                var baseVersion = expectedVersion ?? (aggregate.Version - uncommittedEvents.Count);
                
                // Append events to event store
                await _eventStore.AppendEventsAsync(
                    aggregate.Id,
                    baseVersion,
                    uncommittedEvents,
                    cancellationToken);

                // Publish events to event bus
                await _eventBus.PublishBatchAsync(uncommittedEvents, cancellationToken);

                // Create snapshot if needed
                if (_useSnapshots && aggregate.Version % _snapshotFrequency == 0)
                {
                    var snapshot = aggregate.CreateSnapshot();
                    await _eventStore.SaveSnapshotAsync(
                        aggregate.Id,
                        aggregate.Version,
                        snapshot.Data,
                        cancellationToken);
                    
                    _logger.LogDebug(
                        "Created snapshot for aggregate {AggregateId} at version {Version}",
                        aggregate.Id, aggregate.Version);
                }

                // Mark events as committed
                aggregate.MarkEventsAsCommitted();

                _logger.LogInformation(
                    "Saved {EventCount} events for aggregate {AggregateId}, version {Version}",
                    uncommittedEvents.Count, aggregate.Id, aggregate.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to save aggregate {AggregateId} with {EventCount} events",
                    aggregate.Id, uncommittedEvents.Count);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string aggregateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            var version = await _eventStore.GetAggregateVersionAsync(aggregateId, cancellationToken);
            return version > 0;
        }

        public async Task<long> GetVersionAsync(string aggregateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            return await _eventStore.GetAggregateVersionAsync(aggregateId, cancellationToken);
        }
    }
}