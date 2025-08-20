namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Request model for creating a new automation.
/// </summary>
public class CreateAutomationRequest
{
    /// <summary>
    /// Gets or sets the name of the automation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the automation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of trigger for the automation.
    /// </summary>
    public AutomationTriggerType TriggerType { get; set; }

    /// <summary>
    /// Gets or sets the trigger configuration (JSON serialized).
    /// </summary>
    public string TriggerConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of action for the automation.
    /// </summary>
    public AutomationActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the action configuration (JSON serialized).
    /// </summary>
    public string ActionConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the automation is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the owner address.
    /// </summary>
    public string OwnerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date of the automation.
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets when the automation expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

