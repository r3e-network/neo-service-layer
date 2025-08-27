using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Neo.Compute.Service.Models;
using Neo.Compute.Service.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Neo.Compute.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ComputeApi")]
public class ComputeController : ControllerBase
{
    private readonly IComputeJobService _computeJobService;
    private readonly ISgxEnclaveService _sgxEnclaveService;
    private readonly IAttestationService _attestationService;
    private readonly ISecureComputationService _secureComputationService;
    private readonly IResourceAllocationService _resourceAllocationService;
    private readonly ILogger<ComputeController> _logger;

    public ComputeController(
        IComputeJobService computeJobService,
        ISgxEnclaveService sgxEnclaveService,
        IAttestationService attestationService,
        ISecureComputationService secureComputationService,
        IResourceAllocationService resourceAllocationService,
        ILogger<ComputeController> logger)
    {
        _computeJobService = computeJobService;
        _sgxEnclaveService = sgxEnclaveService;
        _attestationService = attestationService;
        _secureComputationService = secureComputationService;
        _resourceAllocationService = resourceAllocationService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new compute job
    /// </summary>
    /// <param name="request">Job creation parameters</param>
    /// <returns>Created job information</returns>
    [HttpPost("jobs")]
    [Authorize(Policy = "ComputeWrite")]
    [ProducesResponseType(typeof(ComputeJobResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> CreateJob([FromBody] CreateComputeJobRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var job = await _computeJobService.CreateJobAsync(request, userId);

            _logger.LogInformation("Compute job created: {JobId} by user: {UserId}", job.JobId, userId);

            return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, job);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Job Request",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating compute job for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while creating the compute job",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get compute job by ID
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Job details and status</returns>
    [HttpGet("jobs/{id:guid}")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(ComputeJobResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetJob([FromRoute] Guid id)
    {
        try
        {
            var job = await _computeJobService.GetJobAsync(id);
            
            if (job == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Job Not Found",
                    Detail = $"Compute job not found: {id}",
                    Status = 404
                });
            }

            // Check if user owns the job or has admin rights
            var userId = GetCurrentUserId();
            var userRoles = GetUserRoles();
            
            if (job.Status != "Completed" && !userRoles.Contains("compute-admin") && 
                !_computeJobService.IsJobOwnedByUser(id, userId))
            {
                return Forbid();
            }

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compute job: {JobId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the job",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get user's compute jobs
    /// </summary>
    /// <param name="status">Filter by job status</param>
    /// <param name="limit">Maximum number of jobs to return</param>
    /// <param name="offset">Number of jobs to skip</param>
    /// <returns>List of user's compute jobs</returns>
    [HttpGet("jobs")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(List<ComputeJobResponse>), 200)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            var userId = GetCurrentUserId();
            var jobs = await _computeJobService.GetUserJobsAsync(userId, status, limit, offset);
            
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compute jobs for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving jobs",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Cancel a compute job
    /// </summary>
    /// <param name="id">Job ID to cancel</param>
    /// <returns>Cancellation confirmation</returns>
    [HttpPost("jobs/{id:guid}/cancel")]
    [Authorize(Policy = "ComputeWrite")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CancelJob([FromRoute] Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRoles = GetUserRoles();
            
            // Check if user owns the job or has admin rights
            if (!userRoles.Contains("compute-admin") && 
                !_computeJobService.IsJobOwnedByUser(id, userId))
            {
                return Forbid();
            }

            var result = await _computeJobService.CancelJobAsync(id);
            
            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Job Not Found",
                        Detail = $"Compute job not found: {id}",
                        Status = 404
                    });
                }

                return Conflict(new ProblemDetails
                {
                    Title = "Cannot Cancel Job",
                    Detail = result.ErrorMessage ?? "Job cannot be cancelled in its current state",
                    Status = 409
                });
            }

            _logger.LogInformation("Compute job cancelled: {JobId} by user: {UserId}", id, userId);

            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling compute job: {JobId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while cancelling the job",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get compute job logs
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <param name="limit">Maximum number of log entries</param>
    /// <returns>Job execution logs</returns>
    [HttpGet("jobs/{id:guid}/logs")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(List<object>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetJobLogs(
        [FromRoute] Guid id,
        [FromQuery] int limit = 100)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRoles = GetUserRoles();
            
            // Check if user owns the job or has admin rights
            if (!userRoles.Contains("compute-admin") && 
                !_computeJobService.IsJobOwnedByUser(id, userId))
            {
                return Forbid();
            }

            var logs = await _computeJobService.GetJobLogsAsync(id, limit);
            
            if (logs == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Job Not Found",
                    Detail = $"Compute job not found: {id}",
                    Status = 404
                });
            }

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job logs: {JobId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving job logs",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Execute secure computation
    /// </summary>
    /// <param name="request">Secure computation parameters</param>
    /// <returns>Computation result with attestation proof</returns>
    [HttpPost("secure-compute")]
    [Authorize(Policy = "ComputeWrite")]
    [ProducesResponseType(typeof(SecureComputationResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> ExecuteSecureComputation([FromBody] SecureComputationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var result = await _secureComputationService.ExecuteAsync(request, userId);

            _logger.LogInformation("Secure computation executed: {SessionId} by user: {UserId}", 
                result.SessionId, userId);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no available"))
        {
            return StatusCode(503, new ProblemDetails
            {
                Title = "Service Unavailable",
                Detail = "No SGX enclaves available for secure computation",
                Status = 503
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing secure computation for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during secure computation",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get available SGX enclaves
    /// </summary>
    /// <param name="includeStats">Include enclave statistics</param>
    /// <returns>List of available enclaves</returns>
    [HttpGet("enclaves")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(List<EnclaveResponse>), 200)]
    public async Task<IActionResult> GetEnclaves([FromQuery] bool includeStats = false)
    {
        try
        {
            var enclaves = await _sgxEnclaveService.GetAvailableEnclavesAsync(includeStats);
            return Ok(enclaves);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SGX enclaves");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving enclaves",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get SGX enclave details
    /// </summary>
    /// <param name="id">Enclave ID</param>
    /// <returns>Detailed enclave information</returns>
    [HttpGet("enclaves/{id:guid}")]
    [Authorize(Policy = "SgxOperator")]
    [ProducesResponseType(typeof(EnclaveResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetEnclave([FromRoute] Guid id)
    {
        try
        {
            var enclave = await _sgxEnclaveService.GetEnclaveAsync(id);
            
            if (enclave == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Enclave Not Found",
                    Detail = $"SGX enclave not found: {id}",
                    Status = 404
                });
            }

            return Ok(enclave);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SGX enclave: {EnclaveId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the enclave",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Create new SGX enclave
    /// </summary>
    /// <param name="request">Enclave creation parameters</param>
    /// <returns>Created enclave information</returns>
    [HttpPost("enclaves")]
    [Authorize(Policy = "SgxOperator")]
    [ProducesResponseType(typeof(EnclaveResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> CreateEnclave([FromBody] CreateEnclaveRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var enclave = await _sgxEnclaveService.CreateEnclaveAsync(request);

            _logger.LogInformation("SGX enclave created: {EnclaveId} ({Name})", enclave.Id, request.Name);

            return CreatedAtAction(nameof(GetEnclave), new { id = enclave.Id }, enclave);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Enclave Request",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SGX enclave: {Name}", request.Name);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while creating the enclave",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Perform SGX attestation
    /// </summary>
    /// <param name="request">Attestation parameters</param>
    /// <returns>Attestation verification result</returns>
    [HttpPost("attest")]
    [Authorize(Policy = "SgxOperator")]
    [ProducesResponseType(typeof(AttestationResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> PerformAttestation([FromBody] AttestationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _attestationService.VerifyAttestationAsync(request);

            _logger.LogInformation("SGX attestation performed: {AttestationId} for enclave: {EnclaveId}", 
                result.AttestationId, request.EnclaveId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Attestation Request",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing SGX attestation for enclave: {EnclaveId}", request.EnclaveId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred during attestation",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get compute service statistics
    /// </summary>
    /// <returns>Service performance and usage statistics</returns>
    [HttpGet("stats")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(ComputeStatistics), 200)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _computeJobService.GetStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compute statistics");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving statistics",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get job queue status
    /// </summary>
    /// <returns>Current job queue status and metrics</returns>
    [HttpGet("queue")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(JobQueueStatus), 200)]
    public async Task<IActionResult> GetQueueStatus()
    {
        try
        {
            var queueStatus = await _computeJobService.GetQueueStatusAsync();
            return Ok(queueStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job queue status");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving queue status",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get resource allocation details for a job
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Resource allocation and usage information</returns>
    [HttpGet("jobs/{id:guid}/resources")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(ComputeResourceUsage), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetJobResources([FromRoute] Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRoles = GetUserRoles();
            
            // Check if user owns the job or has admin rights
            if (!userRoles.Contains("compute-admin") && 
                !_computeJobService.IsJobOwnedByUser(id, userId))
            {
                return Forbid();
            }

            var resources = await _resourceAllocationService.GetJobResourcesAsync(id);
            
            if (resources == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Job Not Found",
                    Detail = $"Compute job not found: {id}",
                    Status = 404
                });
            }

            return Ok(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job resources: {JobId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving job resources",
                Status = 500
            });
        }
    }

    private string GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userIdClaim;
    }

    private string[] GetUserRoles()
    {
        return HttpContext.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();
    }
}