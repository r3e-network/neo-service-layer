namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Result of executing an automation.
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string? ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the execution result data.
    /// </summary>
    public object? ResultData { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public AutomationExecutionStatus Status { get; set; }
}
