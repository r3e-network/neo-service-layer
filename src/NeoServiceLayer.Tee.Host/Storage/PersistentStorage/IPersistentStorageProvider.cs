using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Interface for a persistent storage provider that ensures data durability.
    /// </summary>
    public interface IPersistentStorageProvider : IDisposable
    {
        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task WriteAsync(string key, byte[] data);

        /// <summary>
        /// Writes data to storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataChunks">The data chunks to write.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task WriteChunkedAsync(string key, IEnumerable<byte[]> dataChunks, int chunkSize);

        /// <summary>
        /// Writes data to storage using a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataStream">The stream containing the data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task WriteStreamAsync(string key, Stream dataStream);

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string key);

        /// <summary>
        /// Reads data from storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>An enumerable of data chunks, or null if the key does not exist.</returns>
        Task<IEnumerable<byte[]>> ReadChunkedAsync(string key, int chunkSize);

        /// <summary>
        /// Reads data from storage as a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>A stream containing the data, or null if the key does not exist.</returns>
        Task<Stream> ReadStreamAsync(string key);

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteAsync(string key);

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Gets all keys in storage.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        Task<IReadOnlyList<string>> GetAllKeysAsync();

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
        /// Flushes any pending writes to storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task FlushAsync();

        /// <summary>
        /// Compacts the storage to reclaim space.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CompactAsync();
    }
}
