using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents an event in the Neo Service Layer.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Gets or sets the unique identifier of the event.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public EventType Type { get; set; }

        /// <summary>
        /// Gets or sets the source of the event.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the data associated with the event.
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Gets or sets the time when the event occurred.
        /// </summary>
        public DateTime OccurredAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the event was processed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the event.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Creates a new instance of the Event class.
        /// </summary>
        public Event()
        {
            Id = Guid.NewGuid().ToString();
            Data = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
            OccurredAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Represents the type of an event.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EventType
    {
        /// <summary>
        /// Blockchain event.
        /// </summary>
        BlockchainEvent,

        /// <summary>
        /// Task event.
        /// </summary>
        TaskEvent,

        /// <summary>
        /// System event.
        /// </summary>
        SystemEvent,

        /// <summary>
        /// User event.
        /// </summary>
        UserEvent
    }
}
