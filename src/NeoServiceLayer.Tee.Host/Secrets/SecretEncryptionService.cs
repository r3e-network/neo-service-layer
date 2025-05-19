using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage;
using HostStorageManager = NeoServiceLayer.Tee.Host.Storage.IStorageManager;
using NeoServiceLayer.Tee.Shared.Secrets;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Secrets
{
    /// <summary>
    /// Service for encrypting and decrypting secrets.
    /// </summary>
    public class SecretEncryptionService : ISecretEncryptionService
    {
        private readonly ILogger<SecretEncryptionService> _logger;
        private readonly HostStorageManager _storageManager;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, byte[]> _encryptionKeys;
        private string _currentKeyId;
        private bool _initialized;
        private bool _disposed;

        // Constants
        private const int KEY_SIZE_BYTES = 32; // 256 bits
        private const int IV_SIZE_BYTES = 16; // 128 bits
        private const int KEY_ID_SIZE_BYTES = 16; // 128 bits
        private const string CURRENT_KEY_ID_KEY = "current_key_id";

        /// <summary>
        /// Initializes a new instance of the SecretEncryptionService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="storageManager">The storage manager.</param>
        public SecretEncryptionService(
            ILogger<SecretEncryptionService> logger,
            HostStorageManager storageManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _semaphore = new SemaphoreSlim(1, 1);
            _encryptionKeys = new ConcurrentDictionary<string, byte[]>();
            _currentKeyId = null;
            _initialized = false;
            _disposed = false;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_initialized)
                    {
                        return true;
                    }

                    // Initialize storage provider for encryption keys
                    var storageProvider = _storageManager.GetProvider("encryption_keys");
                    if (storageProvider == null)
                    {
                        // Create a new storage provider for encryption keys
                        storageProvider = await _storageManager.CreateProviderAsync(
                            "encryption_keys",
                            StorageProviderType.File,
                            new FileStorageOptions { StorageDirectory = "encryption_keys" });

                        if (storageProvider == null)
                        {
                            _logger.LogError("Failed to create storage provider for encryption keys");
                            return false;
                        }
                    }

                    // Load encryption keys from storage
                    if (!await LoadEncryptionKeysAsync())
                    {
                        _logger.LogError("Failed to load encryption keys from storage");
                        return false;
                    }

                    // If no encryption keys exist, generate a new one
                    if (_encryptionKeys.IsEmpty)
                    {
                        await GenerateNewKeyAsync();
                    }

                    _initialized = true;
                    _logger.LogInformation("Secret encryption service initialized with {KeyCount} keys", _encryptionKeys.Count);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing secret encryption service");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> EncryptAsync(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return await EncryptAsync(Encoding.UTF8.GetBytes(value));
        }

        /// <inheritdoc/>
        public async Task<byte[]> EncryptAsync(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get the current encryption key
                var keyId = _currentKeyId;
                var key = _encryptionKeys[keyId];

                // Generate a random IV
                var iv = new byte[IV_SIZE_BYTES];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

                // Encrypt the value
                byte[] encryptedValue;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        // Write the key ID
                        ms.Write(Encoding.UTF8.GetBytes(keyId));

                        // Write the IV
                        ms.Write(iv);

                        // Encrypt the value
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(value, 0, value.Length);
                            cs.FlushFinalBlock();
                        }

                        encryptedValue = ms.ToArray();
                    }
                }

                return encryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting value");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> DecryptToStringAsync(byte[] encryptedValue)
        {
            if (encryptedValue == null)
            {
                throw new ArgumentNullException(nameof(encryptedValue));
            }

            var decryptedBytes = await DecryptToBytesAsync(encryptedValue);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <inheritdoc/>
        public async Task<byte[]> DecryptToBytesAsync(byte[] encryptedValue)
        {
            if (encryptedValue == null)
            {
                throw new ArgumentNullException(nameof(encryptedValue));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Extract the key ID
                var keyId = Encoding.UTF8.GetString(encryptedValue, 0, KEY_ID_SIZE_BYTES);

                // Get the encryption key
                if (!_encryptionKeys.TryGetValue(keyId, out var key))
                {
                    throw new InvalidOperationException($"Encryption key not found: {keyId}");
                }

                // Extract the IV
                var iv = new byte[IV_SIZE_BYTES];
                Array.Copy(encryptedValue, KEY_ID_SIZE_BYTES, iv, 0, IV_SIZE_BYTES);

                // Decrypt the value
                byte[] decryptedValue;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedValue, KEY_ID_SIZE_BYTES + IV_SIZE_BYTES, encryptedValue.Length - KEY_ID_SIZE_BYTES - IV_SIZE_BYTES);
                            cs.FlushFinalBlock();
                        }

                        decryptedValue = ms.ToArray();
                    }
                }

                return decryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting value");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RotateEncryptionKeyAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Generate a new encryption key
                    var newKeyId = await GenerateNewKeyAsync();

                    // Set the new key as the current key
                    _currentKeyId = newKeyId;

                    // Save the current key ID to storage
                    await SaveCurrentKeyIdAsync();

                    _logger.LogInformation("Rotated encryption key to {KeyId}", newKeyId);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating encryption key");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReEncryptAsync(byte[] encryptedValue)
        {
            if (encryptedValue == null)
            {
                throw new ArgumentNullException(nameof(encryptedValue));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Decrypt the value
                var decryptedValue = await DecryptToBytesAsync(encryptedValue);

                // Re-encrypt the value with the current key
                return await EncryptAsync(decryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-encrypting value");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetCurrentKeyIdAsync()
        {
            CheckDisposed();
            CheckInitialized();

            return _currentKeyId;
        }

        /// <inheritdoc/>
        public async Task<string> GetKeyIdForValueAsync(byte[] encryptedValue)
        {
            if (encryptedValue == null)
            {
                throw new ArgumentNullException(nameof(encryptedValue));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Extract the key ID
                return Encoding.UTF8.GetString(encryptedValue, 0, KEY_ID_SIZE_BYTES);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key ID for value");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> NeedsReEncryptionAsync(byte[] encryptedValue)
        {
            if (encryptedValue == null)
            {
                throw new ArgumentNullException(nameof(encryptedValue));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Extract the key ID
                var keyId = await GetKeyIdForValueAsync(encryptedValue);

                // Check if the key ID is the current key ID
                return keyId != _currentKeyId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if value needs re-encryption");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateNewKeyAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Generate a new key ID
                    var keyId = Guid.NewGuid().ToString("N").Substring(0, KEY_ID_SIZE_BYTES);

                    // Generate a new encryption key
                    var key = new byte[KEY_SIZE_BYTES];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(key);
                    }

                    // Store the key
                    _encryptionKeys[keyId] = key;

                    // If this is the first key, set it as the current key
                    if (_currentKeyId == null)
                    {
                        _currentKeyId = keyId;
                        await SaveCurrentKeyIdAsync();
                    }

                    // Save the key to storage
                    await SaveEncryptionKeyAsync(keyId, key);

                    _logger.LogInformation("Generated new encryption key: {KeyId}", keyId);
                    return keyId;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating new encryption key");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteKeyAsync(string keyId)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Check if the key exists
                    if (!_encryptionKeys.ContainsKey(keyId))
                    {
                        _logger.LogWarning("Encryption key not found: {KeyId}", keyId);
                        return false;
                    }

                    // Check if the key is the current key
                    if (keyId == _currentKeyId)
                    {
                        _logger.LogWarning("Cannot delete the current encryption key: {KeyId}", keyId);
                        return false;
                    }

                    // Remove the key from memory
                    if (!_encryptionKeys.TryRemove(keyId, out _))
                    {
                        _logger.LogError("Failed to remove encryption key from memory: {KeyId}", keyId);
                        return false;
                    }

                    // Delete the key from storage
                    var storageProvider = _storageManager.GetProvider("encryption_keys");
                    if (storageProvider == null)
                    {
                        _logger.LogError("Storage provider for encryption keys not found");
                        return false;
                    }

                    if (!await storageProvider.DeleteAsync(keyId))
                    {
                        _logger.LogError("Failed to delete encryption key from storage: {KeyId}", keyId);
                        return false;
                    }

                    _logger.LogInformation("Deleted encryption key: {KeyId}", keyId);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting encryption key: {KeyId}", keyId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetKeyCountAsync()
        {
            CheckDisposed();
            CheckInitialized();

            return _encryptionKeys.Count;
        }

        /// <inheritdoc/>
        public async Task<string[]> GetKeyIdsAsync()
        {
            CheckDisposed();
            CheckInitialized();

            return _encryptionKeys.Keys.ToArray();
        }

        /// <inheritdoc/>
        public async Task<DateTime> GetKeyCreationTimeAsync(string keyId)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get the key metadata
                var storageProvider = _storageManager.GetProvider("encryption_keys");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for encryption keys not found");
                    throw new InvalidOperationException("Storage provider for encryption keys not found");
                }

                // Get the key metadata
                var metadataKey = $"{keyId}_metadata";
                var metadataBytes = await storageProvider.ReadAsync(metadataKey);
                if (metadataBytes == null || metadataBytes.Length == 0)
                {
                    _logger.LogWarning("Encryption key metadata not found: {KeyId}", keyId);
                    return DateTime.MinValue;
                }

                // Deserialize the metadata
                var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(metadataBytes));
                if (metadata.TryGetValue("created_at", out var createdAtStr) && DateTime.TryParse(createdAtStr, out var createdAt))
                {
                    return createdAt;
                }

                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key creation time: {KeyId}", keyId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DateTime?> GetKeyLastRotationTimeAsync(string keyId)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get the key metadata
                var storageProvider = _storageManager.GetProvider("encryption_keys");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for encryption keys not found");
                    throw new InvalidOperationException("Storage provider for encryption keys not found");
                }

                // Get the key metadata
                var metadataKey = $"{keyId}_metadata";
                var metadataBytes = await storageProvider.ReadAsync(metadataKey);
                if (metadataBytes == null || metadataBytes.Length == 0)
                {
                    _logger.LogWarning("Encryption key metadata not found: {KeyId}", keyId);
                    return null;
                }

                // Deserialize the metadata
                var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(metadataBytes));
                if (metadata.TryGetValue("last_rotation", out var lastRotationStr) && DateTime.TryParse(lastRotationStr, out var lastRotation))
                {
                    return lastRotation;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key last rotation time: {KeyId}", keyId);
                throw;
            }
        }

        /// <summary>
        /// Saves an encryption key to storage.
        /// </summary>
        /// <param name="keyId">The key ID.</param>
        /// <param name="key">The encryption key.</param>
        /// <returns>True if the key was saved successfully, false otherwise.</returns>
        private async Task<bool> SaveEncryptionKeyAsync(string keyId, byte[] key)
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("encryption_keys");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for encryption keys not found");
                    return false;
                }

                // Save the key
                if (!await storageProvider.WriteAsync(keyId, key))
                {
                    _logger.LogError("Failed to save encryption key to storage: {KeyId}", keyId);
                    return false;
                }

                // Save the key metadata
                var metadata = new Dictionary<string, string>
                {
                    ["created_at"] = DateTime.UtcNow.ToString("o")
                };

                var metadataBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata));
                var metadataKey = $"{keyId}_metadata";
                if (!await storageProvider.WriteAsync(metadataKey, metadataBytes))
                {
                    _logger.LogError("Failed to save encryption key metadata to storage: {KeyId}", keyId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving encryption key to storage: {KeyId}", keyId);
                return false;
            }
        }

        /// <summary>
        /// Saves the current key ID to storage.
        /// </summary>
        /// <returns>True if the current key ID was saved successfully, false otherwise.</returns>
        private async Task<bool> SaveCurrentKeyIdAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("encryption_keys");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for encryption keys not found");
                    return false;
                }

                // Save the current key ID
                var currentKeyIdBytes = Encoding.UTF8.GetBytes(_currentKeyId);
                return await storageProvider.WriteAsync(CURRENT_KEY_ID_KEY, currentKeyIdBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving current key ID to storage");
                return false;
            }
        }

        /// <summary>
        /// Loads encryption keys from storage.
        /// </summary>
        /// <returns>True if encryption keys were loaded successfully, false otherwise.</returns>
        private async Task<bool> LoadEncryptionKeysAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("encryption_keys");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for encryption keys not found");
                    return false;
                }

                // Get all keys
                var keys = await storageProvider.GetAllKeysAsync();

                // Load all encryption keys
                foreach (var key in keys)
                {
                    // Skip metadata keys and the current key ID key
                    if (key.EndsWith("_metadata") || key == CURRENT_KEY_ID_KEY)
                    {
                        continue;
                    }

                    // Load the encryption key
                    var encryptionKey = await storageProvider.ReadAsync(key);
                    if (encryptionKey == null || encryptionKey.Length == 0)
                    {
                        _logger.LogWarning("Empty encryption key for key ID: {KeyId}", key);
                        continue;
                    }

                    // Store the encryption key in memory
                    _encryptionKeys[key] = encryptionKey;
                }

                // Load the current key ID
                var currentKeyIdBytes = await storageProvider.ReadAsync(CURRENT_KEY_ID_KEY);
                if (currentKeyIdBytes != null && currentKeyIdBytes.Length > 0)
                {
                    _currentKeyId = Encoding.UTF8.GetString(currentKeyIdBytes);
                }
                else if (_encryptionKeys.Count > 0)
                {
                    // If no current key ID is stored, use the first key
                    _currentKeyId = _encryptionKeys.Keys.First();
                    await SaveCurrentKeyIdAsync();
                }

                _logger.LogInformation("Loaded {KeyCount} encryption keys", _encryptionKeys.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading encryption keys from storage");
                return false;
            }
        }

        /// <summary>
        /// Checks if the service is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Secret encryption service is not initialized");
            }
        }

        /// <summary>
        /// Checks if the service is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecretEncryptionService));
            }
        }

        /// <summary>
        /// Disposes the service.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the service.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the service.
        /// </summary>
        ~SecretEncryptionService()
        {
            Dispose(false);
        }
    }
}
