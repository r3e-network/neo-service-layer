using NeoServiceLayer.Services.Notification.Models;
using NeoServiceLayer.Core;

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

/// <summary>
/// Notification subscription.
/// </summary>
internal class NotificationSubscription
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = Array.Empty<string>();
    public NotificationChannel[] PreferredChannels { get; set; } = Array.Empty<NotificationChannel>();
    public NotificationPreferences Preferences { get; set; } = new();
    public Dictionary<string, object> Filters { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
