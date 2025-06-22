using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Represents a trigger that determines when an automation job should execute.
/// </summary>
public class AutomationTrigger
{
    /// <summary>
    /// Gets or sets the type of trigger.
    /// </summary>
    [Required]
    public AutomationTriggerType Type { get; set; }

    /// <summary>
    /// Gets or sets the schedule for time-based triggers (cron expression).
    /// </summary>
    public string? Schedule { get; set; }

    /// <summary>
    /// Gets or sets the event type for event-based triggers.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets the contract address to monitor for events.
    /// </summary>
    public string? ContractAddress { get; set; }

    /// <summary>
    /// Gets or sets the event filter parameters.
    /// </summary>
    public Dictionary<string, object>? EventFilter { get; set; }

    /// <summary>
    /// Gets or sets the delay before execution (in seconds).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum delay for random scheduling (in seconds).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? MaxDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets whether the trigger should repeat.
    /// </summary>
    public bool IsRepeating { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval between repeats (in seconds).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? RepeatIntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of repeats.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxRepeats { get; set; }

    /// <summary>
    /// Gets or sets additional trigger configuration.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}
