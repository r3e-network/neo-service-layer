using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Interface for secure storage of sensitive data.
    /// </summary>
    public interface ISecureStorage : IDisposable
    {
        /// <summary>
        /// Stores a value securely.
        /// </summary>
        /// <param name="key">The key for the value.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StoreAsync(string key, string value);

        /// <summary>
        /// Retrieves a value securely.
        /// </summary>
        /// <param name="key">The key for the value.</param>
        /// <returns>The retrieved value, or null if the key does not exist.</returns>
        Task<string> RetrieveAsync(string key);

        /// <summary>
        /// Removes a value securely.
        /// </summary>
        /// <param name="key">The key for the value.</param>
        /// <returns>True if the value was removed, false if the key does not exist.</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// Checks if a key exists.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Gets all keys.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        Task<IReadOnlyList<string>> GetAllKeysAsync();

        /// <summary>
        /// Clears all values.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ClearAsync();
    }
}
