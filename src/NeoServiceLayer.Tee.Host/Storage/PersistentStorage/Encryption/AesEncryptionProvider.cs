using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Encryption
{
    /// <summary>
    /// An encryption provider using AES encryption.
    /// </summary>
    public class AesEncryptionProvider : IEncryptionProvider
    {
        private readonly ILogger<AesEncryptionProvider> _logger;
        private readonly AesEncryptionOptions _options;
        private readonly SemaphoreSlim _semaphore;
        private byte[] _key;
        private DateTime _keyCreationTime;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesEncryptionProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="options">The options for the encryption provider.</param>
        public AesEncryptionProvider(ILogger<AesEncryptionProvider> logger, AesEncryptionOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new AesEncryptionOptions();
            _semaphore = new SemaphoreSlim(1, 1);
            _initialized = false;
            _disposed = false;
        }

        /// <summary>
        /// Gets the name of the encryption provider.
        /// </summary>
        public string Name => "AES";

        /// <summary>
        /// Gets the description of the encryption provider.
        /// </summary>
        public string Description => "AES encryption provider with GCM mode for authenticated encryption.";

        /// <summary>
        /// Initializes the encryption provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            CheckDisposed();

            if (_initialized)
            {
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                _logger.LogInformation("Initializing AES encryption provider");

                // Load or generate the encryption key
                if (_options.Key != null && _options.Key.Length > 0)
                {
                    _logger.LogDebug("Using provided encryption key");
                    _key = _options.Key;
                }
                else if (!string.IsNullOrEmpty(_options.KeyFile) && File.Exists(_options.KeyFile))
                {
                    _logger.LogDebug("Loading encryption key from file: {KeyFile}", _options.KeyFile);
                    _key = await File.ReadAllBytesAsync(_options.KeyFile);
                }
                else
                {
                    _logger.LogDebug("Generating new encryption key");
                    _key = GenerateKey();

                    // Save the key to a file if specified
                    if (!string.IsNullOrEmpty(_options.KeyFile))
                    {
                        _logger.LogDebug("Saving encryption key to file: {KeyFile}", _options.KeyFile);
                        string directory = Path.GetDirectoryName(_options.KeyFile);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        await File.WriteAllBytesAsync(_options.KeyFile, _key);
                    }
                }

                _keyCreationTime = DateTime.UtcNow;
                _initialized = true;

                _logger.LogInformation("AES encryption provider initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AES encryption provider");
                throw new StorageException("Failed to initialize AES encryption provider", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Encrypts data.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="context">Optional context information for authenticated encryption.</param>
        /// <returns>The encrypted data.</returns>
        public async Task<byte[]> EncryptAsync(byte[] data, byte[] context = null)
        {
            CheckDisposed();
            EnsureInitialized();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Encrypting data ({Size} bytes)", data.Length);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Generate a random IV
                    byte[] iv = new byte[12]; // 96 bits for GCM
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(iv);
                    }

                    // Create the tag (authentication tag)
                    byte[] tag = new byte[16]; // 128 bits for GCM

                    // Encrypt the data
                    byte[] encryptedData = new byte[data.Length];
                    using (var aesGcm = new AesGcm(_key))
                    {
                        aesGcm.Encrypt(iv, data, encryptedData, tag, context);
                    }

                    // Combine IV, tag, and encrypted data
                    byte[] result = new byte[iv.Length + tag.Length + encryptedData.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(tag, 0, result, iv.Length, tag.Length);
                    Buffer.BlockCopy(encryptedData, 0, result, iv.Length + tag.Length, encryptedData.Length);

                    _logger.LogDebug("Data encrypted successfully ({EncryptedSize} bytes)", result.Length);
                    return result;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data");
                throw new StorageException("Failed to encrypt data", ex);
            }
        }

        /// <summary>
        /// Decrypts data.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="context">Optional context information for authenticated encryption.</param>
        /// <returns>The decrypted data.</returns>
        public async Task<byte[]> DecryptAsync(byte[] encryptedData, byte[] context = null)
        {
            CheckDisposed();
            EnsureInitialized();

            if (encryptedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            _logger.LogDebug("Decrypting data ({Size} bytes)", encryptedData.Length);

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Extract IV, tag, and encrypted data
                    int ivLength = 12; // 96 bits for GCM
                    int tagLength = 16; // 128 bits for GCM

                    if (encryptedData.Length < ivLength + tagLength)
                    {
                        throw new StorageException("Invalid encrypted data");
                    }

                    byte[] iv = new byte[ivLength];
                    byte[] tag = new byte[tagLength];
                    byte[] ciphertext = new byte[encryptedData.Length - ivLength - tagLength];

                    Buffer.BlockCopy(encryptedData, 0, iv, 0, ivLength);
                    Buffer.BlockCopy(encryptedData, ivLength, tag, 0, tagLength);
                    Buffer.BlockCopy(encryptedData, ivLength + tagLength, ciphertext, 0, ciphertext.Length);

                    // Decrypt the data
                    byte[] decryptedData = new byte[ciphertext.Length];
                    using (var aesGcm = new AesGcm(_key))
                    {
                        aesGcm.Decrypt(iv, ciphertext, tag, decryptedData, context);
                    }

                    _logger.LogDebug("Data decrypted successfully ({DecryptedSize} bytes)", decryptedData.Length);
                    return decryptedData;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data");
                throw new StorageException("Failed to decrypt data", ex);
            }
        }

        /// <summary>
        /// Rotates the encryption key.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RotateKeyAsync()
        {
            CheckDisposed();
            EnsureInitialized();

            _logger.LogInformation("Rotating encryption key");

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Generate a new key
                    byte[] newKey = GenerateKey();

                    // Backup the old key if key file is specified
                    if (!string.IsNullOrEmpty(_options.KeyFile) && File.Exists(_options.KeyFile))
                    {
                        string backupFile = $"{_options.KeyFile}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
                        _logger.LogDebug("Backing up old encryption key to file: {BackupFile}", backupFile);
                        File.Copy(_options.KeyFile, backupFile);
                    }

                    // Save the new key to a file if specified
                    if (!string.IsNullOrEmpty(_options.KeyFile))
                    {
                        _logger.LogDebug("Saving new encryption key to file: {KeyFile}", _options.KeyFile);
                        await File.WriteAllBytesAsync(_options.KeyFile, newKey);
                    }

                    // Update the key
                    _key = newKey;
                    _keyCreationTime = DateTime.UtcNow;

                    _logger.LogInformation("Encryption key rotated successfully");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate encryption key");
                throw new StorageException("Failed to rotate encryption key", ex);
            }
        }

        /// <summary>
        /// Gets information about the encryption provider.
        /// </summary>
        /// <returns>Information about the encryption provider.</returns>
        public EncryptionProviderInfo GetProviderInfo()
        {
            CheckDisposed();
            EnsureInitialized();

            return new EncryptionProviderInfo
            {
                Name = Name,
                Description = Description,
                Algorithm = "AES-GCM",
                KeySizeBits = _key.Length * 8,
                BlockSizeBits = 128,
                IVSizeBits = 96,
                SupportsAuthenticatedEncryption = true,
                SupportsKeyRotation = true,
                CurrentKeyCreationTime = _keyCreationTime,
                LastKeyRotationTime = _keyCreationTime,
                AdditionalProperties = new Dictionary<string, string>
                {
                    { "KeyFile", _options.KeyFile ?? "Not specified" },
                    { "KeySource", _options.Key != null ? "Provided" : (!string.IsNullOrEmpty(_options.KeyFile) && File.Exists(_options.KeyFile) ? "File" : "Generated") }
                }
            };
        }

        /// <summary>
        /// Disposes the encryption provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the encryption provider.
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
        /// Generates a new encryption key.
        /// </summary>
        /// <returns>The generated key.</returns>
        private byte[] GenerateKey()
        {
            int keySizeBytes = _options.KeySizeBits / 8;
            byte[] key = new byte[keySizeBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>
        /// Checks if the encryption provider has been disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AesEncryptionProvider));
            }
        }

        /// <summary>
        /// Ensures that the encryption provider has been initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Encryption provider has not been initialized. Call InitializeAsync first.");
            }
        }
    }
}
