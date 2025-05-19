using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing API keys.
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class ApiKeysController : BaseApiController
    {
        private readonly IApiKeyService _apiKeyService;

        /// <summary>
        /// Initializes a new instance of the ApiKeysController class.
        /// </summary>
        /// <param name="apiKeyService">The API key service.</param>
        public ApiKeysController(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        }

        /// <summary>
        /// Gets all API keys for the current user.
        /// </summary>
        /// <returns>A list of API keys.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApiKeyResponse>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetApiKeys()
        {
            try
            {
                var userId = GetUserId();
                var apiKeys = await _apiKeyService.GetApiKeysForUserAsync(userId);
                var response = apiKeys.Select(MapToApiKeyResponse);
                return Success(response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting API keys for user {UserId}", GetUserId());
                return InternalServerError<object>("Error getting API keys");
            }
        }

        /// <summary>
        /// Gets an API key by ID.
        /// </summary>
        /// <param name="id">The ID of the API key.</param>
        /// <returns>The API key.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ApiKeyResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetApiKey(string id)
        {
            try
            {
                var apiKey = await _apiKeyService.GetApiKeyAsync(id);
                if (apiKey == null)
                {
                    return NotFound<object>($"API key with ID {id} not found");
                }

                // Check if the API key belongs to the current user
                if (apiKey.UserId != GetUserId())
                {
                    return Forbidden<object>("You do not have permission to access this API key");
                }

                return Success(MapToApiKeyResponse(apiKey));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting API key {ApiKeyId} for user {UserId}", id, GetUserId());
                return InternalServerError<object>($"Error getting API key {id}");
            }
        }

        /// <summary>
        /// Creates a new API key.
        /// </summary>
        /// <param name="request">The API key creation request.</param>
        /// <returns>The created API key.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ApiKeyResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError<object>();
            }

            try
            {
                var userId = GetUserId();
                var apiKey = await _apiKeyService.CreateApiKeyAsync(
                    request.Name,
                    userId,
                    request.ExpiresAt,
                    request.Roles,
                    request.Scopes);

                var response = MapToApiKeyResponse(apiKey);
                return Created(response, $"/api/v1/apikeys/{apiKey.Id}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating API key for user {UserId}", GetUserId());
                return InternalServerError<object>("Error creating API key");
            }
        }

        /// <summary>
        /// Updates an API key.
        /// </summary>
        /// <param name="id">The ID of the API key.</param>
        /// <param name="request">The API key update request.</param>
        /// <returns>The updated API key.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ApiKeyResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> UpdateApiKey(string id, [FromBody] UpdateApiKeyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError<object>();
            }

            try
            {
                // Check if the API key exists and belongs to the current user
                var existingApiKey = await _apiKeyService.GetApiKeyAsync(id);
                if (existingApiKey == null)
                {
                    return NotFound<object>($"API key with ID {id} not found");
                }

                if (existingApiKey.UserId != GetUserId())
                {
                    return Forbidden<object>("You do not have permission to update this API key");
                }

                // Update the API key
                var apiKey = await _apiKeyService.UpdateApiKeyAsync(
                    id,
                    request.Name,
                    request.ExpiresAt,
                    request.Roles,
                    request.Scopes);

                return Success(MapToApiKeyResponse(apiKey));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating API key {ApiKeyId} for user {UserId}", id, GetUserId());
                return InternalServerError<object>($"Error updating API key {id}");
            }
        }

        /// <summary>
        /// Revokes an API key.
        /// </summary>
        /// <param name="id">The ID of the API key.</param>
        /// <returns>A status code indicating success or failure.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> RevokeApiKey(string id)
        {
            try
            {
                // Check if the API key exists and belongs to the current user
                var existingApiKey = await _apiKeyService.GetApiKeyAsync(id);
                if (existingApiKey == null)
                {
                    return NotFound<object>($"API key with ID {id} not found");
                }

                if (existingApiKey.UserId != GetUserId())
                {
                    return Forbidden<object>("You do not have permission to revoke this API key");
                }

                // Revoke the API key
                var success = await _apiKeyService.RevokeApiKeyAsync(id);
                if (!success)
                {
                    return NotFound<object>($"API key with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error revoking API key {ApiKeyId} for user {UserId}", id, GetUserId());
                return InternalServerError<object>($"Error revoking API key {id}");
            }
        }

        /// <summary>
        /// Maps an API key to an API key response.
        /// </summary>
        /// <param name="apiKey">The API key to map.</param>
        /// <returns>The API key response.</returns>
        private static ApiKeyResponse MapToApiKeyResponse(ApiKey apiKey)
        {
            return new ApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = apiKey.Key,
                CreatedAt = apiKey.CreatedAt,
                ExpiresAt = apiKey.ExpiresAt,
                LastUsedAt = apiKey.LastUsedAt,
                IsRevoked = apiKey.IsRevoked,
                RevokedAt = apiKey.RevokedAt,
                Roles = apiKey.Roles,
                Scopes = apiKey.Scopes
            };
        }
    }

    /// <summary>
    /// Request model for creating an API key.
    /// </summary>
    public class CreateApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the name of the API key.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the API key.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the roles associated with the API key.
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the scopes associated with the API key.
        /// </summary>
        public List<string> Scopes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request model for updating an API key.
    /// </summary>
    public class UpdateApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the name of the API key.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the API key.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the roles associated with the API key.
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the scopes associated with the API key.
        /// </summary>
        public List<string> Scopes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Response model for an API key.
    /// </summary>
    public class ApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the ID of the API key.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the API key.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the API key value.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the API key.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the API key.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the last used date of the API key.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the API key is revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the revocation date of the API key.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the roles associated with the API key.
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the scopes associated with the API key.
        /// </summary>
        public List<string> Scopes { get; set; } = new List<string>();
    }
}
