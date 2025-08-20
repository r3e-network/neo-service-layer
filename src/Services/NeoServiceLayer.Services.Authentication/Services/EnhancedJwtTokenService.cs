using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NeoServiceLayer.Core;
using ServiceFrameworkBase = NeoServiceLayer.ServiceFramework.ServiceBase;
using System.ComponentModel.DataAnnotations;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Services
{
    /// <summary>
    /// Represents refresh token information stored in cache.
    /// </summary>
    public class RefreshTokenInfo
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, object> CustomClaims { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
    }
    /// <summary>
    /// Enhanced JWT token service with improved security features including:
    /// - Configurable token expiration
    /// - Refresh token rotation
    /// - Token revocation support
    /// - Audience validation
    /// - Custom claims support
    /// </summary>
    public class EnhancedJwtTokenService : ServiceFrameworkBase, IEnhancedJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EnhancedJwtTokenService> _logger;
        private readonly ITokenBlacklistService _blacklistService;
        private readonly IDistributedCache _cache;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;
        private readonly bool _requireHttps;

        public EnhancedJwtTokenService(
            IConfiguration configuration,
            ILogger<EnhancedJwtTokenService> logger,
            ITokenBlacklistService blacklistService,
            IDistributedCache cache)
            : base("EnhancedJwtTokenService", "1.0.0", "Enhanced JWT token service with comprehensive features", logger)
        {
            _configuration = configuration;
            _logger = logger;
            _blacklistService = blacklistService;
            _cache = cache;

            // Load configuration with validation
            _jwtSecret = configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("JWT secret not configured");

            if (_jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("JWT secret must be at least 32 characters");
            }

            _jwtIssuer = configuration["Jwt:Issuer"] ?? "NeoServiceLayer";
            _jwtAudience = configuration["Jwt:Audience"] ?? "NeoServiceLayer.Api";
            _accessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
            _refreshTokenExpirationDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
            _requireHttps = configuration.GetValue<bool>("Jwt:RequireHttps", true);
        }

        public async Task<AuthenticationTokens> GenerateTokensAsync(
            Guid userId,
            string username,
            string email,
            List<string> roles,
            Dictionary<string, string> customClaims = null)
        {
            var jwtId = Guid.NewGuid().ToString();
            var accessToken = GenerateAccessToken(userId, username, email, roles, jwtId, customClaims);
            var refreshToken = GenerateRefreshToken();

            // Store refresh token metadata for validation
            await StoreRefreshTokenAsync(userId, refreshToken, jwtId);

            _logger.LogInformation("Generated new token pair for user {UserId}", userId);

            return new AuthenticationTokens
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                TokenType = "Bearer"
            };
        }

        private string GenerateAccessToken(
            Guid userId,
            string username,
            string email,
            List<string> roles,
            string jwtId,
            Dictionary<string, string> customClaims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim("token_type", "access")
            };

            // Add roles
            foreach (var role in roles ?? new List<string>())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add custom claims
            if (customClaims != null)
            {
                foreach (var claim in customClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512),
                NotBefore = DateTime.UtcNow
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            // Add timestamp to ensure uniqueness
            var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var combined = new byte[randomNumber.Length + timestamp.Length];
            Buffer.BlockCopy(randomNumber, 0, combined, 0, randomNumber.Length);
            Buffer.BlockCopy(timestamp, 0, combined, randomNumber.Length, timestamp.Length);

            return Convert.ToBase64String(combined)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public async Task<TokenValidationResult> ValidateAccessTokenAsync(string token)
        {
            try
            {
                // Check if token is blacklisted
                if (await _blacklistService.IsBlacklistedAsync(token))
                {
                    return new TokenValidationResult
                    {
                        IsValid = false,
                        Error = "Token has been revoked"
                    };
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;

                // Extract user information
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? throw new SecurityTokenException("User ID not found in token"));
                var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
                var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                return new TokenValidationResult
                {
                    IsValid = true,
                    UserId = userId,
                    Username = username,
                    Email = email,
                    TokenId = jti,
                    Roles = roles,
                    Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value)
                };
            }
            catch (SecurityTokenExpiredException)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    Error = "Token has expired"
                };
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                return new TokenValidationResult
                {
                    IsValid = false,
                    Error = "Invalid token"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token validation");
                return new TokenValidationResult
                {
                    IsValid = false,
                    Error = "Token validation failed"
                };
            }
        }

        public async Task<AuthenticationTokens> RefreshTokensAsync(string refreshToken)
        {
            // Validate refresh token
            var tokenInfo = await ValidateRefreshTokenAsync(refreshToken);
            if (tokenInfo == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // Rotate refresh token (invalidate old, generate new)
            await InvalidateRefreshTokenAsync(refreshToken);

            // Generate new token pair
            return await GenerateTokensAsync(
                tokenInfo.UserId,
                tokenInfo.Username,
                tokenInfo.Email,
                tokenInfo.Roles,
                tokenInfo.CustomClaims);
        }

        public async Task RevokeTokenAsync(string token)
        {
            // Add to blacklist
            await _blacklistService.BlacklistTokenAsync(token);

            // Extract token ID for additional cleanup
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                await InvalidateTokenFamilyAsync(jti);
            }

            _logger.LogInformation("Token revoked: {TokenId}", jti);
        }

        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            // This would typically update a user's token version or similar mechanism
            await _blacklistService.BlacklistUserTokensAsync(userId);
            _logger.LogInformation("All tokens revoked for user {UserId}", userId);
        }

        private async Task StoreRefreshTokenAsync(Guid userId, string token, string jwtId)
        {
            try
            {
                // Store refresh token metadata in distributed cache
                var tokenInfo = new RefreshTokenInfo
                {
                    UserId = userId,
                    Token = token,
                    JwtId = jwtId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                    IsUsed = false
                };

                var cacheKey = $"refresh_token:{token}";
                var serializedToken = System.Text.Json.JsonSerializer.Serialize(tokenInfo);
                
                await _cache.SetStringAsync(cacheKey, serializedToken, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_refreshTokenExpirationDays)
                }).ConfigureAwait(false);

                _logger.LogDebug("Stored refresh token for user {UserId} with JWT ID {JwtId}", userId, jwtId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store refresh token for user {UserId}", userId);
                throw;
            }
        }

        private async Task<RefreshTokenInfo> ValidateRefreshTokenAsync(string token)
        {
            try
            {
                // Validate refresh token from cache
                var cacheKey = $"refresh_token:{token}";
                var tokenData = await _cache.GetStringAsync(cacheKey).ConfigureAwait(false);
                
                if (string.IsNullOrEmpty(tokenData))
                {
                    _logger.LogWarning("Refresh token not found in cache");
                    return null;
                }

                var tokenInfo = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenInfo>(tokenData);
                if (tokenInfo == null)
                {
                    _logger.LogWarning("Failed to deserialize refresh token data");
                    return null;
                }

                // Check if token is expired
                if (tokenInfo.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token has expired");
                    await _cache.RemoveAsync(cacheKey).ConfigureAwait(false);
                    return null;
                }

                // Check if token is already used
                if (tokenInfo.IsUsed)
                {
                    _logger.LogWarning("Refresh token has already been used");
                    return null;
                }

                return tokenInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token");
                return null;
            }
        }

        private async Task InvalidateRefreshTokenAsync(string token)
        {
            try
            {
                // Mark refresh token as used/invalid in cache
                var cacheKey = $"refresh_token:{token}";
                var tokenData = await _cache.GetStringAsync(cacheKey).ConfigureAwait(false);
                
                if (!string.IsNullOrEmpty(tokenData))
                {
                    var tokenInfo = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenInfo>(tokenData);
                    if (tokenInfo != null)
                    {
                        tokenInfo.IsUsed = true;
                        var updatedTokenData = System.Text.Json.JsonSerializer.Serialize(tokenInfo);
                        
                        await _cache.SetStringAsync(cacheKey, updatedTokenData, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) // Keep for audit trail
                        }).ConfigureAwait(false);
                        
                        _logger.LogDebug("Invalidated refresh token for user {UserId}", tokenInfo.UserId);
                    }
                }
                else
                {
                    _logger.LogWarning("Attempted to invalidate non-existent refresh token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate refresh token");
                throw;
            }
        }

        private async Task InvalidateTokenFamilyAsync(string jwtId)
        {
            try
            {
                // Invalidate all tokens with the same JWT ID family from cache
                var familyKey = $"token_family:{jwtId}";
                var familyTokensData = await _cache.GetStringAsync(familyKey).ConfigureAwait(false);
                
                if (!string.IsNullOrEmpty(familyTokensData))
                {
                    var tokenList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(familyTokensData);
                    if (tokenList != null)
                    {
                        foreach (var token in tokenList)
                        {
                            await InvalidateRefreshTokenAsync(token).ConfigureAwait(false);
                        }
                        
                        // Remove the family tracking
                        await _cache.RemoveAsync(familyKey).ConfigureAwait(false);
                        
                        _logger.LogInformation("Invalidated token family {JwtId} containing {TokenCount} tokens", 
                            jwtId, tokenList.Count);
                    }
                }
                else
                {
                    _logger.LogDebug("No token family found for JWT ID {JwtId}", jwtId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate token family {JwtId}", jwtId);
                throw;
            }
        }

        protected override async Task<bool> OnInitializeAsync()
        {
            _logger.LogDebug("Initializing Enhanced JWT Token Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStartAsync()
        {
            _logger.LogInformation("Starting Enhanced JWT Token Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStopAsync()
        {
            _logger.LogInformation("Enhanced JWT Token Service stopping...");
            return await Task.FromResult(true);
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                // Check if JWT secret is configured
                if (string.IsNullOrEmpty(_jwtSecret))
                {
                    _logger.LogWarning("JWT secret is not configured");
                    return await Task.FromResult(ServiceHealth.Unhealthy);
                }

                // Check if service is running
                if (!IsRunning)
                {
                    return await Task.FromResult(ServiceHealth.Degraded);
                }

                return await Task.FromResult(ServiceHealth.Healthy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced JWT Token service health check failed");
                return await Task.FromResult(ServiceHealth.Unhealthy);
            }
        }
    }

    public interface IEnhancedJwtTokenService
    {
        Task<AuthenticationTokens> GenerateTokensAsync(
            Guid userId,
            string username,
            string email,
            List<string> roles,
            Dictionary<string, string> customClaims = null);

        Task<TokenValidationResult> ValidateAccessTokenAsync(string token);
        Task<AuthenticationTokens> RefreshTokensAsync(string refreshToken);
        Task RevokeTokenAsync(string token);
        Task RevokeAllUserTokensAsync(Guid userId);
    }

    public interface ITokenBlacklistService
    {
        Task<bool> IsBlacklistedAsync(string token);
        Task BlacklistTokenAsync(string token);
        Task BlacklistUserTokensAsync(Guid userId);
    }

    public class AuthenticationTokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public string TokenType { get; set; }
    }

    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string TokenId { get; set; }
        public List<string> Roles { get; set; }
        public Dictionary<string, string> Claims { get; set; }
        public string Error { get; set; }
    }

    public class RefreshTokenInfo
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public Dictionary<string, string> CustomClaims { get; set; }
    }
}