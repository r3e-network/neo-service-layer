using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.EventSubscription;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for event subscription operations.
/// </summary>
[Tags("Event Subscription")]
public class EventSubscriptionController : BaseApiController
{
    private readonly IEventSubscriptionService _eventSubscriptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSubscriptionController"/> class.
    /// </summary>
    /// <param name="eventSubscriptionService">The event subscription service.</param>
    /// <param name="logger">The logger.</param>
    public EventSubscriptionController(
        IEventSubscriptionService eventSubscriptionService,
        ILogger<EventSubscriptionController> logger) : base(logger)
    {
        _eventSubscriptionService = eventSubscriptionService;
    }

    /// <summary>
    /// Creates a new event subscription.
    /// </summary>
    /// <param name="subscription">The event subscription to create.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The subscription ID.</returns>
    /// <response code="200">Subscription created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Subscription creation failed.</response>
    [HttpPost("create/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] EventSubscription subscription,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var subscriptionId = await _eventSubscriptionService.CreateSubscriptionAsync(subscription, blockchain);

            Logger.LogInformation("Created event subscription {SubscriptionId} for {EventType} on {BlockchainType} by user {UserId}",
                subscriptionId, subscription.EventType, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(subscriptionId, "Event subscription created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateSubscription");
        }
    }

    /// <summary>
    /// Gets an event subscription by ID.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The event subscription.</returns>
    /// <response code="200">Subscription retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    [HttpGet("{subscriptionId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<EventSubscription>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
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
            var subscription = await _eventSubscriptionService.GetSubscriptionAsync(subscriptionId, blockchain);

            Logger.LogInformation("Retrieved event subscription {SubscriptionId} on {BlockchainType} by user {UserId}",
                subscriptionId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(subscription, "Event subscription retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetSubscription");
        }
    }

    /// <summary>
    /// Updates an existing event subscription.
    /// </summary>
    /// <param name="subscription">The updated event subscription.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The update result.</returns>
    /// <response code="200">Subscription updated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    /// <response code="500">Subscription update failed.</response>
    [HttpPut("update/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> UpdateSubscription(
        [FromBody] EventSubscription subscription,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _eventSubscriptionService.UpdateSubscriptionAsync(subscription, blockchain);

            Logger.LogInformation("Updated event subscription {SubscriptionId} on {BlockchainType} by user {UserId}",
                subscription.SubscriptionId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Event subscription updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UpdateSubscription");
        }
    }

    /// <summary>
    /// Deletes an event subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The deletion result.</returns>
    /// <response code="200">Subscription deleted successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    [HttpDelete("{subscriptionId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteSubscription(
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

            Logger.LogInformation("Deleted event subscription {SubscriptionId} on {BlockchainType} by user {UserId}",
                subscriptionId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Event subscription deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "DeleteSubscription");
        }
    }

    /// <summary>
    /// Lists event subscriptions with pagination.
    /// </summary>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The number of items to take.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The list of subscriptions.</returns>
    /// <response code="200">Subscriptions retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("list/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventSubscription>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListSubscriptions(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromRoute] string blockchainType = "NeoN3")
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (take > 1000)
            {
                return BadRequest(CreateErrorResponse("Take parameter cannot exceed 1000"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var subscriptions = await _eventSubscriptionService.ListSubscriptionsAsync(skip, take, blockchain);

            Logger.LogInformation("Listed {SubscriptionCount} event subscriptions on {BlockchainType} by user {UserId}",
                subscriptions.Count(), blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(subscriptions, "Event subscriptions retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ListSubscriptions");
        }
    }

    /// <summary>
    /// Gets events for a specific subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="skip">The number of events to skip.</param>
    /// <param name="take">The number of events to take.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The list of events.</returns>
    /// <response code="200">Events retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    [HttpGet("{subscriptionId}/events/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventData>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetEvents(
        [FromRoute] string subscriptionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromRoute] string blockchainType = "NeoN3")
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (take > 1000)
            {
                return BadRequest(CreateErrorResponse("Take parameter cannot exceed 1000"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var events = await _eventSubscriptionService.GetEventsAsync(subscriptionId, skip, take, blockchain);

            Logger.LogInformation("Retrieved {EventCount} events for subscription {SubscriptionId} on {BlockchainType} by user {UserId}",
                events.Count(), subscriptionId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(events, "Events retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetEvents");
        }
    }

    /// <summary>
    /// Acknowledges an event.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="eventId">The event ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The acknowledgment result.</returns>
    /// <response code="200">Event acknowledged successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("{subscriptionId}/events/{eventId}/acknowledge/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> AcknowledgeEvent(
        [FromRoute] string subscriptionId,
        [FromRoute] string eventId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _eventSubscriptionService.AcknowledgeEventAsync(subscriptionId, eventId, blockchain);

            Logger.LogInformation("Acknowledged event {EventId} for subscription {SubscriptionId} on {BlockchainType} by user {UserId}",
                eventId, subscriptionId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Event acknowledged successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AcknowledgeEvent");
        }
    }

    /// <summary>
    /// Triggers a test event for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="eventData">The test event data.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The test event ID.</returns>
    /// <response code="200">Test event triggered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    /// <response code="500">Test event failed.</response>
    [HttpPost("{subscriptionId}/test/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> TriggerTestEvent(
        [FromRoute] string subscriptionId,
        [FromBody] EventData eventData,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var testEventId = await _eventSubscriptionService.TriggerTestEventAsync(subscriptionId, eventData, blockchain);

            Logger.LogInformation("Triggered test event {TestEventId} for subscription {SubscriptionId} on {BlockchainType} by user {UserId}",
                testEventId, subscriptionId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(testEventId, "Test event triggered successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "TriggerTestEvent");
        }
    }
} 