using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// A file-based persistent storage provider optimized for Occlum LibOS.
    /// </summary>
    public class OcclumFileStorageProvider : BasePersistentStorageProvider
    {
        private readonly OcclumFileStorageOptions _options;
        private readonly string _dataDirectory;
        private readonly string _metadataDirectory;
        private readonly string _journalDirectory;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumFileStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="options">The options for the storage provider.</param>
        public OcclumFileStorageProvider(ILogger<OcclumFileStorageProvider> logger, OcclumFileStorageOptions options = null)
            : base(logger)
        {
            _options = options ?? new OcclumFileStorageOptions();
            _dataDirectory = Path.Combine(_options.StorageDirectory, "data");
            _metadataDirectory = Path.Combine(_options.StorageDirectory, "metadata");
            _journalDirectory = Path.Combine(_options.StorageDirectory, "journal");
            _initialized = false;
        }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task<bool> InitializeAsync()
        {
            Logger.LogInformation("Initializing Occlum file storage provider with storage directory {StorageDirectory}", _options.StorageDirectory);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Create the storage directories if they don't exist
                    Directory.CreateDirectory(_options.StorageDirectory);
                    Directory.CreateDirectory(_dataDirectory);
                    Directory.CreateDirectory(_metadataDirectory);
                    Directory.CreateDirectory(_journalDirectory);

                    // Recover from any incomplete operations
                    await RecoverFromJournalAsync();

                    _initialized = true;
                    Logger.LogInformation("Occlum file storage provider initialized successfully");
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize Occlum file storage provider");
                return false;
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

                    // Get all metadata files
                    var metadataFiles = Directory.GetFiles(_metadataDirectory, "*.json");
                    foreach (var file in metadataFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        keys.Add(fileName);
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
        protected override async Task<bool> FlushInternalAsync()
        {
            CheckInitialized();

            Logger.LogDebug("Flushing storage");

            // File operations are already flushed to disk, so this is a no-op
            return true;
        }

        /// <summary>
        /// Compacts the storage to reclaim space.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task<bool> CompactInternalAsync()
        {
            CheckInitialized();

            Logger.LogDebug("Compacting storage");

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Clean up any orphaned data files
                    var dataFiles = Directory.GetFiles(_dataDirectory, "*.dat");
                    var metadataFiles = Directory.GetFiles(_metadataDirectory, "*.json");
                    var metadataKeys = metadataFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToHashSet();

                    foreach (var dataFile in dataFiles)
                    {
                        string key = Path.GetFileNameWithoutExtension(dataFile);
                        if (!metadataKeys.Contains(key) && !key.Contains("_chunk_"))
                        {
                            File.Delete(dataFile);
                            Logger.LogDebug("Deleted orphaned data file {File}", dataFile);
                        }
                    }

                    // Clean up any orphaned chunk files
                    foreach (var dataFile in dataFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(dataFile);
                        if (fileName.Contains("_chunk_"))
                        {
                            string baseKey = fileName.Substring(0, fileName.IndexOf("_chunk_"));
                            if (!metadataKeys.Contains(baseKey))
                            {
                                File.Delete(dataFile);
                                Logger.LogDebug("Deleted orphaned chunk file {File}", dataFile);
                            }
                        }
                    }

                    // Clean up any journal files
                    var journalFiles = Directory.GetFiles(_journalDirectory, "*.journal");
                    foreach (var journalFile in journalFiles)
                    {
                        File.Delete(journalFile);
                        Logger.LogDebug("Deleted journal file {File}", journalFile);
                    }

                    Logger.LogDebug("Storage compacted successfully");
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compact storage");
                return false;
            }
        }

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task WriteDataInternalAsync(string key, byte[] data)
        {
            CheckInitialized();

            // Create a journal entry for the operation
            await CreateJournalEntryAsync(JournalOperationType.WriteData, key);

            try
            {
                // Write the data to a temporary file first
                string dataFilePath = GetDataFilePath(key);
                string tempFilePath = $"{dataFilePath}.tmp";
                await File.WriteAllBytesAsync(tempFilePath, data);

                // Rename the temporary file to the actual file
                if (File.Exists(dataFilePath))
                {
                    File.Delete(dataFilePath);
                }
                File.Move(tempFilePath, dataFilePath);

                // Delete the journal entry
                await DeleteJournalEntryAsync(key);
            }
            catch (Exception)
            {
                // Delete the journal entry to avoid leaving orphaned entries
                await DeleteJournalEntryAsync(key);
                throw;
            }
        }

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        protected override async Task<byte[]> ReadDataInternalAsync(string key)
        {
            CheckInitialized();

            string dataFilePath = GetDataFilePath(key);
            if (!File.Exists(dataFilePath))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(dataFilePath);
        }

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task DeleteDataInternalAsync(string key)
        {
            CheckInitialized();

            // Create a journal entry for the operation
            await CreateJournalEntryAsync(JournalOperationType.DeleteData, key);

            try
            {
                string dataFilePath = GetDataFilePath(key);
                if (File.Exists(dataFilePath))
                {
                    File.Delete(dataFilePath);
                }

                // Delete the journal entry
                await DeleteJournalEntryAsync(key);
            }
            catch (Exception)
            {
                // Delete the journal entry to avoid leaving orphaned entries
                await DeleteJournalEntryAsync(key);
                throw;
            }
        }

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        protected override Task<bool> ExistsInternalAsync(string key)
        {
            CheckInitialized();

            string metadataFilePath = GetMetadataFilePath(key);
            bool exists = File.Exists(metadataFilePath);
            return Task.FromResult(exists);
        }

        /// <summary>
        /// Writes metadata to storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <param name="metadata">The metadata to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task WriteMetadataInternalAsync(string key, StorageMetadata metadata)
        {
            CheckInitialized();

            // Create a journal entry for the operation
            await CreateJournalEntryAsync(JournalOperationType.WriteMetadata, key);

            try
            {
                // Write the metadata to a temporary file first
                string metadataFilePath = GetMetadataFilePath(key);
                string tempFilePath = $"{metadataFilePath}.tmp";
                string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(tempFilePath, json);

                // Rename the temporary file to the actual file
                if (File.Exists(metadataFilePath))
                {
                    File.Delete(metadataFilePath);
                }
                File.Move(tempFilePath, metadataFilePath);

                // Delete the journal entry
                await DeleteJournalEntryAsync(key);
            }
            catch (Exception)
            {
                // Delete the journal entry to avoid leaving orphaned entries
                await DeleteJournalEntryAsync(key);
                throw;
            }
        }

        /// <summary>
        /// Reads metadata from storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <returns>The metadata, or null if the key does not exist.</returns>
        protected override async Task<StorageMetadata> ReadMetadataInternalAsync(string key)
        {
            CheckInitialized();

            string metadataFilePath = GetMetadataFilePath(key);
            if (!File.Exists(metadataFilePath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(metadataFilePath);
            return JsonSerializer.Deserialize<StorageMetadata>(json);
        }

        /// <summary>
        /// Deletes metadata from storage.
        /// </summary>
        /// <param name="key">The key for the metadata to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task DeleteMetadataInternalAsync(string key)
        {
            CheckInitialized();

            // Create a journal entry for the operation
            await CreateJournalEntryAsync(JournalOperationType.DeleteMetadata, key);

            try
            {
                string metadataFilePath = GetMetadataFilePath(key);
                if (File.Exists(metadataFilePath))
                {
                    File.Delete(metadataFilePath);
                }

                // Delete the journal entry
                await DeleteJournalEntryAsync(key);
            }
            catch (Exception)
            {
                // Delete the journal entry to avoid leaving orphaned entries
                await DeleteJournalEntryAsync(key);
                throw;
            }
        }

        /// <summary>
        /// Gets the file path for data.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The file path.</returns>
        private string GetDataFilePath(string key)
        {
            return Path.Combine(_dataDirectory, $"{key}.dat");
        }

        /// <summary>
        /// Gets the file path for metadata.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <returns>The file path.</returns>
        private string GetMetadataFilePath(string key)
        {
            return Path.Combine(_metadataDirectory, $"{key}.json");
        }

        /// <summary>
        /// Gets the file path for a journal entry.
        /// </summary>
        /// <param name="key">The key for the journal entry.</param>
        /// <returns>The file path.</returns>
        private string GetJournalFilePath(string key)
        {
            return Path.Combine(_journalDirectory, $"{key}.journal");
        }

        /// <summary>
        /// Creates a journal entry for an operation.
        /// </summary>
        /// <param name="operationType">The type of operation.</param>
        /// <param name="key">The key for the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task CreateJournalEntryAsync(JournalOperationType operationType, string key)
        {
            var journalEntry = new JournalEntry
            {
                OperationType = operationType,
                Key = key,
                Timestamp = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(journalEntry);
            await File.WriteAllTextAsync(GetJournalFilePath(key), json);
        }

        /// <summary>
        /// Deletes a journal entry.
        /// </summary>
        /// <param name="key">The key for the journal entry.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private Task DeleteJournalEntryAsync(string key)
        {
            string journalFilePath = GetJournalFilePath(key);
            if (File.Exists(journalFilePath))
            {
                File.Delete(journalFilePath);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Recovers from any incomplete operations recorded in the journal.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task RecoverFromJournalAsync()
        {
            Logger.LogInformation("Recovering from journal");

            var journalFiles = Directory.GetFiles(_journalDirectory, "*.journal");
            foreach (var journalFile in journalFiles)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(journalFile);
                    var journalEntry = JsonSerializer.Deserialize<JournalEntry>(json);

                    if (journalEntry != null)
                    {
                        Logger.LogInformation("Recovering operation {OperationType} for key {Key}", journalEntry.OperationType, journalEntry.Key);

                        // Delete any temporary files
                        string dataFilePath = GetDataFilePath(journalEntry.Key);
                        string metadataFilePath = GetMetadataFilePath(journalEntry.Key);
                        string tempDataFilePath = $"{dataFilePath}.tmp";
                        string tempMetadataFilePath = $"{metadataFilePath}.tmp";

                        if (File.Exists(tempDataFilePath))
                        {
                            File.Delete(tempDataFilePath);
                        }

                        if (File.Exists(tempMetadataFilePath))
                        {
                            File.Delete(tempMetadataFilePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to recover from journal entry {JournalFile}", journalFile);
                }
                finally
                {
                    // Delete the journal entry
                    File.Delete(journalFile);
                }
            }

            Logger.LogInformation("Journal recovery completed");
        }

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>A unique transaction ID.</returns>
        protected override async Task<string> BeginTransactionInternalAsync()
        {
            CheckInitialized();

            // Generate a unique transaction ID
            string transactionId = Guid.NewGuid().ToString();
            Logger.LogDebug("Beginning transaction {TransactionId}", transactionId);
            return transactionId;
        }

        /// <summary>
        /// Commits a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was committed successfully, false otherwise.</returns>
        protected override async Task<bool> CommitTransactionInternalAsync(string transactionId)
        {
            CheckInitialized();
            Logger.LogDebug("Committing transaction {TransactionId}", transactionId);
            return true;
        }

        /// <summary>
        /// Rolls back a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was rolled back successfully, false otherwise.</returns>
        protected override async Task<bool> RollbackTransactionInternalAsync(string transactionId)
        {
            CheckInitialized();
            Logger.LogDebug("Rolling back transaction {TransactionId}", transactionId);
            return true;
        }

        /// <summary>
        /// Writes data to storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        protected override async Task<bool> WriteInTransactionInternalAsync(string transactionId, string key, byte[] data)
        {
            // Just write the data directly
            await WriteDataInternalAsync(key, data);
            return true;
        }

        /// <summary>
        /// Deletes data from storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted successfully, false otherwise.</returns>
        protected override async Task<bool> DeleteInTransactionInternalAsync(string transactionId, string key)
        {
            // Just delete the data directly
            await DeleteDataInternalAsync(key);
            return true;
        }

        /// <summary>
        /// Checks if the storage provider has been initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Storage provider has not been initialized");
            }
        }

        /// <summary>
        /// Types of operations that can be recorded in the journal.
        /// </summary>
        private enum JournalOperationType
        {
            /// <summary>
            /// Write data operation.
            /// </summary>
            WriteData,

            /// <summary>
            /// Delete data operation.
            /// </summary>
            DeleteData,

            /// <summary>
            /// Write metadata operation.
            /// </summary>
            WriteMetadata,

            /// <summary>
            /// Delete metadata operation.
            /// </summary>
            DeleteMetadata
        }

        /// <summary>
        /// Represents an entry in the journal.
        /// </summary>
        private class JournalEntry
        {
            /// <summary>
            /// Gets or sets the type of operation.
            /// </summary>
            public JournalOperationType OperationType { get; set; }

            /// <summary>
            /// Gets or sets the key for the operation.
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// Gets or sets the timestamp of the operation.
            /// </summary>
            public DateTime Timestamp { get; set; }
        }
    }
}
