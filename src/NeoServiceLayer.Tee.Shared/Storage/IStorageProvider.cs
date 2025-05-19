using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Storage
{
    /// <summary>
    /// Interface for a storage provider that provides basic storage operations.
    /// </summary>
    public interface IStorageProvider : IDisposable
    {
        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> WriteAsync(string key, byte[] data);

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string key);

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
        /// Flushes any pending writes to storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> FlushAsync();
    }
}
