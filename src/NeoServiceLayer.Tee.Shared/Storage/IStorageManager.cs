using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Storage
{
    /// <summary>
    /// Interface for a storage manager that manages multiple storage providers.
    /// </summary>
    public interface IStorageManager : IDisposable
    {
        /// <summary>
        /// Initializes the storage manager.
        /// </summary>
        /// <param name="storagePath">The path to the storage directory.</param>
        /// <returns>True if the storage manager was initialized successfully, false otherwise.</returns>
        Task<bool> InitializeAsync(string storagePath);

        /// <summary>
        /// Creates a storage provider.
        /// </summary>
        /// <param name="name">The name of the provider.</param>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        Task<IStorageProvider> CreateProviderAsync(string name, StorageProviderType providerType, object options = null);

        /// <summary>
        /// Gets a storage provider by name.
        /// </summary>
        /// <param name="name">The name of the provider.</param>
        /// <returns>The provider, or null if the provider does not exist.</returns>
        IStorageProvider GetProvider(string name);

        /// <summary>
        /// Gets all storage providers.
        /// </summary>
        /// <returns>A dictionary of all providers.</returns>
        IReadOnlyDictionary<string, IStorageProvider> GetAllProviders();

        /// <summary>
        /// Removes a storage provider.
        /// </summary>
        /// <param name="name">The name of the provider to remove.</param>
        /// <returns>True if the provider was removed, false if the provider does not exist.</returns>
        Task<bool> RemoveProviderAsync(string name);

        /// <summary>
        /// Gets the default storage provider.
        /// </summary>
        /// <returns>The default provider.</returns>
        IStorageProvider GetDefaultProvider();

        /// <summary>
        /// Sets the default storage provider.
        /// </summary>
        /// <param name="name">The name of the provider to set as default.</param>
        /// <returns>True if the provider was set as default, false if the provider does not exist.</returns>
        bool SetDefaultProvider(string name);

        /// <summary>
        /// Writes data to the default storage provider.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        Task<bool> WriteAsync(string key, byte[] data);

        /// <summary>
        /// Reads data from the default storage provider.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string key);

        /// <summary>
        /// Deletes data from the default storage provider.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteAsync(string key);

        /// <summary>
        /// Checks if a key exists in the default storage provider.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Gets all keys in the default storage provider.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        Task<IReadOnlyList<string>> GetAllKeysAsync();

        /// <summary>
        /// Flushes any pending writes to the default storage provider.
        /// </summary>
        /// <returns>True if the flush was successful, false otherwise.</returns>
        Task<bool> FlushAsync();
    }

    /// <summary>
    /// Types of storage providers.
    /// </summary>
    public enum StorageProviderType
    {
        /// <summary>
        /// In-memory storage provider.
        /// </summary>
        Memory,

        /// <summary>
        /// File-based storage provider.
        /// </summary>
        File,

        /// <summary>
        /// File-based storage provider optimized for Occlum LibOS.
        /// </summary>
        OcclumFile,

        /// <summary>
        /// RocksDB storage provider.
        /// </summary>
        RocksDB,

        /// <summary>
        /// LevelDB storage provider.
        /// </summary>
        LevelDB,

        /// <summary>
        /// SQLite storage provider.
        /// </summary>
        Sqlite,

        /// <summary>
        /// Secure storage provider.
        /// </summary>
        Secure
    }
}
