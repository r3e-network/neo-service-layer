using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents an event trigger that can execute a JavaScript function when a specific event occurs.
    /// </summary>
    public class EventTrigger
    {
        /// <summary>
        /// Gets or sets the unique identifier for the event trigger.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the JavaScript function to execute when the event occurs.
        /// </summary>
        [Required]
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the type of event to listen for.
        /// </summary>
        [Required]
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the filters to apply to the event.
        /// </summary>
        public JsonDocument Filters { get; set; }

        /// <summary>
        /// Gets or sets the mapping of function input parameters to event data.
        /// Uses JSONPath expressions to extract data from the event.
        /// </summary>
        public Dictionary<string, string> InputMapping { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the status of the event trigger.
        /// </summary>
        public EventTriggerStatus Status { get; set; } = EventTriggerStatus.Active;

        /// <summary>
        /// Gets or sets the owner of the event trigger.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents the status of an event trigger.
    /// </summary>
    public enum EventTriggerStatus
    {
        /// <summary>
        /// The event trigger is active and will execute the function when the event occurs.
        /// </summary>
        Active,

        /// <summary>
        /// The event trigger is inactive and will not execute the function when the event occurs.
        /// </summary>
        Inactive,

        /// <summary>
        /// The event trigger is paused due to errors.
        /// </summary>
        Paused,

        /// <summary>
        /// The event trigger has been deleted.
        /// </summary>
        Deleted
    }
}
