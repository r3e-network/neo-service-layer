using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Configuration.Models;

namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Interface for the Configuration Service that provides dynamic configuration management.
/// </summary>
public interface IConfigurationService : IService
{
    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="request">The configuration get request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration value.</returns>
    Task<ConfigurationResult> GetConfigurationAsync(GetConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="request">The configuration set request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration set result.</returns>
    Task<ConfigurationSetResult> SetConfigurationAsync(SetConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a configuration value.
    /// </summary>
    /// <param name="request">The configuration delete request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration delete result.</returns>
    Task<ConfigurationDeleteResult> DeleteConfigurationAsync(DeleteConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Lists configuration keys and values.
    /// </summary>
    /// <param name="request">The configuration list request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration list result.</returns>
    Task<ConfigurationListResult> ListConfigurationsAsync(ListConfigurationsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Validates configuration values.
    /// </summary>
    /// <param name="request">The configuration validation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The validation result.</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(ValidateConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a configuration schema.
    /// </summary>
    /// <param name="request">The schema creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The schema creation result.</returns>
    Task<ConfigurationSchemaResult> CreateSchemaAsync(CreateSchemaRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Exports configuration data.
    /// </summary>
    /// <param name="request">The configuration export request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The export result.</returns>
    Task<ConfigurationExportResult> ExportConfigurationAsync(ExportConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Imports configuration data.
    /// </summary>
    /// <param name="request">The configuration import request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The import result.</returns>
    Task<ConfigurationImportResult> ImportConfigurationAsync(ImportConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Subscribes to configuration changes.
    /// </summary>
    /// <param name="request">The configuration subscription request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The subscription result.</returns>
    Task<ConfigurationSubscriptionResult> SubscribeToChangesAsync(SubscribeToChangesRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets configuration change history.
    /// </summary>
    /// <param name="request">The configuration history request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration history.</returns>
    Task<ConfigurationHistoryResult> GetConfigurationHistoryAsync(GetConfigurationHistoryRequest request, BlockchainType blockchainType);
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
