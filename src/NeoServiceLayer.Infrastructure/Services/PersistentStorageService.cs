using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Services;
using NeoServiceLayer.Core.Storage;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the persistent storage service.
    /// </summary>
    public class PersistentStorageService : IPersistentStorageService, IDisposable
    {
        private readonly IPersistentStorageProvider _provider;
        private readonly ILogger<PersistentStorageService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the PersistentStorageService class.
        /// </summary>
        /// <param name="provider">The storage provider.</param>
        /// <param name="logger">The logger.</param>
        public PersistentStorageService(IPersistentStorageProvider provider, ILogger<PersistentStorageService> logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc/>
        public IPersistentStorageProvider Provider => _provider;

        /// <inheritdoc/>
        public bool IsInitialized => _isInitialized;

        /// <inheritdoc/>
        public async Task InitializeAsync(PersistentStorageOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            await _provider.InitializeAsync(options);
            _isInitialized = true;
            _logger.LogInformation("Persistent storage service initialized with provider {ProviderName}", _provider.Name);
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return await _provider.ReadAsync(key, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<T> ReadJsonAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            var data = await _provider.ReadAsync(key, cancellationToken);
            if (data == null)
            {
                return default;
            }

            try
            {
                var json = Encoding.UTF8.GetString(data);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing JSON for key {Key}", key);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task WriteAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            await _provider.WriteAsync(key, data, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WriteJsonAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var data = Encoding.UTF8.GetBytes(json);
                await _provider.WriteAsync(key, data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serializing JSON for key {Key}", key);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return await _provider.DeleteAsync(key, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return await _provider.ExistsAsync(key, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> ListKeysAsync(string prefix = "", CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return await _provider.ListKeysAsync(prefix, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<long> GetSizeAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return await _provider.GetSizeAsync(key, cancellationToken);
        }

        /// <inheritdoc/>
        public IStorageTransaction BeginTransaction()
        {
            EnsureInitialized();
            return _provider.BeginTransaction();
        }

        /// <inheritdoc/>
        public async Task<Stream> OpenReadStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return await _provider.OpenReadStreamAsync(key, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Stream> OpenWriteStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return await _provider.OpenWriteStreamAsync(key, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            await _provider.FlushAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Storage service is not initialized");
            }
        }
    }
}
