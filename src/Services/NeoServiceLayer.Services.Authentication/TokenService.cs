using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NeoServiceLayer.Infrastructure.Observability.Logging;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// Enhanced JWT token service with refresh tokens and blacklisting
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly IStructuredLogger _structuredLogger;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private readonly string _jwtSecret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public TokenService(
            ILogger<TokenService> logger,
            IStructuredLoggerFactory structuredLoggerFactory,
            IConfiguration configuration,
            IDistributedCache cache)
        {
            _logger = logger;
            _structuredLogger = structuredLoggerFactory?.CreateLogger("TokenService");
            _configuration = configuration;
            _cache = cache;

            _jwtSecret = configuration["Authentication:JwtSecret"]
                ?? throw new InvalidOperationException("JWT secret not configured");

            if (_jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("JWT secret must be at least 32 characters");
            }

            _issuer = configuration["Authentication:Issuer"] ?? "NeoServiceLayer";
            _audience = configuration["Authentication:Audience"] ?? "NeoServiceLayer";
            _accessTokenExpiryMinutes = configuration.GetValue<int>("Authentication:AccessTokenExpiryMinutes", 15);
            _refreshTokenExpiryDays = configuration.GetValue<int>("Authentication:RefreshTokenExpiryDays", 30);

            _tokenHandler = new JwtSecurityTokenHandler();
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
            };
        }

        public async Task<TokenPair> GenerateTokenPairAsync(string userId, string[] roles, Dictionary<string, string> additionalClaims = null)
        {
            _structuredLogger?.LogOperation("GenerateTokenPair", new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["Roles"] = string.Join(",", roles ?? Array.Empty<string>())
            });

            var accessToken = GenerateAccessToken(userId, roles, additionalClaims);
            var refreshToken = await GenerateRefreshTokenAsync(userId);

            var tokenPair = new TokenPair
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessToken.ExpiresAt,
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays)
            };

            // Store refresh token in cache
            var cacheKey = $"refresh_token:{refreshToken}";
            var cacheValue = new RefreshTokenData
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = tokenPair.RefreshTokenExpiry
            };

            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(cacheValue),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenPair.RefreshTokenExpiry
                });

            _logger.LogInformation("Token pair generated for user {UserId}", userId);
            return tokenPair;
        }

        public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
        {
            _structuredLogger?.LogOperation("RefreshToken");

            // Retrieve refresh token from cache
            var cacheKey = $"refresh_token:{refreshToken}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogWarning("Invalid or expired refresh token");
                throw new SecurityTokenException("Invalid or expired refresh token");
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenData>(cachedData);

            // Check if token is expired
            if (tokenData.ExpiresAt < DateTime.UtcNow)
            {
                await _cache.RemoveAsync(cacheKey);
                throw new SecurityTokenException("Refresh token expired");
            }

            // Check if token has been used (one-time use)
            if (tokenData.Used)
            {
                _logger.LogWarning("Refresh token reuse detected for user {UserId}", tokenData.UserId);
                // Revoke all tokens for this user as a security measure
                await RevokeAllUserTokensAsync(tokenData.UserId);
                throw new SecurityTokenException("Refresh token has already been used");
            }

            // Mark token as used
            tokenData.Used = true;
            tokenData.UsedAt = DateTime.UtcNow;
            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            // Get user roles (would typically fetch from database)
            var roles = await GetUserRolesAsync(tokenData.UserId);

            // Generate new token pair
            return await GenerateTokenPairAsync(tokenData.UserId, roles);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                // Check if token is blacklisted
                if (await IsTokenBlacklistedAsync(token))
                {
                    return false;
                }

                // Validate token structure and signature
                var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

                // Additional validation
                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Token validation failed");
                return false;
            }
        }

        public async Task RevokeTokenAsync(string token)
        {
            _structuredLogger?.LogOperation("RevokeToken");

            try
            {
                // Parse token to get expiration
                var jwtToken = _tokenHandler.ReadJwtToken(token);
                var exp = jwtToken.ValidTo;

                // Add to blacklist with expiration
                var cacheKey = $"blacklist:{token}";
                await _cache.SetStringAsync(cacheKey, "revoked",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = exp
                    });

                _logger.LogInformation("Token revoked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke token");
                throw;
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            var cacheKey = $"blacklist:{token}";
            var result = await _cache.GetStringAsync(cacheKey);
            return !string.IsNullOrEmpty(result);
        }

        public ClaimsPrincipal ValidateAndGetPrincipal(string token)
        {
            try
            {
                var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public string GetUserIdFromToken(string token)
        {
            try
            {
                var jwtToken = _tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            catch
            {
                return null;
            }
        }

        private (string Token, DateTime ExpiresAt) GenerateAccessToken(string userId, string[] roles, Dictionary<string, string> additionalClaims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("session_id", Guid.NewGuid().ToString())
            };

            // Add roles
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // Add additional claims
            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return (_tokenHandler.WriteToken(token), expiresAt);
        }

        private async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            // Generate a cryptographically secure random token
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var refreshToken = Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            // Ensure uniqueness
            var cacheKey = $"refresh_token:{refreshToken}";
            var exists = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(exists))
            {
                // Rare case of collision, regenerate
                return await GenerateRefreshTokenAsync(userId);
            }

            return refreshToken;
        }

        private async Task RevokeAllUserTokensAsync(string userId)
        {
            // In a production system, this would invalidate all tokens for a user
            // This could be done by maintaining a per-user token version/generation number
            _logger.LogWarning("Revoking all tokens for user {UserId} due to security concern", userId);

            // Add user to temporary blacklist
            var cacheKey = $"user_blacklist:{userId}";
            await _cache.SetStringAsync(cacheKey, DateTime.UtcNow.ToString(),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
        }

        public string GenerateAccessToken(Guid userId, string username, List<string> roles)
        {
            var result = GenerateAccessToken(userId.ToString(), roles?.ToArray() ?? Array.Empty<string>(), null);
            return result.Token;
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public async Task<(Guid userId, Guid tokenId)> ValidateRefreshTokenAsync(string refreshToken)
        {
            // Validate refresh token from cache storage
            var cacheKey = $"refresh_token:{refreshToken}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(cachedData))
            {
                throw new SecurityTokenException("Invalid refresh token");
            }
            
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenData>(cachedData);
            if (tokenData.ExpiresAt < DateTime.UtcNow)
            {
                throw new SecurityTokenException("Refresh token expired");
            }
            
            return (Guid.Parse(tokenData.UserId), Guid.NewGuid()); // tokenId would come from database in real implementation
        }

        private async Task<string[]> GetUserRolesAsync(string userId)
        {
            try
            {
                // Retrieve user roles from cache (populated by user service)
                var cacheKey = $"user_roles:{userId}";
                var cachedRoles = await _cache.GetStringAsync(cacheKey);
                
                if (!string.IsNullOrEmpty(cachedRoles))
                {
                    var roles = JsonSerializer.Deserialize<string[]>(cachedRoles);
                    if (roles != null && roles.Length > 0)
                    {
                        return roles;
                    }
                }
                
                // Fallback to default user role if no roles found
                _logger.LogWarning("No roles found for user {UserId}, using default 'user' role", userId);
                return new[] { "user" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user {UserId}, using default role", userId);
                return new[] { "user" };
            }
        }

        private class RefreshTokenData
        {
            public string UserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool Used { get; set; }
            public DateTime? UsedAt { get; set; }
        }
    }

    /// <summary>
    /// Token service interface
    /// </summary>
    public interface ITokenService
    {
        Task<TokenPair> GenerateTokenPairAsync(string userId, string[] roles, Dictionary<string, string> additionalClaims = null);
        Task<TokenPair> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task RevokeTokenAsync(string token);
        Task<bool> IsTokenBlacklistedAsync(string token);
        ClaimsPrincipal ValidateAndGetPrincipal(string token);
        string GetUserIdFromToken(string token);
        
        // Additional methods expected by command handlers
        string GenerateAccessToken(Guid userId, string username, List<string> roles);
        string GenerateRefreshToken();
        Task<(Guid userId, Guid tokenId)> ValidateRefreshTokenAsync(string refreshToken);
    }
}