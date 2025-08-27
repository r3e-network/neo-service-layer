using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Services;

public interface IShardingService
{
    Task InitializeAsync();
    Task<List<StorageNode>> GetTargetNodesAsync(string key, int replicationFactor = 3);
    Task<StorageNode> GetPrimaryNodeAsync(string key);
    Task<List<StorageNode>> GetReplicationNodesAsync(string key, int count);
    Task AddNodeAsync(StorageNode node);
    Task RemoveNodeAsync(Guid nodeId);
    Task RebalanceAsync();
    string GetShardKey(string bucketName, string objectKey);
    Task<Dictionary<Guid, double>> GetNodeWeightsAsync();
    Task UpdateNodeWeightAsync(Guid nodeId, double weight);
}