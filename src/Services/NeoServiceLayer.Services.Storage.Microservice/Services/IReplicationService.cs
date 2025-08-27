using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Services;

public interface IReplicationService
{
    Task<List<StorageReplica>> CreateReplicasAsync(Guid objectId, int replicationFactor, string[] preferredNodes = null);
    Task<bool> VerifyReplicaIntegrityAsync(Guid replicaId);
    Task<ReplicationHealthReport> GetReplicationHealthAsync();
    Task<List<ReplicationJob>> GetReplicationJobsAsync(ReplicationJobStatus? status = null);
    Task<bool> RepairObjectReplicationAsync(Guid objectId);
    Task<bool> RebalanceReplicasAsync();
    Task<List<StorageReplica>> GetObjectReplicasAsync(Guid objectId);
    Task<bool> DeleteReplicasAsync(Guid objectId);
    Task<StorageReplica?> GetPrimaryReplicaAsync(Guid objectId);
    Task<List<StorageReplica>> GetAvailableReplicasAsync(Guid objectId);
    Task<bool> PromoteReplicaToPrimaryAsync(Guid replicaId);
    Task<ReplicationJob> CreateReplicationJobAsync(Guid objectId, Guid sourceNodeId, Guid targetNodeId, ReplicationJobType type);
}