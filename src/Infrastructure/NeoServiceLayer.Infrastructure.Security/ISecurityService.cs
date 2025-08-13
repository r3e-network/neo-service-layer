using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Interface for comprehensive security services.
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Validates input for security threats.
    /// </summary>
    Task<SecurityValidationResult> ValidateInputAsync(object input, SecurityValidationOptions options);

    /// <summary>
    /// Sanitizes input to remove dangerous content.
    /// </summary>
    Task<string> SanitizeInputAsync(string input, SanitizationOptions options);

    /// <summary>
    /// Encrypts data using secure algorithms.
    /// </summary>
    Task<EncryptionResult> EncryptDataAsync(byte[] data, EncryptionOptions options);

    /// <summary>
    /// Decrypts data using secure algorithms.
    /// </summary>
    Task<DecryptionResult> DecryptDataAsync(byte[] encryptedData, string keyId);

    /// <summary>
    /// Computes secure hash of data.
    /// </summary>
    Task<string> ComputeSecureHashAsync(byte[] data, HashAlgorithmType algorithm);

    /// <summary>
    /// Checks rate limiting for a resource.
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(string identifier, int maxRequests, TimeSpan timeWindow);

    /// <summary>
    /// Gets security policy for a resource type.
    /// </summary>
    Task<SecurityPolicy> GetSecurityPolicyAsync(string resourceType);
}

/// <summary>
/// Security validation options.
/// </summary>
public class SecurityValidationOptions
{
    public bool CheckSqlInjection { get; set; } = true;
    public bool CheckXss { get; set; } = true;
    public bool CheckCodeInjection { get; set; } = true;
    public bool CheckPathTraversal { get; set; } = true;
    public int MaxInputSize { get; set; } = 10 * 1024 * 1024; // 10MB
}

/// <summary>
/// Input sanitization options.
/// </summary>
public class SanitizationOptions
{
    public bool EncodeHtml { get; set; } = true;
    public bool EncodeJavaScript { get; set; } = true;
    public bool EncodeSqlParameters { get; set; } = true;
    public bool RemoveDangerousChars { get; set; } = true;
    public int MaxLength { get; set; } = 0; // 0 means no limit
}

/// <summary>
/// Encryption options.
/// </summary>
public class EncryptionOptions
{
    public string? KeyId { get; set; }
    public int KeySize { get; set; } = 256; // AES-256
    public bool UseHardwareRng { get; set; } = true;
}

/// <summary>
/// Security validation result.
/// </summary>
public class SecurityValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public SecurityLevel SecurityLevel { get; set; }
}

/// <summary>
/// Encryption result.
/// </summary>
public class EncryptionResult
{
    public bool Success { get; set; }
    public byte[]? EncryptedData { get; set; }
    public string? KeyId { get; set; }
    public string? Algorithm { get; set; }
    public string? IntegrityHash { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Decryption result.
/// </summary>
public class DecryptionResult
{
    public bool Success { get; set; }
    public byte[]? DecryptedData { get; set; }
    public string? Algorithm { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Rate limiting result.
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RequestCount { get; set; }
    public int RemainingRequests { get; set; }
    public DateTime ResetTime { get; set; }
    public TimeSpan RetryAfter { get; set; }
}

/// <summary>
/// Security policy configuration.
/// </summary>
public class SecurityPolicy
{
    public string ResourceType { get; set; } = "";
    public bool RequiresAuthentication { get; set; } = true;
    public bool RequiresEncryption { get; set; } = true;
    public int MaxInputSize { get; set; } = 1024 * 1024; // 1MB
    public int RateLimitRequests { get; set; } = 100;
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);
    public bool ValidateInput { get; set; } = true;
    public bool LogSecurityEvents { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Security levels.
/// </summary>
public enum SecurityLevel
{
    Safe,
    Low,
    Medium,
    High,
    Critical,
    Unknown
}

/// <summary>
/// Hash algorithm types.
/// </summary>
public enum HashAlgorithmType
{
    SHA256,
    SHA384,
    SHA512
}

/// <summary>
/// Security exception.
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}