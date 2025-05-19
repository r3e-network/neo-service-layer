using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Storage
{
    /// <summary>
    /// Interface for a persistent storage provider that ensures data durability.
    /// </summary>
    public interface IPersistentStorageProvider : IStorageProvider
    {
        /// <summary>
        /// Writes data to storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataChunks">The data chunks to write.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> WriteChunkedAsync(string key, IEnumerable<byte[]> dataChunks, int chunkSize);

        /// <summary>
        /// Writes data to storage using a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataStream">The stream containing the data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> WriteStreamAsync(string key, Stream dataStream);

        /// <summary>
        /// Reads data from storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>The data chunks, or null if the key does not exist.</returns>
        Task<IEnumerable<byte[]>> ReadChunkedAsync(string key, int chunkSize);

        /// <summary>
        /// Reads data from storage as a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>A stream containing the data, or null if the key does not exist.</returns>
        Task<Stream> ReadStreamAsync(string key);

        /// <summary>
        /// Gets the size of data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        Task<long> GetSizeAsync(string key);

        /// <summary>
        /// Gets the metadata for data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The metadata for the data, or null if the key does not exist.</returns>
        Task<StorageMetadata> GetMetadataAsync(string key);

        /// <summary>
        /// Updates the metadata for data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="metadata">The metadata to update.</param>
        /// <returns>True if the metadata was updated, false if the key does not exist.</returns>
        Task<bool> UpdateMetadataAsync(string key, StorageMetadata metadata);

        /// <summary>
        /// Compacts the storage to reclaim space.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> CompactAsync();

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>The transaction ID.</returns>
        Task<string> BeginTransactionAsync();

        /// <summary>
        /// Commits a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was committed successfully, false otherwise.</returns>
        Task<bool> CommitTransactionAsync(string transactionId);

        /// <summary>
        /// Rolls back a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was rolled back successfully, false otherwise.</returns>
        Task<bool> RollbackTransactionAsync(string transactionId);

        /// <summary>
        /// Writes data to storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> WriteInTransactionAsync(string transactionId, string key, byte[] data);

        /// <summary>
        /// Deletes data from storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteInTransactionAsync(string transactionId, string key);
    }
}
