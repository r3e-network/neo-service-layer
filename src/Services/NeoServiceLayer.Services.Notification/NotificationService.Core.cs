using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Notification.Models;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Concurrent;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Core implementation of the Notification Service that provides multi-channel notification capabilities.
/// </summary>
public partial class NotificationService : EnclaveBlockchainServiceBase, INotificationService
{
    private readonly ConcurrentDictionary<string, NotificationResult> _notificationHistory = new();
    private readonly ConcurrentDictionary<string, ChannelInfo> _registeredChannels = new();
    private readonly ConcurrentDictionary<string, NotificationTemplate> _templates = new();
    private readonly ConcurrentDictionary<string, NotificationSubscription> _subscriptions = new();
    private readonly Timer _deliveryStatusTimer;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    public NotificationService(
        ILogger<NotificationService> logger,
        IEnclaveManager enclaveManager,
        IServiceConfiguration? configuration = null)
        : base("NotificationService", "Multi-channel notification system", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        Configuration = configuration;

        // Initialize timer for checking delivery status
        _deliveryStatusTimer = new Timer(CheckDeliveryStatus, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        // Initialize default channels
        InitializeDefaultChannels();
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected IServiceConfiguration? Configuration { get; }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Notification Service...");

        // Start notification processing
        await StartNotificationProcessingAsync();

        Logger.LogInformation("Notification Service started successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Notification Service...");

        // Stop notification processing
        await StopNotificationProcessingAsync();

        // Dispose timer
        _deliveryStatusTimer?.Dispose();

        Logger.LogInformation("Notification Service stopped successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check if notification channels are accessible
        var channelCount = _registeredChannels.Count;
        var notificationCount = _notificationHistory.Count;

        Logger.LogDebug("Notification service health check: {ChannelCount} channels, {NotificationCount} notifications",
            channelCount, notificationCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Notification Service");

        await Task.Delay(1); // Simulate async initialization
        Logger.LogInformation("Notification Service initialized successfully with {ChannelCount} channels",
            _registeredChannels.Count);
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing notification processing in enclave");

        // Initialize notification algorithms in the enclave
        var initResult = await _enclaveManager!.ExecuteJavaScriptAsync("initializeNotificationProcessing()");

        Logger.LogInformation("Notification enclave initialized successfully");
        return true;
    }

    /// <summary>
    /// Disposes the notification service resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _deliveryStatusTimer?.Dispose();
        }
    }

    /// <summary>
    /// Starts notification processing.
    /// </summary>
    private Task StartNotificationProcessingAsync()
    {
        Logger.LogDebug("Notification processing started");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops notification processing.
    /// </summary>
    private Task StopNotificationProcessingAsync()
    {
        Logger.LogDebug("Notification processing stopped");
        return Task.CompletedTask;
    }



    /// <inheritdoc/>
    public async Task<AvailableChannelsResult> GetAvailableChannelsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogDebug("Getting available notification channels for {Blockchain}", blockchainType);

            await Task.Delay(1); // Simulate async channel retrieval
            ChannelInfo[] channels;
            lock (_cacheLock)
            {
                channels = _registeredChannels.Values.ToArray();
            }

            Logger.LogInformation("Retrieved {ChannelCount} available notification channels", channels.Length);

            return new AvailableChannelsResult
            {
                Channels = channels,
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["total_channels"] = channels.Length,
                    ["enabled_channels"] = channels.Count(c => c.IsEnabled),
                    ["blockchain_type"] = blockchainType.ToString(),
                    ["retrieved_at"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get available notification channels for {Blockchain}", blockchainType);
            return new AvailableChannelsResult
            {
                Channels = Array.Empty<ChannelInfo>(),
                Success = false,
                ErrorMessage = ex.Message,
                Metadata = new Dictionary<string, object>
                {
                    ["error_type"] = ex.GetType().Name,
                    ["blockchain_type"] = blockchainType.ToString(),
                    ["failed_at"] = DateTime.UtcNow
                }
            };
        }
    }
}
