using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.HealthChecks;

public class StorageNodeHealthCheck : IHealthCheck
{
    private readonly StorageDbContext _context;
    private readonly ILogger<StorageNodeHealthCheck> _logger;

    public StorageNodeHealthCheck(
        StorageDbContext context,
        ILogger<StorageNodeHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();

            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to storage database");
            }

            // Get node statistics
            var totalNodes = await _context.StorageNodes.CountAsync(cancellationToken);
            var activeNodes = await _context.StorageNodes
                .CountAsync(n => n.Status == NodeStatus.Active, cancellationToken);
            var failedNodes = await _context.StorageNodes
                .CountAsync(n => n.Status == NodeStatus.Failed, cancellationToken);
            var warningNodes = await _context.StorageNodes
                .CountAsync(n => n.Status == NodeStatus.Warning, cancellationToken);

            healthData.Add("total_nodes", totalNodes);
            healthData.Add("active_nodes", activeNodes);
            healthData.Add("failed_nodes", failedNodes);
            healthData.Add("warning_nodes", warningNodes);

            // Check if we have minimum number of active nodes
            const int minimumActiveNodes = 2;
            if (activeNodes < minimumActiveNodes)
            {
                return HealthCheckResult.Unhealthy(
                    $"Insufficient active nodes: {activeNodes} (minimum: {minimumActiveNodes})",
                    data: healthData);
            }

            // Check node health distribution
            if (totalNodes > 0)
            {
                var healthyPercentage = (double)activeNodes / totalNodes * 100;
                healthData.Add("healthy_percentage", Math.Round(healthyPercentage, 2));

                if (healthyPercentage < 50)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Too many unhealthy nodes: {healthyPercentage:F1}% healthy",
                        data: healthData);
                }
                
                if (healthyPercentage < 80)
                {
                    return HealthCheckResult.Degraded(
                        $"Some nodes are unhealthy: {healthyPercentage:F1}% healthy",
                        data: healthData);
                }
            }

            // Check recent node failures
            var recentFailures = await _context.StorageNodes
                .Where(n => n.Status == NodeStatus.Failed && 
                           n.LastHeartbeat > DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            healthData.Add("recent_failures", recentFailures);

            if (recentFailures > 2)
            {
                return HealthCheckResult.Degraded(
                    $"Multiple recent node failures: {recentFailures}",
                    data: healthData);
            }

            // Check average node latency
            var avgLatency = await _context.StorageNodes
                .Where(n => n.Status == NodeStatus.Active && n.NetworkLatency > 0)
                .AverageAsync(n => n.NetworkLatency, cancellationToken);

            if (!double.IsNaN(avgLatency))
            {
                healthData.Add("average_latency_ms", Math.Round(avgLatency, 2));

                if (avgLatency > 1000) // 1 second average latency
                {
                    return HealthCheckResult.Degraded(
                        $"High average node latency: {avgLatency:F0}ms",
                        data: healthData);
                }
            }

            // Check storage capacity
            var totalCapacity = await _context.StorageNodes
                .Where(n => n.Status == NodeStatus.Active)
                .SumAsync(n => n.TotalCapacity, cancellationToken);

            var usedCapacity = await _context.StorageNodes
                .Where(n => n.Status == NodeStatus.Active)
                .SumAsync(n => n.UsedCapacity, cancellationToken);

            if (totalCapacity > 0)
            {
                var utilizationPercentage = (double)usedCapacity / totalCapacity * 100;
                healthData.Add("storage_utilization_percent", Math.Round(utilizationPercentage, 2));

                if (utilizationPercentage > 90)
                {
                    return HealthCheckResult.Degraded(
                        $"High storage utilization: {utilizationPercentage:F1}%",
                        data: healthData);
                }
            }

            return HealthCheckResult.Healthy("All storage nodes are healthy", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage node health check failed");
            return HealthCheckResult.Unhealthy(
                "Storage node health check failed",
                ex,
                new Dictionary<string, object> { { "error", ex.Message } });
        }
    }
}