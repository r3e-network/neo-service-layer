using Neo.Compute.Service.Models;

namespace Neo.Compute.Service.Services;

public interface IResourceAllocationService
{
    Task<List<ResourceAllocation>> GetAllocationForJobAsync(Guid jobId);
    Task<ResourceAllocation> AllocateResourcesAsync(Guid jobId, string resourceType, decimal amount, string unit = "");
    Task<bool> ReleaseResourcesAsync(Guid jobId);
    Task<bool> UpdateResourceUsageAsync(Guid allocationId, decimal usedAmount);
    Task<decimal> CalculateCostAsync(Guid jobId);
    Task<List<ResourceAllocation>> GetActiveAllocationsAsync();
    Task<Dictionary<string, decimal>> GetResourceUtilizationAsync();
}