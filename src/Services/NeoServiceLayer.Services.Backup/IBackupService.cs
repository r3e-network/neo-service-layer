using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Backup.Models;

namespace NeoServiceLayer.Services.Backup;

/// <summary>
/// Interface for the Backup Service that provides data backup and recovery capabilities.
/// </summary>
public interface IBackupService : IService
{
    /// <summary>
    /// Creates a backup of the specified data.
    /// </summary>
    /// <param name="request">The backup creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The backup creation result.</returns>
    Task<BackupResult> CreateBackupAsync(CreateBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Restores data from a backup.
    /// </summary>
    /// <param name="request">The restore request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The restore result.</returns>
    Task<RestoreResult> RestoreBackupAsync(RestoreBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a backup operation.
    /// </summary>
    /// <param name="request">The backup status request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The backup status.</returns>
    Task<BackupStatusResult> GetBackupStatusAsync(BackupStatusRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Lists available backups.
    /// </summary>
    /// <param name="request">The list backups request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The list of available backups.</returns>
    Task<BackupListResult> ListBackupsAsync(ListBackupsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a backup.
    /// </summary>
    /// <param name="request">The delete backup request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The deletion result.</returns>
    Task<BackupDeletionResult> DeleteBackupAsync(DeleteBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Validates the integrity of a backup.
    /// </summary>
    /// <param name="request">The backup validation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The validation result.</returns>
    Task<BackupValidationResult> ValidateBackupAsync(ValidateBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a scheduled backup job.
    /// </summary>
    /// <param name="request">The scheduled backup request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The scheduled backup result.</returns>
    Task<ScheduledBackupResult> CreateScheduledBackupAsync(CreateScheduledBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets backup statistics and metrics.
    /// </summary>
    /// <param name="request">The backup statistics request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The backup statistics.</returns>
    Task<BackupStatisticsResult> GetBackupStatisticsAsync(BackupStatisticsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Exports a backup to an external location.
    /// </summary>
    /// <param name="request">The backup export request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The export result.</returns>
    Task<BackupExportResult> ExportBackupAsync(ExportBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Imports a backup from an external location.
    /// </summary>
    /// <param name="request">The backup import request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The import result.</returns>
    Task<BackupImportResult> ImportBackupAsync(ImportBackupRequest request, BlockchainType blockchainType);
}

/// <summary>
/// Create backup request.
/// </summary>
public class CreateBackupRequest
{
    /// <summary>
    /// Gets or sets the backup name.
    /// </summary>
    public string BackupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup type.
    /// </summary>
    public BackupType BackupType { get; set; }

    /// <summary>
    /// Gets or sets the data sources to backup.
    /// </summary>
    public BackupDataSource[] DataSources { get; set; } = Array.Empty<BackupDataSource>();

    /// <summary>
    /// Gets or sets the backup destination.
    /// </summary>
    public BackupDestination Destination { get; set; } = new();

    /// <summary>
    /// Gets or sets the compression settings.
    /// </summary>
    public CompressionSettings Compression { get; set; } = new();

    /// <summary>
    /// Gets or sets the encryption settings.
    /// </summary>
    public EncryptionSettings Encryption { get; set; } = new();

    /// <summary>
    /// Gets or sets the retention policy.
    /// </summary>
    public RetentionPolicy RetentionPolicy { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to verify the backup after creation.
    /// </summary>
    public bool VerifyAfterCreation { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup type enumeration.
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Full backup of all data.
    /// </summary>
    Full,

    /// <summary>
    /// Incremental backup of changes since last backup.
    /// </summary>
    Incremental,

    /// <summary>
    /// Differential backup of changes since last full backup.
    /// </summary>
    Differential,

    /// <summary>
    /// Snapshot backup at a specific point in time.
    /// </summary>
    Snapshot
}

/// <summary>
/// Backup data source.
/// </summary>
public class BackupDataSource
{
    /// <summary>
    /// Gets or sets the source type.
    /// </summary>
    public DataSourceType SourceType { get; set; }

    /// <summary>
    /// Gets or sets the source identifier.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source path or connection string.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inclusion filters.
    /// </summary>
    public string[] IncludeFilters { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the exclusion filters.
    /// </summary>
    public string[] ExcludeFilters { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional source metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Data source type enumeration.
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// File system data source.
    /// </summary>
    FileSystem,

    /// <summary>
    /// Database data source.
    /// </summary>
    Database,

    /// <summary>
    /// Service data source.
    /// </summary>
    Service,

    /// <summary>
    /// Configuration data source.
    /// </summary>
    Configuration,

    /// <summary>
    /// Blockchain data source.
    /// </summary>
    Blockchain,

    /// <summary>
    /// Enclave data source.
    /// </summary>
    Enclave
}

/// <summary>
/// Backup destination.
/// </summary>
public class BackupDestination
{
    /// <summary>
    /// Gets or sets the destination type.
    /// </summary>
    public DestinationType DestinationType { get; set; }

    /// <summary>
    /// Gets or sets the destination path or connection string.
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the access credentials.
    /// </summary>
    public Dictionary<string, string> Credentials { get; set; } = new();
}

/// <summary>
/// Destination type enumeration.
/// </summary>
public enum DestinationType
{
    /// <summary>
    /// Local file system destination.
    /// </summary>
    LocalFileSystem,

    /// <summary>
    /// Network file share destination.
    /// </summary>
    NetworkShare,

    /// <summary>
    /// Cloud storage destination.
    /// </summary>
    CloudStorage,

    /// <summary>
    /// Database destination.
    /// </summary>
    Database,

    /// <summary>
    /// Distributed storage destination.
    /// </summary>
    DistributedStorage
}

/// <summary>
/// Compression settings.
/// </summary>
public class CompressionSettings
{
    /// <summary>
    /// Gets or sets whether compression is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the compression algorithm.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.GZip;

    /// <summary>
    /// Gets or sets the compression level.
    /// </summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Optimal;

    /// <summary>
    /// Gets or sets additional compression options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Compression algorithm enumeration.
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// No compression.
    /// </summary>
    None,

    /// <summary>
    /// GZip compression.
    /// </summary>
    GZip,

    /// <summary>
    /// Deflate compression.
    /// </summary>
    Deflate,

    /// <summary>
    /// Brotli compression.
    /// </summary>
    Brotli,

    /// <summary>
    /// LZ4 compression.
    /// </summary>
    LZ4,

    /// <summary>
    /// LZMA compression.
    /// </summary>
    LZMA
}

/// <summary>
/// Compression level enumeration.
/// </summary>
public enum CompressionLevel
{
    /// <summary>
    /// No compression.
    /// </summary>
    NoCompression,

    /// <summary>
    /// Fastest compression.
    /// </summary>
    Fastest,

    /// <summary>
    /// Optimal compression.
    /// </summary>
    Optimal,

    /// <summary>
    /// Smallest size compression.
    /// </summary>
    SmallestSize
}

/// <summary>
/// Encryption settings.
/// </summary>
public class EncryptionSettings
{
    /// <summary>
    /// Gets or sets whether encryption is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the encryption algorithm.
    /// </summary>
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256;

    /// <summary>
    /// Gets or sets the encryption key.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the key derivation settings.
    /// </summary>
    public KeyDerivationSettings KeyDerivation { get; set; } = new();

    /// <summary>
    /// Gets or sets additional encryption options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Encryption algorithm enumeration.
/// </summary>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// No encryption.
    /// </summary>
    None,

    /// <summary>
    /// AES-128 encryption.
    /// </summary>
    AES128,

    /// <summary>
    /// AES-256 encryption.
    /// </summary>
    AES256,

    /// <summary>
    /// ChaCha20 encryption.
    /// </summary>
    ChaCha20,

    /// <summary>
    /// RSA encryption.
    /// </summary>
    RSA
}

/// <summary>
/// Key derivation settings.
/// </summary>
public class KeyDerivationSettings
{
    /// <summary>
    /// Gets or sets the key derivation function.
    /// </summary>
    public KeyDerivationFunction Function { get; set; } = KeyDerivationFunction.PBKDF2;

    /// <summary>
    /// Gets or sets the number of iterations.
    /// </summary>
    public int Iterations { get; set; } = 100000;

    /// <summary>
    /// Gets or sets the salt.
    /// </summary>
    public byte[] Salt { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets additional derivation options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Key derivation function enumeration.
/// </summary>
public enum KeyDerivationFunction
{
    /// <summary>
    /// PBKDF2 key derivation.
    /// </summary>
    PBKDF2,

    /// <summary>
    /// Scrypt key derivation.
    /// </summary>
    Scrypt,

    /// <summary>
    /// Argon2 key derivation.
    /// </summary>
    Argon2
}

/// <summary>
/// Retention policy.
/// </summary>
public class RetentionPolicy
{
    /// <summary>
    /// Gets or sets the retention period.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the maximum number of backups to keep.
    /// </summary>
    public int MaxBackupCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to auto-delete expired backups.
    /// </summary>
    public bool AutoDeleteExpired { get; set; } = true;

    /// <summary>
    /// Gets or sets additional retention options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}
