namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Get configuration history request.
/// </summary>
public class GetConfigurationHistoryRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope? Scope { get; set; }

    /// <summary>
    /// Gets or sets the environment filter.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service filter.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the start time filter.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time filter.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to include values in the history.
    /// </summary>
    public bool IncludeValues { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration history result.
/// </summary>
public class ConfigurationHistoryResult
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the history entries.
    /// </summary>
    public ConfigurationHistoryEntry[] HistoryEntries { get; set; } = Array.Empty<ConfigurationHistoryEntry>();

    /// <summary>
    /// Gets or sets the total count of history entries.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets whether there are more results.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration history entry.
/// </summary>
public class ConfigurationHistoryEntry
{
    /// <summary>
    /// Gets or sets the history entry ID.
    /// </summary>
    public string EntryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public ConfigurationOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the old value.
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public ConfigurationValueType ValueType { get; set; }

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the environment.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the user who made the change.
    /// </summary>
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets the change reason.
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the changed at timestamp.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the change type.
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
