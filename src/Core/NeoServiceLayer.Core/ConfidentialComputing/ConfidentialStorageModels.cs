using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Configuration options for confidential storage operations
    /// </summary>
    public class ConfidentialStorageOptions
    {
        /// <summary>
        /// Whether to compress data before sealing
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Whether to enable data deduplication
        /// </summary>
        public bool EnableDeduplication { get; set; } = false;

        /// <summary>
        /// Sealing policy to use
        /// </summary>
        public SealingPolicy SealingPolicy { get; set; } = SealingPolicy.MrSigner;

        /// <summary>
        /// Data expiration time (null = never expires)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Access control settings
        /// </summary>
        public ConfidentialAccessControl? AccessControl { get; set; }

        /// <summary>
        /// Custom metadata to associate with the stored data
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Maximum number of access attempts before data is locked
        /// </summary>
        public int? MaxAccessAttempts { get; set; }

        /// <summary>
        /// Whether to enable audit logging for this data
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;
    }

    /// <summary>
    /// Result of a confidential storage operation
    /// </summary>
    public class ConfidentialStorageResult
    {
        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Unique identifier for the stored data
        /// </summary>
        public string? StorageId { get; set; }

        /// <summary>
        /// Size of the sealed data in bytes
        /// </summary>
        public long SealedDataSize { get; set; }

        /// <summary>
        /// Size of the original data in bytes
        /// </summary>
        public long OriginalDataSize { get; set; }

        /// <summary>
        /// Compression ratio achieved (if compression enabled)
        /// </summary>
        public double CompressionRatio { get; set; }

        /// <summary>
        /// Fingerprint of the stored data for integrity verification
        /// </summary>
        public string? DataFingerprint { get; set; }

        /// <summary>
        /// When the data expires (if applicable)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Storage operation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata about the operation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of a confidential data retrieval operation
    /// </summary>
    /// <typeparam name="T">Type of retrieved data</typeparam>
    public class ConfidentialRetrievalResult<T>
    {
        /// <summary>
        /// Whether the retrieval succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The retrieved and unsealed data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Metadata associated with the stored data
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// When the data was originally stored
        /// </summary>
        public DateTime? StoredAt { get; set; }

        /// <summary>
        /// When the data was last accessed
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// Number of times the data has been accessed
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Data expiration time (if applicable)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Fingerprint for integrity verification
        /// </summary>
        public string? DataFingerprint { get; set; }

        /// <summary>
        /// Error message if retrieval failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Retrieval operation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of a confidential data deletion operation
    /// </summary>
    public class ConfidentialDeletionResult
    {
        /// <summary>
        /// Whether the deletion succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether the data was found and deleted
        /// </summary>
        public bool DataFound { get; set; }

        /// <summary>
        /// Whether secure wiping was performed
        /// </summary>
        public bool SecurelyWiped { get; set; }

        /// <summary>
        /// Number of bytes that were deleted
        /// </summary>
        public long BytesDeleted { get; set; }

        /// <summary>
        /// Error message if deletion failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Deletion operation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of a key listing operation
    /// </summary>
    public class ConfidentialKeyListResult
    {
        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// List of keys with their metadata
        /// </summary>
        public List<ConfidentialKeyInfo> Keys { get; set; } = new();

        /// <summary>
        /// Total number of keys found
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Pattern that was used for filtering
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Operation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Information about a stored key
    /// </summary>
    public class ConfidentialKeyInfo
    {
        /// <summary>
        /// The storage key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Size of the sealed data in bytes
        /// </summary>
        public long SealedDataSize { get; set; }

        /// <summary>
        /// When the data was stored
        /// </summary>
        public DateTime StoredAt { get; set; }

        /// <summary>
        /// When the data was last accessed
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// Number of times the data has been accessed
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Data expiration time (if applicable)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Whether the data has expired
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

        /// <summary>
        /// Sealing policy used for this data
        /// </summary>
        public SealingPolicy SealingPolicy { get; set; }

        /// <summary>
        /// Custom metadata associated with the data
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Request for creating a backup of confidential data
    /// </summary>
    public class ConfidentialBackupRequest
    {
        /// <summary>
        /// Backup name/identifier
        /// </summary>
        public string BackupName { get; set; } = string.Empty;

        /// <summary>
        /// Pattern for keys to include in backup (* = all)
        /// </summary>
        public string KeyPattern { get; set; } = "*";

        /// <summary>
        /// Backup destination path/identifier
        /// </summary>
        public string Destination { get; set; } = string.Empty;

        /// <summary>
        /// Whether to encrypt the backup
        /// </summary>
        public bool EncryptBackup { get; set; } = true;

        /// <summary>
        /// Whether to compress the backup
        /// </summary>
        public bool CompressBackup { get; set; } = true;

        /// <summary>
        /// Backup metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of a backup operation
    /// </summary>
    public class ConfidentialBackupResult
    {
        /// <summary>
        /// Whether the backup succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Unique backup identifier
        /// </summary>
        public string? BackupId { get; set; }

        /// <summary>
        /// Number of keys backed up
        /// </summary>
        public int KeysBackedUp { get; set; }

        /// <summary>
        /// Total size of backed up data
        /// </summary>
        public long TotalDataSize { get; set; }

        /// <summary>
        /// Backup file size
        /// </summary>
        public long BackupFileSize { get; set; }

        /// <summary>
        /// Backup compression ratio
        /// </summary>
        public double CompressionRatio { get; set; }

        /// <summary>
        /// Backup location/path
        /// </summary>
        public string? BackupLocation { get; set; }

        /// <summary>
        /// Error message if backup failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Backup timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request for restoring data from a backup
    /// </summary>
    public class ConfidentialRestoreRequest
    {
        /// <summary>
        /// Backup identifier to restore from
        /// </summary>
        public string BackupId { get; set; } = string.Empty;

        /// <summary>
        /// Backup location/path
        /// </summary>
        public string BackupLocation { get; set; } = string.Empty;

        /// <summary>
        /// Pattern for keys to restore (* = all)
        /// </summary>
        public string KeyPattern { get; set; } = "*";

        /// <summary>
        /// Whether to overwrite existing keys
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// Restore metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of a restore operation
    /// </summary>
    public class ConfidentialRestoreResult
    {
        /// <summary>
        /// Whether the restore succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Number of keys restored
        /// </summary>
        public int KeysRestored { get; set; }

        /// <summary>
        /// Number of keys skipped (already existed)
        /// </summary>
        public int KeysSkipped { get; set; }

        /// <summary>
        /// Number of keys that failed to restore
        /// </summary>
        public int KeysFailed { get; set; }

        /// <summary>
        /// Total size of restored data
        /// </summary>
        public long TotalDataSize { get; set; }

        /// <summary>
        /// Error message if restore failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Restore timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Details about individual key restore results
        /// </summary>
        public List<KeyRestoreDetail> Details { get; set; } = new();
    }

    /// <summary>
    /// Details about individual key restore operation
    /// </summary>
    public class KeyRestoreDetail
    {
        /// <summary>
        /// Storage key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Whether this key was restored successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if key restore failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Storage statistics and health information
    /// </summary>
    public class ConfidentialStorageStatistics
    {
        /// <summary>
        /// Total number of keys stored
        /// </summary>
        public int TotalKeys { get; set; }

        /// <summary>
        /// Total size of all sealed data
        /// </summary>
        public long TotalSealedDataSize { get; set; }

        /// <summary>
        /// Total size of all original data
        /// </summary>
        public long TotalOriginalDataSize { get; set; }

        /// <summary>
        /// Overall compression ratio
        /// </summary>
        public double OverallCompressionRatio { get; set; }

        /// <summary>
        /// Available storage space
        /// </summary>
        public long AvailableSpace { get; set; }

        /// <summary>
        /// Storage utilization percentage
        /// </summary>
        public double UtilizationPercent { get; set; }

        /// <summary>
        /// Number of expired keys
        /// </summary>
        public int ExpiredKeys { get; set; }

        /// <summary>
        /// Storage health status
        /// </summary>
        public StorageHealthStatus HealthStatus { get; set; }

        /// <summary>
        /// Last backup time
        /// </summary>
        public DateTime? LastBackupTime { get; set; }

        /// <summary>
        /// Statistics collection timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional storage metrics
        /// </summary>
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    /// <summary>
    /// Result of an integrity check operation
    /// </summary>
    public class ConfidentialIntegrityResult
    {
        /// <summary>
        /// Whether the integrity check passed
        /// </summary>
        public bool IntegrityValid { get; set; }

        /// <summary>
        /// Number of keys checked
        /// </summary>
        public int KeysChecked { get; set; }

        /// <summary>
        /// Number of keys with integrity issues
        /// </summary>
        public int CorruptedKeys { get; set; }

        /// <summary>
        /// List of keys with integrity issues
        /// </summary>
        public List<string> CorruptedKeyList { get; set; } = new();

        /// <summary>
        /// Detailed integrity check results
        /// </summary>
        public List<KeyIntegrityResult> DetailedResults { get; set; } = new();

        /// <summary>
        /// Integrity check timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Integrity result for a specific key
    /// </summary>
    public class KeyIntegrityResult
    {
        /// <summary>
        /// Storage key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Whether the key's integrity is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Expected fingerprint
        /// </summary>
        public string? ExpectedFingerprint { get; set; }

        /// <summary>
        /// Actual computed fingerprint
        /// </summary>
        public string? ActualFingerprint { get; set; }

        /// <summary>
        /// Error message if integrity check failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of a transaction operation
    /// </summary>
    public class ConfidentialTransactionResult
    {
        /// <summary>
        /// Whether the transaction operation succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Number of operations in the transaction
        /// </summary>
        public int OperationCount { get; set; }

        /// <summary>
        /// Transaction completion timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Error message if transaction failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Access control settings for confidential data
    /// </summary>
    public class ConfidentialAccessControl
    {
        /// <summary>
        /// List of allowed user/service identities
        /// </summary>
        public List<string> AllowedIdentities { get; set; } = new();

        /// <summary>
        /// Required permissions for access
        /// </summary>
        public List<string> RequiredPermissions { get; set; } = new();

        /// <summary>
        /// Access time restrictions
        /// </summary>
        public AccessTimeRestriction? TimeRestriction { get; set; }

        /// <summary>
        /// Maximum number of concurrent accesses
        /// </summary>
        public int? MaxConcurrentAccess { get; set; }
    }

    /// <summary>
    /// Time-based access restrictions
    /// </summary>
    public class AccessTimeRestriction
    {
        /// <summary>
        /// Earliest time access is allowed
        /// </summary>
        public TimeSpan? AllowedFromTime { get; set; }

        /// <summary>
        /// Latest time access is allowed
        /// </summary>
        public TimeSpan? AllowedToTime { get; set; }

        /// <summary>
        /// Days of week access is allowed
        /// </summary>
        public List<DayOfWeek> AllowedDays { get; set; } = new();
    }

    /// <summary>
    /// SGX sealing policy options
    /// </summary>
    public enum SealingPolicy
    {
        /// <summary>
        /// Seal to enclave identity (MRENCLAVE) - most restrictive
        /// </summary>
        MrEnclave,

        /// <summary>
        /// Seal to signer identity (MRSIGNER) - allows upgrades
        /// </summary>
        MrSigner,

        /// <summary>
        /// Custom sealing policy
        /// </summary>
        Custom
    }

    /// <summary>
    /// Storage health status enumeration
    /// </summary>
    public enum StorageHealthStatus
    {
        /// <summary>
        /// Storage is healthy and fully operational
        /// </summary>
        Healthy,

        /// <summary>
        /// Storage has minor issues but is operational
        /// </summary>
        Warning,

        /// <summary>
        /// Storage has significant issues affecting operations
        /// </summary>
        Critical,

        /// <summary>
        /// Storage is not operational
        /// </summary>
        Failed
    }
}