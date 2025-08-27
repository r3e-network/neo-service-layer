using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;

namespace Neo.Storage.Service.BackgroundServices;

public class StorageMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StorageMaintenanceService> _logger;
    private readonly TimeSpan _maintenanceInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _deleteGracePeriod = TimeSpan.FromDays(30);

    public StorageMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<StorageMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Storage Maintenance Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken);
                await Task.Delay(_maintenanceInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in storage maintenance service");
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        _logger.LogInformation("Storage Maintenance Service stopped");
    }

    private async Task PerformMaintenanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting storage maintenance tasks");

        var maintenanceTasks = new List<Task>
        {
            CleanupDeletedObjectsAsync(cancellationToken),
            CleanupExpiredTransactionsAsync(cancellationToken),
            CleanupOldAccessLogsAsync(cancellationToken),
            CleanupOldHealthChecksAsync(cancellationToken),
            UpdateBucketStatisticsAsync(cancellationToken),
            CompactDatabaseAsync(cancellationToken),
            VerifyDataIntegrityAsync(cancellationToken),
            CleanupFailedReplicationJobsAsync(cancellationToken)
        };

        await Task.WhenAll(maintenanceTasks);

        _logger.LogInformation("Completed storage maintenance tasks");
    }

    private async Task CleanupDeletedObjectsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            var cutoffDate = DateTime.UtcNow - _deleteGracePeriod;

            // Find objects that have been deleted for longer than grace period
            var objectsToCleanup = await context.StorageObjects
                .Where(o => o.Status == ObjectStatus.Deleted && o.DeletedAt < cutoffDate)
                .Take(100) // Process in batches
                .ToListAsync(cancellationToken);

            if (!objectsToCleanup.Any())
            {
                return;
            }

            _logger.LogInformation("Cleaning up {ObjectCount} deleted objects", objectsToCleanup.Count);

            foreach (var obj in objectsToCleanup)
            {
                // Delete all associated replicas
                var replicas = await context.StorageReplicas
                    .Where(r => r.ObjectId == obj.Id)
                    .ToListAsync(cancellationToken);

                context.StorageReplicas.RemoveRange(replicas);

                // Delete all associated versions
                var versions = await context.ObjectVersions
                    .Where(v => v.ObjectId == obj.Id)
                    .ToListAsync(cancellationToken);

                context.ObjectVersions.RemoveRange(versions);

                // Delete access logs older than 1 year for this object
                var oldAccessLogs = await context.AccessLogs
                    .Where(l => l.ObjectKey == obj.Key && l.BucketName == obj.BucketName 
                                && l.Timestamp < DateTime.UtcNow.AddYears(-1))
                    .ToListAsync(cancellationToken);

                context.AccessLogs.RemoveRange(oldAccessLogs);

                // Finally delete the object
                context.StorageObjects.Remove(obj);
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {ObjectCount} deleted objects", objectsToCleanup.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup deleted objects");
        }
    }

    private async Task CleanupExpiredTransactionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<IStorageTransactionService>();

            await transactionService.CleanupExpiredTransactionsAsync();
            _logger.LogDebug("Cleaned up expired transactions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired transactions");
        }
    }

    private async Task CleanupOldAccessLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            var cutoffDate = DateTime.UtcNow.AddMonths(-6); // Keep logs for 6 months

            var deletedCount = await context.AccessLogs
                .Where(l => l.Timestamp < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {LogCount} old access logs", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old access logs");
        }
    }

    private async Task CleanupOldHealthChecksAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Keep health checks for 7 days

            // Keep the most recent 50 health checks per node, delete older ones beyond cutoff
            var nodesToClean = await context.StorageNodes.Select(n => n.Id).ToListAsync(cancellationToken);

            foreach (var nodeId in nodesToClean)
            {
                var oldHealthChecks = await context.NodeHealthChecks
                    .Where(h => h.NodeId == nodeId && h.CheckedAt < cutoffDate)
                    .OrderByDescending(h => h.CheckedAt)
                    .Skip(50) // Keep 50 most recent
                    .ToListAsync(cancellationToken);

                if (oldHealthChecks.Any())
                {
                    context.NodeHealthChecks.RemoveRange(oldHealthChecks);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Cleaned up old health check records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old health checks");
        }
    }

    private async Task UpdateBucketStatisticsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            var buckets = await context.StorageBuckets
                .Where(b => b.Status == BucketStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var bucket in buckets)
            {
                var stats = await context.StorageObjects
                    .Where(o => o.BucketName == bucket.Name && o.Status == ObjectStatus.Active)
                    .GroupBy(o => 1)
                    .Select(g => new 
                    {
                        Count = g.Count(),
                        TotalSize = g.Sum(o => o.Size)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                bucket.ObjectCount = stats?.Count ?? 0;
                bucket.TotalSize = stats?.TotalSize ?? 0;
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Updated bucket statistics for {BucketCount} buckets", buckets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update bucket statistics");
        }
    }

    private async Task CompactDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            // Only compact during off-peak hours (assuming UTC)
            var currentHour = DateTime.UtcNow.Hour;
            if (currentHour >= 6 && currentHour <= 22) // Skip during peak hours 6 AM - 10 PM UTC
            {
                return;
            }

            // PostgreSQL-specific VACUUM and ANALYZE
            if (context.Database.IsNpgsql())
            {
                await context.Database.ExecuteSqlRawAsync("VACUUM ANALYZE;", cancellationToken);
                _logger.LogInformation("Performed database vacuum and analyze");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compact database");
        }
    }

    private async Task VerifyDataIntegrityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            // Check for orphaned replicas (replicas without objects)
            var orphanedReplicas = await context.StorageReplicas
                .Where(r => !context.StorageObjects.Any(o => o.Id == r.ObjectId))
                .CountAsync(cancellationToken);

            if (orphanedReplicas > 0)
            {
                _logger.LogWarning("Found {OrphanedReplicaCount} orphaned replicas", orphanedReplicas);
                
                // Clean up orphaned replicas
                await context.StorageReplicas
                    .Where(r => !context.StorageObjects.Any(o => o.Id == r.ObjectId))
                    .ExecuteDeleteAsync(cancellationToken);
                
                _logger.LogInformation("Cleaned up {OrphanedReplicaCount} orphaned replicas", orphanedReplicas);
            }

            // Check for objects without any active replicas
            var objectsWithoutReplicas = await context.StorageObjects
                .Where(o => o.Status == ObjectStatus.Active)
                .Where(o => !o.Replicas.Any(r => r.Status == ReplicaStatus.Active))
                .CountAsync(cancellationToken);

            if (objectsWithoutReplicas > 0)
            {
                _logger.LogWarning("Found {ObjectCount} objects without active replicas", objectsWithoutReplicas);
            }

            // Check for inconsistent bucket statistics
            var bucketsWithInconsistentStats = await context.StorageBuckets
                .Where(b => b.Status == BucketStatus.Active)
                .Where(b => b.ObjectCount != context.StorageObjects
                    .Where(o => o.BucketName == b.Name && o.Status == ObjectStatus.Active)
                    .Count())
                .CountAsync(cancellationToken);

            if (bucketsWithInconsistentStats > 0)
            {
                _logger.LogWarning("Found {BucketCount} buckets with inconsistent statistics", bucketsWithInconsistentStats);
            }

            _logger.LogDebug("Data integrity verification completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify data integrity");
        }
    }

    private async Task CleanupFailedReplicationJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Keep failed jobs for 7 days

            var failedJobs = await context.ReplicationJobs
                .Where(j => j.Status == ReplicationJobStatus.Failed && j.CompletedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (failedJobs.Any())
            {
                context.ReplicationJobs.RemoveRange(failedJobs);
                await context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Cleaned up {JobCount} old failed replication jobs", failedJobs.Count);
            }

            // Clean up completed jobs older than 30 days
            var oldCompletedJobs = await context.ReplicationJobs
                .Where(j => j.Status == ReplicationJobStatus.Completed && j.CompletedAt < DateTime.UtcNow.AddDays(-30))
                .ToListAsync(cancellationToken);

            if (oldCompletedJobs.Any())
            {
                context.ReplicationJobs.RemoveRange(oldCompletedJobs);
                await context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Cleaned up {JobCount} old completed replication jobs", oldCompletedJobs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old replication jobs");
        }
    }
}