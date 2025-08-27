using Neo.Compute.Service.Models;

namespace Neo.Compute.Service.Services;

public interface IComputeJobService
{
    Task<ComputeJobResponse> CreateJobAsync(CreateComputeJobRequest request, string userId);
    Task<ComputeJobResponse?> GetJobAsync(Guid jobId);
    Task<List<ComputeJobResponse>> GetJobsForUserAsync(string userId, ComputeJobStatus? status = null, int skip = 0, int take = 20);
    Task<bool> CancelJobAsync(Guid jobId, string userId);
    Task<JobQueueStatus> GetQueueStatusAsync();
    Task<ComputeStatistics> GetStatisticsAsync();
    Task<bool> UpdateJobStatusAsync(Guid jobId, ComputeJobStatus status, string? errorMessage = null);
    Task<bool> AssignJobToEnclaveAsync(Guid jobId, Guid enclaveId);
    Task<List<ComputeJobResponse>> GetJobsForEnclaveAsync(Guid enclaveId, ComputeJobStatus? status = null);
}