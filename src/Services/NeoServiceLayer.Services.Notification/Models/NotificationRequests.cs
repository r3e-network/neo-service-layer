using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Notification.Models
{
    /// <summary>
    /// Request to subscribe to notifications
    /// </summary>
    public class NotificationSubscriptionRequest
    {
        [Required]
        public string SubscriberId { get; set; } = string.Empty;
        
        [Required]
        public string Channel { get; set; } = string.Empty;
        
        [Required]
        public List<string> EventTypes { get; set; } = new();
        
        public Dictionary<string, string>? Filters { get; set; }
        
        public string? CallbackUrl { get; set; }
        
        public bool Active { get; set; } = true;
        
        public Dictionary<string, object>? Metadata { get; set; }
    }
}