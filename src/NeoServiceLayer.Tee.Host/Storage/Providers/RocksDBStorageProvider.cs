using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using RocksDbSharp;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// A persistent storage provider that uses RocksDB for storage.
    /// </summary>
    public class RocksDBStorageProvider : BasePersistentStorageProvider
    {
        private readonly RocksDBStorageOptions _options;
        private readonly ILogger<RocksDBStorageProvider> _logger;
        private RocksDb _db;
        private RocksDb _metadataDb;
        private bool _initialized;
        private readonly Dictionary<string, WriteBatch> _activeTransactions = new Dictionary<string, WriteBatch>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RocksDBStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options for the storage provider.</param>
        public RocksDBStorageProvider(
            ILogger<RocksDBStorageProvider> logger,
            RocksDBStorageOptions? options = null)
            : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new RocksDBStorageOptions();
            _initialized = false;
        }

        /// <inheritdoc/>
        public override async Task<bool> InitializeAsync()
        {
            if (_initialized)
            {
                return true;
            }

            try
            {
                _logger.LogInformation("Initializing RocksDB storage provider at {StoragePath}", _options.StoragePath);

                // Ensure the storage directory exists
                if (!Directory.Exists(_options.StoragePath))
                {
                    Directory.CreateDirectory(_options.StoragePath);
                    _logger.LogInformation("Created storage directory at {StoragePath}", _options.StoragePath);
                }

                // Ensure the metadata directory exists
                string metadataPath = Path.Combine(_options.StoragePath, "metadata");
                if (!Directory.Exists(metadataPath))
                {
                    Directory.CreateDirectory(metadataPath);
                    _logger.LogInformation("Created metadata directory at {MetadataPath}", metadataPath);
                }

                // Create the RocksDB options
                var options = new DbOptions()
                    .SetCreateIfMissing(true)
                    .SetCreateMissingColumnFamilies(true)
                    .SetIncreaseParallelism(Environment.ProcessorCount)
                    .SetMaxBackgroundCompactions(Environment.ProcessorCount)
                    .SetMaxBackgroundFlushes(Environment.ProcessorCount)
                    .SetWriteBufferSize(_options.WriteBufferSize)
                    .SetMaxWriteBufferNumber(_options.MaxWriteBufferNumber)
                    .SetMaxOpenFiles(_options.MaxOpenFiles);

                // Open the RocksDB database
                _db = RocksDb.Open(options, Path.Combine(_options.StoragePath, "data"));
                _metadataDb = RocksDb.Open(options, Path.Combine(_options.StoragePath, "metadata"));

                _initialized = true;
                _logger.LogInformation("RocksDB storage provider initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RocksDB storage provider");
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> WriteInternalAsync(string key, byte[] data)
        {
            EnsureInitialized();
            ValidateKey(key);

            try
            {
                // Write the data to the database
                _db.Put(Encoding.UTF8.GetBytes(key), data);

                // Create or update the metadata
                var metadata = new StorageMetadata
                {
                    Key = key,
                    Size = data.Length,
                    CreationTime = DateTime.UtcNow,
                    LastModifiedTime = DateTime.UtcNow,
                    LastAccessTime = DateTime.UtcNow,
                    ContentType = "application/octet-stream",
                    Hash = ComputeHash(data),
                    HashAlgorithm = "SHA256"
                };

                await WriteMetadataInternalAsync(key, metadata);

                _logger.LogDebug("Successfully wrote {Size} bytes to {Key}", data.Length, key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write data to {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<byte[]> ReadInternalAsync(string key)
        {
            EnsureInitialized();
            ValidateKey(key);

            try
            {
                // Read the data from the database
                byte[] data = _db.Get(Encoding.UTF8.GetBytes(key));

                if (data == null)
                {
                    _logger.LogWarning("Key {Key} does not exist", key);
                    return null;
                }

                // Update the last access time in the metadata
                var metadata = await GetMetadataInternalAsync(key);
                if (metadata != null)
                {
                    metadata.LastAccessTime = DateTime.UtcNow;
                    await WriteMetadataInternalAsync(key, metadata);
                }

                _logger.LogDebug("Successfully read {Size} bytes from {Key}", data.Length, key);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read data from {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> DeleteInternalAsync(string key)
        {
            EnsureInitialized();
            ValidateKey(key);

            try
            {
                // Check if the key exists
                byte[] data = _db.Get(Encoding.UTF8.GetBytes(key));
                if (data == null)
                {
                    _logger.LogWarning("Key {Key} does not exist", key);
                    return false;
                }

                // Delete the data from the database
                _db.Remove(Encoding.UTF8.GetBytes(key));

                // Delete the metadata
                _metadataDb.Remove(Encoding.UTF8.GetBytes(key));

                _logger.LogDebug("Successfully deleted {Key}", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> ExistsInternalAsync(string key)
        {
            EnsureInitialized();
            ValidateKey(key);

            try
            {
                // Check if the key exists
                byte[] data = _db.Get(Encoding.UTF8.GetBytes(key));
                return data != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if {Key} exists", key);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<IReadOnlyList<string>> GetAllKeysInternalAsync()
        {
            EnsureInitialized();

            try
            {
                var keys = new List<string>();

                // Iterate through all keys in the database
                using (var iterator = _db.NewIterator())
                {
                    iterator.SeekToFirst();
                    while (iterator.Valid())
                    {
                        string key = Encoding.UTF8.GetString(iterator.Key());
                        keys.Add(key);
                        iterator.Next();
                    }
                }

                _logger.LogDebug("Found {Count} keys", keys.Count);
                return keys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all keys");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        protected override async Task<StorageMetadata> GetMetadataInternalAsync(string key)
        {
            EnsureInitialized();
            ValidateKey(key);

            try
            {
                // Read the metadata from the database
                byte[] metadataBytes = _metadataDb.Get(Encoding.UTF8.GetBytes(key));

                if (metadataBytes == null)
                {
                    _logger.LogWarning("Metadata for key {Key} does not exist", key);
                    return null;
                }

                // Deserialize the metadata
                string json = Encoding.UTF8.GetString(metadataBytes);
                var metadata = System.Text.Json.JsonSerializer.Deserialize<StorageMetadata>(json);

                _logger.LogDebug("Successfully read metadata for {Key}", key);
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read metadata for {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> WriteMetadataInternalAsync(string key, StorageMetadata metadata)
        {
            EnsureInitialized();
            ValidateKey(key);

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            try
            {
                // Serialize the metadata
                string json = System.Text.Json.JsonSerializer.Serialize(metadata);
                byte[] metadataBytes = Encoding.UTF8.GetBytes(json);

                // Write the metadata to the database
                _metadataDb.Put(Encoding.UTF8.GetBytes(key), metadataBytes);

                _logger.LogDebug("Successfully wrote metadata for {Key}", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write metadata for {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> FlushInternalAsync()
        {
            EnsureInitialized();

            try
            {
                // Flush the database
                _db.Flush(new FlushOptions());
                _metadataDb.Flush(new FlushOptions());

                _logger.LogDebug("Successfully flushed the database");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush the database");
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> CompactInternalAsync()
        {
            EnsureInitialized();

            try
            {
                // Compact the database
                _db.CompactRange();
                _metadataDb.CompactRange();

                _logger.LogDebug("Successfully compacted the database");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compact the database");
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<string> BeginTransactionInternalAsync()
        {
            EnsureInitialized();

            try
            {
                // Generate a unique transaction ID
                string transactionId = Guid.NewGuid().ToString();

                // Create a new write batch for this transaction
                _activeTransactions[transactionId] = new WriteBatch();

                _logger.LogDebug("Beginning transaction {TransactionId}", transactionId);
                return transactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin transaction");
                throw;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> CommitTransactionInternalAsync(string transactionId)
        {
            EnsureInitialized();

            try
            {
                if (!_activeTransactions.TryGetValue(transactionId, out var batch))
                {
                    _logger.LogWarning("Transaction {TransactionId} does not exist", transactionId);
                    return false;
                }

                // Write the batch to the database
                _db.Write(batch);

                // Remove the transaction
                _activeTransactions.Remove(transactionId);

                _logger.LogDebug("Successfully committed transaction {TransactionId}", transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction {TransactionId}", transactionId);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> RollbackTransactionInternalAsync(string transactionId)
        {
            EnsureInitialized();

            try
            {
                if (!_activeTransactions.TryGetValue(transactionId, out var batch))
                {
                    _logger.LogWarning("Transaction {TransactionId} does not exist", transactionId);
                    return false;
                }

                // Just remove the transaction without writing the batch
                _activeTransactions.Remove(transactionId);

                _logger.LogDebug("Successfully rolled back transaction {TransactionId}", transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to roll back transaction {TransactionId}", transactionId);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> WriteInTransactionInternalAsync(string transactionId, string key, byte[] data)
        {
            EnsureInitialized();
            ValidateKey(key);

            try
            {
                if (!_activeTransactions.TryGetValue(transactionId, out var batch))
                {
                    _logger.LogWarning("Transaction {TransactionId} does not exist", transactionId);
                    return false;
                }

                // Add the write operation to the batch
                batch.Put(Encoding.UTF8.GetBytes(key), data);

                _logger.LogDebug("Successfully added write operation for {Key} to transaction {TransactionId}", key, transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add write operation for {Key} to transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> DeleteInTransactionInternalAsync(string transactionId, string key)
        {
            EnsureInitialized();
            ValidateKey(key);

            try
            {
                if (!_activeTransactions.TryGetValue(transactionId, out var batch))
                {
                    _logger.LogWarning("Transaction {TransactionId} does not exist", transactionId);
                    return false;
                }

                // Add the delete operation to the batch
                batch.Delete(Encoding.UTF8.GetBytes(key));

                _logger.LogDebug("Successfully added delete operation for {Key} to transaction {TransactionId}", key, transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add delete operation for {Key} to transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
                _metadataDb?.Dispose();

                // Dispose any active transactions
                foreach (var batch in _activeTransactions.Values)
                {
                    batch.Dispose();
                }
                _activeTransactions.Clear();
            }

            base.Dispose(disposing);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("The storage provider has not been initialized. Call InitializeAsync() first.");
            }
        }

        private void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
        }

        private string ComputeHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(data);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
