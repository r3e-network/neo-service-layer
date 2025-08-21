using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace NeoServiceLayer.Core.Persistence
{
    /// <summary>
    /// Entity Framework implementation of Unit of Work pattern
    /// </summary>
    public class EntityFrameworkUnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private IDbContextTransaction? _transaction;

        public EntityFrameworkUnitOfWork(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Saves all changes to the database
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
                throw new InvalidOperationException("Transaction already started");

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to commit");

            try
            {
                await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to rollback");

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}