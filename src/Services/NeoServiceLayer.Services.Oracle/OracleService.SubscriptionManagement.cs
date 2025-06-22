using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle.Models;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Subscription management functionality for the Oracle service.
/// </summary>
public partial class OracleService
{
    /// <inheritdoc/>
    public Task<string> SubscribeToFeedAsync(string feedId, IDictionary<string, string> parameters, Func<string, Task> callback)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        var subscriptionId = Guid.NewGuid().ToString();
        var interval = parameters.TryGetValue("interval", out var intervalStr) && int.TryParse(intervalStr, out var intervalValue)
            ? TimeSpan.FromMilliseconds(intervalValue)
            : TimeSpan.FromMinutes(5);

        var subscription = new OracleSubscription
        {
            Id = subscriptionId,
            FeedId = feedId,
            Parameters = new Dictionary<string, string>(parameters),
            Callback = callback,
            Interval = interval,
            LastUpdated = DateTime.MinValue
        };

        _subscriptions[subscriptionId] = subscription;
        UpdateMetric("SubscriptionCount", _subscriptions.Count);
        Logger.LogInformation("Created subscription {SubscriptionId} for feed {FeedId} with interval {Interval}ms",
            subscriptionId, feedId, interval.TotalMilliseconds);

        // Start the subscription
        _ = Task.Run(async () =>
        {
            while (_subscriptions.ContainsKey(subscriptionId) && IsRunning)
            {
                try
                {
                    var data = await GetDataAsync(feedId, parameters);
                    await callback(data);
                    subscription.LastUpdated = DateTime.UtcNow;
                    subscription.SuccessCount++;
                    Logger.LogDebug("Updated subscription {SubscriptionId} for feed {FeedId}",
                        subscriptionId, feedId);
                }
                catch (Exception ex)
                {
                    // Log the error but continue the subscription
                    subscription.FailureCount++;
                    Logger.LogError(ex, "Error in subscription {SubscriptionId} for feed {FeedId}",
                        subscriptionId, feedId);
                }

                await Task.Delay(interval);
            }
        });

        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromFeedAsync(string subscriptionId)
    {
        if (_subscriptions.Remove(subscriptionId))
        {
            UpdateMetric("SubscriptionCount", _subscriptions.Count);
            Logger.LogInformation("Removed subscription {SubscriptionId}", subscriptionId);
            return Task.FromResult(true);
        }

        Logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetAvailableFeedsAsync()
    {
        var feeds = new List<string>();

        foreach (var blockchainType in SupportedBlockchains)
        {
            var dataSources = await GetSupportedDataSourcesAsync(blockchainType);
            feeds.AddRange(dataSources);
        }

        return feeds.Distinct();
    }

    /// <inheritdoc/>
    public async Task<DataFeedMetadata> GetFeedMetadataAsync(string feedId)
    {
        ArgumentException.ThrowIfNullOrEmpty(feedId);

        // Find the data source for this feed
        DataSource? dataSource = null;

        lock (_dataSources)
        {
            dataSource = _dataSources.FirstOrDefault(ds => ds.Url == feedId);
        }

        if (dataSource == null)
        {
            throw new ArgumentException($"Feed {feedId} not found", nameof(feedId));
        }

        return await Task.FromResult(new DataFeedMetadata
        {
            FeedId = feedId,
            Name = dataSource.Description,
            Description = dataSource.Description,
            DataType = "JSON",
            UpdateFrequency = TimeSpan.FromMinutes(5),
            IsActive = true,
            LastUpdated = dataSource.LastAccessedAt ?? DateTime.UtcNow,
            SourceUrl = dataSource.Url,
            ReliabilityScore = 0.95
        });
    }

    /// <summary>
    /// Cancels all active subscriptions.
    /// </summary>
    private void CancelAllSubscriptions()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            Logger.LogInformation("Cancelling subscription {SubscriptionId} for feed {FeedId}",
                subscription.Id, subscription.FeedId);
        }

        _subscriptions.Clear();
        UpdateMetric("SubscriptionCount", 0);
    }
}
