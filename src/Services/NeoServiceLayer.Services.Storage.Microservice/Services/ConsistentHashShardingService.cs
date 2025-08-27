using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Storage.Service.Services;

public class ConsistentHashShardingService : IShardingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsistentHashShardingService> _logger;
    private readonly SortedDictionary<uint, StorageNode> _ring = new();
    private readonly Dictionary<Guid, double> _nodeWeights = new();
    private readonly int _virtualNodes = 150; // Number of virtual nodes per physical node
    private readonly ReaderWriterLockSlim _ringLock = new();
    private bool _isInitialized = false;

    public ConsistentHashShardingService(
        IServiceProvider serviceProvider,
        ILogger<ConsistentHashShardingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            var activeNodes = await context.StorageNodes
                .Where(n => n.Status == NodeStatus.Active)
                .ToListAsync();

            _ringLock.EnterWriteLock();
            try
            {
                _ring.Clear();
                _nodeWeights.Clear();

                foreach (var node in activeNodes)
                {
                    await AddNodeToRingAsync(node);
                }

                _isInitialized = true;
                _logger.LogInformation("Consistent hash ring initialized with {NodeCount} nodes and {VirtualNodes} virtual nodes each", 
                    activeNodes.Count, _virtualNodes);
            }
            finally
            {
                _ringLock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize consistent hash ring");
            throw;
        }
    }

    public async Task<List<StorageNode>> GetTargetNodesAsync(string key, int replicationFactor = 3)
    {
        if (!_isInitialized)
            await InitializeAsync();

        var hash = ComputeHash(GetShardKey("", key));
        var targetNodes = new List<StorageNode>();
        var usedNodes = new HashSet<Guid>();

        _ringLock.EnterReadLock();
        try
        {
            if (_ring.Count == 0)
            {
                _logger.LogWarning("No nodes available in consistent hash ring");
                return targetNodes;
            }

            // Find the first node >= hash
            var startNode = _ring.FirstOrDefault(kvp => kvp.Key >= hash);
            if (startNode.Key == 0) // If no node found, wrap around to the first node
            {
                startNode = _ring.First();
            }

            // Collect unique nodes for replication
            var ringNodes = _ring.Values.ToList();
            var startIndex = ringNodes.FindIndex(n => n.Id == startNode.Value.Id);

            for (int i = 0; i < ringNodes.Count && targetNodes.Count < replicationFactor; i++)
            {
                var nodeIndex = (startIndex + i) % ringNodes.Count;
                var node = ringNodes[nodeIndex];

                if (!usedNodes.Contains(node.Id) && await IsNodeHealthyAsync(node))
                {
                    targetNodes.Add(node);
                    usedNodes.Add(node.Id);
                }
            }

            _logger.LogDebug("Selected {NodeCount} target nodes for key {Key} with hash {Hash}", 
                targetNodes.Count, key, hash);

            return targetNodes;
        }
        finally
        {
            _ringLock.ExitReadLock();
        }
    }

    public async Task<StorageNode> GetPrimaryNodeAsync(string key)
    {
        var targetNodes = await GetTargetNodesAsync(key, 1);
        
        if (targetNodes.Count == 0)
        {
            throw new InvalidOperationException("No healthy nodes available for primary placement");
        }

        return targetNodes[0];
    }

    public async Task<List<StorageNode>> GetReplicationNodesAsync(string key, int count)
    {
        var targetNodes = await GetTargetNodesAsync(key, count + 1); // +1 to include primary
        
        // Return all except the first one (which is primary)
        return targetNodes.Skip(1).Take(count).ToList();
    }

    public async Task AddNodeAsync(StorageNode node)
    {
        _ringLock.EnterWriteLock();
        try
        {
            await AddNodeToRingAsync(node);
            _logger.LogInformation("Added node {NodeId} ({NodeName}) to consistent hash ring", node.Id, node.Name);
        }
        finally
        {
            _ringLock.ExitWriteLock();
        }
    }

    public async Task RemoveNodeAsync(Guid nodeId)
    {
        _ringLock.EnterWriteLock();
        try
        {
            var keysToRemove = _ring.Where(kvp => kvp.Value.Id == nodeId).Select(kvp => kvp.Key).ToList();
            
            foreach (var key in keysToRemove)
            {
                _ring.Remove(key);
            }

            _nodeWeights.Remove(nodeId);
            
            _logger.LogInformation("Removed node {NodeId} from consistent hash ring", nodeId);
            
            await Task.CompletedTask; // Maintain async signature
        }
        finally
        {
            _ringLock.ExitWriteLock();
        }
    }

    public async Task RebalanceAsync()
    {
        try
        {
            _logger.LogInformation("Starting consistent hash ring rebalance");
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            // Get current active nodes
            var activeNodes = await context.StorageNodes
                .Where(n => n.Status == NodeStatus.Active)
                .ToListAsync();

            _ringLock.EnterWriteLock();
            try
            {
                // Rebuild the ring with current nodes
                _ring.Clear();
                _nodeWeights.Clear();

                foreach (var node in activeNodes)
                {
                    await AddNodeToRingAsync(node);
                }

                _logger.LogInformation("Consistent hash ring rebalanced with {NodeCount} active nodes", activeNodes.Count);
            }
            finally
            {
                _ringLock.ExitWriteLock();
            }

            // Trigger replication rebalancing in background
            _ = Task.Run(async () => await TriggerReplicationRebalanceAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebalance consistent hash ring");
            throw;
        }
    }

    public string GetShardKey(string bucketName, string objectKey)
    {
        // Combine bucket and object key for sharding
        return string.IsNullOrEmpty(bucketName) ? objectKey : $"{bucketName}/{objectKey}";
    }

    public async Task<Dictionary<Guid, double>> GetNodeWeightsAsync()
    {
        await Task.CompletedTask; // Maintain async signature
        
        _ringLock.EnterReadLock();
        try
        {
            return new Dictionary<Guid, double>(_nodeWeights);
        }
        finally
        {
            _ringLock.ExitReadLock();
        }
    }

    public async Task UpdateNodeWeightAsync(Guid nodeId, double weight)
    {
        _ringLock.EnterWriteLock();
        try
        {
            if (_nodeWeights.ContainsKey(nodeId))
            {
                _nodeWeights[nodeId] = weight;
                
                // Rebuild the ring for this node with new weight
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
                
                var node = await context.StorageNodes.FindAsync(nodeId);
                if (node != null)
                {
                    // Remove old virtual nodes
                    var keysToRemove = _ring.Where(kvp => kvp.Value.Id == nodeId).Select(kvp => kvp.Key).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _ring.Remove(key);
                    }

                    // Add back with new weight
                    await AddNodeToRingAsync(node);
                }
            }
        }
        finally
        {
            _ringLock.ExitWriteLock();
        }
    }

    private async Task AddNodeToRingAsync(StorageNode node)
    {
        // Calculate weight based on node capacity and performance
        var weight = CalculateNodeWeight(node);
        _nodeWeights[node.Id] = weight;

        // Add virtual nodes based on weight
        var virtualNodeCount = (int)(_virtualNodes * weight);
        
        for (int i = 0; i < virtualNodeCount; i++)
        {
            var virtualNodeKey = $"{node.Id}:{i}";
            var hash = ComputeHash(virtualNodeKey);
            _ring[hash] = node;
        }

        _logger.LogDebug("Added {VirtualNodeCount} virtual nodes for {NodeName} with weight {Weight}", 
            virtualNodeCount, node.Name, weight);

        await Task.CompletedTask; // Maintain async signature
    }

    private static double CalculateNodeWeight(StorageNode node)
    {
        // Base weight of 1.0
        double weight = 1.0;

        // Adjust based on node type
        weight *= node.Type switch
        {
            NodeType.HighPerformance => 1.5,
            NodeType.Standard => 1.0,
            NodeType.Archive => 0.7,
            NodeType.Cache => 1.2,
            _ => 1.0
        };

        // Adjust based on capacity utilization
        if (node.TotalCapacity > 0)
        {
            var utilization = (double)node.UsedCapacity / node.TotalCapacity;
            if (utilization > 0.9) // Heavily loaded
                weight *= 0.5;
            else if (utilization > 0.7) // Moderately loaded
                weight *= 0.8;
            else if (utilization < 0.3) // Lightly loaded
                weight *= 1.2;
        }

        // Adjust based on network latency
        if (node.NetworkLatency > 0)
        {
            if (node.NetworkLatency > 100) // High latency
                weight *= 0.8;
            else if (node.NetworkLatency < 10) // Low latency
                weight *= 1.1;
        }

        // Ensure weight is positive and reasonable
        return Math.Max(0.1, Math.Min(3.0, weight));
    }

    private async Task<bool> IsNodeHealthyAsync(StorageNode node)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var nodeService = scope.ServiceProvider.GetRequiredService<IStorageNodeService>();
            
            return await nodeService.IsNodeHealthyAsync(node.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check health for node {NodeId}", node.Id);
            return false;
        }
    }

    private static uint ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        
        // Use first 4 bytes for the hash ring
        return BitConverter.ToUInt32(hashBytes, 0);
    }

    private async Task TriggerReplicationRebalanceAsync()
    {
        try
        {
            _logger.LogInformation("Triggering replication rebalance after ring changes");
            
            using var scope = _serviceProvider.CreateScope();
            var replicationService = scope.ServiceProvider.GetRequiredService<IReplicationService>();
            
            await replicationService.RebalanceReplicasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger replication rebalance");
        }
    }
}