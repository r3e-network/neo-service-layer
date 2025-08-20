using System;

namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Validation error for configuration validation.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name that failed validation.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the attempted value.
    /// </summary>
    public object? AttemptedValue { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

/// <summary>
/// Validation severity enumeration.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Information level.
    /// </summary>
    Info,

    /// <summary>
    /// Warning level.
    /// </summary>
    Warning,

    /// <summary>
    /// Error level.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error level.
    /// </summary>
    Critical
}