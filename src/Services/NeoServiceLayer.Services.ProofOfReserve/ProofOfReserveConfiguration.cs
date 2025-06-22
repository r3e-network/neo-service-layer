using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Configuration settings for the Proof of Reserve Service.
/// </summary>
public class ProofOfReserveConfiguration
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ProofOfReserve";

    /// <summary>
    /// Gets or sets the monitoring settings.
    /// </summary>
    public MonitoringSettings Monitoring { get; set; } = new();

    /// <summary>
    /// Gets or sets the resilience settings.
    /// </summary>
    public ResilienceSettings Resilience { get; set; } = new();

    /// <summary>
    /// Gets or sets the storage settings.
    /// </summary>
    public StorageSettings Storage { get; set; } = new();

    /// <summary>
    /// Gets or sets the cryptographic settings.
    /// </summary>
    public CryptographicSettings Cryptographic { get; set; } = new();

    /// <summary>
    /// Gets or sets the alert settings.
    /// </summary>
    public AlertSettings Alerts { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance settings.
    /// </summary>
    public PerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets environment-specific settings.
    /// </summary>
    public EnvironmentSettings Environment { get; set; } = new();

    /// <summary>
    /// Gets or sets security settings.
    /// </summary>
    public SecuritySettings Security { get; set; } = new();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>Validation results.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();

        // Validate this instance
        Validator.TryValidateObject(this, context, results, true);

        // Validate nested objects
        results.AddRange(Monitoring.Validate());
        results.AddRange(Resilience.Validate());
        results.AddRange(Storage.Validate());
        results.AddRange(Cryptographic.Validate());
        results.AddRange(Alerts.Validate());
        results.AddRange(Performance.Validate());
        results.AddRange(Environment.Validate());
        results.AddRange(Security.Validate());

        return results;
    }
}

/// <summary>
/// Monitoring configuration settings.
/// </summary>
public class MonitoringSettings
{
    /// <summary>
    /// Gets or sets the monitoring interval in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Monitoring interval must be between 1 and 1440 minutes")]
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of snapshots to retain per asset.
    /// </summary>
    [Range(100, 10000, ErrorMessage = "Snapshot retention must be between 100 and 10000")]
    public int MaxSnapshotsPerAsset { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of results to retain.
    /// </summary>
    [Range(1000, 100000, ErrorMessage = "Result retention must be between 1000 and 100000")]
    public int MaxResultRetention { get; set; } = 10000;

    /// <summary>
    /// Gets or sets whether automatic monitoring is enabled.
    /// </summary>
    public bool AutoMonitoringEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the blockchain query timeout in seconds.
    /// </summary>
    [Range(5, 300, ErrorMessage = "Blockchain query timeout must be between 5 and 300 seconds")]
    public int BlockchainQueryTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Validates the monitoring settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);
        return results;
    }
}

/// <summary>
/// Resilience configuration settings.
/// </summary>
public class ResilienceSettings
{
    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max retries must be between 1 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff in milliseconds.
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Base delay must be between 50 and 5000 milliseconds")]
    public int BaseDelayMs { get; set; } = 200;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// </summary>
    [Range(3, 20, ErrorMessage = "Circuit breaker failure threshold must be between 3 and 20")]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker timeout in minutes.
    /// </summary>
    [Range(1, 30, ErrorMessage = "Circuit breaker timeout must be between 1 and 30 minutes")]
    public int CircuitBreakerTimeoutMinutes { get; set; } = 1;

    /// <summary>
    /// Gets or sets the operation timeout in seconds.
    /// </summary>
    [Range(10, 300, ErrorMessage = "Operation timeout must be between 10 and 300 seconds")]
    public int OperationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether resilience patterns are enabled.
    /// </summary>
    public bool ResiliencePatternsEnabled { get; set; } = true;

    /// <summary>
    /// Validates the resilience settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);
        return results;
    }
}

/// <summary>
/// Storage configuration settings.
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// Gets or sets the proof retention period in days.
    /// </summary>
    [Range(30, 3650, ErrorMessage = "Proof retention must be between 30 and 3650 days")]
    public int ProofRetentionDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets the snapshot retention period in days.
    /// </summary>
    [Range(7, 365, ErrorMessage = "Snapshot retention must be between 7 and 365 days")]
    public int SnapshotRetentionDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets the storage encryption algorithm.
    /// </summary>
    [Required(ErrorMessage = "Storage encryption algorithm is required")]
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Gets or sets whether storage compression is enabled.
    /// </summary>
    public bool CompressionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the storage provider type.
    /// </summary>
    [Required(ErrorMessage = "Storage provider is required")]
    public string StorageProvider { get; set; } = "Enclave";

    /// <summary>
    /// Gets or sets the maximum storage size per asset in MB.
    /// </summary>
    [Range(10, 10240, ErrorMessage = "Maximum storage size must be between 10 and 10240 MB")]
    public int MaxStorageSizeMB { get; set; } = 1024;

    /// <summary>
    /// Validates the storage settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);

        // Additional validation
        var validAlgorithms = new[] { "AES-256-GCM", "AES-256-CBC", "ChaCha20-Poly1305" };
        if (!validAlgorithms.Contains(EncryptionAlgorithm))
        {
            results.Add(new ValidationResult($"Encryption algorithm must be one of: {string.Join(", ", validAlgorithms)}"));
        }

        var validProviders = new[] { "Enclave", "Database", "FileSystem", "Cloud" };
        if (!validProviders.Contains(StorageProvider))
        {
            results.Add(new ValidationResult($"Storage provider must be one of: {string.Join(", ", validProviders)}"));
        }

        return results;
    }
}

/// <summary>
/// Cryptographic configuration settings.
/// </summary>
public class CryptographicSettings
{
    /// <summary>
    /// Gets or sets the signature algorithm.
    /// </summary>
    [Required(ErrorMessage = "Signature algorithm is required")]
    public string SignatureAlgorithm { get; set; } = "ECDSA";

    /// <summary>
    /// Gets or sets the hash algorithm.
    /// </summary>
    [Required(ErrorMessage = "Hash algorithm is required")]
    public string HashAlgorithm { get; set; } = "SHA256";

    /// <summary>
    /// Gets or sets the key size in bits.
    /// </summary>
    [Range(256, 4096, ErrorMessage = "Key size must be between 256 and 4096 bits")]
    public int KeySizeBits { get; set; } = 256;

    /// <summary>
    /// Gets or sets the curve type for ECDSA.
    /// </summary>
    public string EcdsaCurve { get; set; } = "secp256r1";

    /// <summary>
    /// Gets or sets whether hardware security modules are required.
    /// </summary>
    public bool RequireHSM { get; set; } = true;

    /// <summary>
    /// Gets or sets the key rotation interval in days.
    /// </summary>
    [Range(30, 365, ErrorMessage = "Key rotation interval must be between 30 and 365 days")]
    public int KeyRotationDays { get; set; } = 90;

    /// <summary>
    /// Validates the cryptographic settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);

        // Additional validation
        var validSignatureAlgorithms = new[] { "ECDSA", "RSA", "EdDSA" };
        if (!validSignatureAlgorithms.Contains(SignatureAlgorithm))
        {
            results.Add(new ValidationResult($"Signature algorithm must be one of: {string.Join(", ", validSignatureAlgorithms)}"));
        }

        var validHashAlgorithms = new[] { "SHA256", "SHA384", "SHA512", "Blake2b" };
        if (!validHashAlgorithms.Contains(HashAlgorithm))
        {
            results.Add(new ValidationResult($"Hash algorithm must be one of: {string.Join(", ", validHashAlgorithms)}"));
        }

        var validCurves = new[] { "secp256r1", "secp384r1", "secp521r1", "curve25519" };
        if (SignatureAlgorithm == "ECDSA" && !validCurves.Contains(EcdsaCurve))
        {
            results.Add(new ValidationResult($"ECDSA curve must be one of: {string.Join(", ", validCurves)}"));
        }

        return results;
    }
}

/// <summary>
/// Alert configuration settings.
/// </summary>
public class AlertSettings
{
    /// <summary>
    /// Gets or sets whether alerts are enabled.
    /// </summary>
    public bool AlertsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default alert threshold.
    /// </summary>
    [Range(0.1, 2.0, ErrorMessage = "Default alert threshold must be between 0.1 and 2.0")]
    public decimal DefaultThreshold { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the alert check interval in minutes.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Alert check interval must be between 1 and 60 minutes")]
    public int CheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum alerts per hour per asset.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max alerts per hour must be between 1 and 100")]
    public int MaxAlertsPerHour { get; set; } = 10;

    /// <summary>
    /// Gets or sets the default notification methods.
    /// </summary>
    public List<string> DefaultNotificationMethods { get; set; } = new() { "Email", "Webhook" };

    /// <summary>
    /// Gets or sets the alert severity escalation rules.
    /// </summary>
    public Dictionary<string, int> SeverityEscalationMinutes { get; set; } = new()
    {
        ["Warning"] = 30,
        ["Critical"] = 5,
        ["Emergency"] = 1
    };

    /// <summary>
    /// Validates the alert settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);

        // Additional validation
        var validMethods = new[] { "Email", "SMS", "Webhook", "Slack", "Discord", "PushNotification" };
        foreach (var method in DefaultNotificationMethods)
        {
            if (!validMethods.Contains(method))
            {
                results.Add(new ValidationResult($"Notification method '{method}' is not valid. Must be one of: {string.Join(", ", validMethods)}"));
            }
        }

        return results;
    }
}

/// <summary>
/// Performance configuration settings.
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Gets or sets the maximum concurrent operations.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max concurrent operations must be between 1 and 100")]
    public int MaxConcurrentOperations { get; set; } = 10;

    /// <summary>
    /// Gets or sets the batch size for processing operations.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Batch size must be between 1 and 1000")]
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    public bool CachingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache expiration time in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Cache expiration must be between 1 and 1440 minutes")]
    public int CacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum cache size in MB.
    /// </summary>
    [Range(10, 1024, ErrorMessage = "Max cache size must be between 10 and 1024 MB")]
    public int MaxCacheSizeMB { get; set; } = 256;

    /// <summary>
    /// Gets or sets whether performance metrics are enabled.
    /// </summary>
    public bool MetricsEnabled { get; set; } = true;

    /// <summary>
    /// Validates the performance settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);
        return results;
    }
}

/// <summary>
/// Environment-specific configuration settings.
/// </summary>
public class EnvironmentSettings
{
    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    [Required(ErrorMessage = "Environment name is required")]
    public string Name { get; set; } = "Development";

    /// <summary>
    /// Gets or sets whether this is a production environment.
    /// </summary>
    public bool IsProduction { get; set; } = false;

    /// <summary>
    /// Gets or sets whether debug logging is enabled.
    /// </summary>
    public bool DebugLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    [Required(ErrorMessage = "Log level is required")]
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets environment-specific feature flags.
    /// </summary>
    public Dictionary<string, bool> FeatureFlags { get; set; } = new()
    {
        ["EnableAdvancedAnalytics"] = false,
        ["EnableExperimentalFeatures"] = false,
        ["EnableDetailedMetrics"] = true,
        ["EnableAutomaticRecovery"] = true
    };

    /// <summary>
    /// Gets or sets the blockchain network configurations.
    /// </summary>
    public Dictionary<string, string> BlockchainNetworks { get; set; } = new()
    {
        ["NeoN3"] = "MainNet",
        ["NeoX"] = "MainNet"
    };

    /// <summary>
    /// Validates the environment settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);

        // Additional validation
        var validEnvironments = new[] { "Development", "Testing", "Staging", "Production" };
        if (!validEnvironments.Contains(Name))
        {
            results.Add(new ValidationResult($"Environment name must be one of: {string.Join(", ", validEnvironments)}"));
        }

        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        if (!validLogLevels.Contains(LogLevel))
        {
            results.Add(new ValidationResult($"Log level must be one of: {string.Join(", ", validLogLevels)}"));
        }

        // Validate blockchain networks
        var validNetworks = new[] { "MainNet", "TestNet", "PrivateNet", "LocalNet" };
        foreach (var network in BlockchainNetworks.Values)
        {
            if (!validNetworks.Contains(network))
            {
                results.Add(new ValidationResult($"Blockchain network '{network}' is not valid. Must be one of: {string.Join(", ", validNetworks)}"));
            }
        }

        return results;
    }
}

/// <summary>
/// Security configuration settings.
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Gets or sets whether security features are enabled.
    /// </summary>
    public bool SecurityEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether rate limiting is enabled.
    /// </summary>
    public bool RateLimitingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether CSRF protection is enabled.
    /// </summary>
    public bool CsrfProtectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether request signature validation is required.
    /// </summary>
    public bool RequireRequestSignature { get; set; } = false;

    /// <summary>
    /// Gets or sets whether session management is enabled.
    /// </summary>
    public bool SessionManagementEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the session timeout in minutes.
    /// </summary>
    [Range(5, 1440, ErrorMessage = "Session timeout must be between 5 and 1440 minutes")]
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours

    /// <summary>
    /// Gets or sets the CSRF token expiration in minutes.
    /// </summary>
    [Range(1, 60, ErrorMessage = "CSRF token expiration must be between 1 and 60 minutes")]
    public int CsrfTokenExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether IP address blocking is enabled.
    /// </summary>
    public bool IpBlockingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum failed authentication attempts before blocking.
    /// </summary>
    [Range(3, 20, ErrorMessage = "Max failed auth attempts must be between 3 and 20")]
    public int MaxFailedAuthAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the authentication attempt window in minutes.
    /// </summary>
    [Range(5, 60, ErrorMessage = "Auth attempt window must be between 5 and 60 minutes")]
    public int AuthAttemptWindowMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets whether timing attack detection is enabled.
    /// </summary>
    public bool TimingAttackDetectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum operation interval in milliseconds.
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Min operation interval must be between 50 and 5000 milliseconds")]
    public int MinOperationIntervalMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether input sanitization is enabled.
    /// </summary>
    public bool InputSanitizationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether detailed security logging is enabled.
    /// </summary>
    public bool DetailedSecurityLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets rate limiting configurations by operation type.
    /// </summary>
    public Dictionary<string, RateLimitConfiguration> RateLimits { get; set; } = new()
    {
        ["ReadOperations"] = new() { RequestLimit = 100, TimeWindowMinutes = 1 },
        ["WriteOperations"] = new() { RequestLimit = 20, TimeWindowMinutes = 1 },
        ["ProofGeneration"] = new() { RequestLimit = 5, TimeWindowMinutes = 1 },
        ["AssetRegistration"] = new() { RequestLimit = 10, TimeWindowMinutes = 60 },
        ["AuditReports"] = new() { RequestLimit = 3, TimeWindowMinutes = 5 },
        ["Authentication"] = new() { RequestLimit = 5, TimeWindowMinutes = 15 }
    };

    /// <summary>
    /// Gets or sets trusted IP address ranges.
    /// </summary>
    public List<string> TrustedIpRanges { get; set; } = new()
    {
        "127.0.0.1/32",
        "::1/128",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16"
    };

    /// <summary>
    /// Gets or sets the security event retention period in days.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Security event retention must be between 1 and 365 days")]
    public int SecurityEventRetentionDays { get; set; } = 30;

    /// <summary>
    /// Validates the security settings.
    /// </summary>
    public IEnumerable<ValidationResult> Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, results, true);

        // Additional validation
        foreach (var rateLimit in RateLimits.Values)
        {
            if (rateLimit.RequestLimit <= 0)
            {
                results.Add(new ValidationResult("Rate limit request limit must be greater than 0"));
            }
            if (rateLimit.TimeWindowMinutes <= 0)
            {
                results.Add(new ValidationResult("Rate limit time window must be greater than 0"));
            }
        }

        return results;
    }
}

/// <summary>
/// Rate limit configuration for specific operations.
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Gets or sets the request limit within the time window.
    /// </summary>
    public int RequestLimit { get; set; }

    /// <summary>
    /// Gets or sets the time window in minutes.
    /// </summary>
    public int TimeWindowMinutes { get; set; }
}
