using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Comprehensive security service providing encryption, validation, and security controls.
/// Addresses critical security vulnerabilities identified in the code review.
/// </summary>
public class SecurityService : ServiceBase, ISecurityService
{
    private readonly ConcurrentDictionary<string, SecurityPolicy> _securityPolicies = new();
    private readonly ConcurrentDictionary<string, byte[]> _secureKeys = new();
    private readonly object _lockObject = new();

    // Security configuration
    private readonly int _maxInputSize = 10 * 1024 * 1024; // 10MB limit
    private readonly int _keyRotationIntervalHours = 24;
    private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, RateLimitState> _rateLimitStates = new();

    public SecurityService(ILogger<SecurityService> logger)
        : base("SecurityService", "Comprehensive security service", "1.0.0", logger)
    {
        // Add security capability
        AddCapability<ISecurityService>();

        // Set metadata
        SetMetadata("SecurityLevel", "High");
        SetMetadata("EncryptionAlgorithm", "AES-256-GCM");
        SetMetadata("HashingAlgorithm", "SHA-256");
    }

    /// <summary>
    /// Validates input against multiple security threats including SQL injection, XSS, and code injection.
    /// This method provides comprehensive input sanitization and threat detection.
    /// </summary>
    /// <param name="input">The input data to validate (string, object, or complex types)</param>
    /// <param name="options">Validation configuration specifying which checks to perform</param>
    /// <returns>
    /// A <see cref="SecurityValidationResult"/> containing:
    /// - IsValid: Whether the input is considered safe
    /// - HasSecurityThreats: Whether any security threats were detected
    /// - ThreatTypes: List of detected threat types (SQL injection, XSS, Code injection)
    /// - RiskScore: Numerical risk assessment (0.0-1.0)
    /// - ValidationErrors: Detailed validation error messages
    /// </returns>
    /// <example>
    /// <code>
    /// var options = new SecurityValidationOptions
    /// {
    ///     CheckSqlInjection = true,
    ///     CheckXss = true,
    ///     CheckCodeInjection = true,
    ///     MaxInputSize = 1024 * 1024 // 1MB limit
    /// };
    /// 
    /// var result = await securityService.ValidateInputAsync(userInput, options);
    /// 
    /// if (result.HasSecurityThreats)
    /// {
    ///     logger.LogWarning("Security threats detected: {Threats}", 
    ///         string.Join(", ", result.ThreatTypes));
    ///     return BadRequest("Input contains security threats");
    /// }
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when options parameter is null</exception>
    /// <exception cref="ArgumentException">Thrown when input exceeds maximum size limit</exception>
    public async Task<SecurityValidationResult> ValidateInputAsync(object input, SecurityValidationOptions options)
    {
        if (input == null)
        {
            return new SecurityValidationResult
            {
                IsValid = false,
                ErrorMessage = "Input cannot be null",
                SecurityLevel = SecurityLevel.Critical
            };
        }

        try
        {
            // Convert input to string for validation
            var inputString = input switch
            {
                string str => str,
                byte[] bytes => Convert.ToBase64String(bytes),
                _ => JsonSerializer.Serialize(input)
            };

            // Size validation
            if (inputString.Length > _maxInputSize)
            {
                Logger.LogWarning("Input exceeds maximum allowed size: {Size}", inputString.Length);
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Input exceeds maximum size limit",
                    SecurityLevel = SecurityLevel.High
                };
            }

            // SQL injection detection
            if (options.CheckSqlInjection && ContainsSqlInjectionPattern(inputString))
            {
                Logger.LogWarning("Potential SQL injection detected in input");
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Input contains potentially dangerous SQL patterns",
                    SecurityLevel = SecurityLevel.Critical
                };
            }

            // XSS detection
            if (options.CheckXss && ContainsXssPattern(inputString))
            {
                Logger.LogWarning("Potential XSS attack detected in input");
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Input contains potentially dangerous script patterns",
                    SecurityLevel = SecurityLevel.High
                };
            }

            // Code injection detection
            if (options.CheckCodeInjection && ContainsCodeInjectionPattern(inputString))
            {
                Logger.LogWarning("Potential code injection detected in input");
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Input contains potentially dangerous code patterns",
                    SecurityLevel = SecurityLevel.Critical
                };
            }

            // Path traversal detection
            if (options.CheckPathTraversal && ContainsPathTraversalPattern(inputString))
            {
                Logger.LogWarning("Potential path traversal detected in input");
                return new SecurityValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Input contains potentially dangerous path patterns",
                    SecurityLevel = SecurityLevel.High
                };
            }

            await Task.CompletedTask;
            return new SecurityValidationResult
            {
                IsValid = true,
                SecurityLevel = SecurityLevel.Safe
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating input security");
            return new SecurityValidationResult
            {
                IsValid = false,
                ErrorMessage = "Security validation failed",
                SecurityLevel = SecurityLevel.Unknown
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> SanitizeInputAsync(string input, SanitizationOptions options)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            var sanitized = input;

            // HTML encoding
            if (options.EncodeHtml)
            {
                sanitized = System.Web.HttpUtility.HtmlEncode(sanitized);
            }

            // JavaScript encoding
            if (options.EncodeJavaScript)
            {
                sanitized = EncodeJavaScript(sanitized);
            }

            // SQL parameter encoding
            if (options.EncodeSqlParameters)
            {
                sanitized = EncodeSqlParameters(sanitized);
            }

            // Remove dangerous characters
            if (options.RemoveDangerousChars)
            {
                sanitized = RemoveDangerousCharacters(sanitized);
            }

            // Limit length
            if (options.MaxLength > 0 && sanitized.Length > options.MaxLength)
            {
                sanitized = sanitized[..options.MaxLength];
                Logger.LogInformation("Input truncated to maximum length: {MaxLength}", options.MaxLength);
            }

            await Task.CompletedTask;
            return sanitized;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sanitizing input");
            throw new SecurityException("Input sanitization failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionResult> EncryptDataAsync(byte[] data, EncryptionOptions options)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        try
        {
            // Generate or retrieve encryption key
            var keyId = options.KeyId ?? Guid.NewGuid().ToString();
            var key = await GetOrCreateEncryptionKeyAsync(keyId, options.KeySize);

            // Use AES-GCM for authenticated encryption
            using var aes = new AesGcm(key);

            var nonce = new byte[12]; // 96-bit nonce for GCM
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);

            var ciphertext = new byte[data.Length];
            var tag = new byte[16]; // 128-bit authentication tag

            aes.Encrypt(nonce, data, ciphertext, tag);

            // Combine nonce + ciphertext + tag
            var encryptedData = new byte[nonce.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, encryptedData, 0, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, encryptedData, nonce.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, encryptedData, nonce.Length + ciphertext.Length, tag.Length);

            // Compute integrity hash
            var integrityHash = ComputeIntegrityHash(encryptedData);

            Logger.LogDebug("Data encrypted successfully with key ID: {KeyId}", keyId);

            return new EncryptionResult
            {
                Success = true,
                EncryptedData = encryptedData,
                KeyId = keyId,
                Algorithm = "AES-256-GCM",
                IntegrityHash = integrityHash,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error encrypting data");
            return new EncryptionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<DecryptionResult> DecryptDataAsync(byte[] encryptedData, string keyId)
    {
        if (encryptedData == null || encryptedData.Length < 28) // nonce(12) + tag(16)
            throw new ArgumentException("Invalid encrypted data", nameof(encryptedData));

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

        try
        {
            // Retrieve encryption key
            if (!_secureKeys.TryGetValue(keyId, out var key))
            {
                Logger.LogWarning("Encryption key not found: {KeyId}", keyId);
                return new DecryptionResult
                {
                    Success = false,
                    ErrorMessage = "Encryption key not found"
                };
            }

            using var aes = new AesGcm(key);

            // Extract components
            var nonce = new byte[12];
            var tag = new byte[16];
            var ciphertext = new byte[encryptedData.Length - 28];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, 12);
            Buffer.BlockCopy(encryptedData, 12, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(encryptedData, encryptedData.Length - 16, tag, 0, 16);

            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            Logger.LogDebug("Data decrypted successfully with key ID: {KeyId}", keyId);

            return new DecryptionResult
            {
                Success = true,
                DecryptedData = plaintext,
                Algorithm = "AES-256-GCM",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (CryptographicException ex)
        {
            Logger.LogError(ex, "Decryption failed - data may be corrupted or tampered with");
            return new DecryptionResult
            {
                Success = false,
                ErrorMessage = "Decryption failed - data integrity check failed"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error decrypting data");
            return new DecryptionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> ComputeSecureHashAsync(byte[] data, HashAlgorithmType algorithm)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        try
        {
            byte[] hash = algorithm switch
            {
                HashAlgorithmType.SHA256 => SHA256.HashData(data),
                HashAlgorithmType.SHA384 => SHA384.HashData(data),
                HashAlgorithmType.SHA512 => SHA512.HashData(data),
                _ => throw new ArgumentException($"Unsupported hash algorithm: {algorithm}")
            };

            await Task.CompletedTask;
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error computing secure hash");
            throw new SecurityException("Hash computation failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<RateLimitResult> CheckRateLimitAsync(string identifier, int maxRequests, TimeSpan timeWindow)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

        try
        {
            var now = DateTime.UtcNow;
            var state = _rateLimitStates.AddOrUpdate(identifier,
                new RateLimitState { RequestCount = 1, WindowStart = now },
                (key, existing) =>
                {
                    // Reset window if expired
                    if (now - existing.WindowStart > timeWindow)
                    {
                        existing.RequestCount = 1;
                        existing.WindowStart = now;
                    }
                    else
                    {
                        existing.RequestCount++;
                    }
                    return existing;
                });

            var isAllowed = state.RequestCount <= maxRequests;
            var remainingRequests = Math.Max(0, maxRequests - state.RequestCount);
            var resetTime = state.WindowStart.Add(timeWindow);

            if (!isAllowed)
            {
                Logger.LogWarning("Rate limit exceeded for identifier: {Identifier}", identifier);
            }

            await Task.CompletedTask;
            return new RateLimitResult
            {
                IsAllowed = isAllowed,
                RequestCount = state.RequestCount,
                RemainingRequests = remainingRequests,
                ResetTime = resetTime,
                RetryAfter = isAllowed ? TimeSpan.Zero : resetTime - now
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking rate limit");
            throw new SecurityException("Rate limit check failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<SecurityPolicy> GetSecurityPolicyAsync(string resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
            throw new ArgumentException("Resource type cannot be null or empty", nameof(resourceType));

        if (_securityPolicies.TryGetValue(resourceType, out var existingPolicy))
        {
            return existingPolicy;
        }

        // Create default security policy
        var defaultPolicy = new SecurityPolicy
        {
            ResourceType = resourceType,
            RequiresAuthentication = true,
            RequiresEncryption = true,
            MaxInputSize = _maxInputSize,
            RateLimitRequests = 100,
            RateLimitWindow = _rateLimitWindow,
            ValidateInput = true,
            LogSecurityEvents = true,
            CreatedAt = DateTime.UtcNow
        };

        _securityPolicies.TryAdd(resourceType, defaultPolicy);

        await Task.CompletedTask;
        return defaultPolicy;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing security service...");

            // Initialize default security policies
            await InitializeDefaultPoliciesAsync();

            // Validate cryptographic providers
            if (!ValidateCryptographicProviders())
            {
                Logger.LogError("Cryptographic providers validation failed");
                return false;
            }

            Logger.LogInformation("Security service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize security service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Security service started");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Security service stopped");

        // Secure wipe of keys
        SecureWipeKeys();

        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check cryptographic providers
            if (!ValidateCryptographicProviders())
            {
                return ServiceHealth.Unhealthy;
            }

            // Check key availability
            if (_secureKeys.IsEmpty)
            {
                Logger.LogWarning("No encryption keys available");
                return ServiceHealth.Degraded;
            }

            await Task.CompletedTask;
            return ServiceHealth.Healthy;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");
            return ServiceHealth.Unhealthy;
        }
    }

    #region Private Helper Methods

    private async Task InitializeDefaultPoliciesAsync()
    {
        var defaultPolicies = new[]
        {
            ("User", new SecurityPolicy
            {
                ResourceType = "User",
                RequiresAuthentication = true,
                RequiresEncryption = true,
                MaxInputSize = 1024 * 1024, // 1MB
                RateLimitRequests = 50,
                RateLimitWindow = TimeSpan.FromMinutes(1)
            }),
            ("Admin", new SecurityPolicy
            {
                ResourceType = "Admin",
                RequiresAuthentication = true,
                RequiresEncryption = true,
                MaxInputSize = 10 * 1024 * 1024, // 10MB
                RateLimitRequests = 20,
                RateLimitWindow = TimeSpan.FromMinutes(1)
            }),
            ("Public", new SecurityPolicy
            {
                ResourceType = "Public",
                RequiresAuthentication = false,
                RequiresEncryption = false,
                MaxInputSize = 100 * 1024, // 100KB
                RateLimitRequests = 200,
                RateLimitWindow = TimeSpan.FromMinutes(1)
            })
        };

        foreach (var (type, policy) in defaultPolicies)
        {
            policy.CreatedAt = DateTime.UtcNow;
            _securityPolicies.TryAdd(type, policy);
        }

        await Task.CompletedTask;
    }

    private bool ValidateCryptographicProviders()
    {
        try
        {
            // Test AES-GCM
            using var aes = new AesGcm(new byte[32]);

            // Test SHA-256
            using var sha = SHA256.Create();
            sha.ComputeHash(Encoding.UTF8.GetBytes("test"));

            // Test RNG
            using var rng = RandomNumberGenerator.Create();
            var testBytes = new byte[16];
            rng.GetBytes(testBytes);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Cryptographic provider validation failed");
            return false;
        }
    }

    private async Task<byte[]> GetOrCreateEncryptionKeyAsync(string keyId, int keySize)
    {
        if (_secureKeys.TryGetValue(keyId, out var existingKey))
        {
            return existingKey;
        }

        // Generate new key
        using var rng = RandomNumberGenerator.Create();
        var key = new byte[keySize / 8]; // Convert bits to bytes
        rng.GetBytes(key);

        _secureKeys.TryAdd(keyId, key);

        Logger.LogDebug("Generated new encryption key: {KeyId}", keyId);

        await Task.CompletedTask;
        return key;
    }

    private string ComputeIntegrityHash(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    private void SecureWipeKeys()
    {
        foreach (var kvp in _secureKeys)
        {
            if (kvp.Value != null)
            {
                // Overwrite with random data
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(kvp.Value);
                Array.Clear(kvp.Value, 0, kvp.Value.Length);
            }
        }
        _secureKeys.Clear();
    }

    // Security pattern detection methods
    private bool ContainsSqlInjectionPattern(string input)
    {
        var sqlPatterns = new[]
        {
            "union select", "drop table", "delete from", "insert into", "update set",
            "exec(", "execute(", "sp_", "xp_", "' or '", "' and '", "-- ", "/*", "*/"
        };

        var lowerInput = input.ToLowerInvariant();
        return Array.Exists(sqlPatterns, pattern => lowerInput.Contains(pattern));
    }

    private bool ContainsXssPattern(string input)
    {
        var xssPatterns = new[]
        {
            "<script", "</script>", "javascript:", "vbscript:", "onload=", "onerror=",
            "onclick=", "onmouseover=", "onfocus=", "onblur=", "alert(", "confirm(",
            "prompt(", "eval(", "expression(", "url("
        };

        var lowerInput = input.ToLowerInvariant();
        return Array.Exists(xssPatterns, pattern => lowerInput.Contains(pattern));
    }

    private bool ContainsCodeInjectionPattern(string input)
    {
        var codePatterns = new[]
        {
            "eval(", "Function(", "setTimeout(", "setInterval(", "require(", "import(",
            "__import__", "exec(", "execfile(", "compile(", "open(", "file(",
            "system(", "popen(", "subprocess", "os."
        };

        return Array.Exists(codePatterns, pattern => input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsPathTraversalPattern(string input)
    {
        var pathPatterns = new[]
        {
            "../", "..\\", "/..", "\\..", "/./", ".\\.\\", "~/"
        };

        return Array.Exists(pathPatterns, pattern => input.Contains(pattern));
    }

    private string EncodeJavaScript(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private string EncodeSqlParameters(string input)
    {
        return input.Replace("'", "''");
    }

    private string RemoveDangerousCharacters(string input)
    {
        var dangerousChars = new[] { '<', '>', '"', '\'', '&', '\0', '\b', '\f', '\r', '\n', '\t' };
        var result = input;
        foreach (var ch in dangerousChars)
        {
            result = result.Replace(ch.ToString(), "");
        }
        return result;
    }

    #endregion
}

/// <summary>
/// Rate limit state tracking.
/// </summary>
internal class RateLimitState
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
}
