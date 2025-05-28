using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Health;

/// <summary>
/// Health monitoring operations for the Health Service.
/// </summary>
public partial class HealthService
{
    /// <inheritdoc/>
    public async Task<ConsensusHealthReport> GetConsensusHealthAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var consensusNodes = new List<NodeHealthReport>();

            lock (_nodesLock)
            {
                consensusNodes.AddRange(_monitoredNodes.Values.Where(n => n.IsConsensusNode));
            }

            var totalConsensusNodes = consensusNodes.Count;
            var activeConsensusNodes = consensusNodes.Count(n => n.Status == NodeStatus.Online);
            var healthyConsensusNodes = consensusNodes.Count(n => n.Status == NodeStatus.Online && n.UptimePercentage >= 95.0);

            var currentBlockHeight = consensusNodes.Any() ? consensusNodes.Max(n => n.BlockHeight) : 0;
            var averageBlockTime = TimeSpan.FromSeconds(15); // Neo N3 target block time

            await Task.CompletedTask; // Ensure this is async

            return new ConsensusHealthReport
            {
                TotalConsensusNodes = totalConsensusNodes,
                ActiveConsensusNodes = activeConsensusNodes,
                HealthyConsensusNodes = healthyConsensusNodes,
                ConsensusEfficiency = totalConsensusNodes > 0 ? (double)healthyConsensusNodes / totalConsensusNodes : 0,
                AverageBlockTime = averageBlockTime,
                CurrentBlockHeight = currentBlockHeight,
                LastBlockTime = DateTime.UtcNow,
                ConsensusNodes = consensusNodes,
                NetworkMetrics = new Dictionary<string, object>
                {
                    ["TotalNodes"] = _monitoredNodes.Count,
                    ["ConsensusParticipation"] = totalConsensusNodes > 0 ? (double)activeConsensusNodes / totalConsensusNodes : 0,
                    ["NetworkHealth"] = CalculateNetworkHealth()
                }
            };
        });
    }

    /// <inheritdoc/>
    public Task<HealthMetrics> GetNetworkMetricsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        lock (_nodesLock)
        {
            var totalNodes = _monitoredNodes.Count;
            var onlineNodes = _monitoredNodes.Values.Count(n => n.Status == NodeStatus.Online);
            var averageResponseTime = totalNodes > 0
                ? TimeSpan.FromMilliseconds(_monitoredNodes.Values.Average(n => n.ResponseTime.TotalMilliseconds))
                : TimeSpan.Zero;

            return Task.FromResult(new HealthMetrics
            {
                TotalRequests = totalNodes,
                SuccessfulRequests = onlineNodes,
                FailedRequests = totalNodes - onlineNodes,
                AverageResponseTime = averageResponseTime,
                SuccessRate = totalNodes > 0 ? (double)onlineNodes / totalNodes : 0,
                CustomMetrics = new Dictionary<string, object>
                {
                    ["TotalMonitoredNodes"] = totalNodes,
                    ["OnlineNodes"] = onlineNodes,
                    ["ConsensusNodes"] = _monitoredNodes.Values.Count(n => n.IsConsensusNode),
                    ["NetworkHealth"] = CalculateNetworkHealth()
                }
            });
        }
    }

    /// <summary>
    /// Monitors all registered nodes.
    /// </summary>
    /// <param name="state">Timer state.</param>
    private async void MonitorNodes(object? state)
    {
        try
        {
            var nodesToMonitor = new List<string>();

            lock (_nodesLock)
            {
                nodesToMonitor.AddRange(_monitoredNodes.Keys);
            }

            foreach (var nodeAddress in nodesToMonitor)
            {
                await PerformNodeHealthCheckAsync(nodeAddress, BlockchainType.NeoN3);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during node monitoring");
        }
    }

    /// <summary>
    /// Performs a health check on a specific node.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The health report.</returns>
    private async Task<NodeHealthReport> PerformNodeHealthCheckAsync(string nodeAddress, BlockchainType blockchainType)
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

            // Update the stored health report
            lock (_nodesLock)
            {
                if (_monitoredNodes.ContainsKey(nodeAddress))
                {
                    var existingReport = _monitoredNodes[nodeAddress];
                    existingReport.Status = healthReport.Status;
                    existingReport.BlockHeight = healthReport.BlockHeight;
                    existingReport.ResponseTime = healthReport.ResponseTime;
                    existingReport.UptimePercentage = healthReport.UptimePercentage;
                    existingReport.LastSeen = healthReport.LastSeen;
                    existingReport.Metrics = healthReport.Metrics;

                    healthReport = existingReport;
                }
                else
                {
                    _monitoredNodes[nodeAddress] = healthReport;
                }
            }

            // Check for threshold violations and create alerts
            await CheckThresholdsAndCreateAlertsAsync(healthReport);

            return healthReport;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error performing health check for node {NodeAddress}", nodeAddress);

            var errorReport = new NodeHealthReport
            {
                NodeAddress = nodeAddress,
                Status = NodeStatus.Offline,
                LastSeen = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime
            };

            lock (_nodesLock)
            {
                _monitoredNodes[nodeAddress] = errorReport;
            }

            return errorReport;
        }
    }

    /// <summary>
    /// Performs health check on all nodes immediately.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>List of health reports.</returns>
    public async Task<List<NodeHealthReport>> PerformImmediateHealthCheckAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var nodesToCheck = new List<string>();
        lock (_nodesLock)
        {
            nodesToCheck.AddRange(_monitoredNodes.Keys);
        }

        var healthReports = new List<NodeHealthReport>();
        var tasks = nodesToCheck.Select(nodeAddress =>
            PerformNodeHealthCheckAsync(nodeAddress, blockchainType));

        var results = await Task.WhenAll(tasks);
        healthReports.AddRange(results);

        Logger.LogInformation("Performed immediate health check on {NodeCount} nodes", healthReports.Count);
        return healthReports;
    }

    /// <summary>
    /// Gets health summary for all nodes.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Health summary.</returns>
    public async Task<HealthSummary> GetHealthSummaryAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var nodeStats = GetNodeStatistics();
        var consensusHealth = await GetConsensusHealthAsync(blockchainType);
        var networkMetrics = await GetNetworkMetricsAsync(blockchainType);

        return new HealthSummary
        {
            NodeStatistics = nodeStats,
            ConsensusHealth = consensusHealth,
            NetworkMetrics = networkMetrics,
            NetworkHealthScore = CalculateNetworkHealth(),
            ActiveAlertCount = GetActiveAlertCount(),
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets the count of active alerts.
    /// </summary>
    /// <returns>Number of active alerts.</returns>
    private int GetActiveAlertCount()
    {
        lock (_alertsLock)
        {
            return _activeAlerts.Values.Count(a => !a.IsResolved);
        }
    }

    /// <summary>
    /// Checks if the network is healthy based on consensus participation.
    /// </summary>
    /// <returns>True if network is healthy.</returns>
    public bool IsNetworkHealthy()
    {
        var consensusNodes = GetConsensusNodes();
        if (consensusNodes.Count == 0)
            return false;

        var onlineConsensusNodes = consensusNodes.Count(n => n.Status == NodeStatus.Online);
        var consensusParticipation = (double)onlineConsensusNodes / consensusNodes.Count;

        // Network is healthy if at least 67% of consensus nodes are online (Byzantine fault tolerance)
        return consensusParticipation >= 0.67;
    }
}
