using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data;
using NeoServiceLayer.Infrastructure.Data.Entities;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Service for managing API keys.
    /// </summary>
    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ApiKeyService> _logger;

        /// <summary>
        /// Initializes a new instance of the ApiKeyService class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="logger">The logger.</param>
        public ApiKeyService(ApplicationDbContext dbContext, ILogger<ApiKeyService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ApiKeyInfo> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            try
            {
                // Hash the API key for lookup
                var keyHash = HashApiKey(apiKey);

                // Find the API key in the database
                var apiKeyEntity = await _dbContext.ApiKeys
                    .Include(k => k.Roles)
                    .Include(k => k.Scopes)
                    .FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsRevoked);

                if (apiKeyEntity == null)
                {
                    return null;
                }

                // Update the last used date
                apiKeyEntity.LastUsedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // Map to API key info
                return new ApiKeyInfo
                {
                    Id = apiKeyEntity.Id,
                    Name = apiKeyEntity.Name,
                    UserId = apiKeyEntity.UserId,
                    ExpiresAt = apiKeyEntity.ExpiresAt,
                    Roles = apiKeyEntity.Roles.Select(r => r.Role).ToList(),
                    Scopes = apiKeyEntity.Scopes.Select(s => s.Scope).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<ApiKey> CreateApiKeyAsync(string name, string userId, DateTime? expiresAt = null, IEnumerable<string> roles = null, IEnumerable<string> scopes = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("API key name is required", nameof(name));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID is required", nameof(userId));
            }

            try
            {
                // Generate a new API key
                var apiKeyValue = GenerateApiKey();
                var apiKeyHash = HashApiKey(apiKeyValue);

                // Create the API key entity
                var apiKeyEntity = new ApiKeyEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    KeyHash = apiKeyHash,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsRevoked = false
                };

                // Add roles
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        apiKeyEntity.Roles.Add(new ApiKeyRoleEntity
                        {
                            ApiKeyId = apiKeyEntity.Id,
                            Role = role
                        });
                    }
                }

                // Add scopes
                if (scopes != null)
                {
                    foreach (var scope in scopes)
                    {
                        apiKeyEntity.Scopes.Add(new ApiKeyScopeEntity
                        {
                            ApiKeyId = apiKeyEntity.Id,
                            Scope = scope
                        });
                    }
                }

                // Save to the database
                _dbContext.ApiKeys.Add(apiKeyEntity);
                await _dbContext.SaveChangesAsync();

                // Map to API key
                return new ApiKey
                {
                    Id = apiKeyEntity.Id,
                    Name = apiKeyEntity.Name,
                    Key = apiKeyValue,
                    UserId = apiKeyEntity.UserId,
                    CreatedAt = apiKeyEntity.CreatedAt,
                    ExpiresAt = apiKeyEntity.ExpiresAt,
                    IsRevoked = apiKeyEntity.IsRevoked,
                    Roles = apiKeyEntity.Roles.Select(r => r.Role).ToList(),
                    Scopes = apiKeyEntity.Scopes.Select(s => s.Scope).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ApiKey> GetApiKeyAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("API key ID is required", nameof(id));
            }

            try
            {
                // Find the API key in the database
                var apiKeyEntity = await _dbContext.ApiKeys
                    .Include(k => k.Roles)
                    .Include(k => k.Scopes)
                    .FirstOrDefaultAsync(k => k.Id == id);

                if (apiKeyEntity == null)
                {
                    return null;
                }

                // Map to API key
                return new ApiKey
                {
                    Id = apiKeyEntity.Id,
                    Name = apiKeyEntity.Name,
                    Key = null, // We don't store the actual key
                    UserId = apiKeyEntity.UserId,
                    CreatedAt = apiKeyEntity.CreatedAt,
                    ExpiresAt = apiKeyEntity.ExpiresAt,
                    LastUsedAt = apiKeyEntity.LastUsedAt,
                    IsRevoked = apiKeyEntity.IsRevoked,
                    RevokedAt = apiKeyEntity.RevokedAt,
                    Roles = apiKeyEntity.Roles.Select(r => r.Role).ToList(),
                    Scopes = apiKeyEntity.Scopes.Select(s => s.Scope).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key {ApiKeyId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApiKey>> GetApiKeysForUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID is required", nameof(userId));
            }

            try
            {
                // Find the API keys in the database
                var apiKeyEntities = await _dbContext.ApiKeys
                    .Include(k => k.Roles)
                    .Include(k => k.Scopes)
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                // Map to API keys
                return apiKeyEntities.Select(k => new ApiKey
                {
                    Id = k.Id,
                    Name = k.Name,
                    Key = null, // We don't store the actual key
                    UserId = k.UserId,
                    CreatedAt = k.CreatedAt,
                    ExpiresAt = k.ExpiresAt,
                    LastUsedAt = k.LastUsedAt,
                    IsRevoked = k.IsRevoked,
                    RevokedAt = k.RevokedAt,
                    Roles = k.Roles.Select(r => r.Role).ToList(),
                    Scopes = k.Scopes.Select(s => s.Scope).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RevokeApiKeyAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("API key ID is required", nameof(id));
            }

            try
            {
                // Find the API key in the database
                var apiKeyEntity = await _dbContext.ApiKeys.FindAsync(id);
                if (apiKeyEntity == null)
                {
                    return false;
                }

                // Revoke the API key
                apiKeyEntity.IsRevoked = true;
                apiKeyEntity.RevokedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key {ApiKeyId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ApiKey> UpdateApiKeyAsync(string id, string name, DateTime? expiresAt = null, IEnumerable<string> roles = null, IEnumerable<string> scopes = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("API key ID is required", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("API key name is required", nameof(name));
            }

            try
            {
                // Find the API key in the database
                var apiKeyEntity = await _dbContext.ApiKeys
                    .Include(k => k.Roles)
                    .Include(k => k.Scopes)
                    .FirstOrDefaultAsync(k => k.Id == id);

                if (apiKeyEntity == null)
                {
                    return null;
                }

                // Update the API key
                apiKeyEntity.Name = name;
                apiKeyEntity.ExpiresAt = expiresAt;

                // Update roles
                apiKeyEntity.Roles.Clear();
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        apiKeyEntity.Roles.Add(new ApiKeyRoleEntity
                        {
                            ApiKeyId = apiKeyEntity.Id,
                            Role = role
                        });
                    }
                }

                // Update scopes
                apiKeyEntity.Scopes.Clear();
                if (scopes != null)
                {
                    foreach (var scope in scopes)
                    {
                        apiKeyEntity.Scopes.Add(new ApiKeyScopeEntity
                        {
                            ApiKeyId = apiKeyEntity.Id,
                            Scope = scope
                        });
                    }
                }

                // Save to the database
                await _dbContext.SaveChangesAsync();

                // Map to API key
                return new ApiKey
                {
                    Id = apiKeyEntity.Id,
                    Name = apiKeyEntity.Name,
                    Key = null, // We don't store the actual key
                    UserId = apiKeyEntity.UserId,
                    CreatedAt = apiKeyEntity.CreatedAt,
                    ExpiresAt = apiKeyEntity.ExpiresAt,
                    LastUsedAt = apiKeyEntity.LastUsedAt,
                    IsRevoked = apiKeyEntity.IsRevoked,
                    RevokedAt = apiKeyEntity.RevokedAt,
                    Roles = apiKeyEntity.Roles.Select(r => r.Role).ToList(),
                    Scopes = apiKeyEntity.Scopes.Select(s => s.Scope).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key {ApiKeyId}", id);
                throw;
            }
        }

        /// <summary>
        /// Generates a new API key.
        /// </summary>
        /// <returns>The generated API key.</returns>
        private string GenerateApiKey()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Hashes an API key.
        /// </summary>
        /// <param name="apiKey">The API key to hash.</param>
        /// <returns>The hashed API key.</returns>
        private string HashApiKey(string apiKey)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(apiKey);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
