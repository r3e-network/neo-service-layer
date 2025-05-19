using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Interfaces;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Base abstract class for secure storage providers that implements common functionality.
    /// </summary>
    public abstract class BaseSecureStorageProvider : BasePersistentStorageProvider, ISecureStorageProvider
    {
        /// <summary>
        /// The TEE interface to use for encryption and decryption.
        /// </summary>
        protected readonly ITeeInterface TeeInterface;

        /// <summary>
        /// The current encryption key ID.
        /// </summary>
        protected string CurrentEncryptionKeyId;

        /// <summary>
        /// The encryption key IDs.
        /// </summary>
        protected readonly List<string> EncryptionKeyIds;

        /// <summary>
        /// Initializes a new instance of the BaseSecureStorageProvider class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="teeInterface">The TEE interface to use for encryption and decryption.</param>
        protected BaseSecureStorageProvider(ILogger logger, ITeeInterface teeInterface)
            : base(logger)
        {
            TeeInterface = teeInterface ?? throw new ArgumentNullException(nameof(teeInterface));
            EncryptionKeyIds = new List<string>();
        }

        /// <inheritdoc/>
        public virtual byte[] Encrypt(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            CheckDisposed();

            try
            {
                // Seal the data using the TEE interface
                return TeeInterface.SealData(data);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to encrypt data");
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual byte[] Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            CheckDisposed();

            try
            {
                // Unseal the data using the TEE interface
                return TeeInterface.UnsealData(encryptedData);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to decrypt data");
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> RotateEncryptionKeyAsync(string newKeyId)
        {
            if (string.IsNullOrEmpty(newKeyId))
            {
                throw new ArgumentException("New key ID cannot be null or empty", nameof(newKeyId));
            }

            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Get all keys
                    var keys = await GetAllKeysAsync();

                    // Re-encrypt all data with the new key
                    foreach (var key in keys)
                    {
                        // Get the data
                        var data = await ReadAsync(key);
                        if (data == null)
                        {
                            continue;
                        }

                        // Decrypt the data with the current key
                        var decryptedData = Decrypt(data);

                        // Encrypt the data with the new key
                        var encryptedData = Encrypt(decryptedData);

                        // Write the data back
                        if (!await WriteAsync(key, encryptedData))
                        {
                            Logger.LogError("Failed to write re-encrypted data for key {Key}", key);
                            return false;
                        }
                    }

                    // Update the current key ID
                    CurrentEncryptionKeyId = newKeyId;

                    // Add the new key ID to the list if it's not already there
                    if (!EncryptionKeyIds.Contains(newKeyId))
                    {
                        EncryptionKeyIds.Add(newKeyId);
                    }

                    Logger.LogInformation("Encryption key rotated successfully to {KeyId}", newKeyId);
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to rotate encryption key to {KeyId}", newKeyId);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> ReEncryptAllDataAsync()
        {
            CheckDisposed();

            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    // Get all keys
                    var keys = await GetAllKeysAsync();

                    // Re-encrypt all data with the current key
                    foreach (var key in keys)
                    {
                        // Get the data
                        var data = await ReadAsync(key);
                        if (data == null)
                        {
                            continue;
                        }

                        // Decrypt the data
                        var decryptedData = Decrypt(data);

                        // Encrypt the data
                        var encryptedData = Encrypt(decryptedData);

                        // Write the data back
                        if (!await WriteAsync(key, encryptedData))
                        {
                            Logger.LogError("Failed to write re-encrypted data for key {Key}", key);
                            return false;
                        }
                    }

                    Logger.LogInformation("All data re-encrypted successfully");
                    return true;
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to re-encrypt all data");
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> VerifyIntegrityAsync(string key)
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
                    return false;
                }

                // Get the data
                var data = await ReadAsync(key);
                if (data == null)
                {
                    return false;
                }

                // Compute the hash
                var hash = ComputeHash(data);

                // Compare the hash
                return hash == metadata.Hash;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to verify integrity for key {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<Dictionary<string, bool>> VerifyAllIntegrityAsync()
        {
            CheckDisposed();

            try
            {
                // Get all keys
                var keys = await GetAllKeysAsync();

                // Verify integrity for each key
                var result = new Dictionary<string, bool>();
                foreach (var key in keys)
                {
                    result[key] = await VerifyIntegrityAsync(key);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to verify integrity for all keys");
                return new Dictionary<string, bool>();
            }
        }

        /// <inheritdoc/>
        public virtual string GetCurrentEncryptionKeyId()
        {
            CheckDisposed();
            return CurrentEncryptionKeyId;
        }

        /// <inheritdoc/>
        public virtual IReadOnlyList<string> GetAllEncryptionKeyIds()
        {
            CheckDisposed();
            return EncryptionKeyIds.AsReadOnly();
        }

        /// <inheritdoc/>
        protected override async Task<bool> WriteInternalAsync(string key, byte[] data)
        {
            // Encrypt the data before writing
            var encryptedData = Encrypt(data);

            // Create metadata
            var metadata = new StorageMetadata
            {
                Key = key,
                Size = data.Length,
                CreationTime = DateTime.UtcNow,
                LastModifiedTime = DateTime.UtcNow,
                LastAccessTime = DateTime.UtcNow,
                IsChunked = false,
                Hash = ComputeHash(encryptedData),
                IsEncrypted = true,
                EncryptionAlgorithm = "AES-GCM",
                EncryptionKeyId = CurrentEncryptionKeyId
            };

            // Write the data
            if (!await base.WriteInternalAsync(key, encryptedData))
            {
                return false;
            }

            // Write the metadata
            return await WriteMetadataInternalAsync(key, metadata);
        }

        /// <inheritdoc/>
        protected override async Task<byte[]> ReadInternalAsync(string key)
        {
            // Read the encrypted data
            var encryptedData = await base.ReadInternalAsync(key);
            if (encryptedData == null)
            {
                return null;
            }

            // Get the metadata
            var metadata = await GetMetadataInternalAsync(key);
            if (metadata == null)
            {
                return null;
            }

            // Update the last access time
            metadata.LastAccessTime = DateTime.UtcNow;
            await WriteMetadataInternalAsync(key, metadata);

            // Decrypt the data
            return Decrypt(encryptedData);
        }
    }
}
