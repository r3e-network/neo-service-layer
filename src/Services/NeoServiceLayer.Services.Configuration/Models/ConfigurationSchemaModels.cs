using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Schema type enumeration.
/// </summary>
public enum SchemaType
{
    /// <summary>
    /// JSON Schema.
    /// </summary>
    JsonSchema,

    /// <summary>
    /// XML Schema.
    /// </summary>
    XmlSchema,

    /// <summary>
    /// Custom schema.
    /// </summary>
    Custom,

    /// <summary>
    /// Simple validation rules.
    /// </summary>
    SimpleRules
}

/// <summary>
/// Create configuration schema request.
/// </summary>
public class CreateConfigurationSchemaRequest
{
    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the schema definition.
    /// </summary>
    [Required]
    public SchemaDefinition Definition { get; set; } = new();

    /// <summary>
    /// Gets or sets the applicable scopes.
    /// </summary>
    public List<ConfigurationScope> ApplicableScopes { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Schema definition.
/// </summary>
public class SchemaDefinition
{
    /// <summary>
    /// Gets or sets the schema type.
    /// </summary>
    public SchemaType SchemaType { get; set; } = SchemaType.JsonSchema;

    /// <summary>
    /// Gets or sets the schema content.
    /// </summary>
    [Required]
    public object SchemaContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation rules.
    /// </summary>
    public List<ValidationRule> ValidationRules { get; set; } = new();

    /// <summary>
    /// Gets or sets the default values.
    /// </summary>
    public Dictionary<string, object> DefaultValues { get; set; } = new();

    /// <summary>
    /// Gets or sets additional schema properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Validation rule.
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    [Required]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule expression.
    /// </summary>
    [Required]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this rule is required.
    /// </summary>
    public bool IsRequired { get; set; } = false;
}

/// <summary>
/// Configuration schema response.
/// </summary>
public class ConfigurationSchemaResponse
{
    /// <summary>
    /// Gets or sets the schema ID.
    /// </summary>
    public string SchemaId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}