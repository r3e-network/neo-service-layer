namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Request model for updating an automation.
/// </summary>
public class UpdateAutomationRequest
{
    /// <summary>
    /// Gets or sets the automation ID to update.
    /// </summary>
    public required string AutomationId { get; set; }

    /// <summary>
    /// Gets or sets the updated name of the automation.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the updated description of the automation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the automation is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets the updated trigger configuration (JSON serialized).
    /// </summary>
    public string? TriggerConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the updated action configuration (JSON serialized).
    /// </summary>
    public string? ActionConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the updated expiration date of the automation.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Response model for update automation operation.
/// </summary>
public class UpdateAutomationResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}