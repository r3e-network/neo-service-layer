# Neo Service Layer - Component Interface Specifications

## Overview

This document defines the comprehensive interface specifications for all components within the Neo Service Layer architecture, ensuring consistency, maintainability, and clear contracts between system components.

## Interface Design Principles

### 1. Core Principles
- **Contract-First Design**: Interfaces define contracts before implementation
- **Separation of Concerns**: Each interface has a single, well-defined responsibility
- **Dependency Inversion**: Depend on abstractions, not concretions
- **Testability**: All interfaces support easy unit testing and mocking
- **Versioning**: Forward compatibility through interface evolution
- **Async-First**: Asynchronous operations by default for scalability

### 2. Common Patterns

#### 2.1 Result Pattern
```csharp
public class ServiceResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    
    public static ServiceResult<T> SuccessResult(T data, Dictionary<string, object>? metadata = null) 
        => new() { Success = true, Data = data, Metadata = metadata };
    
    public static ServiceResult<T> FailureResult(string errorMessage, string? errorCode = null) 
        => new() { Success = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
}
```

#### 2.2 Pagination Pattern
```csharp
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public string? NextCursor { get; init; }
    public string? PreviousCursor { get; init; }
    public bool HasNext { get; init; }
    public bool HasPrevious { get; init; }
    public long? TotalCount { get; init; }
}

public class PaginationRequest
{
    public string? Cursor { get; init; }
    public int Limit { get; init; } = 20;
    public string? OrderBy { get; init; }
    public bool Ascending { get; init; } = true;
}
```

## Core Service Interfaces

### 3. Authentication Services

#### 3.1 Authentication Service Interface
```csharp
public interface IAuthenticationService
{
    // User authentication
    Task<ServiceResult<AuthenticationResult>> AuthenticateAsync(
        AuthenticationRequest request, 
        CancellationToken cancellationToken = default);

    // Multi-factor authentication
    Task<ServiceResult<MfaResult>> InitiateMfaAsync(
        string userId, 
        MfaMethod method, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AuthenticationResult>> CompleteMfaAsync(
        string userId, 
        string mfaToken, 
        string code, 
        CancellationToken cancellationToken = default);

    // Token management
    Task<ServiceResult<TokenResult>> RefreshTokenAsync(
        string refreshToken, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> RevokeTokenAsync(
        string token, 
        CancellationToken cancellationToken = default);

    // Session management
    Task<ServiceResult<SessionInfo>> GetSessionInfoAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> InvalidateSessionAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> InvalidateAllUserSessionsAsync(
        string userId, 
        CancellationToken cancellationToken = default);
}

// Supporting types
public record AuthenticationRequest(
    string Username, 
    string Password, 
    string? DeviceId = null, 
    Dictionary<string, string>? Metadata = null);

public record AuthenticationResult(
    string UserId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    bool RequiresMfa,
    string? MfaToken = null,
    UserProfile? UserProfile = null);

public record MfaResult(
    string MfaToken,
    MfaMethod Method,
    string? Challenge = null,
    DateTime ExpiresAt = default);

public enum MfaMethod
{
    SMS,
    Email,
    TOTP,
    Push,
    Hardware
}
```

#### 3.2 User Management Service Interface
```csharp
public interface IUserManagementService
{
    // User lifecycle
    Task<ServiceResult<User>> CreateUserAsync(
        CreateUserRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> GetUserAsync(
        string userId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> GetUserByUsernameAsync(
        string username, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<User>> UpdateUserAsync(
        string userId, 
        UpdateUserRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteUserAsync(
        string userId, 
        CancellationToken cancellationToken = default);

    // User queries
    Task<ServiceResult<PagedResult<User>>> SearchUsersAsync(
        UserSearchRequest request, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default);

    // Role management
    Task<ServiceResult<bool>> AssignRoleAsync(
        string userId, 
        string roleId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> RemoveRoleAsync(
        string userId, 
        string roleId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<Role>>> GetUserRolesAsync(
        string userId, 
        CancellationToken cancellationToken = default);

    // Password management
    Task<ServiceResult<bool>> ChangePasswordAsync(
        string userId, 
        string currentPassword, 
        string newPassword, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> ResetPasswordAsync(
        string userId, 
        string resetToken, 
        string newPassword, 
        CancellationToken cancellationToken = default);
}
```

### 4. Blockchain Service Interfaces

#### 4.1 Blockchain Client Interface
```csharp
public interface IBlockchainClient
{
    // Network information
    string NetworkName { get; }
    BlockchainType BlockchainType { get; }
    bool IsConnected { get; }

    // Connection management
    Task<ServiceResult<ConnectionInfo>> ConnectAsync(
        ConnectionConfig config, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DisconnectAsync(
        CancellationToken cancellationToken = default);

    // Block operations
    Task<ServiceResult<Block>> GetBlockAsync(
        string blockHash, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Block>> GetBlockAsync(
        long blockNumber, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<Block>> GetLatestBlockAsync(
        CancellationToken cancellationToken = default);

    // Transaction operations
    Task<ServiceResult<Transaction>> GetTransactionAsync(
        string transactionHash, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TransactionReceipt>> SendTransactionAsync(
        TransactionRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<decimal>> EstimateGasAsync(
        TransactionRequest request, 
        CancellationToken cancellationToken = default);

    // Balance operations
    Task<ServiceResult<Balance>> GetBalanceAsync(
        string address, 
        string? asset = null, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<Balance>>> GetAllBalancesAsync(
        string address, 
        CancellationToken cancellationToken = default);

    // Event monitoring
    Task<ServiceResult<string>> SubscribeToBlocksAsync(
        Func<Block, Task> onBlock, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<string>> SubscribeToTransactionsAsync(
        string address, 
        Func<Transaction, Task> onTransaction, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> UnsubscribeAsync(
        string subscriptionId, 
        CancellationToken cancellationToken = default);
}

// Supporting types
public enum BlockchainType
{
    NeoN3,
    NeoX,
    Ethereum,
    Bitcoin
}

public record ConnectionConfig(
    string RpcEndpoint,
    string? WebSocketEndpoint = null,
    int TimeoutSeconds = 30,
    Dictionary<string, string>? Headers = null);

public record TransactionRequest(
    string FromAddress,
    string ToAddress,
    decimal Amount,
    string Asset,
    decimal? GasLimit = null,
    decimal? GasPrice = null,
    byte[]? Data = null);
```

#### 4.2 Smart Contract Service Interface
```csharp
public interface ISmartContractsService
{
    // Contract deployment
    Task<ServiceResult<DeploymentResult>> DeployContractAsync(
        DeployContractRequest request, 
        CancellationToken cancellationToken = default);

    // Contract interaction
    Task<ServiceResult<InvocationResult>> InvokeContractAsync(
        InvokeContractRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<object>> CallContractAsync(
        CallContractRequest request, 
        CancellationToken cancellationToken = default);

    // Contract queries
    Task<ServiceResult<SmartContract>> GetContractAsync(
        string contractAddress, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<SmartContract>>> GetUserContractsAsync(
        string userId, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default);

    // Event handling
    Task<ServiceResult<string>> SubscribeToContractEventsAsync(
        string contractAddress, 
        string? eventName, 
        Func<ContractEvent, Task> onEvent, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<ContractEvent>>> GetContractEventsAsync(
        string contractAddress, 
        EventFilter filter, 
        CancellationToken cancellationToken = default);

    // Contract management
    Task<ServiceResult<bool>> UpgradeContractAsync(
        string contractAddress, 
        UpgradeContractRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> PauseContractAsync(
        string contractAddress, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> ResumeContractAsync(
        string contractAddress, 
        CancellationToken cancellationToken = default);
}

public record DeployContractRequest(
    string UserId,
    string ContractCode,
    ContractManifest Manifest,
    decimal GasLimit,
    Dictionary<string, object>? Parameters = null);

public record InvokeContractRequest(
    string UserId,
    string ContractAddress,
    string Method,
    IReadOnlyList<ContractParameter> Parameters,
    decimal GasLimit,
    decimal? Amount = null);
```

### 5. Cryptographic Service Interfaces

#### 5.1 Key Management Service Interface
```csharp
public interface IKeyManagementService
{
    // Key lifecycle
    Task<ServiceResult<KeyInfo>> CreateKeyAsync(
        CreateKeyRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KeyInfo>> GetKeyAsync(
        string keyId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<KeyInfo>>> GetUserKeysAsync(
        string userId, 
        KeyFilter filter, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default);

    // Key operations
    Task<ServiceResult<SignatureResult>> SignAsync(
        string keyId, 
        byte[] data, 
        SignatureOptions? options = null, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> VerifySignatureAsync(
        string keyId, 
        byte[] data, 
        byte[] signature, 
        SignatureOptions? options = null, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<EncryptionResult>> EncryptAsync(
        string keyId, 
        byte[] plaintext, 
        EncryptionOptions? options = null, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<byte[]>> DecryptAsync(
        string keyId, 
        EncryptionResult encryptedData, 
        EncryptionOptions? options = null, 
        CancellationToken cancellationToken = default);

    // Key management
    Task<ServiceResult<KeyInfo>> RotateKeyAsync(
        string keyId, 
        RotateKeyRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> RevokeKeyAsync(
        string keyId, 
        RevokeKeyRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> EnableKeyAsync(
        string keyId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DisableKeyAsync(
        string keyId, 
        CancellationToken cancellationToken = default);

    // Key derivation
    Task<ServiceResult<KeyInfo>> DeriveKeyAsync(
        string parentKeyId, 
        string derivationPath, 
        CancellationToken cancellationToken = default);

    // Backup and recovery
    Task<ServiceResult<KeyBackup>> BackupKeyAsync(
        string keyId, 
        BackupOptions options, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<KeyInfo>> RestoreKeyAsync(
        KeyBackup backup, 
        RestoreOptions options, 
        CancellationToken cancellationToken = default);
}

public record CreateKeyRequest(
    string UserId,
    string KeyName,
    KeyType KeyType,
    KeyPurpose Purpose,
    KeyOptions? Options = null);

public record SignatureResult(
    byte[] Signature,
    string Algorithm,
    DateTime SignedAt,
    Dictionary<string, object>? Metadata = null);

public record EncryptionResult(
    byte[] Ciphertext,
    byte[] IV,
    string Algorithm,
    byte[]? Tag = null,
    Dictionary<string, object>? Metadata = null);

public enum KeyType
{
    Secp256k1,
    Ed25519,
    RSA2048,
    RSA4096,
    AES256
}

public enum KeyPurpose
{
    Signing,
    Encryption,
    Authentication,
    KeyDerivation
}
```

#### 5.2 Secrets Management Service Interface
```csharp
public interface ISecretsManagementService
{
    // Secret lifecycle
    Task<ServiceResult<SecretInfo>> CreateSecretAsync(
        CreateSecretRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<byte[]>> GetSecretValueAsync(
        string secretId, 
        int? version = null, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SecretInfo>> GetSecretInfoAsync(
        string secretId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SecretInfo>> UpdateSecretAsync(
        string secretId, 
        UpdateSecretRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteSecretAsync(
        string secretId, 
        CancellationToken cancellationToken = default);

    // Secret queries
    Task<ServiceResult<PagedResult<SecretInfo>>> ListSecretsAsync(
        string userId, 
        SecretFilter filter, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<SecretVersion>>> GetSecretVersionsAsync(
        string secretId, 
        CancellationToken cancellationToken = default);

    // Access control
    Task<ServiceResult<bool>> GrantAccessAsync(
        string secretId, 
        string userId, 
        SecretPermissions permissions, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> RevokeAccessAsync(
        string secretId, 
        string userId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<SecretAccessInfo>>> GetSecretAccessAsync(
        string secretId, 
        CancellationToken cancellationToken = default);

    // Secret rotation
    Task<ServiceResult<SecretInfo>> RotateSecretAsync(
        string secretId, 
        RotateSecretRequest request, 
        CancellationToken cancellationToken = default);

    // Backup and recovery
    Task<ServiceResult<SecretBackup>> BackupSecretAsync(
        string secretId, 
        BackupOptions options, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SecretInfo>> RestoreSecretAsync(
        SecretBackup backup, 
        RestoreOptions options, 
        CancellationToken cancellationToken = default);
}
```

### 6. Storage Service Interfaces

#### 6.1 Storage Service Interface
```csharp
public interface IStorageService
{
    // Basic operations
    Task<ServiceResult<StorageInfo>> StoreDataAsync(
        StoreDataRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<byte[]>> RetrieveDataAsync(
        string dataId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteDataAsync(
        string dataId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DataExistsAsync(
        string dataId, 
        CancellationToken cancellationToken = default);

    // Metadata operations
    Task<ServiceResult<StorageMetadata>> GetMetadataAsync(
        string dataId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<StorageMetadata>> UpdateMetadataAsync(
        string dataId, 
        UpdateMetadataRequest request, 
        CancellationToken cancellationToken = default);

    // Query operations
    Task<ServiceResult<PagedResult<StorageInfo>>> ListUserDataAsync(
        string userId, 
        DataFilter filter, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<StorageInfo>>> SearchDataAsync(
        DataSearchRequest request, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default);

    // Batch operations
    Task<ServiceResult<IReadOnlyList<StorageInfo>>> StoreBatchAsync(
        IReadOnlyList<StoreDataRequest> requests, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<byte[]>>> RetrieveBatchAsync(
        IReadOnlyList<string> dataIds, 
        CancellationToken cancellationToken = default);

    // Storage analytics
    Task<ServiceResult<StorageUsage>> GetStorageUsageAsync(
        string userId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<StorageMetrics>>> GetStorageMetricsAsync(
        string userId, 
        TimeRange timeRange, 
        CancellationToken cancellationToken = default);
}

public record StoreDataRequest(
    string UserId,
    string Key,
    byte[] Data,
    bool Encrypt = true,
    TimeSpan? TTL = null,
    Dictionary<string, string>? Metadata = null,
    string? ContentType = null);

public record StorageInfo(
    string DataId,
    string Key,
    string UserId,
    long Size,
    bool IsEncrypted,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string? ContentType,
    Dictionary<string, string>? Metadata);
```

#### 6.2 Backup Service Interface
```csharp
public interface IBackupService
{
    // Backup operations
    Task<ServiceResult<BackupInfo>> CreateBackupAsync(
        CreateBackupRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BackupInfo>> GetBackupAsync(
        string backupId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<BackupInfo>>> ListBackupsAsync(
        string userId, 
        BackupFilter filter, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default);

    // Restore operations
    Task<ServiceResult<RestoreInfo>> RestoreFromBackupAsync(
        string backupId, 
        RestoreOptions options, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<RestoreInfo>> GetRestoreStatusAsync(
        string restoreId, 
        CancellationToken cancellationToken = default);

    // Backup management
    Task<ServiceResult<bool>> DeleteBackupAsync(
        string backupId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BackupInfo>> UpdateBackupAsync(
        string backupId, 
        UpdateBackupRequest request, 
        CancellationToken cancellationToken = default);

    // Automated backup
    Task<ServiceResult<BackupSchedule>> CreateBackupScheduleAsync(
        CreateBackupScheduleRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BackupSchedule>> UpdateBackupScheduleAsync(
        string scheduleId, 
        UpdateBackupScheduleRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteBackupScheduleAsync(
        string scheduleId, 
        CancellationToken cancellationToken = default);

    // Backup verification
    Task<ServiceResult<VerificationResult>> VerifyBackupIntegrityAsync(
        string backupId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<BackupMetrics>>> GetBackupMetricsAsync(
        string userId, 
        TimeRange timeRange, 
        CancellationToken cancellationToken = default);
}
```

### 7. AI and Analytics Service Interfaces

#### 7.1 Pattern Recognition Service Interface
```csharp
public interface IPatternRecognitionService
{
    // Pattern analysis
    Task<ServiceResult<PatternAnalysisResult>> AnalyzePatternAsync(
        PatternAnalysisRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AnomalyDetectionResult>> DetectAnomaliesAsync(
        AnomalyDetectionRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrendAnalysisResult>> AnalyzeTrendsAsync(
        TrendAnalysisRequest request, 
        CancellationToken cancellationToken = default);

    // Model management
    Task<ServiceResult<ModelInfo>> TrainModelAsync(
        TrainModelRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ModelInfo>> GetModelAsync(
        string modelId, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteModelAsync(
        string modelId, 
        CancellationToken cancellationToken = default);

    // Batch processing
    Task<ServiceResult<BatchAnalysisResult>> AnalyzeBatchAsync(
        BatchAnalysisRequest request, 
        CancellationToken cancellationToken = default);

    // Real-time monitoring
    Task<ServiceResult<string>> StartMonitoringAsync(
        MonitoringConfig config, 
        Func<PatternAlert, Task> onAlert, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> StopMonitoringAsync(
        string monitoringId, 
        CancellationToken cancellationToken = default);

    // Analytics
    Task<ServiceResult<PatternStatistics>> GetPatternStatisticsAsync(
        string userId, 
        TimeRange timeRange, 
        CancellationToken cancellationToken = default);
}
```

#### 7.2 Prediction Service Interface
```csharp
public interface IPredictionService
{
    // Predictions
    Task<ServiceResult<PredictionResult>> MakePredictionAsync(
        PredictionRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<PredictionResult>>> MakeBatchPredictionAsync(
        BatchPredictionRequest request, 
        CancellationToken cancellationToken = default);

    // Model management
    Task<ServiceResult<PredictionModel>> CreateModelAsync(
        CreatePredictionModelRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PredictionModel>> UpdateModelAsync(
        string modelId, 
        UpdatePredictionModelRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteModelAsync(
        string modelId, 
        CancellationToken cancellationToken = default);

    // Model evaluation
    Task<ServiceResult<ModelEvaluationResult>> EvaluateModelAsync(
        string modelId, 
        EvaluationDataset dataset, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ModelMetrics>> GetModelMetricsAsync(
        string modelId, 
        TimeRange timeRange, 
        CancellationToken cancellationToken = default);

    // Feature engineering
    Task<ServiceResult<FeatureSet>> ExtractFeaturesAsync(
        FeatureExtractionRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FeatureImportance>> GetFeatureImportanceAsync(
        string modelId, 
        CancellationToken cancellationToken = default);
}
```

### 8. Monitoring and Observability Interfaces

#### 8.1 Monitoring Service Interface
```csharp
public interface IMonitoringService
{
    // Metrics collection
    Task<ServiceResult<bool>> RecordMetricAsync(
        MetricRecord metric, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> RecordMetricsAsync(
        IReadOnlyList<MetricRecord> metrics, 
        CancellationToken cancellationToken = default);

    // Metrics queries
    Task<ServiceResult<IReadOnlyList<MetricValue>>> GetMetricsAsync(
        MetricQuery query, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<MetricSummary>> GetMetricSummaryAsync(
        string metricName, 
        TimeRange timeRange, 
        AggregationType aggregation = AggregationType.Average, 
        CancellationToken cancellationToken = default);

    // Health monitoring
    Task<ServiceResult<HealthStatus>> GetHealthStatusAsync(
        string serviceName, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<HealthStatus>>> GetAllHealthStatusesAsync(
        CancellationToken cancellationToken = default);

    // Alerting
    Task<ServiceResult<AlertRule>> CreateAlertRuleAsync(
        CreateAlertRuleRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> UpdateAlertRuleAsync(
        string ruleId, 
        UpdateAlertRuleRequest request, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAlertRuleAsync(
        string ruleId, 
        CancellationToken cancellationToken = default);

    // Dashboard data
    Task<ServiceResult<DashboardData>> GetDashboardDataAsync(
        string dashboardId, 
        TimeRange timeRange, 
        CancellationToken cancellationToken = default);
}

public record MetricRecord(
    string Name,
    double Value,
    DateTime Timestamp,
    Dictionary<string, string>? Tags = null,
    string? Unit = null);

public record MetricQuery(
    string MetricName,
    TimeRange TimeRange,
    Dictionary<string, string>? Tags = null,
    AggregationType? Aggregation = null,
    TimeSpan? Interval = null);

public enum AggregationType
{
    Average,
    Sum,
    Min,
    Max,
    Count,
    Percentile95,
    Percentile99
}
```

#### 8.2 Health Check Service Interface
```csharp
public interface IHealthCheckService
{
    // Health checks
    Task<ServiceResult<HealthCheckResult>> CheckHealthAsync(
        string serviceName, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<HealthCheckResult>>> CheckAllHealthAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResult<DetailedHealthReport>> GetDetailedHealthReportAsync(
        string serviceName, 
        CancellationToken cancellationToken = default);

    // Health check registration
    Task<ServiceResult<bool>> RegisterHealthCheckAsync(
        HealthCheckRegistration registration, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> UnregisterHealthCheckAsync(
        string healthCheckId, 
        CancellationToken cancellationToken = default);

    // Health monitoring
    Task<ServiceResult<string>> StartHealthMonitoringAsync(
        HealthMonitoringConfig config, 
        Func<HealthChangeEvent, Task> onHealthChange, 
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> StopHealthMonitoringAsync(
        string monitoringId, 
        CancellationToken cancellationToken = default);

    // Health history
    Task<ServiceResult<IReadOnlyList<HealthHistoryEntry>>> GetHealthHistoryAsync(
        string serviceName, 
        TimeRange timeRange, 
        CancellationToken cancellationToken = default);
}

public record HealthCheckResult(
    string ServiceName,
    HealthStatus Status,
    string? Description = null,
    TimeSpan ResponseTime = default,
    DateTime CheckedAt = default,
    Dictionary<string, object>? Data = null);

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
```

## Interface Evolution and Versioning

### 9. Versioning Strategy

#### 9.1 Interface Versioning
```csharp
// Version 1 interface
public interface IUserService
{
    Task<User> GetUserAsync(string userId);
}

// Version 2 interface with backward compatibility
public interface IUserServiceV2 : IUserService
{
    Task<User> GetUserAsync(string userId, UserFetchOptions? options = null);
    Task<UserProfile> GetUserProfileAsync(string userId);
}

// Version 3 interface with async patterns
public interface IUserServiceV3 : IUserServiceV2
{
    Task<ServiceResult<User>> GetUserAsyncV3(string userId, UserFetchOptions? options = null);
    Task<ServiceResult<UserProfile>> GetUserProfileAsyncV3(string userId);
}
```

#### 9.2 Migration Patterns
```csharp
public class ServiceMigrationHelper
{
    public static async Task<ServiceResult<T>> MigrateFromLegacyAsync<T>(
        Func<Task<T>> legacyOperation, 
        string operationName)
    {
        try
        {
            var result = await legacyOperation();
            return ServiceResult<T>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.FailureResult(
                $"Legacy operation '{operationName}' failed: {ex.Message}", 
                "LEGACY_OPERATION_FAILED");
        }
    }
}
```

### 10. Testing Interfaces

#### 10.1 Test Interface Patterns
```csharp
public interface ITestableService
{
    // Normal operations
    Task<ServiceResult<T>> PerformOperationAsync<T>(OperationRequest request);
    
    // Test-specific operations
    Task<ServiceResult<TestResult>> ExecuteTestScenarioAsync(TestScenario scenario);
    Task<ServiceResult<bool>> ResetToTestStateAsync(string testStateId);
    Task<ServiceResult<TestMetrics>> GetTestMetricsAsync();
}

// Test doubles
public interface IMockableExternalService
{
    bool IsMocked { get; }
    Task<ServiceResult<T>> CallExternalServiceAsync<T>(ExternalServiceRequest request);
    void SetMockBehavior(MockBehavior behavior);
}
```

This comprehensive interface specification ensures consistent, maintainable, and testable components throughout the Neo Service Layer architecture.