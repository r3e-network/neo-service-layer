using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification.Models;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Services.Notification;

/// <summary>
/// Delivery simulation result.
/// </summary>
internal class DeliverySimulationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int DeliveryTimeMs { get; set; }
}

/// <summary>
/// Notification template.
/// </summary>
internal class NotificationTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[] Variables { get; set; } = Array.Empty<string>();
    public NotificationChannel[] SupportedChannels { get; set; } = Array.Empty<NotificationChannel>();
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
