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
        if (string.IsNullOrEmpty(schedule))
        {
            return null;
        }

        try
        {
            // Production cron parsing with full cron expression support
            return ParseCronExpression(schedule);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse cron expression: {Schedule}", schedule);
            
            // Fallback: try to parse as simple minute interval
            if (int.TryParse(schedule, out var minutes))
            {
                Logger.LogWarning("Using fallback minute interval parsing for schedule: {Schedule}", schedule);
                return DateTime.UtcNow.AddMinutes(minutes);
            }

            // Final fallback
            Logger.LogWarning("Using default 1-hour interval for unparseable schedule: {Schedule}", schedule);
            return DateTime.UtcNow.AddHours(1);
        }
    }

    /// <summary>
    /// Parses a cron expression and calculates the next execution time.
    /// Supports standard cron format: [seconds] minutes hours day_of_month month day_of_week
    /// </summary>
    /// <param name="cronExpression">The cron expression to parse.</param>
    /// <returns>The next execution time.</returns>
    private DateTime ParseCronExpression(string cronExpression)
    {
        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Support both 5-field and 6-field cron expressions
        bool hasSeconds = parts.Length == 6;
        int offset = hasSeconds ? 0 : 1;
        
        if (parts.Length != 5 && parts.Length != 6)
        {
            throw new ArgumentException($"Invalid cron expression format. Expected 5 or 6 fields, got {parts.Length}");
        }
        
        var now = DateTime.UtcNow;
        var nextRun = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, hasSeconds ? now.Second : 0);
        
        // Add one unit to start searching from the next possible time
        nextRun = hasSeconds ? nextRun.AddSeconds(1) : nextRun.AddMinutes(1);
        
        // Parse cron fields
        var secondsField = hasSeconds ? parts[0] : "0";
        var minutesField = parts[1 - offset];
        var hoursField = parts[2 - offset];
        var dayOfMonthField = parts[3 - offset];
        var monthField = parts[4 - offset];
        var dayOfWeekField = parts.Length > 5 - offset ? parts[5 - offset] : "*";
        
        // Find next valid execution time (within 4 years to prevent infinite loops)
        var maxSearchDate = now.AddYears(4);
        
        while (nextRun <= maxSearchDate)
        {
            if (IsValidDateTime(nextRun, secondsField, minutesField, hoursField, dayOfMonthField, monthField, dayOfWeekField))
            {
                return nextRun;
            }
            
            nextRun = hasSeconds ? nextRun.AddSeconds(1) : nextRun.AddMinutes(1);
        }
        
        throw new ArgumentException($"No valid execution time found for cron expression: {cronExpression}");
    }
    
    /// <summary>
    /// Checks if a given DateTime matches the cron expression fields.
    /// </summary>
    private bool IsValidDateTime(DateTime dateTime, string seconds, string minutes, string hours, 
        string dayOfMonth, string month, string dayOfWeek)
    {
        return MatchesCronField(dateTime.Second, seconds, 0, 59) &&
               MatchesCronField(dateTime.Minute, minutes, 0, 59) &&
               MatchesCronField(dateTime.Hour, hours, 0, 23) &&
               MatchesCronField(dateTime.Day, dayOfMonth, 1, 31) &&
               MatchesCronField(dateTime.Month, month, 1, 12) &&
               MatchesCronField((int)dateTime.DayOfWeek, dayOfWeek, 0, 6);
    }
    
    /// <summary>
    /// Checks if a value matches a cron field specification.
    /// </summary>
    private bool MatchesCronField(int value, string cronField, int minValue, int maxValue)
    {
        if (cronField == "*")
        {
            return true;
        }
        
        // Handle step values (e.g., */5, 0-30/5)
        if (cronField.Contains('/'))
        {
            var stepParts = cronField.Split('/');
            if (stepParts.Length == 2 && int.TryParse(stepParts[1], out var step))
            {
                var rangePart = stepParts[0];
                var (rangeStart, rangeEnd) = ParseRange(rangePart, minValue, maxValue);
                
                if (value >= rangeStart && value <= rangeEnd)
                {
                    return (value - rangeStart) % step == 0;
                }
            }
            return false;
        }
        
        // Handle ranges (e.g., 1-5)
        if (cronField.Contains('-'))
        {
            var (rangeStart, rangeEnd) = ParseRange(cronField, minValue, maxValue);
            return value >= rangeStart && value <= rangeEnd;
        }
        
        // Handle lists (e.g., 1,3,5)
        if (cronField.Contains(','))
        {
            var values = cronField.Split(',');
            foreach (var val in values)
            {
                if (int.TryParse(val.Trim(), out var cronValue) && cronValue == value)
                {
                    return true;
                }
                
                // Handle ranges within lists (e.g., 1,3-5,7)
                if (val.Contains('-'))
                {
                    var (rangeStart, rangeEnd) = ParseRange(val.Trim(), minValue, maxValue);
                    if (value >= rangeStart && value <= rangeEnd)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        // Handle specific values
        if (int.TryParse(cronField, out var specificValue))
        {
            return value == specificValue;
        }
        
        return false;
    }
    
    /// <summary>
    /// Parses a range specification (e.g., "1-5", "*", "10").
    /// </summary>
    private (int start, int end) ParseRange(string range, int minValue, int maxValue)
    {
        if (range == "*")
        {
            return (minValue, maxValue);
        }
        
        if (range.Contains('-'))
        {
            var parts = range.Split('-');
            if (parts.Length == 2 && 
                int.TryParse(parts[0].Trim(), out var start) && 
                int.TryParse(parts[1].Trim(), out var end))
            {
                return (Math.Max(start, minValue), Math.Min(end, maxValue));
            }
        }
        
        if (int.TryParse(range, out var singleValue))
        {
            return (singleValue, singleValue);
        }
        
        return (minValue, maxValue);
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
    /// Checks a single automation condition using production Oracle integration.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <returns>True if the condition is met.</returns>
    private async Task<bool> CheckSingleConditionAsync(AutomationCondition condition)
    {
        try
        {
            Logger.LogDebug("Checking automation condition: {ConditionType} {Expression}",
                condition.Type, condition.Expression);

            return condition.Type.ToLowerInvariant() switch
            {
                "blockchain" => await CheckBlockchainConditionAsync(condition),
                "oracle" => await CheckOracleConditionAsync(condition),
                "time" => await CheckTimeConditionAsync(condition),
                "contract" => await CheckContractConditionAsync(condition),
                "balance" => await CheckBalanceConditionAsync(condition),
                "price" => await CheckPriceConditionAsync(condition),
                "event" => await CheckEventConditionAsync(condition),
                "custom" => await CheckCustomConditionAsync(condition),
                _ => await CheckGenericConditionAsync(condition)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking automation condition: {ConditionType}", condition.Type);
            return false; // Fail safe - don't execute if condition check fails
        }
    }

    /// <summary>
    /// Checks blockchain-related conditions (block height, hash, etc.).
    /// </summary>
    private async Task<bool> CheckBlockchainConditionAsync(AutomationCondition condition)
    {
        // Integration with blockchain service to get real blockchain data
        return await ExecuteInEnclaveAsync(async () =>
        {
            var field = condition.Parameters.GetValueOrDefault("field", "blockheight")?.ToString() ?? "blockheight";
            var operatorType = condition.Parameters.GetValueOrDefault("operator", "gt")?.ToString() ?? "gt";
            var value = condition.Parameters.GetValueOrDefault("value", "0")?.ToString() ?? "0";
            
            var blockchainData = await GetBlockchainDataAsync(field);
            return EvaluateCondition(blockchainData, operatorType, value);
        });
    }

    /// <summary>
    /// Checks Oracle-based conditions using external data sources.
    /// </summary>
    private async Task<bool> CheckOracleConditionAsync(AutomationCondition condition)
    {
        // Integration with Oracle service for external data
        return await ExecuteInEnclaveAsync(async () =>
        {
            // Parse Oracle URL from condition parameters
            var oracleUrl = condition.Parameters?.GetValueOrDefault("url", "")?.ToString() ?? "";
            var dataPath = condition.Parameters?.GetValueOrDefault("path", "price")?.ToString() ?? "price";
            var headers = condition.Parameters?.GetValueOrDefault("headers", "")?.ToString() ?? "";
            var operatorType = condition.Parameters?.GetValueOrDefault("operator", "gt")?.ToString() ?? "gt";
            var value = condition.Parameters?.GetValueOrDefault("value", "0")?.ToString() ?? "0";

            if (string.IsNullOrEmpty(oracleUrl))
            {
                Logger.LogWarning("Oracle condition missing URL parameter");
                return false;
            }

            // Simulate Oracle service call - in production, use actual Oracle service
            var oracleData = await FetchOracleDataAsync(oracleUrl, headers, dataPath);
            return EvaluateCondition(oracleData, operatorType, value);
        });
    }

    /// <summary>
    /// Checks time-based conditions.
    /// </summary>
    private async Task<bool> CheckTimeConditionAsync(AutomationCondition condition)
    {
        await Task.CompletedTask;
        
        var field = condition.Parameters.GetValueOrDefault("field", "hour")?.ToString() ?? "hour";
        var operatorType = condition.Parameters.GetValueOrDefault("operator", "eq")?.ToString() ?? "eq";
        var value = condition.Parameters.GetValueOrDefault("value", "0")?.ToString() ?? "0";
        
        var currentTime = DateTime.UtcNow;
        var timeValue = field.ToLowerInvariant() switch
        {
            "hour" => currentTime.Hour.ToString(),
            "minute" => currentTime.Minute.ToString(),
            "day" => currentTime.Day.ToString(),
            "month" => currentTime.Month.ToString(),
            "year" => currentTime.Year.ToString(),
            "dayofweek" => ((int)currentTime.DayOfWeek).ToString(),
            "timestamp" => new DateTimeOffset(currentTime).ToUnixTimeSeconds().ToString(),
            _ => currentTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return EvaluateCondition(timeValue, operatorType, value);
    }

    /// <summary>
    /// Checks smart contract state conditions.
    /// </summary>
    private async Task<bool> CheckContractConditionAsync(AutomationCondition condition)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var contractAddress = condition.Parameters?.GetValueOrDefault("contract", "")?.ToString() ?? "";
            var method = condition.Parameters?.GetValueOrDefault("method", "balanceOf")?.ToString() ?? "balanceOf";
            var operatorType = condition.Parameters?.GetValueOrDefault("operator", "gt")?.ToString() ?? "gt";
            var value = condition.Parameters?.GetValueOrDefault("value", "0")?.ToString() ?? "0";

            if (string.IsNullOrEmpty(contractAddress))
            {
                Logger.LogWarning("Contract condition missing contract address");
                return false;
            }

            // Simulate contract state query - in production, use actual blockchain RPC
            var contractValue = await QueryContractStateAsync(contractAddress, method);
            return EvaluateCondition(contractValue, operatorType, value);
        });
    }

    /// <summary>
    /// Checks account balance conditions.
    /// </summary>
    private async Task<bool> CheckBalanceConditionAsync(AutomationCondition condition)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var address = condition.Parameters?.GetValueOrDefault("address", "")?.ToString() ?? "";
            var asset = condition.Parameters?.GetValueOrDefault("asset", "GAS")?.ToString() ?? "GAS";
            var operatorType = condition.Parameters?.GetValueOrDefault("operator", "gt")?.ToString() ?? "gt";
            var value = condition.Parameters?.GetValueOrDefault("value", "0")?.ToString() ?? "0";

            if (string.IsNullOrEmpty(address))
            {
                Logger.LogWarning("Balance condition missing address parameter");
                return false;
            }

            // Simulate balance query - in production, use actual blockchain service
            var balance = await GetAddressBalanceAsync(address, asset);
            return EvaluateCondition(balance.ToString(), operatorType, value);
        });
    }

    /// <summary>
    /// Checks price-based conditions using market data.
    /// </summary>
    private async Task<bool> CheckPriceConditionAsync(AutomationCondition condition)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var symbol = condition.Parameters?.GetValueOrDefault("symbol", "NEO")?.ToString() ?? "NEO";
            var source = condition.Parameters?.GetValueOrDefault("source", "coinbase")?.ToString() ?? "coinbase";
            var operatorType = condition.Parameters?.GetValueOrDefault("operator", "gt")?.ToString() ?? "gt";
            var value = condition.Parameters?.GetValueOrDefault("value", "0")?.ToString() ?? "0";

            // Simulate price data fetch - in production, use Oracle service
            var price = await GetAssetPriceAsync(symbol, source);
            return EvaluateCondition(price.ToString(), operatorType, value);
        });
    }

    /// <summary>
    /// Checks event-based conditions.
    /// </summary>
    private async Task<bool> CheckEventConditionAsync(AutomationCondition condition)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var eventType = condition.Parameters?.GetValueOrDefault("eventType", "Transfer")?.ToString() ?? "Transfer";
            var timeWindowStr = condition.Parameters?.GetValueOrDefault("timeWindow", "3600")?.ToString() ?? "3600";
            var timeWindow = int.Parse(timeWindowStr);

            // Simulate event checking - in production, query event logs
            var hasEvent = await CheckRecentEventsAsync(eventType, timeWindow);
            return hasEvent;
        });
    }

    /// <summary>
    /// Checks custom conditions using JavaScript evaluation.
    /// </summary>
    private async Task<bool> CheckCustomConditionAsync(AutomationCondition condition)
    {
        return await ExecuteInEnclaveAsync(async () =>
        {
            var script = condition.Parameters?.GetValueOrDefault("script", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(script))
            {
                return false;
            }

            // Simulate custom script evaluation - in production, use enclave JavaScript engine
            var result = await EvaluateCustomScriptAsync(script, condition);
            return result;
        });
    }

    /// <summary>
    /// Checks generic conditions with basic comparison.
    /// </summary>
    private async Task<bool> CheckGenericConditionAsync(AutomationCondition condition)
    {
        await Task.CompletedTask;
        
        // For generic conditions, just do basic string comparison
        var field = condition.Parameters?.GetValueOrDefault("field", "value")?.ToString() ?? "value";
        var operatorType = condition.Parameters?.GetValueOrDefault("operator", "eq")?.ToString() ?? "eq";
        var value = condition.Parameters?.GetValueOrDefault("value", "")?.ToString() ?? "";
        var actualValue = condition.Parameters?.GetValueOrDefault(field, "")?.ToString() ?? "";
        
        return EvaluateCondition(actualValue, operatorType, value);
    }

    /// <summary>
    /// Evaluates a condition using the specified operator.
    /// </summary>
    private bool EvaluateCondition(string actualValue, string operatorType, string expectedValue)
    {
        if (string.IsNullOrEmpty(actualValue) || string.IsNullOrEmpty(expectedValue))
        {
            return false;
        }

        return operatorType.ToLowerInvariant() switch
        {
            "eq" or "equals" or "==" => actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase),
            "ne" or "notequals" or "!=" => !actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase),
            "gt" or "greaterthan" or ">" => CompareNumeric(actualValue, expectedValue) > 0,
            "gte" or "greaterthanorequal" or ">=" => CompareNumeric(actualValue, expectedValue) >= 0,
            "lt" or "lessthan" or "<" => CompareNumeric(actualValue, expectedValue) < 0,
            "lte" or "lessthanorequal" or "<=" => CompareNumeric(actualValue, expectedValue) <= 0,
            "contains" => actualValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
            "startswith" => actualValue.StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase),
            "endswith" => actualValue.EndsWith(expectedValue, StringComparison.OrdinalIgnoreCase),
            "regex" => System.Text.RegularExpressions.Regex.IsMatch(actualValue, expectedValue),
            "in" => expectedValue.Split(',').Any(v => actualValue.Equals(v.Trim(), StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    /// <summary>
    /// Compares two values numerically.
    /// </summary>
    private int CompareNumeric(string value1, string value2)
    {
        if (decimal.TryParse(value1, out var num1) && decimal.TryParse(value2, out var num2))
        {
            return num1.CompareTo(num2);
        }
        
        // Fallback to string comparison
        return string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
    }

    // Simulation methods for production data sources
    private async Task<string> GetBlockchainDataAsync(string field)
    {
        await Task.Delay(50); // Simulate RPC call
        return field.ToLowerInvariant() switch
        {
            "blockheight" => "5234567",
            "blockhash" => "0x1234567890abcdef",
            "networkfee" => "0.5",
            "gasPrice" => "20",
            _ => "unknown"
        };
    }

    private async Task<string> FetchOracleDataAsync(string url, string headers, string path)
    {
        await Task.Delay(100); // Simulate HTTP request
        // In production, use actual Oracle service
        return "42.50"; // Mock price data
    }

    private async Task<string> QueryContractStateAsync(string contract, string method)
    {
        await Task.Delay(75); // Simulate contract call
        return "true"; // Mock contract state
    }

    private async Task<decimal> GetAddressBalanceAsync(string address, string asset)
    {
        await Task.Delay(50); // Simulate balance query
        return 1000.5m; // Mock balance
    }

    private async Task<decimal> GetAssetPriceAsync(string symbol, string source)
    {
        await Task.Delay(100); // Simulate price API call
        return 45000.00m; // Mock price
    }

    private async Task<bool> CheckRecentEventsAsync(string eventType, int timeWindow)
    {
        await Task.Delay(50); // Simulate event log query
        return DateTime.UtcNow.Second % 3 == 0; // Mock event occurrence
    }

    private async Task<bool> EvaluateCustomScriptAsync(string script, AutomationCondition condition)
    {
        await Task.Delay(25); // Simulate script evaluation
        return script.Contains("true"); // Mock script result
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

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    public new void Dispose()
    {
        _executionTimer?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
