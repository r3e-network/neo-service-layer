namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Request model for creating a new automation.
/// </summary>
public class CreateAutomationRequest
{
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
    /// Gets or sets the expiration date of the automation.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Response model for create automation operation.
/// </summary>
public class CreateAutomationResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the automation ID.
    /// </summary>
    public string? AutomationId { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}