using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Models
{
    /// <summary>
    /// User entity for authentication system
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string PasswordSalt { get; set; }

        public bool EmailVerified { get; set; }
        public string EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiry { get; set; }

        public bool TwoFactorEnabled { get; set; }
        public bool MfaEnabled { get; set; }
        public string TwoFactorSecret { get; set; }
        public string MfaSecret { get; set; }
        public string MfaType { get; set; }
        public List<BackupCode> BackupCodes { get; set; }

        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        
        public DateTime? LastPasswordChangeAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string LastLoginIp { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
        
        // Additional compatibility properties
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneVerified { get; set; }
        public string? LockReason { get; set; }
        public DateTime? LockedUntil => LockoutEnd;
        public bool RequiresPasswordChange { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Computed properties
        public string[] Roles => UserRoles?.Select(ur => ur.Role?.Name).Where(name => !string.IsNullOrEmpty(name)).ToArray() ?? Array.Empty<string>();

        // Navigation properties
        public List<UserRole> UserRoles { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public List<UserSession> Sessions { get; set; }
        public List<AuditLog> AuditLogs { get; set; }
        public List<UserPermission> Permissions { get; set; }
    }

    /// <summary>
    /// Role entity for RBAC
    /// </summary>
    public class Role
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        public bool IsSystemRole { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public List<UserRole> UserRoles { get; set; }
        public List<RolePermission> RolePermissions { get; set; }
    }

    /// <summary>
    /// User-Role mapping
    /// </summary>
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string AssignedBy { get; set; }
    }

    /// <summary>
    /// Permission entity
    /// </summary>
    public class Permission
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string Resource { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; }

        // Navigation properties
        public List<RolePermission> RolePermissions { get; set; }
        public List<UserPermission> UserPermissions { get; set; }
    }

    /// <summary>
    /// Role-Permission mapping
    /// </summary>
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; }

        public DateTime GrantedAt { get; set; }
    }

    /// <summary>
    /// Direct user permissions (overrides role permissions)
    /// </summary>
    public class UserPermission
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; }

        public bool IsGranted { get; set; } // Can be used to explicitly deny
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Refresh token entity
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public string JwtId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string RevokedBy { get; set; }
        public string RevokedReason { get; set; }
        public string ReplacedByToken { get; set; }

        public string CreatedByIp { get; set; }
        public string RevokedByIp { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }

    /// <summary>
    /// User session entity
    /// </summary>
    public class UserSession
    {
        public Guid Id { get; set; }

        [Required]
        public string SessionId { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string IpAddress { get; set; }

        public string UserAgent { get; set; }
        public string DeviceInfo { get; set; }
        public string Location { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime? TerminatedAt { get; set; }

        public string TerminationReason { get; set; }

        public bool IsActive => TerminatedAt == null &&
                               (ExpiredAt == null || DateTime.UtcNow < ExpiredAt);
    }

    /// <summary>
    /// Backup code for 2FA
    /// </summary>
    public class BackupCode
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string Code { get; set; }

        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Blacklisted token entity
    /// </summary>
    public class BlacklistedToken
    {
        public Guid Id { get; set; }

        [Required]
        public string TokenHash { get; set; }

        public string JwtId { get; set; }
        public Guid? UserId { get; set; }

        public DateTime BlacklistedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Reason { get; set; }
        public string BlacklistedBy { get; set; }
    }

    /// <summary>
    /// Audit log entity
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }
        public User User { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventCategory { get; set; }

        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

        public bool Success { get; set; }
        public string FailureReason { get; set; }

        public string OldValues { get; set; } // JSON
        public string NewValues { get; set; } // JSON
        public string Metadata { get; set; } // JSON

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Rate limit tracking
    /// </summary>
    public class RateLimitEntry
    {
        public Guid Id { get; set; }

        [Required]
        public string Key { get; set; } // IP address or user ID

        [Required]
        public string Action { get; set; } // login, register, etc.

        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }

        public bool IsBlocked { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }

    /// <summary>
    /// Email template for notifications
    /// </summary>
    public class EmailTemplate
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string HtmlBody { get; set; }

        public string TextBody { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Password history to prevent reuse
    /// </summary>
    public class PasswordHistory
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Login attempt tracking
    /// </summary>
    public class LoginAttempt
    {
        public Guid Id { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public Guid? UserId { get; set; }

        [Required]
        public string IpAddress { get; set; }

        public string UserAgent { get; set; }
        public bool Success { get; set; }
        public string FailureReason { get; set; }

        public DateTime AttemptedAt { get; set; }
    }
}