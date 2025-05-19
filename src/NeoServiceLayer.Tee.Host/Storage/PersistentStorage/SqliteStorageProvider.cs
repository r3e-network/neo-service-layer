using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// A persistent storage provider using SQLite.
    /// </summary>
    public class SqliteStorageProvider : BasePersistentStorageProvider, IPersistentStorageProvider
    {
        private readonly SqliteStorageOptions _options;
        private SqliteConnection _connection;
        private bool _initialized;
        private readonly Dictionary<string, SqliteTransaction> _activeTransactions = new Dictionary<string, SqliteTransaction>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="options">The options for the storage provider.</param>
        public SqliteStorageProvider(ILogger<SqliteStorageProvider> logger, SqliteStorageOptions options = null)
            : base(logger)
        {
            _options = options ?? new SqliteStorageOptions();
            _initialized = false;
        }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task<bool> InitializeAsync()
        {
            Logger.LogInformation("Initializing SQLite storage provider with database file {DatabaseFile}", _options.DatabaseFile);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Create the directory if it doesn't exist
                    string directory = Path.GetDirectoryName(_options.DatabaseFile);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Create the connection string
                    var connectionStringBuilder = new SqliteConnectionStringBuilder
                    {
                        DataSource = _options.DatabaseFile,
                        Mode = SqliteOpenMode.ReadWriteCreate,
                        Cache = SqliteCacheMode.Shared
                    };

                    // Create the connection
                    _connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                    await _connection.OpenAsync();

                    // Create the tables if they don't exist
                    await CreateTablesAsync();

                    _initialized = true;
                    Logger.LogInformation("SQLite storage provider initialized successfully");
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize SQLite storage provider");
                return false;
            }
        }

        /// <summary>
        /// Gets all keys in storage.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        protected override async Task<IReadOnlyList<string>> GetAllKeysInternalAsync()
        {
            CheckInitialized();

            Logger.LogDebug("Getting all keys");

            try
            {
                var keys = new List<string>();

                // Get all keys from the metadata table
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT key FROM metadata";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string key = reader.GetString(0);
                            keys.Add(key);
                        }
                    }
                }

                Logger.LogDebug("Found {KeyCount} keys", keys.Count);
                return keys;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get all keys");
                return new List<string>();
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

            try
            {
                // Execute a PRAGMA to flush the database
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA wal_checkpoint(FULL)";
                    await command.ExecuteNonQueryAsync();
                }

                Logger.LogDebug("Storage flushed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to flush storage");
                return false;
            }
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
                // Execute a VACUUM to compact the database
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "VACUUM";
                    await command.ExecuteNonQueryAsync();
                }

                Logger.LogDebug("Storage compacted successfully");
                return true;
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
        protected override async Task<bool> WriteInternalAsync(string key, byte[] data)
        {
            CheckInitialized();

            try
            {
                // Begin a transaction
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // Check if the key already exists
                        bool exists = await ExistsInternalAsync(key);

                        if (exists)
                        {
                            // Update the data
                            using (var command = _connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = "UPDATE data SET value = @value WHERE key = @key";
                                command.Parameters.AddWithValue("@key", key);
                                command.Parameters.AddWithValue("@value", data);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Insert the data
                            using (var command = _connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = "INSERT INTO data (key, value) VALUES (@key, @value)";
                                command.Parameters.AddWithValue("@key", key);
                                command.Parameters.AddWithValue("@value", data);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        // Commit the transaction
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        // Rollback the transaction
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write data for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        protected override async Task<byte[]> ReadInternalAsync(string key)
        {
            CheckInitialized();

            try
            {
                // Read the data
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT value FROM data WHERE key = @key";
                    command.Parameters.AddWithValue("@key", key);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetFieldValue<byte[]>(0);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read data for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task<bool> DeleteInternalAsync(string key)
        {
            CheckInitialized();

            try
            {
                // Begin a transaction
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // Delete the data
                        using (var command = _connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = "DELETE FROM data WHERE key = @key";
                            command.Parameters.AddWithValue("@key", key);
                            await command.ExecuteNonQueryAsync();
                        }

                        // Delete the metadata
                        using (var command = _connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = "DELETE FROM metadata WHERE key = @key";
                            command.Parameters.AddWithValue("@key", key);
                            await command.ExecuteNonQueryAsync();
                        }

                        // Commit the transaction
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        // Rollback the transaction
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete data for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        protected override async Task<bool> ExistsInternalAsync(string key)
        {
            CheckInitialized();

            try
            {
                // Check if the key exists in the metadata table
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM metadata WHERE key = @key";
                    command.Parameters.AddWithValue("@key", key);
                    long count = (long)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to check if key {Key} exists", key);
                return false;
            }
        }

        /// <summary>
        /// Writes metadata to storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <param name="metadata">The metadata to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task<bool> WriteMetadataInternalAsync(string key, StorageMetadata metadata)
        {
            CheckInitialized();

            try
            {
                // Serialize the metadata to JSON
                string json = JsonSerializer.Serialize(metadata);

                // Begin a transaction
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // Check if the key already exists
                        bool exists = await ExistsInternalAsync(key);

                        if (exists)
                        {
                            // Update the metadata
                            using (var command = _connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = "UPDATE metadata SET value = @value WHERE key = @key";
                                command.Parameters.AddWithValue("@key", key);
                                command.Parameters.AddWithValue("@value", json);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Insert the metadata
                            using (var command = _connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = "INSERT INTO metadata (key, value) VALUES (@key, @value)";
                                command.Parameters.AddWithValue("@key", key);
                                command.Parameters.AddWithValue("@value", json);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        // Commit the transaction
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        // Rollback the transaction
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write metadata for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Reads metadata from storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <returns>The metadata, or null if the key does not exist.</returns>
        protected override async Task<StorageMetadata> GetMetadataInternalAsync(string key)
        {
            CheckInitialized();

            try
            {
                // Read the metadata
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT value FROM metadata WHERE key = @key";
                    command.Parameters.AddWithValue("@key", key);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string json = reader.GetString(0);
                            return JsonSerializer.Deserialize<StorageMetadata>(json);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read metadata for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Adds transaction support to the storage provider.
        /// </summary>
        /// <returns>A unique transaction ID.</returns>
        protected override async Task<string> BeginTransactionInternalAsync()
        {
            CheckInitialized();

            try
            {
                // Generate a unique transaction ID
                string transactionId = Guid.NewGuid().ToString();

                // Begin a transaction
                var transaction = _connection.BeginTransaction();
                _activeTransactions[transactionId] = transaction;

                Logger.LogDebug("Transaction {TransactionId} started", transactionId);
                return transactionId;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to begin transaction");
                return null;
            }
        }

        /// <summary>
        /// Commits a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was committed successfully, false otherwise.</returns>
        protected override async Task<bool> CommitTransactionInternalAsync(string transactionId)
        {
            CheckInitialized();

            try
            {
                // Get the transaction
                if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
                {
                    Logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                    return false;
                }

                // Commit the transaction
                transaction.Commit();
                _activeTransactions.Remove(transactionId);

                Logger.LogDebug("Transaction {TransactionId} committed", transactionId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to commit transaction {TransactionId}", transactionId);
                return false;
            }
        }

        /// <summary>
        /// Rolls back a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was rolled back successfully, false otherwise.</returns>
        protected override async Task<bool> RollbackTransactionInternalAsync(string transactionId)
        {
            CheckInitialized();

            try
            {
                // Get the transaction
                if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
                {
                    Logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                    return false;
                }

                // Rollback the transaction
                transaction.Rollback();
                _activeTransactions.Remove(transactionId);

                Logger.LogDebug("Transaction {TransactionId} rolled back", transactionId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to rollback transaction {TransactionId}", transactionId);
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
        protected override async Task<bool> WriteInTransactionInternalAsync(string transactionId, string key, byte[] data)
        {
            CheckInitialized();

            try
            {
                // Get the transaction
                if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
                {
                    Logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                    return false;
                }

                // Check if the key already exists
                bool exists = await ExistsInternalAsync(key);

                if (exists)
                {
                    // Update the data
                    using (var command = _connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "UPDATE data SET value = @value WHERE key = @key";
                        command.Parameters.AddWithValue("@key", key);
                        command.Parameters.AddWithValue("@value", data);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    // Insert the data
                    using (var command = _connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "INSERT INTO data (key, value) VALUES (@key, @value)";
                        command.Parameters.AddWithValue("@key", key);
                        command.Parameters.AddWithValue("@value", data);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                Logger.LogDebug("Data written for key {Key} in transaction {TransactionId}", key, transactionId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write data for key {Key} in transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <summary>
        /// Deletes data from storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        protected override async Task<bool> DeleteInTransactionInternalAsync(string transactionId, string key)
        {
            CheckInitialized();

            try
            {
                // Get the transaction
                if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
                {
                    Logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                    return false;
                }

                // Delete the data
                using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM data WHERE key = @key";
                    command.Parameters.AddWithValue("@key", key);
                    await command.ExecuteNonQueryAsync();
                }

                // Delete the metadata
                using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM metadata WHERE key = @key";
                    command.Parameters.AddWithValue("@key", key);
                    await command.ExecuteNonQueryAsync();
                }

                Logger.LogDebug("Data deleted for key {Key} in transaction {TransactionId}", key, transactionId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete data for key {Key} in transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <summary>
        /// Creates the tables if they don't exist.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task CreateTablesAsync()
        {
            try
            {
                // Create the data table
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS data (
                            key TEXT PRIMARY KEY,
                            value BLOB
                        )";
                    await command.ExecuteNonQueryAsync();
                }

                // Create the metadata table
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS metadata (
                            key TEXT PRIMARY KEY,
                            value TEXT
                        )";
                    await command.ExecuteNonQueryAsync();
                }

                // Create indexes
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_data_key ON data (key)";
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_metadata_key ON metadata (key)";
                    await command.ExecuteNonQueryAsync();
                }

                // Set pragmas
                if (_options.UseWalJournalMode)
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA journal_mode = WAL";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                if (_options.EnableForeignKeys)
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA foreign_keys = ON";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA busy_timeout = {_options.BusyTimeoutMs}";
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA cache_size = {_options.CacheSizePages}";
                    await command.ExecuteNonQueryAsync();
                }

                if (_options.EnableAutoVacuum)
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA auto_vacuum = INCREMENTAL";
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create tables");
                throw;
            }
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
                    foreach (var transaction in _activeTransactions.Values)
                    {
                        try
                        {
                            transaction.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Failed to dispose transaction");
                        }
                    }
                    _activeTransactions.Clear();

                    try
                    {
                        _connection?.Close();
                        _connection?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to dispose connection");
                    }
                }

                Disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
