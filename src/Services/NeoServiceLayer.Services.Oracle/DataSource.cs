using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Represents a data source.
/// </summary>
public class DataSource
{
    /// <summary>
    /// Gets or sets the URL of the data source.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the data source.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last accessed timestamp.
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the access count.
    /// </summary>
    public int AccessCount { get; set; }
}
