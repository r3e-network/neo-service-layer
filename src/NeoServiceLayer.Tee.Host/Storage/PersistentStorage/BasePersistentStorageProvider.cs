using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Base abstract class for persistent storage providers that implements common functionality.
    /// </summary>
    public abstract class BasePersistentStorageProvider : IPersistentStorageProvider
    {
        /// <summary>
        /// Default chunk size in bytes (1 MB).
        /// </summary>
        protected const int DefaultChunkSize = 1024 * 1024;

        /// <summary>
        /// Logger instance.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Semaphore for synchronizing access to storage.
        /// </summary>
        protected readonly SemaphoreSlim Semaphore;

        /// <summary>
        /// Whether the provider has been disposed.
        /// </summary>
        protected bool Disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePersistentStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        protected BasePersistentStorageProvider(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Semaphore = new SemaphoreSlim(1, 1);
            Disposed = false;
        }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public abstract Task InitializeAsync();

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task WriteAsync(string key, byte[] data)
        {
            ValidateKey(key);
            ValidateData(data);

            Logger.LogDebug("Writing data for key {Key} ({Size} bytes)", key, data.Length);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Create metadata for the data
                    var metadata = new StorageMetadata
                    {
                        Key = key,
                        Size = data.Length,
                        CreationTime = DateTime.UtcNow,
                        LastModifiedTime = DateTime.UtcNow,
                        LastAccessTime = DateTime.UtcNow,
                        IsChunked = false,
                        Hash = ComputeHash(data)
                    };

                    // Write the data
                    await WriteDataInternalAsync(key, data);

                    // Store the metadata
                    await WriteMetadataInternalAsync(key, metadata);

                    Logger.LogDebug("Data written successfully for key {Key}", key);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write data for key {Key}", key);
                throw new StorageException($"Failed to write data for key {key}", ex);
            }
        }

        /// <summary>
        /// Writes data to storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataChunks">The data chunks to write.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task WriteChunkedAsync(string key, IEnumerable<byte[]> dataChunks, int chunkSize)
        {
            ValidateKey(key);
            if (dataChunks == null)
            {
                throw new ArgumentNullException(nameof(dataChunks));
            }
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than zero", nameof(chunkSize));
            }

            Logger.LogDebug("Writing chunked data for key {Key} with chunk size {ChunkSize}", key, chunkSize);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Convert chunks to a list for easier processing
                    var chunksList = new List<byte[]>(dataChunks);
                    if (chunksList.Count == 0)
                    {
                        throw new ArgumentException("Data chunks cannot be empty", nameof(dataChunks));
                    }

                    // Calculate total size
                    long totalSize = 0;
                    foreach (var chunk in chunksList)
                    {
                        ValidateData(chunk);
                        totalSize += chunk.Length;
                    }

                    // Create metadata for the data
                    var metadata = new StorageMetadata
                    {
                        Key = key,
                        Size = totalSize,
                        CreationTime = DateTime.UtcNow,
                        LastModifiedTime = DateTime.UtcNow,
                        LastAccessTime = DateTime.UtcNow,
                        IsChunked = true,
                        ChunkSize = chunkSize,
                        ChunkCount = chunksList.Count
                    };

                    // Write each chunk
                    for (int i = 0; i < chunksList.Count; i++)
                    {
                        string chunkKey = GetChunkKey(key, i);
                        await WriteDataInternalAsync(chunkKey, chunksList[i]);
                    }

                    // Store the metadata
                    await WriteMetadataInternalAsync(key, metadata);

                    Logger.LogDebug("Chunked data written successfully for key {Key} ({ChunkCount} chunks)", key, chunksList.Count);
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write chunked data for key {Key}", key);
                throw new StorageException($"Failed to write chunked data for key {key}", ex);
            }
        }

        /// <summary>
        /// Writes data to storage using a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="dataStream">The stream containing the data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task WriteStreamAsync(string key, Stream dataStream)
        {
            ValidateKey(key);
            if (dataStream == null)
            {
                throw new ArgumentNullException(nameof(dataStream));
            }
            if (!dataStream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(dataStream));
            }

            Logger.LogDebug("Writing stream data for key {Key}", key);

            try
            {
                // Convert the stream to chunks
                var chunks = new List<byte[]>();
                int chunkIndex = 0;
                byte[] buffer = new byte[DefaultChunkSize];
                int bytesRead;

                while ((bytesRead = await dataStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, bytesRead);
                    chunks.Add(chunk);
                    chunkIndex++;
                }

                // Write the chunks
                await WriteChunkedAsync(key, chunks, DefaultChunkSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to write stream data for key {Key}", key);
                throw new StorageException($"Failed to write stream data for key {key}", ex);
            }
        }

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        public virtual async Task<byte[]> ReadAsync(string key)
        {
            ValidateKey(key);

            Logger.LogDebug("Reading data for key {Key}", key);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Check if the key exists
                    if (!await ExistsInternalAsync(key))
                    {
                        Logger.LogDebug("Key {Key} not found", key);
                        return null;
                    }

                    // Get the metadata
                    var metadata = await ReadMetadataInternalAsync(key);
                    if (metadata == null)
                    {
                        Logger.LogWarning("Metadata not found for key {Key}", key);
                        return null;
                    }

                    // Update the last access time
                    metadata.LastAccessTime = DateTime.UtcNow;
                    await WriteMetadataInternalAsync(key, metadata);

                    // If the data is chunked, read and combine the chunks
                    if (metadata.IsChunked)
                    {
                        return await ReadChunkedDataInternalAsync(key, metadata);
                    }

                    // Read the data
                    byte[] data = await ReadDataInternalAsync(key);
                    if (data == null)
                    {
                        Logger.LogWarning("Data not found for key {Key}", key);
                        return null;
                    }

                    // Verify the hash if available
                    if (!string.IsNullOrEmpty(metadata.Hash))
                    {
                        string hash = ComputeHash(data);
                        if (hash != metadata.Hash)
                        {
                            Logger.LogWarning("Hash mismatch for key {Key}", key);
                            throw new StorageException($"Hash mismatch for key {key}");
                        }
                    }

                    Logger.LogDebug("Data read successfully for key {Key} ({Size} bytes)", key, data.Length);
                    return data;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read data for key {Key}", key);
                throw new StorageException($"Failed to read data for key {key}", ex);
            }
        }

        /// <summary>
        /// Reads data from storage in chunks.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <returns>An enumerable of data chunks, or null if the key does not exist.</returns>
        public virtual async Task<IEnumerable<byte[]>> ReadChunkedAsync(string key, int chunkSize)
        {
            ValidateKey(key);
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than zero", nameof(chunkSize));
            }

            Logger.LogDebug("Reading chunked data for key {Key} with chunk size {ChunkSize}", key, chunkSize);

            try
            {
                // Read the full data
                byte[] data = await ReadAsync(key);
                if (data == null)
                {
                    return null;
                }

                // Split the data into chunks
                var chunks = new List<byte[]>();
                int offset = 0;
                while (offset < data.Length)
                {
                    int length = Math.Min(chunkSize, data.Length - offset);
                    byte[] chunk = new byte[length];
                    Array.Copy(data, offset, chunk, 0, length);
                    chunks.Add(chunk);
                    offset += length;
                }

                Logger.LogDebug("Chunked data read successfully for key {Key} ({ChunkCount} chunks)", key, chunks.Count);
                return chunks;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read chunked data for key {Key}", key);
                throw new StorageException($"Failed to read chunked data for key {key}", ex);
            }
        }

        /// <summary>
        /// Reads data from storage as a stream.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>A stream containing the data, or null if the key does not exist.</returns>
        public virtual async Task<Stream> ReadStreamAsync(string key)
        {
            ValidateKey(key);

            Logger.LogDebug("Reading stream data for key {Key}", key);

            try
            {
                // Read the full data
                byte[] data = await ReadAsync(key);
                if (data == null)
                {
                    return null;
                }

                // Create a memory stream from the data
                var stream = new MemoryStream(data);
                stream.Position = 0;

                Logger.LogDebug("Stream data read successfully for key {Key} ({Size} bytes)", key, data.Length);
                return stream;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read stream data for key {Key}", key);
                throw new StorageException($"Failed to read stream data for key {key}", ex);
            }
        }

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>True if the data was deleted, false if the key does not exist.</returns>
        public virtual async Task<bool> DeleteAsync(string key)
        {
            ValidateKey(key);

            Logger.LogDebug("Deleting data for key {Key}", key);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Check if the key exists
                    if (!await ExistsInternalAsync(key))
                    {
                        Logger.LogDebug("Key {Key} not found", key);
                        return false;
                    }

                    // Get the metadata
                    var metadata = await ReadMetadataInternalAsync(key);
                    if (metadata == null)
                    {
                        Logger.LogWarning("Metadata not found for key {Key}", key);
                        return false;
                    }

                    // If the data is chunked, delete each chunk
                    if (metadata.IsChunked)
                    {
                        for (int i = 0; i < metadata.ChunkCount; i++)
                        {
                            string chunkKey = GetChunkKey(key, i);
                            await DeleteDataInternalAsync(chunkKey);
                        }
                    }

                    // Delete the data and metadata
                    await DeleteDataInternalAsync(key);
                    await DeleteMetadataInternalAsync(key);

                    Logger.LogDebug("Data deleted successfully for key {Key}", key);
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete data for key {Key}", key);
                throw new StorageException($"Failed to delete data for key {key}", ex);
            }
        }

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public virtual async Task<bool> ExistsAsync(string key)
        {
            ValidateKey(key);

            Logger.LogDebug("Checking if key {Key} exists", key);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    bool exists = await ExistsInternalAsync(key);
                    Logger.LogDebug("Key {Key} {ExistsStatus}", key, exists ? "exists" : "does not exist");
                    return exists;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to check if key {Key} exists", key);
                throw new StorageException($"Failed to check if key {key} exists", ex);
            }
        }

        /// <summary>
        /// Gets all keys in storage.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        public abstract Task<IReadOnlyList<string>> GetAllKeysAsync();

        /// <summary>
        /// Gets the size of data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The size of the data in bytes, or -1 if the key does not exist.</returns>
        public virtual async Task<long> GetSizeAsync(string key)
        {
            ValidateKey(key);

            Logger.LogDebug("Getting size for key {Key}", key);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Check if the key exists
                    if (!await ExistsInternalAsync(key))
                    {
                        Logger.LogDebug("Key {Key} not found", key);
                        return -1;
                    }

                    // Get the metadata
                    var metadata = await ReadMetadataInternalAsync(key);
                    if (metadata == null)
                    {
                        Logger.LogWarning("Metadata not found for key {Key}", key);
                        return -1;
                    }

                    Logger.LogDebug("Size for key {Key} is {Size} bytes", key, metadata.Size);
                    return metadata.Size;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get size for key {Key}", key);
                throw new StorageException($"Failed to get size for key {key}", ex);
            }
        }

        /// <summary>
        /// Gets the metadata for data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The metadata for the data, or null if the key does not exist.</returns>
        public virtual async Task<StorageMetadata> GetMetadataAsync(string key)
        {
            ValidateKey(key);

            Logger.LogDebug("Getting metadata for key {Key}", key);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Check if the key exists
                    if (!await ExistsInternalAsync(key))
                    {
                        Logger.LogDebug("Key {Key} not found", key);
                        return null;
                    }

                    // Get the metadata
                    var metadata = await ReadMetadataInternalAsync(key);
                    if (metadata == null)
                    {
                        Logger.LogWarning("Metadata not found for key {Key}", key);
                        return null;
                    }

                    Logger.LogDebug("Metadata retrieved successfully for key {Key}", key);
                    return metadata;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get metadata for key {Key}", key);
                throw new StorageException($"Failed to get metadata for key {key}", ex);
            }
        }

        /// <summary>
        /// Updates the metadata for data in storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="metadata">The metadata to update.</param>
        /// <returns>True if the metadata was updated, false if the key does not exist.</returns>
        public virtual async Task<bool> UpdateMetadataAsync(string key, StorageMetadata metadata)
        {
            ValidateKey(key);
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Logger.LogDebug("Updating metadata for key {Key}", key);

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Check if the key exists
                    if (!await ExistsInternalAsync(key))
                    {
                        Logger.LogDebug("Key {Key} not found", key);
                        return false;
                    }

                    // Get the existing metadata
                    var existingMetadata = await ReadMetadataInternalAsync(key);
                    if (existingMetadata == null)
                    {
                        Logger.LogWarning("Metadata not found for key {Key}", key);
                        return false;
                    }

                    // Update the metadata
                    metadata.Key = key; // Ensure the key is correct
                    metadata.LastModifiedTime = DateTime.UtcNow;
                    await WriteMetadataInternalAsync(key, metadata);

                    Logger.LogDebug("Metadata updated successfully for key {Key}", key);
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update metadata for key {Key}", key);
                throw new StorageException($"Failed to update metadata for key {key}", ex);
            }
        }

        /// <summary>
        /// Flushes any pending writes to storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public abstract Task FlushAsync();

        /// <summary>
        /// Compacts the storage to reclaim space.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public abstract Task CompactAsync();

        /// <summary>
        /// Disposes the storage provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the storage provider.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    Semaphore.Dispose();
                }

                Disposed = true;
            }
        }

        /// <summary>
        /// Validates a key.
        /// </summary>
        /// <param name="key">The key to validate.</param>
        protected virtual void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
        }

        /// <summary>
        /// Validates data.
        /// </summary>
        /// <param name="data">The data to validate.</param>
        protected virtual void ValidateData(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
        }

        /// <summary>
        /// Gets a key for a chunk.
        /// </summary>
        /// <param name="key">The base key.</param>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <returns>The chunk key.</returns>
        protected virtual string GetChunkKey(string key, int chunkIndex)
        {
            return $"{key}_chunk_{chunkIndex}";
        }

        /// <summary>
        /// Computes a hash for data.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>The hash as a string.</returns>
        protected virtual string ComputeHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Reads chunked data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="metadata">The metadata for the data.</param>
        /// <returns>The combined data from all chunks.</returns>
        protected virtual async Task<byte[]> ReadChunkedDataInternalAsync(string key, StorageMetadata metadata)
        {
            // Create a buffer for the combined data
            byte[] data = new byte[metadata.Size];
            int offset = 0;

            // Read each chunk and copy it to the buffer
            for (int i = 0; i < metadata.ChunkCount; i++)
            {
                string chunkKey = GetChunkKey(key, i);
                byte[] chunk = await ReadDataInternalAsync(chunkKey);
                if (chunk == null)
                {
                    Logger.LogWarning("Chunk {ChunkIndex} not found for key {Key}", i, key);
                    throw new StorageException($"Chunk {i} not found for key {key}");
                }

                Array.Copy(chunk, 0, data, offset, chunk.Length);
                offset += chunk.Length;
            }

            return data;
        }

        /// <summary>
        /// Writes data to storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected abstract Task WriteDataInternalAsync(string key, byte[] data);

        /// <summary>
        /// Reads data from storage.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>The data, or null if the key does not exist.</returns>
        protected abstract Task<byte[]> ReadDataInternalAsync(string key);

        /// <summary>
        /// Deletes data from storage.
        /// </summary>
        /// <param name="key">The key for the data to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected abstract Task DeleteDataInternalAsync(string key);

        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        protected abstract Task<bool> ExistsInternalAsync(string key);

        /// <summary>
        /// Writes metadata to storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <param name="metadata">The metadata to write.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected abstract Task WriteMetadataInternalAsync(string key, StorageMetadata metadata);

        /// <summary>
        /// Reads metadata from storage.
        /// </summary>
        /// <param name="key">The key for the metadata.</param>
        /// <returns>The metadata, or null if the key does not exist.</returns>
        protected abstract Task<StorageMetadata> ReadMetadataInternalAsync(string key);

        /// <summary>
        /// Deletes metadata from storage.
        /// </summary>
        /// <param name="key">The key for the metadata to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected abstract Task DeleteMetadataInternalAsync(string key);
    }
}
