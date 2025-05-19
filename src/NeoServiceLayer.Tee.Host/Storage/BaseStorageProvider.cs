using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Base abstract class for storage providers that implements common functionality.
    /// </summary>
    public abstract class BaseStorageProvider : IStorageProvider
    {
        /// <summary>
        /// Default chunk size in bytes (1 MB).
        /// </summary>
        protected const int DefaultChunkSize = 1024 * 1024;

        /// <summary>
        /// Logger instance.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Semaphore for synchronizing access to storage.
        /// </summary>
        protected readonly SemaphoreSlim Semaphore;

        /// <summary>
        /// Whether the provider has been disposed.
        /// </summary>
        protected bool Disposed;

        /// <summary>
        /// Initializes a new instance of the BaseStorageProvider class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        protected BaseStorageProvider(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Semaphore = new SemaphoreSlim(1, 1);
            Disposed = false;
        }

        /// <inheritdoc/>
        public abstract Task<bool> InitializeAsync();

        /// <inheritdoc/>
        public virtual async Task<bool> WriteAsync(string key, byte[] data)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await WriteInternalAsync(key, data);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write data for key {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<byte[]> ReadAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await ReadInternalAsync(key);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read data for key {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await DeleteInternalAsync(key);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete data for key {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await ExistsInternalAsync(key);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to check if key {Key} exists", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IReadOnlyList<string>> GetAllKeysAsync()
        {
            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await GetAllKeysInternalAsync();
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get all keys");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> FlushAsync()
        {
            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await FlushInternalAsync();
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to flush storage");
                return false;
            }
        }

        /// <summary>
        /// Internal implementation of writing data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        protected abstract Task<bool> WriteInternalAsync(string key, byte[] data);

        /// <summary>
        /// Internal implementation of reading data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        protected abstract Task<byte[]> ReadInternalAsync(string key);

        /// <summary>
        /// Internal implementation of deleting data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        protected abstract Task<bool> DeleteInternalAsync(string key);

        /// <summary>
        /// Internal implementation of checking if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        protected abstract Task<bool> ExistsInternalAsync(string key);

        /// <summary>
        /// Internal implementation of getting all keys in storage.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        protected abstract Task<IReadOnlyList<string>> GetAllKeysInternalAsync();

        /// <summary>
        /// Internal implementation of flushing any pending writes to storage.
        /// </summary>
        /// <returns>True if the flush was successful, false otherwise.</returns>
        protected abstract Task<bool> FlushInternalAsync();

        /// <summary>
        /// Computes a hash for data.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>The hash as a base64 string.</returns>
        protected string ComputeHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Checks if the provider has been disposed.
        /// </summary>
        protected void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Disposes the provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the provider.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Semaphore.Dispose();
                }

                Disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the provider.
        /// </summary>
        ~BaseStorageProvider()
        {
            Dispose(false);
        }
    }
}
