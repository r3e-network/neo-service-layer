using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for notification management and delivery.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
[Tags("Notifications")]
public class NotificationController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationController"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger.</param>
    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a notification.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The notification ID.</returns>
    /// <response code="200">Notification sent successfully.</response>
    /// <response code="400">Invalid notification request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> SendNotification(
        [FromBody] NotificationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var notificationId = await _notificationService.SendNotificationAsync(request, blockchain);
            
            _logger.LogInformation("Notification sent with ID: {NotificationId} on {Blockchain}", 
                notificationId, blockchainType);
            
            return Ok(CreateSuccessResponse(notificationId, "Notification sent successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid notification request");
            return BadRequest(CreateErrorResponse($"Invalid notification request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, CreateErrorResponse($"Failed to send notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// Sends a batch of notifications.
    /// </summary>
    /// <param name="request">The batch notification request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The batch result.</returns>
    /// <response code="200">Batch notifications sent successfully.</response>
    /// <response code="400">Invalid batch request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("batch/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<BatchNotificationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> SendBatchNotifications(
        [FromBody] BatchNotificationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (request.Notifications == null || request.Notifications.Count == 0)
            {
                return BadRequest(CreateErrorResponse("No notifications specified"));
            }

            if (request.Notifications.Count > 100)
            {
                return BadRequest(CreateErrorResponse("Too many notifications (max 100)"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _notificationService.SendBatchNotificationsAsync(request, blockchain);
            
            _logger.LogInformation("Batch sent {Count} notifications on {Blockchain}", 
                request.Notifications.Count, blockchainType);
            
            return Ok(CreateSuccessResponse(result, "Batch notifications processed"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid batch notification request");
            return BadRequest(CreateErrorResponse($"Invalid batch request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending batch notifications");
            return StatusCode(500, CreateErrorResponse($"Failed to send batch notifications: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets the status of a notification.
    /// </summary>
    /// <param name="notificationId">The notification ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The notification status.</returns>
    /// <response code="200">Status retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Notification not found.</response>
    [HttpGet("{notificationId}/status/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<NotificationStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetNotificationStatus(
        [FromRoute] string notificationId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var status = await _notificationService.GetNotificationStatusAsync(notificationId, blockchain);
            
            if (status == null)
            {
                return NotFound(CreateErrorResponse($"Notification not found: {notificationId}"));
            }
            
            return Ok(CreateSuccessResponse(status, "Status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification status: {NotificationId}", notificationId);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates a notification subscription.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The subscription ID.</returns>
    /// <response code="200">Subscription created successfully.</response>
    /// <response code="400">Invalid subscription request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("subscriptions/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] NotificationSubscriptionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var subscriptionId = await _notificationService.CreateSubscriptionAsync(request, blockchain);
            
            _logger.LogInformation("Notification subscription created with ID: {SubscriptionId} on {Blockchain}", 
                subscriptionId, blockchainType);
            
            return Ok(CreateSuccessResponse(subscriptionId, "Subscription created successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription request");
            return BadRequest(CreateErrorResponse($"Invalid subscription request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, CreateErrorResponse($"Failed to create subscription: {ex.Message}"));
        }
    }

    /// <summary>
    /// Updates a notification subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="update">The update request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Subscription updated successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    [HttpPut("subscriptions/{subscriptionId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateSubscription(
        [FromRoute] string subscriptionId,
        [FromBody] NotificationSubscriptionUpdate update,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var success = await _notificationService.UpdateSubscriptionAsync(subscriptionId, update, blockchain);
            
            if (!success)
            {
                return NotFound(CreateErrorResponse($"Subscription not found: {subscriptionId}"));
            }
            
            _logger.LogInformation("Subscription updated: {SubscriptionId} on {Blockchain}", 
                subscriptionId, blockchainType);
            
            return Ok(CreateSuccessResponse(success, "Subscription updated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update request for subscription: {SubscriptionId}", subscriptionId);
            return BadRequest(CreateErrorResponse($"Invalid update request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription: {SubscriptionId}", subscriptionId);
            return StatusCode(500, CreateErrorResponse($"Failed to update subscription: {ex.Message}"));
        }
    }

    /// <summary>
    /// Cancels a notification subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Subscription cancelled successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    [HttpDelete("subscriptions/{subscriptionId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CancelSubscription(
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
            var success = await _notificationService.CancelSubscriptionAsync(subscriptionId, blockchain);
            
            if (!success)
            {
                return NotFound(CreateErrorResponse($"Subscription not found: {subscriptionId}"));
            }
            
            _logger.LogInformation("Subscription cancelled: {SubscriptionId} on {Blockchain}", 
                subscriptionId, blockchainType);
            
            return Ok(CreateSuccessResponse(success, "Subscription cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription: {SubscriptionId}", subscriptionId);
            return StatusCode(500, CreateErrorResponse($"Failed to cancel subscription: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lists notification templates.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="skip">Number of items to skip (for pagination).</param>
    /// <param name="take">Number of items to take (for pagination).</param>
    /// <returns>List of templates.</returns>
    /// <response code="200">Templates retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("templates/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<NotificationTemplate>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListTemplates(
        [FromRoute] string blockchainType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (skip < 0 || take <= 0 || take > 100)
            {
                return BadRequest(CreateErrorResponse("Invalid pagination parameters"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var templates = await _notificationService.GetTemplatesAsync(blockchain);
            var paginatedTemplates = templates.Skip(skip).Take(take).ToList();
            
            var response = new PaginatedResponse<NotificationTemplate>
            {
                Items = paginatedTemplates,
                TotalCount = templates.Count(),
                Skip = skip,
                Take = take,
                HasMore = templates.Count() > skip + take
            };
            
            return Ok(CreateSuccessResponse(response, "Templates retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing templates");
            return StatusCode(500, CreateErrorResponse($"Failed to list templates: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates a notification template.
    /// </summary>
    /// <param name="request">The template creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The template ID.</returns>
    /// <response code="200">Template created successfully.</response>
    /// <response code="400">Invalid template.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("templates/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] NotificationTemplateRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var templateId = await _notificationService.CreateTemplateAsync(request, blockchain);
            
            _logger.LogInformation("Template created with ID: {TemplateId} on {Blockchain}", 
                templateId, blockchainType);
            
            return Ok(CreateSuccessResponse(templateId, "Template created successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid template request");
            return BadRequest(CreateErrorResponse($"Invalid template: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, CreateErrorResponse($"Failed to create template: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets notification history for the authenticated user.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="skip">Number of items to skip (for pagination).</param>
    /// <param name="take">Number of items to take (for pagination).</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>Notification history.</returns>
    /// <response code="200">History retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("history/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<NotificationHistoryItem>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetNotificationHistory(
        [FromRoute] string blockchainType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (skip < 0 || take <= 0 || take > 100)
            {
                return BadRequest(CreateErrorResponse("Invalid pagination parameters"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var userId = GetUserIdentifier();
            
            var history = await _notificationService.GetHistoryAsync(userId, blockchain, status);
            var paginatedHistory = history.Skip(skip).Take(take).ToList();
            
            var response = new PaginatedResponse<NotificationHistoryItem>
            {
                Items = paginatedHistory,
                TotalCount = history.Count(),
                Skip = skip,
                Take = take,
                HasMore = history.Count() > skip + take
            };
            
            return Ok(CreateSuccessResponse(response, "History retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification history");
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve history: {ex.Message}"));
        }
    }
}