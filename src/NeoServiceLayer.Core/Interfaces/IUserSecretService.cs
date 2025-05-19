using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for managing user secrets.
    /// </summary>
    public interface IUserSecretService
    {
        /// <summary>
        /// Creates a new user secret.
        /// </summary>
        /// <param name="secret">The user secret to create.</param>
        /// <returns>The created user secret (without the value).</returns>
        Task<UserSecret> CreateSecretAsync(UserSecret secret);

        /// <summary>
        /// Gets a user secret by ID.
        /// </summary>
        /// <param name="secretId">The ID of the user secret to get.</param>
        /// <param name="includeValue">Whether to include the secret value in the result.</param>
        /// <returns>The user secret.</returns>
        Task<UserSecret> GetSecretAsync(string secretId, bool includeValue = false);

        /// <summary>
        /// Updates a user secret.
        /// </summary>
        /// <param name="secret">The user secret to update.</param>
        /// <returns>The updated user secret (without the value).</returns>
        Task<UserSecret> UpdateSecretAsync(UserSecret secret);

        /// <summary>
        /// Deletes a user secret.
        /// </summary>
        /// <param name="secretId">The ID of the user secret to delete.</param>
        /// <returns>True if the secret was deleted, false otherwise.</returns>
        Task<bool> DeleteSecretAsync(string secretId);

        /// <summary>
        /// Lists all user secrets for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of user secrets (without values).</returns>
        Task<(List<UserSecret> Secrets, int TotalCount)> ListSecretsAsync(
            string userId, 
            int page = 1, 
            int pageSize = 10);

        /// <summary>
        /// Gets the values of multiple user secrets by their names.
        /// This method should only be called from within the TEE.
        /// </summary>
        /// <param name="secretNames">The names of the user secrets to get.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A dictionary of secret names to secret values.</returns>
        Task<Dictionary<string, string>> GetSecretValuesByNamesAsync(List<string> secretNames, string userId);
    }
}
