using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Services.Voting.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for voting and governance operations.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Voting")]
public class VotingController : BaseApiController
{
    private readonly VotingService _votingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="VotingController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="votingService">The voting service.</param>
    public VotingController(ILogger<VotingController> logger, VotingService votingService)
        : base(logger)
    {
        _votingService = votingService;
    }

    /// <summary>
    /// Creates a new voting session.
    /// </summary>
    /// <param name="request">The voting session creation request.</param>
    /// <returns>The created voting session details.</returns>
    [HttpPost("sessions")]
    [Authorize(Roles = "Admin,GovernanceUser")]
    [ProducesResponseType(typeof(ApiResponse<VotingSessionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateVotingSession([FromBody] CreateVotingSessionRequest request)
    {
        try
        {
            Logger.LogInformation("Creating voting session for user {UserId}", GetCurrentUserId());
            
            var result = await _votingService.CreateVotingSessionAsync(request);
            return Ok(CreateResponse(result, "Voting session created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateVotingSession");
        }
    }

    /// <summary>
    /// Casts a vote in a voting session.
    /// </summary>
    /// <param name="sessionId">The voting session ID.</param>
    /// <param name="request">The vote casting request.</param>
    /// <returns>The vote confirmation.</returns>
    [HttpPost("sessions/{sessionId}/votes")]
    [Authorize(Roles = "Admin,GovernanceUser,Voter")]
    [ProducesResponseType(typeof(ApiResponse<VoteCastResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CastVote(string sessionId, [FromBody] CastVoteRequest request)
    {
        try
        {
            request.SessionId = sessionId;
            request.VoterId = GetCurrentUserId();
            
            Logger.LogInformation("Casting vote in session {SessionId} for user {UserId}", sessionId, GetCurrentUserId());
            
            var result = await _votingService.CastVoteAsync(request);
            return Ok(CreateResponse(result, "Vote cast successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CastVote");
        }
    }

    /// <summary>
    /// Gets the results of a voting session.
    /// </summary>
    /// <param name="sessionId">The voting session ID.</param>
    /// <returns>The voting results.</returns>
    [HttpGet("sessions/{sessionId}/results")]
    [Authorize(Roles = "Admin,GovernanceUser,Voter")]
    [ProducesResponseType(typeof(ApiResponse<VotingResultsResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetVotingResults(string sessionId)
    {
        try
        {
            var result = await _votingService.GetVotingResultsAsync(sessionId);
            return Ok(CreateResponse(result, "Voting results retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetVotingResults");
        }
    }

    /// <summary>
    /// Gets all active voting sessions.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>The list of active voting sessions.</returns>
    [HttpGet("sessions/active")]
    [Authorize(Roles = "Admin,GovernanceUser,Voter")]
    [ProducesResponseType(typeof(PaginatedResponse<VotingSessionSummary>), 200)]
    public async Task<IActionResult> GetActiveVotingSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _votingService.GetActiveVotingSessionsAsync(page, pageSize);
            
            return Ok(new PaginatedResponse<VotingSessionSummary>
            {
                Success = true,
                Data = result.Items,
                Message = "Active voting sessions retrieved successfully",
                Timestamp = DateTime.UtcNow,
                Page = page,
                PageSize = pageSize,
                TotalItems = result.TotalCount,
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetActiveVotingSessions");
        }
    }

    /// <summary>
    /// Gets details of a specific voting session.
    /// </summary>
    /// <param name="sessionId">The voting session ID.</param>
    /// <returns>The voting session details.</returns>
    [HttpGet("sessions/{sessionId}")]
    [Authorize(Roles = "Admin,GovernanceUser,Voter")]
    [ProducesResponseType(typeof(ApiResponse<VotingSessionDetailsResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetVotingSession(string sessionId)
    {
        try
        {
            var result = await _votingService.GetVotingSessionAsync(sessionId);
            return Ok(CreateResponse(result, "Voting session details retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetVotingSession");
        }
    }

    /// <summary>
    /// Closes a voting session.
    /// </summary>
    /// <param name="sessionId">The voting session ID.</param>
    /// <returns>The session closure confirmation.</returns>
    [HttpPost("sessions/{sessionId}/close")]
    [Authorize(Roles = "Admin,GovernanceUser")]
    [ProducesResponseType(typeof(ApiResponse<VotingSessionCloseResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CloseVotingSession(string sessionId)
    {
        try
        {
            Logger.LogInformation("Closing voting session {SessionId} for user {UserId}", sessionId, GetCurrentUserId());
            
            var result = await _votingService.CloseVotingSessionAsync(sessionId);
            return Ok(CreateResponse(result, "Voting session closed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CloseVotingSession");
        }
    }

    /// <summary>
    /// Gets the voting history for the current user.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>The user's voting history.</returns>
    [HttpGet("history")]
    [Authorize(Roles = "Admin,GovernanceUser,Voter")]
    [ProducesResponseType(typeof(PaginatedResponse<VoteHistoryItem>), 200)]
    public async Task<IActionResult> GetVotingHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _votingService.GetVotingHistoryAsync(userId, page, pageSize);
            
            return Ok(new PaginatedResponse<VoteHistoryItem>
            {
                Success = true,
                Data = result.Items,
                Message = "Voting history retrieved successfully",
                Timestamp = DateTime.UtcNow,
                Page = page,
                PageSize = pageSize,
                TotalItems = result.TotalCount,
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetVotingHistory");
        }
    }

    /// <summary>
    /// Gets voting statistics and analytics.
    /// </summary>
    /// <param name="sessionId">Optional session ID to get specific statistics.</param>
    /// <returns>The voting statistics.</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,GovernanceUser")]
    [ProducesResponseType(typeof(ApiResponse<VotingStatistics>), 200)]
    public async Task<IActionResult> GetVotingStatistics([FromQuery] string? sessionId = null)
    {
        try
        {
            var result = await _votingService.GetVotingStatisticsAsync(sessionId);
            return Ok(CreateResponse(result, "Voting statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetVotingStatistics");
        }
    }
}