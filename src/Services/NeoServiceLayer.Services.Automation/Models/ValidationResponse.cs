using System.Collections.Generic;
using System;

namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Represents the response from an automation validation request.
/// </summary>
public class ValidationResponse
{
    /// <summary>
    /// Gets or sets whether the automation configuration is valid.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata about the validation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation timestamp.
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any warnings that don't prevent validation but should be noted.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}