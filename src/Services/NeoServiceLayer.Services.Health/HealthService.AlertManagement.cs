using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Health;

/// <summary>
/// Alert management operations for the Health Service.
/// </summary>
public partial class HealthService
{
    /// <inheritdoc/>
    public Task<IEnumerable<HealthAlert>> GetActiveAlertsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        lock (_alertsLock)
        {
            return Task.FromResult<IEnumerable<HealthAlert>>(_activeAlerts.Values.Where(a => !a.IsResolved).ToList());
        }
    }

    /// <summary>
    /// Checks health thresholds and creates alerts if necessary.
    /// </summary>
    /// <param name="healthReport">The health report to check.</param>
    private async Task CheckThresholdsAndCreateAlertsAsync(NodeHealthReport healthReport)
    {
        HealthThreshold? threshold = null;

        lock (_nodesLock)
        {
            _nodeThresholds.TryGetValue(healthReport.NodeAddress, out threshold);
        }

        threshold ??= new HealthThreshold(); // Use default thresholds

        var alerts = new List<HealthAlert>();

        // Check response time threshold
        if (healthReport.ResponseTime > threshold.MaxResponseTime)
        {
            alerts.Add(new HealthAlert
            {
                Id = Guid.NewGuid().ToString(),
                NodeAddress = healthReport.NodeAddress,
                Severity = HealthAlertSeverity.Warning,
                AlertType = "HighResponseTime",
                Message = $"Node response time ({healthReport.ResponseTime.TotalMilliseconds:F0}ms) exceeds threshold ({threshold.MaxResponseTime.TotalMilliseconds:F0}ms)",
                Details = new Dictionary<string, object>
                {
                    ["ActualResponseTime"] = healthReport.ResponseTime.TotalMilliseconds,
                    ["ThresholdResponseTime"] = threshold.MaxResponseTime.TotalMilliseconds
                }
            });
        }

        // Check uptime threshold
        if (healthReport.UptimePercentage < threshold.MinUptimePercentage)
        {
            alerts.Add(new HealthAlert
            {
                Id = Guid.NewGuid().ToString(),
                NodeAddress = healthReport.NodeAddress,
                Severity = HealthAlertSeverity.Error,
                AlertType = "LowUptime",
                Message = $"Node uptime ({healthReport.UptimePercentage:F1}%) below threshold ({threshold.MinUptimePercentage:F1}%)",
                Details = new Dictionary<string, object>
                {
                    ["ActualUptime"] = healthReport.UptimePercentage,
                    ["ThresholdUptime"] = threshold.MinUptimePercentage
                }
            });
        }

        // Check if node is offline
        if (healthReport.Status == NodeStatus.Offline)
        {
            alerts.Add(new HealthAlert
            {
                Id = Guid.NewGuid().ToString(),
                NodeAddress = healthReport.NodeAddress,
                Severity = HealthAlertSeverity.Critical,
                AlertType = "NodeOffline",
                Message = "Node is offline and not responding",
                Details = new Dictionary<string, object>
                {
                    ["LastSeen"] = healthReport.LastSeen,
                    ["Status"] = healthReport.Status.ToString()
                }
            });
        }

        // Check memory usage if available (using custom thresholds)
        if (threshold.CustomThresholds.TryGetValue("MaxMemoryUsage", out var maxMemoryUsage) &&
            healthReport.Metrics?.MemoryUsage > maxMemoryUsage)
        {
            alerts.Add(new HealthAlert
            {
                Id = Guid.NewGuid().ToString(),
                NodeAddress = healthReport.NodeAddress,
                Severity = HealthAlertSeverity.Warning,
                AlertType = "HighMemoryUsage",
                Message = $"Node memory usage ({healthReport.Metrics.MemoryUsage / 1_000_000:F0}MB) exceeds threshold ({maxMemoryUsage / 1_000_000:F0}MB)",
                Details = new Dictionary<string, object>
                {
                    ["ActualMemoryUsage"] = healthReport.Metrics.MemoryUsage,
                    ["ThresholdMemoryUsage"] = maxMemoryUsage
                }
            });
        }

        // Check CPU usage if available (using custom thresholds)
        if (threshold.CustomThresholds.TryGetValue("MaxCpuUsage", out var maxCpuUsage) &&
            healthReport.Metrics?.CpuUsage > maxCpuUsage)
        {
            alerts.Add(new HealthAlert
            {
                Id = Guid.NewGuid().ToString(),
                NodeAddress = healthReport.NodeAddress,
                Severity = HealthAlertSeverity.Warning,
                AlertType = "HighCpuUsage",
                Message = $"Node CPU usage ({healthReport.Metrics.CpuUsage:F1}%) exceeds threshold ({maxCpuUsage:F1}%)",
                Details = new Dictionary<string, object>
                {
                    ["ActualCpuUsage"] = healthReport.Metrics.CpuUsage,
                    ["ThresholdCpuUsage"] = maxCpuUsage
                }
            });
        }

        // Store new alerts
        bool alertsAdded = false;
        lock (_alertsLock)
        {
            foreach (var alert in alerts)
            {
                // Check if similar alert already exists
                var existingAlert = _activeAlerts.Values.FirstOrDefault(a =>
                    a.NodeAddress == alert.NodeAddress &&
                    a.AlertType == alert.AlertType &&
                    !a.IsResolved);

                if (existingAlert == null)
                {
                    alert.CreatedAt = DateTime.UtcNow;
                    _activeAlerts[alert.Id] = alert;
                    Logger.LogWarning("Health alert created: {AlertType} for node {NodeAddress} - {Message}",
                        alert.AlertType, alert.NodeAddress, alert.Message);
                    alertsAdded = true;
                }
            }
        }

        // Persist alerts if any were added
        if (alertsAdded)
        {
            await PersistActiveAlertsAsync();
        }
    }

    /// <summary>
    /// Resolves an alert by ID.
    /// </summary>
    /// <param name="alertId">The alert ID.</param>
    /// <param name="resolvedBy">Who resolved the alert.</param>
    /// <returns>True if alert was resolved.</returns>
    public async Task<bool> ResolveAlertAsync(string alertId, string resolvedBy = "System")
    {
        ArgumentException.ThrowIfNullOrEmpty(alertId);

        bool resolved = false;
        lock (_alertsLock)
        {
            if (_activeAlerts.TryGetValue(alertId, out var alert) && !alert.IsResolved)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                // Store resolved by information in Details
                alert.Details["ResolvedBy"] = resolvedBy;
                resolved = true;

                Logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", alertId, resolvedBy);
            }
        }

        if (resolved)
        {
            await PersistActiveAlertsAsync();
        }

        return resolved;
    }

    /// <summary>
    /// Resolves all alerts for a specific node.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="resolvedBy">Who resolved the alerts.</param>
    /// <returns>Number of alerts resolved.</returns>
    public async Task<int> ResolveNodeAlertsAsync(string nodeAddress, string resolvedBy = "System")
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAddress);

        var resolvedCount = 0;
        lock (_alertsLock)
        {
            var nodeAlerts = _activeAlerts.Values.Where(a =>
                a.NodeAddress == nodeAddress && !a.IsResolved).ToList();

            foreach (var alert in nodeAlerts)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                // Store resolved by information in Details
                alert.Details["ResolvedBy"] = resolvedBy;
                resolvedCount++;
            }
        }

        if (resolvedCount > 0)
        {
            await PersistActiveAlertsAsync();
            Logger.LogInformation("Resolved {ResolvedCount} alerts for node {NodeAddress}",
                resolvedCount, nodeAddress);
        }

        return resolvedCount;
    }

    /// <summary>
    /// Gets alerts by severity level.
    /// </summary>
    /// <param name="severity">The alert severity.</param>
    /// <returns>List of alerts with the specified severity.</returns>
    public List<HealthAlert> GetAlertsBySeverity(HealthAlertSeverity severity)
    {
        lock (_alertsLock)
        {
            return _activeAlerts.Values.Where(a => a.Severity == severity && !a.IsResolved).ToList();
        }
    }

    /// <summary>
    /// Gets alerts for a specific node.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="includeResolved">Whether to include resolved alerts.</param>
    /// <returns>List of alerts for the node.</returns>
    public List<HealthAlert> GetNodeAlerts(string nodeAddress, bool includeResolved = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAddress);

        lock (_alertsLock)
        {
            return _activeAlerts.Values.Where(a =>
                a.NodeAddress == nodeAddress &&
                (includeResolved || !a.IsResolved)).ToList();
        }
    }

    /// <summary>
    /// Cleans up old resolved alerts.
    /// </summary>
    /// <param name="olderThan">Remove alerts resolved before this time.</param>
    /// <returns>Number of alerts cleaned up.</returns>
    public async Task<int> CleanupOldAlertsAsync(DateTime olderThan)
    {
        var cleanedCount = 0;
        var alertsToRemove = new List<string>();

        lock (_alertsLock)
        {
            foreach (var kvp in _activeAlerts)
            {
                var alert = kvp.Value;
                if (alert.IsResolved && alert.ResolvedAt.HasValue && alert.ResolvedAt.Value < olderThan)
                {
                    alertsToRemove.Add(kvp.Key);
                }
            }

            foreach (var alertId in alertsToRemove)
            {
                _activeAlerts.Remove(alertId);
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            await PersistActiveAlertsAsync();
            Logger.LogInformation("Cleaned up {CleanedCount} old resolved alerts", cleanedCount);
        }

        return cleanedCount;
    }

    /// <summary>
    /// Gets alert statistics.
    /// </summary>
    /// <returns>Alert statistics summary.</returns>
    public AlertStatistics GetAlertStatistics()
    {
        lock (_alertsLock)
        {
            var totalAlerts = _activeAlerts.Count;
            var activeAlerts = _activeAlerts.Values.Count(a => !a.IsResolved);
            var resolvedAlerts = totalAlerts - activeAlerts;

            var criticalAlerts = _activeAlerts.Values.Count(a => a.Severity == HealthAlertSeverity.Critical && !a.IsResolved);
            var errorAlerts = _activeAlerts.Values.Count(a => a.Severity == HealthAlertSeverity.Error && !a.IsResolved);
            var warningAlerts = _activeAlerts.Values.Count(a => a.Severity == HealthAlertSeverity.Warning && !a.IsResolved);

            return new AlertStatistics
            {
                TotalAlerts = totalAlerts,
                ActiveAlerts = activeAlerts,
                ResolvedAlerts = resolvedAlerts,
                CriticalAlerts = criticalAlerts,
                ErrorAlerts = errorAlerts,
                WarningAlerts = warningAlerts,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
