using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.NetworkSecurity;
using NeoServiceLayer.Services.NetworkSecurity.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for network security and firewall management operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/network-security")]
[Authorize]
[Tags("Network Security")]
public class NetworkSecurityController : BaseApiController
{
    private readonly INetworkSecurityService _networkSecurityService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkSecurityController"/> class.
    /// </summary>
    /// <param name="networkSecurityService">The network security service.</param>
    /// <param name="logger">The logger.</param>
    public NetworkSecurityController(INetworkSecurityService networkSecurityService, ILogger<NetworkSecurityController> logger)
        : base(logger)
    {
        _networkSecurityService = networkSecurityService ?? throw new ArgumentNullException(nameof(networkSecurityService));
    }

    /// <summary>
    /// Creates a new firewall rule.
    /// </summary>
    /// <param name="request">The firewall rule request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created rule details.</returns>
    /// <response code="200">Rule created successfully.</response>
    /// <response code="400">Invalid rule parameters.</response>
    /// <response code="409">Rule conflicts with existing rules.</response>
    [HttpPost("{blockchainType}/firewall/rules")]
    [Authorize(Roles = "Admin,Security")]
    [ProducesResponseType(typeof(ApiResponse<FirewallRuleResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> CreateFirewallRule(
        [FromBody] CreateFirewallRuleRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.CreateFirewallRuleAsync(request, blockchain);

            Logger.LogInformation("Created firewall rule {RuleId} for {Direction} traffic on {Blockchain}",
                result.RuleId, request.Direction, blockchainType);

            return Ok(CreateResponse(result, "Firewall rule created successfully"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("conflict"))
        {
            return StatusCode(409, CreateErrorResponse("Rule conflicts with existing firewall rules"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating firewall rule");
        }
    }

    /// <summary>
    /// Gets firewall rules with filtering options.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="direction">Filter by traffic direction.</param>
    /// <param name="status">Filter by rule status.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>List of firewall rules.</returns>
    /// <response code="200">Rules retrieved successfully.</response>
    [HttpGet("{blockchainType}/firewall/rules")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<FirewallRule>>), 200)]
    public async Task<IActionResult> GetFirewallRules(
        [FromRoute] string blockchainType,
        [FromQuery] string? direction = null,
        [FromQuery] string? status = null,
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
            var filter = new FirewallRuleFilter
            {
                Direction = direction,
                Status = status,
                Page = page,
                PageSize = Math.Min(pageSize, 100) // Cap at 100
            };

            var result = await _networkSecurityService.GetFirewallRulesAsync(filter, blockchain);

            return Ok(CreateResponse(result, "Firewall rules retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving firewall rules");
        }
    }

    /// <summary>
    /// Updates a firewall rule.
    /// </summary>
    /// <param name="ruleId">The rule ID to update.</param>
    /// <param name="request">The update request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Update result.</returns>
    /// <response code="200">Rule updated successfully.</response>
    /// <response code="404">Rule not found.</response>
    [HttpPut("{blockchainType}/firewall/rules/{ruleId}")]
    [Authorize(Roles = "Admin,Security")]
    [ProducesResponseType(typeof(ApiResponse<FirewallRuleResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateFirewallRule(
        [FromRoute] string ruleId,
        [FromBody] UpdateFirewallRuleRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.UpdateFirewallRuleAsync(ruleId, request, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Firewall rule not found: {ruleId}"));
            }

            Logger.LogInformation("Updated firewall rule {RuleId} on {Blockchain}", ruleId, blockchainType);
            return Ok(CreateResponse(result, "Firewall rule updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "updating firewall rule");
        }
    }

    /// <summary>
    /// Deletes a firewall rule.
    /// </summary>
    /// <param name="ruleId">The rule ID to delete.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Deletion result.</returns>
    /// <response code="200">Rule deleted successfully.</response>
    /// <response code="404">Rule not found.</response>
    [HttpDelete("{blockchainType}/firewall/rules/{ruleId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteFirewallRule(
        [FromRoute] string ruleId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.DeleteFirewallRuleAsync(ruleId, blockchain);

            if (!result)
            {
                return NotFound(CreateErrorResponse($"Firewall rule not found: {ruleId}"));
            }

            Logger.LogInformation("Deleted firewall rule {RuleId} on {Blockchain}", ruleId, blockchainType);
            return Ok(CreateResponse(result, "Firewall rule deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "deleting firewall rule");
        }
    }

    /// <summary>
    /// Establishes a secure communication channel.
    /// </summary>
    /// <param name="request">The secure channel request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The secure channel details.</returns>
    /// <response code="200">Secure channel established successfully.</response>
    /// <response code="400">Invalid channel parameters.</response>
    [HttpPost("{blockchainType}/secure-channels")]
    [Authorize(Roles = "Admin,Service")]
    [ProducesResponseType(typeof(ApiResponse<SecureChannelResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> EstablishSecureChannel(
        [FromBody] SecureChannelRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.EstablishSecureChannelAsync(request, blockchain);

            Logger.LogInformation("Established secure channel {ChannelId} between {LocalEndpoint} and {RemoteEndpoint}",
                result.ChannelId, request.LocalEndpoint, request.RemoteEndpoint);

            return Ok(CreateResponse(result, "Secure channel established successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "establishing secure channel");
        }
    }

    /// <summary>
    /// Gets active secure channels.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>List of active secure channels.</returns>
    /// <response code="200">Channels retrieved successfully.</response>
    [HttpGet("{blockchainType}/secure-channels")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SecureChannelInfo>>), 200)]
    public async Task<IActionResult> GetSecureChannels(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.GetSecureChannelsAsync(blockchain);

            return Ok(CreateResponse(result, "Secure channels retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving secure channels");
        }
    }

    /// <summary>
    /// Closes a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID to close.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Closure result.</returns>
    /// <response code="200">Channel closed successfully.</response>
    /// <response code="404">Channel not found.</response>
    [HttpDelete("{blockchainType}/secure-channels/{channelId}")]
    [Authorize(Roles = "Admin,Service")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CloseSecureChannel(
        [FromRoute] string channelId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.CloseSecureChannelAsync(channelId, blockchain);

            if (!result)
            {
                return NotFound(CreateErrorResponse($"Secure channel not found: {channelId}"));
            }

            Logger.LogInformation("Closed secure channel {ChannelId} on {Blockchain}", channelId, blockchainType);
            return Ok(CreateResponse(result, "Secure channel closed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "closing secure channel");
        }
    }

    /// <summary>
    /// Performs a network security audit.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Security audit results.</returns>
    /// <response code="200">Audit completed successfully.</response>
    [HttpPost("{blockchainType}/audit")]
    [Authorize(Roles = "Admin,Security,Auditor")]
    [ProducesResponseType(typeof(ApiResponse<SecurityAuditResult>), 200)]
    public async Task<IActionResult> PerformSecurityAudit(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.PerformSecurityAuditAsync(blockchain);

            Logger.LogInformation("Completed network security audit on {Blockchain} - Score: {SecurityScore}",
                blockchainType, result.SecurityScore);

            return Ok(CreateResponse(result, "Security audit completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "performing security audit");
        }
    }

    /// <summary>
    /// Gets network traffic statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="timeRange">Time range for statistics (hours).</param>
    /// <returns>Traffic statistics.</returns>
    /// <response code="200">Statistics retrieved successfully.</response>
    [HttpGet("{blockchainType}/traffic/statistics")]
    [ProducesResponseType(typeof(ApiResponse<TrafficStatistics>), 200)]
    public async Task<IActionResult> GetTrafficStatistics(
        [FromRoute] string blockchainType,
        [FromQuery] int timeRange = 24)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.GetTrafficStatisticsAsync(blockchain, timeRange);

            return Ok(CreateResponse(result, "Traffic statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving traffic statistics");
        }
    }

    /// <summary>
    /// Gets detected security threats.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="severity">Filter by threat severity.</param>
    /// <param name="limit">Maximum number of threats to return.</param>
    /// <returns>List of detected threats.</returns>
    /// <response code="200">Threats retrieved successfully.</response>
    [HttpGet("{blockchainType}/threats")]
    [Authorize(Roles = "Admin,Security,Monitor")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SecurityThreat>>), 200)]
    public async Task<IActionResult> GetSecurityThreats(
        [FromRoute] string blockchainType,
        [FromQuery] string? severity = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var filter = new ThreatFilter
            {
                Severity = severity,
                Limit = Math.Min(limit, 100) // Cap at 100
            };

            var result = await _networkSecurityService.GetSecurityThreatsAsync(filter, blockchain);

            return Ok(CreateResponse(result, "Security threats retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving security threats");
        }
    }

    /// <summary>
    /// Blocks an IP address or network range.
    /// </summary>
    /// <param name="request">The block request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Block operation result.</returns>
    /// <response code="200">IP/network blocked successfully.</response>
    [HttpPost("{blockchainType}/block-ip")]
    [Authorize(Roles = "Admin,Security")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> BlockIpAddress(
        [FromBody] BlockIpRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.BlockIpAddressAsync(request, blockchain);

            Logger.LogWarning("Blocked IP/network {IpAddress} on {Blockchain} - Reason: {Reason}",
                request.IpAddress, blockchainType, request.Reason);

            return Ok(CreateResponse(result, "IP address blocked successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "blocking IP address");
        }
    }
}