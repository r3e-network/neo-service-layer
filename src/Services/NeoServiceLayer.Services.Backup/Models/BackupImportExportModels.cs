namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Export destination type enumeration.
/// </summary>
public enum ExportDestinationType
{
    /// <summary>
    /// Local file system.
    /// </summary>
    LocalFileSystem,

    /// <summary>
    /// FTP server.
    /// </summary>
    FTP,

    /// <summary>
    /// SFTP server.
    /// </summary>
    SFTP,

    /// <summary>
    /// Cloud storage.
    /// </summary>
    CloudStorage,

    /// <summary>
    /// HTTP endpoint.
    /// </summary>
    HTTP,

    /// <summary>
    /// Email attachment.
    /// </summary>
    Email
}

/// <summary>
/// Export format enumeration.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Native backup format.
    /// </summary>
    Native,

    /// <summary>
    /// ZIP archive format.
    /// </summary>
    ZIP,

    /// <summary>
    /// TAR archive format.
    /// </summary>
    TAR,

    /// <summary>
    /// 7-Zip format.
    /// </summary>
    SevenZip,

    /// <summary>
    /// Custom format.
    /// </summary>
    Custom
}

/// <summary>
/// Import source type enumeration.
/// </summary>
public enum ImportSourceType
{
    /// <summary>
    /// Local file system.
    /// </summary>
    LocalFileSystem,

    /// <summary>
    /// FTP server.
    /// </summary>
    FTP,

    /// <summary>
    /// SFTP server.
    /// </summary>
    SFTP,

    /// <summary>
    /// Cloud storage.
    /// </summary>
    CloudStorage,

    /// <summary>
    /// HTTP endpoint.
    /// </summary>
    HTTP,

    /// <summary>
    /// Email attachment.
    /// </summary>
    Email
}

/// <summary>
/// Export backup request.
/// </summary>
public class ExportBackupRequest
{
    /// <summary>
    /// Gets or sets the backup ID to export.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the export destination.
    /// </summary>
    public ExportDestination Destination { get; set; } = new();

    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Native;

    /// <summary>
    /// Gets or sets the export options.
    /// </summary>
    public ExportOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Export destination.
/// </summary>
public class ExportDestination
{
    /// <summary>
    /// Gets or sets the destination type.
    /// </summary>
    public ExportDestinationType DestinationType { get; set; }

    /// <summary>
    /// Gets or sets the destination path or URL.
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access credentials.
    /// </summary>
    public Dictionary<string, string> Credentials { get; set; } = new();

    /// <summary>
    /// Gets or sets the destination configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Export options.
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Gets or sets whether to include metadata.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to verify the export.
    /// </summary>
    public bool VerifyExport { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to encrypt the export.
    /// </summary>
    public bool EncryptExport { get; set; } = false;

    /// <summary>
    /// Gets or sets the encryption password.
    /// </summary>
    public string? EncryptionPassword { get; set; }

    /// <summary>
    /// Gets or sets additional export options.
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}

/// <summary>
/// Backup export result.
/// </summary>
public class BackupExportResult
{
    /// <summary>
    /// Gets or sets the export ID.
    /// </summary>
    public string ExportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the export was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the export failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the export location.
    /// </summary>
    public string ExportLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the export size in bytes.
    /// </summary>
    public long ExportSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the export start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the export completion time.
    /// </summary>
    public DateTime? CompletionTime { get; set; }

    /// <summary>
    /// Gets or sets the export checksum.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Import backup request.
/// </summary>
public class ImportBackupRequest
{
    /// <summary>
    /// Gets or sets the import source.
    /// </summary>
    public ImportSource Source { get; set; } = new();

    /// <summary>
    /// Gets or sets the import destination.
    /// </summary>
    public BackupDestination Destination { get; set; } = new();

    /// <summary>
    /// Gets or sets the import options.
    /// </summary>
    public ImportOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Import source.
/// </summary>
public class ImportSource
{
    /// <summary>
    /// Gets or sets the source type.
    /// </summary>
    public ImportSourceType SourceType { get; set; }

    /// <summary>
    /// Gets or sets the source path or URL.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access credentials.
    /// </summary>
    public Dictionary<string, string> Credentials { get; set; } = new();

    /// <summary>
    /// Gets or sets the source configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Import options.
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// Gets or sets whether to validate the import.
    /// </summary>
    public bool ValidateImport { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to overwrite existing backups.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Gets or sets the decryption password.
    /// </summary>
    public string? DecryptionPassword { get; set; }

    /// <summary>
    /// Gets or sets whether to preserve original metadata.
    /// </summary>
    public bool PreserveMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets additional import options.
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}

/// <summary>
/// Backup import result.
/// </summary>
public class BackupImportResult
{
    /// <summary>
    /// Gets or sets the import ID.
    /// </summary>
    public string ImportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the imported backup ID.
    /// </summary>
    public string ImportedBackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the import failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the import start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the import completion time.
    /// </summary>
    public DateTime? CompletionTime { get; set; }

    /// <summary>
    /// Gets or sets the imported data size in bytes.
    /// </summary>
    public long ImportedSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of items imported.
    /// </summary>
    public int ItemsImported { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
