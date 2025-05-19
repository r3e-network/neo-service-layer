using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for key management service.
    /// </summary>
    public interface IKeyService
    {
        /// <summary>
        /// Generates a new key.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="type">The key type.</param>
        /// <param name="algorithm">The algorithm to use.</param>
        /// <param name="metadata">Additional metadata for the key.</param>
        /// <returns>The generated key.</returns>
        Task<Key> GenerateKeyAsync(string userId, KeyType type, string algorithm, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Gets a key by ID.
        /// </summary>
        /// <param name="keyId">The key ID.</param>
        /// <returns>The key if found, null otherwise.</returns>
        Task<Key> GetKeyAsync(string keyId);

        /// <summary>
        /// Gets keys for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="type">Optional key type filter.</param>
        /// <returns>The keys for the user.</returns>
        Task<IEnumerable<Key>> GetKeysAsync(string userId, KeyType? type = null);

        /// <summary>
        /// Revokes a key.
        /// </summary>
        /// <param name="keyId">The key ID.</param>
        /// <returns>The revoked key if found, null otherwise.</returns>
        Task<Key> RevokeKeyAsync(string keyId);

        /// <summary>
        /// Signs data with a key.
        /// </summary>
        /// <param name="keyId">The key ID.</param>
        /// <param name="data">The data to sign.</param>
        /// <returns>The signature.</returns>
        Task<string> SignDataAsync(string keyId, byte[] data);

        /// <summary>
        /// Verifies a signature.
        /// </summary>
        /// <param name="keyId">The key ID.</param>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> VerifySignatureAsync(string keyId, byte[] data, string signature);

        /// <summary>
        /// Encrypts data with a key.
        /// </summary>
        /// <param name="keyId">The key ID.</param>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        Task<byte[]> EncryptDataAsync(string keyId, byte[] data);

        /// <summary>
        /// Decrypts data with a key.
        /// </summary>
        /// <param name="keyId">The key ID.</param>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        Task<byte[]> DecryptDataAsync(string keyId, byte[] encryptedData);
    }
}
