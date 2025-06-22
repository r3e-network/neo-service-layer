namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Response model for pause/resume automation operations.
/// </summary>
public class PauseResumeResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the current status of the automation.
    /// </summary>
    public AutomationJobStatus? CurrentStatus { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime OperationTime { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
