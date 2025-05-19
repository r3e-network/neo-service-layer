using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockKeyService : IKeyService
    {
        private readonly ILogger<MockKeyService> _logger;
        private readonly List<Key> _keys = new List<Key>();

        public MockKeyService(ILogger<MockKeyService> logger)
        {
            _logger = logger;

            // Add some sample keys
            _keys.Add(new Key
            {
                Id = "key123",
                UserId = "user123",
                Type = KeyType.AES,
                Algorithm = "AES-256",
                PublicKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Status = KeyStatus.Active,
                Metadata = new Dictionary<string, object>
                {
                    { "purpose", "data_encryption" }
                }
            });

            _keys.Add(new Key
            {
                Id = "key456",
                UserId = "user123",
                Type = KeyType.ECDSA,
                Algorithm = "ECDSA",
                PublicKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpiresAt = DateTime.UtcNow.AddDays(60),
                Status = KeyStatus.Active,
                Metadata = new Dictionary<string, object>
                {
                    { "purpose", "transaction_signing" }
                }
            });
        }

        public Task<Key> GenerateKeyAsync(string userId, KeyType type, string algorithm, Dictionary<string, object>? metadata = null)
        {
            _logger.LogInformation("Generating key for user {UserId} with type {KeyType} and algorithm {Algorithm}", userId, type, algorithm);

            var key = new Key
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = type,
                Algorithm = algorithm,
                PublicKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(90),
                Status = KeyStatus.Active,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            _keys.Add(key);

            return Task.FromResult(key);
        }

        public Task<Key> GetKeyAsync(string keyId)
        {
            _logger.LogInformation("Getting key {KeyId}", keyId);

            var key = _keys.FirstOrDefault(k => k.Id == keyId);
            return Task.FromResult(key);
        }

        public Task<IEnumerable<Key>> GetKeysAsync(string userId, KeyType? type = null)
        {
            _logger.LogInformation("Getting keys for user {UserId} with type {KeyType}", userId, type);

            var query = _keys.Where(k => k.UserId == userId);

            if (type.HasValue)
            {
                query = query.Where(k => k.Type == type.Value);
            }

            return Task.FromResult(query.AsEnumerable());
        }

        public Task<Key> RevokeKeyAsync(string keyId)
        {
            _logger.LogInformation("Revoking key {KeyId}", keyId);

            var key = _keys.FirstOrDefault(k => k.Id == keyId);
            if (key != null)
            {
                key.Status = KeyStatus.Revoked;
                key.RevokedAt = DateTime.UtcNow;
            }

            return Task.FromResult(key);
        }

        public Task<string> SignDataAsync(string keyId, byte[] data)
        {
            _logger.LogInformation("Signing data with key {KeyId}", keyId);

            var key = _keys.FirstOrDefault(k => k.Id == keyId);
            if (key != null && key.Status == KeyStatus.Active && key.Type == KeyType.ECDSA)
            {
                // Use the private key to sign the data
                using (var rsa = RSA.Create())
                {
                    // Load the private key
                    rsa.ImportFromPem(key.PrivateKey);

                    // Sign the data using SHA-256
                    byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    return Task.FromResult(Convert.ToBase64String(signature));
                }
            }

            return Task.FromResult(string.Empty);
        }

        public Task<bool> VerifySignatureAsync(string keyId, byte[] data, string signature)
        {
            _logger.LogInformation("Verifying signature with key {KeyId}", keyId);

            var key = _keys.FirstOrDefault(k => k.Id == keyId);
            if (key != null && key.Type == KeyType.ECDSA)
            {
                // Use the public key to verify the signature
                using (var rsa = RSA.Create())
                {
                    // Load the public key
                    rsa.ImportFromPem(key.PublicKey);

                    // Verify the signature using SHA-256
                    byte[] signatureBytes = Convert.FromBase64String(signature);
                    return Task.FromResult(rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                }
            }

            return Task.FromResult(false);
        }

        public Task<byte[]> EncryptDataAsync(string keyId, byte[] data)
        {
            _logger.LogInformation("Encrypting data with key {KeyId}", keyId);

            var key = _keys.FirstOrDefault(k => k.Id == keyId);
            if (key != null && key.Status == KeyStatus.Active && key.Type == KeyType.AES)
            {
                // Use the public key to encrypt the data
                using (var rsa = RSA.Create())
                {
                    // Load the public key
                    rsa.ImportFromPem(key.PublicKey);

                    // Encrypt the data using OAEP padding with SHA-256
                    return Task.FromResult(rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256));
                }
            }

            return Task.FromResult(Array.Empty<byte>());
        }

        public Task<byte[]> DecryptDataAsync(string keyId, byte[] encryptedData)
        {
            _logger.LogInformation("Decrypting data with key {KeyId}", keyId);

            var key = _keys.FirstOrDefault(k => k.Id == keyId);
            if (key != null && key.Status == KeyStatus.Active && key.Type == KeyType.AES)
            {
                // Use the private key to decrypt the data
                using (var rsa = RSA.Create())
                {
                    // Load the private key
                    rsa.ImportFromPem(key.PrivateKey);

                    // Decrypt the data using OAEP padding with SHA-256
                    return Task.FromResult(rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256));
                }
            }

            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
