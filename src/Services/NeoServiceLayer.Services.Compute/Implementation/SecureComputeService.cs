using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Compute.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Compute.Implementation
{
    /// <summary>
    /// Secure compute service with Intel SGX enclave support
    /// </summary>
    public class SecureComputeService : EnclaveServiceBase, IComputeService
    {
        private readonly ILogger<SecureComputeService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEnclaveManager _enclaveManager;
        private readonly IJobRepository _jobRepository;
        private readonly IMetricsCollector _metrics;
        private readonly ConcurrentDictionary<Guid, ComputeJob> _activeJobs;
        private readonly ConcurrentQueue<ComputeJob> _jobQueue;
        private readonly SemaphoreSlim _jobSemaphore;
        private readonly int _maxConcurrentJobs;
        private readonly int _maxJobQueueSize;
        private CancellationTokenSource _cancellationTokenSource;

        public SecureComputeService(
            ILogger<SecureComputeService> logger,
            IConfiguration configuration,
            IEnclaveManager enclaveManager,
            IJobRepository jobRepository,
            IMetricsCollector metrics)
            : base("SecureComputeService", "Secure computation service with SGX support", "1.0.0", logger)
        {
            _logger = logger;
            _configuration = configuration;
            _enclaveManager = enclaveManager;
            _jobRepository = jobRepository;
            _metrics = metrics;

            _activeJobs = new ConcurrentDictionary<Guid, ComputeJob>();
            _jobQueue = new ConcurrentQueue<ComputeJob>();
            
            _maxConcurrentJobs = configuration.GetValue<int>("Compute:MaxConcurrentJobs", 10);
            _maxJobQueueSize = configuration.GetValue<int>("Compute:MaxJobQueueSize", 100);
            _jobSemaphore = new SemaphoreSlim(_maxConcurrentJobs, _maxConcurrentJobs);
        }

        public async Task<ComputeJob> CreateJobAsync(CreateJobRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new ArgumentException("Job name is required", nameof(request.Name));
                }

                // Check queue capacity
                if (_jobQueue.Count >= _maxJobQueueSize)
                {
                    throw new InvalidOperationException($"Job queue is full (max: {_maxJobQueueSize})");
                }

                // Create job
                var job = new ComputeJob
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Type = request.Type,
                    Status = JobStatus.Queued,
                    Priority = request.Priority ?? JobPriority.Normal,
                    Parameters = request.Parameters ?? new Dictionary<string, object>(),
                    RequiresEnclave = request.RequiresEnclave,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.UserId,
                    Progress = 0
                };

                // Estimate resources
                job.EstimatedResources = EstimateResources(request);

                // Store job
                await _jobRepository.CreateAsync(job);
                _activeJobs.TryAdd(job.Id, job);

                // Queue for processing
                _jobQueue.Enqueue(job);

                // Trigger processing
                _ = Task.Run(() => ProcessJobQueueAsync(_cancellationTokenSource.Token));

                _logger.LogInformation("Job {JobId} created: {JobName}", job.Id, job.Name);
                _metrics.IncrementCounter("compute.jobs.created", 
                    new[] { ("type", job.Type.ToString()), ("priority", job.Priority.ToString()) });

                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating compute job");
                _metrics.IncrementCounter("compute.jobs.create_error");
                throw;
            }
        }

        public async Task<ComputeJob> GetJobAsync(Guid jobId)
        {
            if (_activeJobs.TryGetValue(jobId, out var activeJob))
            {
                return activeJob;
            }

            return await _jobRepository.GetByIdAsync(jobId);
        }

        public async Task<IEnumerable<ComputeJob>> ListJobsAsync(JobFilter filter = null)
        {
            filter ??= new JobFilter();
            
            var jobs = await _jobRepository.ListAsync(filter);
            
            // Merge with active jobs for most recent status
            foreach (var job in jobs)
            {
                if (_activeJobs.TryGetValue(job.Id, out var activeJob))
                {
                    // Update with live status
                    job.Status = activeJob.Status;
                    job.Progress = activeJob.Progress;
                }
            }

            return jobs;
        }

        public async Task<bool> CancelJobAsync(Guid jobId)
        {
            try
            {
                if (!_activeJobs.TryGetValue(jobId, out var job))
                {
                    job = await _jobRepository.GetByIdAsync(jobId);
                    if (job == null)
                    {
                        return false;
                    }
                }

                if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
                {
                    return false; // Cannot cancel completed/failed jobs
                }

                job.Status = JobStatus.Cancelled;
                job.CompletedAt = DateTime.UtcNow;
                job.Error = "Job cancelled by user";

                await _jobRepository.UpdateAsync(job);
                _activeJobs.TryRemove(jobId, out _);

                _logger.LogInformation("Job {JobId} cancelled", jobId);
                _metrics.IncrementCounter("compute.jobs.cancelled");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
                return false;
            }
        }

        public async Task<ComputeResult> ExecuteInEnclaveAsync(EnclaveExecutionRequest request)
        {
            try
            {
                // Ensure enclave is initialized
                if (!IsEnclaveInitialized)
                {
                    await InitializeEnclaveAsync();
                }

                var startTime = DateTime.UtcNow;
                _metrics.IncrementCounter("compute.enclave.executions.started");

                // Prepare input for enclave
                var enclaveInput = new EnclaveComputeInput
                {
                    Operation = request.Operation,
                    Data = request.InputData,
                    Parameters = request.Parameters
                };

                // Execute in enclave
                var result = await SecureComputeAsync(
                    request.Operation,
                    enclaveInput.Serialize(),
                    request.Parameters);

                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Generate attestation if requested
                AttestationReport attestation = null;
                if (request.RequireAttestation)
                {
                    attestation = await GenerateAttestationReportAsync();
                }

                _metrics.RecordLatency("compute.enclave.execution_time", executionTime,
                    new[] { ("operation", request.Operation) });
                _metrics.IncrementCounter("compute.enclave.executions.completed");

                return new ComputeResult
                {
                    Id = Guid.NewGuid(),
                    JobId = request.JobId,
                    Success = true,
                    OutputData = result,
                    ExecutionTimeMs = executionTime,
                    Attestation = attestation,
                    ComputedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enclave execution error for operation {Operation}", request.Operation);
                _metrics.IncrementCounter("compute.enclave.executions.failed");
                
                return new ComputeResult
                {
                    Id = Guid.NewGuid(),
                    JobId = request.JobId,
                    Success = false,
                    Error = ex.Message,
                    ComputedAt = DateTime.UtcNow
                };
            }
        }

        private async Task ProcessJobQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _jobQueue.TryDequeue(out var job))
            {
                await _jobSemaphore.WaitAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessJobAsync(job, cancellationToken);
                    }
                    finally
                    {
                        _jobSemaphore.Release();
                    }
                }, cancellationToken);
            }
        }

        private async Task ProcessJobAsync(ComputeJob job, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing job {JobId}: {JobName}", job.Id, job.Name);
                var startTime = DateTime.UtcNow;

                // Update status
                job.Status = JobStatus.Running;
                job.StartedAt = startTime;
                await _jobRepository.UpdateAsync(job);

                // Execute based on job type
                ComputeResult result;
                switch (job.Type)
                {
                    case JobType.BatchProcessing:
                        result = await ProcessBatchJobAsync(job, cancellationToken);
                        break;
                    
                    case JobType.StreamProcessing:
                        result = await ProcessStreamJobAsync(job, cancellationToken);
                        break;
                    
                    case JobType.MachineLearning:
                        result = await ProcessMachineLearningJobAsync(job, cancellationToken);
                        break;
                    
                    case JobType.DataAnalysis:
                        result = await ProcessDataAnalysisJobAsync(job, cancellationToken);
                        break;
                    
                    case JobType.Cryptographic:
                        result = await ProcessCryptographicJobAsync(job, cancellationToken);
                        break;
                    
                    default:
                        throw new NotSupportedException($"Job type {job.Type} is not supported");
                }

                // Update job with result
                job.Status = result.Success ? JobStatus.Completed : JobStatus.Failed;
                job.Result = result;
                job.CompletedAt = DateTime.UtcNow;
                job.Progress = 100;

                if (!result.Success)
                {
                    job.Error = result.Error;
                }

                await _jobRepository.UpdateAsync(job);
                _activeJobs.TryRemove(job.Id, out _);

                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _metrics.RecordLatency("compute.jobs.execution_time", executionTime,
                    new[] { ("type", job.Type.ToString()), ("status", job.Status.ToString()) });
                _metrics.IncrementCounter($"compute.jobs.{job.Status.ToString().ToLower()}");

                _logger.LogInformation("Job {JobId} completed with status {Status}", job.Id, job.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", job.Id);
                
                job.Status = JobStatus.Failed;
                job.Error = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                
                await _jobRepository.UpdateAsync(job);
                _activeJobs.TryRemove(job.Id, out _);
                
                _metrics.IncrementCounter("compute.jobs.failed");
            }
        }

        private async Task<ComputeResult> ProcessBatchJobAsync(ComputeJob job, CancellationToken cancellationToken)
        {
            // Simulate batch processing
            var batchSize = job.Parameters.GetValueOrDefault("batch_size", 100);
            var items = job.Parameters.GetValueOrDefault("items", new List<object>());

            for (int i = 0; i < 10; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                await Task.Delay(500, cancellationToken); // Simulate processing
                job.Progress = (i + 1) * 10;
                await _jobRepository.UpdateProgressAsync(job.Id, job.Progress);
            }

            return new ComputeResult
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                Success = true,
                OutputData = new byte[] { 1, 2, 3, 4, 5 }, // Placeholder result
                ComputedAt = DateTime.UtcNow
            };
        }

        private async Task<ComputeResult> ProcessStreamJobAsync(ComputeJob job, CancellationToken cancellationToken)
        {
            // Implement stream processing logic
            await Task.Delay(1000, cancellationToken);
            return new ComputeResult { Success = true, JobId = job.Id };
        }

        private async Task<ComputeResult> ProcessMachineLearningJobAsync(ComputeJob job, CancellationToken cancellationToken)
        {
            // Implement ML job processing
            if (job.RequiresEnclave)
            {
                var request = new EnclaveExecutionRequest
                {
                    JobId = job.Id,
                    Operation = "ml_inference",
                    InputData = job.Parameters.GetValueOrDefault("model_input", new byte[0]) as byte[],
                    Parameters = job.Parameters,
                    RequireAttestation = true
                };

                return await ExecuteInEnclaveAsync(request);
            }

            await Task.Delay(2000, cancellationToken);
            return new ComputeResult { Success = true, JobId = job.Id };
        }

        private async Task<ComputeResult> ProcessDataAnalysisJobAsync(ComputeJob job, CancellationToken cancellationToken)
        {
            // Implement data analysis logic
            await Task.Delay(1500, cancellationToken);
            return new ComputeResult { Success = true, JobId = job.Id };
        }

        private async Task<ComputeResult> ProcessCryptographicJobAsync(ComputeJob job, CancellationToken cancellationToken)
        {
            // Cryptographic operations should always run in enclave
            var operation = job.Parameters.GetValueOrDefault("crypto_operation", "encrypt").ToString();
            var data = job.Parameters.GetValueOrDefault("data", new byte[0]) as byte[];

            var request = new EnclaveExecutionRequest
            {
                JobId = job.Id,
                Operation = operation,
                InputData = data,
                Parameters = job.Parameters,
                RequireAttestation = true
            };

            return await ExecuteInEnclaveAsync(request);
        }

        private ResourceRequirements EstimateResources(CreateJobRequest request)
        {
            // Estimate based on job type and parameters
            var requirements = new ResourceRequirements();

            switch (request.Type)
            {
                case JobType.MachineLearning:
                    requirements.CpuCores = 4;
                    requirements.MemoryMB = 8192;
                    requirements.GpuRequired = true;
                    break;
                
                case JobType.BatchProcessing:
                    var batchSize = request.Parameters?.GetValueOrDefault("batch_size", 100) ?? 100;
                    requirements.CpuCores = Math.Min(8, (int)batchSize / 100);
                    requirements.MemoryMB = Math.Min(16384, (int)batchSize * 10);
                    break;
                
                default:
                    requirements.CpuCores = 2;
                    requirements.MemoryMB = 2048;
                    break;
            }

            if (request.RequiresEnclave)
            {
                requirements.EnclaveRequired = true;
                requirements.EnclaveSizeMB = 128;
            }

            return requirements;
        }

        protected override async Task<bool> OnInitializeEnclaveAsync()
        {
            try
            {
                _logger.LogInformation("Initializing SGX enclave for compute service");
                
                // Initialize enclave through enclave manager
                var enclaveId = await _enclaveManager.CreateEnclaveAsync("compute_enclave");
                
                // Perform attestation
                var attestation = await _enclaveManager.AttestEnclaveAsync(enclaveId);
                if (!attestation.IsValid)
                {
                    throw new SecurityException("Enclave attestation failed");
                }

                _logger.LogInformation("SGX enclave initialized and attested successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SGX enclave");
                return false;
            }
        }

        protected override async Task<string> OnGetAttestationAsync()
        {
            var attestation = await _enclaveManager.GetAttestationReportAsync();
            return Convert.ToBase64String(attestation.Quote);
        }

        protected override async Task<bool> OnValidateEnclaveAsync()
        {
            return await _enclaveManager.ValidateEnclaveAsync();
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                var dbHealthy = await _jobRepository.CheckHealthAsync();
                var enclaveHealthy = IsEnclaveInitialized && await OnValidateEnclaveAsync();
                var queueHealthy = _jobQueue.Count < _maxJobQueueSize * 0.9;

                if (dbHealthy && enclaveHealthy && queueHealthy)
                {
                    return ServiceHealth.Healthy;
                }
                else if (dbHealthy && queueHealthy)
                {
                    return ServiceHealth.Degraded; // Enclave issue but can still process non-enclave jobs
                }
                else
                {
                    return ServiceHealth.Unhealthy;
                }
            }
            catch
            {
                return ServiceHealth.Unhealthy;
            }
        }

        protected override async Task<bool> OnInitializeAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("Secure Compute Service initialized");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStartAsync()
        {
            // Start job processing loop
            _ = Task.Run(() => ProcessJobQueueAsync(_cancellationTokenSource.Token));
            _logger.LogInformation("Secure Compute Service started");
            return await Task.FromResult(true);
        }

        protected override async Task<bool> OnStopAsync()
        {
            _cancellationTokenSource?.Cancel();
            
            // Wait for active jobs to complete gracefully
            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            while (_activeJobs.Any() && stopwatch.Elapsed < timeout)
            {
                await Task.Delay(100);
            }

            _logger.LogInformation("Secure Compute Service stopped");
            return true;
        }
    }
}