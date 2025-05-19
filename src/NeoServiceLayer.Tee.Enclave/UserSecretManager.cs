using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Manages user secrets in the Occlum enclave.
    /// </summary>
    public class UserSecretManager
    {
        private readonly ILogger<UserSecretManager> _logger;
        private readonly Dictionary<string, Dictionary<string, string>> _userSecrets;
        private readonly byte[] _encryptionKey;
        private readonly IPersistentStorageService _storageService;
        private readonly IOcclumInterface _occlumInterface;
        private const string SecretKeyPrefix = "user_secret:";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserSecretManager"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="storageService">The persistent storage service.</param>
        /// <param name="occlumInterface">The Occlum interface.</param>
        public UserSecretManager(
            ILogger<UserSecretManager> logger,
            IPersistentStorageService storageService,
            IOcclumInterface occlumInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _userSecrets = new Dictionary<string, Dictionary<string, string>>();

            // Generate a random encryption key for this enclave instance
            using (var rng = RandomNumberGenerator.Create())
            {
                _encryptionKey = new byte[32];
                rng.GetBytes(_encryptionKey);
            }
            
            // Initialize the storage service
            InitializeStorageAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes the storage service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeStorageAsync()
        {
            try
            {
                // Check if key exists and load from storage if it does
                bool keyExists = await _storageService.ExistsAsync("enclave_encryption_key");
                if (keyExists)
                {
                    byte[] storedKey = await _storageService.ReadAsync("enclave_encryption_key");
                    if (storedKey != null && storedKey.Length == 32)
                    {
                        Array.Copy(storedKey, _encryptionKey, 32);
                        _logger.LogInformation("Loaded encryption key from storage");
                    }
                }
                else
                {
                    // Store the new key
                    await _storageService.WriteAsync("enclave_encryption_key", _encryptionKey);
                    _logger.LogInformation("Generated and stored new encryption key");
                }

                // Pre-load secrets from storage for better performance
                await PreloadSecretsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing secret storage");
                throw;
            }
        }

        /// <summary>
        /// Preloads secrets from storage into memory.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PreloadSecretsAsync()
        {
            try
            {
                var keyPrefix = SecretKeyPrefix;
                var keys = await _storageService.ListKeysAsync(keyPrefix);

                foreach (var key in keys)
                {
                    // Key format: user_secret:{userId}:{secretName}
                    var parts = key.Split(new[] { ':' }, 3);
                    if (parts.Length == 3)
                    {
                        var userId = parts[1];
                        var secretName = parts[2];
                        
                        // Read from storage
                        var encryptedBytes = await _storageService.ReadAsync(key);
                        if (encryptedBytes != null)
                        {
                            var encryptedValue = Convert.ToBase64String(encryptedBytes);
                            
                            // Store in memory
                            if (!_userSecrets.TryGetValue(userId, out var userSecretDict))
                            {
                                userSecretDict = new Dictionary<string, string>();
                                _userSecrets[userId] = userSecretDict;
                            }
                            
                            userSecretDict[secretName] = encryptedValue;
                        }
                    }
                }

                _logger.LogInformation("Preloaded {Count} secrets from storage", keys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preloading secrets from storage");
            }
        }

        /// <summary>
        /// Stores a user secret.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <param name="secretValue">The secret value.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StoreSecretAsync(string userId, string secretName, string secretValue)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentNullException(nameof(secretName));
            }

            if (secretValue == null)
            {
                throw new ArgumentNullException(nameof(secretValue));
            }

            try
            {
                // Encrypt the secret value
                var encryptedValue = EncryptValue(secretValue);

                // Store the encrypted secret in memory
                if (!_userSecrets.TryGetValue(userId, out var userSecretDict))
                {
                    userSecretDict = new Dictionary<string, string>();
                    _userSecrets[userId] = userSecretDict;
                }

                userSecretDict[secretName] = encryptedValue;
                
                // Store in persistent storage
                string key = $"{SecretKeyPrefix}{userId}:{secretName}";
                byte[] encryptedBytes = Convert.FromBase64String(encryptedValue);
                await _storageService.WriteAsync(key, encryptedBytes);
                
                _logger.LogInformation("Stored secret for user {UserId}: {SecretName}", userId, secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secret for user {UserId}: {SecretName}", userId, secretName);
                throw;
            }
        }

        /// <summary>
        /// Gets a user secret.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The secret value.</returns>
        public async Task<string> GetSecretAsync(string userId, string secretName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentNullException(nameof(secretName));
            }

            try
            {
                string encryptedValue = null;
                
                // Try to get from memory first
                if (_userSecrets.TryGetValue(userId, out var userSecretDict) &&
                    userSecretDict.TryGetValue(secretName, out encryptedValue))
                {
                    // Found in memory cache
                }
                else
                {
                    // Try to load from storage
                    string key = $"{SecretKeyPrefix}{userId}:{secretName}";
                    bool exists = await _storageService.ExistsAsync(key);
                    
                    if (!exists)
                    {
                        _logger.LogWarning("Secret not found for user {UserId}: {SecretName}", userId, secretName);
                        return string.Empty;
                    }
                    
                    var encryptedBytes = await _storageService.ReadAsync(key);
                    encryptedValue = Convert.ToBase64String(encryptedBytes);
                    
                    // Cache in memory for future use
                    if (!_userSecrets.TryGetValue(userId, out userSecretDict))
                    {
                        userSecretDict = new Dictionary<string, string>();
                        _userSecrets[userId] = userSecretDict;
                    }
                    
                    userSecretDict[secretName] = encryptedValue;
                }

                // Decrypt the secret value
                var decryptedValue = DecryptValue(encryptedValue);
                _logger.LogInformation("Retrieved secret for user {UserId}: {SecretName}", userId, secretName);

                return decryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret for user {UserId}: {SecretName}", userId, secretName);
                throw;
            }
        }

        /// <summary>
        /// Gets multiple user secrets.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretNames">The secret names.</param>
        /// <returns>A dictionary of secret names to secret values.</returns>
        public async Task<Dictionary<string, string>> GetSecretsAsync(string userId, IEnumerable<string> secretNames)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (secretNames == null)
            {
                throw new ArgumentNullException(nameof(secretNames));
            }

            try
            {
                var result = new Dictionary<string, string>();

                foreach (var secretName in secretNames)
                {
                    var secretValue = await GetSecretAsync(userId, secretName);
                    if (!string.IsNullOrEmpty(secretValue))
                    {
                        result[secretName] = secretValue;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secrets for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a user secret.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteSecretAsync(string userId, string secretName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentNullException(nameof(secretName));
            }

            try
            {
                // Remove from memory
                if (_userSecrets.TryGetValue(userId, out var userSecretDict))
                {
                    userSecretDict.Remove(secretName);
                }
                
                // Remove from storage
                string key = $"{SecretKeyPrefix}{userId}:{secretName}";
                await _storageService.DeleteAsync(key);
                
                _logger.LogInformation("Deleted secret for user {UserId}: {SecretName}", userId, secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret for user {UserId}: {SecretName}", userId, secretName);
                throw;
            }
        }

        /// <summary>
        /// Lists all secret names for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A list of secret names.</returns>
        public async Task<List<string>> ListSecretNamesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }
            
            try
            {
                var secretNames = new List<string>();
                string keyPrefix = $"{SecretKeyPrefix}{userId}:";
                var keys = await _storageService.ListKeysAsync(keyPrefix);
                
                foreach (var key in keys)
                {
                    // Extract secret name from key
                    string secretName = key.Substring(keyPrefix.Length);
                    secretNames.Add(secretName);
                }
                
                return secretNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing secret names for user {UserId}", userId);
                throw;
            }
        }

        private string EncryptValue(string value)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new System.IO.MemoryStream())
                {
                    // Write the IV to the beginning of the stream
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new System.IO.StreamWriter(cs))
                    {
                        sw.Write(value);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string DecryptValue(string encryptedValue)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedValue);

            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;

                // Get the IV from the beginning of the encrypted data
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new System.IO.MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new System.IO.StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
