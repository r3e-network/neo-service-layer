using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Represents a request to create an automation job.
/// </summary>
public class AutomationJobRequest
{
    /// <summary>
    /// Gets or sets the name of the job.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the job.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the address of the job owner.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string OwnerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target contract address.
    /// </summary>
    [Required]
    [StringLength(42)]
    public string TargetContract { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target method to execute.
    /// </summary>
    [Required]
    [StringLength(100)]
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
    /// Gets or sets the condition configuration for the automation.
    /// </summary>
    public Dictionary<string, object> ConditionConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the job should be enabled immediately.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets when the job should expire.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of executions allowed.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxExecutions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retries for failed executions.
    /// </summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;
}
