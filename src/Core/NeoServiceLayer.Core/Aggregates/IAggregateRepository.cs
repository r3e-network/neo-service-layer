using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Aggregates
{
    /// <summary>
    /// Repository interface for loading and saving aggregates
    /// </summary>
    /// <typeparam name="TAggregate">Type of aggregate</typeparam>
    public interface IAggregateRepository<TAggregate> where TAggregate : AggregateRoot, new()
    {
        /// <summary>
        /// Gets an aggregate by its ID
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The aggregate if found, null otherwise</returns>
        Task<TAggregate?> GetByIdAsync(string aggregateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an aggregate by its ID at a specific version
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="version">Version to load up to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The aggregate at the specified version</returns>
        Task<TAggregate?> GetByIdAsync(string aggregateId, long version, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves an aggregate and its uncommitted events
        /// </summary>
        /// <param name="aggregate">Aggregate to save</param>
        /// <param name="expectedVersion">Expected version for optimistic concurrency</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveAsync(TAggregate aggregate, long? expectedVersion = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an aggregate exists
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the aggregate exists</returns>
        Task<bool> ExistsAsync(string aggregateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current version of an aggregate
        /// </summary>
        /// <param name="aggregateId">Aggregate identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current version or 0 if not found</returns>
        Task<long> GetVersionAsync(string aggregateId, CancellationToken cancellationToken = default);
    }
}
