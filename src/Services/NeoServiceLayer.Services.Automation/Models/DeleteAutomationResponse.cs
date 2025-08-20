using System;

namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Response model for delete automation operation.
/// </summary>
public class DeleteAutomationResponse
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
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}