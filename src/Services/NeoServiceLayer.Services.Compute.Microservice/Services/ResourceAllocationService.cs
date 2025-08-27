using Microsoft.EntityFrameworkCore;
using Neo.Compute.Service.Data;
using Neo.Compute.Service.Models;
using Neo.Compute.Service.Services;

namespace Neo.Compute.Service.Services;

public class ResourceAllocationService : IResourceAllocationService
{
    private readonly ComputeDbContext _context;
    private readonly ILogger<ResourceAllocationService> _logger;

    public ResourceAllocationService(
        ComputeDbContext context,
        ILogger<ResourceAllocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ResourceAllocation>> GetAllocationForJobAsync(Guid jobId)
    {
        var job = await _context.ComputeJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job not found: {jobId}");
        }

        return await _context.ResourceAllocations
            .Where(r => r.JobId == jobId)
            .OrderBy(r => r.AllocatedAt)
            .ToListAsync();
    }

    public async Task<ResourceAllocation> AllocateResourcesAsync(Guid jobId, string resourceType, decimal amount, string unit = "")
    {
        var job = await _context.ComputeJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job not found: {jobId}");
        }

        // Check resource availability
        await ValidateResourceAvailabilityAsync(resourceType, amount);

        var allocation = new ResourceAllocation
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            ResourceType = resourceType,
            AllocatedAmount = amount,
            UsedAmount = 0,
            Unit = unit,
            AllocatedAt = DateTime.UtcNow,
            Cost = CalculateResourceCost(resourceType, amount, TimeSpan.Zero)
        };

        _context.ResourceAllocations.Add(allocation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource allocated: {ResourceType} {Amount} {Unit} for job {JobId}", 
            resourceType, amount, unit, jobId);

        return allocation;
    }

    public async Task<bool> ReleaseResourcesAsync(Guid jobId)
    {
        var allocations = await _context.ResourceAllocations
            .Where(r => r.JobId == jobId && r.ReleasedAt == null)
            .ToListAsync();

        if (!allocations.Any()) return false;

        var releaseTime = DateTime.UtcNow;

        foreach (var allocation in allocations)
        {
            allocation.ReleasedAt = releaseTime;
            allocation.Duration = releaseTime - allocation.AllocatedAt;
            allocation.Cost = CalculateResourceCost(
                allocation.ResourceType, 
                allocation.UsedAmount > 0 ? allocation.UsedAmount : allocation.AllocatedAmount, 
                allocation.Duration.Value);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Resources released for job {JobId}: {ResourceCount} allocations", 
            jobId, allocations.Count);

        return true;
    }

    public async Task<bool> UpdateResourceUsageAsync(Guid allocationId, decimal usedAmount)
    {
        var allocation = await _context.ResourceAllocations
            .FirstOrDefaultAsync(r => r.Id == allocationId);

        if (allocation == null) return false;

        allocation.UsedAmount = Math.Min(usedAmount, allocation.AllocatedAmount);
        
        // Update cost based on actual usage
        if (allocation.Duration.HasValue)
        {
            allocation.Cost = CalculateResourceCost(
                allocation.ResourceType, 
                allocation.UsedAmount, 
                allocation.Duration.Value);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> CalculateCostAsync(Guid jobId)
    {
        var allocations = await _context.ResourceAllocations
            .Where(r => r.JobId == jobId)
            .ToListAsync();

        return allocations.Sum(a => a.Cost);
    }

    public async Task<List<ResourceAllocation>> GetActiveAllocationsAsync()
    {
        return await _context.ResourceAllocations
            .Where(r => r.ReleasedAt == null)
            .Include(r => r.Job)
            .OrderBy(r => r.AllocatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<string, decimal>> GetResourceUtilizationAsync()
    {
        var activeAllocations = await GetActiveAllocationsAsync();
        var utilization = new Dictionary<string, decimal>();

        // Get resource limits (would come from configuration)
        var resourceLimits = GetResourceLimits();

        foreach (var resourceType in resourceLimits.Keys)
        {
            var allocatedAmount = activeAllocations
                .Where(a => a.ResourceType.Equals(resourceType, StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.AllocatedAmount);

            var limit = resourceLimits[resourceType];
            var utilizationPercent = limit > 0 ? (allocatedAmount / limit) * 100 : 0;

            utilization[resourceType] = Math.Round(utilizationPercent, 2);
        }

        return utilization;
    }

    private async Task ValidateResourceAvailabilityAsync(string resourceType, decimal requestedAmount)
    {
        var resourceLimits = GetResourceLimits();
        
        if (!resourceLimits.TryGetValue(resourceType, out var limit))
        {
            _logger.LogWarning("Unknown resource type requested: {ResourceType}", resourceType);
            return; // Allow unknown resource types
        }

        var currentAllocations = await _context.ResourceAllocations
            .Where(r => r.ResourceType.Equals(resourceType, StringComparison.OrdinalIgnoreCase) && 
                       r.ReleasedAt == null)
            .SumAsync(r => r.AllocatedAmount);

        var availableAmount = limit - currentAllocations;

        if (requestedAmount > availableAmount)
        {
            throw new InvalidOperationException(
                $"Insufficient {resourceType} resources. Requested: {requestedAmount}, Available: {availableAmount}");
        }
    }

    private static decimal CalculateResourceCost(string resourceType, decimal amount, TimeSpan duration)
    {
        // Cost calculation based on resource type and usage duration
        var hourlyRates = GetResourceHourlyRates();
        
        if (!hourlyRates.TryGetValue(resourceType, out var hourlyRate))
        {
            hourlyRate = 0.01m; // Default rate
        }

        var hours = (decimal)duration.TotalHours;
        if (hours <= 0) hours = 0.1m; // Minimum billing unit

        return resourceType.ToUpper() switch
        {
            "CPU" => amount * hourlyRate * hours, // Per core per hour
            "MEMORY" => (amount / 1024) * hourlyRate * hours, // Per GB per hour (convert MB to GB)
            "STORAGE" => (amount / 1024) * hourlyRate * hours, // Per GB per hour
            "NETWORK" => amount * hourlyRate * hours, // Per Mbps per hour
            "GPU" => amount * hourlyRate * hours, // Per GPU per hour
            _ => amount * 0.001m * hours // Default minimal rate
        };
    }

    private static Dictionary<string, decimal> GetResourceLimits()
    {
        // In a real implementation, these would come from configuration or dynamic discovery
        return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "CPU", 1000 }, // 1000 CPU cores
            { "Memory", 2048 * 1024 }, // 2TB memory in MB
            { "Storage", 100 * 1024 * 1024 }, // 100TB storage in MB
            { "Network", 10000 }, // 10Gbps network
            { "GPU", 64 } // 64 GPUs
        };
    }

    private static Dictionary<string, decimal> GetResourceHourlyRates()
    {
        // Pricing in USD per unit per hour
        return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "CPU", 0.05m }, // $0.05 per CPU core per hour
            { "Memory", 0.02m }, // $0.02 per GB per hour
            { "Storage", 0.001m }, // $0.001 per GB per hour
            { "Network", 0.01m }, // $0.01 per Mbps per hour
            { "GPU", 1.00m } // $1.00 per GPU per hour
        };
    }
}