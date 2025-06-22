using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.EventSubscription;

/// <summary>
/// Core implementation of the Event Subscription service.
/// </summary>
public partial class EventSubscriptionService : EnclaveBlockchainServiceBase, IEventSubscriptionService
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly Dictionary<string, EventSubscription> _subscriptionCache = new();
    private readonly Dictionary<string, List<EventData>> _eventCache = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSubscriptionService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    public EventSubscriptionService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<EventSubscriptionService> logger)
        : base("EventSubscription", "Event Subscription Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Add capabilities
        AddCapability<IEventSubscriptionService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxSubscriptionCount", _configuration.GetValue("EventSubscription:MaxSubscriptionCount", "1000"));
        SetMetadata("MaxEventsPerSubscription", _configuration.GetValue("EventSubscription:MaxEventsPerSubscription", "10000"));
        SetMetadata("SupportedEventTypes", "Block,Transaction,Contract,Token,Custom");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Event Subscription Service...");

            // Initialize service-specific components
            await RefreshSubscriptionCacheAsync();

            Logger.LogInformation("Event Subscription Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Event Subscription Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Event Subscription Service enclave...");
            await _enclaveManager.InitializeAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Event Subscription Service enclave.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Event Subscription Service...");

            // Load existing subscriptions from the enclave
            await RefreshSubscriptionCacheAsync();

            // Start the event processor
            await StartEventProcessorAsync();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Event Subscription Service.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Event Subscription Service...");
            _subscriptionCache.Clear();
            _eventCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Event Subscription Service.");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Checks if the service supports the specified blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if supported, false otherwise.</returns>
    public new bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return blockchainType == BlockchainType.NeoN3 || blockchainType == BlockchainType.NeoX;
    }

    /// <summary>
    /// Updates a metric value.
    /// </summary>
    /// <param name="metricName">The metric name.</param>
    /// <param name="value">The metric value.</param>
    public new void UpdateMetric(string metricName, object value)
    {
        SetMetadata(metricName, value.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Validates common operation parameters.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    private void ValidateOperation(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }
    }

    /// <summary>
    /// Increments request counters.
    /// </summary>
    private void IncrementRequestCounters()
    {
        _requestCount++;
        _lastRequestTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Records successful operation.
    /// </summary>
    private void RecordSuccess()
    {
        _successCount++;
        UpdateMetric("LastSuccessTime", DateTime.UtcNow);
    }

    /// <summary>
    /// Records failed operation.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    private void RecordFailure(Exception ex)
    {
        _failureCount++;
        UpdateMetric("LastFailureTime", DateTime.UtcNow);
        UpdateMetric("LastErrorMessage", ex.Message);
    }

    /// <summary>
    /// Refreshes the subscription cache from the enclave.
    /// </summary>
    private async Task RefreshSubscriptionCacheAsync()
    {
        try
        {
            // Get all subscriptions from the enclave
            string subscriptionsJson = await _enclaveManager.ExecuteJavaScriptAsync("getAllSubscriptions()");

            if (!string.IsNullOrEmpty(subscriptionsJson))
            {
                var subscriptions = JsonSerializer.Deserialize<Dictionary<string, EventSubscription>>(subscriptionsJson);
                if (subscriptions != null)
                {
                    lock (_subscriptionCache)
                    {
                        _subscriptionCache.Clear();
                        foreach (var kvp in subscriptions)
                        {
                            _subscriptionCache[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            Logger.LogDebug("Refreshed subscription cache with {Count} items", _subscriptionCache.Count);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to refresh subscription cache");
        }
    }

    /// <summary>
    /// Starts the event processor.
    /// </summary>
    private async Task StartEventProcessorAsync()
    {
        try
        {
            // Initialize event processing in the enclave
            await _enclaveManager.ExecuteJavaScriptAsync("startEventProcessor()");
            Logger.LogDebug("Event processor started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start event processor");
            throw;
        }
    }

    /// <summary>
    /// Gets service statistics.
    /// </summary>
    /// <returns>Service statistics.</returns>
    public EventSubscriptionStatistics GetStatistics()
    {
        lock (_subscriptionCache)
        {
            return new EventSubscriptionStatistics
            {
                TotalSubscriptions = _subscriptionCache.Count,
                ActiveSubscriptions = _subscriptionCache.Values.Count(s => s.Enabled),
                TotalEvents = _eventCache.Values.Sum(events => events.Count),
                RequestCount = _requestCount,
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                LastRequestTime = _lastRequestTime,
                SuccessRate = _requestCount > 0 ? (double)_successCount / _requestCount : 0.0
            };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AcknowledgeEventAsync(string subscriptionId, string eventId, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);
        IncrementRequestCounters();

        try
        {
            var request = new
            {
                SubscriptionId = subscriptionId,
                EventId = eventId,
                BlockchainType = blockchainType.ToString(),
                Timestamp = DateTime.UtcNow
            };

            var result = await _enclaveManager.ExecuteJavaScriptAsync($"acknowledgeEvent({JsonSerializer.Serialize(request)})");
            var success = result?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            if (success)
            {
                RecordSuccess();
                Logger.LogInformation("Acknowledged event {EventId} for subscription {SubscriptionId}", eventId, subscriptionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Failed to acknowledge event {EventId} for subscription {SubscriptionId}", eventId, subscriptionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> TriggerTestEventAsync(string subscriptionId, EventData eventData, BlockchainType blockchainType)
    {
        ValidateOperation(blockchainType);
        IncrementRequestCounters();

        try
        {
            var request = new
            {
                SubscriptionId = subscriptionId,
                EventData = eventData,
                BlockchainType = blockchainType.ToString(),
                Timestamp = DateTime.UtcNow
            };

            var result = await _enclaveManager.ExecuteJavaScriptAsync($"triggerTestEvent({JsonSerializer.Serialize(request)})");
            var eventId = JsonSerializer.Deserialize<string>(result ?? "\"\"") ?? string.Empty;

            if (!string.IsNullOrEmpty(eventId))
            {
                RecordSuccess();
                Logger.LogInformation("Triggered test event {EventId} for subscription {SubscriptionId}", eventId, subscriptionId);
            }

            return eventId;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Failed to trigger test event for subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check enclave health
            if (!IsEnclaveInitialized)
                return ServiceHealth.Degraded;

            // Check if we can execute basic operations
            var healthCheck = await _enclaveManager.ExecuteJavaScriptAsync("healthCheck()");
            var isHealthy = healthCheck?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            // Update health metrics
            UpdateMetric("LastHealthCheck", DateTime.UtcNow);
            UpdateMetric("IsHealthy", isHealthy);

            return isHealthy ? ServiceHealth.Healthy : ServiceHealth.Degraded;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");
            return ServiceHealth.Unhealthy;
        }
    }
}
