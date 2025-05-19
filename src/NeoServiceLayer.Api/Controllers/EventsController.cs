using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing event subscriptions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        /// <summary>
        /// Initializes a new instance of the EventsController class.
        /// </summary>
        /// <param name="eventService">The event service.</param>
        /// <param name="logger">The logger.</param>
        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new subscription.
        /// </summary>
        /// <param name="request">The request to create a subscription.</param>
        /// <returns>The created subscription.</returns>
        [HttpPost("subscriptions")]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 201)]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 400)]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 401)]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 500)]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                _logger.LogInformation("Creating subscription for event type {EventType}", request.EventType);

                var subscription = new Subscription
                {
                    EventType = request.EventType,
                    EventFilter = request.EventFilter,
                    CallbackUrl = request.CallbackUrl,
                    UserId = User.Identity.Name
                };

                var createdSubscription = await _eventService.CreateSubscriptionAsync(subscription);

                _logger.LogInformation("Subscription {SubscriptionId} created successfully", createdSubscription.Id);

                return CreatedAtAction(nameof(GetSubscription), new { subscriptionId = createdSubscription.Id }, ApiResponse<Subscription>.CreateSuccess(createdSubscription));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return StatusCode(500, ApiResponse<Subscription>.CreateError(ApiErrorCodes.InternalError, "An error occurred while creating the subscription."));
            }
        }

        /// <summary>
        /// Gets a subscription by ID.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription to get.</param>
        /// <returns>The subscription with the specified ID.</returns>
        [HttpGet("subscriptions/{subscriptionId}")]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 200)]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 401)]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 404)]
        [ProducesResponseType(typeof(ApiResponse<Subscription>), 500)]
        public async Task<IActionResult> GetSubscription(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Getting subscription {SubscriptionId}", subscriptionId);

                var subscription = await _eventService.GetSubscriptionAsync(subscriptionId);

                if (subscription == null)
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                    return NotFound(ApiResponse<Subscription>.CreateError(ApiErrorCodes.ResourceNotFound, $"Subscription with ID {subscriptionId} not found."));
                }

                // Check if the subscription belongs to the authenticated user
                if (subscription.UserId != User.Identity.Name)
                {
                    _logger.LogWarning("User {UserId} attempted to access subscription {SubscriptionId} belonging to user {SubscriptionUserId}", User.Identity.Name, subscriptionId, subscription.UserId);
                    return Unauthorized(ApiResponse<Subscription>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to access this subscription."));
                }

                _logger.LogInformation("Subscription {SubscriptionId} retrieved successfully", subscriptionId);

                return Ok(ApiResponse<Subscription>.CreateSuccess(subscription));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, ApiResponse<Subscription>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the subscription."));
            }
        }

        /// <summary>
        /// Gets all subscriptions for the authenticated user.
        /// </summary>
        /// <param name="eventType">Optional event type filter.</param>
        /// <returns>A list of subscriptions for the authenticated user.</returns>
        [HttpGet("subscriptions")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Subscription>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Subscription>>), 401)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Subscription>>), 500)]
        public async Task<IActionResult> GetSubscriptions([FromQuery] EventType? eventType = null)
        {
            try
            {
                _logger.LogInformation("Getting subscriptions for user {UserId} with event type {EventType}", User.Identity.Name, eventType);

                var subscriptions = await _eventService.GetSubscriptionsAsync(User.Identity.Name, eventType);

                _logger.LogInformation("Retrieved subscriptions for user {UserId}", User.Identity.Name);

                return Ok(ApiResponse<IEnumerable<Subscription>>.CreateSuccess(subscriptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for user {UserId}", User.Identity.Name);
                return StatusCode(500, ApiResponse<IEnumerable<Subscription>>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the subscriptions."));
            }
        }

        /// <summary>
        /// Deletes a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription to delete.</param>
        /// <returns>A success response if the subscription was deleted.</returns>
        [HttpDelete("subscriptions/{subscriptionId}")]
        [ProducesResponseType(typeof(ApiResponse<DeleteSubscriptionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<DeleteSubscriptionResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<DeleteSubscriptionResponse>), 404)]
        [ProducesResponseType(typeof(ApiResponse<DeleteSubscriptionResponse>), 500)]
        public async Task<IActionResult> DeleteSubscription(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Deleting subscription {SubscriptionId}", subscriptionId);

                var subscription = await _eventService.GetSubscriptionAsync(subscriptionId);

                if (subscription == null)
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                    return NotFound(ApiResponse<DeleteSubscriptionResponse>.CreateError(ApiErrorCodes.ResourceNotFound, $"Subscription with ID {subscriptionId} not found."));
                }

                // Check if the subscription belongs to the authenticated user
                if (subscription.UserId != User.Identity.Name)
                {
                    _logger.LogWarning("User {UserId} attempted to delete subscription {SubscriptionId} belonging to user {SubscriptionUserId}", User.Identity.Name, subscriptionId, subscription.UserId);
                    return Unauthorized(ApiResponse<DeleteSubscriptionResponse>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to delete this subscription."));
                }

                var deleted = await _eventService.DeleteSubscriptionAsync(subscriptionId);

                if (!deleted)
                {
                    _logger.LogWarning("Failed to delete subscription {SubscriptionId}", subscriptionId);
                    return StatusCode(500, ApiResponse<DeleteSubscriptionResponse>.CreateError(ApiErrorCodes.InternalError, "Failed to delete the subscription."));
                }

                _logger.LogInformation("Subscription {SubscriptionId} deleted successfully", subscriptionId);

                var response = new DeleteSubscriptionResponse
                {
                    Deleted = true
                };

                return Ok(ApiResponse<DeleteSubscriptionResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, ApiResponse<DeleteSubscriptionResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while deleting the subscription."));
            }
        }

        /// <summary>
        /// Gets all events for the authenticated user.
        /// </summary>
        /// <param name="eventType">Optional event type filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="limit">The page size.</param>
        /// <returns>A list of events for the authenticated user.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<Event>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<Event>>), 401)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<Event>>), 500)]
        public async Task<IActionResult> GetEvents([FromQuery] EventType? eventType = null, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            try
            {
                _logger.LogInformation("Getting events for user {UserId} with event type {EventType}, page {Page}, limit {Limit}", User.Identity.Name, eventType, page, limit);

                var (events, totalCount) = await _eventService.GetEventsAsync(User.Identity.Name, eventType, page, limit);

                var result = PaginatedResult<Event>.Create(events, totalCount, page, limit);

                _logger.LogInformation("Retrieved {Count} events for user {UserId}", result.Items, User.Identity.Name);

                return Ok(ApiResponse<PaginatedResult<Event>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for user {UserId}", User.Identity.Name);
                return StatusCode(500, ApiResponse<PaginatedResult<Event>>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the events."));
            }
        }
    }

    /// <summary>
    /// Represents a request to create a subscription.
    /// </summary>
    public class CreateSubscriptionRequest
    {
        /// <summary>
        /// Gets or sets the type of the event to subscribe to.
        /// </summary>
        public EventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the filter for the event.
        /// </summary>
        public Dictionary<string, object> EventFilter { get; set; }

        /// <summary>
        /// Gets or sets the URL to call when an event occurs.
        /// </summary>
        public string CallbackUrl { get; set; }
    }

    /// <summary>
    /// Represents a response to a delete subscription request.
    /// </summary>
    public class DeleteSubscriptionResponse
    {
        /// <summary>
        /// Gets or sets whether the subscription was deleted.
        /// </summary>
        public bool Deleted { get; set; }
    }
}
