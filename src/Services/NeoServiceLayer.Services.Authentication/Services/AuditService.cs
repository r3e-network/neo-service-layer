using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication.Models;
using NeoServiceLayer.Services.Authentication.Repositories;
using NeoServiceLayer.Services.Authentication.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Services
{
    /// <summary>
    /// Comprehensive audit service for security event logging
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly IAuditLogRepository _repository;

        public AuditService(
            ILogger<AuditService> logger,
            IAuditLogRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task LogAuthenticationAttemptAsync(
            string username,
            string ipAddress,
            bool success,
            string reason)
        {
            try
            {
                await _repository.LogAuthenticationAttemptAsync(username, ipAddress, success, reason);

                if (!success)
                {
                    _logger.LogWarning(
                        "Failed authentication attempt for {Username} from {IpAddress}: {Reason}",
                        username, ipAddress, reason);
                }
                else
                {
                    _logger.LogInformation(
                        "Successful authentication for {Username} from {IpAddress}",
                        username, ipAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging authentication attempt");
            }
        }

        public async Task LogLogoutAsync(Guid userId, string sessionId)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = "Logout",
                    EventCategory = "Authentication",
                    Description = $"User logged out from session {sessionId}",
                    Success = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);
                _logger.LogInformation("User {UserId} logged out from session {SessionId}", userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging logout event");
            }
        }

        public async Task LogPasswordChangeAsync(Guid userId)
        {
            try
            {
                await _repository.LogPasswordChangeAsync(userId, null);
                _logger.LogInformation("Password changed for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging password change");
            }
        }

        public async Task LogSecurityEventAsync(
            string eventType,
            Guid? userId,
            string details)
        {
            try
            {
                await _repository.LogSecurityEventAsync(eventType, userId, details, null);
                _logger.LogWarning("Security event: {EventType} for user {UserId}: {Details}",
                    eventType, userId, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event");
            }
        }

        public async Task LogTwoFactorEventAsync(
            Guid userId,
            string eventType,
            bool success)
        {
            try
            {
                await _repository.LogTwoFactorEventAsync(userId, eventType, success);

                if (!success)
                {
                    _logger.LogWarning("Failed 2FA event {EventType} for user {UserId}", eventType, userId);
                }
                else
                {
                    _logger.LogInformation("Successful 2FA event {EventType} for user {UserId}", eventType, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging 2FA event");
            }
        }

        public async Task LogSessionEventAsync(
            Guid userId,
            string sessionId,
            string eventType,
            string ipAddress = null)
        {
            try
            {
                await _repository.LogSessionEventAsync(userId, sessionId, eventType);
                _logger.LogInformation("Session event {EventType} for user {UserId}, session {SessionId}",
                    eventType, userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging session event");
            }
        }

        public async Task LogPermissionChangeAsync(
            Guid userId,
            string permission,
            bool granted,
            string changedBy)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = granted ? "PermissionGranted" : "PermissionRevoked",
                    EventCategory = "Authorization",
                    Description = $"Permission '{permission}' {(granted ? "granted to" : "revoked from")} user",
                    Success = true,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        Permission = permission,
                        Granted = granted,
                        ChangedBy = changedBy
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);
                _logger.LogInformation(
                    "Permission {Permission} {Action} user {UserId} by {ChangedBy}",
                    permission, granted ? "granted to" : "revoked from", userId, changedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging permission change");
            }
        }

        public async Task LogRoleChangeAsync(
            Guid userId,
            string role,
            bool assigned,
            string changedBy)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = assigned ? "RoleAssigned" : "RoleRemoved",
                    EventCategory = "Authorization",
                    Description = $"Role '{role}' {(assigned ? "assigned to" : "removed from")} user",
                    Success = true,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        Role = role,
                        Assigned = assigned,
                        ChangedBy = changedBy
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);
                _logger.LogInformation(
                    "Role {Role} {Action} user {UserId} by {ChangedBy}",
                    role, assigned ? "assigned to" : "removed from", userId, changedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging role change");
            }
        }

        public async Task LogAccountLockoutAsync(
            Guid userId,
            string reason,
            DateTime lockoutEnd)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = "AccountLocked",
                    EventCategory = "Security",
                    Description = $"Account locked: {reason}",
                    Success = true,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        Reason = reason,
                        LockoutEnd = lockoutEnd
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);
                _logger.LogWarning(
                    "Account {UserId} locked until {LockoutEnd}: {Reason}",
                    userId, lockoutEnd, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging account lockout");
            }
        }

        public async Task LogDataAccessAsync(
            Guid userId,
            string resource,
            string action,
            bool authorized)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = "DataAccess",
                    EventCategory = "Authorization",
                    Description = $"User accessed {resource} for {action}",
                    Success = authorized,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        Resource = resource,
                        Action = action,
                        Authorized = authorized
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);

                if (!authorized)
                {
                    _logger.LogWarning(
                        "Unauthorized data access attempt by user {UserId} for {Resource}/{Action}",
                        userId, resource, action);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging data access");
            }
        }

        public async Task LogApiAccessAsync(
            string endpoint,
            string method,
            Guid? userId,
            string ipAddress,
            int statusCode,
            long responseTimeMs)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = "ApiAccess",
                    EventCategory = "API",
                    Description = $"{method} {endpoint}",
                    IpAddress = ipAddress,
                    Success = statusCode >= 200 && statusCode < 400,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        Endpoint = endpoint,
                        Method = method,
                        StatusCode = statusCode,
                        ResponseTimeMs = responseTimeMs
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);

                if (responseTimeMs > 1000)
                {
                    _logger.LogWarning(
                        "Slow API response: {Method} {Endpoint} took {ResponseTime}ms",
                        method, endpoint, responseTimeMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging API access");
            }
        }

        public async Task LogTokenEventAsync(
            Guid userId,
            string eventType,
            string tokenId,
            string details = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = $"Token{eventType}",
                    EventCategory = "Token",
                    Description = details ?? $"Token {eventType.ToLower()} for user",
                    Success = true,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        TokenId = tokenId,
                        EventType = eventType
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);
                _logger.LogInformation(
                    "Token event {EventType} for user {UserId}, token {TokenId}",
                    eventType, userId, tokenId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging token event");
            }
        }

        public async Task LogEmailEventAsync(
            Guid? userId,
            string emailType,
            string recipient,
            bool success,
            string error = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = $"Email{emailType}",
                    EventCategory = "Email",
                    Description = $"Email '{emailType}' sent to {recipient}",
                    Success = success,
                    FailureReason = error,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        EmailType = emailType,
                        Recipient = recipient,
                        Success = success,
                        Error = error
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.LogAsync(auditLog);

                if (!success)
                {
                    _logger.LogError(
                        "Failed to send {EmailType} email to {Recipient}: {Error}",
                        emailType, recipient, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging email event");
            }
        }
    }

    /// <summary>
    /// Extended audit service interface
    /// </summary>
    public interface IAuditServiceExtended : IAuditService
    {
        Task LogTwoFactorEventAsync(Guid userId, string eventType, bool success);
        Task LogSessionEventAsync(Guid userId, string sessionId, string eventType, string ipAddress = null);
        Task LogPermissionChangeAsync(Guid userId, string permission, bool granted, string changedBy);
        Task LogRoleChangeAsync(Guid userId, string role, bool assigned, string changedBy);
        Task LogAccountLockoutAsync(Guid userId, string reason, DateTime lockoutEnd);
        Task LogDataAccessAsync(Guid userId, string resource, string action, bool authorized);
        Task LogApiAccessAsync(string endpoint, string method, Guid? userId, string ipAddress, int statusCode, long responseTimeMs);
        Task LogTokenEventAsync(Guid userId, string eventType, string tokenId, string details = null);
        Task LogEmailEventAsync(Guid? userId, string emailType, string recipient, bool success, string error = null);
    }
}