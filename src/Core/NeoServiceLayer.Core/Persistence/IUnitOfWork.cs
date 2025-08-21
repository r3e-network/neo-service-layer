using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Persistence
{
    /// <summary>
    /// Unit of Work pattern for managing database transactions
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Saves all changes to the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected rows</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transaction object</returns>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Unit of Work with domain event publishing
    /// </summary>
    public interface IUnitOfWorkWithEvents : IUnitOfWork
    {
        /// <summary>
        /// Saves changes and publishes domain events
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected rows</returns>
        Task<int> SaveChangesWithEventsAsync(CancellationToken cancellationToken = default);
    }
}