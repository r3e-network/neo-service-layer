/// <summary>
/// Interface for data feed services that provide external data to the blockchain.
/// </summary>
    /// <summary>
    /// Gets data from a specific feed.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <param name="parameters">Additional parameters for the feed request.</param>
    /// <returns>The data from the feed.</returns>
    /// <summary>
    /// Subscribes to a data feed for real-time updates.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <param name="parameters">Additional parameters for the subscription.</param>
    /// <param name="callback">The callback function to invoke when new data is available.</param>
    /// <returns>The subscription identifier.</returns>
    /// <summary>
    /// Unsubscribes from a data feed.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>True if the unsubscription was successful, false otherwise.</returns>
    /// <summary>
    /// Gets the list of available data feeds.
    /// </summary>
    /// <returns>The list of available feed identifiers.</returns>
    /// <summary>
    /// Gets metadata about a specific data feed.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <returns>The feed metadata.</returns>
/// <summary>
/// Represents metadata about a data feed.
/// </summary>
    /// <summary>
    /// Gets or sets the feed identifier.
    /// </summary>
    /// <summary>
    /// Gets or sets the feed name.
    /// </summary>
    /// <summary>
    /// Gets or sets the feed description.
    /// </summary>
    /// <summary>
    /// Gets or sets the data type provided by the feed.
    /// </summary>
    /// <summary>
    /// Gets or sets the update frequency.
    /// </summary>
    /// <summary>
    /// Gets or sets the supported parameters.
    /// </summary>
    /// <summary>
    /// Gets or sets whether the feed is currently active.
    /// </summary>
    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    /// <summary>
    /// Gets or sets the feed source URL.
    /// </summary>
    /// <summary>
    /// Gets or sets the feed reliability score (0-1).
    /// </summary>
