namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Configuration scope enumeration.
/// </summary>
public enum ConfigurationScope
{
    /// <summary>
    /// Global configuration scope.
    /// </summary>
    Global,

    /// <summary>
    /// Service-specific configuration scope.
    /// </summary>
    Service,

    /// <summary>
    /// Environment-specific configuration scope.
    /// </summary>
    Environment,

    /// <summary>
    /// User-specific configuration scope.
    /// </summary>
    User,

    /// <summary>
    /// Application-specific configuration scope.
    /// </summary>
    Application,

    /// <summary>
    /// Tenant-specific configuration scope.
    /// </summary>
    Tenant
}

/// <summary>
/// Configuration value type enumeration.
/// </summary>
public enum ConfigurationValueType
{
    /// <summary>
    /// String value type.
    /// </summary>
    String,

    /// <summary>
    /// Integer value type.
    /// </summary>
    Integer,

    /// <summary>
    /// Boolean value type.
    /// </summary>
    Boolean,

    /// <summary>
    /// Double value type.
    /// </summary>
    Double,

    /// <summary>
    /// DateTime value type.
    /// </summary>
    DateTime,

    /// <summary>
    /// JSON object value type.
    /// </summary>
    JsonObject,

    /// <summary>
    /// Array value type.
    /// </summary>
    Array,

    /// <summary>
    /// Binary data value type.
    /// </summary>
    Binary,

    /// <summary>
    /// Encrypted value type.
    /// </summary>
    Encrypted
}

/// <summary>
/// Get configuration request.
/// </summary>
public class GetConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;

    /// <summary>
    /// Gets or sets the environment filter.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service filter.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the default value to return if key is not found.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether to decrypt encrypted values.
    /// </summary>
    public bool DecryptValue { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration result.
/// </summary>
public class ConfigurationResult
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public ConfigurationValueType ValueType { get; set; }

    /// <summary>
    /// Gets or sets whether the value was found.
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Gets or sets whether the value is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the version of the configuration.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Set configuration request.
/// </summary>
public class SetConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public ConfigurationValueType ValueType { get; set; }

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;

    /// <summary>
    /// Gets or sets the environment.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets whether to encrypt the value.
    /// </summary>
    public bool EncryptValue { get; set; } = false;

    /// <summary>
    /// Gets or sets the configuration description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration tags.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to create a backup before updating.
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration set result.
/// </summary>
public class ConfigurationSetResult
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the previous value.
    /// </summary>
    public object? PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the new version number.
    /// </summary>
    public int NewVersion { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the operation.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the backup ID if a backup was created.
    /// </summary>
    public string? BackupId { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Delete configuration request.
/// </summary>
public class DeleteConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;

    /// <summary>
    /// Gets or sets the environment filter.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service filter.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets whether to create a backup before deletion.
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration delete result.
/// </summary>
public class ConfigurationDeleteResult
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the deleted value.
    /// </summary>
    public object? DeletedValue { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the operation.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the backup ID if a backup was created.
    /// </summary>
    public string? BackupId { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
