using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Filter for querying automation jobs.
/// </summary>
public class AutomationFilter
{
    /// <summary>
    /// Gets or sets the owner address filter.
    /// </summary>
    public string? OwnerAddress { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public AutomationJobStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets whether to include only enabled jobs.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the minimum created date.
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Gets or sets the maximum created date.
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Gets or sets the target contract filter.
    /// </summary>
    public string? TargetContract { get; set; }

    /// <summary>
    /// Gets or sets the target method filter.
    /// </summary>
    public string? TargetMethod { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the page index for pagination (alias for Page).
    /// </summary>
    public int PageIndex
    {
        get => Page;
        set => Page = value;
    }

    /// <summary>
    /// Gets or sets whether to filter by active status (alias for IsEnabled).
    /// </summary>
    public bool? IsActive
    {
        get => IsEnabled;
        set => IsEnabled = value;
    }

    /// <summary>
    /// Gets or sets the trigger type filter.
    /// </summary>
    public AutomationTriggerType? TriggerType { get; set; }

    /// <summary>
    /// Gets or sets the name pattern filter.
    /// </summary>
    public string? NamePattern { get; set; }
}

/// <summary>
/// Automation job information for listing and display.
/// </summary>
public class AutomationInfo
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the automation ID (alias for JobId).
    /// </summary>
    public string AutomationId
    {
        get => JobId;
        set => JobId = value;
    }

    /// <summary>
    /// Gets or sets the job name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the owner address.
    /// </summary>
    public string OwnerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target contract address.
    /// </summary>
    public string TargetContract { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target method.
    /// </summary>
    public string TargetMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    public AutomationJobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets whether the job is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the job was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the job last executed.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets when the job will next execute.
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets when the job expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the trigger type.
    /// </summary>
    public AutomationTriggerType TriggerType { get; set; }

    /// <summary>
    /// Gets or sets the trigger configuration.
    /// </summary>
    public Dictionary<string, object> TriggerConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets the action type.
    /// </summary>
    public AutomationActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the action configuration.
    /// </summary>
    public Dictionary<string, object> ActionConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the automation is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when the automation was updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Result of an automation execution.
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets when the execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the execution completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if successful.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the gas consumed.
    /// </summary>
    public long? GasConsumed { get; set; }

    /// <summary>
    /// Gets or sets any error message if execution failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the execution logs.
    /// </summary>
    public List<string> Logs { get; set; } = new();

    /// <summary>
    /// Gets or sets additional execution metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message (alias for Error).
    /// </summary>
    public string? ErrorMessage 
    { 
        get => Error; 
        set => Error = value; 
    }

    /// <summary>
    /// Gets or sets when the execution occurred (alias for StartedAt).
    /// </summary>
    public DateTime ExecutedAt 
    { 
        get => StartedAt; 
        set => StartedAt = value; 
    }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public AutomationExecutionStatus Status { get; set; }
}

/// <summary>
/// Request for execution history.
/// </summary>
public class ExecutionHistoryRequest
{
    /// <summary>
    /// Gets or sets the job ID to get history for.
    /// </summary>
    [Required]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the automation ID (alias for JobId).
    /// </summary>
    public string AutomationId
    {
        get => JobId;
        set => JobId = value;
    }

    /// <summary>
    /// Gets or sets the start date filter.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the from date filter (alias for StartDate).
    /// </summary>
    public DateTime? FromDate
    {
        get => StartDate;
        set => StartDate = value;
    }

    /// <summary>
    /// Gets or sets the to date filter (alias for EndDate).
    /// </summary>
    public DateTime? ToDate
    {
        get => EndDate;
        set => EndDate = value;
    }

    /// <summary>
    /// Gets or sets whether to include only successful executions.
    /// </summary>
    public bool? SuccessOnly { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page index for pagination (alias for Page).
    /// </summary>
    public int PageIndex
    {
        get => Page;
        set => Page = value;
    }

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response for pause/resume operations.
/// </summary>
public class PauseResumeResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the job ID that was paused/resumed.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new status of the job.
    /// </summary>
    public AutomationJobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the status change occurred.
    /// </summary>
    public DateTime StatusChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any error message if operation failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Request for validation operations with extended options.
/// </summary>
public class ExtendedValidationRequest
{
    /// <summary>
    /// Gets or sets the job configuration to validate.
    /// </summary>
    [Required]
    public AutomationJobRequest JobRequest { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to perform deep validation.
    /// </summary>
    public bool DeepValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to validate against blockchain state.
    /// </summary>
    public bool ValidateBlockchainState { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to simulate execution.
    /// </summary>
    public bool SimulateExecution { get; set; } = false;
}