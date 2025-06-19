using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Services.Voting.Models;

namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VotingController : ControllerBase
{
    private readonly IVotingService _votingService;
    private readonly ILogger<VotingController> _logger;

    public VotingController(IVotingService votingService, ILogger<VotingController> logger)
    {
        _votingService = votingService;
        _logger = logger;
    }

    [HttpPost("proposal")]
    public async Task<IActionResult> CreateProposal([FromBody] CreateProposalRequest request)
    {
        try
        {
            var result = await _votingService.CreateProposalAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating proposal");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("vote")]
    public async Task<IActionResult> CastVote([FromBody] CastVoteRequest request)
    {
        try
        {
            var result = await _votingService.CastVoteAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error casting vote");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("proposal/{proposalId}")]
    public async Task<IActionResult> GetProposal(string proposalId)
    {
        try
        {
            var request = new GetProposalRequest { ProposalId = proposalId };
            var result = await _votingService.GetProposalAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proposal");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("proposals")]
    public async Task<IActionResult> GetProposals([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new GetProposalsRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _votingService.GetProposalsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proposals");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("results/{proposalId}")]
    public async Task<IActionResult> GetVotingResults(string proposalId)
    {
        try
        {
            var request = new GetVotingResultsRequest { ProposalId = proposalId };
            var result = await _votingService.GetVotingResultsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voting results");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("delegate")]
    public async Task<IActionResult> DelegateVote([FromBody] DelegateVoteRequest request)
    {
        try
        {
            var result = await _votingService.DelegateVoteAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delegating vote");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("delegate/{delegationId}")]
    public async Task<IActionResult> RevokeDelegation(string delegationId)
    {
        try
        {
            var request = new RevokeDelegationRequest { DelegationId = delegationId };
            var result = await _votingService.RevokeDelegationAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking delegation");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("candidate/{candidateId}")]
    public async Task<IActionResult> GetCandidate(string candidateId)
    {
        try
        {
            var request = new GetCandidateRequest { CandidateId = candidateId };
            var result = await _votingService.GetCandidateAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting candidate");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new GetCandidatesRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _votingService.GetCandidatesAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting candidates");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("candidate")]
    public async Task<IActionResult> RegisterCandidate([FromBody] RegisterCandidateRequest request)
    {
        try
        {
            var result = await _votingService.RegisterCandidateAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering candidate");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("candidate/{candidateId}")]
    public async Task<IActionResult> UnregisterCandidate(string candidateId)
    {
        try
        {
            var request = new UnregisterCandidateRequest { CandidateId = candidateId };
            var result = await _votingService.UnregisterCandidateAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering candidate");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetVotingStatistics()
    {
        try
        {
            var request = new VotingStatisticsRequest();
            var result = await _votingService.GetVotingStatisticsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voting statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("strategy")]
    public async Task<IActionResult> ExecuteVotingStrategy([FromBody] ExecuteVotingStrategyRequest request)
    {
        try
        {
            var result = await _votingService.ExecuteVotingStrategyAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing voting strategy");
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 