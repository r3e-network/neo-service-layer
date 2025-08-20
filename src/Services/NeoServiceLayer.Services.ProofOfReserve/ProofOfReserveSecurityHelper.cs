using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Security helper for the Proof of Reserve Service with rate limiting, CSRF protection, and authentication.
/// </summary>
public class ProofOfReserveSecurityHelper : IDisposable
{
    private readonly ILogger<ProofOfReserveSecurityHelper> Logger;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _rateLimitBuckets = new();
    private readonly ConcurrentDictionary<string, CsrfToken> _csrfTokens = new();
    private readonly ConcurrentDictionary<string, SecuritySession> _activeSessions = new();
    private readonly Timer _cleanupTimer;
    private readonly object _securityLock = new();
    private SecurityStatistics _statistics = new();

    /// <summary>
    /// Rate limiting configurations for different operation types.
    /// </summary>
    public static class RateLimits
    {
        public static readonly RateLimitConfig ReadOperations = new(100, TimeSpan.FromMinutes(1)); // 100 requests per minute
        public static readonly RateLimitConfig WriteOperations = new(20, TimeSpan.FromMinutes(1)); // 20 requests per minute
        public static readonly RateLimitConfig ProofGeneration = new(5, TimeSpan.FromMinutes(1)); // 5 proofs per minute
        public static readonly RateLimitConfig AssetRegistration = new(10, TimeSpan.FromHours(1)); // 10 registrations per hour
        public static readonly RateLimitConfig AuditReports = new(3, TimeSpan.FromMinutes(5)); // 3 reports per 5 minutes
        public static readonly RateLimitConfig Authentication = new(5, TimeSpan.FromMinutes(15)); // 5 auth attempts per 15 minutes
    }

    /// <summary>
    /// Security event types for monitoring and auditing.
    /// </summary>
    public enum SecurityEventType
    {
        RateLimitExceeded,
        InvalidCsrfToken,
        UnauthorizedAccess,
        SuspiciousActivity,
        AuthenticationFailure,
        SessionExpired,
        IpAddressBlocked,
        BruteForceAttempt
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofOfReserveSecurityHelper"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ProofOfReserveSecurityHelper(ILogger<ProofOfReserveSecurityHelper> logger)
    {
        Logger = logger;

        // Setup cleanup timer to run every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        Logger.LogDebug("Proof of Reserve security helper initialized");
    }

    /// <summary>
    /// Checks rate limiting for a specific client and operation.
    /// </summary>
    /// <param name="clientId">The client identifier (IP address, user ID, etc.).</param>
    /// <param name="operation">The operation type.</param>
    /// <param name="config">The rate limit configuration.</param>
    /// <returns>True if the request is allowed.</returns>
    public bool CheckRateLimit(string clientId, string operation, RateLimitConfig config)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(operation);
        ArgumentNullException.ThrowIfNull(config);

        var bucketKey = $"{clientId}:{operation}";
        var bucket = _rateLimitBuckets.GetOrAdd(bucketKey, _ => new RateLimitBucket(config));

        var isAllowed = bucket.TryConsume();

        if (!isAllowed)
        {
            RecordSecurityEvent(SecurityEventType.RateLimitExceeded, clientId, new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["Limit"] = config.RequestLimit,
                ["Window"] = config.TimeWindow.TotalMinutes
            });

            Logger.LogWarning("Rate limit exceeded for client {ClientId} on operation {Operation}", clientId, operation);
        }

        return isAllowed;
    }

    /// <summary>
    /// Generates a CSRF token for a client session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The CSRF token.</returns>
    public string GenerateCsrfToken(string sessionId, string clientId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ArgumentException.ThrowIfNullOrEmpty(clientId);

        var tokenData = new
        {
            SessionId = sessionId,
            ClientId = clientId,
            Timestamp = DateTime.UtcNow,
            Nonce = GenerateSecureNonce()
        };

        var tokenJson = JsonSerializer.Serialize(tokenData);
        var tokenBytes = Encoding.UTF8.GetBytes(tokenJson);
        var hash = SHA256.HashData(tokenBytes);
        var token = Convert.ToBase64String(hash);

        var csrfToken = new CsrfToken
        {
            Token = token,
            SessionId = sessionId,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30) // 30-minute expiration
        };

        _csrfTokens[token] = csrfToken;

        Logger.LogDebug("Generated CSRF token for session {SessionId}", sessionId);
        return token;
    }

    /// <summary>
    /// Validates a CSRF token for a client session.
    /// </summary>
    /// <param name="token">The CSRF token to validate.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>True if the token is valid.</returns>
    public bool ValidateCsrfToken(string token, string sessionId, string clientId)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ArgumentException.ThrowIfNullOrEmpty(clientId);

        if (!_csrfTokens.TryGetValue(token, out var csrfToken))
        {
            RecordSecurityEvent(SecurityEventType.InvalidCsrfToken, clientId, new Dictionary<string, object>
            {
                ["Token"] = token[..Math.Min(8, token.Length)] + "...", // Log only first 8 chars
                ["SessionId"] = sessionId
            });

            Logger.LogWarning("Invalid CSRF token provided by client {ClientId}", clientId);
            return false;
        }

        if (csrfToken.ExpiresAt < DateTime.UtcNow)
        {
            _csrfTokens.TryRemove(token, out _);
            Logger.LogWarning("Expired CSRF token provided by client {ClientId}", clientId);
            return false;
        }

        if (csrfToken.SessionId != sessionId || csrfToken.ClientId != clientId)
        {
            RecordSecurityEvent(SecurityEventType.InvalidCsrfToken, clientId, new Dictionary<string, object>
            {
                ["ExpectedSessionId"] = sessionId,
                ["ActualSessionId"] = csrfToken.SessionId,
                ["ExpectedClientId"] = clientId,
                ["ActualClientId"] = csrfToken.ClientId
            });

            Logger.LogWarning("CSRF token session/client mismatch for client {ClientId}", clientId);
            return false;
        }

        Logger.LogDebug("CSRF token validated successfully for session {SessionId}", sessionId);
        return true;
    }

    /// <summary>
    /// Creates a new security session for a client.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="userAgent">The client user agent.</param>
    /// <returns>The session identifier.</returns>
    public string CreateSecuritySession(string clientId, string ipAddress, string? userAgent = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(ipAddress);

        var sessionId = Guid.NewGuid().ToString();
        var session = new SecuritySession
        {
            SessionId = sessionId,
            ClientId = clientId,
            IpAddress = ipAddress,
            UserAgent = userAgent ?? "Unknown",
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8), // 8-hour session
            IsActive = true
        };

        _activeSessions[sessionId] = session;

        Logger.LogInformation("Created security session {SessionId} for client {ClientId}", sessionId, clientId);
        return sessionId;
    }

    /// <summary>
    /// Validates a security session and updates last activity.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <returns>True if the session is valid.</returns>
    public bool ValidateSecuritySession(string sessionId, string clientId, string ipAddress)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(ipAddress);

        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            RecordSecurityEvent(SecurityEventType.UnauthorizedAccess, clientId, new Dictionary<string, object>
            {
                ["SessionId"] = sessionId,
                ["IpAddress"] = ipAddress
            });

            Logger.LogWarning("Invalid session {SessionId} for client {ClientId}", sessionId, clientId);
            return false;
        }

        if (!session.IsActive || session.ExpiresAt < DateTime.UtcNow)
        {
            _activeSessions.TryRemove(sessionId, out _);
            RecordSecurityEvent(SecurityEventType.SessionExpired, clientId, new Dictionary<string, object>
            {
                ["SessionId"] = sessionId,
                ["ExpiresAt"] = session.ExpiresAt
            });

            Logger.LogWarning("Expired session {SessionId} for client {ClientId}", sessionId, clientId);
            return false;
        }

        if (session.ClientId != clientId)
        {
            RecordSecurityEvent(SecurityEventType.UnauthorizedAccess, clientId, new Dictionary<string, object>
            {
                ["SessionId"] = sessionId,
                ["ExpectedClientId"] = clientId,
                ["ActualClientId"] = session.ClientId
            });

            Logger.LogWarning("Session client mismatch for session {SessionId}", sessionId);
            return false;
        }

        // Check for IP address changes (potential session hijacking)
        if (session.IpAddress != ipAddress)
        {
            RecordSecurityEvent(SecurityEventType.SuspiciousActivity, clientId, new Dictionary<string, object>
            {
                ["SessionId"] = sessionId,
                ["OriginalIp"] = session.IpAddress,
                ["CurrentIp"] = ipAddress,
                ["Activity"] = "IP address change"
            });

            Logger.LogWarning("IP address change detected for session {SessionId}: {OriginalIp} -> {CurrentIp}",
                sessionId, session.IpAddress, ipAddress);

            // For now, allow but log. In production, you might want to invalidate the session
        }

        // Update last activity
        session.LastActivity = DateTime.UtcNow;

        Logger.LogDebug("Session {SessionId} validated successfully", sessionId);
        return true;
    }

    /// <summary>
    /// Invalidates a security session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="reason">The reason for invalidation.</param>
    public void InvalidateSession(string sessionId, string reason = "User logout")
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);

        if (_activeSessions.TryRemove(sessionId, out var session))
        {
            session.IsActive = false;
            Logger.LogInformation("Invalidated session {SessionId}: {Reason}", sessionId, reason);
        }
    }

    /// <summary>
    /// Checks if an IP address is blocked due to suspicious activity.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>True if the IP address is blocked.</returns>
    public bool IsIpAddressBlocked(string ipAddress)
    {
        ArgumentException.ThrowIfNullOrEmpty(ipAddress);

        // Check for brute force attempts
        var authAttempts = GetRecentAuthenticationAttempts(ipAddress);
        if (authAttempts.Count >= 10 && authAttempts.All(a => !a.Success))
        {
            RecordSecurityEvent(SecurityEventType.IpAddressBlocked, ipAddress, new Dictionary<string, object>
            {
                ["Reason"] = "Excessive failed authentication attempts",
                ["AttemptCount"] = authAttempts.Count
            });

            Logger.LogWarning("IP address {IpAddress} blocked due to brute force attempts", ipAddress);
            return true;
        }

        // Check rate limit violations
        var rateLimitViolations = GetRecentRateLimitViolations(ipAddress);
        if (rateLimitViolations >= 5)
        {
            RecordSecurityEvent(SecurityEventType.IpAddressBlocked, ipAddress, new Dictionary<string, object>
            {
                ["Reason"] = "Excessive rate limit violations",
                ["ViolationCount"] = rateLimitViolations
            });

            Logger.LogWarning("IP address {IpAddress} blocked due to rate limit violations", ipAddress);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Records an authentication attempt for monitoring.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="success">Whether the attempt was successful.</param>
    /// <param name="method">The authentication method used.</param>
    public void RecordAuthenticationAttempt(string clientId, string ipAddress, bool success, string method = "Unknown")
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(ipAddress);

        var attempt = new AuthenticationAttempt
        {
            ClientId = clientId,
            IpAddress = ipAddress,
            Success = success,
            Method = method,
            Timestamp = DateTime.UtcNow
        };

        lock (_securityLock)
        {
            _statistics.AuthenticationAttempts.Add(attempt);

            // Keep only last 100 attempts
            if (_statistics.AuthenticationAttempts.Count > 100)
            {
                _statistics.AuthenticationAttempts.RemoveAt(0);
            }
        }

        if (!success)
        {
            RecordSecurityEvent(SecurityEventType.AuthenticationFailure, clientId, new Dictionary<string, object>
            {
                ["IpAddress"] = ipAddress,
                ["Method"] = method
            });
        }

        Logger.LogDebug("Authentication attempt recorded: Client {ClientId}, Success {Success}", clientId, success);
    }

    /// <summary>
    /// Validates request signature for API authentication.
    /// </summary>
    /// <param name="requestBody">The request body.</param>
    /// <param name="signature">The provided signature.</param>
    /// <param name="publicKey">The client's public key.</param>
    /// <returns>True if the signature is valid.</returns>
    public bool ValidateRequestSignature(string requestBody, string signature, string publicKey)
    {
        ArgumentNullException.ThrowIfNull(requestBody);
        ArgumentException.ThrowIfNullOrEmpty(signature);
        ArgumentException.ThrowIfNullOrEmpty(publicKey);

        try
        {
            // In a real implementation, this would use proper cryptographic signature verification
            // For now, create a simple hash-based validation
            var expectedSignature = ComputeRequestSignature(requestBody, publicKey);
            var isValid = signature == expectedSignature;

            Logger.LogDebug("Request signature validation: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating request signature");
            return false;
        }
    }

    /// <summary>
    /// Gets security statistics for monitoring.
    /// </summary>
    /// <returns>The security statistics.</returns>
    public SecurityStatistics GetSecurityStatistics()
    {
        lock (_securityLock)
        {
            return new SecurityStatistics
            {
                TotalSessions = _activeSessions.Count,
                ActiveSessions = _activeSessions.Values.Count(s => s.IsActive),
                TotalRateLimitBuckets = _rateLimitBuckets.Count,
                TotalCsrfTokens = _csrfTokens.Count,
                RecentSecurityEvents = _statistics.SecurityEvents.TakeLast(50).ToList(),
                AuthenticationAttempts = _statistics.AuthenticationAttempts.ToList(),
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Generates a secure random nonce.
    /// </summary>
    /// <returns>The generated nonce.</returns>
    private string GenerateSecureNonce()
    {
        var nonce = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(nonce);
        return Convert.ToBase64String(nonce);
    }

    /// <summary>
    /// Computes a request signature for validation.
    /// </summary>
    /// <param name="requestBody">The request body.</param>
    /// <param name="publicKey">The public key.</param>
    /// <returns>The computed signature.</returns>
    private string ComputeRequestSignature(string requestBody, string publicKey)
    {
        var data = $"{requestBody}:{publicKey}:{DateTime.UtcNow:yyyy-MM-dd}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Gets recent authentication attempts for an IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <returns>The recent authentication attempts.</returns>
    private List<AuthenticationAttempt> GetRecentAuthenticationAttempts(string ipAddress)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-15);

        lock (_securityLock)
        {
            return _statistics.AuthenticationAttempts
                .Where(a => a.IpAddress == ipAddress && a.Timestamp >= cutoff)
                .ToList();
        }
    }

    /// <summary>
    /// Gets recent rate limit violations for an IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <returns>The number of recent violations.</returns>
    private int GetRecentRateLimitViolations(string ipAddress)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);

        lock (_securityLock)
        {
            return _statistics.SecurityEvents
                .Count(e => e.EventType == SecurityEventType.RateLimitExceeded &&
                           e.ClientId == ipAddress &&
                           e.Timestamp >= cutoff);
        }
    }

    /// <summary>
    /// Records a security event for monitoring and auditing.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="metadata">Additional event metadata.</param>
    private void RecordSecurityEvent(SecurityEventType eventType, string clientId, Dictionary<string, object>? metadata = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            ClientId = clientId,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        lock (_securityLock)
        {
            _statistics.SecurityEvents.Add(securityEvent);

            // Keep only last 200 events
            if (_statistics.SecurityEvents.Count > 200)
            {
                _statistics.SecurityEvents.RemoveAt(0);
            }
        }

        Logger.LogInformation("Security event recorded: {EventType} for client {ClientId}", eventType, clientId);
    }

    /// <summary>
    /// Cleans up expired entries (sessions, tokens, rate limit buckets).
    /// </summary>
    /// <param name="state">Timer state.</param>
    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var removedCount = 0;

            // Clean expired sessions
            var expiredSessions = _activeSessions
                .Where(kvp => !kvp.Value.IsActive || kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in expiredSessions)
            {
                if (_activeSessions.TryRemove(sessionId, out _))
                    removedCount++;
            }

            // Clean expired CSRF tokens
            var expiredTokens = _csrfTokens
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                if (_csrfTokens.TryRemove(token, out _))
                    removedCount++;
            }

            // Clean old rate limit buckets
            var expiredBuckets = _rateLimitBuckets
                .Where(kvp => kvp.Value.LastReset.Add(kvp.Value.Config.TimeWindow.Add(TimeSpan.FromMinutes(5))) < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var bucketKey in expiredBuckets)
            {
                if (_rateLimitBuckets.TryRemove(bucketKey, out _))
                    removedCount++;
            }

            if (removedCount > 0)
            {
                Logger.LogDebug("Cleaned up {Count} expired security entries", removedCount);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during security cleanup");
        }
    }

    /// <summary>
    /// Disposes the security helper.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _rateLimitBuckets.Clear();
        _csrfTokens.Clear();
        _activeSessions.Clear();
        Logger.LogDebug("Proof of Reserve security helper disposed");
    }
}

/// <summary>
/// Rate limit configuration.
/// </summary>
public record RateLimitConfig(int RequestLimit, TimeSpan TimeWindow);

/// <summary>
/// Rate limit bucket for token bucket algorithm.
/// </summary>
public class RateLimitBucket
{
    public RateLimitConfig Config { get; }
    public int TokensRemaining { get; private set; }
    public DateTime LastReset { get; private set; }
    private readonly object _lock = new();

    public RateLimitBucket(RateLimitConfig config)
    {
        Config = config;
        TokensRemaining = config.RequestLimit;
        LastReset = DateTime.UtcNow;
    }

    public bool TryConsume()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // Reset bucket if time window has passed
            if (now - LastReset >= Config.TimeWindow)
            {
                TokensRemaining = Config.RequestLimit;
                LastReset = now;
            }

            if (TokensRemaining > 0)
            {
                TokensRemaining--;
                return true;
            }

            return false;
        }
    }
}

/// <summary>
/// CSRF token information.
/// </summary>
public class CsrfToken
{
    public string Token { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Security session information.
/// </summary>
public class SecuritySession
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Security event information.
/// </summary>
public class SecurityEvent
{
    public string EventId { get; set; } = string.Empty;
    public ProofOfReserveSecurityHelper.SecurityEventType EventType { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Authentication attempt information.
/// </summary>
public class AuthenticationAttempt
{
    public string ClientId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Security statistics for monitoring.
/// </summary>
public class SecurityStatistics
{
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalRateLimitBuckets { get; set; }
    public int TotalCsrfTokens { get; set; }
    public List<SecurityEvent> SecurityEvents { get; set; } = new();
    public List<SecurityEvent> RecentSecurityEvents { get; set; } = new();
    public List<AuthenticationAttempt> AuthenticationAttempts { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
