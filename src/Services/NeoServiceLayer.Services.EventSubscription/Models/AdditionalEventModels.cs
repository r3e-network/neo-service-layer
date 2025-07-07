using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.EventSubscription.Models;

/// <summary>
/// Request to create an event subscription
/// </summary>
public class EventSubscriptionRequest
{
    public string SubscriptionName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public List<string> EventSignatures { get; set; } = new();
    public string CallbackUrl { get; set; } = string.Empty;
    public Dictionary<string, object> FilterCriteria { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Event subscription details
/// </summary>
public class EventSubscriptionDetails
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastTriggered { get; set; }
    public int EventCount { get; set; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}
