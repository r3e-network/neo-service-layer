using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Storage;

namespace NeoServiceLayer.Core.Services
{
    /// <summary>
    /// Interface for persistent storage service.
    /// </summary>
    public interface IPersistentStorageService
    {
        /// <summary>
        /// Gets the storage provider.
        /// </summary>
        IPersistentStorageProvider Provider { get; }

        /// <summary>
        /// Gets a value indicating whether the storage service is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the storage service.
        /// </summary>
        /// <param name="options">The storage options.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync(PersistentStorageOptions options);

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The data as a byte array, or null if the key does not exist.</returns>
        Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads data from storage and deserializes it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="key">The key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized object, or default if the key does not exist.</returns>
        Task<T> ReadJsonAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key to write.</param>
        /// <param name="data">The data to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes an object to JSON and writes it to storage.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="key">The key to write.</param>
        /// <param name="value">The object to serialize and write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WriteJsonAsync<T>(string key, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the key was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all keys in storage with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to filter keys by.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of keys.</returns>
        Task<IEnumerable<string>> ListKeysAsync(string prefix = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the size of the data for the specified key.
        /// </summary>
        /// <param name="key">The key to get the size for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        Task<long> GetSizeAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>A transaction object.</returns>
        /// <exception cref="NotSupportedException">Thrown if transactions are not supported.</exception>
        IStorageTransaction BeginTransaction();

        /// <summary>
        /// Opens a stream for reading from storage.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A stream for reading, or null if the key does not exist.</returns>
        Task<Stream> OpenReadStreamAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a stream for writing to storage.
        /// </summary>
        /// <param name="key">The key to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A stream for writing.</returns>
        Task<Stream> OpenWriteStreamAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes any pending changes to storage.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}
