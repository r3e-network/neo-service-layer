using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Security functionality for the Proof of Reserve Service.
/// </summary>
public partial class ProofOfReserveService
{
    private ProofOfReserveSecurityHelper? _securityHelper;
    private readonly Dictionary<string, DateTime> _operationTimestamps = new();
    private readonly object _securityLock = new();

    /// <summary>
    /// Initializes the security helper.
    /// </summary>
    public void InitializeSecurity()
    {
        try
        {
            var securityLogger = new SecurityHelperLogger(Logger);
            _securityHelper = new ProofOfReserveSecurityHelper(securityLogger);

            Logger.LogInformation("Security features enabled for Proof of Reserve Service");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize security features");
        }
    }

    /// <summary>
    /// Validates security context for operations.
    /// </summary>
    /// <param name="context">The security context.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <returns>The validation result.</returns>
    public async Task<SecurityValidationResult> ValidateSecurityContextAsync(SecurityContext context, string operation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(operation);

        if (_securityHelper == null)
        {
            return SecurityValidationResult.Success("Security not initialized");
        }

        try
        {
            var result = new SecurityValidationResult();

            // Check if IP is blocked
            if (_securityHelper.IsIpAddressBlocked(context.IpAddress))
            {
                result.IsValid = false;
                result.ErrorMessage = "IP address is blocked due to suspicious activity";
                result.ErrorCode = "IP_BLOCKED";
                return result;
            }

            // Validate session if provided
            if (!string.IsNullOrEmpty(context.SessionId))
            {
                var sessionValid = _securityHelper.ValidateSecuritySession(
                    context.SessionId,
                    context.ClientId,
                    context.IpAddress);

                if (!sessionValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid or expired session";
                    result.ErrorCode = "INVALID_SESSION";
                    return result;
                }
            }

            // Check rate limiting based on operation type
            var rateLimitConfig = GetRateLimitConfigForOperation(operation);
            var rateLimitPassed = _securityHelper.CheckRateLimit(
                context.ClientId,
                operation,
                rateLimitConfig);

            if (!rateLimitPassed)
            {
                result.IsValid = false;
                result.ErrorMessage = "Rate limit exceeded for this operation";
                result.ErrorCode = "RATE_LIMIT_EXCEEDED";
                return result;
            }

            // Validate CSRF token for write operations
            if (IsWriteOperation(operation) && !string.IsNullOrEmpty(context.CsrfToken))
            {
                var csrfValid = _securityHelper.ValidateCsrfToken(
                    context.CsrfToken,
                    context.SessionId ?? "anonymous",
                    context.ClientId);

                if (!csrfValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid CSRF token";
                    result.ErrorCode = "INVALID_CSRF_TOKEN";
                    return result;
                }
            }

            // Validate request signature for critical operations
            if (IsCriticalOperation(operation) && !string.IsNullOrEmpty(context.RequestSignature))
            {
                var signatureValid = _securityHelper.ValidateRequestSignature(
                    context.RequestBody ?? string.Empty,
                    context.RequestSignature,
                    context.PublicKey ?? string.Empty);

                if (!signatureValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid request signature";
                    result.ErrorCode = "INVALID_SIGNATURE";
                    return result;
                }
            }

            // Check for operation timing attacks
            if (await DetectTimingAttackAsync(context.ClientId, operation))
            {
                result.IsValid = false;
                result.ErrorMessage = "Suspicious timing pattern detected";
                result.ErrorCode = "TIMING_ATTACK_DETECTED";
                return result;
            }

            Logger.LogDebug("Security validation passed for operation {Operation} by client {ClientId}",
                operation, context.ClientId);

            result.IsValid = true;
            result.SessionId = context.SessionId;
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during security validation for operation {Operation}", operation);

            return new SecurityValidationResult
            {
                IsValid = false,
                ErrorMessage = "Internal security validation error",
                ErrorCode = "SECURITY_ERROR"
            };
        }
    }

    /// <summary>
    /// Creates a new authenticated session.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="userAgent">The client user agent.</param>
    /// <param name="authenticationMethod">The authentication method used.</param>
    /// <returns>The session information.</returns>
    public async Task<SessionCreationResult> CreateAuthenticatedSessionAsync(
        string clientId,
        string ipAddress,
        string? userAgent = null,
        string authenticationMethod = "API_KEY")
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(ipAddress);

        if (_securityHelper == null)
        {
            return new SessionCreationResult
            {
                Success = false,
                ErrorMessage = "Security not initialized"
            };
        }

        try
        {
            // Check if IP is blocked
            if (_securityHelper.IsIpAddressBlocked(ipAddress))
            {
                return new SessionCreationResult
                {
                    Success = false,
                    ErrorMessage = "IP address is blocked"
                };
            }

            // Record authentication attempt
            _securityHelper.RecordAuthenticationAttempt(clientId, ipAddress, true, authenticationMethod);

            // Create security session
            var sessionId = _securityHelper.CreateSecuritySession(clientId, ipAddress, userAgent);

            // Generate CSRF token
            var csrfToken = _securityHelper.GenerateCsrfToken(sessionId, clientId);

            Logger.LogInformation("Created authenticated session {SessionId} for client {ClientId}",
                sessionId, clientId);

            return new SessionCreationResult
            {
                Success = true,
                SessionId = sessionId,
                CsrfToken = csrfToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating authenticated session for client {ClientId}", clientId);

            // Record failed authentication
            _securityHelper.RecordAuthenticationAttempt(clientId, ipAddress, false, authenticationMethod);

            return new SessionCreationResult
            {
                Success = false,
                ErrorMessage = "Failed to create session"
            };
        }
    }

    /// <summary>
    /// Invalidates an authenticated session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="reason">The reason for invalidation.</param>
    public void InvalidateSession(string sessionId, string reason = "User logout")
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);

        try
        {
            _securityHelper?.InvalidateSession(sessionId, reason);
            Logger.LogInformation("Invalidated session {SessionId}: {Reason}", sessionId, reason);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error invalidating session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Gets security statistics for monitoring.
    /// </summary>
    /// <returns>The security statistics.</returns>
    public SecurityStatistics? GetSecurityStatistics()
    {
        try
        {
            return _securityHelper?.GetSecurityStatistics();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting security statistics");
            return null;
        }
    }

    /// <summary>
    /// Gets the rate limit configuration for a specific operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>The rate limit configuration.</returns>
    private RateLimitConfig GetRateLimitConfigForOperation(string operation)
    {
        return operation.ToLowerInvariant() switch
        {
            "registerasset" => ProofOfReserveSecurityHelper.RateLimits.AssetRegistration,
            "generateproof" => ProofOfReserveSecurityHelper.RateLimits.ProofGeneration,
            "generateauditreport" => ProofOfReserveSecurityHelper.RateLimits.AuditReports,
            "updatereservedata" or "setalertthreshold" or "setupalert" => ProofOfReserveSecurityHelper.RateLimits.WriteOperations,
            _ => ProofOfReserveSecurityHelper.RateLimits.ReadOperations
        };
    }

    /// <summary>
    /// Determines if an operation is a write operation requiring CSRF protection.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>True if it's a write operation.</returns>
    private bool IsWriteOperation(string operation)
    {
        var writeOperations = new[]
        {
            "registerasset",
            "updatereservedata",
            "setalertthreshold",
            "setupalert",
            "invalidatesession"
        };

        return writeOperations.Contains(operation.ToLowerInvariant());
    }

    /// <summary>
    /// Determines if an operation is critical and requires signature validation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>True if it's a critical operation.</returns>
    private bool IsCriticalOperation(string operation)
    {
        var criticalOperations = new[]
        {
            "registerasset",
            "generateproof",
            "updatereservedata"
        };

        return criticalOperations.Contains(operation.ToLowerInvariant());
    }

    /// <summary>
    /// Detects potential timing attacks based on operation patterns.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <returns>True if a timing attack is detected.</returns>
    private async Task<bool> DetectTimingAttackAsync(string clientId, string operation)
    {
        try
        {
            await Task.CompletedTask;

            lock (_securityLock)
            {
                var operationKey = $"{clientId}:{operation}";
                var now = DateTime.UtcNow;

                if (_operationTimestamps.TryGetValue(operationKey, out var lastOperationTime))
                {
                    var timeSinceLastOperation = now - lastOperationTime;

                    // If operations are happening too frequently (less than 100ms apart)
                    if (timeSinceLastOperation < TimeSpan.FromMilliseconds(100))
                    {
                        Logger.LogWarning("Potential timing attack detected for client {ClientId} on operation {Operation}",
                            clientId, operation);
                        return true;
                    }
                }

                _operationTimestamps[operationKey] = now;

                // Clean old timestamps (keep only last hour)
                var cutoff = now.AddHours(-1);
                var keysToRemove = _operationTimestamps
                    .Where(kvp => kvp.Value < cutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _operationTimestamps.Remove(key);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error detecting timing attack for client {ClientId}", clientId);
            return false;
        }
    }

    /// <summary>
    /// Validates input parameters against injection attacks.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>True if the input is safe.</returns>
    private bool ValidateInputSafety(string input, string parameterName)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        // Check for common injection patterns
        var dangerousPatterns = new[]
        {
            "<script", "javascript:", "vbscript:", "onload=", "onerror=",
            "SELECT ", "INSERT ", "UPDATE ", "DELETE ", "DROP ",
            "UNION ", "OR 1=1", "'; --", "/*", "*/",
            "../", "..\\", "%2e%2e", "%2f", "%5c"
        };

        var inputLower = input.ToLowerInvariant();
        foreach (var pattern in dangerousPatterns)
        {
            if (inputLower.Contains(pattern.ToLowerInvariant()))
            {
                Logger.LogWarning("Potential injection attack detected in parameter {Parameter}: {Pattern}",
                    parameterName, pattern);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Sanitizes input string to prevent injection attacks.
    /// </summary>
    /// <param name="input">The input to sanitize.</param>
    /// <returns>The sanitized input.</returns>
    private string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove potentially dangerous characters
        var sanitized = input
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;")
            .Replace("/", "&#x2F;");

        return sanitized;
    }

    /// <summary>
    /// Disposes security-related resources.
    /// </summary>
    partial void DisposeSecurityResources()
    {
        _securityHelper?.Dispose();
    }
}

/// <summary>
/// Security context for operations.
/// </summary>
public class SecurityContext
{
    public string ClientId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? CsrfToken { get; set; }
    public string? RequestSignature { get; set; }
    public string? PublicKey { get; set; }
    public string? RequestBody { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Security validation result.
/// </summary>
public class SecurityValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static SecurityValidationResult Success(string? message = null)
    {
        return new SecurityValidationResult
        {
            IsValid = true,
            ErrorMessage = message
        };
    }

    public static SecurityValidationResult Failure(string errorMessage, string? errorCode = null)
    {
        return new SecurityValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Session creation result.
/// </summary>
public class SessionCreationResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? CsrfToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Logger wrapper for the security helper.
/// </summary>
internal class SecurityHelperLogger : ILogger<ProofOfReserveSecurityHelper>
{
    private readonly ILogger _baseLogger;

    public SecurityHelperLogger(ILogger baseLogger)
    {
        _baseLogger = baseLogger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _baseLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _baseLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _baseLogger.Log(logLevel, eventId, state, exception, formatter);
    }
}
