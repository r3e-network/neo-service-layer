using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Security;
using NeoServiceLayer.ServiceFramework;
using System.Runtime.CompilerServices;
using System.Security;

namespace NeoServiceLayer.ServiceFramework.Security;

/// <summary>
/// Base class for services that require security logging capabilities.
/// </summary>
public abstract class SecurityAwareServiceBase : ServiceBase
{
    protected readonly ISecurityLogger SecurityLogger;
    private readonly string _serviceName;

    protected SecurityAwareServiceBase(ILogger logger, ISecurityLogger securityLogger) 
        : base("SecurityAware", "1.0", "Security-aware service base", logger)
    {
        SecurityLogger = securityLogger ?? throw new ArgumentNullException(nameof(securityLogger));
        _serviceName = GetType().Name;
    }

    /// <summary>
    /// Logs a security event with service context.
    /// </summary>
    protected void LogSecurityEvent(SecurityEventType eventType, string message, 
        string? clientId = null, Dictionary<string, object>? additionalData = null,
        [CallerMemberName] string? operationName = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            OperationName = operationName,
            ClientId = clientId,
            AdditionalData = additionalData
        };

        SecurityLogger.RecordSecurityEvent(eventType, message, metadata);
    }

    /// <summary>
    /// Logs an authentication attempt.
    /// </summary>
    protected void LogAuthenticationAttempt(string userId, bool success, string? reason = null,
        string? clientId = null, string? ipAddress = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            ClientId = clientId,
            IpAddress = ipAddress
        };

        SecurityLogger.RecordAuthenticationAttempt(userId, success, reason, metadata);
    }

    /// <summary>
    /// Logs an authorization check.
    /// </summary>
    protected void LogAuthorizationCheck(string userId, string resource, string action, bool allowed,
        string? clientId = null)
    {
        if (!allowed)
        {
            var metadata = new SecurityEventMetadata
            {
                ServiceName = _serviceName,
                ClientId = clientId
            };

            SecurityLogger.RecordAuthorizationFailure(userId, resource, action, metadata);
        }
        else
        {
            LogSecurityEvent(SecurityEventType.Authorization, 
                $"Authorization granted for user {userId} to {action} on {resource}", clientId);
        }
    }

    /// <summary>
    /// Logs a rate limit check.
    /// </summary>
    protected void LogRateLimitCheck(string clientId, string operation, bool allowed, int attemptCount)
    {
        if (!allowed)
        {
            var metadata = new SecurityEventMetadata
            {
                ServiceName = _serviceName
            };

            SecurityLogger.RecordRateLimitViolation(clientId, operation, attemptCount, metadata);
        }
    }

    /// <summary>
    /// Logs suspicious activity detection.
    /// </summary>
    protected void LogSuspiciousActivity(string description, SuspiciousActivityType activityType,
        string? clientId = null, string? ipAddress = null, Dictionary<string, object>? evidence = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            ClientId = clientId,
            IpAddress = ipAddress,
            AdditionalData = evidence
        };

        SecurityLogger.RecordSuspiciousActivity(description, activityType, metadata);
    }

    /// <summary>
    /// Logs a cryptographic operation.
    /// </summary>
    protected void LogCryptographicOperation(CryptoOperationType operationType, bool success,
        string? algorithm = null, string? keyId = null, [CallerMemberName] string? operationName = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            OperationName = operationName,
            AdditionalData = keyId != null ? new Dictionary<string, object> { ["KeyId"] = keyId } : null
        };

        SecurityLogger.RecordCryptographicOperation(operationType, success, algorithm, metadata);
    }

    /// <summary>
    /// Logs an enclave operation.
    /// </summary>
    protected void LogEnclaveOperation(string operation, bool success, string? attestationStatus = null,
        [CallerMemberName] string? callerOperation = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            OperationName = callerOperation
        };

        SecurityLogger.RecordEnclaveOperation(operation, success, attestationStatus, metadata);
    }

    /// <summary>
    /// Logs data access.
    /// </summary>
    protected void LogDataAccess(string userId, string resource, DataAccessType accessType, bool success,
        string? clientId = null, int? recordCount = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            ClientId = clientId,
            AdditionalData = recordCount.HasValue 
                ? new Dictionary<string, object> { ["RecordCount"] = recordCount.Value } 
                : null
        };

        SecurityLogger.RecordDataAccess(userId, resource, accessType, success, metadata);
    }

    /// <summary>
    /// Logs a configuration change.
    /// </summary>
    protected void LogConfigurationChange(string userId, string configKey, string? oldValue, string? newValue,
        string? clientId = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            ClientId = clientId
        };

        SecurityLogger.RecordConfigurationChange(userId, configKey, oldValue, newValue, metadata);
    }

    /// <summary>
    /// Logs input validation failure.
    /// </summary>
    protected void LogValidationFailure(string validationType, string details,
        Dictionary<string, object>? validationErrors = null, string? clientId = null)
    {
        var metadata = new SecurityEventMetadata
        {
            ServiceName = _serviceName,
            ClientId = clientId,
            AdditionalData = validationErrors
        };

        SecurityLogger.RecordValidationFailure(validationType, details, metadata);
    }

    /// <summary>
    /// Validates input and logs security events for failures.
    /// </summary>
    protected bool ValidateAndLogInput<T>(T input, Func<T, (bool isValid, string? error)> validator,
        string validationType, string? clientId = null)
    {
        var (isValid, error) = validator(input);
        
        if (!isValid)
        {
            LogValidationFailure(validationType, error ?? "Validation failed", 
                new Dictionary<string, object> { ["Input"] = input?.ToString() ?? "null" }, clientId);
            
            // Check for potential security threats in validation failures
            if (error?.Contains("script", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                LogSuspiciousActivity("Potential XSS attempt detected in input validation", 
                    SuspiciousActivityType.XssAttempt, clientId);
            }
            else if ((error?.Contains("sql", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (error?.Contains("'; ", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                LogSuspiciousActivity("Potential SQL injection attempt detected in input validation", 
                    SuspiciousActivityType.SqlInjection, clientId);
            }
            else if ((error?.Contains("../", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (error?.Contains("..\\", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                LogSuspiciousActivity("Potential path traversal attempt detected in input validation", 
                    SuspiciousActivityType.PathTraversal, clientId);
            }
        }

        return isValid;
    }

    /// <summary>
    /// Executes an operation with security logging.
    /// </summary>
    protected async Task<TResult> ExecuteWithSecurityLoggingAsync<TResult>(
        Func<Task<TResult>> operation,
        string operationDescription,
        string? userId = null,
        string? clientId = null,
        [CallerMemberName] string? operationName = null)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            Logger.LogDebug("Starting secure operation: {Operation} for user {UserId}", 
                operationDescription, userId ?? "Unknown");
            
            var result = await operation();
            
            var duration = DateTime.UtcNow - startTime;
            Logger.LogInformation("Secure operation completed: {Operation} in {Duration}ms", 
                operationDescription, duration.TotalMilliseconds);
            
            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            LogAuthorizationCheck(userId ?? "Unknown", operationDescription, "Execute", false, clientId);
            Logger.LogError(ex, "Authorization failed for operation: {Operation}", operationDescription);
            throw;
        }
        catch (SecurityException ex)
        {
            LogSecurityEvent(SecurityEventType.SecurityViolation, 
                $"Security violation in {operationDescription}: {ex.Message}", clientId);
            Logger.LogError(ex, "Security violation in operation: {Operation}", operationDescription);
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError(ex, "Operation failed: {Operation} after {Duration}ms", 
                operationDescription, duration.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Checks rate limit with security logging.
    /// </summary>
    protected async Task<bool> CheckRateLimitAsync(string clientId, string operation, 
        Func<Task<(bool allowed, int attemptCount)>> rateLimitCheck)
    {
        var (allowed, attemptCount) = await rateLimitCheck();
        LogRateLimitCheck(clientId, operation, allowed, attemptCount);
        return allowed;
    }
}