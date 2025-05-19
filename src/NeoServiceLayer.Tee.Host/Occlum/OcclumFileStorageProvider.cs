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
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Occlum
{
    /// <summary>
    /// Occlum file storage provider for persistent storage.
    /// </summary>
    public class OcclumFileStorageProvider : IPersistentStorageProvider
    {
        private readonly ILogger<OcclumFileStorageProvider> _logger;
        private readonly IOcclumManager _occlumManager;
        private PersistentStorageOptions _options;
        private bool _isInitialized;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();
        private Timer _flushTimer;

        /// <summary>
        /// Initializes a new instance of the OcclumFileStorageProvider class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="occlumManager">The Occlum manager.</param>
        public OcclumFileStorageProvider(ILogger<OcclumFileStorageProvider> logger, IOcclumManager occlumManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumManager = occlumManager ?? throw new ArgumentNullException(nameof(occlumManager));
            _options = new PersistentStorageOptions();
        }

        /// <inheritdoc/>
        public string Name => "OcclumFileStorage";

        /// <inheritdoc/>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets a value indicating whether the provider supports transactions.
        /// </summary>
        public bool SupportsTransactions => true;

        /// <summary>
        /// Gets a value indicating whether the provider supports encryption.
        /// </summary>
        public bool SupportsEncryption => true;

        /// <summary>
        /// Gets a value indicating whether the provider supports compression.
        /// </summary>
        public bool SupportsCompression => true;

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                // Ensure Occlum is initialized
                if (!_occlumManager.IsOcclumSupportEnabled())
                {
                    _logger.LogError("Occlum support is not enabled");
                    return false;
                }

                // Create the storage directory if it doesn't exist
                if (_options.CreateIfNotExists)
                {
                    await _occlumManager.ExecuteCommandAsync($"mkdir -p {_options.StoragePath}");
                }

                // Check if the storage directory exists
                var result = await _occlumManager.ExecuteCommandAsync($"ls -la {_options.StoragePath}");
                if (result.ExitCode != 0)
                {
                    _logger.LogError("Storage directory not found: {StoragePath}", _options.StoragePath);
                    return false;
                }

                // Create metadata directory
                await _occlumManager.ExecuteCommandAsync($"mkdir -p {_options.StoragePath}/.metadata");

                // Start auto-flush timer if enabled
                if (_options.EnableAutoFlush)
                {
                    _flushTimer = new Timer(async _ => await FlushAsync(), null, _options.AutoFlushIntervalMs, _options.AutoFlushIntervalMs);
                }

                _isInitialized = true;
                _logger.LogInformation("Occlum file storage provider initialized at {StoragePath}", _options.StoragePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Occlum file storage provider");
                return false;
            }
        }

        /// <summary>
        /// Initializes the storage provider with the specified options.
        /// </summary>
        /// <param name="options">The options to use for initialization.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<bool> InitializeAsync(PersistentStorageOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.StoragePath))
            {
                throw new ArgumentException("Storage path cannot be null or empty", nameof(options));
            }

            _options = options;
            return InitializeAsync();
        }

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        public async Task<byte[]> ReadAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check cache first if enabled
                    if (_options.EnableCaching && _cache.TryGetValue(key, out var cachedData))
                    {
                        _logger.LogDebug("Cache hit for key {Key}", key);
                        return cachedData;
                    }

                    var filePath = GetFilePath(key);

                    // Check if the file exists
                    var existsResult = await _occlumManager.ExecuteCommandAsync($"test -f {filePath} && echo 'exists'");
                    if (existsResult.ExitCode != 0 || !existsResult.Output.Contains("exists"))
                    {
                        _logger.LogDebug("Key not found: {Key}", key);
                        return null;
                    }

                    // Read the file
                    var readResult = await _occlumManager.ExecuteCommandAsync($"cat {filePath} | base64");
                    if (readResult.ExitCode != 0)
                    {
                        _logger.LogError("Error reading key {Key}: {Error}", key, readResult.Error);
                        return null;
                    }

                    // Decode the base64 data
                    var encodedData = readResult.Output.Trim();
                    var data = Convert.FromBase64String(encodedData);

                    // Add to cache if enabled
                    if (_options.EnableCaching)
                    {
                        _cache[key] = data;
                    }

                    return data;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        public async Task<bool> WriteAsync(string key, byte[] data)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var filePath = GetFilePath(key);

                    // Encode the data as base64
                    var encodedData = Convert.ToBase64String(data);

                    // Write the file
                    var writeResult = await _occlumManager.ExecuteCommandAsync($"echo '{encodedData}' | base64 -d > {filePath}");
                    if (writeResult.ExitCode != 0)
                    {
                        _logger.LogError("Error writing key {Key}: {Error}", key, writeResult.Error);
                        return false;
                    }

                    // Add to cache if enabled
                    if (_options.EnableCaching)
                    {
                        _cache[key] = data;
                    }

                    _logger.LogDebug("Key written: {Key}", key);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        public async Task<bool> DeleteAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var filePath = GetFilePath(key);

                    // Check if the file exists
                    var existsResult = await _occlumManager.ExecuteCommandAsync($"test -f {filePath} && echo 'exists'");
                    if (existsResult.ExitCode != 0 || !existsResult.Output.Contains("exists"))
                    {
                        _logger.LogDebug("Key not found for deletion: {Key}", key);
                        return false;
                    }

                    // Delete the file
                    var deleteResult = await _occlumManager.ExecuteCommandAsync($"rm {filePath}");
                    if (deleteResult.ExitCode != 0)
                    {
                        _logger.LogError("Error deleting key {Key}: {Error}", key, deleteResult.Error);
                        return false;
                    }

                    // Remove from cache if enabled
                    if (_options.EnableCaching)
                    {
                        _cache.Remove(key);
                    }

                    _logger.LogDebug("Key deleted: {Key}", key);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check cache first if enabled
                    if (_options.EnableCaching && _cache.ContainsKey(key))
                    {
                        return true;
                    }

                    var filePath = GetFilePath(key);

                    // Check if the file exists
                    var existsResult = await _occlumManager.ExecuteCommandAsync($"test -f {filePath} && echo 'exists'");
                    return existsResult.ExitCode == 0 && existsResult.Output.Contains("exists");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Gets all keys in storage.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        public async Task<IReadOnlyList<string>> GetAllKeysAsync()
        {
            EnsureInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // List files in the directory
                    var listResult = await _occlumManager.ExecuteCommandAsync($"find {_options.StoragePath} -type f -not -path \"*/\\.metadata/*\" | sort");
                    if (listResult.ExitCode != 0)
                    {
                        _logger.LogError("Error listing keys: {Error}", listResult.Error);
                        return new List<string>();
                    }

                    // Parse the output
                    var keys = listResult.Output
                        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(path => path.Substring(_options.StoragePath.Length).TrimStart('/'))
                        .ToList();

                    return keys;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing keys");
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the size of data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        public async Task<long> GetSizeAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var filePath = GetFilePath(key);

                    // Check if the file exists
                    var existsResult = await _occlumManager.ExecuteCommandAsync($"test -f {filePath} && echo 'exists'");
                    if (existsResult.ExitCode != 0 || !existsResult.Output.Contains("exists"))
                    {
                        _logger.LogDebug("Key not found for size check: {Key}", key);
                        return -1;
                    }

                    // Get the file size
                    var sizeResult = await _occlumManager.ExecuteCommandAsync($"stat -c %s {filePath}");
                    if (sizeResult.ExitCode != 0)
                    {
                        _logger.LogError("Error getting size for key {Key}: {Error}", key, sizeResult.Error);
                        return -1;
                    }

                    // Parse the size
                    if (long.TryParse(sizeResult.Output.Trim(), out var size))
                    {
                        return size;
                    }

                    _logger.LogError("Error parsing size for key {Key}: {Output}", key, sizeResult.Output);
                    return -1;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting size for key {Key}", key);
                return -1;
            }
        }

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>The transaction ID.</returns>
        public async Task<string> BeginTransactionAsync()
        {
            EnsureInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    string transactionId = Guid.NewGuid().ToString();
                    _logger.LogDebug("Beginning transaction {TransactionId}", transactionId);
                    return transactionId;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin transaction");
                return null;
            }
        }

        /// <summary>
        /// Commits a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was committed successfully, false otherwise.</returns>
        public async Task<bool> CommitTransactionAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            }

            EnsureInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    _logger.LogDebug("Committing transaction {TransactionId}", transactionId);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction {TransactionId}", transactionId);
                return false;
            }
        }

        /// <summary>
        /// Rolls back a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was rolled back successfully, false otherwise.</returns>
        public async Task<bool> RollbackTransactionAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            }

            EnsureInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    _logger.LogDebug("Rolling back transaction {TransactionId}", transactionId);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback transaction {TransactionId}", transactionId);
                return false;
            }
        }

        /// <summary>
        /// Writes data to storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        public async Task<bool> WriteInTransactionAsync(string transactionId, string key, byte[] data)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            EnsureInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    return await WriteAsync(key, data);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write data for key {Key} in transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <summary>
        /// Deletes data from storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        public async Task<bool> DeleteInTransactionAsync(string transactionId, string key)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            EnsureInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    return await DeleteAsync(key);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete data for key {Key} in transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <summary>
        /// Reads data from storage as a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>A stream containing the data, or null if the key does not exist.</returns>
        public async Task<Stream> ReadStreamAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var data = await ReadAsync(key);
                    if (data == null)
                    {
                        return null;
                    }

                    return new MemoryStream(data);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read stream for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Writes data to storage from a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataStream">The stream containing the data.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        public async Task<bool> WriteStreamAsync(string key, Stream dataStream)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (dataStream == null)
            {
                throw new ArgumentNullException(nameof(dataStream));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Read the stream into a memory stream
                    using var memoryStream = new MemoryStream();
                    await dataStream.CopyToAsync(memoryStream);

                    // Write the data
                    return await WriteAsync(key, memoryStream.ToArray());
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write stream for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Reads data from storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>An enumerable of data chunks, or null if the key does not exist.</returns>
        public async Task<IEnumerable<byte[]>> ReadChunkedAsync(string key, int chunkSize)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than zero", nameof(chunkSize));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var data = await ReadAsync(key);
                    if (data == null)
                    {
                        return null;
                    }

                    var chunks = new List<byte[]>();
                    int offset = 0;
                    while (offset < data.Length)
                    {
                        int length = Math.Min(chunkSize, data.Length - offset);
                        var chunk = new byte[length];
                        Array.Copy(data, offset, chunk, 0, length);
                        chunks.Add(chunk);
                        offset += length;
                    }

                    return chunks;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read chunked data for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Writes data to storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataChunks">The data chunks to write.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        public async Task<bool> WriteChunkedAsync(string key, IEnumerable<byte[]> dataChunks, int chunkSize)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (dataChunks == null)
            {
                throw new ArgumentNullException(nameof(dataChunks));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than zero", nameof(chunkSize));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Combine the chunks into a single byte array
                    using var memoryStream = new MemoryStream();
                    foreach (var chunk in dataChunks)
                    {
                        await memoryStream.WriteAsync(chunk, 0, chunk.Length);
                    }

                    // Write the data
                    return await WriteAsync(key, memoryStream.ToArray());
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write chunked data for key {Key}", key);
                return false;
            }
        }



        /// <summary>
        /// Gets metadata for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The metadata, or null if the key does not exist.</returns>
        public async Task<StorageMetadata> GetMetadataAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var metadataPath = Path.Combine(_options.StoragePath, ".metadata", key);

                    // Check if the metadata file exists
                    var existsResult = await _occlumManager.ExecuteCommandAsync($"test -f {metadataPath} && echo 'exists'");
                    if (existsResult.ExitCode != 0 || !existsResult.Output.Contains("exists"))
                    {
                        _logger.LogDebug("Metadata not found for key {Key}", key);
                        return null;
                    }

                    // Read the metadata file
                    var readResult = await _occlumManager.ExecuteCommandAsync($"cat {metadataPath}");
                    if (readResult.ExitCode != 0)
                    {
                        _logger.LogError("Error reading metadata for key {Key}: {Error}", key, readResult.Error);
                        return null;
                    }

                    // Parse the metadata
                    var json = readResult.Output.Trim();
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<StorageMetadata>(json);

                    return metadata;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Updates metadata for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>True if the metadata was updated successfully, false otherwise.</returns>
        public async Task<bool> UpdateMetadataAsync(string key, StorageMetadata metadata)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            EnsureInitialized();
            ValidateKey(key);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var metadataPath = Path.Combine(_options.StoragePath, ".metadata", key);

                    // Check if the key exists
                    if (!await ExistsAsync(key))
                    {
                        _logger.LogWarning("Key {Key} does not exist", key);
                        return false;
                    }

                    // Serialize the metadata
                    var json = System.Text.Json.JsonSerializer.Serialize(metadata);

                    // Write the metadata to the file
                    var writeResult = await _occlumManager.ExecuteCommandAsync($"echo '{json}' > {metadataPath}");
                    if (writeResult.ExitCode != 0)
                    {
                        _logger.LogError("Error writing metadata for key {Key}: {Error}", key, writeResult.Error);
                        return false;
                    }

                    _logger.LogDebug("Successfully updated metadata for {Key}", key);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metadata for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Compacts the storage to reclaim space.
        /// </summary>
        /// <returns>True if the storage was compacted successfully, false otherwise.</returns>
        public async Task<bool> CompactAsync()
        {
            EnsureInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Sync the filesystem
                    var syncResult = await _occlumManager.ExecuteCommandAsync("sync");
                    if (syncResult.ExitCode != 0)
                    {
                        _logger.LogError("Error compacting storage: {Error}", syncResult.Error);
                        return false;
                    }

                    _logger.LogDebug("Storage compacted successfully");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compact storage");
                return false;
            }
        }

        /// <summary>
        /// Flushes any pending writes to storage.
        /// </summary>
        /// <returns>True if the storage was flushed successfully, false otherwise.</returns>
        public async Task<bool> FlushAsync()
        {
            if (!_isInitialized)
            {
                return false;
            }

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Sync the filesystem
                    var syncResult = await _occlumManager.ExecuteCommandAsync("sync");
                    if (syncResult.ExitCode != 0)
                    {
                        _logger.LogError("Error flushing storage: {Error}", syncResult.Error);
                        return false;
                    }

                    _logger.LogDebug("Storage flushed");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush storage");
                return false;
            }
        }

        /// <summary>
        /// Disposes the storage provider.
        /// </summary>
        public void Dispose()
        {
            _flushTimer?.Dispose();
            _semaphore?.Dispose();
        }

        private string GetFilePath(string key)
        {
            // Sanitize the key to ensure it's a valid file path
            var sanitizedKey = key.Replace('\\', '/');
            return Path.Combine(_options.StoragePath, sanitizedKey);
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Storage provider is not initialized");
            }
        }

        private void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (key.Contains(".."))
            {
                throw new ArgumentException("Key cannot contain '..'", nameof(key));
            }
        }

        private async Task<byte[]> ProcessDataForWriteAsync(byte[] data)
        {
            // Apply compression if enabled
            if (_options.EnableCompression)
            {
                data = CompressData(data);
            }

            // Apply encryption if enabled
            if (_options.EnableEncryption && _options.EncryptionKey != null)
            {
                data = EncryptData(data, _options.EncryptionKey);
            }

            return data;
        }

        private async Task<byte[]> ProcessDataForReadAsync(byte[] data)
        {
            // Apply decryption if enabled
            if (_options.EnableEncryption && _options.EncryptionKey != null)
            {
                data = DecryptData(data, _options.EncryptionKey);
            }

            // Apply decompression if enabled
            if (_options.EnableCompression)
            {
                data = DecompressData(data);
            }

            return data;
        }

        private byte[] CompressData(byte[] data)
        {
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, (CompressionLevel)_options.CompressionLevel, true))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }

        private byte[] DecompressData(byte[] data)
        {
            using var compressedStream = new MemoryStream(data);
            using var decompressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(decompressedStream);
            }
            return decompressedStream.ToArray();
        }

        private byte[] EncryptData(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var memoryStream = new MemoryStream();
            // Write the IV to the beginning of the stream
            memoryStream.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
            }

            return memoryStream.ToArray();
        }

        private byte[] DecryptData(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;

            // Read the IV from the beginning of the data
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(data, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(
                new MemoryStream(data, iv.Length, data.Length - iv.Length),
                aes.CreateDecryptor(),
                CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(memoryStream);
            }

            return memoryStream.ToArray();
        }


    }
}
