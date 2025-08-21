using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NeoServiceLayer.Core.Domain;

namespace NeoServiceLayer.Core.Data
{
    /// <summary>
    /// Entity Framework implementation of the Unit of Work pattern with domain events
    /// </summary>
    public class EntityFrameworkUnitOfWorkWithEvents : IUnitOfWorkWithEvents
    {
        private readonly DbContext _context;
        private readonly IDomainEventPublisher _eventPublisher;
        private IDbContextTransaction? _transaction;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of EntityFrameworkUnitOfWorkWithEvents
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="eventPublisher">The domain event publisher</param>
        public EntityFrameworkUnitOfWorkWithEvents(
            DbContext context,
            IDomainEventPublisher eventPublisher)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        /// <inheritdoc />
        public bool HasActiveTransaction => _transaction != null;

        /// <inheritdoc />
        public string? CurrentTransactionId => _transaction?.TransactionId.ToString();

        /// <inheritdoc />
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already active");
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (_transaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit");
            }

            try
            {
                // Save changes first
                await _context.SaveChangesAsync(cancellationToken);
                
                // Publish domain events
                await PublishDomainEventsAsync(cancellationToken);
                
                // Commit the transaction
                await _transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        /// <inheritdoc />
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (_transaction == null)
            {
                throw new InvalidOperationException("No active transaction to rollback");
            }

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task PublishDomainEventsAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Get all aggregate roots with domain events
            var aggregateRoots = _context.ChangeTracker.Entries<AggregateRoot<object>>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            // Collect all domain events
            var domainEvents = aggregateRoots
                .SelectMany(ar => ar.DomainEvents)
                .ToList();

            // Clear domain events from aggregates before publishing
            foreach (var aggregateRoot in aggregateRoots)
            {
                aggregateRoot.ClearDomainEvents();
            }

            // Publish events
            foreach (var domainEvent in domainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
        }

        /// <summary>
        /// Disposes the current transaction
        /// </summary>
        /// <returns>Task representing the operation</returns>
        private async Task DisposeTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Throws an exception if the object is disposed
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the object is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EntityFrameworkUnitOfWorkWithEvents));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _disposed = true;
            }
        }
    }
}