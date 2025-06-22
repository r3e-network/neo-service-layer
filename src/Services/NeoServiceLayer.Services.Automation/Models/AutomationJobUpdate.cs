using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Represents a request to update an existing automation job.
/// </summary>
public class AutomationJobUpdate
{
    /// <summary>
    /// Gets or sets the updated name of the job.
    /// </summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the updated description of the job.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated trigger configuration for the job.
    /// </summary>
    public AutomationTrigger? Trigger { get; set; }

    /// <summary>
    /// Gets or sets the updated conditions that must be met for execution.
    /// </summary>
    public AutomationCondition[]? Conditions { get; set; }

    /// <summary>
    /// Gets or sets whether the job should be enabled or disabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the updated expiration date for the job.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the updated maximum number of executions allowed.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxExecutions { get; set; }

    /// <summary>
    /// Gets or sets the updated maximum number of retries for failed executions.
    /// </summary>
    [Range(0, 10)]
    public int? MaxRetries { get; set; }
}
