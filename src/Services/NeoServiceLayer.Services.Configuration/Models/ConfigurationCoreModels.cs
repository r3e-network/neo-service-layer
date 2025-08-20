using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using NeoServiceLayer.Core;

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
    /// Decimal value type.
    /// </summary>
    Decimal,

    /// <summary>
    /// Double precision floating point value type.
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
    Binary
}

/// <summary>
/// Get configuration request.
/// </summary>
public class GetConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;

    /// <summary>
    /// Gets or sets whether to include metadata.
    /// </summary>
    public bool IncludeMetadata { get; set; } = false;

    /// <summary>
    /// Gets or sets the default value to return if key is not found.
    /// </summary>
    public object? DefaultValue { get; set; }
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
    public ConfigurationValueType ValueType { get; set; } = ConfigurationValueType.String;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the message for the operation result.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the configuration version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets whether the configuration was found.
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// Set configuration request.
/// </summary>
public class SetConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    [Required]
    public object Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public ConfigurationValueType ValueType { get; set; } = ConfigurationValueType.String;

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to encrypt the value.
    /// </summary>
    public bool EncryptValue { get; set; }
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
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the new version number after the update.
    /// </summary>
    public int? NewVersion { get; set; }

    /// <summary>
    /// Gets or sets whether this was a new configuration.
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Gets or sets the previous value.
    /// </summary>
    public object? PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the operation.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Delete configuration request.
/// </summary>
public class DeleteConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;

    /// <summary>
    /// Gets or sets the deletion reason.
    /// </summary>
    public string? Reason { get; set; }
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
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets whether the key existed before deletion.
    /// </summary>
    public bool KeyExisted { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the operation.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Validate configuration request.
/// </summary>
public class ValidateConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value to validate.
    /// </summary>
    [Required]
    public object Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema identifier for validation.
    /// </summary>
    public string? SchemaId { get; set; }

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;
}

/// <summary>
/// Configuration validation result.
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets validation errors.
    /// </summary>
    public ValidationError[] ValidationErrors { get; set; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}