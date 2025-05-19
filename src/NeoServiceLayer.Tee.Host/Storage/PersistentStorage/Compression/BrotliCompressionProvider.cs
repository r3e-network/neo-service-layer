using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression
{
    /// <summary>
    /// A compression provider using Brotli compression.
    /// </summary>
    public class BrotliCompressionProvider : ICompressionProvider
    {
        private readonly ILogger<BrotliCompressionProvider> _logger;
        private readonly BrotliCompressionOptions _options;
        private readonly SemaphoreSlim _semaphore;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrotliCompressionProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="options">The options for the compression provider.</param>
        public BrotliCompressionProvider(ILogger<BrotliCompressionProvider> logger, BrotliCompressionOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new BrotliCompressionOptions();
            _semaphore = new SemaphoreSlim(1, 1);
            _initialized = false;
            _disposed = false;
        }

        /// <summary>
        /// Gets the name of the compression provider.
        /// </summary>
        public string Name => "Brotli";

        /// <summary>
        /// Gets the description of the compression provider.
        /// </summary>
        public string Description => "Brotli compression provider.";

        /// <summary>
        /// Initializes the compression provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task InitializeAsync()
        {
            CheckDisposed();

            if (_initialized)
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("Initializing Brotli compression provider");
            _initialized = true;
            _logger.LogInformation("Brotli compression provider initialized successfully");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Compresses data.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed data.</returns>
        public async Task<byte[]> CompressAsync(byte[] data)
        {
            CheckDisposed();
            EnsureInitialized();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Compressing data ({Size} bytes)", data.Length);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var brotliStream = new BrotliStream(memoryStream, CompressionMode.Compress, true))
                        {
                            brotliStream.CompressionLevel = _options.CompressionLevel;
                            await brotliStream.WriteAsync(data, 0, data.Length);
                        }

                        byte[] compressedData = memoryStream.ToArray();
                        _logger.LogDebug("Data compressed successfully ({CompressedSize} bytes)", compressedData.Length);
                        return compressedData;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compress data");
                throw new StorageException("Failed to compress data", ex);
            }
        }

        /// <summary>
        /// Decompresses data.
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress.</param>
        /// <returns>The decompressed data.</returns>
        public async Task<byte[]> DecompressAsync(byte[] compressedData)
        {
            CheckDisposed();
            EnsureInitialized();

            if (compressedData == null)
            {
                throw new ArgumentNullException(nameof(compressedData));
            }

            _logger.LogDebug("Decompressing data ({Size} bytes)", compressedData.Length);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    using (var compressedStream = new MemoryStream(compressedData))
                    using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
                    using (var resultStream = new MemoryStream())
                    {
                        await brotliStream.CopyToAsync(resultStream);
                        byte[] decompressedData = resultStream.ToArray();
                        _logger.LogDebug("Data decompressed successfully ({DecompressedSize} bytes)", decompressedData.Length);
                        return decompressedData;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decompress data");
                throw new StorageException("Failed to decompress data", ex);
            }
        }

        /// <summary>
        /// Gets information about the compression provider.
        /// </summary>
        /// <returns>Information about the compression provider.</returns>
        public CompressionProviderInfo GetProviderInfo()
        {
            CheckDisposed();
            EnsureInitialized();

            return new CompressionProviderInfo
            {
                Name = Name,
                Description = Description,
                Algorithm = "Brotli",
                CompressionLevel = _options.CompressionLevel,
                SupportsStreaming = true,
                AdditionalProperties = new Dictionary<string, string>
                {
                    { "CompressionLevel", _options.CompressionLevel.ToString() }
                }
            };
        }

        /// <summary>
        /// Disposes the compression provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the compression provider.
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
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Checks if the compression provider has been disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BrotliCompressionProvider));
            }
        }

        /// <summary>
        /// Ensures that the compression provider has been initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Compression provider has not been initialized. Call InitializeAsync first.");
            }
        }
    }
}
