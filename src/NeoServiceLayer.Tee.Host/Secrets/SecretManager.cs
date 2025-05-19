using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Manager for user secrets.
    /// </summary>
    public class SecretManager : ISecretManager
    {
        private readonly ILogger<SecretManager> _logger;
        private readonly HostStorageManager _storageManager;
        private readonly ISecretEncryptionService _encryptionService;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> _userSecrets;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the SecretManager class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="storageManager">The storage manager.</param>
        /// <param name="encryptionService">The encryption service.</param>
        public SecretManager(
            ILogger<SecretManager> logger,
            HostStorageManager storageManager,
            ISecretEncryptionService encryptionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _semaphore = new SemaphoreSlim(1, 1);
            _userSecrets = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>>();
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

                    // Initialize encryption service
                    if (!await _encryptionService.InitializeAsync())
                    {
                        _logger.LogError("Failed to initialize encryption service");
                        return false;
                    }

                    // Initialize storage provider for secrets
                    var storageProvider = _storageManager.GetProvider("secrets");
                    if (storageProvider == null)
                    {
                        // Create a new storage provider for secrets
                        storageProvider = await _storageManager.CreateProviderAsync(
                            "secrets",
                            StorageProviderType.File,
                            new FileStorageOptions { StorageDirectory = "secrets" });

                        if (storageProvider == null)
                        {
                            _logger.LogError("Failed to create storage provider for secrets");
                            return false;
                        }
                    }

                    // Load secrets from storage
                    if (!await LoadSecretsAsync())
                    {
                        _logger.LogError("Failed to load secrets from storage");
                        return false;
                    }

                    _initialized = true;
                    _logger.LogInformation("Secret manager initialized with {UserCount} users", _userSecrets.Count);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing secret manager");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> StoreSecretAsync(string userId, string secretName, string secretValue)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
            }

            if (secretValue == null)
            {
                throw new ArgumentNullException(nameof(secretValue));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Encrypt the secret value
                byte[] encryptedValue = await _encryptionService.EncryptAsync(secretValue);

                // Store the encrypted secret
                var userSecrets = _userSecrets.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte[]>());
                userSecrets[secretName] = encryptedValue;

                // Save to storage
                await SaveSecretAsync(userId, secretName, encryptedValue);

                _logger.LogInformation("Stored secret for user {UserId}: {SecretName}", userId, secretName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secret for user {UserId}: {SecretName}", userId, secretName);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string userId, string secretName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get the encrypted secret
                if (!_userSecrets.TryGetValue(userId, out var userSecrets))
                {
                    _logger.LogWarning("No secrets found for user {UserId}", userId);
                    return null;
                }

                if (!userSecrets.TryGetValue(secretName, out var encryptedValue))
                {
                    _logger.LogWarning("Secret not found for user {UserId}: {SecretName}", userId, secretName);
                    return null;
                }

                // Check if the secret needs to be re-encrypted
                if (await _encryptionService.NeedsReEncryptionAsync(encryptedValue))
                {
                    // Re-encrypt the secret
                    encryptedValue = await _encryptionService.ReEncryptAsync(encryptedValue);

                    // Update the secret in memory
                    userSecrets[secretName] = encryptedValue;

                    // Save to storage
                    await SaveSecretAsync(userId, secretName, encryptedValue);
                }

                // Decrypt the secret value
                string decryptedValue = await _encryptionService.DecryptToStringAsync(encryptedValue);

                _logger.LogInformation("Retrieved secret for user {UserId}: {SecretName}", userId, secretName);
                return decryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret for user {UserId}: {SecretName}", userId, secretName);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>> GetSecretsAsync(string userId, IEnumerable<string> secretNames)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (secretNames == null)
            {
                throw new ArgumentNullException(nameof(secretNames));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                var result = new Dictionary<string, string>();

                foreach (var secretName in secretNames)
                {
                    var secretValue = await GetSecretAsync(userId, secretName);
                    if (secretValue != null)
                    {
                        result[secretName] = secretValue;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secrets for user {UserId}", userId);
                return new Dictionary<string, string>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteSecretAsync(string userId, string secretName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Check if the user exists
                if (!_userSecrets.TryGetValue(userId, out var userSecrets))
                {
                    _logger.LogWarning("No secrets found for user {UserId}", userId);
                    return false;
                }

                // Check if the secret exists
                if (!userSecrets.TryGetValue(secretName, out _))
                {
                    _logger.LogWarning("Secret not found for user {UserId}: {SecretName}", userId, secretName);
                    return false;
                }

                // Remove the secret
                if (!userSecrets.TryRemove(secretName, out _))
                {
                    _logger.LogError("Failed to remove secret for user {UserId}: {SecretName}", userId, secretName);
                    return false;
                }

                // If the user has no more secrets, remove the user
                if (userSecrets.IsEmpty)
                {
                    _userSecrets.TryRemove(userId, out _);
                }

                // Delete from storage
                await DeleteSecretFromStorageAsync(userId, secretName);

                _logger.LogInformation("Deleted secret for user {UserId}: {SecretName}", userId, secretName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret for user {UserId}: {SecretName}", userId, secretName);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> GetSecretNamesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Check if the user exists
                if (!_userSecrets.TryGetValue(userId, out var userSecrets))
                {
                    _logger.LogWarning("No secrets found for user {UserId}", userId);
                    return new List<string>();
                }

                // Get all secret names
                var secretNames = userSecrets.Keys.ToList();

                _logger.LogInformation("Retrieved {SecretCount} secret names for user {UserId}", secretNames.Count, userId);
                return secretNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret names for user {UserId}", userId);
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SecretExistsAsync(string userId, string secretName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Check if the user exists
                if (!_userSecrets.TryGetValue(userId, out var userSecrets))
                {
                    return false;
                }

                // Check if the secret exists
                return userSecrets.ContainsKey(secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if secret exists for user {UserId}: {SecretName}", userId, secretName);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateSecretAsync(string userId, string secretName, string secretValue)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
            }

            if (secretValue == null)
            {
                throw new ArgumentNullException(nameof(secretValue));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Check if the secret exists
                if (!await SecretExistsAsync(userId, secretName))
                {
                    _logger.LogWarning("Secret not found for user {UserId}: {SecretName}", userId, secretName);
                    return false;
                }

                // Store the secret (this will overwrite the existing one)
                return await StoreSecretAsync(userId, secretName, secretValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret for user {UserId}: {SecretName}", userId, secretName);
                return false;
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
                    // Rotate the encryption key
                    if (!await _encryptionService.RotateEncryptionKeyAsync())
                    {
                        _logger.LogError("Failed to rotate encryption key");
                        return false;
                    }

                    // Re-encrypt all secrets
                    return await ReEncryptSecretsAsync();
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
        public async Task<bool> ReEncryptSecretsAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Re-encrypt all secrets
                    foreach (var userId in _userSecrets.Keys)
                    {
                        var userSecrets = _userSecrets[userId];
                        foreach (var secretName in userSecrets.Keys)
                        {
                            var encryptedValue = userSecrets[secretName];

                            // Check if the secret needs to be re-encrypted
                            if (await _encryptionService.NeedsReEncryptionAsync(encryptedValue))
                            {
                                // Re-encrypt the secret
                                encryptedValue = await _encryptionService.ReEncryptAsync(encryptedValue);

                                // Update the secret in memory
                                userSecrets[secretName] = encryptedValue;

                                // Save to storage
                                await SaveSecretAsync(userId, secretName, encryptedValue);
                            }
                        }
                    }

                    _logger.LogInformation("Re-encrypted all secrets");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-encrypting secrets");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetSecretCountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Check if the user exists
                if (!_userSecrets.TryGetValue(userId, out var userSecrets))
                {
                    return 0;
                }

                return userSecrets.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret count for user {UserId}", userId);
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetTotalSecretCountAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                int totalCount = 0;
                foreach (var userSecrets in _userSecrets.Values)
                {
                    totalCount += userSecrets.Count;
                }
                return totalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total secret count");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> GetUserIdsAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                return _userSecrets.Keys.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user IDs");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ClearUserSecretsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Check if the user exists
                if (!_userSecrets.TryGetValue(userId, out var userSecrets))
                {
                    _logger.LogWarning("No secrets found for user {UserId}", userId);
                    return false;
                }

                // Get all secret names
                var secretNames = userSecrets.Keys.ToList();

                // Delete all secrets
                foreach (var secretName in secretNames)
                {
                    await DeleteSecretAsync(userId, secretName);
                }

                _logger.LogInformation("Cleared all secrets for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing secrets for user {UserId}", userId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ClearAllSecretsAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Get all user IDs
                    var userIds = _userSecrets.Keys.ToList();

                    // Clear all secrets for all users
                    foreach (var userId in userIds)
                    {
                        await ClearUserSecretsAsync(userId);
                    }

                    _logger.LogInformation("Cleared all secrets for all users");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all secrets");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>> ExportUserSecretsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get all secret names
                var secretNames = await GetSecretNamesAsync(userId);

                // Get all secrets
                return await GetSecretsAsync(userId, secretNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting secrets for user {UserId}", userId);
                return new Dictionary<string, string>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ImportUserSecretsAsync(string userId, Dictionary<string, string> secrets, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (secrets == null)
            {
                throw new ArgumentNullException(nameof(secrets));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Import all secrets
                foreach (var secret in secrets)
                {
                    // Check if the secret exists
                    bool exists = await SecretExistsAsync(userId, secret.Key);

                    // Skip if the secret exists and overwrite is false
                    if (exists && !overwrite)
                    {
                        continue;
                    }

                    // Store the secret
                    await StoreSecretAsync(userId, secret.Key, secret.Value);
                }

                _logger.LogInformation("Imported {SecretCount} secrets for user {UserId}", secrets.Count, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing secrets for user {UserId}", userId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> BackupSecretsAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Create a backup object
                    var backup = new Dictionary<string, Dictionary<string, string>>();

                    // Export all secrets for all users
                    foreach (var userId in _userSecrets.Keys)
                    {
                        var userSecrets = await ExportUserSecretsAsync(userId);
                        backup[userId] = userSecrets;
                    }

                    // Serialize the backup object
                    var json = JsonSerializer.Serialize(backup);

                    // Write to file
                    await File.WriteAllTextAsync(filePath, json);

                    _logger.LogInformation("Backed up all secrets to {FilePath}", filePath);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up secrets to {FilePath}", filePath);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RestoreSecretsAsync(string filePath, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Read the backup file
                    var json = await File.ReadAllTextAsync(filePath);

                    // Deserialize the backup object
                    var backup = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

                    // Import all secrets for all users
                    foreach (var userId in backup.Keys)
                    {
                        await ImportUserSecretsAsync(userId, backup[userId], overwrite);
                    }

                    _logger.LogInformation("Restored all secrets from {FilePath}", filePath);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring secrets from {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Saves a secret to storage.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="encryptedValue">The encrypted value of the secret.</param>
        /// <returns>True if the secret was saved successfully, false otherwise.</returns>
        private async Task<bool> SaveSecretAsync(string userId, string secretName, byte[] encryptedValue)
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("secrets");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for secrets not found");
                    return false;
                }

                // Create a storage key for the secret
                string storageKey = $"{userId}:{secretName}";

                // Store the encrypted value
                return await storageProvider.WriteAsync(storageKey, encryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving secret for user {UserId}: {SecretName}", userId, secretName);
                return false;
            }
        }

        /// <summary>
        /// Deletes a secret from storage.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <returns>True if the secret was deleted successfully, false otherwise.</returns>
        private async Task<bool> DeleteSecretFromStorageAsync(string userId, string secretName)
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("secrets");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for secrets not found");
                    return false;
                }

                // Create a storage key for the secret
                string storageKey = $"{userId}:{secretName}";

                // Delete the secret
                return await storageProvider.DeleteAsync(storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret from storage for user {UserId}: {SecretName}", userId, secretName);
                return false;
            }
        }

        /// <summary>
        /// Loads secrets from storage.
        /// </summary>
        /// <returns>True if secrets were loaded successfully, false otherwise.</returns>
        private async Task<bool> LoadSecretsAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("secrets");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for secrets not found");
                    return false;
                }

                // Get all keys
                var keys = await storageProvider.GetAllKeysAsync();

                // Load all secrets
                foreach (var key in keys)
                {
                    try
                    {
                        // Parse the key to get the user ID and secret name
                        var parts = key.Split(':', 2);
                        if (parts.Length != 2)
                        {
                            _logger.LogWarning("Invalid secret key format: {Key}", key);
                            continue;
                        }

                        string userId = parts[0];
                        string secretName = parts[1];

                        // Get the encrypted value
                        byte[] encryptedValue = await storageProvider.ReadAsync(key);
                        if (encryptedValue == null || encryptedValue.Length == 0)
                        {
                            _logger.LogWarning("Empty secret value for key: {Key}", key);
                            continue;
                        }

                        // Store the secret in memory
                        var userSecrets = _userSecrets.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte[]>());
                        userSecrets[secretName] = encryptedValue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading secret for key: {Key}", key);
                    }
                }

                _logger.LogInformation("Loaded {SecretCount} secrets for {UserCount} users",
                    _userSecrets.Values.Sum(us => us.Count), _userSecrets.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading secrets from storage");
                return false;
            }
        }

        /// <summary>
        /// Checks if the manager is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Secret manager is not initialized");
            }
        }

        /// <summary>
        /// Checks if the manager is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecretManager));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the manager.
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
        /// Finalizes the manager.
        /// </summary>
        ~SecretManager()
        {
            Dispose(false);
        }
    }
}
