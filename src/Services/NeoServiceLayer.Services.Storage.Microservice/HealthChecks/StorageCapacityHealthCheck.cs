using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.HealthChecks;

public class StorageCapacityHealthCheck : IHealthCheck
{
    private readonly StorageDbContext _context;
    private readonly ILogger<StorageCapacityHealthCheck> _logger;
    private readonly StorageCapacityOptions _options;

    public StorageCapacityHealthCheck(
        StorageDbContext context,
        IConfiguration configuration,
        ILogger<StorageCapacityHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
        _options = configuration.GetSection("StorageCapacity").Get<StorageCapacityOptions>() 
                   ?? new StorageCapacityOptions();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();

            // Get overall capacity statistics
            var capacityStats = await GetCapacityStatisticsAsync(cancellationToken);
            
            healthData.Add("total_capacity_gb", Math.Round(capacityStats.TotalCapacity / (1024.0 * 1024 * 1024), 2));
            healthData.Add("used_capacity_gb", Math.Round(capacityStats.UsedCapacity / (1024.0 * 1024 * 1024), 2));
            healthData.Add("available_capacity_gb", Math.Round(capacityStats.AvailableCapacity / (1024.0 * 1024 * 1024), 2));
            healthData.Add("utilization_percentage", Math.Round(capacityStats.UtilizationPercentage, 2));

            // Check critical capacity thresholds
            if (capacityStats.UtilizationPercentage > _options.CriticalThresholdPercentage)
            {
                return HealthCheckResult.Unhealthy(
                    $"Critical storage capacity: {capacityStats.UtilizationPercentage:F1}% used",
                    data: healthData);
            }

            if (capacityStats.UtilizationPercentage > _options.WarningThresholdPercentage)
            {
                return HealthCheckResult.Degraded(
                    $"High storage capacity usage: {capacityStats.UtilizationPercentage:F1}% used",
                    data: healthData);
            }

            // Check per-node capacity distribution
            var nodeCapacityIssues = await CheckNodeCapacityDistributionAsync(healthData, cancellationToken);
            if (nodeCapacityIssues.HasCriticalIssues)
            {
                return HealthCheckResult.Unhealthy(nodeCapacityIssues.Message, data: healthData);
            }
            if (nodeCapacityIssues.HasWarningIssues)
            {
                return HealthCheckResult.Degraded(nodeCapacityIssues.Message, data: healthData);
            }

            // Check capacity by storage class
            var storageClassIssues = await CheckStorageClassCapacityAsync(healthData, cancellationToken);
            if (storageClassIssues.HasIssues)
            {
                return HealthCheckResult.Degraded(storageClassIssues.Message, data: healthData);
            }

            // Check growth rate and projected capacity exhaustion
            var growthIssues = await CheckCapacityGrowthRateAsync(healthData, cancellationToken);
            if (growthIssues.HasIssues)
            {
                return HealthCheckResult.Degraded(growthIssues.Message, data: healthData);
            }

            return HealthCheckResult.Healthy("Storage capacity is healthy", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage capacity health check failed");
            return HealthCheckResult.Unhealthy(
                "Storage capacity health check failed",
                ex,
                new Dictionary<string, object> { { "error", ex.Message } });
        }
    }

    private async Task<CapacityStatistics> GetCapacityStatisticsAsync(CancellationToken cancellationToken)
    {
        var activeNodes = await _context.StorageNodes
            .Where(n => n.Status == NodeStatus.Active)
            .Select(n => new { n.TotalCapacity, n.UsedCapacity })
            .ToListAsync(cancellationToken);

        var totalCapacity = activeNodes.Sum(n => n.TotalCapacity);
        var usedCapacity = activeNodes.Sum(n => n.UsedCapacity);
        var availableCapacity = totalCapacity - usedCapacity;
        var utilizationPercentage = totalCapacity > 0 ? (double)usedCapacity / totalCapacity * 100 : 0;

        return new CapacityStatistics
        {
            TotalCapacity = totalCapacity,
            UsedCapacity = usedCapacity,
            AvailableCapacity = availableCapacity,
            UtilizationPercentage = utilizationPercentage
        };
    }

    private async Task<CapacityIssueResult> CheckNodeCapacityDistributionAsync(
        Dictionary<string, object> healthData, 
        CancellationToken cancellationToken)
    {
        var nodeCapacities = await _context.StorageNodes
            .Where(n => n.Status == NodeStatus.Active && n.TotalCapacity > 0)
            .Select(n => new
            {
                n.Id,
                n.Name,
                n.TotalCapacity,
                n.UsedCapacity,
                UtilizationPercentage = (double)n.UsedCapacity / n.TotalCapacity * 100
            })
            .ToListAsync(cancellationToken);

        if (!nodeCapacities.Any())
        {
            return new CapacityIssueResult { HasCriticalIssues = true, Message = "No active nodes with capacity information" };
        }

        var criticalNodes = nodeCapacities.Where(n => n.UtilizationPercentage > 95).ToList();
        var warningNodes = nodeCapacities.Where(n => n.UtilizationPercentage > 85).ToList();
        var avgUtilization = nodeCapacities.Average(n => n.UtilizationPercentage);
        var maxUtilization = nodeCapacities.Max(n => n.UtilizationPercentage);
        var minUtilization = nodeCapacities.Min(n => n.UtilizationPercentage);

        healthData.Add("nodes_with_critical_capacity", criticalNodes.Count);
        healthData.Add("nodes_with_high_capacity", warningNodes.Count);
        healthData.Add("average_node_utilization", Math.Round(avgUtilization, 2));
        healthData.Add("max_node_utilization", Math.Round(maxUtilization, 2));
        healthData.Add("min_node_utilization", Math.Round(minUtilization, 2));

        // Check for unbalanced capacity distribution
        var utilizationSpread = maxUtilization - minUtilization;
        healthData.Add("utilization_spread", Math.Round(utilizationSpread, 2));

        if (criticalNodes.Count > 0)
        {
            return new CapacityIssueResult
            {
                HasCriticalIssues = true,
                Message = $"{criticalNodes.Count} nodes have critical capacity (>95% full)"
            };
        }

        if (warningNodes.Count > nodeCapacities.Count / 2)
        {
            return new CapacityIssueResult
            {
                HasWarningIssues = true,
                Message = $"{warningNodes.Count} nodes have high capacity usage (>85% full)"
            };
        }

        if (utilizationSpread > 40) // Large imbalance between nodes
        {
            return new CapacityIssueResult
            {
                HasWarningIssues = true,
                Message = $"Unbalanced capacity distribution: {utilizationSpread:F1}% spread between nodes"
            };
        }

        return new CapacityIssueResult();
    }

    private async Task<CapacityIssueResult> CheckStorageClassCapacityAsync(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        var storageClassUsage = await _context.StorageObjects
            .Where(o => o.Status == ObjectStatus.Active)
            .GroupBy(o => o.StorageClass)
            .Select(g => new
            {
                StorageClass = g.Key,
                ObjectCount = g.Count(),
                TotalSize = g.Sum(o => o.Size)
            })
            .ToListAsync(cancellationToken);

        foreach (var usage in storageClassUsage)
        {
            healthData.Add($"{usage.StorageClass.ToString().ToLower()}_objects", usage.ObjectCount);
            healthData.Add($"{usage.StorageClass.ToString().ToLower()}_size_gb", 
                Math.Round(usage.TotalSize / (1024.0 * 1024 * 1024), 2));
        }

        // Check if high-performance storage is taking too much space
        var highPerfUsage = storageClassUsage.FirstOrDefault(u => u.StorageClass == StorageClass.HighPerformance);
        if (highPerfUsage != null)
        {
            var totalSize = storageClassUsage.Sum(u => u.TotalSize);
            var highPerfPercentage = totalSize > 0 ? (double)highPerfUsage.TotalSize / totalSize * 100 : 0;
            
            healthData.Add("high_performance_usage_percentage", Math.Round(highPerfPercentage, 2));

            if (highPerfPercentage > 50) // High-performance storage should be limited
            {
                return new CapacityIssueResult
                {
                    HasIssues = true,
                    Message = $"High-performance storage usage is high: {highPerfPercentage:F1}% of total"
                };
            }
        }

        return new CapacityIssueResult();
    }

    private async Task<CapacityIssueResult> CheckCapacityGrowthRateAsync(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        try
        {
            // Calculate growth rate based on objects created in the last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var recentGrowth = await _context.StorageObjects
                .Where(o => o.CreatedAt > sevenDaysAgo && o.Status == ObjectStatus.Active)
                .SumAsync(o => o.Size, cancellationToken);

            var monthlyGrowth = await _context.StorageObjects
                .Where(o => o.CreatedAt > thirtyDaysAgo && o.Status == ObjectStatus.Active)
                .SumAsync(o => o.Size, cancellationToken);

            // Estimate weekly growth rate
            var weeklyGrowthRate = recentGrowth; // Last 7 days
            var estimatedMonthlyGrowth = weeklyGrowthRate * 4.3; // 4.3 weeks per month

            healthData.Add("weekly_growth_gb", Math.Round(weeklyGrowthRate / (1024.0 * 1024 * 1024), 2));
            healthData.Add("monthly_growth_gb", Math.Round(monthlyGrowth / (1024.0 * 1024 * 1024), 2));
            healthData.Add("estimated_monthly_growth_gb", Math.Round(estimatedMonthlyGrowth / (1024.0 * 1024 * 1024), 2));

            // Calculate time to capacity exhaustion
            var currentCapacity = await GetCapacityStatisticsAsync(cancellationToken);
            
            if (weeklyGrowthRate > 0 && currentCapacity.AvailableCapacity > 0)
            {
                var weeksUntilFull = (double)currentCapacity.AvailableCapacity / weeklyGrowthRate;
                healthData.Add("estimated_weeks_until_full", Math.Round(weeksUntilFull, 1));

                if (weeksUntilFull < 4) // Less than 4 weeks
                {
                    return new CapacityIssueResult
                    {
                        HasIssues = true,
                        Message = $"Projected capacity exhaustion in {weeksUntilFull:F1} weeks at current growth rate"
                    };
                }
                
                if (weeksUntilFull < 12) // Less than 12 weeks
                {
                    return new CapacityIssueResult
                    {
                        HasIssues = true,
                        Message = $"Capacity may be exhausted in {weeksUntilFull:F1} weeks - consider expansion"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not calculate growth rate");
            healthData.Add("growth_rate_error", "Unable to calculate growth rate");
        }

        return new CapacityIssueResult();
    }

    private class StorageCapacityOptions
    {
        public double WarningThresholdPercentage { get; set; } = 80.0;
        public double CriticalThresholdPercentage { get; set; } = 90.0;
    }

    private class CapacityStatistics
    {
        public long TotalCapacity { get; set; }
        public long UsedCapacity { get; set; }
        public long AvailableCapacity { get; set; }
        public double UtilizationPercentage { get; set; }
    }

    private class CapacityIssueResult
    {
        public bool HasCriticalIssues { get; set; }
        public bool HasWarningIssues { get; set; }
        public bool HasIssues { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}