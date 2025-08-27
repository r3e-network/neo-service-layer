using Microsoft.EntityFrameworkCore;
using Neo.Compute.Service.Data;
using Neo.Compute.Service.Models;
using Neo.Compute.Service.Services;
using System.Text.Json;

namespace Neo.Compute.Service.Services;

public class ComputeJobService : IComputeJobService
{
    private readonly ComputeDbContext _context;
    private readonly ISgxEnclaveService _enclaveService;
    private readonly ILogger<ComputeJobService> _logger;

    public ComputeJobService(
        ComputeDbContext context,
        ISgxEnclaveService enclaveService,
        ILogger<ComputeJobService> logger)
    {
        _context = context;
        _enclaveService = enclaveService;
        _logger = logger;
    }

    public async Task<ComputeJobResponse> CreateJobAsync(CreateComputeJobRequest request, string userId)
    {
        var job = new ComputeJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobType = request.JobType,
            Algorithm = request.Algorithm,
            InputDataHash = request.InputDataHash,
            Priority = request.Priority,
            RequiresSgx = request.RequiresSgx,
            CpuCores = request.CpuCores,
            MemoryMb = request.MemoryMb,
            StorageMb = request.StorageMb,
            Configuration = request.Configuration != null 
                ? JsonSerializer.Serialize(request.Configuration) 
                : "{}",
            Metadata = request.Metadata != null 
                ? JsonSerializer.Serialize(request.Metadata) 
                : "{}",
            CreatedAt = DateTime.UtcNow,
            Status = ComputeJobStatus.Pending
        };

        _context.ComputeJobs.Add(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Compute job created: {JobId} for user: {UserId}", job.Id, userId);

        return MapToResponse(job);
    }

    public async Task<ComputeJobResponse?> GetJobAsync(Guid jobId)
    {
        var job = await _context.ComputeJobs
            .Include(j => j.Enclave)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        return job != null ? MapToResponse(job) : null;
    }

    public async Task<List<ComputeJobResponse>> GetJobsForUserAsync(string userId, ComputeJobStatus? status = null, int skip = 0, int take = 20)
    {
        var query = _context.ComputeJobs
            .Where(j => j.UserId == userId)
            .Include(j => j.Enclave);

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return jobs.Select(MapToResponse).ToList();
    }

    public async Task<bool> CancelJobAsync(Guid jobId, string userId)
    {
        var job = await _context.ComputeJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);

        if (job == null || job.Status == ComputeJobStatus.Completed || job.Status == ComputeJobStatus.Cancelled)
        {
            return false;
        }

        job.Status = ComputeJobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Compute job cancelled: {JobId}", jobId);
        return true;
    }

    public async Task<JobQueueStatus> GetQueueStatusAsync()
    {
        var pendingJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Pending)
            .CountAsync();

        var queuedJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Queued)
            .CountAsync();

        var runningJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Running)
            .CountAsync();

        var jobsByPriority = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Pending || j.Status == ComputeJobStatus.Queued)
            .GroupBy(j => j.Priority)
            .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count());

        var jobsByType = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Pending || j.Status == ComputeJobStatus.Queued)
            .GroupBy(j => j.JobType)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var avgWaitTime = await CalculateAverageWaitTimeAsync();
        var estimatedProcessingTime = await CalculateEstimatedProcessingTimeAsync();

        return new JobQueueStatus
        {
            PendingJobs = pendingJobs,
            QueuedJobs = queuedJobs,
            RunningJobs = runningJobs,
            JobsByPriority = jobsByPriority,
            JobsByType = jobsByType,
            AverageWaitTimeMinutes = avgWaitTime,
            EstimatedProcessingTimeMinutes = estimatedProcessingTime
        };
    }

    public async Task<ComputeStatistics> GetStatisticsAsync()
    {
        var totalJobs = await _context.ComputeJobs.CountAsync();
        var activeJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Running)
            .CountAsync();
        var completedJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Completed)
            .CountAsync();
        var failedJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Failed)
            .CountAsync();

        var availableEnclaves = await _context.SgxEnclaves
            .Where(e => e.Status == SgxEnclaveStatus.Ready)
            .CountAsync();
        var busyEnclaves = await _context.SgxEnclaves
            .Where(e => e.Status == SgxEnclaveStatus.Busy || e.Status == SgxEnclaveStatus.Running)
            .CountAsync();

        var avgJobDuration = await CalculateAverageJobDurationAsync();
        var successRate = totalJobs > 0 ? (decimal)completedJobs / totalJobs : 0;

        var resourceStats = await CalculateResourceStatisticsAsync();
        var enclaveStats = await GetEnclaveStatisticsAsync();

        return new ComputeStatistics
        {
            TotalJobs = totalJobs,
            ActiveJobs = activeJobs,
            CompletedJobs = completedJobs,
            FailedJobs = failedJobs,
            AvailableEnclaves = availableEnclaves,
            BusyEnclaves = busyEnclaves,
            AverageJobDurationMinutes = avgJobDuration,
            SuccessRate = successRate,
            TotalCpuHours = resourceStats.TotalCpuHours,
            TotalMemoryGbHours = resourceStats.TotalMemoryGbHours,
            LastUpdated = DateTime.UtcNow,
            EnclaveStats = enclaveStats
        };
    }

    public async Task<bool> UpdateJobStatusAsync(Guid jobId, ComputeJobStatus status, string? errorMessage = null)
    {
        var job = await _context.ComputeJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null) return false;

        job.Status = status;
        job.ErrorMessage = errorMessage;

        if (status == ComputeJobStatus.Running && job.StartedAt == null)
        {
            job.StartedAt = DateTime.UtcNow;
        }
        else if ((status == ComputeJobStatus.Completed || status == ComputeJobStatus.Failed) && job.CompletedAt == null)
        {
            job.CompletedAt = DateTime.UtcNow;
            if (job.StartedAt.HasValue)
            {
                job.ActualDuration = job.CompletedAt.Value - job.StartedAt.Value;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignJobToEnclaveAsync(Guid jobId, Guid enclaveId)
    {
        var job = await _context.ComputeJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null) return false;

        job.EnclaveId = enclaveId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ComputeJobResponse>> GetJobsForEnclaveAsync(Guid enclaveId, ComputeJobStatus? status = null)
    {
        var query = _context.ComputeJobs
            .Where(j => j.EnclaveId == enclaveId)
            .Include(j => j.Enclave);

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();
        return jobs.Select(MapToResponse).ToList();
    }

    private ComputeJobResponse MapToResponse(ComputeJob job)
    {
        return new ComputeJobResponse
        {
            JobId = job.Id.ToString(),
            Status = job.Status.ToString(),
            EnclaveId = job.EnclaveId?.ToString(),
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            EstimatedDuration = job.EstimatedDuration,
            ActualDuration = job.ActualDuration,
            Progress = CalculateProgress(job),
            ResultHash = job.ResultHash,
            ErrorMessage = job.ErrorMessage,
            ResourceUsage = new ComputeResourceUsage
            {
                CpuCores = job.CpuCores,
                MemoryUsedMb = job.MemoryMb,
                StorageUsedMb = job.StorageMb,
                Duration = job.ActualDuration ?? TimeSpan.Zero,
                Cost = 0 // Would be calculated based on resource usage
            }
        };
    }

    private static int CalculateProgress(ComputeJob job)
    {
        return job.Status switch
        {
            ComputeJobStatus.Pending => 0,
            ComputeJobStatus.Queued => 10,
            ComputeJobStatus.Running => 50,
            ComputeJobStatus.Completed => 100,
            ComputeJobStatus.Failed => 0,
            ComputeJobStatus.Cancelled => 0,
            ComputeJobStatus.Timeout => 0,
            _ => 0
        };
    }

    private async Task<decimal> CalculateAverageWaitTimeAsync()
    {
        var completedJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Completed && j.StartedAt.HasValue)
            .Select(j => new { j.CreatedAt, j.StartedAt })
            .ToListAsync();

        if (!completedJobs.Any()) return 0;

        var waitTimes = completedJobs
            .Select(j => (j.StartedAt!.Value - j.CreatedAt).TotalMinutes)
            .ToList();

        return (decimal)waitTimes.Average();
    }

    private async Task<decimal> CalculateEstimatedProcessingTimeAsync()
    {
        var runningJobsCount = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Running)
            .CountAsync();

        var avgProcessingTime = await CalculateAverageJobDurationAsync();

        // Simple estimation: average processing time * number of running jobs
        return avgProcessingTime * runningJobsCount;
    }

    private async Task<decimal> CalculateAverageJobDurationAsync()
    {
        var completedJobs = await _context.ComputeJobs
            .Where(j => j.Status == ComputeJobStatus.Completed && j.ActualDuration.HasValue)
            .Select(j => j.ActualDuration!.Value.TotalMinutes)
            .ToListAsync();

        return completedJobs.Any() ? (decimal)completedJobs.Average() : 0;
    }

    private async Task<(long TotalCpuHours, long TotalMemoryGbHours)> CalculateResourceStatisticsAsync()
    {
        var resourceAllocations = await _context.ResourceAllocations
            .Where(r => r.Duration.HasValue)
            .ToListAsync();

        var totalCpuHours = resourceAllocations
            .Where(r => r.ResourceType == "CPU")
            .Sum(r => (long)(r.AllocatedAmount * (decimal)r.Duration!.Value.TotalHours));

        var totalMemoryGbHours = resourceAllocations
            .Where(r => r.ResourceType == "Memory")
            .Sum(r => (long)((r.AllocatedAmount / 1024) * (decimal)r.Duration!.Value.TotalHours)); // Convert MB to GB

        return (totalCpuHours, totalMemoryGbHours);
    }

    private async Task<List<EnclaveStatistic>> GetEnclaveStatisticsAsync()
    {
        var enclaves = await _context.SgxEnclaves.ToListAsync();
        var enclaveStats = new List<EnclaveStatistic>();

        foreach (var enclave in enclaves)
        {
            var jobsProcessed = await _context.ComputeJobs
                .Where(j => j.EnclaveId == enclave.Id && j.Status == ComputeJobStatus.Completed)
                .CountAsync();

            var avgDuration = await _context.ComputeJobs
                .Where(j => j.EnclaveId == enclave.Id && j.ActualDuration.HasValue)
                .Select(j => j.ActualDuration!.Value.TotalMinutes)
                .DefaultIfEmpty(0)
                .AverageAsync();

            enclaveStats.Add(new EnclaveStatistic
            {
                EnclaveId = enclave.Id.ToString(),
                Name = enclave.Name,
                Status = enclave.Status.ToString(),
                JobsProcessed = jobsProcessed,
                AverageJobDuration = (decimal)avgDuration,
                CpuUtilization = enclave.CpuUsagePercent,
                MemoryUtilization = enclave.MemoryUsageMb > 0 
                    ? (decimal)(enclave.MemoryUsageMb / 1024.0) // Convert to GB
                    : 0,
                LastActivity = enclave.LastHeartbeat ?? enclave.CreatedAt
            });
        }

        return enclaveStats;
    }
}