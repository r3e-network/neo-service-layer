using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Represents a condition that must be met for an automation job to execute.
/// </summary>
public class AutomationCondition
{
    /// <summary>
    /// Gets or sets the type of condition.
    /// </summary>
    [Required]
    public AutomationConditionType Type { get; set; }

    /// <summary>
    /// Gets or sets the field to evaluate.
    /// </summary>
    [Required]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comparison operator.
    /// </summary>
    [Required]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value to compare against.
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition expression.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Gets or sets the parameters for the condition.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the logical operator to combine with the next condition.
    /// </summary>
    public string LogicalOperator { get; set; } = "AND";

    /// <summary>
    /// Gets or sets the priority of this condition.
    /// </summary>
    [Range(0, 100)]
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether this condition should be negated.
    /// </summary>
    public bool Negate { get; set; }

    /// <summary>
    /// Gets or sets the timeout for condition evaluation (in seconds).
    /// </summary>
    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether this condition is required or optional.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets a description of what this condition checks.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the condition configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}
