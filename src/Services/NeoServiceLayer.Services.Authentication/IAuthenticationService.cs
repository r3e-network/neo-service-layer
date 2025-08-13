using System;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Services.Authentication
{
    public interface IAuthenticationService
    {
        // Core Authentication
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);
        Task<AuthenticationResult> AuthenticateWithMfaAsync(string username, string password, string mfaCode);
        Task<RegistrationResult> RegisterAsync(UserRegistrationRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task RevokeTokenAsync(string token);
        
        // Token Management
        Task<TokenPair> GenerateTokenPairAsync(string userId, string[] roles);
        Task<TokenPair> RefreshTokenAsync(string refreshToken);
        Task<bool> IsTokenBlacklistedAsync(string token);
        
        // MFA Management
        Task<MfaSetupResult> SetupMfaAsync(string userId, MfaType type);
        Task<bool> ValidateMfaCodeAsync(string userId, string code);
        Task<bool> DisableMfaAsync(string userId, string verificationCode);
        Task<string[]> GenerateBackupCodesAsync(string userId);
        
        // Session Management
        Task<SessionInfo> CreateSessionAsync(string userId, string deviceId);
        Task<SessionInfo[]> GetActiveSessionsAsync(string userId);
        Task RevokeSessionAsync(string sessionId);
        Task RevokeAllSessionsAsync(string userId);
        
        // Password Management
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<string> InitiatePasswordResetAsync(string email);
        Task<bool> CompletePasswordResetAsync(string token, string newPassword);
        Task<bool> ValidatePasswordStrengthAsync(string password);
        
        // Account Security
        Task<bool> LockAccountAsync(string userId, string reason);
        Task<bool> UnlockAccountAsync(string userId);
        Task<AccountSecurityStatus> GetAccountSecurityStatusAsync(string userId);
        Task<LoginAttempt[]> GetRecentLoginAttemptsAsync(string userId, int count = 10);
        
        // Rate Limiting & Protection
        Task<bool> CheckRateLimitAsync(string identifier, string action);
        Task RecordFailedAttemptAsync(string identifier);
        Task ResetFailedAttemptsAsync(string identifier);
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string[] Roles { get; set; }
        public bool RequiresMfa { get; set; }
        public string MfaToken { get; set; }
        public string ErrorMessage { get; set; }
        public AuthenticationErrorCode? ErrorCode { get; set; }
    }

    public class RegistrationResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public bool RequiresEmailVerification { get; set; }
        public string VerificationToken { get; set; }
    }

    public class UserRegistrationRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool AcceptTerms { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class TokenPair
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }

    public class MfaSetupResult
    {
        public bool Success { get; set; }
        public string Secret { get; set; }
        public string QrCodeUrl { get; set; }
        public string[] BackupCodes { get; set; }
        public MfaType Type { get; set; }
    }

    public enum MfaType
    {
        None = 0,
        Totp = 1,
        Sms = 2,
        Email = 3,
        Hardware = 4
    }

    public class SessionInfo
    {
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public string DeviceId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class AccountSecurityStatus
    {
        public string UserId { get; set; }
        public bool IsLocked { get; set; }
        public string LockReason { get; set; }
        public DateTime? LockedUntil { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool MfaEnabled { get; set; }
        public MfaType MfaType { get; set; }
        public DateTime LastPasswordChange { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public int ActiveSessions { get; set; }
    }

    public class LoginAttempt
    {
        public DateTime AttemptedAt { get; set; }
        public bool Success { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Location { get; set; }
        public string FailureReason { get; set; }
    }

    public enum AuthenticationErrorCode
    {
        InvalidCredentials = 1,
        AccountLocked = 2,
        AccountDisabled = 3,
        MfaRequired = 4,
        InvalidMfaCode = 5,
        TokenExpired = 6,
        TokenInvalid = 7,
        RateLimitExceeded = 8,
        PasswordExpired = 9,
        EmailNotVerified = 10
    }
}