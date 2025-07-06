using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Services.Compute.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for distributed compute operations within trusted execution environments.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compute")]
[Authorize]
[Tags("Compute")]
public class ComputeController : BaseApiController
{
    private readonly IComputeService _computeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeController"/> class.
    /// </summary>
    /// <param name="computeService">The compute service.</param>
    /// <param name="logger">The logger.</param>
    public ComputeController(IComputeService computeService, ILogger<ComputeController> logger)
        : base(logger)
    {
        _computeService = computeService ?? throw new ArgumentNullException(nameof(computeService));
    }

    /// <summary>
    /// Submits a compute job for execution in a trusted environment.
    /// </summary>
    /// <param name="request">The compute job request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The job submission result.</returns>
    /// <response code="200">Job submitted successfully.</response>
    /// <response code="400">Invalid job parameters.</response>
    /// <response code="429">Compute quota exceeded.</response>
    [HttpPost("{blockchainType}/jobs")]
    [ProducesResponseType(typeof(ApiResponse<ComputeJobResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 429)]
    public async Task<IActionResult> SubmitJob(
        [FromBody] ComputeJobRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.SubmitJobAsync(request, blockchain);

            Logger.LogInformation("Submitted compute job {JobId} of type {JobType} on {Blockchain}",
                result.JobId, request.JobType, blockchainType);

            return Ok(CreateResponse(result, "Compute job submitted successfully"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("quota"))
        {
            return StatusCode(429, CreateErrorResponse("Compute quota exceeded. Please try again later."));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "submitting compute job");
        }
    }

    /// <summary>
    /// Gets the status and results of a compute job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The job status and results.</returns>
    /// <response code="200">Job status retrieved successfully.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet("{blockchainType}/jobs/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<ComputeJobStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetJobStatus(
        [FromRoute] string jobId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.GetJobStatusAsync(jobId, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Compute job not found: {jobId}"));
            }

            return Ok(CreateResponse(result, "Job status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving job status");
        }
    }

    /// <summary>
    /// Cancels a running compute job.
    /// </summary>
    /// <param name="jobId">The job ID to cancel.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Cancellation result.</returns>
    /// <response code="200">Job cancelled successfully.</response>
    /// <response code="404">Job not found.</response>
    /// <response code="409">Job cannot be cancelled in current state.</response>
    [HttpDelete("{blockchainType}/jobs/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> CancelJob(
        [FromRoute] string jobId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.CancelJobAsync(jobId, blockchain);

            if (!result)
            {
                return StatusCode(409, CreateErrorResponse("Job cannot be cancelled in its current state"));
            }

            Logger.LogInformation("Cancelled compute job {JobId} on {Blockchain}", jobId, blockchainType);
            return Ok(CreateResponse(result, "Job cancelled successfully"));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(CreateErrorResponse($"Compute job not found: {jobId}"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "cancelling job");
        }
    }

    /// <summary>
    /// Gets the results of a completed compute job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The job results.</returns>
    /// <response code="200">Job results retrieved successfully.</response>
    /// <response code="404">Job not found.</response>
    /// <response code="409">Job not completed yet.</response>
    [HttpGet("{blockchainType}/jobs/{jobId}/results")]
    [ProducesResponseType(typeof(ApiResponse<ComputeJobResults>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> GetJobResults(
        [FromRoute] string jobId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.GetJobResultsAsync(jobId, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Compute job not found: {jobId}"));
            }

            return Ok(CreateResponse(result, "Job results retrieved successfully"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not completed"))
        {
            return StatusCode(409, CreateErrorResponse("Job is not completed yet. Check job status first."));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving job results");
        }
    }

    /// <summary>
    /// Lists compute jobs with filtering and pagination.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="status">Filter by job status.</param>
    /// <param name="jobType">Filter by job type.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>List of compute jobs.</returns>
    /// <response code="200">Jobs retrieved successfully.</response>
    [HttpGet("{blockchainType}/jobs")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ComputeJobSummary>>), 200)]
    public async Task<IActionResult> ListJobs(
        [FromRoute] string blockchainType,
        [FromQuery] string? status = null,
        [FromQuery] string? jobType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var filter = new ComputeJobFilter
            {
                Status = status,
                JobType = jobType,
                Page = page,
                PageSize = Math.Min(pageSize, 100) // Cap at 100
            };

            var result = await _computeService.ListJobsAsync(filter, blockchain);

            return Ok(CreateResponse(result, "Jobs retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving job list");
        }
    }

    /// <summary>
    /// Gets compute resource availability and pricing.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Resource availability information.</returns>
    /// <response code="200">Resource info retrieved successfully.</response>
    [HttpGet("{blockchainType}/resources")]
    [ProducesResponseType(typeof(ApiResponse<ComputeResourceInfo>), 200)]
    public async Task<IActionResult> GetResourceInfo(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.GetResourceInfoAsync(blockchain);

            return Ok(CreateResponse(result, "Resource information retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving resource information");
        }
    }

    /// <summary>
    /// Estimates the cost and time for a compute job.
    /// </summary>
    /// <param name="request">The estimation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Cost and time estimation.</returns>
    /// <response code="200">Estimation completed successfully.</response>
    [HttpPost("{blockchainType}/estimate")]
    [ProducesResponseType(typeof(ApiResponse<ComputeEstimation>), 200)]
    public async Task<IActionResult> EstimateJob(
        [FromBody] ComputeEstimationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.EstimateJobAsync(request, blockchain);

            return Ok(CreateResponse(result, "Job estimation completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "estimating job cost");
        }
    }

    /// <summary>
    /// Gets compute service metrics and statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Service metrics.</returns>
    /// <response code="200">Metrics retrieved successfully.</response>
    [HttpGet("{blockchainType}/metrics")]
    [Authorize(Roles = "Admin,Monitor")]
    [ProducesResponseType(typeof(ApiResponse<ComputeMetrics>), 200)]
    public async Task<IActionResult> GetMetrics(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.GetMetricsAsync(blockchain);

            return Ok(CreateResponse(result, "Compute metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving compute metrics");
        }
    }
}