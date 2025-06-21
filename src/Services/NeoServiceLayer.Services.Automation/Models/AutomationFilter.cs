namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Filter model for querying automations.
/// </summary>
public class AutomationFilter
{
    /// <summary>
    /// Gets or sets whether to filter by active status.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets the trigger type to filter by.
    /// </summary>
    public AutomationTriggerType? TriggerType { get; set; }

    /// <summary>
    /// Gets or sets the action type to filter by.
    /// </summary>
    public AutomationActionType? ActionType { get; set; }

    /// <summary>
    /// Gets or sets the owner address to filter by.
    /// </summary>
    public string? OwnerAddress { get; set; }

    /// <summary>
    /// Gets or sets the name pattern to filter by.
    /// </summary>
    public string? NamePattern { get; set; }

    /// <summary>
    /// Gets or sets the creation date from filter.
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Gets or sets the creation date to filter.
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the page index for pagination.
    /// </summary>
    public int PageIndex { get; set; } = 0;
}