﻿namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Represents the execution of an automation job.
/// </summary>
public class AutomationExecution
{
    /// <summary>
    /// Gets or sets the unique identifier for this execution.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job ID that was executed.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the execution started.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public AutomationExecutionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the execution result.
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the block number where the transaction was confirmed.
    /// </summary>
    public long? BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the gas used for the execution.
    /// </summary>
    public decimal? GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the execution time duration.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the retry attempt number.
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// Gets or sets whether this execution triggered additional jobs.
    /// </summary>
    public bool TriggeredJobs { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the execution.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
