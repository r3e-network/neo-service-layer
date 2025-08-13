using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Events
{
    /// <summary>
    /// Event store for persisting and retrieving domain events
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Appends events to the event store for a specific aggregate
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="expectedVersion">Expected version for optimistic concurrency</param>
        /// <param name="events">Events to append</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task AppendEventsAsync(
            string aggregateId, 
            long expectedVersion, 
            IEnumerable&lt;IDomainEvent&gt; events, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves all events for a specific aggregate
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="fromVersion">Starting version (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Events for the aggregate</returns>
        Task&lt;IEnumerable&lt;IDomainEvent&gt;&gt; GetEventsAsync(
            string aggregateId, 
            long fromVersion = 0, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves events by event type
        /// </summary>
        /// <param name="eventType">Type of events to retrieve</param>
        /// <param name="fromTimestamp">Starting timestamp</param>
        /// <param name="toTimestamp">Ending timestamp</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Events matching the criteria</returns>
        Task&lt;IEnumerable&lt;IDomainEvent&gt;&gt; GetEventsByTypeAsync(
            string eventType,
            DateTime? fromTimestamp = null,
            DateTime? toTimestamp = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves events by correlation ID
        /// </summary>
        /// <param name="correlationId">Correlation identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Events with matching correlation ID</returns>
        Task&lt;IEnumerable&lt;IDomainEvent&gt;&gt; GetEventsByCorrelationAsync(
            Guid correlationId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the current version of an aggregate
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current version of the aggregate, or 0 if not found</returns>
        Task&lt;long&gt; GetAggregateVersionAsync(
            string aggregateId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a snapshot of the event store state
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="version">Version to snapshot</param>
        /// <param name="snapshot">Snapshot data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveSnapshotAsync(
            string aggregateId, 
            long version, 
            object snapshot, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves the latest snapshot for an aggregate
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Snapshot if available, null otherwise</returns>
        Task&lt;EventSnapshot?&gt; GetLatestSnapshotAsync(
            string aggregateId, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a snapshot of aggregate state
    /// </summary>
    public class EventSnapshot
    {
        public string AggregateId { get; set; } = string.Empty;
        public string AggregateType { get; set; } = string.Empty;
        public long Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public object Data { get; set; } = new();
        public string SerializedData { get; set; } = string.Empty;
    }
}