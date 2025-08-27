using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Auth.Microservice.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public required string PasswordHash { get; set; }
    public required string Salt { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LockReason { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool MfaEnabled { get; set; }
    public MfaType MfaType { get; set; } = MfaType.None;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public DateTime LastPasswordChange { get; set; } = DateTime.UtcNow;
    public bool RequiresPasswordChange { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Guid TenantId { get; set; }
    public string Metadata { get; set; } = "{}";
    public string Preferences { get; set; } = "{}";
}

public class UserSession
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string SessionToken { get; set; }
    public string? DeviceId { get; set; }
    public required string IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RefreshToken
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Token { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class LoginAttempt
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public required string IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string? Username { get; set; }
}

public class MfaSecret
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Secret { get; set; }
    public MfaType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BackupCode
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Code { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public required string Action { get; set; }
    public string? Resource { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = "{}";
}

public enum MfaType
{
    None = 0,
    Totp = 1,
    Sms = 2,
    Email = 3
}

// Request/Response DTOs
public class LoginRequest
{
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string Password { get; set; }
    
    public string? MfaCode { get; set; }
    public string? DeviceId { get; set; }
    public bool RememberMe { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool RequiresMfa { get; set; }
    public string? MfaToken { get; set; }
    public string? ErrorMessage { get; set; }
    public AuthenticationErrorCode? ErrorCode { get; set; }
}

public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public required string Username { get; set; }
    
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public required string Password { get; set; }
    
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public bool AcceptTerms { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? Message { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public string? VerificationToken { get; set; }
    public string[]? Errors { get; set; }
}

public class RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}

public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime AccessTokenExpiry { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; set; }
    
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public required string NewPassword { get; set; }
}

public class MfaSetupRequest
{
    [Required]
    public MfaType Type { get; set; }
    
    public string? PhoneNumber { get; set; }
}

public class MfaSetupResponse
{
    public bool Success { get; set; }
    public string? Secret { get; set; }
    public string? QrCodeUrl { get; set; }
    public string[]? BackupCodes { get; set; }
    public MfaType Type { get; set; }
    public string? Message { get; set; }
}

public class VerifyMfaRequest
{
    [Required]
    public required string Code { get; set; }
    
    public bool IsBackupCode { get; set; }
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