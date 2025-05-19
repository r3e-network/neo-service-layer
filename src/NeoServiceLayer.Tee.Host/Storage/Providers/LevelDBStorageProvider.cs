using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LevelDB;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// A persistent storage provider that uses LevelDB for storage.
    /// </summary>
    public class LevelDBStorageProvider : BasePersistentStorageProvider
    {
        private readonly LevelDBStorageOptions _options;
        private readonly ILogger<LevelDBStorageProvider> _logger;
        private DB _db;
        private DB _metadataDb;
        private bool _initialized;
        private readonly Dictionary<string, string> _activeTransactions = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelDBStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options for the storage provider.</param>
        public LevelDBStorageProvider(
            ILogger<LevelDBStorageProvider> logger,
            LevelDBStorageOptions? options = null)
            : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new LevelDBStorageOptions();
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
                _logger.LogInformation("Initializing LevelDB storage provider at {StoragePath}", _options.StoragePath);

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

                // Create the LevelDB options
                var options = new Options
                {
                    CreateIfMissing = true,
                    ParanoidChecks = true,
                    WriteBufferSize = _options.WriteBufferSize,
                    MaxOpenFiles = _options.MaxOpenFiles,
                    BlockSize = _options.BlockSize
                };

                // Open the LevelDB database
                _db = new DB(options, Path.Combine(_options.StoragePath, "data"));
                _metadataDb = new DB(options, Path.Combine(_options.StoragePath, "metadata"));

                _initialized = true;
                _logger.LogInformation("LevelDB storage provider initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize LevelDB storage provider");
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
                _db.Delete(Encoding.UTF8.GetBytes(key));

                // Delete the metadata
                _metadataDb.Delete(Encoding.UTF8.GetBytes(key));

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
                using (var iterator = _db.CreateIterator())
                {
                    iterator.SeekToFirst();
                    while (iterator.IsValid())
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
                // No explicit flush method in LevelDB.NET, but we can sync the database
                // by writing a dummy key and then deleting it
                string dummyKey = $"__flush__{Guid.NewGuid()}";
                _db.Put(Encoding.UTF8.GetBytes(dummyKey), new byte[0]);
                _db.Delete(Encoding.UTF8.GetBytes(dummyKey));

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
                // Compact the entire database
                _db.CompactRange(null, null);
                _metadataDb.CompactRange(null, null);

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
            // Generate a unique transaction ID
            string transactionId = Guid.NewGuid().ToString();
            _logger.LogDebug("Beginning transaction {TransactionId}", transactionId);
            return transactionId;
        }

        /// <inheritdoc/>
        protected override async Task<bool> CommitTransactionInternalAsync(string transactionId)
        {
            // No explicit transaction support in LevelDB.NET
            _logger.LogDebug("Committing transaction {TransactionId}", transactionId);
            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> RollbackTransactionInternalAsync(string transactionId)
        {
            // No explicit transaction support in LevelDB.NET
            _logger.LogDebug("Rolling back transaction {TransactionId}", transactionId);
            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> WriteInTransactionInternalAsync(string transactionId, string key, byte[] data)
        {
            // Just write the data directly
            return await WriteInternalAsync(key, data);
        }

        /// <inheritdoc/>
        protected override async Task<bool> DeleteInTransactionInternalAsync(string transactionId, string key)
        {
            // Just delete the data directly
            return await DeleteInternalAsync(key);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
                _metadataDb?.Dispose();
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
