namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Request model for validating automation configuration.
/// </summary>
public class ValidationRequest
{
    /// <summary>
    /// Gets or sets the type of trigger for validation.
    /// </summary>
    public AutomationTriggerType TriggerType { get; set; }

    /// <summary>
    /// Gets or sets the trigger configuration to validate (JSON serialized).
    /// </summary>
    public required string TriggerConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the type of action for validation.
    /// </summary>
    public AutomationActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the action configuration to validate (JSON serialized).
    /// </summary>
    public required string ActionConfiguration { get; set; }

    /// <summary>
    /// Gets or sets additional validation parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Response model for validation result.
/// </summary>
public class ValidationResponse
{
    /// <summary>
    /// Gets or sets whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    public List<string> ValidationWarnings { get; set; } = new();

    /// <summary>
    /// Gets or sets additional validation metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}