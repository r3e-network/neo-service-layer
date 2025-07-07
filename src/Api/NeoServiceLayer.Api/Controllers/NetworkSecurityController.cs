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
    /// Configures firewall rules for the enclave.
    /// </summary>
    /// <param name="rules">The firewall rule set.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The configuration result.</returns>
    /// <response code="200">Firewall configured successfully.</response>
    /// <response code="400">Invalid rule parameters.</response>
    [HttpPost("{blockchainType}/firewall/configure")]
    [Authorize(Roles = "Admin,Security")]
    [ProducesResponseType(typeof(ApiResponse<FirewallConfigurationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ConfigureFirewall(
        [FromBody] FirewallRuleSet rules,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.ConfigureFirewallAsync(rules, blockchain);

            Logger.LogInformation("Configured firewall with {RuleCount} rules on {Blockchain}",
                rules.Rules.Count, blockchainType);

            return Ok(CreateResponse(result, "Firewall configured successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "configuring firewall");
        }
    }

    /// <summary>
    /// Creates a secure communication channel.
    /// </summary>
    /// <param name="request">The channel creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created channel information.</returns>
    /// <response code="200">Channel created successfully.</response>
    /// <response code="400">Invalid channel parameters.</response>
    [HttpPost("{blockchainType}/channels")]
    [ProducesResponseType(typeof(ApiResponse<SecureChannelResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateSecureChannel(
        [FromBody] CreateChannelRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.CreateSecureChannelAsync(request, blockchain);

            Logger.LogInformation("Created secure channel {ChannelId} on {Blockchain}",
                result.ChannelId, blockchainType);

            return Ok(CreateResponse(result, "Secure channel created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating secure channel");
        }
    }

    /// <summary>
    /// Sends an encrypted message through a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The message response.</returns>
    /// <response code="200">Message sent successfully.</response>
    /// <response code="404">Channel not found.</response>
    [HttpPost("{blockchainType}/channels/{channelId}/send")]
    [ProducesResponseType(typeof(ApiResponse<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SendMessage(
        [FromRoute] string channelId,
        [FromBody] NetworkMessage message,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.SendMessageAsync(channelId, message, blockchain);

            Logger.LogInformation("Sent message through channel {ChannelId} on {Blockchain}", channelId, blockchainType);
            return Ok(CreateResponse(result, "Message sent successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "sending message");
        }
    }

    /// <summary>
    /// Monitors network traffic and security events.
    /// </summary>
    /// <param name="request">The monitoring request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The network monitoring data.</returns>
    /// <response code="200">Monitoring data retrieved successfully.</response>
    [HttpPost("{blockchainType}/monitor")]
    [ProducesResponseType(typeof(ApiResponse<NetworkMonitoringData>), 200)]
    public async Task<IActionResult> MonitorNetwork(
        [FromBody] MonitoringRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _networkSecurityService.MonitorNetworkAsync(request, blockchain);

            Logger.LogInformation("Retrieved network monitoring data from {StartTime} to {EndTime} on {Blockchain}",
                request.StartTime, request.EndTime, blockchainType);
            return Ok(CreateResponse(result, "Network monitoring data retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "monitoring network");
        }
    }

    /// <summary>
    /// Gets the status of a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The channel status.</returns>
    /// <response code="200">Channel status retrieved successfully.</response>
    /// <response code="404">Channel not found.</response>
    [HttpGet("{blockchainType}/channels/{channelId}/status")]
    [ProducesResponseType(typeof(ApiResponse<ChannelStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetChannelStatus(
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
            var result = await _networkSecurityService.GetChannelStatusAsync(channelId, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Channel not found: {channelId}"));
            }

            return Ok(CreateResponse(result, "Channel status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "getting channel status");
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
    [HttpDelete("{blockchainType}/channels/{channelId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CloseChannel(
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
            var result = await _networkSecurityService.CloseChannelAsync(channelId, blockchain);

            if (!result)
            {
                return NotFound(CreateErrorResponse($"Channel not found: {channelId}"));
            }

            Logger.LogInformation("Closed channel {ChannelId} on {Blockchain}", channelId, blockchainType);
            return Ok(CreateResponse(result, "Channel closed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "closing channel");
        }
    }




}
