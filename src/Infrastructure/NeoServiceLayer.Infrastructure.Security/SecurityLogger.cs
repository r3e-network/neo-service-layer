using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Implementation of security logging for the Neo Service Layer.
/// </summary>
public class SecurityLogger : ISecurityLogger
{
    private readonly ILogger<SecurityLogger> _logger;
    private readonly ConcurrentDictionary<string, SecurityEvent> _recentEvents;
    private readonly ConcurrentDictionary<string, DateTime> _clientLastActivity;
    private readonly object _cleanupLock = new();
    private DateTime _lastCleanup = DateTime.UtcNow;

    public SecurityLogger(ILogger<SecurityLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recentEvents = new ConcurrentDictionary<string, SecurityEvent>();
        _clientLastActivity = new ConcurrentDictionary<string, DateTime>();
    }

    public void RecordSecurityEvent(SecurityEventType eventType, string message, SecurityEventMetadata? metadata = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = eventType,
            Message = message,
            Metadata = metadata,
            Severity = DetermineSeverity(eventType)
        };

        _recentEvents.TryAdd(securityEvent.Id, securityEvent);

        // Log to underlying logger
        var logLevel = securityEvent.Severity;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["SecurityEventId"] = securityEvent.Id,
            ["SecurityEventType"] = eventType.ToString(),
            ["ClientId"] = metadata?.ClientId ?? "Unknown",
            ["IpAddress"] = metadata?.IpAddress ?? "Unknown",
            ["SessionId"] = metadata?.SessionId ?? "Unknown"
        }))
        {
            _logger.Log(logLevel, "[SECURITY] {EventType}: {Message}", eventType, message);
        }

        // Update client activity tracking
        if (metadata?.ClientId != null)
        {
            _clientLastActivity.AddOrUpdate(metadata.ClientId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
        }

        // Periodic cleanup
        PerformPeriodicCleanup();
    }

    public void RecordAuthenticationAttempt(string userId, bool success, string? reason = null, SecurityEventMetadata? metadata = null)
    {
        var message = success
            ? $"Successful authentication for user {userId}"
            : $"Failed authentication for user {userId}" + (reason != null ? $": {reason}" : "");

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["UserId"] = userId;
        enhancedMetadata.AdditionalData["Success"] = success;
        if (reason != null) enhancedMetadata.AdditionalData["Reason"] = reason;

        RecordSecurityEvent(SecurityEventType.Authentication, message, enhancedMetadata);
    }

    public void RecordAuthorizationFailure(string userId, string resource, string action, SecurityEventMetadata? metadata = null)
    {
        var message = $"Authorization failed for user {userId} attempting {action} on {resource}";

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["UserId"] = userId;
        enhancedMetadata.AdditionalData["Resource"] = resource;
        enhancedMetadata.AdditionalData["Action"] = action;

        RecordSecurityEvent(SecurityEventType.Authorization, message, enhancedMetadata);
    }

    public void RecordRateLimitViolation(string clientId, string operation, int attemptCount, SecurityEventMetadata? metadata = null)
    {
        var message = $"Rate limit exceeded for client {clientId} on operation {operation} (attempts: {attemptCount})";

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.ClientId = clientId;
        enhancedMetadata.OperationName = operation;
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["AttemptCount"] = attemptCount;

        RecordSecurityEvent(SecurityEventType.RateLimit, message, enhancedMetadata);
    }

    public void RecordSuspiciousActivity(string description, SuspiciousActivityType activityType, SecurityEventMetadata? metadata = null)
    {
        var message = $"Suspicious activity detected ({activityType}): {description}";

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["ActivityType"] = activityType.ToString();

        RecordSecurityEvent(SecurityEventType.SuspiciousActivity, message, enhancedMetadata);
    }

    public void RecordCryptographicOperation(CryptoOperationType operationType, bool success, string? algorithm = null, SecurityEventMetadata? metadata = null)
    {
        var message = $"Cryptographic operation {operationType}" +
                     (algorithm != null ? $" using {algorithm}" : "") +
                     (success ? " completed successfully" : " failed");

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["OperationType"] = operationType.ToString();
        enhancedMetadata.AdditionalData["Success"] = success;
        if (algorithm != null) enhancedMetadata.AdditionalData["Algorithm"] = algorithm;

        RecordSecurityEvent(SecurityEventType.CryptographicOperation, message, enhancedMetadata);
    }

    public void RecordEnclaveOperation(string operation, bool success, string? attestationStatus = null, SecurityEventMetadata? metadata = null)
    {
        var message = $"Enclave operation '{operation}'" +
                     (success ? " completed successfully" : " failed") +
                     (attestationStatus != null ? $" (attestation: {attestationStatus})" : "");

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["Operation"] = operation;
        enhancedMetadata.AdditionalData["Success"] = success;
        if (attestationStatus != null) enhancedMetadata.AdditionalData["AttestationStatus"] = attestationStatus;

        RecordSecurityEvent(SecurityEventType.EnclaveOperation, message, enhancedMetadata);
    }

    public void RecordDataAccess(string userId, string resource, DataAccessType accessType, bool success, SecurityEventMetadata? metadata = null)
    {
        var message = $"Data access {accessType} on {resource} by user {userId}" +
                     (success ? " succeeded" : " failed");

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["UserId"] = userId;
        enhancedMetadata.AdditionalData["Resource"] = resource;
        enhancedMetadata.AdditionalData["AccessType"] = accessType.ToString();
        enhancedMetadata.AdditionalData["Success"] = success;

        RecordSecurityEvent(SecurityEventType.DataAccess, message, enhancedMetadata);
    }

    public void RecordConfigurationChange(string userId, string configKey, string? oldValue, string? newValue, SecurityEventMetadata? metadata = null)
    {
        var message = $"Configuration '{configKey}' changed by user {userId}";

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["UserId"] = userId;
        enhancedMetadata.AdditionalData["ConfigKey"] = configKey;
        enhancedMetadata.AdditionalData["OldValue"] = oldValue ?? "null";
        enhancedMetadata.AdditionalData["NewValue"] = newValue ?? "null";

        RecordSecurityEvent(SecurityEventType.ConfigurationChange, message, enhancedMetadata);
    }

    public void RecordValidationFailure(string validationType, string details, SecurityEventMetadata? metadata = null)
    {
        var message = $"Validation failure ({validationType}): {details}";

        var enhancedMetadata = EnhanceMetadata(metadata);
        enhancedMetadata.AdditionalData ??= new Dictionary<string, object>();
        enhancedMetadata.AdditionalData["ValidationType"] = validationType;

        RecordSecurityEvent(SecurityEventType.ValidationFailure, message, enhancedMetadata);
    }

    public SecurityEventStatistics GetStatistics(DateTime startTime, DateTime endTime)
    {
        var stats = new SecurityEventStatistics
        {
            StartTime = startTime,
            EndTime = endTime
        };

        var relevantEvents = _recentEvents.Values
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .ToList();

        stats.TotalEvents = relevantEvents.Count;

        // Count by event type
        foreach (var eventGroup in relevantEvents.GroupBy(e => e.EventType))
        {
            stats.EventCounts[eventGroup.Key] = eventGroup.Count();
        }

        // Count specific event types
        stats.SuccessfulAuthentications = relevantEvents.Count(e =>
            e.EventType == SecurityEventType.Authentication &&
            e.Metadata?.AdditionalData?.ContainsKey("Success") == true &&
            (bool)(e.Metadata.AdditionalData["Success"] ?? false));

        stats.FailedAuthentications = relevantEvents.Count(e =>
            e.EventType == SecurityEventType.Authentication &&
            e.Metadata?.AdditionalData?.ContainsKey("Success") == true &&
            !(bool)(e.Metadata.AdditionalData["Success"] ?? true));

        stats.AuthorizationFailures = relevantEvents.Count(e => e.EventType == SecurityEventType.Authorization);
        stats.RateLimitViolations = relevantEvents.Count(e => e.EventType == SecurityEventType.RateLimit);
        stats.SuspiciousActivities = relevantEvents.Count(e => e.EventType == SecurityEventType.SuspiciousActivity);

        // Top clients
        var clientGroups = relevantEvents
            .Where(e => !string.IsNullOrEmpty(e.Metadata?.ClientId))
            .GroupBy(e => e.Metadata!.ClientId!)
            .OrderByDescending(g => g.Count())
            .Take(10);

        foreach (var group in clientGroups)
        {
            stats.TopClientIds[group.Key] = group.Count();
        }

        // Top IP addresses
        var ipGroups = relevantEvents
            .Where(e => !string.IsNullOrEmpty(e.Metadata?.IpAddress))
            .GroupBy(e => e.Metadata!.IpAddress!)
            .OrderByDescending(g => g.Count())
            .Take(10);

        foreach (var group in ipGroups)
        {
            stats.TopIpAddresses[group.Key] = group.Count();
        }

        return stats;
    }

    public IEnumerable<SecurityEvent> GetRecentEvents(int count = 100, SecurityEventType? filterByType = null)
    {
        var query = _recentEvents.Values.AsEnumerable();

        if (filterByType.HasValue)
        {
            query = query.Where(e => e.EventType == filterByType.Value);
        }

        return query
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    public async Task CleanupOldEventsAsync(int retentionDays)
    {
        await Task.Run(() =>
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var eventsToRemove = _recentEvents
                .Where(kvp => kvp.Value.Timestamp < cutoffDate)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var eventId in eventsToRemove)
            {
                _recentEvents.TryRemove(eventId, out _);
            }

            // Cleanup inactive clients
            var inactiveClients = _clientLastActivity
                .Where(kvp => kvp.Value < cutoffDate)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var clientId in inactiveClients)
            {
                _clientLastActivity.TryRemove(clientId, out _);
            }

            _logger.LogInformation("Cleaned up {EventCount} old security events and {ClientCount} inactive clients",
                eventsToRemove.Count, inactiveClients.Count);
        });
    }

    private SecurityEventMetadata EnhanceMetadata(SecurityEventMetadata? metadata)
    {
        if (metadata == null)
        {
            metadata = new SecurityEventMetadata();
        }

        // Add correlation ID if not present
        if (string.IsNullOrEmpty(metadata.CorrelationId))
        {
            metadata.CorrelationId = Guid.NewGuid().ToString();
        }

        return metadata;
    }

    private LogLevel DetermineSeverity(SecurityEventType eventType)
    {
        return eventType switch
        {
            SecurityEventType.Authentication => LogLevel.Information,
            SecurityEventType.Authorization => LogLevel.Warning,
            SecurityEventType.RateLimit => LogLevel.Warning,
            SecurityEventType.SuspiciousActivity => LogLevel.Warning,
            SecurityEventType.CryptographicOperation => LogLevel.Information,
            SecurityEventType.EnclaveOperation => LogLevel.Information,
            SecurityEventType.DataAccess => LogLevel.Information,
            SecurityEventType.ConfigurationChange => LogLevel.Warning,
            SecurityEventType.ValidationFailure => LogLevel.Warning,
            SecurityEventType.SecurityViolation => LogLevel.Error,
            SecurityEventType.AuditTrail => LogLevel.Information,
            _ => LogLevel.Information
        };
    }

    private void PerformPeriodicCleanup()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup > TimeSpan.FromHours(1))
        {
            lock (_cleanupLock)
            {
                if (now - _lastCleanup > TimeSpan.FromHours(1))
                {
                    _lastCleanup = now;
                    // Fire and forget with proper error handling
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await CleanupOldEventsAsync(7); // Keep events for 7 days by default
                        }
                        catch (Exception ex)
                        {
                            // Log the error but don't fail the main operation
                            _logger.LogError(ex, "Error during periodic cleanup of security events");
                        }
                    });
                }
            }
        }
    }
}
