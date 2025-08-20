using System;

namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Response model for updating an automation.
/// </summary>
public class UpdateAutomationResponse
{
    /// <summary>
    /// Gets or sets the automation ID.
    /// </summary>
    public string AutomationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated automation details.
    /// </summary>
    public AutomationJob? Automation { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the automation was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}