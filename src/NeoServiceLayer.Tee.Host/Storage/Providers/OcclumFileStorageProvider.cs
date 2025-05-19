using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// A persistent storage provider that uses Occlum's file system for storage.
    /// </summary>
    public class OcclumFileStorageProvider : BasePersistentStorageProvider
    {
        private readonly OcclumFileStorageOptions _options;
        private readonly string _storagePath;
        private readonly string _metadataPath;
        private readonly ILogger<OcclumFileStorageProvider> _logger;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumFileStorageProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options for the storage provider.</param>
        public OcclumFileStorageProvider(
            ILogger<OcclumFileStorageProvider> logger,
            OcclumFileStorageOptions? options = null)
            : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new OcclumFileStorageOptions();

            // Ensure the storage path is within the Occlum file system
            if (!_options.StoragePath.StartsWith("/"))
            {
                _storagePath = $"/{_options.StoragePath}";
            }
            else
            {
                _storagePath = _options.StoragePath;
            }

            _metadataPath = Path.Combine(_storagePath, ".metadata");
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
                _logger.LogInformation("Initializing Occlum file storage provider at {StoragePath}", _storagePath);

                // Ensure the storage directory exists
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                    _logger.LogInformation("Created storage directory at {StoragePath}", _storagePath);
                }

                // Ensure the metadata directory exists
                if (!Directory.Exists(_metadataPath))
                {
                    Directory.CreateDirectory(_metadataPath);
                    _logger.LogInformation("Created metadata directory at {MetadataPath}", _metadataPath);
                }

                _initialized = true;
                _logger.LogInformation("Occlum file storage provider initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Occlum file storage provider");
                return false;
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> WriteInternalAsync(string key, byte[] data)
        {
            EnsureInitialized();
            ValidateKey(key);

            string filePath = GetFilePath(key);
            string metadataPath = GetMetadataPath(key);

            try
            {
                // Create the directory if it doesn't exist
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the data to the file
                await File.WriteAllBytesAsync(filePath, data);

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

            string filePath = GetFilePath(key);

            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Key {Key} does not exist", key);
                    return null;
                }

                // Read the data from the file
                byte[] data = await File.ReadAllBytesAsync(filePath);

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

            string filePath = GetFilePath(key);
            string metadataPath = GetMetadataPath(key);

            try
            {
                bool fileExists = File.Exists(filePath);
                bool metadataExists = File.Exists(metadataPath);

                if (!fileExists && !metadataExists)
                {
                    _logger.LogWarning("Key {Key} does not exist", key);
                    return false;
                }

                // Delete the file if it exists
                if (fileExists)
                {
                    File.Delete(filePath);
                }

                // Delete the metadata if it exists
                if (metadataExists)
                {
                    File.Delete(metadataPath);
                }

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

            string filePath = GetFilePath(key);
            return File.Exists(filePath);
        }

        /// <inheritdoc/>
        protected override async Task<IReadOnlyList<string>> GetAllKeysInternalAsync()
        {
            EnsureInitialized();

            try
            {
                var keys = new List<string>();
                var files = Directory.GetFiles(_storagePath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Skip metadata files
                    if (file.StartsWith(_metadataPath))
                    {
                        continue;
                    }

                    // Convert the file path to a key
                    string key = file.Substring(_storagePath.Length).TrimStart('/').Replace('\\', '/');
                    keys.Add(key);
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

            string metadataPath = GetMetadataPath(key);

            try
            {
                if (!File.Exists(metadataPath))
                {
                    _logger.LogWarning("Metadata for key {Key} does not exist", key);
                    return null;
                }

                // Read the metadata from the file
                string json = await File.ReadAllTextAsync(metadataPath);
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

            string metadataPath = GetMetadataPath(key);

            try
            {
                // Create the directory if it doesn't exist
                string directory = Path.GetDirectoryName(metadataPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the metadata to the file
                string json = System.Text.Json.JsonSerializer.Serialize(metadata);
                await File.WriteAllTextAsync(metadataPath, json);

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
            // No-op for file storage
            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> CompactInternalAsync()
        {
            // No-op for file storage
            return true;
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
            // No-op for file storage
            _logger.LogDebug("Committing transaction {TransactionId}", transactionId);
            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> RollbackTransactionInternalAsync(string transactionId)
        {
            // No-op for file storage
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
                // No resources to dispose
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

            // Ensure the key doesn't contain invalid characters
            if (key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("Key contains invalid characters", nameof(key));
            }
        }

        private string GetFilePath(string key)
        {
            // Replace forward slashes with the platform-specific directory separator
            string normalizedKey = key.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_storagePath, normalizedKey);
        }

        private string GetMetadataPath(string key)
        {
            // Replace forward slashes with the platform-specific directory separator
            string normalizedKey = key.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_metadataPath, normalizedKey + ".metadata");
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
