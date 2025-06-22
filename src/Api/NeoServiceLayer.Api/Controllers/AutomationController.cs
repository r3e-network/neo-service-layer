using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using AutomationSvc = NeoServiceLayer.Services.Automation;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for automation services.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/automation")]
[ApiVersion("1.0")]
[Authorize]
public class AutomationController : ControllerBase
{
    private readonly AutomationSvc.IAutomationService _automationService;
    private readonly ILogger<AutomationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutomationController"/> class.
    /// </summary>
    /// <param name="automationService">The automation service.</param>
    /// <param name="logger">The logger.</param>
    public AutomationController(AutomationSvc.IAutomationService automationService, ILogger<AutomationController> logger)
    {
        _automationService = automationService ?? throw new ArgumentNullException(nameof(automationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new automation job.
    /// </summary>
    /// <param name="request">The automation job creation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created automation job.</returns>
    [HttpPost("jobs/{blockchainType}")]
    [ProducesResponseType(typeof(AutomationJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AutomationJobResponse>> CreateAutomationJobAsync(
        [FromBody] CreateAutomationJobRequest request,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            // Map the request to CreateAutomationRequest  
            var automationRequest = new NeoServiceLayer.Services.Automation.CreateAutomationRequest
            {
                Name = request.JobType + "_" + request.ContractAddress,
                Description = $"Automation job for {request.MethodName} on {request.ContractAddress}",
                TriggerType = AutomationSvc.AutomationTriggerType.Schedule,
                TriggerConfiguration = System.Text.Json.JsonSerializer.Serialize(request.Schedule),
                ActionType = AutomationSvc.AutomationActionType.SmartContract,
                ActionConfiguration = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ContractAddress = request.ContractAddress,
                    MethodName = request.MethodName,
                    Parameters = request.Parameters
                }),
                IsActive = true
            };

            var result = await _automationService.CreateAutomationAsync(automationRequest, blockchainType);

            var response = new AutomationJobResponse
            {
                JobId = result.AutomationId ?? string.Empty,
                Status = result.Success ? "Created" : "Failed",
                CreatedAt = result.CreatedAt,
                NextExecutionTime = null  // Not available in CreateAutomationResponse
            };

            return CreatedAtAction(
                nameof(GetJobStatusAsync),
                new { jobId = response.JobId, blockchainType },
                response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating automation job");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating automation job");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the automation job");
        }
    }

    /// <summary>
    /// Gets the status of an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The job status.</returns>
    [HttpGet("jobs/{jobId}/status/{blockchainType}")]
    [ProducesResponseType(typeof(AutomationJobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AutomationJobStatusResponse>> GetJobStatusAsync(
        [FromRoute] string jobId,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _automationService.GetJobStatusAsync(jobId, blockchainType);

            var response = new AutomationJobStatusResponse
            {
                JobId = jobId,
                Status = result.ToString(),
                LastExecutionTime = null,  // Not available from status enum alone
                NextExecutionTime = null,  // Not available from status enum alone  
                ExecutionCount = 0,        // Not available from status enum alone
                LastError = null           // Not available from status enum alone
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Automation job with ID '{jobId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automation job status for job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting the job status");
        }
    }

    /// <summary>
    /// Cancels an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cancellation result.</returns>
    [HttpPost("jobs/{jobId}/cancel/{blockchainType}")]
    [ProducesResponseType(typeof(AutomationJobActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AutomationJobActionResponse>> CancelJobAsync(
        [FromRoute] string jobId,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _automationService.CancelJobAsync(jobId, blockchainType);

            var response = new AutomationJobActionResponse
            {
                JobId = jobId,
                Success = result,
                Message = result ? "Job cancelled successfully" : "Failed to cancel job"
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Automation job with ID '{jobId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling automation job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cancelling the job");
        }
    }

    /// <summary>
    /// Pauses an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pause result.</returns>
    [HttpPost("jobs/{jobId}/pause/{blockchainType}")]
    [ProducesResponseType(typeof(AutomationJobActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AutomationJobActionResponse>> PauseJobAsync(
        [FromRoute] string jobId,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _automationService.PauseJobAsync(jobId, blockchainType);

            var response = new AutomationJobActionResponse
            {
                JobId = jobId,
                Success = result,
                Message = result ? "Job paused successfully" : "Failed to pause job"
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Automation job with ID '{jobId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing automation job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while pausing the job");
        }
    }

    /// <summary>
    /// Resumes an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resume result.</returns>
    [HttpPost("jobs/{jobId}/resume/{blockchainType}")]
    [ProducesResponseType(typeof(AutomationJobActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AutomationJobActionResponse>> ResumeJobAsync(
        [FromRoute] string jobId,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _automationService.ResumeJobAsync(jobId, blockchainType);

            var response = new AutomationJobActionResponse
            {
                JobId = jobId,
                Success = result,
                Message = result ? "Job resumed successfully" : "Failed to resume job"
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Automation job with ID '{jobId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming automation job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while resuming the job");
        }
    }

    /// <summary>
    /// Gets automation jobs for a specific address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of automation jobs.</returns>
    [HttpGet("jobs/{address}/{blockchainType}")]
    [ProducesResponseType(typeof(IEnumerable<AutomationJobSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AutomationJobSummary>>> GetJobsForAddressAsync(
        [FromRoute] string address,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _automationService.GetJobsAsync(address, blockchainType);

            var response = result.Select(job => new AutomationJobSummary
            {
                JobId = job.Id,
                JobType = job.Name,  // Using Name as JobType
                Status = job.Status.ToString(),
                CreatedAt = job.CreatedAt,
                LastExecutionTime = job.LastExecuted,
                NextExecutionTime = job.NextExecution
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automation jobs for address {Address}", address);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting automation jobs");
        }
    }

    /// <summary>
    /// Updates an automation job.
    /// </summary>
    /// <param name="request">The update request.</param>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The update result.</returns>
    [HttpPut("jobs/{jobId}/{blockchainType}")]
    [ProducesResponseType(typeof(AutomationJobActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AutomationJobActionResponse>> UpdateJobAsync(
        [FromBody] UpdateAutomationJobRequest request,
        [FromRoute] string jobId,
        [FromRoute] BlockchainType blockchainType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var updateRequest = new NeoServiceLayer.Services.Automation.AutomationJobUpdate
            {
                Name = request.JobType ?? "Updated Job",
                Description = "Updated automation job",
                IsEnabled = true
            };

            var result = await _automationService.UpdateJobAsync(jobId, updateRequest, blockchainType);

            var response = new AutomationJobActionResponse
            {
                JobId = jobId,
                Success = result,
                Message = result ? "Job updated successfully" : "Failed to update job"
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for updating automation job {JobId}", jobId);
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Automation job with ID '{jobId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating automation job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the job");
        }
    }

    /// <summary>
    /// Gets execution history for an automation job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="limit">The maximum number of executions to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The execution history.</returns>
    [HttpGet("jobs/{jobId}/executions/{blockchainType}")]
    [ProducesResponseType(typeof(IEnumerable<AutomationExecutionHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AutomationExecutionHistory>>> GetExecutionHistoryAsync(
        [FromRoute] string jobId,
        [FromRoute] BlockchainType blockchainType,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _automationService.GetExecutionHistoryAsync(jobId, blockchainType);

            var response = result.Select(execution => new AutomationExecutionHistory
            {
                ExecutionId = execution.Id,
                JobId = execution.JobId,
                ExecutionTime = execution.ExecutedAt,
                Status = execution.Status.ToString(),
                TransactionHash = execution.TransactionHash,
                GasUsed = (long?)(execution.GasUsed ?? 0),
                Error = execution.ErrorMessage
            });

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Automation job with ID '{jobId}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution history for job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting execution history");
        }
    }
}

/// <summary>
/// Request model for creating automation jobs.
/// </summary>
public class CreateAutomationJobRequest
{
    /// <summary>
    /// Gets or sets the job type.
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract address.
    /// </summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the trigger conditions.
    /// </summary>
    public Dictionary<string, object> TriggerConditions { get; set; } = new();

    /// <summary>
    /// Gets or sets the schedule.
    /// </summary>
    public Dictionary<string, object> Schedule { get; set; } = new();
}

/// <summary>
/// Request model for updating automation jobs.
/// </summary>
public class UpdateAutomationJobRequest
{
    /// <summary>
    /// Gets or sets the job type.
    /// </summary>
    public string? JobType { get; set; }

    /// <summary>
    /// Gets or sets the parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the trigger conditions.
    /// </summary>
    public Dictionary<string, object>? TriggerConditions { get; set; }

    /// <summary>
    /// Gets or sets the schedule.
    /// </summary>
    public Dictionary<string, object>? Schedule { get; set; }
}

/// <summary>
/// Response model for automation job creation.
/// </summary>
public class AutomationJobResponse
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the next execution time.
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }
}

/// <summary>
/// Response model for automation job status.
/// </summary>
public class AutomationJobStatusResponse
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the next execution time.
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the execution count.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the last error.
    /// </summary>
    public string? LastError { get; set; }
}

/// <summary>
/// Response model for automation job actions.
/// </summary>
public class AutomationJobActionResponse
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the action was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Model for automation job summary.
/// </summary>
public class AutomationJobSummary
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job type.
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the next execution time.
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }
}

/// <summary>
/// Model for automation execution history.
/// </summary>
public class AutomationExecutionHistory
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution time.
    /// </summary>
    public DateTime ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the gas used.
    /// </summary>
    public long? GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? Error { get; set; }
}
