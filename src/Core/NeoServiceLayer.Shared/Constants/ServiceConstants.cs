namespace NeoServiceLayer.Shared.Constants;

/// <summary>
/// Global constants used across the Neo Service Layer.
/// </summary>
public static class ServiceConstants
{
    /// <summary>
    /// Application-wide constants.
    /// </summary>
    public static class Application
    {
        public const string Name = "NeoServiceLayer";
        public const string Version = "1.0.0";
        public const string ApiVersion = "v1";
        public const string DefaultCulture = "en-US";
        public const string DefaultTimeZone = "UTC";
    }

    /// <summary>
    /// HTTP-related constants.
    /// </summary>
    public static class Http
    {
        public const string CorrelationIdHeader = "X-Correlation-ID";
        public const string UserIdHeader = "X-User-ID";
        public const string TraceIdHeader = "X-Trace-ID";
        public const string RequestIdHeader = "X-Request-ID";
        public const string ApiKeyHeader = "X-API-Key";
        public const string ContentTypeJson = "application/json";
        public const string ContentTypeXml = "application/xml";
        public const string BearerScheme = "Bearer";
    }

    /// <summary>
    /// Authentication and authorization constants.
    /// </summary>
    public static class Security
    {
        public const string DefaultScheme = "Bearer";
        public const string ApiKeyScheme = "ApiKey";
        public const string JwtClaimUserId = "user_id";
        public const string JwtClaimUserName = "user_name";
        public const string JwtClaimRoles = "roles";
        public const string JwtClaimPermissions = "permissions";
        public const string JwtClaimTenantId = "tenant_id";
        
        // Role names
        public const string AdminRole = "Admin";
        public const string UserRole = "User";
        public const string ServiceRole = "Service";
        public const string AuditorRole = "Auditor";
        public const string KeyManagerRole = "KeyManager";
        public const string AnalystRole = "Analyst";
    }

    /// <summary>
    /// Cache-related constants.
    /// </summary>
    public static class Cache
    {
        public const string DefaultPrefix = "nsl:";
        public const string UserPrefix = "user:";
        public const string KeyPrefix = "key:";
        public const string BlockchainPrefix = "blockchain:";
        public const string HealthPrefix = "health:";
        public const string MetricsPrefix = "metrics:";
        
        // TTL in seconds
        public const int ShortTtl = 300;    // 5 minutes
        public const int MediumTtl = 1800;  // 30 minutes
        public const int LongTtl = 3600;    // 1 hour
        public const int ExtendedTtl = 86400; // 24 hours
    }

    /// <summary>
    /// Blockchain-related constants.
    /// </summary>
    public static class Blockchain
    {
        public const string NeoN3 = "neo-n3";
        public const string NeoX = "neo-x";
        public const string Bitcoin = "bitcoin";
        public const string Ethereum = "ethereum";
        
        // Transaction statuses
        public const string TxStatusPending = "pending";
        public const string TxStatusConfirmed = "confirmed";
        public const string TxStatusFailed = "failed";
        public const string TxStatusCancelled = "cancelled";
        
        // Gas settings
        public const decimal DefaultGasPrice = 0.00000001m;
        public const long DefaultGasLimit = 100000000L;
    }

    /// <summary>
    /// Configuration section names.
    /// </summary>
    public static class Configuration
    {
        public const string ConnectionStrings = "ConnectionStrings";
        public const string Database = "Database";
        public const string Redis = "Redis";
        public const string RabbitMQ = "RabbitMQ";
        public const string Blockchain = "Blockchain";
        public const string Security = "Security";
        public const string Logging = "Logging";
        public const string HealthChecks = "HealthChecks";
        public const string Monitoring = "Monitoring";
        public const string Features = "Features";
        public const string Services = "Services";
        public const string Enclave = "Enclave";
    }

    /// <summary>
    /// Event names for logging and telemetry.
    /// </summary>
    public static class Events
    {
        public const string ServiceStarted = "ServiceStarted";
        public const string ServiceStopped = "ServiceStopped";
        public const string ServiceFailed = "ServiceFailed";
        public const string RequestProcessed = "RequestProcessed";
        public const string RequestFailed = "RequestFailed";
        public const string SecurityViolation = "SecurityViolation";
        public const string PerformanceWarning = "PerformanceWarning";
        public const string ResourceExhaustion = "ResourceExhaustion";
        public const string DataCorruption = "DataCorruption";
        public const string ExternalServiceFailure = "ExternalServiceFailure";
    }

    /// <summary>
    /// Error codes used across the application.
    /// </summary>
    public static class ErrorCodes
    {
        // General errors
        public const string ValidationError = "VALIDATION_ERROR";
        public const string NotFound = "NOT_FOUND";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string InternalError = "INTERNAL_ERROR";
        public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
        
        // Blockchain errors
        public const string BlockchainConnectionError = "BLOCKCHAIN_CONNECTION_ERROR";
        public const string TransactionFailed = "TRANSACTION_FAILED";
        public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
        public const string InvalidAddress = "INVALID_ADDRESS";
        public const string ContractExecutionError = "CONTRACT_EXECUTION_ERROR";
        
        // Key management errors
        public const string KeyNotFound = "KEY_NOT_FOUND";
        public const string KeyGenerationFailed = "KEY_GENERATION_FAILED";
        public const string SigningFailed = "SIGNING_FAILED";
        public const string VerificationFailed = "VERIFICATION_FAILED";
        public const string EncryptionFailed = "ENCRYPTION_FAILED";
        public const string DecryptionFailed = "DECRYPTION_FAILED";
        
        // Service errors
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
        public const string ServiceTimeout = "SERVICE_TIMEOUT";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string QuotaExceeded = "QUOTA_EXCEEDED";
        public const string ConfigurationError = "CONFIGURATION_ERROR";
    }

    /// <summary>
    /// Metric names for monitoring and telemetry.
    /// </summary>
    public static class Metrics
    {
        public const string RequestsTotal = "requests_total";
        public const string RequestDuration = "request_duration_seconds";
        public const string ErrorsTotal = "errors_total";
        public const string ActiveConnections = "active_connections";
        public const string QueueLength = "queue_length";
        public const string CacheHitRate = "cache_hit_rate";
        public const string DatabaseConnections = "database_connections";
        public const string MemoryUsage = "memory_usage_bytes";
        public const string CpuUsage = "cpu_usage_percent";
        public const string DiskUsage = "disk_usage_bytes";
    }

    /// <summary>
    /// Default configuration values.
    /// </summary>
    public static class Defaults
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int DefaultTimeout = 30; // seconds
        public const int DefaultRetryCount = 3;
        public const int DefaultBatchSize = 100;
        public const string DefaultEncoding = "UTF-8";
        public const string DefaultDateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    }

    /// <summary>
    /// Regular expression patterns for validation.
    /// </summary>
    public static class Patterns
    {
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const string Url = @"^https?://[^\s/$.?#].[^\s]*$";
        public const string HexString = @"^[0-9a-fA-F]+$";
        public const string Base64 = @"^[A-Za-z0-9+/]*={0,2}$";
        public const string NeoAddress = @"^[AN][0-9A-Za-z]{33}$";
        public const string EthereumAddress = @"^0x[a-fA-F0-9]{40}$";
        public const string TransactionHash = @"^0x[a-fA-F0-9]{64}$";
        public const string PublicKey = @"^[0-9a-fA-F]{66}$";
        public const string PrivateKey = @"^[0-9a-fA-F]{64}$";
    }
} 