namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Validation rule type enumeration.
/// </summary>
public enum ValidationRuleType
{
    /// <summary>
    /// Required value validation.
    /// </summary>
    Required,

    /// <summary>
    /// Minimum value validation.
    /// </summary>
    MinValue,

    /// <summary>
    /// Maximum value validation.
    /// </summary>
    MaxValue,

    /// <summary>
    /// Minimum length validation.
    /// </summary>
    MinLength,

    /// <summary>
    /// Maximum length validation.
    /// </summary>
    MaxLength,

    /// <summary>
    /// Regular expression validation.
    /// </summary>
    Regex,

    /// <summary>
    /// Allowed values validation.
    /// </summary>
    AllowedValues,

    /// <summary>
    /// JSON schema validation.
    /// </summary>
    JsonSchema,

    /// <summary>
    /// Custom validation.
    /// </summary>
    Custom
}

/// <summary>
/// Validate configuration request.
/// </summary>
public class ValidateConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value to validate.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public ConfigurationValueType ValueType { get; set; }

    /// <summary>
    /// Gets or sets the validation schema ID.
    /// </summary>
    public string? SchemaId { get; set; }

    /// <summary>
    /// Gets or sets the validation rules.
    /// </summary>
    public ValidationRule[] ValidationRules { get; set; } = Array.Empty<ValidationRule>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Validation rule.
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Gets or sets the rule type.
    /// </summary>
    public ValidationRuleType RuleType { get; set; }

    /// <summary>
    /// Gets or sets the rule parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the rule is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;
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
    /// Gets or sets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public ValidationError[] ValidationErrors { get; set; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    public ValidationWarning[] ValidationWarnings { get; set; } = Array.Empty<ValidationWarning>();

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
/// Validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field path.
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule type that failed.
    /// </summary>
    public ValidationRuleType RuleType { get; set; }

    /// <summary>
    /// Gets or sets additional error details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Validation warning.
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Gets or sets the warning code.
    /// </summary>
    public string WarningCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public string WarningMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field path.
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional warning details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
