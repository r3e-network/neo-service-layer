using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Secrets
{
    /// <summary>
    /// Interface for a service that encrypts and decrypts secrets.
    /// </summary>
    public interface ISecretEncryptionService
    {
        /// <summary>
        /// Initializes the encryption service.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Encrypts a secret value.
        /// </summary>
        /// <param name="value">The value to encrypt.</param>
        /// <returns>The encrypted value.</returns>
        Task<byte[]> EncryptAsync(string value);

        /// <summary>
        /// Encrypts a secret value.
        /// </summary>
        /// <param name="value">The value to encrypt.</param>
        /// <returns>The encrypted value.</returns>
        Task<byte[]> EncryptAsync(byte[] value);

        /// <summary>
        /// Decrypts a secret value.
        /// </summary>
        /// <param name="encryptedValue">The encrypted value to decrypt.</param>
        /// <returns>The decrypted value.</returns>
        Task<string> DecryptToStringAsync(byte[] encryptedValue);

        /// <summary>
        /// Decrypts a secret value.
        /// </summary>
        /// <param name="encryptedValue">The encrypted value to decrypt.</param>
        /// <returns>The decrypted value.</returns>
        Task<byte[]> DecryptToBytesAsync(byte[] encryptedValue);

        /// <summary>
        /// Rotates the encryption key.
        /// </summary>
        /// <returns>True if the encryption key was rotated successfully, false otherwise.</returns>
        Task<bool> RotateEncryptionKeyAsync();

        /// <summary>
        /// Re-encrypts a value with the current encryption key.
        /// </summary>
        /// <param name="encryptedValue">The encrypted value to re-encrypt.</param>
        /// <returns>The re-encrypted value.</returns>
        Task<byte[]> ReEncryptAsync(byte[] encryptedValue);

        /// <summary>
        /// Gets the current encryption key ID.
        /// </summary>
        /// <returns>The current encryption key ID.</returns>
        Task<string> GetCurrentKeyIdAsync();

        /// <summary>
        /// Gets the encryption key ID for an encrypted value.
        /// </summary>
        /// <param name="encryptedValue">The encrypted value.</param>
        /// <returns>The encryption key ID.</returns>
        Task<string> GetKeyIdForValueAsync(byte[] encryptedValue);

        /// <summary>
        /// Checks if an encrypted value needs to be re-encrypted.
        /// </summary>
        /// <param name="encryptedValue">The encrypted value to check.</param>
        /// <returns>True if the value needs to be re-encrypted, false otherwise.</returns>
        Task<bool> NeedsReEncryptionAsync(byte[] encryptedValue);

        /// <summary>
        /// Generates a new encryption key.
        /// </summary>
        /// <returns>The ID of the new encryption key.</returns>
        Task<string> GenerateNewKeyAsync();

        /// <summary>
        /// Deletes an encryption key.
        /// </summary>
        /// <param name="keyId">The ID of the encryption key to delete.</param>
        /// <returns>True if the encryption key was deleted successfully, false otherwise.</returns>
        Task<bool> DeleteKeyAsync(string keyId);

        /// <summary>
        /// Gets the number of encryption keys.
        /// </summary>
        /// <returns>The number of encryption keys.</returns>
        Task<int> GetKeyCountAsync();

        /// <summary>
        /// Gets the IDs of all encryption keys.
        /// </summary>
        /// <returns>The IDs of all encryption keys.</returns>
        Task<string[]> GetKeyIdsAsync();

        /// <summary>
        /// Gets the creation timestamp of an encryption key.
        /// </summary>
        /// <param name="keyId">The ID of the encryption key.</param>
        /// <returns>The creation timestamp of the encryption key.</returns>
        Task<DateTime> GetKeyCreationTimeAsync(string keyId);

        /// <summary>
        /// Gets the last rotation timestamp of an encryption key.
        /// </summary>
        /// <param name="keyId">The ID of the encryption key.</param>
        /// <returns>The last rotation timestamp of the encryption key.</returns>
        Task<DateTime?> GetKeyLastRotationTimeAsync(string keyId);
    }
}
