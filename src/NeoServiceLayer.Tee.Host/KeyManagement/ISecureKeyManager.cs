using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Tee.Host.KeyManagement
{
    /// <summary>
    /// Interface for managing secure keys for enclaves.
    /// </summary>
    public interface ISecureKeyManager : IDisposable
    {
        /// <summary>
        /// Generates a new key with the specified ID and type.
        /// </summary>
        /// <param name="keyId">The ID of the key to generate.</param>
        /// <param name="keyType">The type of key to generate.</param>
        /// <returns>The generated key.</returns>
        Task<SecureKey> GenerateKeyAsync(string keyId, KeyType keyType);

        /// <summary>
        /// Gets an existing key with the specified ID.
        /// </summary>
        /// <param name="keyId">The ID of the key to get.</param>
        /// <returns>The key, or null if the key does not exist.</returns>
        Task<SecureKey> GetKeyAsync(string keyId);

        /// <summary>
        /// Deletes a key with the specified ID.
        /// </summary>
        /// <param name="keyId">The ID of the key to delete.</param>
        /// <returns>True if the key was deleted, false if the key does not exist.</returns>
        Task<bool> DeleteKeyAsync(string keyId);

        /// <summary>
        /// Gets all keys.
        /// </summary>
        /// <returns>A list of all keys.</returns>
        Task<IReadOnlyList<SecureKey>> GetAllKeysAsync();

        /// <summary>
        /// Signs data with the specified key.
        /// </summary>
        /// <param name="keyId">The ID of the key to use for signing.</param>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use for signing.</param>
        /// <returns>The signature.</returns>
        Task<byte[]> SignDataAsync(string keyId, byte[] data, HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256);

        /// <summary>
        /// Verifies a signature with the specified key.
        /// </summary>
        /// <param name="keyId">The ID of the key to use for verification.</param>
        /// <param name="data">The data to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use for verification.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> VerifySignatureAsync(string keyId, byte[] data, byte[] signature, HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256);
    }
}
