using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Health.Monitoring;

/// <summary>
/// Helper class for health monitoring operations.
/// </summary>
public class HealthMonitoringHelper
{
    private readonly ILogger<HealthMonitoringHelper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitoringHelper"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public HealthMonitoringHelper(ILogger<HealthMonitoringHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs a health check on a specific node.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The health report.</returns>
    public async Task<NodeHealthReport> PerformNodeHealthCheckAsync(string nodeAddress, BlockchainType blockchainType)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Simulate node health check (in real implementation, this would call the node's RPC endpoint)
            await Task.Delay(Random.Shared.Next(50, 200)); // Simulate network latency

            var responseTime = DateTime.UtcNow - startTime;
            var isOnline = Random.Shared.NextDouble() > 0.1; // 90% chance node is online
            var blockHeight = Random.Shared.NextInt64(1000000, 1100000);
            var uptimePercentage = Random.Shared.NextDouble() * 10 + 90; // 90-100% uptime

            var healthReport = new NodeHealthReport
            {
                NodeAddress = nodeAddress,
                Status = isOnline ? NodeStatus.Online : NodeStatus.Offline,
                BlockHeight = blockHeight,
                ResponseTime = responseTime,
                UptimePercentage = uptimePercentage,
                LastSeen = DateTime.UtcNow,
                Metrics = new HealthMetrics
                {
                    TotalRequests = Random.Shared.NextInt64(1000, 10000),
                    SuccessfulRequests = Random.Shared.NextInt64(900, 9900),
                    AverageResponseTime = responseTime,
                    SuccessRate = uptimePercentage / 100,
                    MemoryUsage = Random.Shared.NextInt64(1000000, 8000000),
                    CpuUsage = Random.Shared.NextDouble() * 100
                }
            };

            return healthReport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check for node {NodeAddress}", nodeAddress);

            return new NodeHealthReport
            {
                NodeAddress = nodeAddress,
                Status = NodeStatus.Offline,
                LastSeen = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime
            };
        }
    }

    /// <summary>
    /// Checks health thresholds and creates alerts if necessary.
    /// </summary>
    /// <param name="healthReport">The health report to check.</param>
    /// <param name="threshold">The health threshold to check against.</param>
    /// <returns>List of alerts created.</returns>
    public List<HealthAlert> CheckThresholdsAndCreateAlerts(NodeHealthReport healthReport, HealthThreshold? threshold = null)
    {
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

        // Check memory usage threshold (using custom thresholds)
        if (threshold.CustomThresholds.TryGetValue("MaxMemoryUsage", out var maxMemoryUsage) &&
            healthReport.Metrics.MemoryUsage > maxMemoryUsage)
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

        // Check CPU usage threshold (using custom thresholds)
        if (threshold.CustomThresholds.TryGetValue("MaxCpuUsage", out var maxCpuUsage) &&
            healthReport.Metrics.CpuUsage > maxCpuUsage)
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

        return alerts;
    }

    /// <summary>
    /// Calculates the overall network health score.
    /// </summary>
    /// <param name="monitoredNodes">The monitored nodes.</param>
    /// <returns>The network health score (0-1).</returns>
    public double CalculateNetworkHealth(Dictionary<string, NodeHealthReport> monitoredNodes)
    {
        if (monitoredNodes.Count == 0)
        {
            return 0;
        }

        var onlineNodes = monitoredNodes.Values.Count(n => n.Status == NodeStatus.Online);
        var healthyNodes = monitoredNodes.Values.Count(n => n.Status == NodeStatus.Online && n.UptimePercentage >= 95.0);

        var onlineRatio = (double)onlineNodes / monitoredNodes.Count;
        var healthyRatio = (double)healthyNodes / monitoredNodes.Count;

        // Weight online ratio more heavily than healthy ratio
        return (onlineRatio * 0.7) + (healthyRatio * 0.3);
    }
}
