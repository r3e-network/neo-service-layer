using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Base abstract class for persistent storage providers that implements common functionality.
    /// </summary>
    public abstract class BasePersistentStorageProvider : BaseStorageProvider, IPersistentStorageProvider
    {
        /// <summary>
        /// Initializes a new instance of the BasePersistentStorageProvider class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        protected BasePersistentStorageProvider(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc/>
        public virtual async Task<bool> WriteChunkedAsync(string key, IEnumerable<byte[]> dataChunks, int chunkSize)
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

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Create metadata for the chunked data
                    var chunks = dataChunks.ToList();
                    var totalSize = chunks.Sum(chunk => chunk.Length);
                    var metadata = new StorageMetadata
                    {
                        Key = key,
                        Size = totalSize,
                        CreationTime = DateTime.UtcNow,
                        LastModifiedTime = DateTime.UtcNow,
                        LastAccessTime = DateTime.UtcNow,
                        IsChunked = true,
                        ChunkCount = chunks.Count,
                        ChunkSize = chunkSize
                    };

                    // Write each chunk
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        var chunkKey = $"{key}:chunk:{i}";
                        if (!await WriteInternalAsync(chunkKey, chunks[i]))
                        {
                            Logger.LogError("Failed to write chunk {ChunkIndex} for key {Key}", i, key);
                            return false;
                        }
                    }

                    // Write the metadata
                    if (!await WriteMetadataInternalAsync(key, metadata))
                    {
                        Logger.LogError("Failed to write metadata for key {Key}", key);
                        return false;
                    }

                    Logger.LogDebug("Chunked data written successfully for key {Key}", key);
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write chunked data for key {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> WriteStreamAsync(string key, Stream dataStream)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (dataStream == null)
            {
                throw new ArgumentNullException(nameof(dataStream));
            }

            CheckDisposed();

            try
            {
                // Read the stream into a buffer
                using (var memoryStream = new MemoryStream())
                {
                    await dataStream.CopyToAsync(memoryStream);
                    return await WriteAsync(key, memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write stream data for key {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<byte[]>> ReadChunkedAsync(string key, int chunkSize)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than zero", nameof(chunkSize));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Get the metadata
                    var metadata = await GetMetadataAsync(key);
                    if (metadata == null)
                    {
                        Logger.LogWarning("Metadata not found for key {Key}", key);
                        return null;
                    }

                    // Check if the data is chunked
                    if (!metadata.IsChunked)
                    {
                        Logger.LogWarning("Data for key {Key} is not chunked", key);
                        var data = await ReadInternalAsync(key);
                        if (data == null)
                        {
                            return null;
                        }

                        // Split the data into chunks
                        var chunks = new List<byte[]>();
                        for (int i = 0; i < data.Length; i += chunkSize)
                        {
                            var chunkLength = Math.Min(chunkSize, data.Length - i);
                            var chunk = new byte[chunkLength];
                            Array.Copy(data, i, chunk, 0, chunkLength);
                            chunks.Add(chunk);
                        }

                        return chunks;
                    }

                    // Read each chunk
                    var result = new List<byte[]>();
                    for (int i = 0; i < metadata.ChunkCount; i++)
                    {
                        var chunkKey = $"{key}:chunk:{i}";
                        var chunk = await ReadInternalAsync(chunkKey);
                        if (chunk == null)
                        {
                            Logger.LogError("Chunk {ChunkIndex} not found for key {Key}", i, key);
                            return null;
                        }

                        result.Add(chunk);
                    }

                    // Update the last access time
                    metadata.LastAccessTime = DateTime.UtcNow;
                    await WriteMetadataInternalAsync(key, metadata);

                    Logger.LogDebug("Chunked data read successfully for key {Key}", key);
                    return result;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read chunked data for key {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<Stream> ReadStreamAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            CheckDisposed();

            try
            {
                // Read the data
                var data = await ReadAsync(key);
                if (data == null)
                {
                    return null;
                }

                // Create a memory stream from the data
                return new MemoryStream(data);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read stream data for key {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<long> GetSizeAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            CheckDisposed();

            try
            {
                // Get the metadata
                var metadata = await GetMetadataAsync(key);
                if (metadata == null)
                {
                    return -1;
                }

                return metadata.Size;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get size for key {Key}", key);
                return -1;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<StorageMetadata> GetMetadataAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await GetMetadataInternalAsync(key);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get metadata for key {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> UpdateMetadataAsync(string key, StorageMetadata metadata)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await WriteMetadataInternalAsync(key, metadata);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update metadata for key {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> CompactAsync()
        {
            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await CompactInternalAsync();
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

        /// <inheritdoc/>
        public virtual async Task<string> BeginTransactionAsync()
        {
            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await BeginTransactionInternalAsync();
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to begin transaction");
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> CommitTransactionAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await CommitTransactionInternalAsync(transactionId);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to commit transaction {TransactionId}", transactionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> RollbackTransactionAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await RollbackTransactionInternalAsync(transactionId);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to rollback transaction {TransactionId}", transactionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> WriteInTransactionAsync(string transactionId, string key, byte[] data)
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

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await WriteInTransactionInternalAsync(transactionId, key, data);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write data for key {Key} in transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteInTransactionAsync(string transactionId, string key)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    return await DeleteInTransactionInternalAsync(transactionId, key);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete data for key {Key} in transaction {TransactionId}", key, transactionId);
                return false;
            }
        }

        /// <summary>
        /// Internal implementation of getting metadata for data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The metadata for the data, or null if the key does not exist.</returns>
        protected abstract Task<StorageMetadata> GetMetadataInternalAsync(string key);

        /// <summary>
        /// Internal implementation of writing metadata for data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="metadata">The metadata to write.</param>
        /// <returns>True if the metadata was written successfully, false otherwise.</returns>
        protected abstract Task<bool> WriteMetadataInternalAsync(string key, StorageMetadata metadata);

        /// <summary>
        /// Internal implementation of compacting the storage to reclaim space.
        /// </summary>
        /// <returns>True if the compact was successful, false otherwise.</returns>
        protected abstract Task<bool> CompactInternalAsync();

        /// <summary>
        /// Internal implementation of beginning a transaction.
        /// </summary>
        /// <returns>The transaction ID.</returns>
        protected abstract Task<string> BeginTransactionInternalAsync();

        /// <summary>
        /// Internal implementation of committing a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was committed successfully, false otherwise.</returns>
        protected abstract Task<bool> CommitTransactionInternalAsync(string transactionId);

        /// <summary>
        /// Internal implementation of rolling back a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the transaction was rolled back successfully, false otherwise.</returns>
        protected abstract Task<bool> RollbackTransactionInternalAsync(string transactionId);

        /// <summary>
        /// Internal implementation of writing data to storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if the data was written successfully, false otherwise.</returns>
        protected abstract Task<bool> WriteInTransactionInternalAsync(string transactionId, string key, byte[] data);

        /// <summary>
        /// Internal implementation of deleting data from storage as part of a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        protected abstract Task<bool> DeleteInTransactionInternalAsync(string transactionId, string key);
    }
}
