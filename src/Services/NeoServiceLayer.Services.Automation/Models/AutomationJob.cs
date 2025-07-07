using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Represents an automation job that can be scheduled or triggered.
/// </summary>
public class AutomationJob
{
    /// <summary>
    /// Gets or sets the unique identifier for the job.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the job.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the job.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the address of the job owner.
    /// </summary>
    [Required]
    public string OwnerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type for this job.
    /// </summary>
    [Required]
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the target contract address.
    /// </summary>
    [Required]
    public string TargetContract { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target method to execute.
    /// </summary>
    [Required]
    public string TargetMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters for the method execution.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the trigger configuration for the job.
    /// </summary>
    [Required]
    public AutomationTrigger Trigger { get; set; } = new();

    /// <summary>
    /// Gets or sets the conditions that must be met for execution.
    /// </summary>
    public AutomationCondition[] Conditions { get; set; } = Array.Empty<AutomationCondition>();

    /// <summary>
    /// Gets or sets the current status of the job.
    /// </summary>
    public AutomationJobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets whether the job is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the job was last executed.
    /// </summary>
    public DateTime? LastExecuted { get; set; }

    /// <summary>
    /// Gets or sets when the job should next execute.
    /// </summary>
    public DateTime? NextExecution { get; set; }

    /// <summary>
    /// Gets or sets when the job expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the number of times the job has been executed.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of executions allowed.
    /// </summary>
    public int? MaxExecutions { get; set; }

    /// <summary>
    /// Gets or sets the retry count for failed executions.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retries allowed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
