using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;

namespace Neo.Storage.Service.BackgroundServices;

public class NodeHealthMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NodeHealthMonitorService> _logger;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _nodeTimeout = TimeSpan.FromMinutes(10);

    public NodeHealthMonitorService(
        IServiceProvider serviceProvider,
        ILogger<NodeHealthMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Node Health Monitor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorNodeHealthAsync(stoppingToken);
                await Task.Delay(_healthCheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in node health monitor service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Node Health Monitor Service stopped");
    }

    private async Task MonitorNodeHealthAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
        var nodeService = scope.ServiceProvider.GetRequiredService<IStorageNodeService>();

        var activeNodes = await context.StorageNodes
            .Where(n => n.Status == NodeStatus.Active || n.Status == NodeStatus.Warning)
            .ToListAsync(cancellationToken);

        if (!activeNodes.Any())
        {
            _logger.LogInformation("No active nodes to monitor");
            return;
        }

        _logger.LogDebug("Monitoring health of {NodeCount} nodes", activeNodes.Count);

        var healthCheckTasks = activeNodes.Select(node => CheckNodeHealthAsync(node, nodeService, context, cancellationToken));
        await Task.WhenAll(healthCheckTasks);

        await context.SaveChangesAsync(cancellationToken);

        // Check for nodes that haven't reported in a while
        await CheckStaleNodesAsync(context, cancellationToken);

        _logger.LogDebug("Completed node health monitoring");
    }

    private async Task CheckNodeHealthAsync(StorageNode node, IStorageNodeService nodeService, StorageDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var previousStatus = node.Status;
            
            // Check if node has timed out
            if (DateTime.UtcNow - node.LastHeartbeat > _nodeTimeout)
            {
                node.Status = NodeStatus.Failed;
                _logger.LogWarning("Node {NodeId} ({NodeName}) marked as failed due to timeout", node.Id, node.Name);
                
                await HandleNodeFailureAsync(node, context, cancellationToken);
                return;
            }

            // Measure node latency
            var latency = await nodeService.GetNodeLatencyAsync(node.Id);
            
            if (latency == double.MaxValue)
            {
                // Network unreachable
                node.Status = NodeStatus.Failed;
                _logger.LogWarning("Node {NodeId} ({NodeName}) marked as failed due to network unreachability", node.Id, node.Name);
                
                await HandleNodeFailureAsync(node, context, cancellationToken);
                return;
            }

            // Update latency
            node.NetworkLatency = latency;

            // Create health check record
            var healthCheck = new NodeHealth
            {
                NodeId = node.Id,
                CheckedAt = DateTime.UtcNow,
                ResponseTime = latency,
                CpuUsage = await GetNodeCpuUsageAsync(node),
                MemoryUsage = await GetNodeMemoryUsageAsync(node),
                DiskUsage = CalculateDiskUsage(node),
                Status = DetermineHealthStatus(latency, node)
            };

            context.NodeHealthChecks.Add(healthCheck);

            // Update node status based on health
            var newStatus = DetermineNodeStatus(healthCheck, node);
            
            if (newStatus != previousStatus)
            {
                node.Status = newStatus;
                _logger.LogInformation("Node {NodeId} ({NodeName}) status changed from {OldStatus} to {NewStatus}",
                    node.Id, node.Name, previousStatus, newStatus);

                if (newStatus == NodeStatus.Failed)
                {
                    await HandleNodeFailureAsync(node, context, cancellationToken);
                }
                else if (previousStatus == NodeStatus.Failed && newStatus == NodeStatus.Active)
                {
                    await HandleNodeRecoveryAsync(node, context, cancellationToken);
                }
            }

            // Update last heartbeat if node is responding
            if (newStatus != NodeStatus.Failed)
            {
                node.LastHeartbeat = DateTime.UtcNow;
            }

            _logger.LogDebug("Health check completed for node {NodeId}: Status={Status}, Latency={Latency}ms", 
                node.Id, newStatus, latency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check health for node {NodeId} ({NodeName})", node.Id, node.Name);
            
            // Mark node as failed on health check error
            node.Status = NodeStatus.Failed;
            await HandleNodeFailureAsync(node, context, cancellationToken);
        }
    }

    private async Task CheckStaleNodesAsync(StorageDbContext context, CancellationToken cancellationToken)
    {
        var staleThreshold = DateTime.UtcNow.AddHours(-1);
        
        var staleNodes = await context.StorageNodes
            .Where(n => n.Status == NodeStatus.Active && n.LastHeartbeat < staleThreshold)
            .ToListAsync(cancellationToken);

        foreach (var node in staleNodes)
        {
            node.Status = NodeStatus.Warning;
            _logger.LogWarning("Node {NodeId} ({NodeName}) marked as warning due to stale heartbeat", node.Id, node.Name);
        }

        if (staleNodes.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandleNodeFailureAsync(StorageNode node, StorageDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Log failure event
            var failureLog = new NodeFailureLog
            {
                Id = Guid.NewGuid(),
                NodeId = node.Id,
                FailureTime = DateTime.UtcNow,
                Reason = "Health check failure",
                Impact = "Node marked as failed, replicas may be affected"
            };

            context.NodeFailureLogs.Add(failureLog);

            // Check if this node has critical replicas that need immediate attention
            var criticalReplicas = await context.StorageReplicas
                .Include(r => r.Object)
                .Where(r => r.NodeId == node.Id && r.Status == ReplicaStatus.Active)
                .Where(r => r.Object.StorageClass == StorageClass.HighPerformance)
                .ToListAsync(cancellationToken);

            if (criticalReplicas.Any())
            {
                _logger.LogWarning("Node failure affects {CriticalReplicaCount} critical replicas", criticalReplicas.Count);
                
                // Mark replicas as potentially corrupted for verification
                foreach (var replica in criticalReplicas)
                {
                    replica.Status = ReplicaStatus.Verification;
                }
            }

            // Trigger replication service to handle node failure
            using var replicationScope = _serviceProvider.CreateScope();
            var replicationService = replicationScope.ServiceProvider.GetRequiredService<IReplicationService>();
            
            // This would initiate replica migration from failed node
            _ = Task.Run(async () => 
            {
                try
                {
                    // Implementation would move replicas off failed node
                    _logger.LogInformation("Initiating replica recovery for failed node {NodeId}", node.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initiate replica recovery for node {NodeId}", node.Id);
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle node failure for node {NodeId}", node.Id);
        }
    }

    private async Task HandleNodeRecoveryAsync(StorageNode node, StorageDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Node {NodeId} ({NodeName}) has recovered", node.Id, node.Name);

            // Log recovery event
            var recoveryLog = new NodeRecoveryLog
            {
                Id = Guid.NewGuid(),
                NodeId = node.Id,
                RecoveryTime = DateTime.UtcNow,
                DowntimeDuration = DateTime.UtcNow - node.LastHeartbeat
            };

            context.NodeRecoveryLogs.Add(recoveryLog);

            // Check replicas on recovered node
            var replicas = await context.StorageReplicas
                .Where(r => r.NodeId == node.Id)
                .ToListAsync(cancellationToken);

            foreach (var replica in replicas)
            {
                if (replica.Status == ReplicaStatus.Verification)
                {
                    replica.Status = ReplicaStatus.Active;
                    replica.LastVerified = DateTime.UtcNow;
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle node recovery for node {NodeId}", node.Id);
        }
    }

    private static async Task<double> GetNodeCpuUsageAsync(StorageNode node)
    {
        // In a real implementation, this would query the node for CPU usage
        await Task.Delay(1); // Simulate async call
        
        // Return simulated CPU usage
        var random = new Random(node.Id.GetHashCode());
        return random.NextDouble() * 100;
    }

    private static async Task<double> GetNodeMemoryUsageAsync(StorageNode node)
    {
        // In a real implementation, this would query the node for memory usage
        await Task.Delay(1); // Simulate async call
        
        // Return simulated memory usage
        var random = new Random(node.Id.GetHashCode() + 1);
        return random.NextDouble() * 100;
    }

    private static double CalculateDiskUsage(StorageNode node)
    {
        if (node.TotalCapacity == 0)
            return 0;

        return (double)node.UsedCapacity / node.TotalCapacity * 100;
    }

    private static HealthStatus DetermineHealthStatus(double latency, StorageNode node)
    {
        var diskUsage = CalculateDiskUsage(node);

        if (latency > 1000 || diskUsage > 95) // 1s latency or >95% disk usage
            return HealthStatus.Critical;

        if (latency > 500 || diskUsage > 80) // 500ms latency or >80% disk usage
            return HealthStatus.Warning;

        return HealthStatus.Healthy;
    }

    private static NodeStatus DetermineNodeStatus(NodeHealth health, StorageNode node)
    {
        return health.Status switch
        {
            HealthStatus.Healthy => NodeStatus.Active,
            HealthStatus.Warning => NodeStatus.Warning,
            HealthStatus.Critical => NodeStatus.Failed,
            HealthStatus.Unknown => NodeStatus.Inactive,
            _ => NodeStatus.Inactive
        };
    }

    // Additional model classes for logging
    public class NodeFailureLog
    {
        public Guid Id { get; set; }
        public Guid NodeId { get; set; }
        public DateTime FailureTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class NodeRecoveryLog
    {
        public Guid Id { get; set; }
        public Guid NodeId { get; set; }
        public DateTime RecoveryTime { get; set; }
        public TimeSpan DowntimeDuration { get; set; }
    }
}