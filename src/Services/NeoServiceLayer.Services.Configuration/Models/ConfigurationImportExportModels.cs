namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Configuration export format enumeration.
/// </summary>
public enum ConfigurationExportFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// XML format.
    /// </summary>
    Xml,

    /// <summary>
    /// YAML format.
    /// </summary>
    Yaml,

    /// <summary>
    /// Properties format.
    /// </summary>
    Properties,

    /// <summary>
    /// Environment variables format.
    /// </summary>
    EnvironmentVariables,

    /// <summary>
    /// CSV format.
    /// </summary>
    Csv
}

/// <summary>
/// Import mode enumeration.
/// </summary>
public enum ImportMode
{
    /// <summary>
    /// Merge with existing configurations.
    /// </summary>
    Merge,

    /// <summary>
    /// Replace existing configurations.
    /// </summary>
    Replace,

    /// <summary>
    /// Add only new configurations.
    /// </summary>
    AddOnly,

    /// <summary>
    /// Update only existing configurations.
    /// </summary>
    UpdateOnly
}

/// <summary>
/// Import error type enumeration.
/// </summary>
public enum ImportErrorType
{
    /// <summary>
    /// Validation error.
    /// </summary>
    ValidationError,

    /// <summary>
    /// Duplicate key error.
    /// </summary>
    DuplicateKey,

    /// <summary>
    /// Permission denied error.
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// Format error.
    /// </summary>
    FormatError,

    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown
}

/// <summary>
/// Export configuration request.
/// </summary>
public class ExportConfigurationRequest
{
    /// <summary>
    /// Gets or sets the export scope.
    /// </summary>
    public ConfigurationScope? Scope { get; set; }

    /// <summary>
    /// Gets or sets the environment filter.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service filter.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the key prefix filter.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    public ConfigurationExportFormat Format { get; set; } = ConfigurationExportFormat.Json;

    /// <summary>
    /// Gets or sets whether to include encrypted values.
    /// </summary>
    public bool IncludeEncryptedValues { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include metadata.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets who is exporting the configuration.
    /// </summary>
    public string? ExportedBy { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration export result.
/// </summary>
public partial class ConfigurationExportResult
{
    /// <summary>
    /// Gets or sets the export ID.
    /// </summary>
    public string ExportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exported data.
    /// </summary>
    public string ExportedData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the export data (alias for ExportedData).
    /// </summary>
    public string ExportData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    public ConfigurationExportFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the number of configurations exported.
    /// </summary>
    public int ConfigurationCount { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the export timestamp.
    /// </summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Import configuration request.
/// </summary>
public class ImportConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration data to import.
    /// </summary>
    public string ConfigurationData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the import format.
    /// </summary>
    public ConfigurationExportFormat Format { get; set; } = ConfigurationExportFormat.Json;

    /// <summary>
    /// Gets or sets the import mode.
    /// </summary>
    public ImportMode ImportMode { get; set; } = ImportMode.Merge;

    /// <summary>
    /// Gets or sets whether to validate before import.
    /// </summary>
    public bool ValidateBeforeImport { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to create backups before import.
    /// </summary>
    public bool CreateBackups { get; set; } = true;

    /// <summary>
    /// Gets or sets the target scope for imported configurations.
    /// </summary>
    public ConfigurationScope? TargetScope { get; set; }

    /// <summary>
    /// Gets or sets the target environment.
    /// </summary>
    public string? TargetEnvironment { get; set; }

    /// <summary>
    /// Gets or sets the import data.
    /// </summary>
    public string ImportData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to overwrite existing configurations.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration import result.
/// </summary>
public partial class ConfigurationImportResult
{
    /// <summary>
    /// Gets or sets the import ID.
    /// </summary>
    public string ImportId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of configurations imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of configurations updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of configurations skipped.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Gets or sets the import errors.
    /// </summary>
    public ImportError[] ImportErrors { get; set; } = Array.Empty<ImportError>();

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the import timestamp.
    /// </summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// Gets or sets the backup IDs created during import.
    /// </summary>
    public string[] BackupIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Import error.
/// </summary>
public class ImportError
{
    /// <summary>
    /// Gets or sets the configuration key that failed.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error type.
    /// </summary>
    public ImportErrorType ErrorType { get; set; }

    /// <summary>
    /// Gets or sets additional error details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
