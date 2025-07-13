using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.BackgroundJobs;

public static class BackgroundJobsExtensions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BackgroundJobOptions>(configuration.GetSection("BackgroundJobs"));

        // Add job storage
        services.AddDbContext<JobDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("JobsDb"));
        });

        // Add job queue
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();

        // Add job services
        services.AddScoped<IJobStore, DatabaseJobStore>();
        services.AddSingleton<IJobSerializer, JsonJobSerializer>();
        services.AddSingleton<IJobScheduler, JobScheduler>();
        services.AddSingleton<IJobExecutor, JobExecutor>();
        services.AddSingleton<IJobRegistry, JobRegistry>();

        // Add recurring job service
        services.AddSingleton<IRecurringJobManager, RecurringJobManager>();
        services.AddHostedService<RecurringJobScheduler>();

        // Add job processing service
        services.AddHostedService<JobProcessingService>();

        // Add job monitoring
        services.AddSingleton<IJobMonitor, JobMonitor>();

        return services;
    }

    public static IApplicationBuilder UseBackgroundJobs(this IApplicationBuilder app)
    {
        // Ensure database is created
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<JobDbContext>();
            context.Database.EnsureCreated();
        }

        return app;
    }
}

// Job interfaces
public interface IJob
{
    Task ExecuteAsync(JobContext context, CancellationToken cancellationToken);
}

public interface IJobWithResult<TResult> : IJob
{
    new Task<TResult> ExecuteAsync(JobContext context, CancellationToken cancellationToken);
}

// Job context
public class JobContext
{
    public Guid JobId { get; set; }
    public string JobType { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public IJobLogger Logger { get; set; }

    public T GetParameter<T>(string key, T defaultValue = default)
    {
        if (Parameters != null && Parameters.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return defaultValue;
    }
}

// Job queue interface
public interface IJobQueue
{
    Task<bool> EnqueueAsync(JobInfo job, CancellationToken cancellationToken = default);
    Task<JobInfo> DequeueAsync(CancellationToken cancellationToken = default);
    Task<int> GetQueueLengthAsync(string queue = null);
}

// In-memory job queue implementation
public class InMemoryJobQueue : IJobQueue
{
    private readonly ConcurrentDictionary<string, Channel<JobInfo>> _queues = new();
    private readonly ILogger<InMemoryJobQueue> _logger;
    private readonly BackgroundJobOptions _options;

    public InMemoryJobQueue(ILogger<InMemoryJobQueue> logger, IOptions<BackgroundJobOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> EnqueueAsync(JobInfo job, CancellationToken cancellationToken = default)
    {
        var queue = GetOrCreateQueue(job.Queue);
        return await queue.Writer.TryWriteAsync(job, cancellationToken);
    }

    public async Task<JobInfo> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var queues = _queues.Values.ToList();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var queue in queues)
            {
                if (await queue.Reader.WaitToReadAsync(cancellationToken))
                {
                    if (queue.Reader.TryRead(out var job))
                    {
                        return job;
                    }
                }
            }
        }

        return null;
    }

    public Task<int> GetQueueLengthAsync(string queueName = null)
    {
        if (queueName != null)
        {
            var queue = GetOrCreateQueue(queueName);
            return Task.FromResult(queue.Reader.Count);
        }

        var totalCount = _queues.Values.Sum(q => q.Reader.Count);
        return Task.FromResult(totalCount);
    }

    private Channel<JobInfo> GetOrCreateQueue(string queueName)
    {
        return _queues.GetOrAdd(queueName ?? "default", _ =>
        {
            var options = new BoundedChannelOptions(_options.MaxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            return Channel.CreateBounded<JobInfo>(options);
        });
    }
}

// Job store interface
public interface IJobStore
{
    Task<Guid> CreateJobAsync(JobInfo job, CancellationToken cancellationToken = default);
    Task<JobInfo> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task UpdateJobAsync(JobInfo job, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobInfo>> GetPendingJobsAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobInfo>> GetJobsByStatusAsync(JobStatus status, int count, CancellationToken cancellationToken = default);
    Task<JobStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

// Database job store
public class DatabaseJobStore : IJobStore
{
    private readonly JobDbContext _context;
    private readonly ILogger<DatabaseJobStore> _logger;

    public DatabaseJobStore(JobDbContext context, ILogger<DatabaseJobStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> CreateJobAsync(JobInfo job, CancellationToken cancellationToken = default)
    {
        var entity = new JobEntity
        {
            Id = job.Id,
            Type = job.Type,
            Queue = job.Queue,
            Parameters = JsonSerializer.Serialize(job.Parameters),
            Status = job.Status,
            Priority = job.Priority,
            MaxRetries = job.MaxRetries,
            RetryCount = job.RetryCount,
            ScheduledAt = job.ScheduledAt,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };

        _context.Jobs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task<JobInfo> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Jobs.FindAsync(new object[] { jobId }, cancellationToken);
        return entity != null ? MapToJobInfo(entity) : null;
    }

    public async Task UpdateJobAsync(JobInfo job, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Jobs.FindAsync(new object[] { job.Id }, cancellationToken);
        if (entity != null)
        {
            entity.Status = job.Status;
            entity.Result = job.Result != null ? JsonSerializer.Serialize(job.Result) : null;
            entity.Error = job.Error;
            entity.RetryCount = job.RetryCount;
            entity.NextRetryAt = job.NextRetryAt;
            entity.StartedAt = job.StartedAt;
            entity.CompletedAt = job.CompletedAt;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<JobInfo>> GetPendingJobsAsync(int count, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Jobs
            .Where(j => j.Status == JobStatus.Pending && j.ScheduledAt <= DateTimeOffset.UtcNow)
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToJobInfo);
    }

    public async Task<IEnumerable<JobInfo>> GetJobsByStatusAsync(JobStatus status, int count, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Jobs
            .Where(j => j.Status == status)
            .OrderByDescending(j => j.UpdatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToJobInfo);
    }

    public async Task<JobStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _context.Jobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new JobStatistics
        {
            TotalJobs = stats.Sum(s => s.Count),
            PendingJobs = stats.FirstOrDefault(s => s.Status == JobStatus.Pending)?.Count ?? 0,
            RunningJobs = stats.FirstOrDefault(s => s.Status == JobStatus.Running)?.Count ?? 0,
            CompletedJobs = stats.FirstOrDefault(s => s.Status == JobStatus.Completed)?.Count ?? 0,
            FailedJobs = stats.FirstOrDefault(s => s.Status == JobStatus.Failed)?.Count ?? 0
        };
    }

    private JobInfo MapToJobInfo(JobEntity entity)
    {
        return new JobInfo
        {
            Id = entity.Id,
            Type = entity.Type,
            Queue = entity.Queue,
            Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Parameters),
            Status = entity.Status,
            Priority = entity.Priority,
            MaxRetries = entity.MaxRetries,
            RetryCount = entity.RetryCount,
            Result = entity.Result != null ? JsonSerializer.Deserialize<object>(entity.Result) : null,
            Error = entity.Error,
            ScheduledAt = entity.ScheduledAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            NextRetryAt = entity.NextRetryAt
        };
    }
}

// Job scheduler interface
public interface IJobScheduler
{
    Task<Guid> ScheduleAsync<TJob>(Dictionary<string, object> parameters = null, DateTimeOffset? scheduledAt = null) where TJob : IJob;
    Task<Guid> ScheduleAsync(Type jobType, Dictionary<string, object> parameters = null, DateTimeOffset? scheduledAt = null);
    Task<Guid> ScheduleAsync(string jobType, Dictionary<string, object> parameters = null, DateTimeOffset? scheduledAt = null);
    Task<bool> CancelAsync(Guid jobId);
}

// Job scheduler implementation
public class JobScheduler : IJobScheduler
{
    private readonly IJobStore _jobStore;
    private readonly IJobQueue _jobQueue;
    private readonly IJobRegistry _registry;
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(
        IJobStore jobStore,
        IJobQueue jobQueue,
        IJobRegistry registry,
        ILogger<JobScheduler> logger)
    {
        _jobStore = jobStore;
        _jobQueue = jobQueue;
        _registry = registry;
        _logger = logger;
    }

    public Task<Guid> ScheduleAsync<TJob>(Dictionary<string, object> parameters = null, DateTimeOffset? scheduledAt = null) where TJob : IJob
    {
        return ScheduleAsync(typeof(TJob), parameters, scheduledAt);
    }

    public Task<Guid> ScheduleAsync(Type jobType, Dictionary<string, object> parameters = null, DateTimeOffset? scheduledAt = null)
    {
        return ScheduleAsync(jobType.FullName, parameters, scheduledAt);
    }

    public async Task<Guid> ScheduleAsync(string jobType, Dictionary<string, object> parameters = null, DateTimeOffset? scheduledAt = null)
    {
        var job = new JobInfo
        {
            Id = Guid.NewGuid(),
            Type = jobType,
            Parameters = parameters ?? new Dictionary<string, object>(),
            Status = JobStatus.Pending,
            ScheduledAt = scheduledAt ?? DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _jobStore.CreateJobAsync(job);

        if (job.ScheduledAt <= DateTimeOffset.UtcNow)
        {
            await _jobQueue.EnqueueAsync(job);
        }

        _logger.LogInformation("Job {JobId} of type {JobType} scheduled", job.Id, jobType);

        return job.Id;
    }

    public async Task<bool> CancelAsync(Guid jobId)
    {
        var job = await _jobStore.GetJobAsync(jobId);
        if (job != null && job.Status == JobStatus.Pending)
        {
            job.Status = JobStatus.Cancelled;
            await _jobStore.UpdateJobAsync(job);
            
            _logger.LogInformation("Job {JobId} cancelled", jobId);
            return true;
        }

        return false;
    }
}

// Job executor interface
public interface IJobExecutor
{
    Task<JobResult> ExecuteAsync(JobInfo job, CancellationToken cancellationToken = default);
}

// Job executor implementation
public class JobExecutor : IJobExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJobRegistry _registry;
    private readonly IJobStore _jobStore;
    private readonly ILogger<JobExecutor> _logger;
    private readonly BackgroundJobOptions _options;

    public JobExecutor(
        IServiceProvider serviceProvider,
        IJobRegistry registry,
        IJobStore jobStore,
        ILogger<JobExecutor> logger,
        IOptions<BackgroundJobOptions> options)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
        _jobStore = jobStore;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<JobResult> ExecuteAsync(JobInfo job, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // Update job status
            job.Status = JobStatus.Running;
            job.StartedAt = DateTimeOffset.UtcNow;
            await _jobStore.UpdateJobAsync(job, cancellationToken);

            // Get job type
            var jobType = _registry.GetJobType(job.Type);
            if (jobType == null)
            {
                throw new InvalidOperationException($"Job type '{job.Type}' not found");
            }

            // Create job instance
            var jobInstance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, jobType) as IJob;
            if (jobInstance == null)
            {
                throw new InvalidOperationException($"Failed to create job instance for type '{job.Type}'");
            }

            // Create job context
            var context = new JobContext
            {
                JobId = job.Id,
                JobType = job.Type,
                Parameters = job.Parameters,
                ServiceProvider = scope.ServiceProvider,
                CancellationToken = cancellationToken,
                Logger = new JobLogger(_logger, job.Id)
            };

            // Execute job with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.JobTimeout);

            await jobInstance.ExecuteAsync(context, cts.Token);

            // Update job status
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _jobStore.UpdateJobAsync(job, cancellationToken);

            _logger.LogInformation("Job {JobId} completed successfully", job.Id);

            return new JobResult { Success = true };
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.Error = "Job was cancelled";
            await _jobStore.UpdateJobAsync(job, cancellationToken);
            
            _logger.LogWarning("Job {JobId} was cancelled", job.Id);
            
            return new JobResult { Success = false, Error = job.Error };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);

            job.RetryCount++;
            job.Error = ex.Message;

            if (job.RetryCount < job.MaxRetries)
            {
                // Schedule retry
                job.Status = JobStatus.Pending;
                job.NextRetryAt = CalculateNextRetryTime(job.RetryCount);
                job.ScheduledAt = job.NextRetryAt.Value;
            }
            else
            {
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTimeOffset.UtcNow;
            }

            await _jobStore.UpdateJobAsync(job, cancellationToken);

            return new JobResult { Success = false, Error = ex.Message };
        }
    }

    private DateTimeOffset CalculateNextRetryTime(int retryCount)
    {
        // Exponential backoff with jitter
        var baseDelay = Math.Pow(2, retryCount) * 1000; // milliseconds
        var jitter = Random.Shared.Next(0, 1000);
        var delay = TimeSpan.FromMilliseconds(baseDelay + jitter);
        
        return DateTimeOffset.UtcNow.Add(delay);
    }
}

// Job registry
public interface IJobRegistry
{
    void Register<TJob>() where TJob : IJob;
    void Register(Type jobType);
    Type GetJobType(string typeName);
    IEnumerable<Type> GetAllJobTypes();
}

public class JobRegistry : IJobRegistry
{
    private readonly ConcurrentDictionary<string, Type> _jobTypes = new();

    public void Register<TJob>() where TJob : IJob
    {
        Register(typeof(TJob));
    }

    public void Register(Type jobType)
    {
        if (!typeof(IJob).IsAssignableFrom(jobType))
        {
            throw new ArgumentException($"Type {jobType.FullName} does not implement IJob");
        }

        _jobTypes[jobType.FullName] = jobType;
    }

    public Type GetJobType(string typeName)
    {
        return _jobTypes.TryGetValue(typeName, out var type) ? type : null;
    }

    public IEnumerable<Type> GetAllJobTypes()
    {
        return _jobTypes.Values;
    }
}

// Recurring job manager
public interface IRecurringJobManager
{
    Task AddOrUpdateAsync(string jobId, Expression<Func<Task>> methodCall, string cronExpression);
    Task AddOrUpdateAsync<TJob>(string jobId, string cronExpression, Dictionary<string, object> parameters = null) where TJob : IJob;
    Task RemoveAsync(string jobId);
    Task<IEnumerable<RecurringJobInfo>> GetAllAsync();
}

public class RecurringJobManager : IRecurringJobManager
{
    private readonly ConcurrentDictionary<string, RecurringJobInfo> _recurringJobs = new();
    private readonly IJobScheduler _scheduler;
    private readonly ILogger<RecurringJobManager> _logger;

    public RecurringJobManager(IJobScheduler scheduler, ILogger<RecurringJobManager> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    public Task AddOrUpdateAsync(string jobId, Expression<Func<Task>> methodCall, string cronExpression)
    {
        throw new NotImplementedException("Expression-based recurring jobs not yet implemented");
    }

    public Task AddOrUpdateAsync<TJob>(string jobId, string cronExpression, Dictionary<string, object> parameters = null) where TJob : IJob
    {
        var recurringJob = new RecurringJobInfo
        {
            Id = jobId,
            JobType = typeof(TJob).FullName,
            CronExpression = cronExpression,
            Parameters = parameters,
            NextExecution = CalculateNextExecution(cronExpression),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _recurringJobs.AddOrUpdate(jobId, recurringJob, (key, existing) =>
        {
            existing.CronExpression = cronExpression;
            existing.Parameters = parameters;
            existing.NextExecution = CalculateNextExecution(cronExpression);
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            return existing;
        });

        _logger.LogInformation("Recurring job {JobId} added/updated with cron expression {CronExpression}", jobId, cronExpression);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string jobId)
    {
        if (_recurringJobs.TryRemove(jobId, out _))
        {
            _logger.LogInformation("Recurring job {JobId} removed", jobId);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<RecurringJobInfo>> GetAllAsync()
    {
        return Task.FromResult(_recurringJobs.Values.AsEnumerable());
    }

    private DateTimeOffset CalculateNextExecution(string cronExpression)
    {
        // Simplified - in production use a proper cron parser
        return DateTimeOffset.UtcNow.AddMinutes(1);
    }
}

// Background services
public class JobProcessingService : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly IJobExecutor _executor;
    private readonly ILogger<JobProcessingService> _logger;
    private readonly BackgroundJobOptions _options;

    public JobProcessingService(
        IJobQueue queue,
        IJobExecutor executor,
        ILogger<JobProcessingService> logger,
        IOptions<BackgroundJobOptions> options)
    {
        _queue = queue;
        _executor = executor;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job processing service started with {WorkerCount} workers", _options.WorkerCount);

        var tasks = new List<Task>();
        
        for (int i = 0; i < _options.WorkerCount; i++)
        {
            tasks.Add(ProcessJobsAsync(i, stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessJobsAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker {WorkerId} started", workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                if (job != null)
                {
                    _logger.LogDebug("Worker {WorkerId} processing job {JobId}", workerId, job.Id);
                    await _executor.ExecuteAsync(job, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} encountered an error", workerId);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Worker {WorkerId} stopped", workerId);
    }
}

public class RecurringJobScheduler : BackgroundService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IJobScheduler _scheduler;
    private readonly ILogger<RecurringJobScheduler> _logger;

    public RecurringJobScheduler(
        IRecurringJobManager recurringJobManager,
        IJobScheduler scheduler,
        ILogger<RecurringJobScheduler> logger)
    {
        _recurringJobManager = recurringJobManager;
        _scheduler = scheduler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleDueJobsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling recurring jobs");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ScheduleDueJobsAsync(CancellationToken cancellationToken)
    {
        var recurringJobs = await _recurringJobManager.GetAllAsync();
        var now = DateTimeOffset.UtcNow;

        foreach (var recurringJob in recurringJobs.Where(j => j.NextExecution <= now))
        {
            try
            {
                await _scheduler.ScheduleAsync(recurringJob.JobType, recurringJob.Parameters);
                
                // Update next execution time
                recurringJob.LastExecution = now;
                recurringJob.NextExecution = CalculateNextExecution(recurringJob.CronExpression);
                
                _logger.LogDebug("Scheduled recurring job {JobId}", recurringJob.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule recurring job {JobId}", recurringJob.Id);
            }
        }
    }

    private DateTimeOffset CalculateNextExecution(string cronExpression)
    {
        // Simplified - in production use a proper cron parser
        return DateTimeOffset.UtcNow.AddMinutes(1);
    }
}

// Job monitoring
public interface IJobMonitor
{
    Task<JobStatistics> GetStatisticsAsync();
    Task<IEnumerable<JobInfo>> GetRecentJobsAsync(int count = 10);
    Task<IEnumerable<JobInfo>> GetFailedJobsAsync(int count = 10);
}

public class JobMonitor : IJobMonitor
{
    private readonly IJobStore _jobStore;

    public JobMonitor(IJobStore jobStore)
    {
        _jobStore = jobStore;
    }

    public Task<JobStatistics> GetStatisticsAsync()
    {
        return _jobStore.GetStatisticsAsync();
    }

    public Task<IEnumerable<JobInfo>> GetRecentJobsAsync(int count = 10)
    {
        return _jobStore.GetJobsByStatusAsync(JobStatus.Completed, count);
    }

    public Task<IEnumerable<JobInfo>> GetFailedJobsAsync(int count = 10)
    {
        return _jobStore.GetJobsByStatusAsync(JobStatus.Failed, count);
    }
}

// Models
public class JobInfo
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Queue { get; set; } = "default";
    public Dictionary<string, object> Parameters { get; set; }
    public JobStatus Status { get; set; }
    public int Priority { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public int RetryCount { get; set; }
    public object Result { get; set; }
    public string Error { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
}

public class RecurringJobInfo
{
    public string Id { get; set; }
    public string JobType { get; set; }
    public string CronExpression { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public DateTimeOffset NextExecution { get; set; }
    public DateTimeOffset? LastExecution { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class JobResult
{
    public bool Success { get; set; }
    public object Data { get; set; }
    public string Error { get; set; }
}

public class JobStatistics
{
    public int TotalJobs { get; set; }
    public int PendingJobs { get; set; }
    public int RunningJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
}

public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

// Job logger
public interface IJobLogger
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
}

public class JobLogger : IJobLogger
{
    private readonly ILogger _logger;
    private readonly Guid _jobId;

    public JobLogger(ILogger logger, Guid jobId)
    {
        _logger = logger;
        _jobId = jobId;
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation($"[Job {_jobId}] {message}", args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning($"[Job {_jobId}] {message}", args);
    }

    public void LogError(string message, params object[] args)
    {
        _logger.LogError($"[Job {_jobId}] {message}", args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, $"[Job {_jobId}] {message}", args);
    }
}

// Job serializer
public interface IJobSerializer
{
    string Serialize(object obj);
    T Deserialize<T>(string data);
}

public class JsonJobSerializer : IJobSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonJobSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public T Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(data, _options);
    }
}

// Database context
public class JobDbContext : DbContext
{
    public DbSet<JobEntity> Jobs { get; set; }

    public JobDbContext(DbContextOptions<JobDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobEntity>(entity =>
        {
            entity.ToTable("Jobs");
            entity.HasKey(j => j.Id);
            entity.HasIndex(j => j.Status);
            entity.HasIndex(j => new { j.Status, j.ScheduledAt });
            entity.HasIndex(j => j.CreatedAt);
            entity.Property(j => j.Parameters).HasColumnType("jsonb");
            entity.Property(j => j.Result).HasColumnType("jsonb");
        });
    }
}

public class JobEntity
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Queue { get; set; }
    public string Parameters { get; set; }
    public JobStatus Status { get; set; }
    public int Priority { get; set; }
    public int MaxRetries { get; set; }
    public int RetryCount { get; set; }
    public string Result { get; set; }
    public string Error { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
}

// Configuration
public class BackgroundJobOptions
{
    public int WorkerCount { get; set; } = 4;
    public int MaxQueueSize { get; set; } = 1000;
    public TimeSpan JobTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnableJobMonitoring { get; set; } = true;
    public string ConnectionString { get; set; }
}