using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Occlum file system implementation of persistent storage.
    /// </summary>
    public class OcclumFileStorageProvider : IPersistentStorageService, IDisposable
    {
        private readonly ILogger<OcclumFileStorageProvider> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();
        private readonly Dictionary<string, DateTime> _cacheLastAccess = new Dictionary<string, DateTime>();
        private long _cacheSize = 0;
        private Timer _autoFlushTimer;
        private bool _initialized = false;
        private PersistentStorageOptions _options;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumFileStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="occlumInterface">The Occlum interface.</param>
        public OcclumFileStorageProvider(
            ILogger<OcclumFileStorageProvider> logger,
            IOcclumInterface occlumInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
        }

        /// <summary>
        /// Initializes the storage service.
        /// </summary>
        /// <param name="options">The storage options.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync(PersistentStorageOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.StoragePath))
            {
                throw new ArgumentException("Storage path cannot be null or empty.", nameof(options.StoragePath));
            }

            if (options.EnableEncryption && (options.EncryptionKey == null || options.EncryptionKey.Length != 32))
            {
                throw new ArgumentException("Encryption key must be 32 bytes for AES-256.", nameof(options.EncryptionKey));
            }

            await _semaphore.WaitAsync();
            try
            {
                _options = options;

                // Ensure the storage directory exists
                if (!Directory.Exists(options.StoragePath))
                {
                    if (options.CreateIfNotExists)
                    {
                        Directory.CreateDirectory(options.StoragePath);
                        _logger.LogInformation("Created storage directory: {StoragePath}", options.StoragePath);
                    }
                    else
                    {
                        throw new DirectoryNotFoundException($"Storage directory does not exist: {options.StoragePath}");
                    }
                }

                // Set up auto-flush timer if enabled
                if (options.EnableAutoFlush)
                {
                    _autoFlushTimer = new Timer(
                        async _ => await FlushAsync(),
                        null,
                        options.AutoFlushIntervalMs,
                        options.AutoFlushIntervalMs);
                    _logger.LogInformation("Auto-flush enabled with interval: {AutoFlushIntervalMs} ms", options.AutoFlushIntervalMs);
                }

                _initialized = true;
                _logger.LogInformation("Storage service initialized with path: {StoragePath}", options.StoragePath);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>The data read from storage, or null if the key does not exist.</returns>
        public async Task<byte[]> ReadAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureInitialized();

            await _semaphore.WaitAsync();
            try
            {
                // Try to get from cache first
                if (_options.EnableCaching && _cache.TryGetValue(key, out var cachedData))
                {
                    _cacheLastAccess[key] = DateTime.UtcNow;
                    _logger.LogDebug("Read {ByteCount} bytes from cache for key: {Key}", cachedData.Length, key);
                    return cachedData;
                }

                // Read from file
                string filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("Key not found: {Key}", key);
                    return null;
                }

                byte[] data = await File.ReadAllBytesAsync(filePath);

                // Decrypt if encryption is enabled
                if (_options.EnableEncryption)
                {
                    data = DecryptData(data);
                }

                // Decompress if compression is enabled
                if (_options.EnableCompression)
                {
                    data = DecompressData(data);
                }

                // Add to cache if caching is enabled
                if (_options.EnableCaching)
                {
                    AddToCache(key, data);
                }

                _logger.LogDebug("Read {ByteCount} bytes from storage for key: {Key}", data.Length, key);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from storage for key: {Key}", key);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WriteAsync(string key, byte[] data)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            EnsureInitialized();

            await _semaphore.WaitAsync();
            try
            {
                byte[] processedData = data;

                // Compress if compression is enabled
                if (_options.EnableCompression)
                {
                    processedData = CompressData(processedData);
                }

                // Encrypt if encryption is enabled
                if (_options.EnableEncryption)
                {
                    processedData = EncryptData(processedData);
                }

                // Write to file
                string filePath = GetFilePath(key);
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(filePath, processedData);

                // Add to cache if caching is enabled
                if (_options.EnableCaching)
                {
                    AddToCache(key, data);
                }

                _logger.LogDebug("Wrote {ByteCount} bytes to storage for key: {Key}", data.Length, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to storage for key: {Key}", key);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureInitialized();

            await _semaphore.WaitAsync();
            try
            {
                // Remove from cache if caching is enabled
                if (_options.EnableCaching && _cache.TryGetValue(key, out var cachedData))
                {
                    _cacheSize -= cachedData.Length;
                    _cache.Remove(key);
                    _cacheLastAccess.Remove(key);
                }

                // Delete the file
                string filePath = GetFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug("Deleted key: {Key}", key);
                }
                else
                {
                    _logger.LogDebug("Key not found for deletion: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting from storage for key: {Key}", key);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureInitialized();

            await _semaphore.WaitAsync();
            try
            {
                // Check cache first
                if (_options.EnableCaching && _cache.ContainsKey(key))
                {
                    return true;
                }

                // Check file system
                string filePath = GetFilePath(key);
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists: {Key}", key);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Lists all keys in storage with a specified prefix.
        /// </summary>
        /// <param name="prefix">The key prefix.</param>
        /// <returns>A list of keys.</returns>
        public async Task<List<string>> ListKeysAsync(string prefix)
        {
            if (prefix == null)
            {
                prefix = string.Empty;
            }

            EnsureInitialized();

            await _semaphore.WaitAsync();
            try
            {
                var keys = new List<string>();
                string prefixPath = GetFilePath(prefix);
                string prefixDirectory = Path.GetDirectoryName(prefixPath);
                
                if (!Directory.Exists(prefixDirectory))
                {
                    _logger.LogDebug("Prefix directory does not exist: {PrefixDirectory}", prefixDirectory);
                    return keys;
                }

                string prefixFileName = Path.GetFileName(prefixPath);
                var files = Directory.EnumerateFiles(prefixDirectory, $"{prefixFileName}*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string relativePath = file.Substring(_options.StoragePath.Length + 1);
                    relativePath = relativePath.Replace('\\', '/');
                    keys.Add(relativePath);
                }

                _logger.LogDebug("Listed {KeyCount} keys with prefix: {Prefix}", keys.Count, prefix);
                return keys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing keys with prefix: {Prefix}", prefix);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the size of the data for a specified key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        public async Task<long> GetSizeAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureInitialized();

            await _semaphore.WaitAsync();
            try
            {
                // Check cache first
                if (_options.EnableCaching && _cache.TryGetValue(key, out var cachedData))
                {
                    return cachedData.Length;
                }

                // Check file system
                string filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("Key not found: {Key}", key);
                    return -1;
                }

                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting size for key: {Key}", key);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Opens a stream for reading from storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A stream for reading from storage, or null if the key does not exist.</returns>
        public async Task<Stream> OpenReadStreamAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureInitialized();

            await _semaphore.WaitAsync();
            try
            {
                // For simplicity, we'll just read the entire file and return a memory stream
                byte[] data = await ReadAsync(key);
                if (data == null)
                {
                    _logger.LogDebug("Key not found: {Key}", key);
                    return null;
                }

                _logger.LogDebug("Opened read stream for key: {Key}", key);
                return new MemoryStream(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening read stream for key: {Key}", key);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Opens a stream for writing to storage.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A stream for writing to storage.</returns>
        public async Task<Stream> OpenWriteStreamAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureInitialized();

            // For simplicity, we'll use a callback memory stream that writes to storage when closed
            var stream = new CallbackMemoryStream(
                async data => await WriteAsync(key, data),
                () => _logger.LogDebug("Write stream closed for key: {Key}", key));

            _logger.LogDebug("Opened write stream for key: {Key}", key);
            return stream;
        }

        /// <summary>
        /// Flushes any pending changes to storage.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FlushAsync()
        {
            if (!_initialized)
            {
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                // Nothing to do in this implementation since we write directly to files
                _logger.LogDebug("Flushed storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing storage");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Disposes resources used by the storage provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by the storage provider.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _autoFlushTimer?.Dispose();
                _semaphore?.Dispose();
            }

            _disposed = true;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage provider is not initialized. Call InitializeAsync first.");
            }
        }

        private string GetFilePath(string key)
        {
            // Replace characters that are not allowed in file paths
            string safeKey = key.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_options.StoragePath, safeKey);
        }

        private void AddToCache(string key, byte[] data)
        {
            // If cache is enabled, add the data to the cache
            if (!_options.EnableCaching)
            {
                return;
            }

            // Remove old items if cache is full
            while (_cacheSize + data.Length > _options.CacheSizeBytes && _cache.Count > 0)
            {
                // Remove the least recently accessed item
                string oldestKey = _cacheLastAccess.OrderBy(kv => kv.Value).First().Key;
                _cacheSize -= _cache[oldestKey].Length;
                _cache.Remove(oldestKey);
                _cacheLastAccess.Remove(oldestKey);
                _logger.LogDebug("Removed {Key} from cache (cache full)", oldestKey);
            }

            // Add new item to cache
            if (_cache.TryGetValue(key, out var existingData))
            {
                _cacheSize -= existingData.Length;
            }

            _cache[key] = data;
            _cacheLastAccess[key] = DateTime.UtcNow;
            _cacheSize += data.Length;
            _logger.LogDebug("Added {Key} to cache ({DataSize} bytes)", key, data.Length);
        }

        private byte[] CompressData(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(
                    memoryStream,
                    (CompressionLevel)_options.CompressionLevel,
                    leaveOpen: true))
                {
                    gzipStream.Write(data, 0, data.Length);
                }

                return memoryStream.ToArray();
            }
        }

        private byte[] DecompressData(byte[] compressedData)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        private byte[] EncryptData(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _options.EncryptionKey;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var memoryStream = new MemoryStream())
                {
                    // Write the IV to the beginning of the stream
                    memoryStream.Write(aes.IV, 0, aes.IV.Length);

                    using (var cryptoStream = new CryptoStream(
                        memoryStream,
                        encryptor,
                        CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _options.EncryptionKey;

                // Get the IV from the beginning of the encrypted data
                int ivSize = aes.BlockSize / 8;
                byte[] iv = new byte[ivSize];
                Array.Copy(encryptedData, 0, iv, 0, ivSize);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var encryptedStream = new MemoryStream(
                    encryptedData,
                    ivSize,
                    encryptedData.Length - ivSize))
                using (var cryptoStream = new CryptoStream(
                    encryptedStream,
                    decryptor,
                    CryptoStreamMode.Read))
                using (var resultStream = new MemoryStream())
                {
                    cryptoStream.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
        }

        /// <summary>
        /// A memory stream that calls a callback with its data when closed.
        /// </summary>
        private class CallbackMemoryStream : MemoryStream
        {
            private readonly Func<byte[], Task> _onClose;
            private readonly Action _onDispose;
            private bool _isClosed;

            /// <summary>
            /// Initializes a new instance of the <see cref="CallbackMemoryStream"/> class.
            /// </summary>
            /// <param name="onClose">The callback to execute when the stream is closed.</param>
            /// <param name="onDispose">The callback to execute when the stream is disposed.</param>
            public CallbackMemoryStream(Func<byte[], Task> onClose, Action onDispose)
            {
                _onClose = onClose;
                _onDispose = onDispose;
                _isClosed = false;
            }

            /// <summary>
            /// Closes the stream and calls the callback.
            /// </summary>
            public override void Close()
            {
                if (!_isClosed)
                {
                    byte[] data = ToArray();
                    base.Close();
                    _onClose(data).GetAwaiter().GetResult();
                    _isClosed = true;
                }
                else
                {
                    base.Close();
                }
            }

            /// <summary>
            /// Disposes resources used by the stream and calls the callback.
            /// </summary>
            /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
            protected override void Dispose(bool disposing)
            {
                if (disposing && !_isClosed)
                {
                    Close();
                }

                base.Dispose(disposing);
                _onDispose?.Invoke();
            }
        }
    }
} 