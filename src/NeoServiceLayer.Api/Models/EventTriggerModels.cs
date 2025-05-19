using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NeoServiceLayer.Api.Models
{
    /// <summary>
    /// Request model for creating an event trigger.
    /// </summary>
    public class CreateEventTriggerRequest
    {
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
    }

    /// <summary>
    /// Request model for updating an event trigger.
    /// </summary>
    public class UpdateEventTriggerRequest
    {
        /// <summary>
        /// Gets or sets the ID of the JavaScript function to execute when the event occurs.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the type of event to listen for.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the filters to apply to the event.
        /// </summary>
        public JsonDocument Filters { get; set; }

        /// <summary>
        /// Gets or sets the mapping of function input parameters to event data.
        /// Uses JSONPath expressions to extract data from the event.
        /// </summary>
        public Dictionary<string, string> InputMapping { get; set; }

        /// <summary>
        /// Gets or sets the status of the event trigger.
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// Response model for an event trigger.
    /// </summary>
    public class EventTriggerResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the event trigger.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the JavaScript function to execute when the event occurs.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the type of event to listen for.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the filters to apply to the event.
        /// </summary>
        public JsonDocument Filters { get; set; }

        /// <summary>
        /// Gets or sets the mapping of function input parameters to event data.
        /// Uses JSONPath expressions to extract data from the event.
        /// </summary>
        public Dictionary<string, string> InputMapping { get; set; }

        /// <summary>
        /// Gets or sets the status of the event trigger.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response model for a list of event triggers.
    /// </summary>
    public class EventTriggerListResponse
    {
        /// <summary>
        /// Gets or sets the list of event triggers.
        /// </summary>
        public List<EventTriggerResponse> Triggers { get; set; }

        /// <summary>
        /// Gets or sets the total count of event triggers.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }
    }
}
