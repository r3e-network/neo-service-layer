using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace NeoServiceLayer.Core.Data
{
    /// <summary>
    /// Entity Framework implementation of the Unit of Work pattern
    /// </summary>
    public class EntityFrameworkUnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of EntityFrameworkUnitOfWork
        /// </summary>
        /// <param name="context">The database context</param>
        public EntityFrameworkUnitOfWork(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
                await _context.SaveChangesAsync(cancellationToken);
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
                throw new ObjectDisposedException(nameof(EntityFrameworkUnitOfWork));
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