using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RocksDbSharp;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// A persistent storage provider using LevelDB (implemented via RocksDB's LevelDB compatibility mode).
    /// </summary>
    public class LevelDBStorageProvider : BasePersistentStorageProvider
    {
        private readonly LevelDBStorageOptions _options;
        private RocksDb _db;
        private bool _initialized;

        // Key prefixes
        private const string DataKeyPrefix = "data:";
        private const string MetadataKeyPrefix = "meta:";
        private const string ChunkKeyPrefix = "chunk:";

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelDBStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="options">The options for the storage provider.</param>
        public LevelDBStorageProvider(ILogger<LevelDBStorageProvider> logger, LevelDBStorageOptions options = null)
            : base(logger)
        {
            _options = options ?? new LevelDBStorageOptions();
            _initialized = false;
        }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task InitializeAsync()
        {
            Logger.LogInformation("Initializing LevelDB storage provider with storage directory {StorageDirectory}", _options.StorageDirectory);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Create the storage directory if it doesn't exist
                    Directory.CreateDirectory(_options.StorageDirectory);

                    // Configure LevelDB options
                    var options = new DbOptions()
                        .SetCreateIfMissing(true)
                        .SetInfoLogLevel(InfoLogLevel.Error);

                    // Open the database
                    _db = RocksDb.Open(options, _options.StorageDirectory);

                    _initialized = true;
                    Logger.LogInformation("LevelDB storage provider initialized successfully");
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize LevelDB storage provider");
                throw new StorageException("Failed to initialize LevelDB storage provider", ex);
            }
        }

        /// <summary>
        /// Gets all keys in storage.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        public override async Task<IReadOnlyList<string>> GetAllKeysAsync()
        {
            CheckInitialized();

            Logger.LogDebug("Getting all keys");

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    var keys = new HashSet<string>();

                    // Get all keys from the database
                    using (var iterator = _db.NewIterator())
                    {
                        iterator.SeekToFirst();
                        while (iterator.Valid())
                        {
                            string key = Encoding.UTF8.GetString(iterator.Key());
                            if (key.StartsWith(MetadataKeyPrefix))
                            {
                                keys.Add(key.Substring(MetadataKeyPrefix.Length));
                            }
                            iterator.Next();
                        }
                    }

                    Logger.LogDebug("Found {KeyCount} keys", keys.Count);
                    return new List<string>(keys);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get all keys");
                throw new StorageException("Failed to get all keys", ex);
            }
        }

        /// <summary>
        /// Flushes any pending writes to storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task FlushAsync()
        {
            CheckInitialized();

            Logger.LogDebug("Flushing storage");

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Flush the database
                    _db.Flush(new FlushOptions());
                    Logger.LogDebug("Storage flushed successfully");
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to flush storage");
                throw new StorageException("Failed to flush storage", ex);
            }
        }

        /// <summary>
        /// Compacts the storage to reclaim space.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task CompactAsync()
        {
            CheckInitialized();

            Logger.LogDebug("Compacting storage");

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Compact the database
                    _db.CompactRange(null, null);
                    Logger.LogDebug("Storage compacted successfully");
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compact storage");
                throw new StorageException("Failed to compact storage", ex);
            }
        }

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task WriteDataInternalAsync(string key, byte[] data)
        {
            CheckInitialized();

            // Write the data to the database
            _db.Put(GetDataKey(key), data);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        protected override Task<byte[]> ReadDataInternalAsync(string key)
        {
            CheckInitialized();

            // Read the data from the database
            byte[] data = _db.Get(GetDataKey(key));
            return Task.FromResult(data);
        }

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task DeleteDataInternalAsync(string key)
        {
            CheckInitialized();

            // Delete the data from the database
            _db.Remove(GetDataKey(key));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        protected override Task<bool> ExistsInternalAsync(string key)
        {
            CheckInitialized();

            // Check if the metadata exists
            byte[] metadata = _db.Get(GetMetadataKey(key));
            return Task.FromResult(metadata != null);
        }

        /// <summary>
        /// Writes metadata to storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <param name="metadata">The metadata to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task WriteMetadataInternalAsync(string key, StorageMetadata metadata)
        {
            CheckInitialized();

            // Serialize the metadata to JSON
            string json = JsonSerializer.Serialize(metadata);
            byte[] metadataBytes = Encoding.UTF8.GetBytes(json);

            // Write the metadata to the database
            _db.Put(GetMetadataKey(key), metadataBytes);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Reads metadata from storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <returns>The metadata, or null if the key does not exist.</returns>
        protected override Task<StorageMetadata> ReadMetadataInternalAsync(string key)
        {
            CheckInitialized();

            // Read the metadata from the database
            byte[] metadataBytes = _db.Get(GetMetadataKey(key));
            if (metadataBytes == null)
            {
                return Task.FromResult<StorageMetadata>(null);
            }

            // Deserialize the metadata from JSON
            string json = Encoding.UTF8.GetString(metadataBytes);
            var metadata = JsonSerializer.Deserialize<StorageMetadata>(json);
            return Task.FromResult(metadata);
        }

        /// <summary>
        /// Deletes metadata from storage.
        /// </summary>
        /// <param name="key">The key for the metadata to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task DeleteMetadataInternalAsync(string key)
        {
            CheckInitialized();

            // Delete the metadata from the database
            _db.Remove(GetMetadataKey(key));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a data key.
        /// </summary>
        /// <param name="key">The base key.</param>
        /// <returns>The data key.</returns>
        private byte[] GetDataKey(string key)
        {
            return Encoding.UTF8.GetBytes($"{DataKeyPrefix}{key}");
        }

        /// <summary>
        /// Gets a metadata key.
        /// </summary>
        /// <param name="key">The base key.</param>
        /// <returns>The metadata key.</returns>
        private byte[] GetMetadataKey(string key)
        {
            return Encoding.UTF8.GetBytes($"{MetadataKeyPrefix}{key}");
        }

        /// <summary>
        /// Gets a chunk key.
        /// </summary>
        /// <param name="key">The base key.</param>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <returns>The chunk key.</returns>
        private byte[] GetChunkKey(string key, int chunkIndex)
        {
            return Encoding.UTF8.GetBytes($"{ChunkKeyPrefix}{key}:{chunkIndex}");
        }

        /// <summary>
        /// Checks if the storage provider has been initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new StorageException("Storage provider has not been initialized");
            }
        }

        /// <summary>
        /// Disposes the storage provider.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _db?.Dispose();
                }

                Disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
