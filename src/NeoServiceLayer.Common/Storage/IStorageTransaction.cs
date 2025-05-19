using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Common.Storage
{
    /// <summary>
    /// Defines a transaction for storage operations.
    /// </summary>
    public interface IStorageTransaction : IDisposable
    {
        /// <summary>
        /// Gets the transaction ID.
        /// </summary>
        string TransactionId { get; }

        /// <summary>
        /// Reads data from storage within the transaction.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <returns>The data associated with the key, or null if the key does not exist.</returns>
        Task<byte[]?> ReadAsync(string key);

        /// <summary>
        /// Writes data to storage within the transaction.
        /// </summary>
        /// <param name="key">The key to write.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the write was successful, false otherwise.</returns>
        Task<bool> WriteAsync(string key, byte[] data);

        /// <summary>
        /// Deletes data from storage within the transaction.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <returns>True if the delete was successful, false otherwise.</returns>
        Task<bool> DeleteAsync(string key);

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <returns>True if the commit was successful, false otherwise.</returns>
        Task<bool> CommitAsync();

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        /// <returns>True if the rollback was successful, false otherwise.</returns>
        Task<bool> RollbackAsync();
    }
}
