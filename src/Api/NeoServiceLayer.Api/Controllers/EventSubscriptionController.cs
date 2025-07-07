using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.EventSubscription;
using NeoServiceLayer.Services.EventSubscription.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for event subscription management.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/events")]
[Authorize]
[Tags("Event Subscription")]
public class EventSubscriptionController : BaseApiController
{
    private readonly IEventSubscriptionService _eventSubscriptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSubscriptionController"/> class.
    /// </summary>
    /// <param name="eventSubscriptionService">The event subscription service.</param>
    /// <param name="logger">The logger.</param>
    public EventSubscriptionController(IEventSubscriptionService eventSubscriptionService, ILogger<EventSubscriptionController> logger)
        : base(logger)
    {
        _eventSubscriptionService = eventSubscriptionService ?? throw new ArgumentNullException(nameof(eventSubscriptionService));
    }

    /// <summary>
    /// Subscribes to blockchain events.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The subscription ID.</returns>
    [HttpPost("{blockchainType}/subscribe")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> Subscribe(
        [FromBody] EventSubscriptionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            // Convert request to EventSubscription
            var subscription = new EventSubscription
            {
                Name = request.EventType ?? "Event Subscription",
                EventType = request.EventType ?? string.Empty,
                CallbackUrl = request.CallbackUrl ?? string.Empty,
                Enabled = true
            };
            var subscriptionId = await _eventSubscriptionService.CreateSubscriptionAsync(subscription, blockchain);

            return Ok(CreateResponse(subscriptionId, "Subscription created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating subscription");
        }
    }

    /// <summary>
    /// Unsubscribes from events.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    [HttpDelete("{blockchainType}/{subscriptionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> Unsubscribe(
        [FromRoute] string subscriptionId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _eventSubscriptionService.DeleteSubscriptionAsync(subscriptionId, blockchain);

            return Ok(CreateResponse(result, "Unsubscribed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "unsubscribing");
        }
    }

    /// <summary>
    /// Gets subscription details.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The subscription details.</returns>
    [HttpGet("{blockchainType}/{subscriptionId}")]
    [ProducesResponseType(typeof(ApiResponse<EventSubscriptionDetails>), 200)]
    public async Task<IActionResult> GetSubscription(
        [FromRoute] string subscriptionId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _eventSubscriptionService.GetSubscriptionAsync(subscriptionId, blockchain);

            return Ok(CreateResponse(result, "Subscription details retrieved"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving subscription");
        }
    }
}
