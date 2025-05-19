using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for a service that manages API keys.
    /// </summary>
    public interface IApiKeyService
    {
        /// <summary>
        /// Validates an API key.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>The API key information if valid, null otherwise.</returns>
        Task<ApiKeyInfo> ValidateApiKeyAsync(string apiKey);

        /// <summary>
        /// Creates a new API key.
        /// </summary>
        /// <param name="name">The name of the API key.</param>
        /// <param name="userId">The ID of the user creating the API key.</param>
        /// <param name="expiresAt">The expiration date of the API key.</param>
        /// <param name="roles">The roles associated with the API key.</param>
        /// <param name="scopes">The scopes associated with the API key.</param>
        /// <returns>The created API key.</returns>
        Task<ApiKey> CreateApiKeyAsync(string name, string userId, DateTime? expiresAt = null, IEnumerable<string> roles = null, IEnumerable<string> scopes = null);

        /// <summary>
        /// Gets an API key by ID.
        /// </summary>
        /// <param name="id">The ID of the API key.</param>
        /// <returns>The API key if found, null otherwise.</returns>
        Task<ApiKey> GetApiKeyAsync(string id);

        /// <summary>
        /// Gets all API keys for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of API keys.</returns>
        Task<IEnumerable<ApiKey>> GetApiKeysForUserAsync(string userId);

        /// <summary>
        /// Revokes an API key.
        /// </summary>
        /// <param name="id">The ID of the API key.</param>
        /// <returns>True if the API key was revoked, false otherwise.</returns>
        Task<bool> RevokeApiKeyAsync(string id);

        /// <summary>
        /// Updates an API key.
        /// </summary>
        /// <param name="id">The ID of the API key.</param>
        /// <param name="name">The new name of the API key.</param>
        /// <param name="expiresAt">The new expiration date of the API key.</param>
        /// <param name="roles">The new roles associated with the API key.</param>
        /// <param name="scopes">The new scopes associated with the API key.</param>
        /// <returns>The updated API key.</returns>
        Task<ApiKey> UpdateApiKeyAsync(string id, string name, DateTime? expiresAt = null, IEnumerable<string> roles = null, IEnumerable<string> scopes = null);
    }
}
