namespace NeoServiceLayer.Core;

/// <summary>
/// Interface for data feed services that provide external data to the blockchain.
/// </summary>
public interface IDataFeedService
{
    /// <summary>
    /// Gets data from a specific feed.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <param name="parameters">Additional parameters for the feed request.</param>
    /// <returns>The data from the feed.</returns>
    Task<string> GetDataAsync(string feedId, IDictionary<string, string> parameters);

    /// <summary>
    /// Subscribes to a data feed for real-time updates.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <param name="parameters">Additional parameters for the subscription.</param>
    /// <param name="callback">The callback function to invoke when new data is available.</param>
    /// <returns>The subscription identifier.</returns>
    Task<string> SubscribeToFeedAsync(string feedId, IDictionary<string, string> parameters, Func<string, Task> callback);

    /// <summary>
    /// Unsubscribes from a data feed.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>True if the unsubscription was successful, false otherwise.</returns>
    Task<bool> UnsubscribeFromFeedAsync(string subscriptionId);

    /// <summary>
    /// Gets the list of available data feeds.
    /// </summary>
    /// <returns>The list of available feed identifiers.</returns>
    Task<IEnumerable<string>> GetAvailableFeedsAsync();

    /// <summary>
    /// Gets metadata about a specific data feed.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <returns>The feed metadata.</returns>
    Task<DataFeedMetadata> GetFeedMetadataAsync(string feedId);
}

/// <summary>
/// Represents metadata about a data feed.
/// </summary>
public class DataFeedMetadata
{
    /// <summary>
    /// Gets or sets the feed identifier.
    /// </summary>
    public string FeedId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type provided by the feed.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update frequency.
    /// </summary>
    public TimeSpan UpdateFrequency { get; set; }

    /// <summary>
    /// Gets or sets the supported parameters.
    /// </summary>
    public Dictionary<string, string> SupportedParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the feed is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the feed source URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed reliability score (0-1).
    /// </summary>
    public double ReliabilityScore { get; set; } = 1.0;
}
