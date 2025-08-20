using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Health.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Services.Health;

/// <summary>
/// Node management operations for the Health Service.
/// </summary>
public partial class HealthService
{
    /// <inheritdoc/>
    public async Task<NodeHealthReport?> GetNodeHealthAsync(string nodeAddress, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAddress);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            lock (_nodesLock)
            {
                if (_monitoredNodes.TryGetValue(nodeAddress, out var report))
                {
                    return report;
                }
            }

            // Return null for non-monitored nodes
            await Task.CompletedTask; // Ensure this is async
            return null;
        });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<NodeHealthReport>> GetAllNodesHealthAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        lock (_nodesLock)
        {
            return Task.FromResult<IEnumerable<NodeHealthReport>>(_monitoredNodes.Values.ToList());
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RegisterNodeForMonitoringAsync(NodeRegistrationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.NodeAddress);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var healthReport = new NodeHealthReport
            {
                NodeAddress = request.NodeAddress,
                PublicKey = request.PublicKey,
                Status = HealthStatus.Unknown,
                IsConsensusNode = request.IsConsensusNode,
                LastSeen = DateTime.UtcNow,
                Metrics = new List<HealthMetrics>(),
                AdditionalData = request.Metadata
            };

            lock (_nodesLock)
            {
                _monitoredNodes[request.NodeAddress] = healthReport;
                _nodeThresholds[request.NodeAddress] = request.Thresholds;
            }

            Logger.LogInformation("Registered node {NodeAddress} for monitoring on {Blockchain}",
                request.NodeAddress, blockchainType);

            // Persist to storage
            await PersistMonitoredNodesAsync();
            await PersistNodeThresholdsAsync();

            // Perform initial health check
            await PerformNodeHealthCheckAsync(request.NodeAddress, blockchainType);

            return true;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> UnregisterNodeAsync(string nodeAddress, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAddress);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        bool removed;
        lock (_nodesLock)
        {
            removed = _monitoredNodes.Remove(nodeAddress);
            _nodeThresholds.Remove(nodeAddress);

            if (removed)
            {
                Logger.LogInformation("Unregistered node {NodeAddress} from monitoring on {Blockchain}",
                    nodeAddress, blockchainType);
            }
        }

        if (removed)
        {
            // Persist changes to storage
            await PersistMonitoredNodesAsync();
            await PersistNodeThresholdsAsync();
        }

        return removed;
    }

    /// <inheritdoc/>
    public async Task<bool> SetHealthThresholdAsync(string nodeAddress, HealthThreshold threshold, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAddress);
        ArgumentNullException.ThrowIfNull(threshold);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        bool updated;
        lock (_nodesLock)
        {
            if (_monitoredNodes.ContainsKey(nodeAddress))
            {
                _nodeThresholds[nodeAddress] = threshold;
                Logger.LogInformation("Updated health threshold for node {NodeAddress} on {Blockchain}",
                    nodeAddress, blockchainType);
                updated = true;
            }
            else
            {
                updated = false;
            }
        }

        if (updated)
        {
            // Persist changes to storage
            await PersistNodeThresholdsAsync();
        }

        return updated;
    }

    /// <summary>
    /// Gets health threshold for a specific node.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <returns>The health threshold or null if not found.</returns>
    public HealthThreshold? GetNodeThreshold(string nodeAddress)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAddress);

        lock (_nodesLock)
        {
            return _nodeThresholds.TryGetValue(nodeAddress, out var threshold) ? threshold : null;
        }
    }

    /// <summary>
    /// Gets all monitored node addresses.
    /// </summary>
    /// <returns>Array of monitored node addresses.</returns>
    public string[] GetMonitoredNodeAddresses()
    {
        lock (_nodesLock)
        {
            return _monitoredNodes.Keys.ToArray();
        }
    }

    /// <summary>
    /// Gets consensus nodes only.
    /// </summary>
    /// <returns>List of consensus node health reports.</returns>
    public List<NodeHealthReport> GetConsensusNodes()
    {
        lock (_nodesLock)
        {
            return _monitoredNodes.Values.Where(n => n.IsConsensusNode).ToList();
        }
    }

    /// <summary>
    /// Gets online nodes only.
    /// </summary>
    /// <returns>List of online node health reports.</returns>
    public List<NodeHealthReport> GetOnlineNodes()
    {
        lock (_nodesLock)
        {
            return _monitoredNodes.Values.Where(n => n.Status == HealthStatus.Healthy).ToList();
        }
    }

    /// <summary>
    /// Gets nodes by status.
    /// </summary>
    /// <param name="status">The node status to filter by.</param>
    /// <returns>List of nodes with the specified status.</returns>
    public List<NodeHealthReport> GetNodesByStatus(HealthStatus status)
    {
        lock (_nodesLock)
        {
            return _monitoredNodes.Values.Where(n => n.Status == status).ToList();
        }
    }

    /// <summary>
    /// Updates node metadata.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="metadata">The metadata to update.</param>
    /// <returns>True if updated successfully.</returns>
    public async Task<bool> UpdateNodeMetadataAsync(string nodeAddress, Dictionary<string, object> metadata)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAddress);
        ArgumentNullException.ThrowIfNull(metadata);

        bool updated = false;
        lock (_nodesLock)
        {
            if (_monitoredNodes.TryGetValue(nodeAddress, out var node))
            {
                node.AdditionalData = new Dictionary<string, object>(metadata);
                updated = true;
            }
        }

        if (updated)
        {
            await PersistMonitoredNodesAsync();
            Logger.LogDebug("Updated metadata for node {NodeAddress}", nodeAddress);
        }

        return updated;
    }

    /// <summary>
    /// Gets node statistics.
    /// </summary>
    /// <returns>Node statistics summary.</returns>
    public NodeStatistics GetNodeStatistics()
    {
        lock (_nodesLock)
        {
            var totalNodes = _monitoredNodes.Count;
            var onlineNodes = _monitoredNodes.Values.Count(n => n.Status == HealthStatus.Healthy);
            var offlineNodes = _monitoredNodes.Values.Count(n => n.Status == HealthStatus.Unhealthy);
            var consensusNodes = _monitoredNodes.Values.Count(n => n.IsConsensusNode);
            var onlineConsensusNodes = _monitoredNodes.Values.Count(n => n.IsConsensusNode && n.Status == HealthStatus.Healthy);

            double averageUptime;
            if (totalNodes > 0)
            {
                averageUptime = _monitoredNodes.Values.Average(n => n.UptimePercentage);
            }
            else
            {
                averageUptime = 0.0;
            }

            TimeSpan averageResponseTime;
            if (totalNodes > 0)
            {
                averageResponseTime = TimeSpan.FromMilliseconds(_monitoredNodes.Values.Average(n => n.ResponseTime.TotalMilliseconds));
            }
            else
            {
                averageResponseTime = TimeSpan.Zero;
            }

            return new NodeStatistics
            {
                TotalNodes = totalNodes,
                OnlineNodes = onlineNodes,
                OfflineNodes = offlineNodes,
                ConsensusNodes = consensusNodes,
                OnlineConsensusNodes = onlineConsensusNodes,
                AverageUptimePercentage = averageUptime,
                AverageResponseTime = averageResponseTime,
                NetworkHealthScore = CalculateNetworkHealth(),
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
