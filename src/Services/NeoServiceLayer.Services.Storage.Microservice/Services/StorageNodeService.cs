using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;
using System.Diagnostics;

namespace Neo.Storage.Service.Services;

public class StorageNodeService : IStorageNodeService
{
    private readonly StorageDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<StorageNodeService> _logger;

    public StorageNodeService(
        StorageDbContext context,
        HttpClient httpClient,
        ILogger<StorageNodeService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task InitializeStorageNodesAsync()
    {
        try
        {
            _logger.LogInformation("Initializing storage nodes");

            // Check if we have any nodes configured
            var existingNodes = await _context.StorageNodes.CountAsync();
            
            if (existingNodes == 0)
            {
                // Create default nodes for development/testing
                await CreateDefaultNodesAsync();
            }

            // Update node status based on health checks
            await UpdateNodeHealthStatusAsync();

            _logger.LogInformation("Storage nodes initialization completed with {NodeCount} nodes", 
                await _context.StorageNodes.CountAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize storage nodes");
            throw;
        }
    }

    public async Task<StorageNode> RegisterNodeAsync(string name, string endpoint, string region, string zone, NodeType type)
    {
        try
        {
            // Check if node already exists
            var existingNode = await _context.StorageNodes
                .FirstOrDefaultAsync(n => n.Endpoint == endpoint);

            if (existingNode != null)
            {
                _logger.LogInformation("Node already registered: {NodeName} at {Endpoint}", name, endpoint);
                return existingNode;
            }

            var node = new StorageNode
            {
                Id = Guid.NewGuid(),
                Name = name,
                Endpoint = endpoint,
                Region = region,
                Zone = zone,
                Type = type,
                Status = NodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                TotalCapacity = GetDefaultCapacityForType(type),
                UsedCapacity = 0,
                Priority = 100
            };

            _context.StorageNodes.Add(node);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registered new storage node: {NodeName} ({NodeId}) at {Endpoint}", 
                name, node.Id, endpoint);

            return node;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register storage node: {NodeName}", name);
            throw;
        }
    }

    public async Task<List<StorageNode>> GetActiveNodesAsync()
    {
        return await _context.StorageNodes
            .Where(n => n.Status == NodeStatus.Active)
            .OrderBy(n => n.Priority)
            .ThenBy(n => n.UsedCapacity)
            .ToListAsync();
    }

    public async Task<List<StorageNode>> GetNodesByRegionAsync(string region)
    {
        return await _context.StorageNodes
            .Where(n => n.Region == region && n.Status == NodeStatus.Active)
            .OrderBy(n => n.Priority)
            .ThenBy(n => n.UsedCapacity)
            .ToListAsync();
    }

    public async Task<StorageNode?> GetNodeAsync(Guid nodeId)
    {
        return await _context.StorageNodes
            .Include(n => n.HealthChecks.Take(5)) // Include recent health checks
            .FirstOrDefaultAsync(n => n.Id == nodeId);
    }

    public async Task<bool> UpdateNodeHealthAsync(Guid nodeId, NodeHealth healthData)
    {
        try
        {
            var node = await _context.StorageNodes.FirstOrDefaultAsync(n => n.Id == nodeId);
            if (node == null) return false;

            // Update node last heartbeat
            node.LastHeartbeat = DateTime.UtcNow;

            // Add health check record
            healthData.NodeId = nodeId;
            healthData.CheckedAt = DateTime.UtcNow;
            
            _context.NodeHealthChecks.Add(healthData);

            // Update node status based on health
            var previousStatus = node.Status;
            node.Status = DetermineNodeStatus(healthData);

            if (node.Status != previousStatus)
            {
                _logger.LogInformation("Node {NodeId} status changed from {OldStatus} to {NewStatus}", 
                    nodeId, previousStatus, node.Status);
            }

            await _context.SaveChangesAsync();
            
            // Clean up old health check records (keep last 10)
            await CleanupOldHealthChecksAsync(nodeId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update node health for node {NodeId}", nodeId);
            return false;
        }
    }

    public async Task<bool> DecommissionNodeAsync(Guid nodeId)
    {
        try
        {
            var node = await _context.StorageNodes.FirstOrDefaultAsync(n => n.Id == nodeId);
            if (node == null) return false;

            // Check if node has active replicas
            var activeReplicas = await _context.StorageReplicas
                .Where(r => r.NodeId == nodeId && r.Status == ReplicaStatus.Active)
                .CountAsync();

            if (activeReplicas > 0)
            {
                _logger.LogWarning("Cannot decommission node {NodeId} with {ReplicaCount} active replicas", 
                    nodeId, activeReplicas);
                return false;
            }

            node.Status = NodeStatus.Decommissioned;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Node {NodeId} decommissioned successfully", nodeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decommission node {NodeId}", nodeId);
            return false;
        }
    }

    public async Task<List<StorageNode>> SelectOptimalNodesAsync(int count, string? preferredRegion = null)
    {
        try
        {
            var query = _context.StorageNodes
                .Where(n => n.Status == NodeStatus.Active);

            // Prefer nodes in the specified region if provided
            if (!string.IsNullOrEmpty(preferredRegion))
            {
                var regionalNodes = await query
                    .Where(n => n.Region == preferredRegion)
                    .OrderBy(n => GetNodeScore(n))
                    .Take(count)
                    .ToListAsync();

                if (regionalNodes.Count >= count)
                {
                    return regionalNodes;
                }

                // If not enough regional nodes, supplement with nodes from other regions
                var additionalNodes = await query
                    .Where(n => n.Region != preferredRegion)
                    .OrderBy(n => GetNodeScore(n))
                    .Take(count - regionalNodes.Count)
                    .ToListAsync();

                return regionalNodes.Concat(additionalNodes).ToList();
            }

            // Select best nodes overall
            return await query
                .OrderBy(n => GetNodeScore(n))
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select optimal nodes");
            throw;
        }
    }

    public async Task<bool> IsNodeHealthyAsync(Guid nodeId)
    {
        try
        {
            var node = await _context.StorageNodes.FirstOrDefaultAsync(n => n.Id == nodeId);
            if (node == null) return false;

            // Check if node is active and has recent heartbeat
            var isHealthy = node.Status == NodeStatus.Active &&
                           DateTime.UtcNow - node.LastHeartbeat < TimeSpan.FromMinutes(5);

            // Check latest health status
            if (isHealthy)
            {
                var latestHealth = await _context.NodeHealthChecks
                    .Where(h => h.NodeId == nodeId)
                    .OrderByDescending(h => h.CheckedAt)
                    .FirstOrDefaultAsync();

                if (latestHealth != null)
                {
                    isHealthy = latestHealth.Status != HealthStatus.Critical;
                }
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check node health for node {NodeId}", nodeId);
            return false;
        }
    }

    public async Task<NodeStatistic> GetNodeStatisticsAsync(Guid nodeId)
    {
        try
        {
            var node = await _context.StorageNodes
                .Include(n => n.Replicas)
                .FirstOrDefaultAsync(n => n.Id == nodeId);

            if (node == null)
            {
                throw new ArgumentException($"Node not found: {nodeId}");
            }

            var utilizationPercent = node.TotalCapacity > 0 
                ? (double)node.UsedCapacity / node.TotalCapacity * 100 
                : 0;

            var averageResponseTime = await CalculateAverageResponseTimeAsync(nodeId);

            return new NodeStatistic
            {
                NodeId = node.Id.ToString(),
                Name = node.Name,
                Region = node.Region,
                Status = node.Status,
                TotalCapacity = node.TotalCapacity,
                UsedCapacity = node.UsedCapacity,
                UtilizationPercent = utilizationPercent,
                ReplicaCount = node.Replicas.Count(r => r.Status == ReplicaStatus.Active),
                AverageResponseTime = averageResponseTime,
                LastHeartbeat = node.LastHeartbeat
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for node {NodeId}", nodeId);
            throw;
        }
    }

    public async Task<List<NodeStatistic>> GetAllNodeStatisticsAsync()
    {
        try
        {
            var nodes = await _context.StorageNodes
                .Include(n => n.Replicas)
                .ToListAsync();

            var statistics = new List<NodeStatistic>();

            foreach (var node in nodes)
            {
                var stat = await GetNodeStatisticsAsync(node.Id);
                statistics.Add(stat);
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all node statistics");
            throw;
        }
    }

    public async Task<bool> UpdateNodeCapacityAsync(Guid nodeId, long totalCapacity, long usedCapacity)
    {
        try
        {
            var node = await _context.StorageNodes.FirstOrDefaultAsync(n => n.Id == nodeId);
            if (node == null) return false;

            node.TotalCapacity = totalCapacity;
            node.UsedCapacity = usedCapacity;
            node.AvailableCapacity = totalCapacity - usedCapacity;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update capacity for node {NodeId}", nodeId);
            return false;
        }
    }

    public async Task<double> GetNodeLatencyAsync(Guid nodeId)
    {
        try
        {
            var node = await _context.StorageNodes.FirstOrDefaultAsync(n => n.Id == nodeId);
            if (node == null) return double.MaxValue;

            // Perform a simple health check to measure latency
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var response = await _httpClient.GetAsync($"{node.Endpoint}/health");
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    var latency = stopwatch.Elapsed.TotalMilliseconds;
                    
                    // Update node latency
                    node.NetworkLatency = latency;
                    await _context.SaveChangesAsync();
                    
                    return latency;
                }
            }
            catch
            {
                // Network error - return high latency
                return 10000; // 10 seconds
            }
            
            return stopwatch.Elapsed.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to measure latency for node {NodeId}", nodeId);
            return double.MaxValue;
        }
    }

    private async Task CreateDefaultNodesAsync()
    {
        var defaultNodes = new[]
        {
            new { Name = "storage-node-1", Endpoint = "http://storage-node-1:8080", Region = "us-east-1", Zone = "us-east-1a", Type = NodeType.Standard },
            new { Name = "storage-node-2", Endpoint = "http://storage-node-2:8080", Region = "us-east-1", Zone = "us-east-1b", Type = NodeType.Standard },
            new { Name = "storage-node-3", Endpoint = "http://storage-node-3:8080", Region = "us-west-2", Zone = "us-west-2a", Type = NodeType.HighPerformance }
        };

        foreach (var nodeConfig in defaultNodes)
        {
            await RegisterNodeAsync(nodeConfig.Name, nodeConfig.Endpoint, nodeConfig.Region, nodeConfig.Zone, nodeConfig.Type);
        }
    }

    private static long GetDefaultCapacityForType(NodeType type)
    {
        return type switch
        {
            NodeType.HighPerformance => 2L * 1024 * 1024 * 1024 * 1024, // 2TB
            NodeType.Standard => 1L * 1024 * 1024 * 1024 * 1024,        // 1TB
            NodeType.Archive => 5L * 1024 * 1024 * 1024 * 1024,         // 5TB
            NodeType.Cache => 500L * 1024 * 1024 * 1024,                // 500GB
            _ => 1L * 1024 * 1024 * 1024 * 1024                         // 1TB default
        };
    }

    private static NodeStatus DetermineNodeStatus(NodeHealth health)
    {
        return health.Status switch
        {
            HealthStatus.Healthy => NodeStatus.Active,
            HealthStatus.Warning => NodeStatus.Active,
            HealthStatus.Critical => NodeStatus.Failed,
            HealthStatus.Unknown => NodeStatus.Inactive,
            _ => NodeStatus.Inactive
        };
    }

    private static double GetNodeScore(StorageNode node)
    {
        // Lower score = better node
        double score = 0;

        // Capacity utilization (prefer less utilized nodes)
        if (node.TotalCapacity > 0)
        {
            var utilization = (double)node.UsedCapacity / node.TotalCapacity;
            score += utilization * 100; // 0-100 points based on utilization
        }

        // Network latency (prefer lower latency)
        score += node.NetworkLatency; // Add latency in milliseconds

        // Node type preference
        score += node.Type switch
        {
            NodeType.HighPerformance => 0,   // Best
            NodeType.Standard => 10,         // Good
            NodeType.Cache => 5,             // Very good for cache
            NodeType.Archive => 20,          // Slower, use last
            _ => 15
        };

        return score;
    }

    private async Task<double> CalculateAverageResponseTimeAsync(Guid nodeId)
    {
        try
        {
            var recentHealthChecks = await _context.NodeHealthChecks
                .Where(h => h.NodeId == nodeId)
                .OrderByDescending(h => h.CheckedAt)
                .Take(10)
                .ToListAsync();

            return recentHealthChecks.Any() 
                ? recentHealthChecks.Average(h => h.ResponseTime)
                : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate average response time for node {NodeId}", nodeId);
            return 0;
        }
    }

    private async Task CleanupOldHealthChecksAsync(Guid nodeId)
    {
        try
        {
            var oldHealthChecks = await _context.NodeHealthChecks
                .Where(h => h.NodeId == nodeId)
                .OrderByDescending(h => h.CheckedAt)
                .Skip(10) // Keep the 10 most recent
                .ToListAsync();

            if (oldHealthChecks.Any())
            {
                _context.NodeHealthChecks.RemoveRange(oldHealthChecks);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old health checks for node {NodeId}", nodeId);
        }
    }
}