using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using ServiceFrameworkBase = NeoServiceLayer.ServiceFramework.ServiceBase;
using NeoServiceLayer.Services.Authentication.Infrastructure;
using NeoServiceLayer.Services.Authentication.Queries;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Services
{
    /// <summary>
    /// Comprehensive authentication service with advanced security features:
    /// - Account lockout after failed attempts
    /// - Rate limiting per IP and user
    /// - Session management
    /// - Multi-factor authentication support
    /// - Audit logging
    /// </summary>
    public class ComprehensiveAuthenticationService : ServiceFrameworkBase, IComprehensiveAuthenticationService
    {
        private readonly ILogger<ComprehensiveAuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserReadModelStore _userStore;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEnhancedJwtTokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IMemoryCache _memoryCache;
        private readonly IAuditService _auditService;

        private readonly int _maxFailedAttempts;
        private readonly int _lockoutDurationMinutes;
        private readonly int _rateLimitPerMinute;
        private readonly bool _require2FA;

        public ComprehensiveAuthenticationService(
            ILogger<ComprehensiveAuthenticationService> logger,
            IConfiguration configuration,
            IUserReadModelStore userStore,
            IPasswordHasher passwordHasher,
            IEnhancedJwtTokenService tokenService,
            ITwoFactorService twoFactorService,
            IMemoryCache memoryCache,
            IAuditService auditService)
            : base("ComprehensiveAuthenticationService", "1.0.0", "Comprehensive authentication service with advanced features", logger)
        {
            _logger = logger;
            _configuration = configuration;
            _userStore = userStore;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _memoryCache = memoryCache;
            _auditService = auditService;

            // Load configuration
            _maxFailedAttempts = configuration.GetValue<int>("Authentication:MaxFailedAttempts", 5);
            _lockoutDurationMinutes = configuration.GetValue<int>("Authentication:LockoutDurationMinutes", 30);
            _rateLimitPerMinute = configuration.GetValue<int>("Authentication:RateLimitPerMinute", 10);
            _require2FA = configuration.GetValue<bool>("Authentication:Require2FA", false);
        }

        public async Task<AuthenticationResult> AuthenticateAsync(
            string usernameOrEmail,
            string password,
            string ipAddress,
            string userAgent,
            string twoFactorCode = null)
        {
            try
            {
                // Check rate limiting
                if (!await CheckRateLimitAsync(ipAddress))
                {
                    await _auditService.LogAuthenticationAttemptAsync(
                        usernameOrEmail, ipAddress, false, "Rate limit exceeded");

                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Too many attempts. Please try again later.",
                        ErrorCode = AuthenticationErrorCode.RateLimitExceeded
                    };
                }

                // Find user
                var user = await _userStore.GetByUsernameAsync(usernameOrEmail)
                    ?? await _userStore.GetByEmailAsync(usernameOrEmail);

                if (user == null)
                {
                    await _auditService.LogAuthenticationAttemptAsync(
                        usernameOrEmail, ipAddress, false, "User not found");

                    // Don't reveal that user doesn't exist
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Invalid credentials",
                        ErrorCode = AuthenticationErrorCode.InvalidCredentials
                    };
                }

                // Check if account is locked
                if (await IsAccountLockedAsync(user.Id))
                {
                    await _auditService.LogAuthenticationAttemptAsync(
                        user.Username, ipAddress, false, "Account locked");

                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Account is locked. Please try again later.",
                        ErrorCode = AuthenticationErrorCode.AccountLocked
                    };
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
                {
                    await RecordFailedAttemptAsync(user.Id, ipAddress);
                    await _auditService.LogAuthenticationAttemptAsync(
                        user.Username, ipAddress, false, "Invalid password");

                    var remainingAttempts = _maxFailedAttempts - await GetFailedAttemptsAsync(user.Id);

                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = remainingAttempts > 0
                            ? $"Invalid credentials. {remainingAttempts} attempts remaining."
                            : "Account has been locked due to too many failed attempts.",
                        ErrorCode = AuthenticationErrorCode.InvalidCredentials,
                        RemainingAttempts = Math.Max(0, remainingAttempts)
                    };
                }

                // Check if email is verified
                if (!user.EmailVerified)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Email address not verified. Please check your email.",
                        ErrorCode = AuthenticationErrorCode.EmailNotVerified
                    };
                }

                // Check 2FA if enabled
                if (user.TwoFactorEnabled || _require2FA)
                {
                    if (string.IsNullOrEmpty(twoFactorCode))
                    {
                        return new AuthenticationResult
                        {
                            Success = false,
                            RequiresTwoFactor = true,
                            Error = "Two-factor authentication code required",
                            ErrorCode = AuthenticationErrorCode.TwoFactorRequired
                        };
                    }

                    bool isValidCode = false;

                    // Check TOTP code
                    if (!string.IsNullOrEmpty(user.TwoFactorSecret))
                    {
                        isValidCode = _twoFactorService.ValidateTotp(user.TwoFactorSecret, twoFactorCode);
                    }

                    // Check backup codes if TOTP fails
                    if (!isValidCode && user.BackupCodes != null)
                    {
                        isValidCode = await ValidateBackupCodeAsync(user.Id, twoFactorCode);
                    }

                    if (!isValidCode)
                    {
                        await RecordFailedAttemptAsync(user.Id, ipAddress);
                        await _auditService.LogAuthenticationAttemptAsync(
                            user.Username, ipAddress, false, "Invalid 2FA code");

                        return new AuthenticationResult
                        {
                            Success = false,
                            Error = "Invalid two-factor authentication code",
                            ErrorCode = AuthenticationErrorCode.InvalidTwoFactorCode
                        };
                    }
                }

                // Generate tokens
                var tokens = await _tokenService.GenerateTokensAsync(
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Roles,
                    new Dictionary<string, string>
                    {
                        ["ip_address"] = ipAddress,
                        ["user_agent"] = userAgent
                    });

                // Create session
                var sessionId = await CreateSessionAsync(user.Id, ipAddress, userAgent, tokens.RefreshToken);

                // Clear failed attempts
                await ClearFailedAttemptsAsync(user.Id);

                // Log successful authentication
                await _auditService.LogAuthenticationAttemptAsync(
                    user.Username, ipAddress, true, "Authentication successful");

                _logger.LogInformation(
                    "User {Username} authenticated successfully from {IpAddress}",
                    user.Username, ipAddress);

                return new AuthenticationResult
                {
                    Success = true,
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = user.Roles,
                    Tokens = tokens,
                    SessionId = sessionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error for user {UsernameOrEmail}", usernameOrEmail);

                return new AuthenticationResult
                {
                    Success = false,
                    Error = "An error occurred during authentication",
                    ErrorCode = AuthenticationErrorCode.SystemError
                };
            }
        }

        public async Task<bool> ValidateSessionAsync(string sessionId, string ipAddress)
        {
            var cacheKey = $"session_{sessionId}";

            if (_memoryCache.TryGetValue<SessionInfo>(cacheKey, out var session))
            {
                // Validate IP address if strict mode
                if (_configuration.GetValue<bool>("Authentication:StrictSessionValidation", false))
                {
                    return session.IpAddress == ipAddress && session.IsActive;
                }

                return session.IsActive;
            }

            // Check database for session
            // Implementation would depend on your data access layer
            return false;
        }

        public async Task LogoutAsync(string sessionId, Guid userId)
        {
            // Invalidate session
            var cacheKey = $"session_{sessionId}";
            _memoryCache.Remove(cacheKey);

            // Revoke tokens
            await _tokenService.RevokeAllUserTokensAsync(userId);

            // Log logout event
            await _auditService.LogLogoutAsync(userId, sessionId);

            _logger.LogInformation("User {UserId} logged out from session {SessionId}", userId, sessionId);
        }

        private async Task<bool> CheckRateLimitAsync(string ipAddress)
        {
            var cacheKey = $"rate_limit_{ipAddress}";
            var attempts = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (attempts >= _rateLimitPerMinute)
            {
                return false;
            }

            _memoryCache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(1));
            return true;
        }

        private async Task<bool> IsAccountLockedAsync(Guid userId)
        {
            var cacheKey = $"lockout_{userId}";
            return _memoryCache.TryGetValue(cacheKey, out _);
        }

        private async Task RecordFailedAttemptAsync(Guid userId, string ipAddress)
        {
            var cacheKey = $"failed_attempts_{userId}";
            var attempts = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return 0;
            });

            attempts++;
            _memoryCache.Set(cacheKey, attempts, TimeSpan.FromMinutes(30));

            if (attempts >= _maxFailedAttempts)
            {
                // Lock account
                var lockoutKey = $"lockout_{userId}";
                _memoryCache.Set(lockoutKey, true, TimeSpan.FromMinutes(_lockoutDurationMinutes));

                _logger.LogWarning(
                    "Account {UserId} locked due to {Attempts} failed attempts from {IpAddress}",
                    userId, attempts, ipAddress);
            }
        }

        private async Task<int> GetFailedAttemptsAsync(Guid userId)
        {
            var cacheKey = $"failed_attempts_{userId}";
            return _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return 0;
            });
        }

        private async Task ClearFailedAttemptsAsync(Guid userId)
        {
            var cacheKey = $"failed_attempts_{userId}";
            _memoryCache.Remove(cacheKey);
        }

        private async Task<bool> ValidateBackupCodeAsync(Guid userId, string code)
        {
            // This would check and consume a backup code from database
            // Implementation would depend on your data access layer
            return false;
        }

        private async Task<string> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, string refreshToken)
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RefreshToken = refreshToken,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true
            };

            var cacheKey = $"session_{sessionId}";
            _memoryCache.Set(cacheKey, sessionInfo, TimeSpan.FromDays(7));

            // Also persist to database
            // Implementation would depend on your data access layer

            return sessionId;
        }

        protected override async Task<bool> OnInitializeAsync()
        {
            _logger.LogDebug("Initializing Comprehensive Authentication Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStartAsync()
        {
            _logger.LogInformation("Starting Comprehensive Authentication Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStopAsync()
        {
            _logger.LogInformation("Stopping Comprehensive Authentication Service");
            return await Task.FromResult(true);
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                // Check if user store is accessible
                await _userStore.GetAllAsync();
                return await Task.FromResult(ServiceHealth.Healthy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive Authentication service health check failed");
                return await Task.FromResult(ServiceHealth.Unhealthy);
            }
        }

        public async Task<AuthenticationResult> RegisterAsync(string username, string email, string password, bool acceptTerms = true)
        {
            try
            {
                // Check if user already exists
                var existingUsers = await _userStore.GetAllAsync();
                if (existingUsers.Any(u => u.Username == username || u.Email == email))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorCode = AuthenticationErrorCode.InvalidCredentials
                    };
                }

                // Create new user
                var userId = Guid.NewGuid();
                var user = new UserReadModel
                {
                    Id = userId,
                    Username = username,
                    Email = email,
                    PasswordHash = _passwordHasher.HashPassword(password),
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active",
                    EmailVerified = false,
                    TwoFactorEnabled = false,
                    Roles = new List<string> { "User" }
                };

                await _userStore.SaveAsync(user);

                // Generate tokens
                var tokens = await _tokenService.GenerateTokensAsync(
                    userId,
                    username,
                    email,
                    user.Roles,
                    new Dictionary<string, string>());

                return new AuthenticationResult
                {
                    Success = true,
                    UserId = userId,
                    Username = username,
                    Email = email,
                    Roles = user.Roles,
                    Tokens = tokens
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for username {Username}", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorCode = AuthenticationErrorCode.SystemError
                };
            }
        }

        public async Task<UserProfile> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var user = await _userStore.GetByIdAsync(userId);
                if (user == null)
                {
                    return null;
                }

                return new UserProfile
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = ExtractFirstNameFromProfile(user),
                    LastName = "",
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Roles = user.Roles,
                    Metadata = new Dictionary<string, object>
                    {
                        ["EmailVerified"] = user.EmailVerified,
                        ["TwoFactorEnabled"] = user.TwoFactorEnabled,
                        ["IsActive"] = user.Status == "Active"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user profile for {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Extracts first name from user profile with intelligent parsing.
        /// </summary>
        private string ExtractFirstNameFromProfile(User user)
        {
            try
            {
                // Check if user has a display name or full name field
                if (!string.IsNullOrEmpty(user.DisplayName))
                {
                    var parts = user.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 0 ? parts[0] : user.Username;
                }

                // Check if username contains first/last name pattern
                if (user.Username.Contains('.'))
                {
                    var parts = user.Username.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 0 ? char.ToUpper(parts[0][0]) + parts[0][1..].ToLower() : user.Username;
                }

                // Check if email has name pattern
                if (!string.IsNullOrEmpty(user.Email) && user.Email.Contains('@'))
                {
                    var emailLocal = user.Email.Split('@')[0];
                    if (emailLocal.Contains('.'))
                    {
                        var parts = emailLocal.Split('.', StringSplitOptions.RemoveEmptyEntries);
                        return parts.Length > 0 ? char.ToUpper(parts[0][0]) + parts[0][1..].ToLower() : user.Username;
                    }
                }

                // Fallback to username with proper casing
                return !string.IsNullOrEmpty(user.Username) && user.Username.Length > 0 ? 
                    char.ToUpper(user.Username[0]) + user.Username[1..].ToLower() : 
                    "User";
            }
            catch
            {
                return user.Username ?? "User";
            }
        }
    }

    public interface IComprehensiveAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateAsync(
            string usernameOrEmail,
            string password,
            string ipAddress,
            string userAgent,
            string twoFactorCode = null);

        Task<bool> ValidateSessionAsync(string sessionId, string ipAddress);
        Task LogoutAsync(string sessionId, Guid userId);
        Task<AuthenticationResult> RegisterAsync(string username, string email, string password, bool acceptTerms = true);
        Task<UserProfile> GetUserProfileAsync(Guid userId);
    }

    public interface IAuditService
    {
        Task LogAuthenticationAttemptAsync(string username, string ipAddress, bool success, string reason);
        Task LogLogoutAsync(Guid userId, string sessionId);
        Task LogPasswordChangeAsync(Guid userId);
        Task LogSecurityEventAsync(string eventType, Guid? userId, string details);
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public AuthenticationTokens Tokens { get; set; }
        public string SessionId { get; set; }
        public string Error { get; set; }
        public AuthenticationErrorCode ErrorCode { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public int RemainingAttempts { get; set; }
        public string EmailVerificationToken { get; set; }
    }

    public enum AuthenticationErrorCode
    {
        None = 0,
        InvalidCredentials = 1,
        AccountLocked = 2,
        EmailNotVerified = 3,
        TwoFactorRequired = 4,
        InvalidTwoFactorCode = 5,
        RateLimitExceeded = 6,
        SessionExpired = 7,
        SystemError = 99
    }

    public class SessionInfo
    {
        public string SessionId { get; set; }
        public Guid UserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RefreshToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool EmailVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}