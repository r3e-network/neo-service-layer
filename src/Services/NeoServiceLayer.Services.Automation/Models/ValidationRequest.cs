using System.Collections.Generic;
using System;

namespace NeoServiceLayer.Services.Automation.Models;

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
    public string TriggerConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of action for validation.
    /// </summary>
    public AutomationActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the action configuration to validate (JSON serialized).
    /// </summary>
    public string ActionConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional validation parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}