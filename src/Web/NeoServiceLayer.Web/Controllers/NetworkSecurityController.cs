using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.NetworkSecurity;
using NeoServiceLayer.Services.NetworkSecurity.Models;
using NeoServiceLayer.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Controller for network security service operations.
/// </summary>
[Authorize]
[Tags("Network Security")]
public class NetworkSecurityController : BaseApiController
{
    private readonly INetworkSecurityService _networkSecurityService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkSecurityController"/> class.
    /// </summary>
    public NetworkSecurityController(
        INetworkSecurityService networkSecurityService,
        ILogger<NetworkSecurityController> logger)
        : base(logger)
    {
        _networkSecurityService = networkSecurityService;
    }

    /// <summary>
    /// Creates a new secure communication channel.
    /// </summary>
    /// <param name="request">The channel creation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The created channel information.</returns>
    [HttpPost("channel/create/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<SecureChannelResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CreateSecureChannel(
        [FromBody] CreateChannelRequest request,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Invalid request", GetModelStateErrors()));
            }

            var result = await _networkSecurityService.CreateSecureChannelAsync(request, blockchainType);

            return Ok(CreateSuccessResponse(result, "Secure channel created successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create secure channel");
            return StatusCode(500, CreateErrorResponse("Failed to create secure channel", ex.Message));
        }
    }

    /// <summary>
    /// Sends an encrypted message through a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The message response.</returns>
    [HttpPost("channel/{channelId}/send/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SendMessage(
        [FromRoute] string channelId,
        [FromBody] NetworkMessage message,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Invalid request", GetModelStateErrors()));
            }

            var result = await _networkSecurityService.SendMessageAsync(channelId, message, blockchainType);

            return Ok(CreateSuccessResponse(result, "Message sent successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send message");
            return StatusCode(500, CreateErrorResponse("Failed to send message", ex.Message));
        }
    }

    /// <summary>
    /// Configures firewall rules for the enclave.
    /// </summary>
    /// <param name="rules">The firewall rule set.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The configuration result.</returns>
    [HttpPut("firewall/rules/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<FirewallConfigurationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ConfigureFirewall(
        [FromBody] FirewallRuleSet rules,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Invalid request", GetModelStateErrors()));
            }

            var result = await _networkSecurityService.ConfigureFirewallAsync(rules, blockchainType);

            return Ok(CreateSuccessResponse(result, "Firewall rules configured successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure firewall");
            return StatusCode(500, CreateErrorResponse("Failed to configure firewall", ex.Message));
        }
    }

    /// <summary>
    /// Monitors network traffic and security events.
    /// </summary>
    /// <param name="startTime">The start time for monitoring.</param>
    /// <param name="endTime">The end time for monitoring.</param>
    /// <param name="channelId">The channel ID to monitor.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The network monitoring data.</returns>
    [HttpGet("monitor/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<NetworkMonitoringData>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> MonitorNetwork(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? channelId,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            var request = new MonitoringRequest
            {
                StartTime = startTime,
                EndTime = endTime,
                ChannelId = channelId
            };

            var result = await _networkSecurityService.MonitorNetworkAsync(request, blockchainType);

            return Ok(CreateSuccessResponse(result, "Network monitoring data retrieved"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to monitor network");
            return StatusCode(500, CreateErrorResponse("Failed to monitor network", ex.Message));
        }
    }

    /// <summary>
    /// Closes a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID to close.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The close result.</returns>
    [HttpDelete("channel/{channelId}/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CloseChannel(
        [FromRoute] string channelId,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            var result = await _networkSecurityService.CloseChannelAsync(channelId, blockchainType);

            if (!result)
            {
                return NotFound(CreateErrorResponse("Channel not found", $"Channel {channelId} does not exist"));
            }

            return Ok(CreateSuccessResponse(result, "Channel closed successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to close channel");
            return StatusCode(500, CreateErrorResponse("Failed to close channel", ex.Message));
        }
    }

    /// <summary>
    /// Gets the status of a secure channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The channel status.</returns>
    [HttpGet("channel/{channelId}/status/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<ChannelStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetChannelStatus(
        [FromRoute] string channelId,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            var result = await _networkSecurityService.GetChannelStatusAsync(channelId, blockchainType);

            return Ok(CreateSuccessResponse(result, "Channel status retrieved"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(CreateErrorResponse("Channel not found", ex.Message));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get channel status");
            return StatusCode(500, CreateErrorResponse("Failed to get channel status", ex.Message));
        }
    }
}
