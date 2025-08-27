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
    /// Create a new compute job
    /// </summary>
    /// <param name="request">Compute job creation parameters</param>
    /// <returns>Created compute job information</returns>
    [HttpPost("jobs")]
    [Authorize(Policy = "ComputeWrite")]
    [ProducesResponseType(typeof(ComputeJobResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> CreateComputeJob([FromBody] CreateComputeJobRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var job = await _computeJobService.CreateJobAsync(request, userId);

            _logger.LogInformation("Compute job created: {JobId} for user: {UserId}", job.JobId, userId);

            return CreatedAtAction(nameof(GetComputeJob), new { id = job.JobId }, job);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("resource"))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Resource Unavailable",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating compute job");
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
    /// <returns>Compute job details</returns>
    [HttpGet("jobs/{id:guid}")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(ComputeJobResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetComputeJob([FromRoute] Guid id)
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

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compute job: {JobId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the compute job",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get all compute jobs for the current user
    /// </summary>
    /// <param name="status">Filter by job status</param>
    /// <param name="skip">Number of jobs to skip</param>
    /// <param name="take">Number of jobs to take</param>
    /// <returns>List of compute jobs</returns>
    [HttpGet("jobs")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(List<ComputeJobResponse>), 200)]
    public async Task<IActionResult> GetComputeJobs(
        [FromQuery] ComputeJobStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var jobs = await _computeJobService.GetJobsForUserAsync(userId, status, skip, take);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compute jobs");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving compute jobs",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Cancel a compute job
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Cancellation confirmation</returns>
    [HttpPost("jobs/{id:guid}/cancel")]
    [Authorize(Policy = "ComputeWrite")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> CancelComputeJob([FromRoute] Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _computeJobService.CancelJobAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Job Not Found",
                    Detail = $"Compute job not found or cannot be cancelled: {id}",
                    Status = 404
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
                Detail = "An error occurred while cancelling the compute job",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get queue status and statistics
    /// </summary>
    /// <returns>Queue status information</returns>
    [HttpGet("queue/status")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(JobQueueStatus), 200)]
    public async Task<IActionResult> GetQueueStatus()
    {
        try
        {
            var status = await _computeJobService.GetQueueStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue status");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving queue status",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Create a new SGX enclave
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

            _logger.LogInformation("SGX enclave created: {EnclaveId}", enclave.Id);

            return CreatedAtAction(nameof(GetEnclave), new { id = enclave.Id }, enclave);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("SGX"))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "SGX Not Available",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SGX enclave");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while creating the SGX enclave",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get SGX enclave by ID
    /// </summary>
    /// <param name="id">Enclave ID</param>
    /// <returns>Enclave details</returns>
    [HttpGet("enclaves/{id:guid}")]
    [Authorize(Policy = "ComputeRead")]
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
                Detail = "An error occurred while retrieving the SGX enclave",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get all available SGX enclaves
    /// </summary>
    /// <param name="status">Filter by enclave status</param>
    /// <returns>List of SGX enclaves</returns>
    [HttpGet("enclaves")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(List<EnclaveResponse>), 200)]
    public async Task<IActionResult> GetEnclaves([FromQuery] SgxEnclaveStatus? status = null)
    {
        try
        {
            var enclaves = await _sgxEnclaveService.GetEnclavesAsync(status);
            return Ok(enclaves);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SGX enclaves");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving SGX enclaves",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Perform attestation for an SGX enclave
    /// </summary>
    /// <param name="request">Attestation parameters</param>
    /// <returns>Attestation result</returns>
    [HttpPost("attestation")]
    [Authorize(Policy = "SgxOperator")]
    [ProducesResponseType(typeof(AttestationResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> PerformAttestation([FromBody] AttestationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var attestation = await _attestationService.PerformAttestationAsync(request);

            _logger.LogInformation("Attestation performed: {AttestationId} for enclave: {EnclaveId}", 
                attestation.AttestationId, request.EnclaveId);

            return CreatedAtAction(nameof(GetAttestation), new { id = attestation.AttestationId }, attestation);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("enclave"))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Enclave",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing attestation for enclave: {EnclaveId}", request.EnclaveId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while performing attestation",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get attestation result by ID
    /// </summary>
    /// <param name="id">Attestation ID</param>
    /// <returns>Attestation details</returns>
    [HttpGet("attestation/{id:guid}")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(AttestationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetAttestation([FromRoute] Guid id)
    {
        try
        {
            var attestation = await _attestationService.GetAttestationAsync(id);
            
            if (attestation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Attestation Not Found",
                    Detail = $"Attestation result not found: {id}",
                    Status = 404
                });
            }

            return Ok(attestation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attestation: {AttestationId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the attestation",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Perform secure computation
    /// </summary>
    /// <param name="request">Secure computation parameters</param>
    /// <returns>Computation result</returns>
    [HttpPost("secure-compute")]
    [Authorize(Policy = "ComputeWrite")]
    [ProducesResponseType(typeof(SecureComputationResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> PerformSecureComputation([FromBody] SecureComputationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _secureComputationService.PerformComputationAsync(request);

            _logger.LogInformation("Secure computation completed: {SessionId}", result.SessionId);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("enclave"))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Enclave Unavailable",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing secure computation");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while performing secure computation",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get compute service statistics
    /// </summary>
    /// <returns>Service performance and resource usage statistics</returns>
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
    /// Get resource allocation for a job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Resource allocation details</returns>
    [HttpGet("jobs/{jobId:guid}/resources")]
    [Authorize(Policy = "ComputeRead")]
    [ProducesResponseType(typeof(List<ResourceAllocation>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetResourceAllocation([FromRoute] Guid jobId)
    {
        try
        {
            var resources = await _resourceAllocationService.GetAllocationForJobAsync(jobId);
            return Ok(resources);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job Not Found",
                Detail = ex.Message,
                Status = 404
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource allocation for job: {JobId}", jobId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving resource allocation",
                Status = 500
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }
}