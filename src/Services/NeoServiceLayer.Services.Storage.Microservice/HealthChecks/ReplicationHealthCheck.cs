using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;

namespace Neo.Storage.Service.HealthChecks;

public class ReplicationHealthCheck : IHealthCheck
{
    private readonly StorageDbContext _context;
    private readonly IReplicationService _replicationService;
    private readonly ILogger<ReplicationHealthCheck> _logger;

    public ReplicationHealthCheck(
        StorageDbContext context,
        IReplicationService replicationService,
        ILogger<ReplicationHealthCheck> logger)
    {
        _context = context;
        _replicationService = replicationService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();

            // Get comprehensive replication health report
            var healthReport = await _replicationService.GetReplicationHealthAsync();

            healthData.Add("total_replicas", healthReport.TotalReplicas);
            healthData.Add("active_replicas", healthReport.ActiveReplicas);
            healthData.Add("failed_replicas", healthReport.FailedReplicas);
            healthData.Add("creating_replicas", healthReport.CreatingReplicas);
            healthData.Add("under_replicated_objects", healthReport.UnderReplicatedObjects);
            healthData.Add("average_replication_factor", Math.Round(healthReport.AverageReplicationFactor, 2));
            healthData.Add("health_percentage", Math.Round(healthReport.HealthPercentage, 2));

            // Check critical thresholds
            if (healthReport.HealthPercentage < 50)
            {
                return HealthCheckResult.Unhealthy(
                    $"Critical replication health: {healthReport.HealthPercentage:F1}% healthy replicas",
                    data: healthData);
            }

            if (healthReport.HealthPercentage < 80)
            {
                return HealthCheckResult.Degraded(
                    $"Degraded replication health: {healthReport.HealthPercentage:F1}% healthy replicas",
                    data: healthData);
            }

            // Check for under-replicated objects
            if (healthReport.UnderReplicatedObjects > 0)
            {
                var totalObjects = await _context.StorageObjects
                    .CountAsync(o => o.Status == ObjectStatus.Active, cancellationToken);

                var underReplicatedPercentage = totalObjects > 0 
                    ? (double)healthReport.UnderReplicatedObjects / totalObjects * 100 
                    : 0;

                healthData.Add("under_replicated_percentage", Math.Round(underReplicatedPercentage, 2));

                if (underReplicatedPercentage > 10)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Too many under-replicated objects: {healthReport.UnderReplicatedObjects} ({underReplicatedPercentage:F1}%)",
                        data: healthData);
                }
                
                if (underReplicatedPercentage > 5)
                {
                    return HealthCheckResult.Degraded(
                        $"Some objects are under-replicated: {healthReport.UnderReplicatedObjects} ({underReplicatedPercentage:F1}%)",
                        data: healthData);
                }
            }

            // Check replication job queue health
            var pendingJobs = await _context.ReplicationJobs
                .CountAsync(j => j.Status == ReplicationJobStatus.Pending, cancellationToken);

            var failedJobs = await _context.ReplicationJobs
                .CountAsync(j => j.Status == ReplicationJobStatus.Failed, cancellationToken);

            var stuckJobs = await _context.ReplicationJobs
                .CountAsync(j => j.Status == ReplicationJobStatus.InProgress && 
                            j.StartedAt < DateTime.UtcNow.AddHours(-2), cancellationToken);

            healthData.Add("pending_replication_jobs", pendingJobs);
            healthData.Add("failed_replication_jobs", failedJobs);
            healthData.Add("stuck_replication_jobs", stuckJobs);

            // Check for job queue issues
            if (stuckJobs > 5)
            {
                return HealthCheckResult.Degraded(
                    $"Multiple stuck replication jobs: {stuckJobs}",
                    data: healthData);
            }

            if (pendingJobs > 100)
            {
                return HealthCheckResult.Degraded(
                    $"High number of pending replication jobs: {pendingJobs}",
                    data: healthData);
            }

            if (failedJobs > 20)
            {
                return HealthCheckResult.Degraded(
                    $"High number of failed replication jobs: {failedJobs}",
                    data: healthData);
            }

            // Check replication job success rate (last 24 hours)
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var recentCompletedJobs = await _context.ReplicationJobs
                .CountAsync(j => j.CompletedAt > oneDayAgo && j.Status == ReplicationJobStatus.Completed, cancellationToken);

            var recentFailedJobs = await _context.ReplicationJobs
                .CountAsync(j => j.CompletedAt > oneDayAgo && j.Status == ReplicationJobStatus.Failed, cancellationToken);

            var totalRecentJobs = recentCompletedJobs + recentFailedJobs;
            
            if (totalRecentJobs > 0)
            {
                var successRate = (double)recentCompletedJobs / totalRecentJobs * 100;
                healthData.Add("recent_job_success_rate", Math.Round(successRate, 2));

                if (successRate < 50)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Low replication job success rate: {successRate:F1}%",
                        data: healthData);
                }
                
                if (successRate < 80)
                {
                    return HealthCheckResult.Degraded(
                        $"Reduced replication job success rate: {successRate:F1}%",
                        data: healthData);
                }
            }

            // Check for critical storage class objects
            var criticalObjects = await _context.StorageObjects
                .Where(o => o.Status == ObjectStatus.Active && o.StorageClass == StorageClass.HighPerformance)
                .CountAsync(cancellationToken);

            var criticalObjectsWithInsufficientReplicas = await _context.StorageObjects
                .Where(o => o.Status == ObjectStatus.Active && o.StorageClass == StorageClass.HighPerformance)
                .Where(o => o.Replicas.Count(r => r.Status == ReplicaStatus.Active) < 3)
                .CountAsync(cancellationToken);

            if (criticalObjects > 0)
            {
                var criticalHealthPercentage = (double)(criticalObjects - criticalObjectsWithInsufficientReplicas) / criticalObjects * 100;
                healthData.Add("critical_objects_health_percentage", Math.Round(criticalHealthPercentage, 2));

                if (criticalHealthPercentage < 95)
                {
                    return HealthCheckResult.Degraded(
                        $"Some critical objects have insufficient replicas: {criticalObjectsWithInsufficientReplicas} of {criticalObjects}",
                        data: healthData);
                }
            }

            // Check average replication factor
            if (healthReport.AverageReplicationFactor < 2.0)
            {
                return HealthCheckResult.Degraded(
                    $"Low average replication factor: {healthReport.AverageReplicationFactor:F2}",
                    data: healthData);
            }

            return HealthCheckResult.Healthy("Replication system is healthy", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replication health check failed");
            return HealthCheckResult.Unhealthy(
                "Replication health check failed",
                ex,
                new Dictionary<string, object> { { "error", ex.Message } });
        }
    }
}