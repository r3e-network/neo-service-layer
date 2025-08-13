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
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Security;
using NeoServiceLayer.ServiceFramework;
using OtpNet;

namespace NeoServiceLayer.Services.Authentication
{
    public class AuthenticationService : EnclaveServiceBase, IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private readonly IUserRepository _userRepository;
        private readonly ISecurityLogger _securityLogger;
        private readonly string _jwtSecret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;
        private readonly int _maxFailedAttempts;
        private readonly int _lockoutDurationMinutes;

        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            IConfiguration configuration,
            IDistributedCache cache,
            IUserRepository userRepository,
            ISecurityLogger securityLogger) : base(logger)
        {
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
            _userRepository = userRepository;
            _securityLogger = securityLogger;
            
            _jwtSecret = configuration["Authentication:JwtSecret"] 
                ?? throw new InvalidOperationException("JWT secret not configured. Please set Authentication:JwtSecret in configuration.");
            
            if (string.IsNullOrWhiteSpace(_jwtSecret) || _jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("JWT secret must be at least 32 characters long for security.");
            }
            
            _issuer = configuration["Authentication:Issuer"] 
                ?? throw new InvalidOperationException("Authentication:Issuer must be configured.");
            _audience = configuration["Authentication:Audience"] 
                ?? throw new InvalidOperationException("Authentication:Audience must be configured.");
            _accessTokenExpiryMinutes = configuration.GetValue<int>("Authentication:AccessTokenExpiryMinutes", 15);
            _refreshTokenExpiryDays = configuration.GetValue<int>("Authentication:RefreshTokenExpiryDays", 30);
            _maxFailedAttempts = configuration.GetValue<int>("Authentication:MaxFailedAttempts", 5);
            _lockoutDurationMinutes = configuration.GetValue<int>("Authentication:LockoutDurationMinutes", 30);
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            try
            {
                // Check rate limiting
                if (!await CheckRateLimitAsync(username, "login"))
                {
                    await _securityLogger.LogSecurityEventAsync("RateLimitExceeded", username);
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Too many login attempts. Please try again later.",
                        ErrorCode = AuthenticationErrorCode.RateLimitExceeded
                    };
                }

                // Get user
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    await RecordFailedAttemptAsync(username);
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid credentials",
                        ErrorCode = AuthenticationErrorCode.InvalidCredentials
                    };
                }

                // Check if account is locked
                if (user.IsLocked)
                {
                    await _securityLogger.LogSecurityEventAsync("LockedAccountAccess", username);
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Account is locked",
                        ErrorCode = AuthenticationErrorCode.AccountLocked
                    };
                }

                // Verify password
                if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                {
                    await RecordFailedAttemptAsync(username);
                    await _securityLogger.LogSecurityEventAsync("InvalidPassword", username);
                    
                    // Check if should lock account
                    var failedAttempts = await GetFailedAttemptsAsync(username);
                    if (failedAttempts >= _maxFailedAttempts)
                    {
                        await LockAccountAsync(user.Id, "Too many failed login attempts");
                    }
                    
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid credentials",
                        ErrorCode = AuthenticationErrorCode.InvalidCredentials
                    };
                }

                // Check if MFA is required
                if (user.MfaEnabled)
                {
                    var mfaToken = GenerateMfaToken(user.Id);
                    await CacheMfaTokenAsync(mfaToken, user.Id);
                    
                    return new AuthenticationResult
                    {
                        Success = false,
                        RequiresMfa = true,
                        MfaToken = mfaToken,
                        ErrorCode = AuthenticationErrorCode.MfaRequired
                    };
                }

                // Generate tokens
                var tokenPair = await GenerateTokenPairAsync(user.Id, user.Roles);
                
                // Create session
                await CreateSessionAsync(user.Id, GetDeviceId());
                
                // Reset failed attempts
                await ResetFailedAttemptsAsync(username);
                
                // Log successful authentication
                await _securityLogger.LogSecurityEventAsync("SuccessfulLogin", username);
                
                return new AuthenticationResult
                {
                    Success = true,
                    UserId = user.Id,
                    AccessToken = tokenPair.AccessToken,
                    RefreshToken = tokenPair.RefreshToken,
                    ExpiresAt = tokenPair.AccessTokenExpiry,
                    Roles = user.Roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed for user {Username}", username);
                throw;
            }
        }

        public async Task<AuthenticationResult> AuthenticateWithMfaAsync(string username, string password, string mfaCode)
        {
            // First authenticate with username/password
            var authResult = await AuthenticateAsync(username, password);
            
            if (!authResult.RequiresMfa)
            {
                return authResult;
            }

            // Validate MFA code
            var user = await _userRepository.GetByUsernameAsync(username);
            if (!await ValidateMfaCodeAsync(user.Id, mfaCode))
            {
                await _securityLogger.LogSecurityEventAsync("InvalidMfaCode", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Invalid MFA code",
                    ErrorCode = AuthenticationErrorCode.InvalidMfaCode
                };
            }

            // Generate tokens
            var tokenPair = await GenerateTokenPairAsync(user.Id, user.Roles);
            
            // Create session
            await CreateSessionAsync(user.Id, GetDeviceId());
            
            // Log successful MFA authentication
            await _securityLogger.LogSecurityEventAsync("SuccessfulMfaLogin", username);
            
            return new AuthenticationResult
            {
                Success = true,
                UserId = user.Id,
                AccessToken = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken,
                ExpiresAt = tokenPair.AccessTokenExpiry,
                Roles = user.Roles
            };
        }

        public async Task<TokenPair> GenerateTokenPairAsync(string userId, string[] roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);
            
            // Generate access token
            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                }.Concat(roles.Select(role => new Claim(ClaimTypes.Role, role)))),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
            var accessTokenString = tokenHandler.WriteToken(accessToken);
            
            // Generate refresh token
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);
            
            // Store refresh token
            await StoreRefreshTokenAsync(refreshToken, userId, refreshTokenExpiry);
            
            return new TokenPair
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessTokenDescriptor.Expires.Value,
                RefreshTokenExpiry = refreshTokenExpiry
            };
        }

        public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
        {
            // Validate refresh token
            var tokenData = await GetRefreshTokenDataAsync(refreshToken);
            if (tokenData == null || tokenData.ExpiresAt < DateTime.UtcNow)
            {
                throw new SecurityTokenException("Invalid or expired refresh token");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(tokenData.UserId);
            if (user == null || user.IsLocked)
            {
                throw new SecurityTokenException("User not found or account locked");
            }

            // Revoke old refresh token
            await RevokeRefreshTokenAsync(refreshToken);
            
            // Generate new token pair
            return await GenerateTokenPairAsync(user.Id, user.Roles);
        }

        public async Task<MfaSetupResult> SetupMfaAsync(string userId, MfaType type)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            switch (type)
            {
                case MfaType.Totp:
                    return await SetupTotpMfaAsync(user);
                case MfaType.Sms:
                    return await SetupSmsMfaAsync(user);
                case MfaType.Email:
                    return await SetupEmailMfaAsync(user);
                default:
                    throw new NotSupportedException($"MFA type {type} is not supported");
            }
        }

        private async Task<MfaSetupResult> SetupTotpMfaAsync(User user)
        {
            // Generate secret
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(key);
            
            // Generate QR code URL
            var qrCodeUrl = $"otpauth://totp/NeoServiceLayer:{user.Username}?secret={base32Secret}&issuer=NeoServiceLayer";
            
            // Generate backup codes
            var backupCodes = GenerateBackupCodes(8);
            
            // Store MFA settings
            await _userRepository.UpdateMfaSettingsAsync(user.Id, new MfaSettings
            {
                Type = MfaType.Totp,
                Secret = base32Secret,
                BackupCodes = backupCodes,
                Enabled = true
            });
            
            await _securityLogger.LogSecurityEventAsync("MfaEnabled", user.Username, new { Type = "TOTP" });
            
            return new MfaSetupResult
            {
                Success = true,
                Secret = base32Secret,
                QrCodeUrl = qrCodeUrl,
                BackupCodes = backupCodes,
                Type = MfaType.Totp
            };
        }

        public async Task<bool> ValidateMfaCodeAsync(string userId, string code)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.MfaEnabled)
            {
                return false;
            }

            var mfaSettings = await _userRepository.GetMfaSettingsAsync(userId);
            
            switch (mfaSettings.Type)
            {
                case MfaType.Totp:
                    return ValidateTotpCode(mfaSettings.Secret, code);
                case MfaType.Sms:
                case MfaType.Email:
                    return await ValidateTemporaryCodeAsync(userId, code);
                default:
                    return false;
            }
        }

        private bool ValidateTotpCode(string secret, string code)
        {
            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key);
            return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
        }

        public async Task<SessionInfo> CreateSessionAsync(string userId, string deviceId)
        {
            var session = new SessionInfo
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = userId,
                DeviceId = deviceId,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            await StoreSessionAsync(session);
            return session;
        }

        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify current password
            if (!VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            {
                await _securityLogger.LogSecurityEventAsync("InvalidPasswordChange", user.Username);
                return false;
            }

            // Validate new password strength
            if (!await ValidatePasswordStrengthAsync(newPassword))
            {
                return false;
            }

            // Hash new password
            var (hash, salt) = HashPassword(newPassword);
            
            // Update password
            await _userRepository.UpdatePasswordAsync(userId, hash, salt);
            
            // Revoke all sessions
            await RevokeAllSessionsAsync(userId);
            
            await _securityLogger.LogSecurityEventAsync("PasswordChanged", user.Username);
            
            return true;
        }

        public async Task<bool> ValidatePasswordStrengthAsync(string password)
        {
            // Minimum requirements
            if (password.Length < 12)
                return false;
                
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
            
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        // Helper methods
        private (string hash, string salt) HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = Convert.ToBase64String(hmac.Key);
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return (hash, salt);
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using var hmac = new HMACSHA512(saltBytes);
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return computedHash == hash;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string[] GenerateBackupCodes(int count)
        {
            var codes = new string[count];
            for (int i = 0; i < count; i++)
            {
                codes[i] = GenerateBackupCode();
            }
            return codes;
        }

        private string GenerateBackupCode()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateMfaToken(string userId)
        {
            return Guid.NewGuid().ToString("N");
        }

        // Placeholder methods for additional functionality
        public async Task<RegistrationResult> RegisterAsync(UserRegistrationRequest request)
        {
            // Implementation would include user creation, email verification, etc.
            throw new NotImplementedException();
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            // Implementation would validate JWT token
            throw new NotImplementedException();
        }

        public async Task RevokeTokenAsync(string token)
        {
            // Implementation would blacklist the token
            throw new NotImplementedException();
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            // Check if token is in blacklist
            throw new NotImplementedException();
        }

        public async Task<bool> DisableMfaAsync(string userId, string verificationCode)
        {
            // Implementation would disable MFA after verification
            throw new NotImplementedException();
        }

        public async Task<string[]> GenerateBackupCodesAsync(string userId)
        {
            // Generate new backup codes
            throw new NotImplementedException();
        }

        public async Task<SessionInfo[]> GetActiveSessionsAsync(string userId)
        {
            // Get all active sessions for user
            throw new NotImplementedException();
        }

        public async Task RevokeSessionAsync(string sessionId)
        {
            // Revoke specific session
            throw new NotImplementedException();
        }

        public async Task RevokeAllSessionsAsync(string userId)
        {
            // Revoke all user sessions
            throw new NotImplementedException();
        }

        public async Task<string> InitiatePasswordResetAsync(string email)
        {
            // Start password reset flow
            throw new NotImplementedException();
        }

        public async Task<bool> CompletePasswordResetAsync(string token, string newPassword)
        {
            // Complete password reset
            throw new NotImplementedException();
        }

        public async Task<bool> LockAccountAsync(string userId, string reason)
        {
            // Lock user account
            throw new NotImplementedException();
        }

        public async Task<bool> UnlockAccountAsync(string userId)
        {
            // Unlock user account
            throw new NotImplementedException();
        }

        public async Task<AccountSecurityStatus> GetAccountSecurityStatusAsync(string userId)
        {
            // Get security status
            throw new NotImplementedException();
        }

        public async Task<LoginAttempt[]> GetRecentLoginAttemptsAsync(string userId, int count = 10)
        {
            // Get recent login attempts
            throw new NotImplementedException();
        }

        public async Task<bool> CheckRateLimitAsync(string identifier, string action)
        {
            // Check rate limiting
            throw new NotImplementedException();
        }

        public async Task RecordFailedAttemptAsync(string identifier)
        {
            // Record failed login attempt
            throw new NotImplementedException();
        }

        public async Task ResetFailedAttemptsAsync(string identifier)
        {
            // Reset failed attempts counter
            throw new NotImplementedException();
        }

        // Placeholder helper methods
        private async Task<int> GetFailedAttemptsAsync(string username)
        {
            // Get failed attempts count
            throw new NotImplementedException();
        }

        private async Task CacheMfaTokenAsync(string token, string userId)
        {
            // Cache MFA token temporarily
            throw new NotImplementedException();
        }

        private async Task<MfaSetupResult> SetupSmsMfaAsync(User user)
        {
            // Setup SMS MFA
            throw new NotImplementedException();
        }

        private async Task<MfaSetupResult> SetupEmailMfaAsync(User user)
        {
            // Setup Email MFA
            throw new NotImplementedException();
        }

        private async Task<bool> ValidateTemporaryCodeAsync(string userId, string code)
        {
            // Validate temporary MFA code
            throw new NotImplementedException();
        }

        private async Task StoreRefreshTokenAsync(string token, string userId, DateTime expiry)
        {
            // Store refresh token
            throw new NotImplementedException();
        }

        private async Task<RefreshTokenData> GetRefreshTokenDataAsync(string token)
        {
            // Get refresh token data
            throw new NotImplementedException();
        }

        private async Task RevokeRefreshTokenAsync(string token)
        {
            // Revoke refresh token
            throw new NotImplementedException();
        }

        private async Task StoreSessionAsync(SessionInfo session)
        {
            // Store session
            throw new NotImplementedException();
        }

        private string GetDeviceId()
        {
            // Get device ID from request
            return "device-" + Guid.NewGuid().ToString("N");
        }

        private string GetClientIpAddress()
        {
            // Get client IP from request
            return "127.0.0.1";
        }

        private string GetUserAgent()
        {
            // Get user agent from request
            return "Mozilla/5.0";
        }
    }

    // Supporting classes
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<string> CreateAsync(User user);
        Task UpdateAsync(User user);
        Task UpdatePasswordAsync(string userId, string passwordHash, string passwordSalt);
        Task UpdateMfaSettingsAsync(string userId, MfaSettings settings);
        Task<MfaSettings> GetMfaSettingsAsync(string userId);
    }

    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string[] Roles { get; set; }
        public bool IsLocked { get; set; }
        public bool MfaEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class MfaSettings
    {
        public MfaType Type { get; set; }
        public string Secret { get; set; }
        public string[] BackupCodes { get; set; }
        public bool Enabled { get; set; }
    }

    public class RefreshTokenData
    {
        public string Token { get; set; }
        public string UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
    }
}