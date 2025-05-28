namespace NeoServiceLayer.Services.Backup.Models;

/// <summary>
/// Validation type enumeration.
/// </summary>
public enum ValidationType
{
    /// <summary>
    /// Integrity validation.
    /// </summary>
    Integrity,

    /// <summary>
    /// Checksum validation.
    /// </summary>
    Checksum,

    /// <summary>
    /// Content validation.
    /// </summary>
    Content,

    /// <summary>
    /// Complete validation.
    /// </summary>
    Complete
}

/// <summary>
/// Validation error type enumeration.
/// </summary>
public enum ValidationErrorType
{
    /// <summary>
    /// Checksum mismatch error.
    /// </summary>
    ChecksumMismatch,

    /// <summary>
    /// Missing file error.
    /// </summary>
    MissingFile,

    /// <summary>
    /// Corrupted data error.
    /// </summary>
    CorruptedData,

    /// <summary>
    /// Invalid format error.
    /// </summary>
    InvalidFormat,

    /// <summary>
    /// Access denied error.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown
}

/// <summary>
/// Error severity enumeration.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Low severity.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity.
    /// </summary>
    Critical
}

/// <summary>
/// Validate backup request.
/// </summary>
public class ValidateBackupRequest
{
    /// <summary>
    /// Gets or sets the backup ID to validate.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation type.
    /// </summary>
    public ValidationType ValidationType { get; set; } = ValidationType.Integrity;

    /// <summary>
    /// Gets or sets whether to perform deep validation.
    /// </summary>
    public bool DeepValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Backup validation result.
/// </summary>
public class BackupValidationResult
{
    /// <summary>
    /// Gets or sets the backup ID that was validated.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the validation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets whether the backup is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public ValidationError[] ValidationErrors { get; set; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Gets or sets the validation timestamp.
    /// </summary>
    public DateTime ValidationTime { get; set; }

    /// <summary>
    /// Gets or sets the validation duration.
    /// </summary>
    public TimeSpan ValidationDuration { get; set; }

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
    /// Gets or sets the error type.
    /// </summary>
    public ValidationErrorType ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected item.
    /// </summary>
    public string AffectedItem { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error severity.
    /// </summary>
    public ErrorSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets additional error details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
