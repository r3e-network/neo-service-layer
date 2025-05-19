using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Represents a transaction for atomic operations on a storage provider.
    /// </summary>
    public class StorageTransaction : IDisposable
    {
        private readonly ILogger<StorageTransaction> _logger;
        private readonly IPersistentStorageProvider _provider;
        private readonly List<TransactionOperation> _operations;
        private readonly string _transactionId;
        private bool _committed;
        private bool _rolledBack;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageTransaction"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="provider">The storage provider to use for the transaction.</param>
        public StorageTransaction(ILogger<StorageTransaction> logger, IPersistentStorageProvider provider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _operations = new List<TransactionOperation>();
            _transactionId = Guid.NewGuid().ToString();
            _committed = false;
            _rolledBack = false;
            _disposed = false;

            _logger.LogDebug("Transaction {TransactionId} created", _transactionId);
        }

        /// <summary>
        /// Gets the transaction ID.
        /// </summary>
        public string TransactionId => _transactionId;

        /// <summary>
        /// Gets whether the transaction has been committed.
        /// </summary>
        public bool IsCommitted => _committed;

        /// <summary>
        /// Gets whether the transaction has been rolled back.
        /// </summary>
        public bool IsRolledBack => _rolledBack;

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task WriteAsync(string key, byte[] data)
        {
            CheckDisposed();
            CheckNotCommittedOrRolledBack();

            _logger.LogDebug("Transaction {TransactionId}: Adding write operation for key {Key}", _transactionId, key);
            _operations.Add(new TransactionOperation
            {
                OperationType = TransactionOperationType.Write,
                Key = key,
                Data = data
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task DeleteAsync(string key)
        {
            CheckDisposed();
            CheckNotCommittedOrRolledBack();

            _logger.LogDebug("Transaction {TransactionId}: Adding delete operation for key {Key}", _transactionId, key);
            _operations.Add(new TransactionOperation
            {
                OperationType = TransactionOperationType.Delete,
                Key = key
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CommitAsync()
        {
            CheckDisposed();
            CheckNotCommittedOrRolledBack();

            _logger.LogDebug("Transaction {TransactionId}: Committing {OperationCount} operations", _transactionId, _operations.Count);

            try
            {
                // Execute all operations
                foreach (var operation in _operations)
                {
                    switch (operation.OperationType)
                    {
                        case TransactionOperationType.Write:
                            await _provider.WriteAsync(operation.Key, operation.Data);
                            break;

                        case TransactionOperationType.Delete:
                            await _provider.DeleteAsync(operation.Key);
                            break;

                        default:
                            throw new StorageException($"Unsupported operation type: {operation.OperationType}");
                    }
                }

                // Flush the provider to ensure all operations are persisted
                await _provider.FlushAsync();

                _committed = true;
                _logger.LogDebug("Transaction {TransactionId}: Committed successfully", _transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction {TransactionId}: Failed to commit", _transactionId);
                throw new StorageException($"Failed to commit transaction {_transactionId}", ex);
            }
        }

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task RollbackAsync()
        {
            CheckDisposed();
            CheckNotCommittedOrRolledBack();

            _logger.LogDebug("Transaction {TransactionId}: Rolling back {OperationCount} operations", _transactionId, _operations.Count);

            // Clear the operations
            _operations.Clear();
            _rolledBack = true;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the transaction.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the transaction.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (!_committed && !_rolledBack)
                    {
                        // Automatically roll back the transaction if not committed or rolled back
                        _logger.LogWarning("Transaction {TransactionId}: Automatically rolling back uncommitted transaction", _transactionId);
                        RollbackAsync().GetAwaiter().GetResult();
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Checks if the transaction has been disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Checks if the transaction has not been committed or rolled back.
        /// </summary>
        private void CheckNotCommittedOrRolledBack()
        {
            if (_committed)
            {
                throw new StorageException("Transaction has already been committed");
            }

            if (_rolledBack)
            {
                throw new StorageException("Transaction has already been rolled back");
            }
        }

        /// <summary>
        /// Types of operations that can be performed in a transaction.
        /// </summary>
        private enum TransactionOperationType
        {
            /// <summary>
            /// Write operation.
            /// </summary>
            Write,

            /// <summary>
            /// Delete operation.
            /// </summary>
            Delete
        }

        /// <summary>
        /// Represents an operation in a transaction.
        /// </summary>
        private class TransactionOperation
        {
            /// <summary>
            /// Gets or sets the type of operation.
            /// </summary>
            public TransactionOperationType OperationType { get; set; }

            /// <summary>
            /// Gets or sets the key for the operation.
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// Gets or sets the data for the operation.
            /// </summary>
            public byte[] Data { get; set; }
        }
    }
}
