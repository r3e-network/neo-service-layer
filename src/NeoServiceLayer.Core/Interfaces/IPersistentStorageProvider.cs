using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for persistent storage providers.
    /// </summary>
    public interface IPersistentStorageProvider
    {
        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Stores data in persistent storage.
        /// </summary>
        /// <param name="key">The key to store the data under.</param>
        /// <param name="data">The data to store.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StoreAsync(string key, byte[] data);

        /// <summary>
        /// Retrieves data from persistent storage.
        /// </summary>
        /// <param name="key">The key to retrieve the data for.</param>
        /// <returns>The retrieved data, or null if the key does not exist.</returns>
        Task<byte[]> RetrieveAsync(string key);

        /// <summary>
        /// Deletes data from persistent storage.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync(string key);

        /// <summary>
        /// Checks if a key exists in persistent storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Lists all keys in persistent storage.
        /// </summary>
        /// <returns>An array of keys.</returns>
        Task<string[]> ListKeysAsync();
    }
}
