using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents an event subscription in the Neo Service Layer.
    /// </summary>
    public class Subscription
    {
        /// <summary>
        /// Gets or sets the unique identifier of the subscription.
        /// </summary>
        public string Id { get; set; }

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

        /// <summary>
        /// Gets or sets the status of the subscription.
        /// </summary>
        public SubscriptionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created the subscription.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the time when the subscription was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the subscription expires.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the subscription.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Creates a new instance of the Subscription class.
        /// </summary>
        public Subscription()
        {
            Id = Guid.NewGuid().ToString();
            EventFilter = new Dictionary<string, object>();
            Status = SubscriptionStatus.Active;
            Metadata = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Represents the status of a subscription.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SubscriptionStatus
    {
        /// <summary>
        /// The subscription is active.
        /// </summary>
        Active,

        /// <summary>
        /// The subscription is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// The subscription is expired.
        /// </summary>
        Expired,

        /// <summary>
        /// The subscription is cancelled.
        /// </summary>
        Cancelled
    }
}
