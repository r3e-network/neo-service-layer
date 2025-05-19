using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Storage
{
    /// <summary>
    /// Interface for a secure storage provider that ensures data confidentiality and integrity.
    /// </summary>
    public interface ISecureStorageProvider : IPersistentStorageProvider
    {
        /// <summary>
        /// Encrypts data before storing it.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// Decrypts data after retrieving it.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        byte[] Decrypt(byte[] encryptedData);

        /// <summary>
        /// Rotates the encryption key used for data encryption.
        /// </summary>
        /// <param name="newKeyId">The ID of the new key to use.</param>
        /// <returns>True if the key was rotated successfully, false otherwise.</returns>
        Task<bool> RotateEncryptionKeyAsync(string newKeyId);

        /// <summary>
        /// Re-encrypts all data with the current encryption key.
        /// </summary>
        /// <returns>True if all data was re-encrypted successfully, false otherwise.</returns>
        Task<bool> ReEncryptAllDataAsync();

        /// <summary>
        /// Verifies the integrity of data.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns>True if the data integrity is verified, false otherwise.</returns>
        Task<bool> VerifyIntegrityAsync(string key);

        /// <summary>
        /// Verifies the integrity of all data.
        /// </summary>
        /// <returns>A dictionary mapping keys to integrity verification results.</returns>
        Task<Dictionary<string, bool>> VerifyAllIntegrityAsync();

        /// <summary>
        /// Gets the current encryption key ID.
        /// </summary>
        /// <returns>The current encryption key ID.</returns>
        string GetCurrentEncryptionKeyId();

        /// <summary>
        /// Gets all encryption key IDs.
        /// </summary>
        /// <returns>A list of all encryption key IDs.</returns>
        IReadOnlyList<string> GetAllEncryptionKeyIds();
    }
}
