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
/// Create schema request.
/// </summary>
public class CreateSchemaRequest
{
    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the schema definition.
    /// </summary>
    public SchemaDefinition Definition { get; set; } = new();

    /// <summary>
    /// Gets or sets the applicable scopes.
    /// </summary>
    public ConfigurationScope[] ApplicableScopes { get; set; } = Array.Empty<ConfigurationScope>();

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
    public string SchemaContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation rules.
    /// </summary>
    public ValidationRule[] ValidationRules { get; set; } = Array.Empty<ValidationRule>();

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
/// Configuration schema result.
/// </summary>
public class ConfigurationSchemaResult
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
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
