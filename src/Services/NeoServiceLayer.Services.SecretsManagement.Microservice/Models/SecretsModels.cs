using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Models;

// Core Secret Management Models
public class Secret
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SecretType Type { get; set; }
    public string EncryptedValue { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty; // Reference to encryption key
    public string CreatedBy { get; set; } = string.Empty;
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public SecretStatus Status { get; set; } = SecretStatus.Active;
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int Version { get; set; } = 1;
    
    // Navigation properties
    public List<SecretVersion> Versions { get; set; } = new();
    public List<SecretAccess> AccessHistory { get; set; } = new();
    public List<SecretShare> Shares { get; set; } = new();
}

public class SecretVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SecretId { get; set; }
    public int Version { get; set; }
    public string EncryptedValue { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ChangeReason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation property
    public Secret Secret { get; set; } = null!;
}

public class EncryptionKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string KeyId { get; set; } = string.Empty; // External key identifier
    public string Name { get; set; } = string.Empty;
    public KeyType Type { get; set; }
    public KeyStatus Status { get; set; } = KeyStatus.Active;
    public string Algorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public string? HsmSlotId { get; set; } // Hardware Security Module slot
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastRotatedAt { get; set; }
    public int RotationIntervalDays { get; set; } = 90;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SecretAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SecretId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public SecretOperation Operation { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public string ClientIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    
    // Navigation property
    public Secret Secret { get; set; } = null!;
}

public class SecretShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SecretId { get; set; }
    public string SharedWithUserId { get; set; } = string.Empty;
    public string SharedByUserId { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public SecretShareStatus Status { get; set; } = SecretShareStatus.Active;
    public string? ShareToken { get; set; } // For temporary sharing
    
    // Navigation property
    public Secret Secret { get; set; } = null!;
}

public class SecretPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PathPattern { get; set; } = string.Empty; // Regex pattern for secret paths
    public List<string> AllowedUsers { get; set; } = new();
    public List<string> AllowedServices { get; set; } = new();
    public List<string> AllowedOperations { get; set; } = new();
    public Dictionary<string, object> Conditions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

public class RotationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SecretId { get; set; }
    public Guid? KeyId { get; set; }
    public RotationType Type { get; set; }
    public RotationStatus Status { get; set; } = RotationStatus.Pending;
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class SecretBackup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BackupName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public BackupStatus Status { get; set; } = BackupStatus.InProgress;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Enums
public enum SecretType
{
    Generic,
    Password,
    ApiKey,
    Certificate,
    DatabaseConnection,
    OAuthToken,
    SshKey,
    TlsCertificate,
    AwsCredentials,
    AzureCredentials,
    GcpCredentials
}

public enum SecretStatus
{
    Active,
    Expired,
    Disabled,
    Deleted,
    PendingRotation
}

public enum KeyType
{
    Symmetric,
    Asymmetric,
    MasterKey,
    DataEncryptionKey
}

public enum KeyStatus
{
    Active,
    Rotated,
    Expired,
    Revoked,
    PendingActivation
}

public enum SecretOperation
{
    Read,
    Create,
    Update,
    Delete,
    List,
    Share,
    Rotate,
    Backup,
    Restore
}

public enum SecretShareStatus
{
    Active,
    Expired,
    Revoked,
    Used
}

public enum RotationType
{
    Secret,
    EncryptionKey,
    Certificate
}

public enum RotationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum BackupStatus
{
    InProgress,
    Completed,
    Failed,
    Expired
}

// DTOs and Request/Response Models
public class CreateSecretRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Path { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public SecretType Type { get; set; } = SecretType.Generic;
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class UpdateSecretRequest
{
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? ChangeReason { get; set; }
}

public class SecretResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SecretType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public SecretStatus Status { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int Version { get; set; }
    public string? Value { get; set; } // Only included when explicitly requested
}

public class ListSecretsRequest
{
    public string? PathPrefix { get; set; }
    public SecretType? Type { get; set; }
    public SecretStatus? Status { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
    public string? SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
}

public class ShareSecretRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public List<string> Permissions { get; set; } = new();
    
    public DateTime? ExpiresAt { get; set; }
    public bool GenerateShareToken { get; set; } = false;
}

public class ShareSecretResponse
{
    public Guid ShareId { get; set; }
    public string? ShareToken { get; set; }
    public DateTime SharedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class GenerateKeyRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public KeyType Type { get; set; } = KeyType.Symmetric;
    public string Algorithm { get; set; } = "AES-256-GCM";
    public int KeySize { get; set; } = 256;
    public bool UseHsm { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public int RotationIntervalDays { get; set; } = 90;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class RotateSecretRequest
{
    public string? NewValue { get; set; } // If null, auto-generate based on type
    public string? ChangeReason { get; set; }
    public bool ForceRotation { get; set; } = false;
}

public class BackupRequest
{
    public string BackupName { get; set; } = string.Empty;
    public List<string>? PathPrefixes { get; set; } // If null, backup all
    public bool IncludeMetadata { get; set; } = true;
    public bool EncryptBackup { get; set; } = true;
}

public class RestoreRequest
{
    [Required]
    public Guid BackupId { get; set; }
    
    public string? TargetPathPrefix { get; set; }
    public bool OverwriteExisting { get; set; } = false;
    public bool ValidateChecksums { get; set; } = true;
}

public class SecretStatistics
{
    public int TotalSecrets { get; set; }
    public int ActiveSecrets { get; set; }
    public int ExpiredSecrets { get; set; }
    public int SecretsExpiringIn30Days { get; set; }
    public Dictionary<SecretType, int> SecretsByType { get; set; } = new();
    public Dictionary<string, int> SecretsByTag { get; set; } = new();
    public int TotalAccesses { get; set; }
    public int AccessesLast24Hours { get; set; }
    public int FailedAccessesLast24Hours { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string? ResourcePath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ClientIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class HealthCheckResult
{
    public string Component { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metrics { get; set; } = new();
}