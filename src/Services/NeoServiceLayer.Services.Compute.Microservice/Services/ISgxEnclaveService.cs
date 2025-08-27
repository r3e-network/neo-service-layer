using Neo.Compute.Service.Models;

namespace Neo.Compute.Service.Services;

public interface ISgxEnclaveService
{
    Task<EnclaveResponse> CreateEnclaveAsync(CreateEnclaveRequest request);
    Task<EnclaveResponse?> GetEnclaveAsync(Guid enclaveId);
    Task<List<EnclaveResponse>> GetEnclavesAsync(SgxEnclaveStatus? status = null);
    Task<bool> UpdateEnclaveStatusAsync(Guid enclaveId, SgxEnclaveStatus status);
    Task<bool> UpdateEnclaveHeartbeatAsync(Guid enclaveId);
    Task<EnclaveResponse?> GetAvailableEnclaveAsync(ComputeJobPriority priority = ComputeJobPriority.Normal);
    Task<bool> DeleteEnclaveAsync(Guid enclaveId);
    Task<List<EnclaveResponse>> GetEnclavesForNodeAsync(string nodeName);
}