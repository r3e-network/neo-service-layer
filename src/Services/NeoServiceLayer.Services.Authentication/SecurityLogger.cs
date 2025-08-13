using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Observability.Logging;

namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// Security event logger for audit trails and compliance
    /// </summary>
    public interface ISecurityLogger
    {
        Task LogSecurityEventAsync(string eventType, string userId, Dictionary<string, object> metadata = null);
        Task LogAuthenticationEventAsync(string userId, bool success, string method, string ipAddress);
        Task LogAuthorizationEventAsync(string userId, string resource, string action, bool granted);
        Task LogPasswordChangeAsync(string userId, string ipAddress);
        Task LogAccountLockoutAsync(string userId, string reason);
        Task LogSuspiciousActivityAsync(string userId, string activity, string details);
    }

    public class SecurityLogger : ISecurityLogger
    {
        private readonly ILogger<SecurityLogger> _logger;
        private readonly IStructuredLogger _structuredLogger;

        public SecurityLogger(
            ILogger<SecurityLogger> logger,
            IStructuredLoggerFactory structuredLoggerFactory)
        {
            _logger = logger;
            _structuredLogger = structuredLoggerFactory?.CreateLogger("Security");
        }

        public async Task LogSecurityEventAsync(string eventType, string userId, Dictionary<string, object> metadata = null)
        {
            var properties = new Dictionary<string, object>
            {
                ["EventType"] = eventType,
                ["UserId"] = userId,
                ["Timestamp"] = DateTime.UtcNow,
                ["EventId"] = Guid.NewGuid().ToString()
            };

            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    properties[item.Key] = item.Value;
                }
            }

            _structuredLogger?.LogOperation($"SecurityEvent:{eventType}", properties);
            
            _logger.LogInformation(
                "Security Event: {EventType} for User: {UserId} at {Timestamp}",
                eventType, userId, DateTime.UtcNow);

            await Task.CompletedTask;
        }

        public async Task LogAuthenticationEventAsync(string userId, bool success, string method, string ipAddress)
        {
            var eventType = success ? "AuthenticationSuccess" : "AuthenticationFailure";
            
            await LogSecurityEventAsync(eventType, userId, new Dictionary<string, object>
            {
                ["Method"] = method,
                ["IpAddress"] = ipAddress,
                ["Success"] = success
            });

            if (!success)
            {
                _logger.LogWarning(
                    "Authentication failed for User: {UserId} from IP: {IpAddress} using method: {Method}",
                    userId, ipAddress, method);
            }
        }

        public async Task LogAuthorizationEventAsync(string userId, string resource, string action, bool granted)
        {
            var eventType = granted ? "AuthorizationGranted" : "AuthorizationDenied";
            
            await LogSecurityEventAsync(eventType, userId, new Dictionary<string, object>
            {
                ["Resource"] = resource,
                ["Action"] = action,
                ["Granted"] = granted
            });

            if (!granted)
            {
                _logger.LogWarning(
                    "Authorization denied for User: {UserId} attempting {Action} on {Resource}",
                    userId, action, resource);
            }
        }

        public async Task LogPasswordChangeAsync(string userId, string ipAddress)
        {
            await LogSecurityEventAsync("PasswordChanged", userId, new Dictionary<string, object>
            {
                ["IpAddress"] = ipAddress,
                ["ChangeType"] = "UserInitiated"
            });

            _logger.LogInformation(
                "Password changed for User: {UserId} from IP: {IpAddress}",
                userId, ipAddress);
        }

        public async Task LogAccountLockoutAsync(string userId, string reason)
        {
            await LogSecurityEventAsync("AccountLocked", userId, new Dictionary<string, object>
            {
                ["Reason"] = reason,
                ["LockedAt"] = DateTime.UtcNow
            });

            _logger.LogWarning(
                "Account locked for User: {UserId}. Reason: {Reason}",
                userId, reason);
        }

        public async Task LogSuspiciousActivityAsync(string userId, string activity, string details)
        {
            await LogSecurityEventAsync("SuspiciousActivity", userId, new Dictionary<string, object>
            {
                ["Activity"] = activity,
                ["Details"] = details,
                ["Severity"] = "High"
            });

            _logger.LogWarning(
                "Suspicious activity detected for User: {UserId}. Activity: {Activity}. Details: {Details}",
                userId, activity, details);
        }
    }
}