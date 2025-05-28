using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;

namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Implementation of the Automation Service that provides smart contract automation and scheduling capabilities.
/// </summary>
public class AutomationService : EnclaveBlockchainServiceBase, IAutomationService, IDisposable
{
    private readonly Dictionary<string, AutomationJob> _jobs = new();
    private readonly Dictionary<string, List<AutomationExecution>> _executionHistory = new();
    private readonly object _jobsLock = new();
    private readonly Timer _executionTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutomationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    public AutomationService(ILogger<AutomationService> logger, IServiceConfiguration? configuration = null)
        : base("AutomationService", "Smart contract automation and scheduling service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        Configuration = configuration;

        // Initialize execution timer (runs every minute)
        _executionTimer = new Timer(ExecuteScheduledJobs, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        AddCapability<IAutomationService>();
        AddDependency(new ServiceDependency("EventSubscriptionService", true, "1.0.0"));
        AddDependency(new ServiceDependency("OracleService", false, "1.0.0"));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected IServiceConfiguration? Configuration { get; }

    /// <inheritdoc/>
    public async Task<string> CreateJobAsync(AutomationJobRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var jobId = Guid.NewGuid().ToString();
            var job = new AutomationJob
            {
                Id = jobId,
                Name = request.Name,
                Description = request.Description,
                OwnerAddress = request.OwnerAddress,
                TargetContract = request.TargetContract,
                TargetMethod = request.TargetMethod,
                Parameters = request.Parameters,
                Trigger = request.Trigger,
                Conditions = request.Conditions,
                Status = AutomationJobStatus.Created,
                CreatedAt = DateTime.UtcNow,
                IsEnabled = request.IsEnabled,
                ExpiresAt = request.ExpiresAt
            };

            // Calculate next execution time
            job.NextExecution = CalculateNextExecution(job.Trigger);

            lock (_jobsLock)
            {
                _jobs[jobId] = job;
                _executionHistory[jobId] = new List<AutomationExecution>();
            }

            // Activate the job if enabled
            if (job.IsEnabled)
            {
                job.Status = AutomationJobStatus.Active;
            }

            await Task.CompletedTask; // Ensure async operation
            Logger.LogInformation("Created automation job {JobId} for contract {Contract} on {Blockchain}",
                jobId, request.TargetContract, blockchainType);

            return jobId;
        });
    }

    /// <inheritdoc/>
    public async Task<AutomationJobStatus> GetJobStatusAsync(string jobId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        lock (_jobsLock)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                return job.Status;
            }
        }

        await Task.CompletedTask;
        throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
    }

    /// <inheritdoc/>
    public async Task<bool> CancelJobAsync(string jobId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            lock (_jobsLock)
            {
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    if (job.Status == AutomationJobStatus.Active || job.Status == AutomationJobStatus.Paused)
                    {
                        job.Status = AutomationJobStatus.Cancelled;
                        job.IsEnabled = false;

                        Logger.LogInformation("Cancelled automation job {JobId} on {Blockchain}", jobId, blockchainType);
                        return true;
                    }
                }
            }

            await Task.CompletedTask; // Ensure async operation
            Logger.LogWarning("Job {JobId} not found or cannot be cancelled on {Blockchain}", jobId, blockchainType);
            return false;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> PauseJobAsync(string jobId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            lock (_jobsLock)
            {
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    if (job.Status == AutomationJobStatus.Active)
                    {
                        job.Status = AutomationJobStatus.Paused;
                        job.IsEnabled = false;

                        Logger.LogInformation("Paused automation job {JobId} on {Blockchain}", jobId, blockchainType);
                        return true;
                    }
                }
            }

            await Task.CompletedTask; // Ensure async operation
            Logger.LogWarning("Job {JobId} not found or cannot be paused on {Blockchain}", jobId, blockchainType);
            return false;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ResumeJobAsync(string jobId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            lock (_jobsLock)
            {
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    if (job.Status == AutomationJobStatus.Paused)
                    {
                        job.Status = AutomationJobStatus.Active;
                        job.IsEnabled = true;
                        job.NextExecution = CalculateNextExecution(job.Trigger);

                        Logger.LogInformation("Resumed automation job {JobId} on {Blockchain}", jobId, blockchainType);
                        return true;
                    }
                }
            }

            await Task.CompletedTask; // Ensure async operation
            Logger.LogWarning("Job {JobId} not found or cannot be resumed on {Blockchain}", jobId, blockchainType);
            return false;
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AutomationJob>> GetJobsAsync(string address, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(address);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        IEnumerable<AutomationJob> jobs;
        lock (_jobsLock)
        {
            jobs = _jobs.Values.Where(j => j.OwnerAddress.Equals(address, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        await Task.CompletedTask;
        return jobs;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateJobAsync(string jobId, AutomationJobUpdate update, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);
        ArgumentNullException.ThrowIfNull(update);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            lock (_jobsLock)
            {
                if (_jobs.TryGetValue(jobId, out var job))
                {
                    // Update job properties (only if provided)
                    if (update.Name != null) job.Name = update.Name;
                    if (update.Description != null) job.Description = update.Description;
                    if (update.Trigger != null) job.Trigger = update.Trigger;
                    if (update.Conditions != null) job.Conditions = update.Conditions;
                    if (update.IsEnabled.HasValue) job.IsEnabled = update.IsEnabled.Value;
                    if (update.ExpiresAt.HasValue) job.ExpiresAt = update.ExpiresAt;

                    // Recalculate next execution
                    job.NextExecution = CalculateNextExecution(job.Trigger);

                    // Update status based on enabled state
                    if (job.IsEnabled && job.Status == AutomationJobStatus.Paused)
                    {
                        job.Status = AutomationJobStatus.Active;
                    }
                    else if (!job.IsEnabled && job.Status == AutomationJobStatus.Active)
                    {
                        job.Status = AutomationJobStatus.Paused;
                    }

                    Logger.LogInformation("Updated automation job {JobId} on {Blockchain}", jobId, blockchainType);
                    return true;
                }
            }

            await Task.CompletedTask; // Ensure async operation
            Logger.LogWarning("Job {JobId} not found for update on {Blockchain}", jobId, blockchainType);
            return false;
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AutomationExecution>> GetExecutionHistoryAsync(string jobId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(jobId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        List<AutomationExecution>? history = null;
        lock (_jobsLock)
        {
            if (_executionHistory.TryGetValue(jobId, out var historyList))
            {
                history = historyList.ToList();
            }
        }

        await Task.CompletedTask;
        return history ?? Enumerable.Empty<AutomationExecution>();
    }

    /// <summary>
    /// Executes scheduled jobs.
    /// </summary>
    /// <param name="state">Timer state.</param>
    private async void ExecuteScheduledJobs(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var jobsToExecute = new List<AutomationJob>();

            lock (_jobsLock)
            {
                jobsToExecute.AddRange(_jobs.Values.Where(job =>
                    job.Status == AutomationJobStatus.Active &&
                    job.IsEnabled &&
                    job.NextExecution.HasValue &&
                    job.NextExecution.Value <= now &&
                    (!job.ExpiresAt.HasValue || job.ExpiresAt.Value > now)));
            }

            foreach (var job in jobsToExecute)
            {
                await ExecuteJobAsync(job);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing scheduled jobs");
        }
    }

    /// <summary>
    /// Executes a specific automation job.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    private async Task ExecuteJobAsync(AutomationJob job)
    {
        var execution = new AutomationExecution
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            ExecutedAt = DateTime.UtcNow,
            Status = AutomationExecutionStatus.Executing
        };

        try
        {
            Logger.LogDebug("Executing automation job {JobId}", job.Id);

            // Check conditions before execution
            if (job.Conditions.Length > 0)
            {
                var conditionsMet = await CheckConditionsAsync(job.Conditions);
                if (!conditionsMet)
                {
                    Logger.LogDebug("Conditions not met for job {JobId}, skipping execution", job.Id);

                    // Update next execution time
                    job.NextExecution = CalculateNextExecution(job.Trigger);
                    return;
                }
            }

            // Execute the job within the enclave
            var result = await ExecuteInEnclaveAsync(async () =>
            {
                // Simulate contract execution (in real implementation, this would call the blockchain)
                await Task.Delay(100); // Simulate execution time
                return $"Executed {job.TargetMethod} on {job.TargetContract}";
            });

            execution.Status = AutomationExecutionStatus.Completed;
            execution.Result = result;
            execution.TransactionHash = Guid.NewGuid().ToString(); // Simulate transaction hash

            // Update job statistics
            job.LastExecuted = DateTime.UtcNow;
            job.ExecutionCount++;
            job.NextExecution = CalculateNextExecution(job.Trigger);

            Logger.LogInformation("Successfully executed automation job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            execution.Status = AutomationExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;

            Logger.LogError(ex, "Failed to execute automation job {JobId}", job.Id);
        }
        finally
        {
            execution.ExecutionTime = DateTime.UtcNow - execution.ExecutedAt;

            // Record execution history
            lock (_jobsLock)
            {
                if (_executionHistory.TryGetValue(job.Id, out var history))
                {
                    history.Add(execution);

                    // Keep only last 100 executions
                    if (history.Count > 100)
                    {
                        history.RemoveAt(0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculates the next execution time for a trigger.
    /// </summary>
    /// <param name="trigger">The automation trigger.</param>
    /// <returns>The next execution time.</returns>
    private DateTime? CalculateNextExecution(AutomationTrigger trigger)
    {
        return trigger.Type switch
        {
            AutomationTriggerType.Time => CalculateNextTimeExecution(trigger.Schedule),
            AutomationTriggerType.Event => null, // Event-based triggers don't have scheduled execution
            AutomationTriggerType.Condition => DateTime.UtcNow.AddMinutes(5), // Check conditions every 5 minutes
            AutomationTriggerType.Manual => null, // Manual triggers don't have scheduled execution
            _ => null
        };
    }

    /// <summary>
    /// Calculates the next execution time for a time-based trigger.
    /// </summary>
    /// <param name="schedule">The cron schedule.</param>
    /// <returns>The next execution time.</returns>
    private DateTime? CalculateNextTimeExecution(string schedule)
    {
        // Simplified cron parsing - in real implementation, use a proper cron library
        if (string.IsNullOrEmpty(schedule))
        {
            return null;
        }

        // For demo purposes, assume schedule is in minutes
        if (int.TryParse(schedule, out var minutes))
        {
            return DateTime.UtcNow.AddMinutes(minutes);
        }

        return DateTime.UtcNow.AddHours(1); // Default to 1 hour
    }

    /// <summary>
    /// Checks if automation conditions are met.
    /// </summary>
    /// <param name="conditions">The conditions to check.</param>
    /// <returns>True if all conditions are met.</returns>
    private async Task<bool> CheckConditionsAsync(AutomationCondition[] conditions)
    {
        foreach (var condition in conditions)
        {
            var conditionMet = await CheckSingleConditionAsync(condition);
            if (!conditionMet)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks a single automation condition.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <returns>True if the condition is met.</returns>
    private async Task<bool> CheckSingleConditionAsync(AutomationCondition condition)
    {
        // Simplified condition checking - in real implementation, integrate with oracle service
        await Task.Delay(10); // Simulate condition checking

        // For demo purposes, randomly return true/false
        return Random.Shared.NextDouble() > 0.3; // 70% chance condition is met
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Automation Service enclave...");

            // Initialize automation-specific enclave components
            await Task.Delay(100); // Simulate enclave initialization

            Logger.LogInformation("Automation Service enclave initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Automation Service enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Automation Service");

        // Base initialization is handled by the framework
        await Task.CompletedTask;

        Logger.LogInformation("Automation Service initialized successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Automation Service");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Automation Service");

        // Dispose timer
        _executionTimer?.Dispose();

        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check automation-specific health
        var activeJobCount = _jobs.Values.Count(j => j.Status == AutomationJobStatus.Active);

        Logger.LogDebug("Automation Service health check: {ActiveJobs} active jobs", activeJobCount);

        var health = new ServiceHealth
        {
            ServiceName = ServiceName,
            IsHealthy = IsRunning,
            Status = IsRunning ? "Running" : "Stopped",
            LastChecked = DateTime.UtcNow
        };

        return Task.FromResult(health);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _executionTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
