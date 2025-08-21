using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Data
{
    /// <summary>
    /// Interface for the Unit of Work pattern
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all changes made in the unit of work
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of affected entities</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets whether the unit of work has an active transaction
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Gets the current transaction ID
        /// </summary>
        string? CurrentTransactionId { get; }
    }

    /// <summary>
    /// Interface for unit of work that supports domain events
    /// </summary>
    public interface IUnitOfWorkWithEvents : IUnitOfWork
    {
        /// <summary>
        /// Publishes domain events for all entities in the current unit of work
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task PublishDomainEventsAsync(CancellationToken cancellationToken = default);
    }
}