using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Services;

public interface IStorageNodeService
{
    Task InitializeStorageNodesAsync();
    Task<StorageNode> RegisterNodeAsync(string name, string endpoint, string region, string zone, NodeType type);
    Task<List<StorageNode>> GetActiveNodesAsync();
    Task<List<StorageNode>> GetNodesByRegionAsync(string region);
    Task<StorageNode?> GetNodeAsync(Guid nodeId);
    Task<bool> UpdateNodeHealthAsync(Guid nodeId, NodeHealth healthData);
    Task<bool> DecommissionNodeAsync(Guid nodeId);
    Task<List<StorageNode>> SelectOptimalNodesAsync(int count, string? preferredRegion = null);
    Task<bool> IsNodeHealthyAsync(Guid nodeId);
    Task<NodeStatistic> GetNodeStatisticsAsync(Guid nodeId);
    Task<List<NodeStatistic>> GetAllNodeStatisticsAsync();
    Task<bool> UpdateNodeCapacityAsync(Guid nodeId, long totalCapacity, long usedCapacity);
    Task<double> GetNodeLatencyAsync(Guid nodeId);
}