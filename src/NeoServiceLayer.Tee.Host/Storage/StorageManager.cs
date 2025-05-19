using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Manager for storage providers.
    /// </summary>
    public class StorageManager : IStorageManager
    {
        private readonly ILogger<StorageManager> _logger;
        private readonly IStorageFactory _factory;
        private readonly ConcurrentDictionary<string, IStorageProvider> _providers;
        private readonly SemaphoreSlim _semaphore;
        private bool _initialized;
        private bool _disposed;
        private string _defaultProviderName;
        private string _storagePath;

        /// <summary>
        /// Initializes a new instance of the StorageManager class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="factory">The factory to use for creating storage providers.</param>
        public StorageManager(ILogger<StorageManager> logger, IStorageFactory factory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _providers = new ConcurrentDictionary<string, IStorageProvider>();
            _semaphore = new SemaphoreSlim(1, 1);
            _initialized = false;
            _disposed = false;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync(string storagePath)
        {
            if (string.IsNullOrEmpty(storagePath))
            {
                throw new ArgumentException("Storage path cannot be null or empty", nameof(storagePath));
            }

            CheckDisposed();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_initialized)
                    {
                        return true;
                    }

                    _storagePath = storagePath;
                    _initialized = true;

                    _logger.LogInformation("Storage manager initialized with path: {StoragePath}", _storagePath);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize storage manager");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IStorageProvider> CreateProviderAsync(string name, StorageProviderType providerType, object options = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(name));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check if the provider already exists
                    if (_providers.TryGetValue(name, out var existingProvider))
                    {
                        return existingProvider;
                    }

                    // Create the provider
                    var provider = _factory.CreateProvider(providerType, options);

                    // Initialize the provider
                    if (provider is IPersistentStorageProvider persistentProvider)
                    {
                        if (!await persistentProvider.InitializeAsync())
                        {
                            _logger.LogError("Failed to initialize provider {ProviderName}", name);
                            return null;
                        }
                    }

                    // Add the provider to the dictionary
                    _providers[name] = provider;

                    // Set as default if it's the first provider
                    if (_providers.Count == 1)
                    {
                        _defaultProviderName = name;
                    }

                    _logger.LogInformation("Provider {ProviderName} of type {ProviderType} created successfully", name, providerType);
                    return provider;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create provider {ProviderName} of type {ProviderType}", name, providerType);
                return null;
            }
        }

        /// <inheritdoc/>
        public IStorageProvider GetProvider(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(name));
            }

            CheckDisposed();
            CheckInitialized();

            if (_providers.TryGetValue(name, out var provider))
            {
                return provider;
            }

            return null;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, IStorageProvider> GetAllProviders()
        {
            CheckDisposed();
            CheckInitialized();

            return new Dictionary<string, IStorageProvider>(_providers);
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveProviderAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(name));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check if the provider exists
                    if (!_providers.TryGetValue(name, out var provider))
                    {
                        return false;
                    }

                    // Remove the provider
                    if (!_providers.TryRemove(name, out _))
                    {
                        return false;
                    }

                    // Dispose the provider
                    provider.Dispose();

                    // If the default provider was removed, set a new default
                    if (_defaultProviderName == name)
                    {
                        _defaultProviderName = _providers.Count > 0 ? _providers.Keys.First() : null;
                    }

                    _logger.LogInformation("Provider {ProviderName} removed successfully", name);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove provider {ProviderName}", name);
                return false;
            }
        }

        /// <inheritdoc/>
        public IStorageProvider GetDefaultProvider()
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(_defaultProviderName))
            {
                return null;
            }

            return GetProvider(_defaultProviderName);
        }

        /// <inheritdoc/>
        public bool SetDefaultProvider(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(name));
            }

            CheckDisposed();
            CheckInitialized();

            if (!_providers.ContainsKey(name))
            {
                return false;
            }

            _defaultProviderName = name;
            _logger.LogInformation("Default provider set to {ProviderName}", name);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> WriteAsync(string key, byte[] data)
        {
            CheckDisposed();
            CheckInitialized();

            var provider = GetDefaultProvider();
            if (provider == null)
            {
                throw new InvalidOperationException("No default provider is set");
            }

            return await provider.WriteAsync(key, data);
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReadAsync(string key)
        {
            CheckDisposed();
            CheckInitialized();

            var provider = GetDefaultProvider();
            if (provider == null)
            {
                throw new InvalidOperationException("No default provider is set");
            }

            return await provider.ReadAsync(key);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string key)
        {
            CheckDisposed();
            CheckInitialized();

            var provider = GetDefaultProvider();
            if (provider == null)
            {
                throw new InvalidOperationException("No default provider is set");
            }

            return await provider.DeleteAsync(key);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string key)
        {
            CheckDisposed();
            CheckInitialized();

            var provider = GetDefaultProvider();
            if (provider == null)
            {
                throw new InvalidOperationException("No default provider is set");
            }

            return await provider.ExistsAsync(key);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> GetAllKeysAsync()
        {
            CheckDisposed();
            CheckInitialized();

            var provider = GetDefaultProvider();
            if (provider == null)
            {
                throw new InvalidOperationException("No default provider is set");
            }

            return await provider.GetAllKeysAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> FlushAsync()
        {
            CheckDisposed();
            CheckInitialized();

            var provider = GetDefaultProvider();
            if (provider == null)
            {
                throw new InvalidOperationException("No default provider is set");
            }

            return await provider.FlushAsync();
        }

        /// <summary>
        /// Checks if the manager has been disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(StorageManager));
            }
        }

        /// <summary>
        /// Checks if the manager has been initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage manager has not been initialized");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the manager.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose all providers
                    foreach (var provider in _providers.Values)
                    {
                        provider.Dispose();
                    }

                    _providers.Clear();
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the manager.
        /// </summary>
        ~StorageManager()
        {
            Dispose(false);
        }
    }
}
