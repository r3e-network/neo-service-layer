using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using System.Text.Json;

namespace NeoServiceLayer.Services.Health;

/// <summary>
/// Core implementation of the Health Service that provides Neo N3 consensus node health monitoring capabilities.
/// </summary>
public partial class HealthService : EnclaveBlockchainServiceBase, IHealthService, IDisposable
{
    private readonly IStorageService _storageService;
    private readonly Dictionary<string, NodeHealthReport> _monitoredNodes = new();
    private readonly Dictionary<string, HealthAlert> _activeAlerts = new();
    private readonly Dictionary<string, HealthThreshold> _nodeThresholds = new();
    private readonly object _nodesLock = new();
    private readonly object _alertsLock = new();
    private readonly Timer _monitoringTimer;

    // Storage keys
    private const string NodesStorageKey = "health:nodes";
    private const string AlertsStorageKey = "health:alerts";
    private const string ThresholdsStorageKey = "health:thresholds";

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    public IServiceConfiguration? Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="storageService">The storage service.</param>
    /// <param name="configuration">The service configuration.</param>
    public HealthService(ILogger<HealthService> logger, IEnclaveManager enclaveManager, IStorageService storageService, IServiceConfiguration? configuration = null)
        : base("HealthService", "Neo N3 consensus node health monitoring service", "1.0.0", logger, new[] { BlockchainType.NeoN3 }, enclaveManager)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        Configuration = configuration;

        // Initialize monitoring timer (runs every 30 seconds)
        _monitoringTimer = new Timer(MonitorNodes, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        AddCapability<IHealthService>();
        AddDependency(new ServiceDependency("OracleService", false, "1.0.0"));
        AddDependency(new ServiceDependency("StorageService", true, "1.0.0"));
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Health Service...");

        // Load persisted data
        await LoadPersistedDataAsync();

        Logger.LogInformation("Health Service started successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Health Service...");

        // Persist current state
        await PersistAllDataAsync();

        // Dispose timer
        _monitoringTimer?.Dispose();

        Logger.LogInformation("Health Service stopped successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var nodeCount = _monitoredNodes.Count;
        var onlineCount = _monitoredNodes.Values.Count(n => n.Status == NodeStatus.Online);

        Logger.LogDebug("Health service health check: {OnlineCount}/{NodeCount} nodes online",
            onlineCount, nodeCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Health Service");

        try
        {
            // Load persisted data
            await LoadPersistedDataAsync();

            Logger.LogInformation("Health Service initialized successfully with {NodeCount} nodes and {AlertCount} alerts",
                _monitoredNodes.Count, _activeAlerts.Count);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Health Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Health Service enclave operations");

        try
        {
            // Initialize health monitoring algorithms in the enclave
            var initResult = await _enclaveManager!.ExecuteJavaScriptAsync("initializeHealthMonitoring()");

            Logger.LogDebug("Enclave health monitoring initialized: {Result}", initResult);
            Logger.LogInformation("Health Service enclave operations initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Health Service enclave operations");
            return false;
        }
    }

    /// <summary>
    /// Checks if the service supports the specified blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if supported, false otherwise.</returns>
    private new static bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return blockchainType == BlockchainType.NeoN3;
    }

    /// <summary>
    /// Calculates overall network health score.
    /// </summary>
    /// <returns>Network health score between 0 and 1.</returns>
    private double CalculateNetworkHealth()
    {
        lock (_nodesLock)
        {
            if (_monitoredNodes.Count == 0)
                return 0.0;

            var onlineNodes = _monitoredNodes.Values.Count(n => n.Status == NodeStatus.Online);
            var healthyNodes = _monitoredNodes.Values.Count(n =>
                n.Status == NodeStatus.Online && n.UptimePercentage >= 95.0);

            var onlineRatio = (double)onlineNodes / _monitoredNodes.Count;
            var healthyRatio = (double)healthyNodes / _monitoredNodes.Count;

            // Weight online status more heavily than health metrics
            return (onlineRatio * 0.7) + (healthyRatio * 0.3);
        }
    }

    /// <summary>
    /// Loads persisted data from storage.
    /// </summary>
    private async Task LoadPersistedDataAsync()
    {
        try
        {
            // Load monitored nodes
            try
            {
                var nodesData = await _storageService.RetrieveDataAsync(NodesStorageKey, BlockchainType.NeoN3);
                var nodesJson = System.Text.Encoding.UTF8.GetString(nodesData);
                var nodes = JsonSerializer.Deserialize<Dictionary<string, NodeHealthReport>>(nodesJson);

                if (nodes != null)
                {
                    lock (_nodesLock)
                    {
                        foreach (var kvp in nodes)
                        {
                            _monitoredNodes[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            // Load active alerts
            try
            {
                var alertsData = await _storageService.RetrieveDataAsync(AlertsStorageKey, BlockchainType.NeoN3);
                var alertsJson = System.Text.Encoding.UTF8.GetString(alertsData);
                var alerts = JsonSerializer.Deserialize<Dictionary<string, HealthAlert>>(alertsJson);

                if (alerts != null)
                {
                    lock (_alertsLock)
                    {
                        foreach (var kvp in alerts)
                        {
                            _activeAlerts[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            // Load node thresholds
            try
            {
                var thresholdsData = await _storageService.RetrieveDataAsync(ThresholdsStorageKey, BlockchainType.NeoN3);
                var thresholdsJson = System.Text.Encoding.UTF8.GetString(thresholdsData);
                var thresholds = JsonSerializer.Deserialize<Dictionary<string, HealthThreshold>>(thresholdsJson);

                if (thresholds != null)
                {
                    lock (_nodesLock)
                    {
                        foreach (var kvp in thresholds)
                        {
                            _nodeThresholds[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Data doesn't exist yet, which is fine for first run
            }

            Logger.LogInformation("Loaded persisted health data: {NodeCount} nodes, {AlertCount} alerts, {ThresholdCount} thresholds",
                _monitoredNodes.Count, _activeAlerts.Count, _nodeThresholds.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load persisted health data");
        }
    }

    /// <summary>
    /// Persists all data to storage.
    /// </summary>
    private async Task PersistAllDataAsync()
    {
        await PersistMonitoredNodesAsync();
        await PersistActiveAlertsAsync();
        await PersistNodeThresholdsAsync();
    }

    /// <summary>
    /// Persists monitored nodes to storage.
    /// </summary>
    protected async Task PersistMonitoredNodesAsync()
    {
        try
        {
            Dictionary<string, NodeHealthReport> nodesToPersist;
            lock (_nodesLock)
            {
                nodesToPersist = new Dictionary<string, NodeHealthReport>(_monitoredNodes);
            }

            var json = JsonSerializer.Serialize(nodesToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            await _storageService.StoreDataAsync(NodesStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist monitored nodes");
        }
    }

    /// <summary>
    /// Persists active alerts to storage.
    /// </summary>
    protected async Task PersistActiveAlertsAsync()
    {
        try
        {
            Dictionary<string, HealthAlert> alertsToPersist;
            lock (_alertsLock)
            {
                alertsToPersist = new Dictionary<string, HealthAlert>(_activeAlerts);
            }

            var json = JsonSerializer.Serialize(alertsToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            await _storageService.StoreDataAsync(AlertsStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist active alerts");
        }
    }

    /// <summary>
    /// Persists node thresholds to storage.
    /// </summary>
    protected async Task PersistNodeThresholdsAsync()
    {
        try
        {
            Dictionary<string, HealthThreshold> thresholdsToPersist;
            lock (_nodesLock)
            {
                thresholdsToPersist = new Dictionary<string, HealthThreshold>(_nodeThresholds);
            }

            var json = JsonSerializer.Serialize(thresholdsToPersist);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            await _storageService.StoreDataAsync(ThresholdsStorageKey, data, new StorageOptions(), BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist node thresholds");
        }
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected new void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            _monitoringTimer?.Dispose();
        }
    }
}
