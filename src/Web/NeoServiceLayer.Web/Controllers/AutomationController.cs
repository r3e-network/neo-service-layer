using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Automation;
using AutomationService = NeoServiceLayer.Services.Automation;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for Smart Contract Automation operations.
/// </summary>
[Tags("Automation")]
public class AutomationController : BaseApiController
{
    private readonly AutomationService.IAutomationService _automationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutomationController"/> class.
    /// </summary>
    /// <param name="automationService">The automation service.</param>
    /// <param name="logger">The logger.</param>
    public AutomationController(
        AutomationService.IAutomationService automationService,
        ILogger<AutomationController> logger) : base(logger)
    {
        _automationService = automationService;
    }

    /// <summary>
    /// Creates a new automation job.
    /// </summary>
    /// <param name="request">The automation job request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created job ID.</returns>
    /// <response code="200">Job created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Job creation failed.</response>
    [HttpPost("create-job/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CreateJob(
        [FromBody] AutomationService.AutomationJobRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var jobId = await _automationService.CreateJobAsync(request, blockchain);

            Logger.LogInformation("Created automation job {JobId} for user {UserId} on {BlockchainType}",
                jobId, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(jobId, "Automation job created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateJob");
        }
    }

    /// <summary>
    /// Gets the status of an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The job status.</returns>
    /// <response code="200">Job status retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet("job/{jobId}/status/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<AutomationService.AutomationJobStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
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
            var status = await _automationService.GetJobStatusAsync(jobId, blockchain);

            return Ok(CreateResponse(status, "Job status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetJobStatus");
        }
    }

    /// <summary>
    /// Cancels an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The cancellation result.</returns>
    /// <response code="200">Job cancelled successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Job not found.</response>
    [HttpPost("job/{jobId}/cancel/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
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
            var result = await _automationService.CancelJobAsync(jobId, blockchain);

            Logger.LogInformation("Cancelled automation job {JobId} by user {UserId}", jobId, GetCurrentUserId());

            return Ok(CreateResponse(result, result ? "Job cancelled successfully" : "Job could not be cancelled"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CancelJob");
        }
    }

    /// <summary>
    /// Pauses an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The pause result.</returns>
    /// <response code="200">Job paused successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Job not found.</response>
    [HttpPost("job/{jobId}/pause/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> PauseJob(
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
            var result = await _automationService.PauseJobAsync(jobId, blockchain);

            Logger.LogInformation("Paused automation job {JobId} by user {UserId}", jobId, GetCurrentUserId());

            return Ok(CreateResponse(result, result ? "Job paused successfully" : "Job could not be paused"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "PauseJob");
        }
    }

    /// <summary>
    /// Resumes an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The resume result.</returns>
    /// <response code="200">Job resumed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Job not found.</response>
    [HttpPost("job/{jobId}/resume/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ResumeJob(
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
            var result = await _automationService.ResumeJobAsync(jobId, blockchain);

            Logger.LogInformation("Resumed automation job {JobId} by user {UserId}", jobId, GetCurrentUserId());

            return Ok(CreateResponse(result, result ? "Job resumed successfully" : "Job could not be resumed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ResumeJob");
        }
    }

    /// <summary>
    /// Gets automation jobs for an address.
    /// </summary>
    /// <param name="address">The owner address.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The list of jobs.</returns>
    /// <response code="200">Jobs retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("jobs/{address}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AutomationService.AutomationJob>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetJobs(
        [FromRoute] string address,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var jobs = await _automationService.GetJobsAsync(address, blockchain);

            return Ok(CreateResponse(jobs, "Jobs retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetJobs");
        }
    }

    /// <summary>
    /// Updates an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="update">The job update.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The update result.</returns>
    /// <response code="200">Job updated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Job not found.</response>
    [HttpPut("job/{jobId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateJob(
        [FromRoute] string jobId,
        [FromBody] AutomationService.AutomationJobUpdate update,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _automationService.UpdateJobAsync(jobId, update, blockchain);

            Logger.LogInformation("Updated automation job {JobId} by user {UserId}", jobId, GetCurrentUserId());

            return Ok(CreateResponse(result, result ? "Job updated successfully" : "Job could not be updated"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UpdateJob");
        }
    }

    /// <summary>
    /// Gets execution history for a job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The execution history.</returns>
    /// <response code="200">Execution history retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet("job/{jobId}/history/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AutomationService.AutomationExecution>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetExecutionHistory(
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
            var history = await _automationService.GetExecutionHistoryAsync(jobId, blockchain);

            return Ok(CreateResponse(history, "Execution history retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetExecutionHistory");
        }
    }
}
