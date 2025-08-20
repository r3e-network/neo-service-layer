using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Automation.Models;
using System.Text.Json;
using ServiceFrameworkBase = NeoServiceLayer.ServiceFramework.ServiceBase;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Automation.Services
{
    /// <summary>
    /// Service for managing automation job lifecycle.
    /// </summary>
    public class JobManagementService : ServiceFrameworkBase, IJobManagementService
    {
        private new readonly ILogger<JobManagementService> Logger;
        private readonly IPersistentStorageProvider? _persistentStorage;
        private readonly Dictionary<string, AutomationJob> _jobs;
        private readonly Dictionary<string, List<AutomationExecution>> _executionHistory;
        private readonly object _jobsLock = new();

        public JobManagementService(
            ILogger<JobManagementService> logger,
            IPersistentStorageProvider? persistentStorage = null)

        : base("JobManagementService", "1.0.0", "JobManagementService service", logger)
    {
            Logger = logger;
            _persistentStorage = persistentStorage;
            _jobs = new Dictionary<string, AutomationJob>();
            _executionHistory = new Dictionary<string, List<AutomationExecution>>();
        }

        public async Task<string> CreateJobAsync(AutomationJobRequest request, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(request);

            var automationId = Guid.NewGuid().ToString();
            var job = new AutomationJob
            {
                Id = automationId,
                Name = request.Name,
                Description = request.Description,
                OwnerAddress = request.OwnerAddress ?? string.Empty,
                TargetContract = string.Empty,
                TargetMethod = string.Empty,
                Parameters = new Dictionary<string, object>(),
                Trigger = new AutomationTrigger
                {
                    Type = request.Trigger.Type,
                    Schedule = request.Trigger.Type == AutomationTriggerType.Schedule
                        ? request.Trigger.Schedule : null,
                    EventType = request.Trigger.Type == AutomationTriggerType.Event
                        ? request.Trigger.EventType : null,
                    Configuration = request.Trigger.Configuration ?? new Dictionary<string, object>()
                },
                Conditions = Array.Empty<AutomationCondition>(),
                Status = AutomationJobStatus.Created,
                CreatedAt = DateTime.UtcNow,
                IsEnabled = request.IsEnabled,
                ExpiresAt = request.ExpiresAt,
                BlockchainType = blockchainType
            };

            // Set action configuration from request
            job.TargetContract = request.TargetContract;
            job.TargetMethod = request.TargetMethod;
            job.Parameters = request.Parameters;

            // Set conditions from request
            job.Conditions = request.Conditions;
            {
                job.Conditions = ParseConditions(request.ConditionConfiguration);
            }

            lock (_jobsLock)
            {
                _jobs[automationId] = job;
                _executionHistory[automationId] = new List<AutomationExecution>();
            }

            // Persist if storage available
            if (_persistentStorage != null)
            {
                await StoreJobAsync(job).ConfigureAwait(false);
            }

            Logger.LogInformation("Created automation job {JobId} for blockchain {Blockchain}",
                automationId, blockchainType);

            return automationId;
        }

        public async Task<AutomationJobStatus> GetJobStatusAsync(string jobId, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(jobId);

            AutomationJob? job;
            lock (_jobsLock)
            {
                if (!_jobs.TryGetValue(jobId, out job))
                {
                    return AutomationJobStatus.NotFound;
                }
            }

            if (job.BlockchainType != blockchainType)
            {
                throw new InvalidOperationException($"Job {jobId} belongs to {job.BlockchainType}, not {blockchainType}");
            }

            return await Task.FromResult(job.Status).ConfigureAwait(false);
        }

        public async Task<bool> CancelJobAsync(string jobId, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(jobId);

            AutomationJob? job;
            lock (_jobsLock)
            {
                if (!_jobs.TryGetValue(jobId, out job))
                {
                    return false;
                }

                if (job.BlockchainType != blockchainType)
                {
                    throw new InvalidOperationException($"Job {jobId} belongs to {job.BlockchainType}, not {blockchainType}");
                }

                if (job.Status == AutomationJobStatus.Cancelled ||
                    job.Status == AutomationJobStatus.Completed)
                {
                    return false;
                }

                job.Status = AutomationJobStatus.Cancelled;
                job.UpdatedAt = DateTime.UtcNow;
            }

            // Persist if storage available
            if (_persistentStorage != null)
            {
                await StoreJobAsync(job).ConfigureAwait(false);
            }

            Logger.LogInformation("Cancelled automation job {JobId}", jobId);
            return true;
        }

        public async Task<bool> PauseJobAsync(string jobId, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(jobId);

            AutomationJob? job;
            lock (_jobsLock)
            {
                if (!_jobs.TryGetValue(jobId, out job))
                {
                    return false;
                }

                if (job.BlockchainType != blockchainType)
                {
                    throw new InvalidOperationException($"Job {jobId} belongs to {job.BlockchainType}, not {blockchainType}");
                }

                if (job.Status != AutomationJobStatus.Active)
                {
                    return false;
                }

                job.Status = AutomationJobStatus.Paused;
                job.UpdatedAt = DateTime.UtcNow;
            }

            // Persist if storage available
            if (_persistentStorage != null)
            {
                await StoreJobAsync(job).ConfigureAwait(false);
            }

            Logger.LogInformation("Paused automation job {JobId}", jobId);
            return true;
        }

        public async Task<bool> ResumeJobAsync(string jobId, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(jobId);

            AutomationJob? job;
            lock (_jobsLock)
            {
                if (!_jobs.TryGetValue(jobId, out job))
                {
                    return false;
                }

                if (job.BlockchainType != blockchainType)
                {
                    throw new InvalidOperationException($"Job {jobId} belongs to {job.BlockchainType}, not {blockchainType}");
                }

                if (job.Status != AutomationJobStatus.Paused)
                {
                    return false;
                }

                job.Status = AutomationJobStatus.Active;
                job.UpdatedAt = DateTime.UtcNow;
            }

            // Persist if storage available
            if (_persistentStorage != null)
            {
                await StoreJobAsync(job).ConfigureAwait(false);
            }

            Logger.LogInformation("Resumed automation job {JobId}", jobId);
            return true;
        }

        public async Task<IEnumerable<AutomationJob>> GetJobsAsync(string address, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(address);

            List<AutomationJob> userJobs;
            lock (_jobsLock)
            {
                userJobs = _jobs.Values
                    .Where(j => j.OwnerAddress == address && j.BlockchainType == blockchainType)
                    .ToList();
            }

            return await Task.FromResult(userJobs).ConfigureAwait(false);
        }

        public async Task<bool> UpdateJobAsync(string jobId, AutomationJobUpdate update, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(jobId);
            ArgumentNullException.ThrowIfNull(update);

            AutomationJob? job;
            lock (_jobsLock)
            {
                if (!_jobs.TryGetValue(jobId, out job))
                {
                    return false;
                }

                if (job.BlockchainType != blockchainType)
                {
                    throw new InvalidOperationException($"Job {jobId} belongs to {job.BlockchainType}, not {blockchainType}");
                }

                // Update allowed fields
                if (!string.IsNullOrEmpty(update.Name))
                    job.Name = update.Name;

                if (!string.IsNullOrEmpty(update.Description))
                    job.Description = update.Description;

                if (update.IsEnabled.HasValue)
                    job.IsEnabled = update.IsEnabled.Value;

                if (update.ExpiresAt.HasValue)
                    job.ExpiresAt = update.ExpiresAt;

                if (update.Parameters != null)
                    job.Parameters = update.Parameters;

                job.UpdatedAt = DateTime.UtcNow;
            }

            // Persist if storage available
            if (_persistentStorage != null)
            {
                await StoreJobAsync(job).ConfigureAwait(false);
            }

            Logger.LogInformation("Updated automation job {JobId}", jobId);
            return true;
        }

        public async Task<IEnumerable<AutomationExecution>> GetExecutionHistoryAsync(string jobId, BlockchainType blockchainType)
        {
            ArgumentNullException.ThrowIfNull(jobId);

            List<AutomationExecution> history;
            lock (_jobsLock)
            {
                if (!_executionHistory.TryGetValue(jobId, out var executions))
                {
                    return Enumerable.Empty<AutomationExecution>();
                }

                history = executions.ToList();
            }

            return await Task.FromResult(history).ConfigureAwait(false);
        }

        public async Task StoreJobAsync(AutomationJob job)
        {
            if (_persistentStorage == null)
                return;

            var key = $"automation:job:{job.Id}";
            var data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(job);

            await _persistentStorage.StoreAsync(key, data).ConfigureAwait(false);
            Logger.LogDebug("Persisted job {JobId} to storage", job.Id);
        }

        public async Task<AutomationJob?> LoadJobAsync(string jobId)
        {
            if (_persistentStorage == null)
                return null;

            var key = $"automation:job:{jobId}";
            var data = await _persistentStorage.RetrieveAsync(key).ConfigureAwait(false);

            if (data == null)
                return null;

            var job = System.Text.Json.JsonSerializer.Deserialize<AutomationJob>(data);
            Logger.LogDebug("Loaded job {JobId} from storage", jobId);

            return job;
        }

        public async Task CleanupOldExecutionsAsync(TimeSpan retentionPeriod)
        {
            var cutoffDate = DateTime.UtcNow - retentionPeriod;
            var cleanedCount = 0;

            lock (_jobsLock)
            {
                foreach (var kvp in _executionHistory)
                {
                    var oldCount = kvp.Value.Count;
                    kvp.Value.RemoveAll(e => e.ExecutedAt < cutoffDate);
                    cleanedCount += oldCount - kvp.Value.Count;
                }
            }

            Logger.LogInformation("Cleaned up {Count} old executions older than {Cutoff}",
                cleanedCount, cutoffDate);

            await Task.CompletedTask.ConfigureAwait(false);
        }

        private AutomationCondition[] ParseConditions(Dictionary<string, object> config)
        {
            var conditions = new List<AutomationCondition>();

            if (config.TryGetValue("conditions", out var conditionsObj) &&
                conditionsObj is List<Dictionary<string, object>> conditionsList)
            {
                foreach (var conditionDict in conditionsList)
                {
                    var condition = new AutomationCondition
                    {
                        Type = Enum.Parse<AutomationConditionType>(
                            conditionDict.GetValueOrDefault("type", "Custom")?.ToString() ?? "Custom"),
                        Field = conditionDict.GetValueOrDefault("field", string.Empty)?.ToString() ?? string.Empty,
                        Operator = conditionDict.GetValueOrDefault("operator", "equals")?.ToString() ?? "equals",
                        Value = conditionDict.GetValueOrDefault("value", string.Empty)?.ToString() ?? string.Empty,
                        Configuration = conditionDict
                    };
                    conditions.Add(condition);
                }
            }

            return conditions.ToArray();
        }

        protected override async Task<bool> OnInitializeAsync()
        {
            Logger.LogDebug("Initializing Job Management Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStartAsync()
        {
            Logger.LogInformation("Starting Job Management Service");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStopAsync()
        {
            Logger.LogInformation("Stopping Job Management Service");
            return await Task.FromResult(true);
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                // Check if jobs dictionary is accessible
                var jobCount = _jobs.Count;
                Logger.LogDebug("Job Management Service health check: {JobCount} jobs managed", jobCount);
                return await Task.FromResult(ServiceHealth.Healthy);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Job Management Service health check failed");
                return await Task.FromResult(ServiceHealth.Unhealthy);
            }
        }
    }
}
