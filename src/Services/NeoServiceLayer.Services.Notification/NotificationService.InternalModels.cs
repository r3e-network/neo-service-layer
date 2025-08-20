using NeoServiceLayer.Services.Notification.Models;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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
/// Internal notification template model.
/// </summary>
internal class InternalNotificationTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[] Variables { get; set; } = Array.Empty<string>();
    public NotificationChannel[] SupportedChannels { get; set; } = Array.Empty<NotificationChannel>();
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Notification subscription.
/// </summary>
public class NotificationSubscription
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public NotificationChannel[] Channels { get; set; } = Array.Empty<NotificationChannel>();
    public Dictionary<string, object> Preferences { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the subscription ID (alias for SubscriptionId).
    /// </summary>
    public string Id { get => SubscriptionId; set => SubscriptionId = value; }

    /// <summary>
    /// Gets or sets the recipient (alias for SubscriberId).
    /// </summary>
    public string Recipient { get => SubscriberId; set => SubscriberId = value; }

    /// <summary>
    /// Gets or sets the notification categories.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the notification channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets the event types to subscribe to.
    /// </summary>
    public string[] EventTypes { get; set; } = Array.Empty<string>();
}
