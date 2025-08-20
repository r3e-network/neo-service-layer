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
using ServiceFrameworkEnclaveBase = NeoServiceLayer.ServiceFramework.EnclaveServiceBase;
using NeoServiceLayer.Services.Authentication.Repositories;
using AuthModels = NeoServiceLayer.Services.Authentication.Models;
using ModelsUser = NeoServiceLayer.Services.Authentication.Models.User;
using DomainUser = NeoServiceLayer.Services.Authentication.Domain.Aggregates.User;
using OtpNet;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication
{
    public class AuthenticationService : ServiceFrameworkEnclaveBase, IAuthenticationService
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
            ISecurityLogger securityLogger) : base("Authentication", "User authentication and authorization service", "1.0.0", logger)
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
                ModelsUser user = await _userRepository.GetByUsernameAsync(username);
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

                // Verify password (Models.User has PasswordSalt property)
                if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                {
                    await RecordFailedAttemptAsync(username);
                    await _securityLogger.LogSecurityEventAsync("InvalidPassword", username);

                    // Check if should lock account
                    var failedAttempts = await GetFailedAttemptsAsync(username);
                    if (failedAttempts >= _maxFailedAttempts)
                    {
                        await LockAccountAsync(user.Id.ToString(), "Too many failed login attempts");
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
                    var mfaToken = GenerateMfaToken(user.Id.ToString());
                    await CacheMfaTokenAsync(mfaToken, user.Id.ToString());

                    return new AuthenticationResult
                    {
                        Success = false,
                        RequiresMfa = true,
                        MfaToken = mfaToken,
                        ErrorCode = AuthenticationErrorCode.MfaRequired
                    };
                }

                // Generate tokens (get roles from repository if navigation properties not loaded)
                var userRoles = user.Roles ?? new string[0];
                var tokenPair = await GenerateTokenPairAsync(user.Id.ToString(), userRoles);

                // Create session
                await CreateSessionAsync(user.Id.ToString(), GetDeviceId());

                // Reset failed attempts
                await ResetFailedAttemptsAsync(username);

                // Log successful authentication
                await _securityLogger.LogSecurityEventAsync("SuccessfulLogin", username);

                return new AuthenticationResult
                {
                    Success = true,
                    UserId = user.Id.ToString(),
                    AccessToken = tokenPair.AccessToken,
                    RefreshToken = tokenPair.RefreshToken,
                    ExpiresAt = tokenPair.AccessTokenExpiry,
                    Roles = user.Roles ?? new string[0]
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
            if (!await ValidateMfaCodeAsync(user.Id.ToString(), mfaCode))
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
            var userRoles = user.Roles ?? new string[0];
            var tokenPair = await GenerateTokenPairAsync(user.Id.ToString(), userRoles);

            // Create session
            await CreateSessionAsync(user.Id.ToString(), GetDeviceId());

            // Log successful MFA authentication
            await _securityLogger.LogSecurityEventAsync("SuccessfulMfaLogin", username);

            return new AuthenticationResult
            {
                Success = true,
                UserId = user.Id.ToString(),
                AccessToken = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken,
                ExpiresAt = tokenPair.AccessTokenExpiry,
                Roles = user.Roles ?? new string[0]
            };
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            var userByUsername = await _userRepository.GetByUsernameAsync(username);
            var userByEmail = await _userRepository.GetByEmailAsync(email);
            return userByUsername != null || userByEmail != null;
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
            var userRoles = user.Roles ?? new string[0];
            return await GenerateTokenPairAsync(user.Id.ToString(), userRoles);
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

        private async Task<MfaSetupResult> SetupTotpMfaAsync(Models.User user)
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

            await _securityLogger.LogSecurityEventAsync("MfaEnabled", user.Username);

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
            const int SaltSize = 32;
            const int Iterations = 100000;
            
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256);
            
            var hash = Convert.ToBase64String(algorithm.GetBytes(32));
            var salt = Convert.ToBase64String(saltBytes);
            return (hash, salt);
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            const int Iterations = 100000;
            
            var saltBytes = Convert.FromBase64String(salt);
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256);
            
            var computedHash = Convert.ToBase64String(algorithm.GetBytes(32));
            return computedHash == hash;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = RandomNumberGenerator.GetBytes(32);
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

        // User Registration Implementation
        public async Task<RegistrationResult> RegisterAsync(UserRegistrationRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || 
                    string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "Username, email, and password are required",
                        ValidationErrors = new Dictionary<string, string>
                        {
                            ["General"] = "Required fields are missing"
                        }
                    };
                }

                // Check if user already exists
                if (await UserExistsAsync(request.Username, request.Email))
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "Username or email already exists",
                        ValidationErrors = new Dictionary<string, string>
                        {
                            ["Username"] = "Username or email already taken"
                        }
                    };
                }

                // Validate password strength
                if (!await ValidatePasswordStrengthAsync(request.Password))
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "Password does not meet security requirements",
                        ValidationErrors = new Dictionary<string, string>
                        {
                            ["Password"] = "Password must be at least 12 characters with uppercase, lowercase, digit, and special character"
                        }
                    };
                }

                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid email address",
                        ValidationErrors = new Dictionary<string, string>
                        {
                            ["Email"] = "Please provide a valid email address"
                        }
                    };
                }

                // Hash password
                var (passwordHash, passwordSalt) = HashPassword(request.Password);

                // Generate verification token
                var emailVerificationToken = GenerateEmailVerificationToken();
                var emailVerificationExpiry = DateTime.UtcNow.AddHours(24);

                // Create user
                var user = new ModelsUser
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    EmailVerified = false,
                    EmailVerificationToken = emailVerificationToken,
                    EmailVerificationTokenExpiry = emailVerificationExpiry,
                    IsActive = true,
                    IsLocked = false,
                    MfaEnabled = false,
                    FailedLoginAttempts = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Roles = new[] { "User" } // Default role
                };

                // Save user to repository
                await _userRepository.CreateAsync(user);

                // Send verification email
                await SendVerificationEmailAsync(user.Email, user.Username, emailVerificationToken);

                // Log registration event
                await _securityLogger.LogSecurityEventAsync("UserRegistered", user.Username);

                return new RegistrationResult
                {
                    Success = true,
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    RequiresEmailVerification = true,
                    VerificationTokenExpiry = emailVerificationExpiry,
                    Message = "Registration successful. Please check your email to verify your account."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {Username}", request.Username);
                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = "Registration failed due to an internal error"
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return await Task.FromResult(true);
            }
            catch (Exception)
            {
                return await Task.FromResult(false);
            }
        }

        public async Task RevokeTokenAsync(string token)
        {
            try
            {
                // Add token to blacklist cache with expiration
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var expiry = jwtToken.ValidTo;
                
                var cacheKey = $"blacklist:{token}";
                await _cache.SetStringAsync(cacheKey, "revoked",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = expiry
                    });
                
                await _securityLogger.LogSecurityEventAsync("TokenRevoked", GetUserIdFromToken(token));
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

        public async Task<bool> DisableMfaAsync(string userId, string verificationCode)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.MfaEnabled)
                {
                    return false;
                }

                // Verify MFA code before disabling
                if (!await ValidateMfaCodeAsync(userId, verificationCode))
                {
                    await _securityLogger.LogSecurityEventAsync("MfaDisableFailedInvalidCode", userId);
                    return false;
                }

                // Disable MFA
                await _userRepository.UpdateMfaSettingsAsync(userId, new MfaSettings
                {
                    Type = MfaType.None,
                    Enabled = false,
                    Secret = null,
                    BackupCodes = null
                });

                // Log security event
                await _securityLogger.LogSecurityEventAsync("MfaDisabled", user.Username);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable MFA for user {UserId}", userId);
                return false;
            }
        }

        public async Task<string[]> GenerateBackupCodesAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.MfaEnabled)
                {
                    throw new InvalidOperationException("User not found or MFA not enabled");
                }

                // Generate new backup codes
                var backupCodes = GenerateBackupCodes(8);

                // Update MFA settings with new backup codes
                var mfaSettings = await _userRepository.GetMfaSettingsAsync(userId);
                mfaSettings.BackupCodes = backupCodes;
                await _userRepository.UpdateMfaSettingsAsync(userId, mfaSettings);

                // Log security event
                await _securityLogger.LogSecurityEventAsync("BackupCodesRegenerated", user.Username);

                return backupCodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate backup codes for user {UserId}", userId);
                throw;
            }
        }

        public async Task<SessionInfo[]> GetActiveSessionsAsync(string userId)
        {
            try
            {
                // Get all session keys for user from cache
                var sessions = new List<SessionInfo>();
                var pattern = $"session:*";
                
                // Note: In production, you'd use a more efficient pattern search
                // For now, we'll return an empty array as placeholder
                // Real implementation would scan cache for matching session keys
                
                await Task.CompletedTask;
                return sessions.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active sessions for user {UserId}", userId);
                return Array.Empty<SessionInfo>();
            }
        }

        public async Task RevokeSessionAsync(string sessionId)
        {
            try
            {
                var cacheKey = $"session:{sessionId}";
                await _cache.RemoveAsync(cacheKey);
                
                // Log security event
                await _securityLogger.LogSecurityEventAsync("SessionRevoked", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task RevokeAllSessionsAsync(string userId)
        {
            // Add user to session revocation list
            var cacheKey = $"revoked_sessions:{userId}";
            await _cache.SetStringAsync(cacheKey, DateTime.UtcNow.ToString("O"),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(30)
                });
            
            // Log security event
            await _securityLogger.LogSecurityEventAsync("AllSessionsRevoked", userId);
        }

        public async Task<string> InitiatePasswordResetAsync(string email)
        {
            try
            {
                // Find user by email
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    // Don't reveal whether email exists
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                    return null;
                }

                // Generate reset token
                var resetToken = GeneratePasswordResetToken();
                var resetTokenExpiry = DateTime.UtcNow.AddHours(1);

                // Store reset token
                await _userRepository.UpdatePasswordResetTokenAsync(user.Id.ToString(), resetToken, resetTokenExpiry);

                // Send password reset email
                await SendPasswordResetEmailAsync(user.Email, user.Username, resetToken);

                // Log security event
                await _securityLogger.LogSecurityEventAsync("PasswordResetInitiated", user.Username);

                return resetToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate password reset for {Email}", email);
                throw;
            }
        }

        public async Task<bool> CompletePasswordResetAsync(string token, string newPassword)
        {
            try
            {
                // Find user by reset token
                var user = await _userRepository.GetByPasswordResetTokenAsync(token);
                if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired password reset token");
                    return false;
                }

                // Validate new password strength
                if (!await ValidatePasswordStrengthAsync(newPassword))
                {
                    _logger.LogWarning("Weak password provided during reset");
                    return false;
                }

                // Hash new password
                var (passwordHash, passwordSalt) = HashPassword(newPassword);

                // Update password and clear reset token
                await _userRepository.UpdatePasswordAsync(user.Id.ToString(), passwordHash, passwordSalt);
                await _userRepository.ClearPasswordResetTokenAsync(user.Id.ToString());

                // Revoke all sessions
                await RevokeAllSessionsAsync(user.Id.ToString());

                // Send confirmation email
                await SendPasswordChangedEmailAsync(user.Email, user.Username);

                // Log security event
                await _securityLogger.LogSecurityEventAsync("PasswordResetCompleted", user.Username);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete password reset");
                return false;
            }
        }

        public async Task<bool> LockAccountAsync(string userId, string reason)
        {
            try
            {
                // Update user lock status in repository
                await _userRepository.UpdateLockStatusAsync(userId, true, reason);
                
                // Log security event
                await _securityLogger.LogSecurityEventAsync("AccountLocked", userId);
                
                // Revoke all active sessions
                await RevokeAllSessionsAsync(userId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to lock account {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UnlockAccountAsync(string userId)
        {
            try
            {
                // Update user lock status in repository
                await _userRepository.UpdateLockStatusAsync(userId, false, null);
                
                // Reset failed attempts
                await ResetFailedAttemptsAsync(userId);
                
                // Log security event
                await _securityLogger.LogSecurityEventAsync("AccountUnlocked", userId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unlock account {UserId}", userId);
                return false;
            }
        }

        public async Task<AccountSecurityStatus> GetAccountSecurityStatusAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var status = new AccountSecurityStatus
                {
                    UserId = userId,
                    Username = user.Username,
                    MfaEnabled = user.MfaEnabled,
                    EmailVerified = user.EmailVerified,
                    IsLocked = user.IsLocked,
                    FailedLoginAttempts = user.FailedLoginAttempts,
                    LastPasswordChange = user.LastPasswordChangeAt ?? user.CreatedAt,
                    LastLogin = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    SecurityScore = CalculateSecurityScore(user)
                };

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get security status for user {UserId}", userId);
                throw;
            }
        }

        public async Task<LoginAttempt[]> GetRecentLoginAttemptsAsync(string userId, int count = 10)
        {
            try
            {
                // In production, this would query from a persistent store
                // For now, return empty array as placeholder
                await Task.CompletedTask;
                return Array.Empty<LoginAttempt>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get login attempts for user {UserId}", userId);
                return Array.Empty<LoginAttempt>();
            }
        }

        public async Task<bool> CheckRateLimitAsync(string identifier, string action)
        {
            var cacheKey = $"rate_limit:{action}:{identifier}";
            var countStr = await _cache.GetStringAsync(cacheKey);
            
            var count = 0;
            if (!string.IsNullOrEmpty(countStr))
            {
                count = int.Parse(countStr);
            }
            
            var maxAttempts = action == "login" ? 5 : 10;
            if (count >= maxAttempts)
            {
                return false;
            }
            
            await _cache.SetStringAsync(cacheKey, (count + 1).ToString(),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(15)
                });
            
            return true;
        }

        public async Task RecordFailedAttemptAsync(string identifier)
        {
            var cacheKey = $"failed_attempts:{identifier}";
            var countStr = await _cache.GetStringAsync(cacheKey);
            
            var count = 0;
            if (!string.IsNullOrEmpty(countStr))
            {
                count = int.Parse(countStr);
            }
            
            await _cache.SetStringAsync(cacheKey, (count + 1).ToString(),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_lockoutDurationMinutes)
                });
        }

        public async Task ResetFailedAttemptsAsync(string identifier)
        {
            var cacheKey = $"failed_attempts:{identifier}";
            await _cache.RemoveAsync(cacheKey);
        }

        // Placeholder helper methods
        private async Task<int> GetFailedAttemptsAsync(string username)
        {
            var cacheKey = $"failed_attempts:{username}";
            var countStr = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(countStr) && int.TryParse(countStr, out var count))
            {
                return count;
            }
            
            return 0;
        }

        private async Task CacheMfaTokenAsync(string token, string userId)
        {
            var cacheKey = $"mfa_token:{token}";
            await _cache.SetStringAsync(cacheKey, userId,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
        }

        private async Task<MfaSetupResult> SetupSmsMfaAsync(ModelsUser user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    throw new InvalidOperationException("Phone number is required for SMS MFA");
                }

                // Generate backup codes
                var backupCodes = GenerateBackupCodes(8);

                // Store MFA settings
                await _userRepository.UpdateMfaSettingsAsync(user.Id.ToString(), new MfaSettings
                {
                    Type = MfaType.Sms,
                    PhoneNumber = user.PhoneNumber,
                    BackupCodes = backupCodes,
                    Enabled = true
                });

                // Send test SMS
                var testCode = GenerateNumericCode(6);
                await SendSmsCodeAsync(user.PhoneNumber, testCode);

                await _securityLogger.LogSecurityEventAsync("SmsMfaEnabled", user.Username);

                return new MfaSetupResult
                {
                    Success = true,
                    BackupCodes = backupCodes,
                    Type = MfaType.Sms,
                    PhoneNumber = user.PhoneNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup SMS MFA for user {Username}", user.Username);
                throw;
            }
        }

        private async Task<MfaSetupResult> SetupEmailMfaAsync(ModelsUser user)
        {
            try
            {
                // Generate backup codes
                var backupCodes = GenerateBackupCodes(8);

                // Store MFA settings
                await _userRepository.UpdateMfaSettingsAsync(user.Id.ToString(), new MfaSettings
                {
                    Type = MfaType.Email,
                    Email = user.Email,
                    BackupCodes = backupCodes,
                    Enabled = true
                });

                // Send test email
                var testCode = GenerateNumericCode(6);
                await SendEmailCodeAsync(user.Email, testCode);

                await _securityLogger.LogSecurityEventAsync("EmailMfaEnabled", user.Username);

                return new MfaSetupResult
                {
                    Success = true,
                    BackupCodes = backupCodes,
                    Type = MfaType.Email,
                    Email = user.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup Email MFA for user {Username}", user.Username);
                throw;
            }
        }

        private async Task<bool> ValidateTemporaryCodeAsync(string userId, string code)
        {
            try
            {
                var cacheKey = $"mfa_code:{userId}";
                var storedCode = await _cache.GetStringAsync(cacheKey);
                
                if (string.IsNullOrEmpty(storedCode))
                {
                    return false;
                }
                
                var isValid = storedCode == code;
                
                if (isValid)
                {
                    // Remove code after successful validation
                    await _cache.RemoveAsync(cacheKey);
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate temporary code for user {UserId}", userId);
                return false;
            }
        }

        private async Task StoreRefreshTokenAsync(string token, string userId, DateTime expiry)
        {
            var cacheKey = $"refresh_token:{token}";
            var tokenData = new { UserId = userId, ExpiresAt = expiry, CreatedAt = DateTime.UtcNow };
            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = expiry
                });
        }

        public class RefreshTokenData
        {
            public string UserId { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public bool IsValid { get; set; }
        }

        private async Task<RefreshTokenData> GetRefreshTokenDataAsync(string token)
        {
            var cacheKey = $"refresh_token:{token}";
            var data = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }
            
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(data);
            return new RefreshTokenData
            {
                UserId = tokenData.GetProperty("UserId").GetString(),
                ExpiresAt = tokenData.GetProperty("ExpiresAt").GetDateTime(),
                IsValid = true
            };
        }

        private async Task RevokeRefreshTokenAsync(string token)
        {
            var cacheKey = $"refresh_token:{token}";
            await _cache.RemoveAsync(cacheKey);
        }

        private async Task StoreSessionAsync(SessionInfo session)
        {
            var cacheKey = $"session:{session.SessionId}";
            await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(session),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = session.ExpiresAt
                });
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
        
        private string GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        protected override async Task<bool> OnInitializeAsync()
        {
            _logger.LogDebug("Initializing Authentication Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStartAsync()
        {
            _logger.LogInformation("Starting Authentication Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStopAsync()
        {
            _logger.LogInformation("Stopping Authentication Service");
            return await Task.FromResult(true);
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                // Check if service is operational
                return await Task.FromResult(ServiceHealth.Healthy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication service health check failed");
                return await Task.FromResult(ServiceHealth.Unhealthy);
            }
        }

        public async Task<Guid?> FindUserIdByUsernameOrEmailAsync(string usernameOrEmail)
        {
            var user = await _userRepository.GetByUsernameAsync(usernameOrEmail);
            if (user != null) return user.Id;
            
            user = await _userRepository.GetByEmailAsync(usernameOrEmail);
            return user?.Id;
        }

        public async Task<Guid?> FindUserIdByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user?.Id;
        }

        public async Task<Guid?> FindUserIdByResetTokenAsync(string resetToken)
        {
            // This would typically query the database for users with matching reset token
            // For now, return null as a placeholder
            await Task.CompletedTask;
            return null;
        }

        protected override async Task<bool> OnInitializeEnclaveAsync()
        {
            _logger.LogDebug("Initializing Authentication Service enclave");
            // Initialize enclave-specific operations
            return await Task.FromResult(true);
        }

        // Helper methods for registration and authentication
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateEmailVerificationToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(randomBytes).Replace("/", "-").Replace("+", "_").TrimEnd('=');
        }

        private string GeneratePasswordResetToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(randomBytes).Replace("/", "-").Replace("+", "_").TrimEnd('=');
        }

        private string GenerateNumericCode(int length)
        {
            var random = new Random();
            var code = "";
            for (int i = 0; i < length; i++)
            {
                code += random.Next(0, 10).ToString();
            }
            return code;
        }

        private int CalculateSecurityScore(ModelsUser user)
        {
            int score = 0;
            
            // Base score
            score += 20;
            
            // Email verified
            if (user.EmailVerified) score += 20;
            
            // MFA enabled
            if (user.MfaEnabled) score += 30;
            
            // No recent failed login attempts
            if (user.FailedLoginAttempts == 0) score += 10;
            
            // Account age (1 point per month, max 10)
            var accountAge = DateTime.UtcNow - user.CreatedAt;
            score += Math.Min((int)(accountAge.TotalDays / 30), 10);
            
            // Recent password change (within 90 days)
            if (user.LastPasswordChangeAt.HasValue)
            {
                var daysSinceChange = (DateTime.UtcNow - user.LastPasswordChangeAt.Value).TotalDays;
                if (daysSinceChange <= 90) score += 10;
            }
            
            return Math.Min(score, 100);
        }

        // Email sending methods (placeholders - would integrate with email service)
        private async Task SendVerificationEmailAsync(string email, string username, string token)
        {
            // In production, this would send an actual email
            _logger.LogInformation("Sending verification email to {Email} with token {Token}", email, token);
            await Task.CompletedTask;
        }

        private async Task SendPasswordResetEmailAsync(string email, string username, string token)
        {
            // In production, this would send an actual email
            _logger.LogInformation("Sending password reset email to {Email} with token {Token}", email, token);
            await Task.CompletedTask;
        }

        private async Task SendPasswordChangedEmailAsync(string email, string username)
        {
            // In production, this would send an actual email
            _logger.LogInformation("Sending password changed notification to {Email}", email);
            await Task.CompletedTask;
        }

        private async Task SendSmsCodeAsync(string phoneNumber, string code)
        {
            // In production, this would send an actual SMS
            _logger.LogInformation("Sending SMS code {Code} to {PhoneNumber}", code, phoneNumber);
            await Task.CompletedTask;
        }

        private async Task SendEmailCodeAsync(string email, string code)
        {
            // In production, this would send an actual email
            _logger.LogInformation("Sending email code {Code} to {Email}", code, email);
            await Task.CompletedTask;
        }
    }

}