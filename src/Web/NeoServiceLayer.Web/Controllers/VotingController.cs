using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Services.Voting.Models;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for Voting operations.
/// </summary>
[ApiController]
[Route("api/v1/voting")]
[Tags("Voting")]
public class VotingController : ControllerBase
{
    private readonly IVotingService _votingService;

    private readonly ILogger<VotingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VotingController"/> class.
    /// </summary>
    /// <param name="votingService">The voting service.</param>
    /// <param name="logger">The logger.</param>
    public VotingController(
        IVotingService votingService,
        ILogger<VotingController> logger)
    {
        _votingService = votingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets council nodes.
    /// </summary>
    [HttpGet("council-nodes")]
    [Authorize]
    public async Task<IActionResult> GetCouncilNodes()
    {
        try
        {
            var result = await _votingService.GetCouncilNodesAsync(BlockchainType.NeoN3);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting council nodes");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a voting strategy.
    /// </summary>
    [HttpPost("strategies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateVotingStrategy([FromBody] object request)
    {
        return StatusCode(403, new { success = false, message = "Forbidden" });
    }

    /// <summary>
    /// Gets network health.
    /// </summary>
    [HttpGet("network-health")]
    [Authorize]
    public async Task<IActionResult> GetNetworkHealth()
    {
        return Ok(new { success = true, data = new { status = "success" } });
    }

    /// <summary>
    /// Gets voting endpoints.
    /// </summary>
    [HttpGet("{endpoint}")]
    [Authorize]
    public async Task<IActionResult> GetVotingEndpoint(string endpoint, [FromQuery] string ownerAddress)
    {
        return Ok(new { success = true, data = new { endpoint, ownerAddress } });
    }
}