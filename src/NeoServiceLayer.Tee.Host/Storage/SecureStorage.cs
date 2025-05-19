using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Provides secure storage for sensitive data.
    /// </summary>
    public class SecureStorage : ISecureStorage, IDisposable
    {
        private readonly ILogger<SecureStorage> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly SecureStorageOptions _options;
        private readonly ConcurrentDictionary<string, byte[]> _cache;
        private readonly IPersistentStorageProvider _storageProvider;
        private readonly StorageUtility _storageUtility;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureStorage"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for sealing and unsealing data.</param>
        /// <param name="options">The options for the secure storage.</param>
        /// <param name="storageProvider">The persistent storage provider to use. If null, a file-based provider will be created.</param>
        public SecureStorage(
            ILogger<SecureStorage> logger,
            IOcclumInterface occlumInterface,
            SecureStorageOptions options = null,
            IPersistentStorageProvider storageProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _options = options ?? new SecureStorageOptions();
            _cache = new ConcurrentDictionary<string, byte[]>();
            _semaphore = new SemaphoreSlim(1, 1);
            _disposed = false;
            _initialized = false;

            // Create the storage utility
            _storageUtility = new StorageUtility(
                new Logger<StorageUtility>(new LoggerFactory()),
                _occlumInterface);

            // Use the provided storage provider or create a file-based one
            _storageProvider = storageProvider ?? new OcclumFileStorageProvider(
                new Logger<OcclumFileStorageProvider>(new LoggerFactory()),
                new OcclumFileStorageOptions
                {
                    StorageDirectory = _options.StorageDirectory
                });
        }

        /// <summary>
        /// Initializes the secure storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            CheckDisposed();

            if (_initialized)
            {
                return;
            }

            await _storageProvider.InitializeAsync();
            _initialized = true;
        }

        /// <summary>
        /// Stores a value securely.
        /// </summary>
        /// <param name="key">The key for the value.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StoreAsync(string key, string value)
        {
            CheckDisposed();
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _logger.LogDebug("Storing value for key {Key}", key);

            try
            {
                // Convert the value to bytes
                byte[] valueBytes = Encoding.UTF8.GetBytes(value);

                // Seal the value using the storage utility
                byte[] sealedValue = _storageUtility.Encrypt(valueBytes);

                // Store the sealed value in the cache
                if (_options.EnableCaching)
                {
                    _cache[key] = sealedValue;
                }

                // Store the sealed value in the persistent storage if enabled
                if (_options.EnablePersistence)
                {
                    await _storageProvider.WriteAsync(key, sealedValue);
                }

                _logger.LogDebug("Value stored successfully for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store value for key {Key}", key);
                throw new SecureStorageException($"Failed to store value for key {key}", ex);
            }
        }

        /// <summary>
        /// Retrieves a value securely.
        /// </summary>
        /// <param name="key">The key for the value.</param>
        /// <returns>The retrieved value, or null if the key does not exist.</returns>
        public async Task<string> RetrieveAsync(string key)
        {
            CheckDisposed();
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Retrieving value for key {Key}", key);

            try
            {
                // Try to get the sealed value from the cache
                byte[] sealedValue = null;
                if (_options.EnableCaching && _cache.TryGetValue(key, out sealedValue))
                {
                    _logger.LogDebug("Value found in cache for key {Key}", key);
                }
                else if (_options.EnablePersistence)
                {
                    // Try to get the sealed value from persistent storage
                    sealedValue = await _storageProvider.ReadAsync(key);
                    if (sealedValue != null)
                    {
                        _logger.LogDebug("Value found in persistent storage for key {Key}", key);

                        // Store the sealed value in the cache
                        if (_options.EnableCaching)
                        {
                            _cache[key] = sealedValue;
                        }
                    }
                }

                if (sealedValue == null)
                {
                    _logger.LogDebug("Value not found for key {Key}", key);
                    return null;
                }

                // Unseal the value using the storage utility
                byte[] valueBytes = _storageUtility.Decrypt(sealedValue);

                // Convert the value to a string
                string value = Encoding.UTF8.GetString(valueBytes);

                _logger.LogDebug("Value retrieved successfully for key {Key}", key);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve value for key {Key}", key);
                throw new SecureStorageException($"Failed to retrieve value for key {key}", ex);
            }
        }

        /// <summary>
        /// Removes a value securely.
        /// </summary>
        /// <param name="key">The key for the value.</param>
        /// <returns>True if the value was removed, false if the key does not exist.</returns>
        public async Task<bool> RemoveAsync(string key)
        {
            CheckDisposed();
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Removing value for key {Key}", key);

            try
            {
                bool removed = false;

                // Remove the value from the cache
                if (_options.EnableCaching)
                {
                    removed = _cache.TryRemove(key, out _);
                }

                // Remove the value from persistent storage if enabled
                if (_options.EnablePersistence)
                {
                    bool removedFromStorage = await _storageProvider.DeleteAsync(key);
                    removed = removed || removedFromStorage;
                }

                _logger.LogDebug("Value {RemovedStatus} for key {Key}", removed ? "removed successfully" : "not found", key);
                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove value for key {Key}", key);
                throw new SecureStorageException($"Failed to remove value for key {key}", ex);
            }
        }

        /// <summary>
        /// Checks if a key exists.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public async Task<bool> ExistsAsync(string key)
        {
            CheckDisposed();
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Checking if key {Key} exists", key);

            try
            {
                // Check if the key exists in the cache
                if (_options.EnableCaching && _cache.ContainsKey(key))
                {
                    _logger.LogDebug("Key {Key} found in cache", key);
                    return true;
                }

                // Check if the key exists in persistent storage if enabled
                if (_options.EnablePersistence)
                {
                    bool existsInStorage = await _storageProvider.ExistsAsync(key);
                    _logger.LogDebug("Key {Key} {ExistsStatus} in persistent storage", key, existsInStorage ? "found" : "not found");
                    return existsInStorage;
                }

                _logger.LogDebug("Key {Key} not found", key);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if key {Key} exists", key);
                throw new SecureStorageException($"Failed to check if key {key} exists", ex);
            }
        }

        /// <summary>
        /// Gets all keys.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        public async Task<IReadOnlyList<string>> GetAllKeysAsync()
        {
            CheckDisposed();
            EnsureInitialized();

            _logger.LogDebug("Getting all keys");

            try
            {
                var keys = new HashSet<string>();

                // Add keys from the cache
                if (_options.EnableCaching)
                {
                    foreach (var key in _cache.Keys)
                    {
                        keys.Add(key);
                    }
                }

                // Add keys from persistent storage if enabled
                if (_options.EnablePersistence)
                {
                    var keysFromStorage = await _storageProvider.GetAllKeysAsync();
                    foreach (var key in keysFromStorage)
                    {
                        keys.Add(key);
                    }
                }

                _logger.LogDebug("Found {KeyCount} keys", keys.Count);
                return new List<string>(keys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all keys");
                throw new SecureStorageException("Failed to get all keys", ex);
            }
        }

        /// <summary>
        /// Clears all values.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ClearAsync()
        {
            CheckDisposed();
            EnsureInitialized();

            _logger.LogDebug("Clearing all values");

            try
            {
                // Clear the cache
                if (_options.EnableCaching)
                {
                    _cache.Clear();
                }

                // Clear persistent storage if enabled
                if (_options.EnablePersistence)
                {
                    var keys = await _storageProvider.GetAllKeysAsync();
                    foreach (var key in keys)
                    {
                        await _storageProvider.DeleteAsync(key);
                    }
                }

                _logger.LogDebug("All values cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear all values");
                throw new SecureStorageException("Failed to clear all values", ex);
            }
        }

        /// <summary>
        /// Disposes the secure storage.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the secure storage.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _semaphore.Dispose();

                    // Dispose the storage provider if it's disposable
                    if (_storageProvider != null)
                    {
                        _storageProvider.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Checks if the secure storage has been disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecureStorage));
            }
        }

        /// <summary>
        /// Ensures that the secure storage has been initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Secure storage has not been initialized. Call InitializeAsync first.");
            }
        }
    }
}
