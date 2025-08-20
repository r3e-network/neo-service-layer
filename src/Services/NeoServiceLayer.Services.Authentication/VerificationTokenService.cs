using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// Result of token validation
    /// </summary>
    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Service for managing verification tokens (email verification, password reset, etc.)
    /// </summary>
    public interface IVerificationTokenService
    {
        Task<string> GenerateEmailVerificationTokenAsync(string userId, string email);
        Task<TokenValidationResult> ValidateEmailVerificationTokenAsync(string token);
        Task<string> GeneratePasswordResetTokenAsync(string userId, string email);
        Task<TokenValidationResult> ValidatePasswordResetTokenAsync(string token);
        Task<string> GenerateMfaTokenAsync(string userId, string method);
        Task<bool> ValidateMfaTokenAsync(string userId, string token, string method);
        Task RevokeTokenAsync(string token);
        Task RevokeAllUserTokensAsync(string userId, string tokenType);
    }

    public class VerificationTokenService : IVerificationTokenService
    {
        private readonly ILogger<VerificationTokenService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly int _emailVerificationExpiryHours;
        private readonly int _passwordResetExpiryMinutes;
        private readonly int _mfaTokenExpiryMinutes;

        public VerificationTokenService(
            ILogger<VerificationTokenService> logger,
            IDistributedCache cache,
            IConfiguration configuration)
        {
            _logger = logger;
            _cache = cache;
            _configuration = configuration;

            _emailVerificationExpiryHours = configuration.GetValue<int>("Authentication:EmailVerificationExpiryHours", 24);
            _passwordResetExpiryMinutes = configuration.GetValue<int>("Authentication:PasswordResetExpiryMinutes", 60);
            _mfaTokenExpiryMinutes = configuration.GetValue<int>("Authentication:MfaTokenExpiryMinutes", 5);
        }

        public async Task<string> GenerateEmailVerificationTokenAsync(string userId, string email)
        {
            var token = GenerateSecureToken();
            var cacheKey = $"email_verify:{token}";

            var tokenData = new VerificationTokenData
            {
                UserId = userId,
                Email = email,
                TokenType = "EmailVerification",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(_emailVerificationExpiryHours)
            };

            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            _logger.LogInformation("Email verification token generated for user {UserId}", userId);
            return token;
        }

        public async Task<TokenValidationResult> ValidateEmailVerificationTokenAsync(string token)
        {
            var result = new TokenValidationResult { IsValid = false };

            var cacheKey = $"email_verify:{token}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogWarning("Invalid or expired email verification token");
                return result;
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<VerificationTokenData>(cachedData);

            if (tokenData.ExpiresAt < DateTime.UtcNow)
            {
                await _cache.RemoveAsync(cacheKey);
                _logger.LogWarning("Expired email verification token for user {UserId}", tokenData.UserId);
                return result;
            }

            if (tokenData.Used)
            {
                _logger.LogWarning("Email verification token already used for user {UserId}", tokenData.UserId);
                return result;
            }

            // Mark token as used
            tokenData.Used = true;
            tokenData.UsedAt = DateTime.UtcNow;
            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            result.IsValid = true;
            result.UserId = tokenData.UserId;
            result.Email = tokenData.Email;

            _logger.LogInformation("Email verification token validated for user {UserId}", result.UserId);
            return result;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string userId, string email)
        {
            // Revoke any existing password reset tokens for this user
            await RevokeAllUserTokensAsync(userId, "PasswordReset");

            var token = GenerateSecureToken();
            var cacheKey = $"password_reset:{token}";

            var tokenData = new VerificationTokenData
            {
                UserId = userId,
                Email = email,
                TokenType = "PasswordReset",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_passwordResetExpiryMinutes)
            };

            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            // Also store by user ID for revocation
            var userTokenKey = $"user_tokens:{userId}:password_reset";
            await _cache.SetStringAsync(userTokenKey, token,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            _logger.LogInformation("Password reset token generated for user {UserId}", userId);
            return token;
        }

        public async Task<TokenValidationResult> ValidatePasswordResetTokenAsync(string token)
        {
            var result = new TokenValidationResult { IsValid = false };

            var cacheKey = $"password_reset:{token}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogWarning("Invalid or expired password reset token");
                return result;
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<VerificationTokenData>(cachedData);

            if (tokenData.ExpiresAt < DateTime.UtcNow)
            {
                await _cache.RemoveAsync(cacheKey);
                _logger.LogWarning("Expired password reset token for user {UserId}", tokenData.UserId);
                return result;
            }

            if (tokenData.Used)
            {
                _logger.LogWarning("Password reset token already used for user {UserId}", tokenData.UserId);
                return result;
            }

            // Mark token as used
            tokenData.Used = true;
            tokenData.UsedAt = DateTime.UtcNow;
            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            result.IsValid = true;
            result.UserId = tokenData.UserId;
            result.Email = tokenData.Email;

            _logger.LogInformation("Password reset token validated for user {UserId}", result.UserId);
            return result;
        }

        public async Task<string> GenerateMfaTokenAsync(string userId, string method)
        {
            // Generate a 6-digit code for MFA
            var code = GenerateMfaCode();
            var cacheKey = $"mfa:{userId}:{method}:{code}";

            var tokenData = new MfaTokenData
            {
                UserId = userId,
                Method = method,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_mfaTokenExpiryMinutes),
                Attempts = 0,
                MaxAttempts = 3
            };

            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            _logger.LogInformation("MFA token generated for user {UserId} using method {Method}", userId, method);
            return code;
        }

        public async Task<bool> ValidateMfaTokenAsync(string userId, string token, string method)
        {
            var cacheKey = $"mfa:{userId}:{method}:{token}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogWarning("Invalid MFA token for user {UserId}", userId);
                return false;
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<MfaTokenData>(cachedData);

            if (tokenData.ExpiresAt < DateTime.UtcNow)
            {
                await _cache.RemoveAsync(cacheKey);
                _logger.LogWarning("Expired MFA token for user {UserId}", userId);
                return false;
            }

            if (tokenData.Used)
            {
                _logger.LogWarning("MFA token already used for user {UserId}", userId);
                return false;
            }

            tokenData.Attempts++;

            if (tokenData.Attempts > tokenData.MaxAttempts)
            {
                await _cache.RemoveAsync(cacheKey);
                _logger.LogWarning("MFA token max attempts exceeded for user {UserId}", userId);
                return false;
            }

            // Mark token as used on successful validation
            tokenData.Used = true;
            tokenData.UsedAt = DateTime.UtcNow;
            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = tokenData.ExpiresAt
                });

            // Remove the token after successful use
            await _cache.RemoveAsync(cacheKey);

            _logger.LogInformation("MFA token validated for user {UserId}", userId);
            return true;
        }

        public async Task RevokeTokenAsync(string token)
        {
            var keys = new[]
            {
                $"email_verify:{token}",
                $"password_reset:{token}"
            };

            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key);
            }

            _logger.LogInformation("Token revoked: {Token}", token.Substring(0, 8) + "...");
        }

        public async Task RevokeAllUserTokensAsync(string userId, string tokenType)
        {
            // This implementation would need to track all tokens per user
            // For now, we'll just revoke known token types
            var userTokenKey = $"user_tokens:{userId}:{tokenType.ToLower()}";
            var token = await _cache.GetStringAsync(userTokenKey);

            if (!string.IsNullOrEmpty(token))
            {
                var tokenKey = tokenType switch
                {
                    "EmailVerification" => $"email_verify:{token}",
                    "PasswordReset" => $"password_reset:{token}",
                    _ => null
                };

                if (tokenKey != null)
                {
                    await _cache.RemoveAsync(tokenKey);
                }

                await _cache.RemoveAsync(userTokenKey);
            }

            _logger.LogInformation("All {TokenType} tokens revoked for user {UserId}", tokenType, userId);
        }

        private string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private string GenerateMfaCode()
        {
            var bytes = new byte[4];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var value = BitConverter.ToUInt32(bytes, 0);
            return (value % 900000 + 100000).ToString(); // 6-digit code between 100000-999999
        }

        private class VerificationTokenData
        {
            public string UserId { get; set; }
            public string Email { get; set; }
            public string TokenType { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool Used { get; set; }
            public DateTime? UsedAt { get; set; }
        }

        private class MfaTokenData
        {
            public string UserId { get; set; }
            public string Method { get; set; }
            public string Code { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public int Attempts { get; set; }
            public int MaxAttempts { get; set; }
            public bool Used { get; set; }
            public DateTime? UsedAt { get; set; }
        }
    }
}