using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Secrets
{
    /// <summary>
    /// Interface for a secret manager that manages user secrets.
    /// </summary>
    public interface ISecretManager : IDisposable
    {
        /// <summary>
        /// Initializes the secret manager.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Stores a secret for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="secretValue">The value of the secret.</param>
        /// <returns>True if the secret was stored successfully, false otherwise.</returns>
        Task<bool> StoreSecretAsync(string userId, string secretName, string secretValue);

        /// <summary>
        /// Gets a secret for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <returns>The value of the secret, or null if the secret does not exist.</returns>
        Task<string> GetSecretAsync(string userId, string secretName);

        /// <summary>
        /// Gets multiple secrets for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretNames">The names of the secrets to get.</param>
        /// <returns>A dictionary mapping secret names to secret values.</returns>
        Task<Dictionary<string, string>> GetSecretsAsync(string userId, IEnumerable<string> secretNames);

        /// <summary>
        /// Deletes a secret for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The name of the secret to delete.</param>
        /// <returns>True if the secret was deleted successfully, false otherwise.</returns>
        Task<bool> DeleteSecretAsync(string userId, string secretName);

        /// <summary>
        /// Gets all secret names for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A list of secret names.</returns>
        Task<IReadOnlyList<string>> GetSecretNamesAsync(string userId);

        /// <summary>
        /// Checks if a secret exists for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The name of the secret to check.</param>
        /// <returns>True if the secret exists, false otherwise.</returns>
        Task<bool> SecretExistsAsync(string userId, string secretName);

        /// <summary>
        /// Updates a secret for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The name of the secret to update.</param>
        /// <param name="secretValue">The new value of the secret.</param>
        /// <returns>True if the secret was updated successfully, false otherwise.</returns>
        Task<bool> UpdateSecretAsync(string userId, string secretName, string secretValue);

        /// <summary>
        /// Rotates the encryption key used to encrypt secrets.
        /// </summary>
        /// <returns>True if the encryption key was rotated successfully, false otherwise.</returns>
        Task<bool> RotateEncryptionKeyAsync();

        /// <summary>
        /// Re-encrypts all secrets with the current encryption key.
        /// </summary>
        /// <returns>True if all secrets were re-encrypted successfully, false otherwise.</returns>
        Task<bool> ReEncryptSecretsAsync();

        /// <summary>
        /// Gets the number of secrets for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The number of secrets for the user.</returns>
        Task<int> GetSecretCountAsync(string userId);

        /// <summary>
        /// Gets the total number of secrets across all users.
        /// </summary>
        /// <returns>The total number of secrets.</returns>
        Task<int> GetTotalSecretCountAsync();

        /// <summary>
        /// Gets the list of user IDs that have secrets.
        /// </summary>
        /// <returns>A list of user IDs.</returns>
        Task<IReadOnlyList<string>> GetUserIdsAsync();

        /// <summary>
        /// Clears all secrets for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if all secrets were cleared successfully, false otherwise.</returns>
        Task<bool> ClearUserSecretsAsync(string userId);

        /// <summary>
        /// Clears all secrets for all users.
        /// </summary>
        /// <returns>True if all secrets were cleared successfully, false otherwise.</returns>
        Task<bool> ClearAllSecretsAsync();

        /// <summary>
        /// Exports all secrets for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A dictionary mapping secret names to secret values.</returns>
        Task<Dictionary<string, string>> ExportUserSecretsAsync(string userId);

        /// <summary>
        /// Imports secrets for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secrets">A dictionary mapping secret names to secret values.</param>
        /// <param name="overwrite">Whether to overwrite existing secrets.</param>
        /// <returns>True if all secrets were imported successfully, false otherwise.</returns>
        Task<bool> ImportUserSecretsAsync(string userId, Dictionary<string, string> secrets, bool overwrite = false);

        /// <summary>
        /// Backs up all secrets to a file.
        /// </summary>
        /// <param name="filePath">The path to the backup file.</param>
        /// <returns>True if the backup was successful, false otherwise.</returns>
        Task<bool> BackupSecretsAsync(string filePath);

        /// <summary>
        /// Restores all secrets from a backup file.
        /// </summary>
        /// <param name="filePath">The path to the backup file.</param>
        /// <param name="overwrite">Whether to overwrite existing secrets.</param>
        /// <returns>True if the restore was successful, false otherwise.</returns>
        Task<bool> RestoreSecretsAsync(string filePath, bool overwrite = false);
    }
}
