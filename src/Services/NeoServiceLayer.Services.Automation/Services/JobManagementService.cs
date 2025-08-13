using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;

namespace NeoServiceLayer.Services.Automation.Services
{
    /// <summary>
    /// Service for managing automation job lifecycle.
    /// </summary>
    public class JobManagementService : IJobManagementService
    {
        private readonly ILogger<JobManagementService> _logger;
        private readonly IPersistentStorageProvider? _persistentStorage;
        private readonly Dictionary<string, AutomationJob> _jobs;
        private readonly Dictionary<string, List<AutomationExecution>> _executionHistory;
        private readonly object _jobsLock = new();

        public JobManagementService(
            ILogger<JobManagementService> logger,
            IPersistentStorageProvider? persistentStorage = null)
        {
            _logger = logger;
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
                    Type = request.TriggerType,
                    Schedule = request.TriggerType == AutomationTriggerType.Schedule
                        ? request.TriggerConfiguration : null,
                    EventType = request.TriggerType == AutomationTriggerType.Event
                        ? request.TriggerConfiguration : null,
                    Configuration = new Dictionary<string, object>()
                },
                Conditions = Array.Empty<AutomationCondition>(),
                Status = AutomationJobStatus.Created,
                CreatedAt = DateTime.UtcNow,
                IsEnabled = request.IsActive,
                ExpiresAt = request.ExpiresAt,
                BlockchainType = blockchainType
            };

            // Parse action configuration
            if (request.ActionConfiguration != null)
            {
                job.TargetContract = request.ActionConfiguration.GetValueOrDefault("contract", string.Empty);
                job.TargetMethod = request.ActionConfiguration.GetValueOrDefault("method", string.Empty);
                job.Parameters = request.ActionConfiguration.GetValueOrDefault("parameters", new Dictionary<string, object>());
            }

            // Parse conditions
            if (request.ConditionConfiguration != null)
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

            _logger.LogInformation("Created automation job {JobId} for blockchain {Blockchain}",
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

            _logger.LogInformation("Cancelled automation job {JobId}", jobId);
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

            _logger.LogInformation("Paused automation job {JobId}", jobId);
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

            _logger.LogInformation("Resumed automation job {JobId}", jobId);
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

            _logger.LogInformation("Updated automation job {JobId}", jobId);
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
            _logger.LogDebug("Persisted job {JobId} to storage", job.Id);
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
            _logger.LogDebug("Loaded job {JobId} from storage", jobId);

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

            _logger.LogInformation("Cleaned up {Count} old executions older than {Cutoff}",
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
                            conditionDict.GetValueOrDefault("type", "Custom")),
                        Field = conditionDict.GetValueOrDefault("field", string.Empty),
                        Operator = conditionDict.GetValueOrDefault("operator", "equals"),
                        Value = conditionDict.GetValueOrDefault("value", string.Empty),
                        Configuration = conditionDict
                    };
                    conditions.Add(condition);
                }
            }

            return conditions.ToArray();
        }
    }
}
