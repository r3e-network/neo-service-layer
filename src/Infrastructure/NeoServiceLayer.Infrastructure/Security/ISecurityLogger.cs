using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Provides security-specific logging capabilities for the Neo Service Layer.
/// </summary>
public interface ISecurityLogger
{
    /// <summary>
    /// Records a security event with detailed metadata.
    /// </summary>
    void RecordSecurityEvent(SecurityEventType eventType, string message, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records an authentication attempt.
    /// </summary>
    void RecordAuthenticationAttempt(string userId, bool success, string? reason = null, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records an authorization failure.
    /// </summary>
    void RecordAuthorizationFailure(string userId, string resource, string action, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records a rate limit violation.
    /// </summary>
    void RecordRateLimitViolation(string clientId, string operation, int attemptCount, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records suspicious activity.
    /// </summary>
    void RecordSuspiciousActivity(string description, SuspiciousActivityType activityType, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records a cryptographic operation.
    /// </summary>
    void RecordCryptographicOperation(CryptoOperationType operationType, bool success, string? algorithm = null, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records an enclave operation.
    /// </summary>
    void RecordEnclaveOperation(string operation, bool success, string? attestationStatus = null, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records a data access event.
    /// </summary>
    void RecordDataAccess(string userId, string resource, DataAccessType accessType, bool success, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records a configuration change.
    /// </summary>
    void RecordConfigurationChange(string userId, string configKey, string? oldValue, string? newValue, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Records a security validation failure.
    /// </summary>
    void RecordValidationFailure(string validationType, string details, SecurityEventMetadata? metadata = null);

    /// <summary>
    /// Gets security event statistics for a given time period.
    /// </summary>
    SecurityEventStatistics GetStatistics(DateTime startTime, DateTime endTime);

    /// <summary>
    /// Gets recent security events.
    /// </summary>
    IEnumerable<SecurityEvent> GetRecentEvents(int count = 100, SecurityEventType? filterByType = null);

    /// <summary>
    /// Clears old security events based on retention policy.
    /// </summary>
    Task CleanupOldEventsAsync(int retentionDays);
}

/// <summary>
/// Types of security events.
/// </summary>
public enum SecurityEventType
{
    Authentication,
    Authorization,
    RateLimit,
    SuspiciousActivity,
    CryptographicOperation,
    EnclaveOperation,
    DataAccess,
    ConfigurationChange,
    ValidationFailure,
    SecurityViolation,
    AuditTrail
}

/// <summary>
/// Types of suspicious activities.
/// </summary>
public enum SuspiciousActivityType
{
    TimingAttack,
    BruteForceAttempt,
    SqlInjection,
    XssAttempt,
    PathTraversal,
    UnusualPattern,
    IpAddressChange,
    GeoLocationAnomaly,
    UnknownDevice,
    AbnormalBehavior
}

/// <summary>
/// Types of cryptographic operations.
/// </summary>
public enum CryptoOperationType
{
    Encryption,
    Decryption,
    Signing,
    Verification,
    KeyGeneration,
    KeyRotation,
    HashComputation,
    RandomGeneration
}

/// <summary>
/// Types of data access operations.
/// </summary>
public enum DataAccessType
{
    Read,
    Write,
    Update,
    Delete,
    Export,
    Import,
    Query,
    BulkOperation
}

/// <summary>
/// Metadata associated with a security event.
/// </summary>
public class SecurityEventMetadata
{
    public string? ClientId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ServiceName { get; set; }
    public string? OperationName { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Represents a recorded security event.
/// </summary>
public class SecurityEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public SecurityEventType EventType { get; set; }
    public string Message { get; set; } = string.Empty;
    public SecurityEventMetadata? Metadata { get; set; }
    public LogLevel Severity { get; set; }
}

/// <summary>
/// Statistics about security events.
/// </summary>
public class SecurityEventStatistics
{
    public Dictionary<SecurityEventType, int> EventCounts { get; set; } = new();
    public Dictionary<string, int> TopClientIds { get; set; } = new();
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();
    public int TotalEvents { get; set; }
    public int SuccessfulAuthentications { get; set; }
    public int FailedAuthentications { get; set; }
    public int AuthorizationFailures { get; set; }
    public int RateLimitViolations { get; set; }
    public int SuspiciousActivities { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}