using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;

namespace Neo.Storage.Service.BackgroundServices;

public class ReplicationManagerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReplicationManagerService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public ReplicationManagerService(
        IServiceProvider serviceProvider,
        ILogger<ReplicationManagerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Replication Manager Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessReplicationJobsAsync(stoppingToken);
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in replication manager service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Replication Manager Service stopped");
    }

    private async Task ProcessReplicationJobsAsync(CancellationToken cancellationToken)
    {
        if (!await _processingSemaphore.WaitAsync(100, cancellationToken))
        {
            return; // Skip if already processing
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
            var replicationService = scope.ServiceProvider.GetRequiredService<IReplicationService>();

            // Get pending replication jobs ordered by priority
            var pendingJobs = await context.ReplicationJobs
                .Include(j => j.Object)
                .Include(j => j.SourceNode)
                .Include(j => j.TargetNode)
                .Where(j => j.Status == ReplicationJobStatus.Pending)
                .OrderByDescending(j => j.Priority)
                .ThenBy(j => j.CreatedAt)
                .Take(50) // Process up to 50 jobs at a time
                .ToListAsync(cancellationToken);

            if (!pendingJobs.Any())
            {
                return;
            }

            _logger.LogInformation("Processing {JobCount} replication jobs", pendingJobs.Count);

            var tasks = pendingJobs.Select(job => ProcessReplicationJobAsync(job, context, cancellationToken));
            await Task.WhenAll(tasks);

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed processing replication jobs");
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    private async Task ProcessReplicationJobAsync(ReplicationJob job, StorageDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            job.Status = ReplicationJobStatus.InProgress;
            job.StartedAt = DateTime.UtcNow;

            var success = await ExecuteReplicationJobAsync(job, cancellationToken);

            if (success)
            {
                job.Status = ReplicationJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogDebug("Replication job {JobId} completed successfully", job.Id);
            }
            else
            {
                job.RetryCount++;
                if (job.RetryCount >= job.MaxRetries)
                {
                    job.Status = ReplicationJobStatus.Failed;
                    job.CompletedAt = DateTime.UtcNow;
                    job.FailureReason = "Maximum retries exceeded";
                    _logger.LogError("Replication job {JobId} failed after {RetryCount} retries", job.Id, job.RetryCount);
                }
                else
                {
                    job.Status = ReplicationJobStatus.Pending;
                    job.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, job.RetryCount)); // Exponential backoff
                    _logger.LogWarning("Replication job {JobId} failed, scheduling retry {RetryCount}/{MaxRetries}", 
                        job.Id, job.RetryCount, job.MaxRetries);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing replication job {JobId}", job.Id);
            job.Status = ReplicationJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.FailureReason = ex.Message;
        }
    }

    private async Task<bool> ExecuteReplicationJobAsync(ReplicationJob job, CancellationToken cancellationToken)
    {
        try
        {
            switch (job.Type)
            {
                case ReplicationJobType.Create:
                    return await ExecuteCreateReplicationAsync(job, cancellationToken);
                case ReplicationJobType.Repair:
                    return await ExecuteRepairReplicationAsync(job, cancellationToken);
                case ReplicationJobType.Migration:
                    return await ExecuteMigrationReplicationAsync(job, cancellationToken);
                case ReplicationJobType.Delete:
                    return await ExecuteDeleteReplicationAsync(job, cancellationToken);
                default:
                    _logger.LogWarning("Unknown replication job type: {Type}", job.Type);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute replication job {JobId} of type {Type}", job.Id, job.Type);
            return false;
        }
    }

    private async Task<bool> ExecuteCreateReplicationAsync(ReplicationJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

        // Find the replica to create
        var replica = await context.StorageReplicas
            .Include(r => r.Object)
            .Include(r => r.Node)
            .FirstOrDefaultAsync(r => r.ObjectId == job.ObjectId && r.NodeId == job.TargetNodeId, cancellationToken);

        if (replica == null)
        {
            _logger.LogWarning("Replica not found for create job {JobId}", job.Id);
            return false;
        }

        // In a real implementation, this would:
        // 1. Download object data from source node or primary storage
        // 2. Upload data to target node
        // 3. Verify upload integrity
        // 4. Update replica status

        // For simulation, we'll mark the replica as active
        replica.Status = ReplicaStatus.Active;
        replica.CreatedAt = DateTime.UtcNow;
        replica.LastVerified = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created replica {ReplicaId} on node {NodeId} for object {ObjectId}",
            replica.Id, replica.NodeId, replica.ObjectId);

        return true;
    }

    private async Task<bool> ExecuteRepairReplicationAsync(ReplicationJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

        // Find a healthy source replica
        var healthyReplica = await context.StorageReplicas
            .Include(r => r.Node)
            .Where(r => r.ObjectId == job.ObjectId && r.Status == ReplicaStatus.Active)
            .Where(r => r.Node.Status == NodeStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (healthyReplica == null)
        {
            _logger.LogWarning("No healthy replica found for repair job {JobId}", job.Id);
            return false;
        }

        // Find or create the target replica
        var targetReplica = await context.StorageReplicas
            .FirstOrDefaultAsync(r => r.ObjectId == job.ObjectId && r.NodeId == job.TargetNodeId, cancellationToken);

        if (targetReplica == null)
        {
            // Create new replica
            targetReplica = new StorageReplica
            {
                Id = Guid.NewGuid(),
                ObjectId = job.ObjectId,
                NodeId = job.TargetNodeId,
                Status = ReplicaStatus.Creating,
                IsPrimary = false,
                CreatedAt = DateTime.UtcNow,
                Hash = healthyReplica.Hash
            };
            context.StorageReplicas.Add(targetReplica);
        }

        // Repair the replica
        targetReplica.Status = ReplicaStatus.Active;
        targetReplica.LastVerified = DateTime.UtcNow;
        targetReplica.Hash = healthyReplica.Hash;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Repaired replica {ReplicaId} on node {NodeId} for object {ObjectId}",
            targetReplica.Id, targetReplica.NodeId, targetReplica.ObjectId);

        return true;
    }

    private async Task<bool> ExecuteMigrationReplicationAsync(ReplicationJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

        // Find source replica
        var sourceReplica = await context.StorageReplicas
            .FirstOrDefaultAsync(r => r.ObjectId == job.ObjectId && r.NodeId == job.SourceNodeId, cancellationToken);

        if (sourceReplica == null)
        {
            _logger.LogWarning("Source replica not found for migration job {JobId}", job.Id);
            return false;
        }

        // Create target replica
        var targetReplica = new StorageReplica
        {
            Id = Guid.NewGuid(),
            ObjectId = job.ObjectId,
            NodeId = job.TargetNodeId,
            Status = ReplicaStatus.Active,
            IsPrimary = sourceReplica.IsPrimary,
            CreatedAt = DateTime.UtcNow,
            LastVerified = DateTime.UtcNow,
            Hash = sourceReplica.Hash
        };

        context.StorageReplicas.Add(targetReplica);

        // Remove source replica
        sourceReplica.Status = ReplicaStatus.Deleting;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Migrated replica for object {ObjectId} from node {SourceNodeId} to node {TargetNodeId}",
            job.ObjectId, job.SourceNodeId, job.TargetNodeId);

        return true;
    }

    private async Task<bool> ExecuteDeleteReplicationAsync(ReplicationJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

        var replica = await context.StorageReplicas
            .FirstOrDefaultAsync(r => r.ObjectId == job.ObjectId && r.NodeId == job.TargetNodeId, cancellationToken);

        if (replica != null)
        {
            context.StorageReplicas.Remove(replica);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Deleted replica {ReplicaId} on node {NodeId} for object {ObjectId}",
                replica.Id, replica.NodeId, replica.ObjectId);
        }

        return true;
    }

    public override void Dispose()
    {
        _processingSemaphore?.Dispose();
        base.Dispose();
    }
}