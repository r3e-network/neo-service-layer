using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            var result = await _notificationService.SendNotificationAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        try
        {
            var result = await _notificationService.BroadcastNotificationAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] NotificationSubscriptionRequest request)
    {
        try
        {
            var result = await _notificationService.SubscribeAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to notifications");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("unsubscribe/{subscriptionId}")]
    public async Task<IActionResult> Unsubscribe(string subscriptionId)
    {
        try
        {
            var request = new UnsubscribeNotificationRequest { SubscriptionId = subscriptionId };
            var result = await _notificationService.UnsubscribeAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from notifications");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("template")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateNotificationTemplateRequest request)
    {
        try
        {
            var result = await _notificationService.CreateTemplateAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification template");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("template/{templateId}")]
    public async Task<IActionResult> UpdateTemplate(string templateId, [FromBody] UpdateNotificationTemplateRequest request)
    {
        try
        {
            request.TemplateId = templateId;
            var result = await _notificationService.UpdateTemplateAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification template");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("template/{templateId}")]
    public async Task<IActionResult> DeleteTemplate(string templateId)
    {
        try
        {
            var request = new DeleteNotificationTemplateRequest { TemplateId = templateId };
            var result = await _notificationService.DeleteTemplateAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification template");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new GetNotificationTemplatesRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _notificationService.GetTemplatesAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification templates");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetNotificationHistory([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new GetNotificationHistoryRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _notificationService.GetNotificationHistoryAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("status/{notificationId}")]
    public async Task<IActionResult> GetNotificationStatus(string notificationId)
    {
        try
        {
            var request = new GetNotificationStatusRequest { NotificationId = notificationId };
            var result = await _notificationService.GetNotificationStatusAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("channel")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateNotificationChannelRequest request)
    {
        try
        {
            var result = await _notificationService.CreateChannelAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification channel");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("channel/{channelId}")]
    public async Task<IActionResult> UpdateChannel(string channelId, [FromBody] UpdateNotificationChannelRequest request)
    {
        try
        {
            request.ChannelId = channelId;
            var result = await _notificationService.UpdateChannelAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification channel");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("channel/{channelId}")]
    public async Task<IActionResult> DeleteChannel(string channelId)
    {
        try
        {
            var request = new DeleteNotificationChannelRequest { ChannelId = channelId };
            var result = await _notificationService.DeleteChannelAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification channel");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("channels")]
    public async Task<IActionResult> GetChannels([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new GetNotificationChannelsRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _notificationService.GetChannelsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification channels");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetNotificationStatistics()
    {
        try
        {
            var request = new NotificationStatisticsRequest();
            var result = await _notificationService.GetNotificationStatisticsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 