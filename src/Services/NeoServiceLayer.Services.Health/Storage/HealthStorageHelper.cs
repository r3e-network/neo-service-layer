using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Health.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text.Json;


namespace NeoServiceLayer.Services.Health.Storage;

/// <summary>
/// Helper class for managing Health Service persistent storage operations.
/// </summary>
public class HealthStorageHelper
{
    private readonly IStorageService _storageService;
    private readonly ILogger<HealthStorageHelper> Logger;

    // Storage keys
    private const string NodesStorageKey = "health:nodes";
    private const string AlertsStorageKey = "health:alerts";
    private const string ThresholdsStorageKey = "health:thresholds";

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthStorageHelper"/> class.
    /// </summary>
    /// <param name="storageService">The storage service.</param>
    /// <param name="logger">The logger.</param>
    public HealthStorageHelper(IStorageService storageService, ILogger<HealthStorageHelper> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads monitored nodes from storage.
    /// </summary>
    /// <returns>Dictionary of monitored nodes.</returns>
    public async Task<Dictionary<string, NodeHealthReport>> LoadMonitoredNodesAsync()
    {
        try
        {
            var data = await _storageService.GetDataAsync(NodesStorageKey, BlockchainType.NeoN3);
            if (data != null && data.Length > 0)
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var nodes = JsonSerializer.Deserialize<Dictionary<string, NodeHealthReport>>(json);
                return nodes ?? new Dictionary<string, NodeHealthReport>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not load monitored nodes from storage");
        }

        return new Dictionary<string, NodeHealthReport>();
    }

    /// <summary>
    /// Loads active alerts from storage.
    /// </summary>
    /// <returns>Dictionary of active alerts.</returns>
    public async Task<Dictionary<string, HealthAlert>> LoadActiveAlertsAsync()
    {
        try
        {
            var data = await _storageService.GetDataAsync(AlertsStorageKey, BlockchainType.NeoN3);
            if (data != null && data.Length > 0)
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var alerts = JsonSerializer.Deserialize<Dictionary<string, HealthAlert>>(json);
                return alerts ?? new Dictionary<string, HealthAlert>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not load active alerts from storage");
        }

        return new Dictionary<string, HealthAlert>();
    }

    /// <summary>
    /// Loads node thresholds from storage.
    /// </summary>
    /// <returns>Dictionary of node thresholds.</returns>
    public async Task<Dictionary<string, HealthThreshold>> LoadNodeThresholdsAsync()
    {
        try
        {
            var data = await _storageService.GetDataAsync(ThresholdsStorageKey, BlockchainType.NeoN3);
            if (data != null && data.Length > 0)
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var thresholds = JsonSerializer.Deserialize<Dictionary<string, HealthThreshold>>(json);
                return thresholds ?? new Dictionary<string, HealthThreshold>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not load node thresholds from storage");
        }

        return new Dictionary<string, HealthThreshold>();
    }

    /// <summary>
    /// Persists monitored nodes to storage.
    /// </summary>
    /// <param name="nodes">The nodes to persist.</param>
    public async Task PersistMonitoredNodesAsync(Dictionary<string, NodeHealthReport> nodes)
    {
        try
        {
            var json = JsonSerializer.Serialize(nodes);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new StorageOptions
            {
                Encrypt = true,
                Compress = true
            };

            await _storageService.StoreDataAsync(NodesStorageKey, data, options, BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting monitored nodes");
        }
    }

    /// <summary>
    /// Persists active alerts to storage.
    /// </summary>
    /// <param name="alerts">The alerts to persist.</param>
    public async Task PersistActiveAlertsAsync(Dictionary<string, HealthAlert> alerts)
    {
        try
        {
            var json = JsonSerializer.Serialize(alerts);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new StorageOptions
            {
                Encrypt = true,
                Compress = true
            };

            await _storageService.StoreDataAsync(AlertsStorageKey, data, options, BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting active alerts");
        }
    }

    /// <summary>
    /// Persists node thresholds to storage.
    /// </summary>
    /// <param name="thresholds">The thresholds to persist.</param>
    public async Task PersistNodeThresholdsAsync(Dictionary<string, HealthThreshold> thresholds)
    {
        try
        {
            var json = JsonSerializer.Serialize(thresholds);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new StorageOptions
            {
                Encrypt = true,
                Compress = true
            };

            await _storageService.StoreDataAsync(ThresholdsStorageKey, data, options, BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting node thresholds");
        }
    }
}
