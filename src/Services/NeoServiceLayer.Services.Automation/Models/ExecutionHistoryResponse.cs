using System.Collections.Generic;

namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Response model for execution history requests.
/// </summary>
public class ExecutionHistoryResponse
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
    /// Gets or sets the execution history.
    /// </summary>
    public List<AutomationExecution> ExecutionHistory { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of executions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the executions (alias for ExecutionHistory).
    /// </summary>
    public List<AutomationExecution> Executions 
    { 
        get => ExecutionHistory; 
        set => ExecutionHistory = value; 
    }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the page index.
    /// </summary>
    public int PageIndex { get; set; }
}