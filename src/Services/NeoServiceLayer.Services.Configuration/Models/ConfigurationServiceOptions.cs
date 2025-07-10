namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Configuration options for the Configuration service
/// </summary>
public class ConfigurationServiceOptions
{
    /// <summary>
    /// Gets or sets the default storage backend
    /// </summary>
    public string StorageBackend { get; set; } = "InMemory";

    /// <summary>
    /// Gets or sets whether to enable encryption for sensitive values
    /// </summary>
    public bool EnableEncryption { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in seconds
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether to enable hot reload
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum configuration size in KB
    /// </summary>
    public int MaxConfigurationSizeKB { get; set; } = 1024;
}

/// <summary>
/// Request to get configuration
/// </summary>
public class ConfigurationRequest
{
    /// <summary>
    /// Gets or sets the application name
    /// </summary>
    public string Application { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the environment (e.g., dev, staging, prod)
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// Gets or sets the configuration key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include metadata
    /// </summary>
    public bool IncludeMetadata { get; set; } = false;
}
