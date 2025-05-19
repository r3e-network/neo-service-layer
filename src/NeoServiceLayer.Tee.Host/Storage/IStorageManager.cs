using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Interface for managing storage operations.
    /// </summary>
    public interface IStorageManager : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the storage manager is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the storage manager.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Gets a storage provider by name.
        /// </summary>
        /// <param name="name">The name of the storage provider.</param>
        /// <returns>The storage provider, or null if not found.</returns>
        IStorageProvider GetProvider(string name);

        /// <summary>
        /// Gets all storage providers.
        /// </summary>
        /// <returns>A list of all storage providers.</returns>
        IReadOnlyList<IStorageProvider> GetProviders();

        /// <summary>
        /// Registers a storage provider.
        /// </summary>
        /// <param name="provider">The storage provider to register.</param>
        /// <returns>True if the provider was registered, false otherwise.</returns>
        bool RegisterProvider(IStorageProvider provider);

        /// <summary>
        /// Unregisters a storage provider.
        /// </summary>
        /// <param name="name">The name of the storage provider to unregister.</param>
        /// <returns>True if the provider was unregistered, false otherwise.</returns>
        bool UnregisterProvider(string name);

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string providerName, string key);

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        Task<bool> WriteAsync(string providerName, string key, byte[] data);

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteAsync(string providerName, string key);

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string providerName, string key);

        /// <summary>
        /// Gets all keys in storage.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <returns>A list of all keys.</returns>
        Task<IReadOnlyList<string>> GetAllKeysAsync(string providerName);

        /// <summary>
        /// Gets the size of data in storage.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <param name="key">The key for the data.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        Task<long> GetSizeAsync(string providerName, string key);

        /// <summary>
        /// Reads data from storage as a stream.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <param name="key">The key for the data.</param>
        /// <returns>A stream containing the data, or null if the key does not exist.</returns>
        Task<Stream> ReadStreamAsync(string providerName, string key);

        /// <summary>
        /// Writes data to storage from a stream.
        /// </summary>
        /// <param name="providerName">The name of the storage provider.</param>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataStream">The stream containing the data.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        Task<bool> WriteStreamAsync(string providerName, string key, Stream dataStream);
    }
}
