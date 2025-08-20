using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Services.Authentication.Models;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Repositories
{
    /// <summary>
    /// User repository interface
    /// </summary>
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid userId);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByPasswordResetTokenAsync(string token);
        Task<User> GetByEmailVerificationTokenAsync(string token);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid userId);
        Task<bool> ExistsAsync(string username, string email);
        Task<List<User>> GetAllAsync(int page, int pageSize);
        Task<int> GetCountAsync();

        // Password management
        Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash);
        Task<List<PasswordHistory>> GetPasswordHistoryAsync(Guid userId, int count);
        Task AddPasswordHistoryAsync(Guid userId, string passwordHash);

        // Failed attempts and lockout
        Task<int> GetFailedAttemptsAsync(Guid userId);
        Task IncrementFailedAttemptsAsync(Guid userId);
        Task ResetFailedAttemptsAsync(Guid userId);
        Task SetLockoutEndAsync(Guid userId, DateTime? lockoutEnd);

        // 2FA management
        Task EnableTwoFactorAsync(Guid userId, string secret);
        Task DisableTwoFactorAsync(Guid userId);
        Task<List<BackupCode>> GetBackupCodesAsync(Guid userId);
        Task<bool> UseBackupCodeAsync(Guid userId, string code);
        Task RegenerateBackupCodesAsync(Guid userId, List<string> codes);
    }

    /// <summary>
    /// Role repository interface
    /// </summary>
    public interface IRoleRepository
    {
        Task<Role> GetByIdAsync(Guid roleId);
        Task<Role> GetByNameAsync(string roleName);
        Task<List<Role>> GetAllAsync();
        Task<Role> CreateAsync(Role role);
        Task<Role> UpdateAsync(Role role);
        Task<bool> DeleteAsync(Guid roleId);

        // User-Role management
        Task<List<Role>> GetUserRolesAsync(Guid userId);
        Task AssignRoleToUserAsync(Guid userId, Guid roleId, string assignedBy);
        Task RemoveRoleFromUserAsync(Guid userId, Guid roleId);
        Task<bool> UserHasRoleAsync(Guid userId, string roleName);
    }

    /// <summary>
    /// Permission repository interface
    /// </summary>
    public interface IPermissionRepository
    {
        Task<Permission> GetByIdAsync(Guid permissionId);
        Task<Permission> GetByNameAsync(string permissionName);
        Task<List<Permission>> GetAllAsync();
        Task<Permission> CreateAsync(Permission permission);
        Task<Permission> UpdateAsync(Permission permission);
        Task<bool> DeleteAsync(Guid permissionId);

        // Role-Permission management
        Task<List<Permission>> GetRolePermissionsAsync(Guid roleId);
        Task AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
        Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);

        // User-Permission management
        Task<List<Permission>> GetUserPermissionsAsync(Guid userId);
        Task<List<Permission>> GetEffectiveUserPermissionsAsync(Guid userId);
        Task AssignPermissionToUserAsync(Guid userId, Guid permissionId, bool isGranted);
        Task RemovePermissionFromUserAsync(Guid userId, Guid permissionId);
        Task<bool> UserHasPermissionAsync(Guid userId, string permissionName);
    }

    /// <summary>
    /// Refresh token repository interface
    /// </summary>
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task<RefreshToken> GetByJwtIdAsync(string jwtId);
        Task<List<RefreshToken>> GetUserActiveTokensAsync(Guid userId);
        Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
        Task RevokeTokenAsync(string token, string revokedBy, string reason);
        Task RevokeUserTokensAsync(Guid userId, string revokedBy, string reason);
        Task RevokeTokenFamilyAsync(string jwtId, string revokedBy, string reason);
        Task<bool> IsTokenRevokedAsync(string token);
        Task CleanupExpiredTokensAsync();
    }

    /// <summary>
    /// Session repository interface
    /// </summary>
    public interface ISessionRepository
    {
        Task<UserSession> GetByIdAsync(Guid sessionId);
        Task<UserSession> GetBySessionIdAsync(string sessionId);
        Task<List<UserSession>> GetUserActiveSessionsAsync(Guid userId);
        Task<UserSession> CreateAsync(UserSession session);
        Task UpdateLastActivityAsync(string sessionId);
        Task TerminateSessionAsync(string sessionId, string reason);
        Task TerminateUserSessionsAsync(Guid userId, string reason);
        Task CleanupExpiredSessionsAsync();
        Task<int> GetActiveSessionCountAsync(Guid userId);
    }

    /// <summary>
    /// Token blacklist repository interface
    /// </summary>
    public interface ITokenBlacklistRepository
    {
        Task<bool> IsBlacklistedAsync(string tokenHash);
        Task BlacklistTokenAsync(string tokenHash, string jwtId, Guid? userId, DateTime expiresAt, string reason);
        Task BlacklistUserTokensAsync(Guid userId, string reason);
        Task CleanupExpiredBlacklistEntriesAsync();
        Task<List<BlacklistedToken>> GetBlacklistedTokensAsync(int page, int pageSize);
    }

    /// <summary>
    /// Audit log repository interface
    /// </summary>
    public interface IAuditLogRepository
    {
        Task LogAsync(AuditLog auditLog);
        Task LogAuthenticationAttemptAsync(string username, string ipAddress, bool success, string reason);
        Task LogPasswordChangeAsync(Guid userId, string ipAddress);
        Task LogTwoFactorEventAsync(Guid userId, string eventType, bool success);
        Task LogSessionEventAsync(Guid userId, string sessionId, string eventType);
        Task LogSecurityEventAsync(string eventType, Guid? userId, string details, string ipAddress);
        Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int page, int pageSize);
        Task<List<AuditLog>> GetAuditLogsByTypeAsync(string eventType, DateTime from, DateTime to);
        Task<List<AuditLog>> GetFailedLoginAttemptsAsync(string ipAddress, DateTime from);
    }

    /// <summary>
    /// Rate limit repository interface
    /// </summary>
    public interface IRateLimitRepository
    {
        Task<RateLimitEntry> GetAsync(string key, string action);
        Task<bool> IsBlockedAsync(string key, string action);
        Task IncrementAsync(string key, string action, TimeSpan window);
        Task BlockAsync(string key, string action, TimeSpan duration);
        Task ResetAsync(string key, string action);
        Task CleanupExpiredEntriesAsync();
    }

    /// <summary>
    /// Login attempt repository interface
    /// </summary>
    public interface ILoginAttemptRepository
    {
        Task RecordAttemptAsync(LoginAttempt attempt);
        Task<List<LoginAttempt>> GetRecentAttemptsAsync(string ipAddress, TimeSpan window);
        Task<List<LoginAttempt>> GetUserRecentAttemptsAsync(Guid userId, TimeSpan window);
        Task<int> GetFailedAttemptCountAsync(string ipAddress, TimeSpan window);
        Task<int> GetUserFailedAttemptCountAsync(Guid userId, TimeSpan window);
        Task CleanupOldAttemptsAsync(TimeSpan retention);
    }
}