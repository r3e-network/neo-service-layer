using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// In-memory storage provider.
    /// </summary>
    public class MemoryStorageProvider : BaseStorageProvider
    {
        private readonly ConcurrentDictionary<string, byte[]> _storage;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the MemoryStorageProvider class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        public MemoryStorageProvider(ILogger<MemoryStorageProvider> logger)
            : base(logger)
        {
            _storage = new ConcurrentDictionary<string, byte[]>();
            _initialized = false;
        }

        /// <inheritdoc/>
        public override async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    if (_initialized)
                    {
                        return true;
                    }

                    _initialized = true;
                    Logger.LogInformation("Memory storage provider initialized successfully");
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize memory storage provider");
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> WriteInternalAsync(string key, byte[] data)
        {
            // Clone the data to prevent external modification
            var clonedData = new byte[data.Length];
            Array.Copy(data, clonedData, data.Length);

            // Store the data
            _storage[key] = clonedData;
            Logger.LogDebug("Data written successfully for key {Key}", key);
            return true;
        }

        /// <inheritdoc/>
        protected override async Task<byte[]> ReadInternalAsync(string key)
        {
            if (!_storage.TryGetValue(key, out var data))
            {
                Logger.LogDebug("Key {Key} not found", key);
                return null;
            }

            // Clone the data to prevent external modification
            var clonedData = new byte[data.Length];
            Array.Copy(data, clonedData, data.Length);

            Logger.LogDebug("Data read successfully for key {Key}", key);
            return clonedData;
        }

        /// <inheritdoc/>
        protected override async Task<bool> DeleteInternalAsync(string key)
        {
            if (!_storage.TryRemove(key, out _))
            {
                Logger.LogDebug("Key {Key} not found for deletion", key);
                return false;
            }

            Logger.LogDebug("Data deleted successfully for key {Key}", key);
            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> ExistsInternalAsync(string key)
        {
            var exists = _storage.ContainsKey(key);
            Logger.LogDebug("Key {Key} exists: {Exists}", key, exists);
            return exists;
        }

        /// <inheritdoc/>
        protected override async Task<IReadOnlyList<string>> GetAllKeysInternalAsync()
        {
            var keys = _storage.Keys.ToList();
            Logger.LogDebug("Retrieved {Count} keys", keys.Count);
            return keys;
        }

        /// <inheritdoc/>
        protected override async Task<bool> FlushInternalAsync()
        {
            // Nothing to flush for in-memory storage
            Logger.LogDebug("Flush operation is a no-op for memory storage");
            return true;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    _storage.Clear();
                }

                base.Dispose(disposing);
            }
        }
    }
}
