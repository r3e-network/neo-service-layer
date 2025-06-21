namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Represents detailed information about an automation.
/// </summary>
public class AutomationInfo
{
    /// <summary>
    /// Gets or sets the automation ID.
    /// </summary>
    public required string AutomationId { get; set; }

    /// <summary>
    /// Gets or sets the name of the automation.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the automation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of trigger for the automation.
    /// </summary>
    public AutomationTriggerType TriggerType { get; set; }

    /// <summary>
    /// Gets or sets the trigger configuration (JSON serialized).
    /// </summary>
    public required string TriggerConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the type of action for the automation.
    /// </summary>
    public AutomationActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the action configuration (JSON serialized).
    /// </summary>
    public required string ActionConfiguration { get; set; }

    /// <summary>
    /// Gets or sets whether the automation is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the owner address.
    /// </summary>
    public string? OwnerAddress { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the automation.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the last execution timestamp.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled execution timestamp.
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }

    /// <summary>
    /// Gets or sets the execution count.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public AutomationJobStatus Status { get; set; }
}