using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;
using System.Net.Http;
using System.Text.Json;

namespace Neo.Storage.Service.Services;

public class ReplicationService : IReplicationService
{
    private readonly StorageDbContext _context;
    private readonly IShardingService _shardingService;
    private readonly IStorageNodeService _nodeService;
    private readonly IDistributedHashService _hashService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReplicationService> _logger;

    public ReplicationService(
        StorageDbContext context,
        IShardingService shardingService,
        IStorageNodeService nodeService,
        IDistributedHashService hashService,
        HttpClient httpClient,
        ILogger<ReplicationService> logger)
    {
        _context = context;
        _shardingService = shardingService;
        _nodeService = nodeService;
        _hashService = hashService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<StorageReplica>> CreateReplicasAsync(Guid objectId, int replicationFactor, string[]? preferredNodes = null)
    {
        try
        {
            var storageObject = await _context.StorageObjects
                .FirstOrDefaultAsync(o => o.Id == objectId);

            if (storageObject == null)
            {
                throw new ArgumentException($"Storage object not found: {objectId}");
            }

            // Get target nodes for replication
            var shardKey = $"{storageObject.BucketName}/{storageObject.Key}";
            var targetNodes = await _shardingService.GetTargetNodesAsync(shardKey, replicationFactor);

            if (targetNodes.Count < replicationFactor)
            {
                _logger.LogWarning("Only {NodeCount} nodes available for replication factor {ReplicationFactor}",
                    targetNodes.Count, replicationFactor);
            }

            var replicas = new List<StorageReplica>();
            bool isPrimary = true;

            foreach (var node in targetNodes)
            {
                var replica = new StorageReplica
                {
                    Id = Guid.NewGuid(),
                    ObjectId = objectId,
                    NodeId = node.Id,
                    Status = ReplicaStatus.Creating,
                    IsPrimary = isPrimary,
                    CreatedAt = DateTime.UtcNow,
                    Hash = storageObject.Hash
                };

                _context.StorageReplicas.Add(replica);
                replicas.Add(replica);
                isPrimary = false; // Only first replica is primary
            }

            await _context.SaveChangesAsync();

            // Create replication jobs for each replica
            foreach (var replica in replicas)
            {
                await CreateReplicationJobAsync(
                    objectId,
                    targetNodes.First().Id, // Use primary node as source
                    replica.NodeId,
                    ReplicationJobType.Create);
            }

            _logger.LogInformation("Created {ReplicaCount} replicas for object {ObjectId}",
                replicas.Count, objectId);

            return replicas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create replicas for object {ObjectId}", objectId);
            throw;
        }
    }

    public async Task<bool> VerifyReplicaIntegrityAsync(Guid replicaId)
    {
        try
        {
            var replica = await _context.StorageReplicas
                .Include(r => r.Object)
                .Include(r => r.Node)
                .FirstOrDefaultAsync(r => r.Id == replicaId);

            if (replica == null)
            {
                _logger.LogWarning("Replica not found: {ReplicaId}", replicaId);
                return false;
            }

            // Verify hash integrity using distributed hash service
            var isValid = await _hashService.VerifyReplicaHashAsync(replicaId);

            if (!isValid)
            {
                replica.Status = ReplicaStatus.Failed;
                replica.LastError = "Hash verification failed";
                await _context.SaveChangesAsync();

                _logger.LogError("Replica {ReplicaId} failed integrity verification", replicaId);
                
                // Create repair job
                await CreateReplicationJobAsync(
                    replica.ObjectId,
                    Guid.Empty, // Find healthy source automatically
                    replica.NodeId,
                    ReplicationJobType.Repair);
            }
            else
            {
                replica.LastVerified = DateTime.UtcNow;
                if (replica.Status == ReplicaStatus.Creating)
                {
                    replica.Status = ReplicaStatus.Active;
                }
                await _context.SaveChangesAsync();
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify replica integrity: {ReplicaId}", replicaId);
            return false;
        }
    }

    public async Task<ReplicationHealthReport> GetReplicationHealthAsync()
    {
        try
        {
            var totalReplicas = await _context.StorageReplicas.CountAsync();
            var activeReplicas = await _context.StorageReplicas
                .CountAsync(r => r.Status == ReplicaStatus.Active);
            var failedReplicas = await _context.StorageReplicas
                .CountAsync(r => r.Status == ReplicaStatus.Failed);
            var creatingReplicas = await _context.StorageReplicas
                .CountAsync(r => r.Status == ReplicaStatus.Creating);

            var underReplicatedObjects = await _context.StorageObjects
                .Where(o => o.Replicas.Count(r => r.Status == ReplicaStatus.Active) < GetMinimumReplicas(o.StorageClass))
                .CountAsync();

            var averageReplicationFactor = totalReplicas > 0
                ? await _context.StorageObjects
                    .Select(o => o.Replicas.Count(r => r.Status == ReplicaStatus.Active))
                    .AverageAsync()
                : 0;

            return new ReplicationHealthReport
            {
                TotalReplicas = totalReplicas,
                ActiveReplicas = activeReplicas,
                FailedReplicas = failedReplicas,
                CreatingReplicas = creatingReplicas,
                UnderReplicatedObjects = underReplicatedObjects,
                AverageReplicationFactor = averageReplicationFactor,
                HealthPercentage = totalReplicas > 0 ? (double)activeReplicas / totalReplicas * 100 : 100,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get replication health report");
            throw;
        }
    }

    public async Task<List<ReplicationJob>> GetReplicationJobsAsync(ReplicationJobStatus? status = null)
    {
        try
        {
            var query = _context.ReplicationJobs
                .Include(j => j.Object)
                .Include(j => j.SourceNode)
                .Include(j => j.TargetNode)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(j => j.Status == status.Value);
            }

            return await query
                .OrderByDescending(j => j.CreatedAt)
                .Take(100)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get replication jobs");
            throw;
        }
    }

    public async Task<bool> RepairObjectReplicationAsync(Guid objectId)
    {
        try
        {
            var storageObject = await _context.StorageObjects
                .Include(o => o.Replicas)
                .ThenInclude(r => r.Node)
                .FirstOrDefaultAsync(o => o.Id == objectId);

            if (storageObject == null)
            {
                return false;
            }

            var healthyReplicas = storageObject.Replicas
                .Where(r => r.Status == ReplicaStatus.Active)
                .ToList();

            var requiredReplicas = GetMinimumReplicas(storageObject.StorageClass);
            
            if (healthyReplicas.Count >= requiredReplicas)
            {
                return true; // Already has sufficient replicas
            }

            var sourceReplica = healthyReplicas.FirstOrDefault();
            if (sourceReplica == null)
            {
                _logger.LogError("No healthy replicas available for object {ObjectId}", objectId);
                return false;
            }

            // Create additional replicas to meet minimum requirements
            var shardKey = $"{storageObject.BucketName}/{storageObject.Key}";
            var targetNodes = await _shardingService.GetTargetNodesAsync(shardKey, requiredReplicas);
            
            var existingNodeIds = healthyReplicas.Select(r => r.NodeId).ToHashSet();
            var newTargetNodes = targetNodes.Where(n => !existingNodeIds.Contains(n.Id)).ToList();

            foreach (var node in newTargetNodes.Take(requiredReplicas - healthyReplicas.Count))
            {
                var replica = new StorageReplica
                {
                    Id = Guid.NewGuid(),
                    ObjectId = objectId,
                    NodeId = node.Id,
                    Status = ReplicaStatus.Creating,
                    IsPrimary = false,
                    CreatedAt = DateTime.UtcNow,
                    Hash = storageObject.Hash
                };

                _context.StorageReplicas.Add(replica);

                await CreateReplicationJobAsync(
                    objectId,
                    sourceReplica.NodeId,
                    node.Id,
                    ReplicationJobType.Repair);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Initiated repair for object {ObjectId}, creating {NewReplicaCount} new replicas",
                objectId, newTargetNodes.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair object replication: {ObjectId}", objectId);
            return false;
        }
    }

    public async Task<bool> RebalanceReplicasAsync()
    {
        try
        {
            _logger.LogInformation("Starting replica rebalancing");

            // Get all objects with their current replica distribution
            var objects = await _context.StorageObjects
                .Include(o => o.Replicas)
                .ThenInclude(r => r.Node)
                .Where(o => o.Status == ObjectStatus.Active)
                .ToListAsync();

            var rebalanceTasks = new List<Task>();
            const int maxConcurrentRebalances = 5;
            var semaphore = new SemaphoreSlim(maxConcurrentRebalances);

            foreach (var obj in objects)
            {
                rebalanceTasks.Add(RebalanceObjectReplicasAsync(obj, semaphore));
            }

            await Task.WhenAll(rebalanceTasks);

            _logger.LogInformation("Completed replica rebalancing for {ObjectCount} objects", objects.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebalance replicas");
            return false;
        }
    }

    public async Task<List<StorageReplica>> GetObjectReplicasAsync(Guid objectId)
    {
        return await _context.StorageReplicas
            .Include(r => r.Node)
            .Where(r => r.ObjectId == objectId)
            .OrderByDescending(r => r.IsPrimary)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteReplicasAsync(Guid objectId)
    {
        try
        {
            var replicas = await _context.StorageReplicas
                .Where(r => r.ObjectId == objectId)
                .ToListAsync();

            foreach (var replica in replicas)
            {
                replica.Status = ReplicaStatus.Deleting;
                
                // Create deletion job
                await CreateReplicationJobAsync(
                    objectId,
                    replica.NodeId,
                    replica.NodeId,
                    ReplicationJobType.Delete);
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Initiated deletion of {ReplicaCount} replicas for object {ObjectId}",
                replicas.Count, objectId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete replicas for object {ObjectId}", objectId);
            return false;
        }
    }

    public async Task<StorageReplica?> GetPrimaryReplicaAsync(Guid objectId)
    {
        return await _context.StorageReplicas
            .Include(r => r.Node)
            .FirstOrDefaultAsync(r => r.ObjectId == objectId && r.IsPrimary && r.Status == ReplicaStatus.Active);
    }

    public async Task<List<StorageReplica>> GetAvailableReplicasAsync(Guid objectId)
    {
        return await _context.StorageReplicas
            .Include(r => r.Node)
            .Where(r => r.ObjectId == objectId && r.Status == ReplicaStatus.Active)
            .Where(r => r.Node.Status == NodeStatus.Active)
            .OrderByDescending(r => r.IsPrimary)
            .ThenBy(r => r.Node.NetworkLatency)
            .ToListAsync();
    }

    public async Task<bool> PromoteReplicaToPrimaryAsync(Guid replicaId)
    {
        try
        {
            var replica = await _context.StorageReplicas
                .Include(r => r.Object)
                .FirstOrDefaultAsync(r => r.Id == replicaId);

            if (replica == null || replica.Status != ReplicaStatus.Active)
            {
                return false;
            }

            // Demote current primary
            var currentPrimary = await _context.StorageReplicas
                .FirstOrDefaultAsync(r => r.ObjectId == replica.ObjectId && r.IsPrimary);

            if (currentPrimary != null)
            {
                currentPrimary.IsPrimary = false;
            }

            // Promote new primary
            replica.IsPrimary = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Promoted replica {ReplicaId} to primary for object {ObjectId}",
                replicaId, replica.ObjectId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to promote replica {ReplicaId} to primary", replicaId);
            return false;
        }
    }

    public async Task<ReplicationJob> CreateReplicationJobAsync(Guid objectId, Guid sourceNodeId, Guid targetNodeId, ReplicationJobType type)
    {
        var job = new ReplicationJob
        {
            Id = Guid.NewGuid(),
            ObjectId = objectId,
            SourceNodeId = sourceNodeId == Guid.Empty ? null : sourceNodeId,
            TargetNodeId = targetNodeId,
            Type = type,
            Status = ReplicationJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Priority = GetJobPriority(type),
            RetryCount = 0,
            MaxRetries = 3
        };

        _context.ReplicationJobs.Add(job);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created replication job {JobId} for object {ObjectId}, type {Type}",
            job.Id, objectId, type);

        return job;
    }

    private async Task RebalanceObjectReplicasAsync(StorageObject obj, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var shardKey = $"{obj.BucketName}/{obj.Key}";
            var optimalNodes = await _shardingService.GetTargetNodesAsync(shardKey, obj.Replicas.Count);
            
            var currentNodes = obj.Replicas
                .Where(r => r.Status == ReplicaStatus.Active)
                .Select(r => r.NodeId)
                .ToHashSet();

            var optimalNodeIds = optimalNodes.Select(n => n.Id).ToHashSet();

            // Find replicas that should be moved
            var replicasToMove = obj.Replicas
                .Where(r => r.Status == ReplicaStatus.Active && !optimalNodeIds.Contains(r.NodeId))
                .ToList();

            // Find new target nodes
            var newTargetNodes = optimalNodes
                .Where(n => !currentNodes.Contains(n.Id))
                .Take(replicasToMove.Count)
                .ToList();

            // Create migration jobs
            for (int i = 0; i < Math.Min(replicasToMove.Count, newTargetNodes.Count); i++)
            {
                await CreateReplicationJobAsync(
                    obj.Id,
                    replicasToMove[i].NodeId,
                    newTargetNodes[i].Id,
                    ReplicationJobType.Migration);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static int GetMinimumReplicas(StorageClass storageClass)
    {
        return storageClass switch
        {
            StorageClass.Standard => 3,
            StorageClass.ReducedRedundancy => 2,
            StorageClass.Archive => 2,
            StorageClass.ColdArchive => 1,
            StorageClass.HighPerformance => 3,
            _ => 2
        };
    }

    private static int GetJobPriority(ReplicationJobType type)
    {
        return type switch
        {
            ReplicationJobType.Create => 100,
            ReplicationJobType.Repair => 90,
            ReplicationJobType.Migration => 50,
            ReplicationJobType.Delete => 30,
            _ => 50
        };
    }
}