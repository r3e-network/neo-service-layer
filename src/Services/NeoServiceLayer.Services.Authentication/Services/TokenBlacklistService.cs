using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication.Repositories;
using NeoServiceLayer.Services.Authentication.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Services
{
    /// <summary>
    /// Token blacklist service implementation with distributed caching
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly ILogger<TokenBlacklistService> _logger;
        private readonly ITokenBlacklistRepository _repository;
        private readonly IDistributedCache _cache;
        private const string CacheKeyPrefix = "blacklist:token:";
        private const string UserBlacklistPrefix = "blacklist:user:";

        public TokenBlacklistService(
            ILogger<TokenBlacklistService> logger,
            ITokenBlacklistRepository repository,
            IDistributedCache cache)
        {
            _logger = logger;
            _repository = repository;
            _cache = cache;
        }

        public async Task<bool> IsBlacklistedAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true; // Treat empty tokens as blacklisted
            }

            try
            {
                var tokenHash = ComputeTokenHash(token);
                var cacheKey = $"{CacheKeyPrefix}{tokenHash}";

                // Check cache first
                var cachedValue = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedValue))
                {
                    return true; // Token is blacklisted
                }

                // Check database
                var isBlacklisted = await _repository.IsBlacklistedAsync(tokenHash);

                if (isBlacklisted)
                {
                    // Add to cache for faster subsequent checks
                    await _cache.SetStringAsync(
                        cacheKey,
                        "1",
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                        });
                }

                return isBlacklisted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token blacklist");
                // In case of error, assume token is blacklisted for security
                return true;
            }
        }

        public async Task BlacklistTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            try
            {
                var tokenHash = ComputeTokenHash(token);

                // Extract token details
                var (jwtId, userId, expiresAt) = ExtractTokenDetails(token);

                // Add to database
                await _repository.BlacklistTokenAsync(
                    tokenHash,
                    jwtId,
                    userId,
                    expiresAt,
                    "Manually revoked");

                // Add to cache
                var cacheKey = $"{CacheKeyPrefix}{tokenHash}";
                var cacheExpiration = expiresAt > DateTime.UtcNow
                    ? expiresAt - DateTime.UtcNow
                    : TimeSpan.FromHours(24);

                await _cache.SetStringAsync(
                    cacheKey,
                    "1",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = cacheExpiration
                    });

                _logger.LogInformation("Token blacklisted: {TokenHash}", tokenHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blacklisting token");
                throw;
            }
        }

        public async Task BlacklistUserTokensAsync(Guid userId)
        {
            try
            {
                // Blacklist all user tokens in database
                await _repository.BlacklistUserTokensAsync(userId, "User tokens revoked");

                // Add user to blacklist cache
                var cacheKey = $"{UserBlacklistPrefix}{userId}";
                await _cache.SetStringAsync(
                    cacheKey,
                    DateTime.UtcNow.ToString("O"),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                    });

                _logger.LogInformation("All tokens blacklisted for user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blacklisting user tokens for {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsUserBlacklistedAsync(Guid userId)
        {
            try
            {
                var cacheKey = $"{UserBlacklistPrefix}{userId}";
                var cachedValue = await _cache.GetStringAsync(cacheKey);

                return !string.IsNullOrEmpty(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user blacklist for {UserId}", userId);
                // In case of error, assume user is blacklisted for security
                return true;
            }
        }

        public async Task CleanupExpiredEntriesAsync()
        {
            try
            {
                await _repository.CleanupExpiredBlacklistEntriesAsync();
                _logger.LogInformation("Cleaned up expired blacklist entries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired blacklist entries");
            }
        }

        private string ComputeTokenHash(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private (string jwtId, Guid? userId, DateTime expiresAt) ExtractTokenDetails(string token)
        {
            try
            {
                // Parse JWT token without validation to extract claims
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var jwtId = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                var userIdClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                var userId = Guid.TryParse(userIdClaim, out var uid) ? uid : (Guid?)null;

                var expiresAt = jwtToken.ValidTo;

                return (jwtId, userId, expiresAt);
            }
            catch
            {
                // If token parsing fails, return defaults
                return (null, null, DateTime.UtcNow.AddDays(1));
            }
        }
    }
}