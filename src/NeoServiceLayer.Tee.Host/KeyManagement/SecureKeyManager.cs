using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.KeyManagement
{
    /// <summary>
    /// Provides secure key management operations for enclaves.
    /// </summary>
    public class SecureKeyManager : ISecureKeyManager
    {
        private readonly ILogger<SecureKeyManager> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly ConcurrentDictionary<string, byte[]> _sealedKeys;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureKeyManager"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for cryptographic operations.</param>
        public SecureKeyManager(ILogger<SecureKeyManager> logger, IOcclumInterface occlumInterface)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _sealedKeys = new ConcurrentDictionary<string, byte[]>();
            _disposed = false;
            
            _logger.LogInformation("Secure key manager initialized");
        }

        /// <summary>
        /// Generates a new key with the specified ID and type.
        /// </summary>
        /// <param name="keyId">The ID of the key to generate.</param>
        /// <param name="keyType">The type of key to generate.</param>
        /// <returns>The generated key.</returns>
        public async Task<SecureKey> GenerateKeyAsync(string keyId, KeyType keyType)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            _logger.LogInformation("Generating key {KeyId} of type {KeyType}", keyId, keyType);

            // Generate the key
            byte[] keyData;
            switch (keyType)
            {
                case KeyType.Aes256:
                    using (var aes = Aes.Create())
                    {
                        aes.KeySize = 256;
                        aes.GenerateKey();
                        keyData = aes.Key;
                    }
                    break;
                case KeyType.Rsa2048:
                    using (var rsa = RSA.Create(2048))
                    {
                        keyData = rsa.ExportRSAPrivateKey();
                    }
                    break;
                case KeyType.Rsa4096:
                    using (var rsa = RSA.Create(4096))
                    {
                        keyData = rsa.ExportRSAPrivateKey();
                    }
                    break;
                case KeyType.EcdsaP256:
                    using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
                    {
                        keyData = ecdsa.ExportECPrivateKey();
                    }
                    break;
                case KeyType.EcdsaP384:
                    using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384))
                    {
                        keyData = ecdsa.ExportECPrivateKey();
                    }
                    break;
                default:
                    throw new ArgumentException($"Unsupported key type: {keyType}", nameof(keyType));
            }

            // Seal the key
            byte[] sealedKeyData = await SealDataAsync(keyData);

            // Store the sealed key
            _sealedKeys[keyId] = sealedKeyData;

            // Create the secure key
            var secureKey = new SecureKey
            {
                KeyId = keyId,
                KeyType = keyType,
                CreationTime = DateTime.UtcNow,
                LastUsedTime = DateTime.UtcNow
            };

            _logger.LogInformation("Key {KeyId} generated successfully", keyId);
            return secureKey;
        }

        /// <summary>
        /// Gets an existing key with the specified ID.
        /// </summary>
        /// <param name="keyId">The ID of the key to get.</param>
        /// <returns>The key, or null if the key does not exist.</returns>
        public async Task<SecureKey> GetKeyAsync(string keyId)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            if (!_sealedKeys.TryGetValue(keyId, out var sealedKeyData))
            {
                _logger.LogWarning("Key {KeyId} not found", keyId);
                return null;
            }

            // Unseal the key to verify it
            try
            {
                byte[] keyData = await UnsealDataAsync(sealedKeyData);
                if (keyData == null || keyData.Length == 0)
                {
                    _logger.LogError("Failed to unseal key {KeyId}", keyId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unseal key {KeyId}", keyId);
                return null;
            }

            // Create the secure key (without the actual key data)
            var secureKey = new SecureKey
            {
                KeyId = keyId,
                KeyType = DetermineKeyType(sealedKeyData),
                CreationTime = DateTime.UtcNow, // We don't store creation time, so use current time
                LastUsedTime = DateTime.UtcNow
            };

            _logger.LogDebug("Key {KeyId} retrieved successfully", keyId);
            return secureKey;
        }

        /// <summary>
        /// Deletes a key with the specified ID.
        /// </summary>
        /// <param name="keyId">The ID of the key to delete.</param>
        /// <returns>True if the key was deleted, false if the key does not exist.</returns>
        public Task<bool> DeleteKeyAsync(string keyId)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            if (_sealedKeys.TryRemove(keyId, out _))
            {
                _logger.LogInformation("Key {KeyId} deleted successfully", keyId);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Key {KeyId} not found for deletion", keyId);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets all keys.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        public Task<IReadOnlyList<SecureKey>> GetAllKeysAsync()
        {
            CheckDisposed();

            var keys = new List<SecureKey>();
            foreach (var keyId in _sealedKeys.Keys)
            {
                var key = GetKeyAsync(keyId).GetAwaiter().GetResult();
                if (key != null)
                {
                    keys.Add(key);
                }
            }

            return Task.FromResult<IReadOnlyList<SecureKey>>(keys);
        }

        /// <summary>
        /// Signs data with the specified key.
        /// </summary>
        /// <param name="keyId">The ID of the key to use for signing.</param>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use for signing.</param>
        /// <returns>The signature.</returns>
        public async Task<byte[]> SignDataAsync(string keyId, byte[] data, HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data cannot be null or empty", nameof(data));
            }

            if (!_sealedKeys.TryGetValue(keyId, out var sealedKeyData))
            {
                _logger.LogError("Key {KeyId} not found for signing", keyId);
                throw new KeyNotFoundException($"Key {keyId} not found");
            }

            // Unseal the key
            byte[] keyData = await UnsealDataAsync(sealedKeyData);
            if (keyData == null || keyData.Length == 0)
            {
                _logger.LogError("Failed to unseal key {KeyId} for signing", keyId);
                throw new InvalidOperationException($"Failed to unseal key {keyId}");
            }

            // Determine the key type
            var keyType = DetermineKeyType(sealedKeyData);

            // Sign the data
            byte[] signature;
            try
            {
                switch (keyType)
                {
                    case KeyType.Rsa2048:
                    case KeyType.Rsa4096:
                        using (var rsa = RSA.Create())
                        {
                            rsa.ImportRSAPrivateKey(keyData, out _);
                            signature = rsa.SignData(data, GetHashAlgorithmName(hashAlgorithm), RSASignaturePadding.Pkcs1);
                        }
                        break;
                    case KeyType.EcdsaP256:
                    case KeyType.EcdsaP384:
                        using (var ecdsa = ECDsa.Create())
                        {
                            ecdsa.ImportECPrivateKey(keyData, out _);
                            signature = ecdsa.SignData(data, GetHashAlgorithmName(hashAlgorithm));
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Key type {keyType} does not support signing");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign data with key {KeyId}", keyId);
                throw new SigningException($"Failed to sign data with key {keyId}", ex);
            }

            _logger.LogInformation("Data signed successfully with key {KeyId}", keyId);
            return signature;
        }

        /// <summary>
        /// Verifies a signature with the specified key.
        /// </summary>
        /// <param name="keyId">The ID of the key to use for verification.</param>
        /// <param name="data">The data to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use for verification.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        public async Task<bool> VerifySignatureAsync(string keyId, byte[] data, byte[] signature, HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data cannot be null or empty", nameof(data));
            }

            if (signature == null || signature.Length == 0)
            {
                throw new ArgumentException("Signature cannot be null or empty", nameof(signature));
            }

            if (!_sealedKeys.TryGetValue(keyId, out var sealedKeyData))
            {
                _logger.LogError("Key {KeyId} not found for verification", keyId);
                throw new KeyNotFoundException($"Key {keyId} not found");
            }

            // Unseal the key
            byte[] keyData = await UnsealDataAsync(sealedKeyData);
            if (keyData == null || keyData.Length == 0)
            {
                _logger.LogError("Failed to unseal key {KeyId} for verification", keyId);
                throw new InvalidOperationException($"Failed to unseal key {keyId}");
            }

            // Determine the key type
            var keyType = DetermineKeyType(sealedKeyData);

            // Verify the signature
            bool isValid;
            try
            {
                switch (keyType)
                {
                    case KeyType.Rsa2048:
                    case KeyType.Rsa4096:
                        using (var rsa = RSA.Create())
                        {
                            rsa.ImportRSAPrivateKey(keyData, out _);
                            isValid = rsa.VerifyData(data, signature, GetHashAlgorithmName(hashAlgorithm), RSASignaturePadding.Pkcs1);
                        }
                        break;
                    case KeyType.EcdsaP256:
                    case KeyType.EcdsaP384:
                        using (var ecdsa = ECDsa.Create())
                        {
                            ecdsa.ImportECPrivateKey(keyData, out _);
                            isValid = ecdsa.VerifyData(data, signature, GetHashAlgorithmName(hashAlgorithm));
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Key type {keyType} does not support verification");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify signature with key {KeyId}", keyId);
                throw new VerificationException($"Failed to verify signature with key {keyId}", ex);
            }

            _logger.LogInformation("Signature verification result for key {KeyId}: {IsValid}", keyId, isValid);
            return isValid;
        }

        /// <summary>
        /// Disposes the secure key manager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the secure key manager.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clear the sealed keys
                    _sealedKeys.Clear();
                }

                _disposed = true;
            }
        }

        private async Task<byte[]> SealDataAsync(byte[] data)
        {
            try
            {
                // Use the enclave to seal the data
                return _occlumInterface.SealData(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seal data");
                throw new SealingException("Failed to seal data", ex);
            }
        }

        private async Task<byte[]> UnsealDataAsync(byte[] sealedData)
        {
            try
            {
                // Use the enclave to unseal the data
                return _occlumInterface.UnsealData(sealedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unseal data");
                throw new UnsealingException("Failed to unseal data", ex);
            }
        }

        private KeyType DetermineKeyType(byte[] sealedKeyData)
        {
            // In a real implementation, we would store the key type with the sealed key
            // For now, we'll just return a default value
            return KeyType.Rsa2048;
        }

        private HashAlgorithmName GetHashAlgorithmName(HashAlgorithmType hashAlgorithm)
        {
            switch (hashAlgorithm)
            {
                case HashAlgorithmType.Sha256:
                    return HashAlgorithmName.SHA256;
                case HashAlgorithmType.Sha384:
                    return HashAlgorithmName.SHA384;
                case HashAlgorithmType.Sha512:
                    return HashAlgorithmName.SHA512;
                default:
                    throw new ArgumentException($"Unsupported hash algorithm: {hashAlgorithm}", nameof(hashAlgorithm));
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecureKeyManager));
            }
        }
    }
}
