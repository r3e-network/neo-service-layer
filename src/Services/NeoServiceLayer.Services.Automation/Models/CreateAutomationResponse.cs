namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Response model for creating a new automation.
/// </summary>
public class CreateAutomationResponse
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
    /// Gets or sets the created automation details.
    /// </summary>
    public AutomationJob? Automation { get; set; }

    /// <summary>
    /// Gets or sets when the automation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}