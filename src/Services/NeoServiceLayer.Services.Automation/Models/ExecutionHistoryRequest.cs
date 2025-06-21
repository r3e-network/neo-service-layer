namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Request model for querying execution history.
/// </summary>
public class ExecutionHistoryRequest
{
    /// <summary>
    /// Gets or sets the automation ID to query history for.
    /// </summary>
    public required string AutomationId { get; set; }

    /// <summary>
    /// Gets or sets the from date for the history query.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date for the history query.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the execution status to filter by.
    /// </summary>
    public AutomationExecutionStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page index for pagination.
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to include execution details.
    /// </summary>
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Response model for execution history query.
/// </summary>
public class ExecutionHistoryResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the executions.
    /// </summary>
    public List<AutomationExecution> Executions { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of executions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the page index.
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}