using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Manager for persistent storage providers.
    /// </summary>
    public class PersistentStorageManager : IDisposable
    {
        private readonly ILogger<PersistentStorageManager> _logger;
        private readonly PersistentStorageFactory _factory;
        private readonly ConcurrentDictionary<string, IPersistentStorageProvider> _providers;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentStorageManager"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="factory">The factory to use for creating storage providers.</param>
        public PersistentStorageManager(ILogger<PersistentStorageManager> logger, PersistentStorageFactory factory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _providers = new ConcurrentDictionary<string, IPersistentStorageProvider>();
            _semaphore = new SemaphoreSlim(1, 1);
            _disposed = false;
        }

        /// <summary>
        /// Gets a storage provider.
        /// </summary>
        /// <param name="name">The name of the provider.</param>
        /// <returns>The storage provider, or null if not found.</returns>
        public IPersistentStorageProvider GetProvider(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            }

            if (_providers.TryGetValue(name, out var provider))
            {
                return provider;
            }

            return null;
        }

        /// <summary>
        /// Creates a storage provider.
        /// </summary>
        /// <param name="name">The name of the provider.</param>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        public async Task<IPersistentStorageProvider> CreateProviderAsync(string name, PersistentStorageProviderType providerType, object options = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            }

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
                await provider.InitializeAsync();

                // Add the provider to the dictionary
                _providers[name] = provider;

                return provider;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Removes a storage provider.
        /// </summary>
        /// <param name="name">The name of the provider.</param>
        /// <returns>True if the provider was removed, false if not found.</returns>
        public async Task<bool> RemoveProviderAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            }

            await _semaphore.WaitAsync();
            try
            {
                // Check if the provider exists
                if (!_providers.TryGetValue(name, out var provider))
                {
                    return false;
                }

                // Remove the provider from the dictionary
                if (!_providers.TryRemove(name, out _))
                {
                    return false;
                }

                // Dispose the provider
                provider.Dispose();

                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets all storage providers.
        /// </summary>
        /// <returns>A dictionary of all providers.</returns>
        public IReadOnlyDictionary<string, IPersistentStorageProvider> GetAllProviders()
        {
            return new Dictionary<string, IPersistentStorageProvider>(_providers);
        }

        /// <summary>
        /// Disposes the storage manager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the storage manager.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
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
    }
}
