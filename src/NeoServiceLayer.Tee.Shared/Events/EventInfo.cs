using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NeoServiceLayer.Tee.Shared.Events
{
    /// <summary>
    /// Information about an event.
    /// </summary>
    public class EventInfo
    {
        /// <summary>
        /// Gets or sets the ID of the event.
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
        /// Gets or sets the data of the event.
        /// </summary>
        public JsonDocument Data { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who triggered the event.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the event.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the time when the event was processed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of triggers executed for this event.
        /// </summary>
        public int TriggersExecuted { get; set; }

        /// <summary>
        /// Gets or sets the status of the event.
        /// </summary>
        public EventStatus Status { get; set; } = EventStatus.Pending;

        /// <summary>
        /// Initializes a new instance of the EventInfo class.
        /// </summary>
        public EventInfo()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the EventInfo class.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="source">The source of the event.</param>
        /// <param name="data">The data of the event.</param>
        /// <param name="userId">The ID of the user who triggered the event.</param>
        public EventInfo(EventType type, string source, JsonDocument data, string userId = null)
        {
            Id = Guid.NewGuid().ToString();
            Type = type;
            Source = source;
            Data = data;
            Timestamp = DateTime.UtcNow;
            UserId = userId;
            Status = EventStatus.Pending;
        }

        /// <summary>
        /// Initializes a new instance of the EventInfo class.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="source">The source of the event.</param>
        /// <param name="data">The data of the event as a JSON string.</param>
        /// <param name="userId">The ID of the user who triggered the event.</param>
        public EventInfo(EventType type, string source, string data, string userId = null)
        {
            Id = Guid.NewGuid().ToString();
            Type = type;
            Source = source;
            Data = JsonDocument.Parse(data);
            Timestamp = DateTime.UtcNow;
            UserId = userId;
            Status = EventStatus.Pending;
        }

        /// <summary>
        /// Gets the data of the event as a JSON string.
        /// </summary>
        /// <returns>The data of the event as a JSON string.</returns>
        public string GetDataAsString()
        {
            if (Data == null)
            {
                return "{}";
            }

            using (var stream = new System.IO.MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    Data.WriteTo(writer);
                }

                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// Sets the event as processed.
        /// </summary>
        /// <param name="triggersExecuted">The number of triggers executed for this event.</param>
        public void SetProcessed(int triggersExecuted)
        {
            ProcessedAt = DateTime.UtcNow;
            TriggersExecuted = triggersExecuted;
            Status = EventStatus.Processed;
        }

        /// <summary>
        /// Sets the event as failed.
        /// </summary>
        /// <param name="error">The error message.</param>
        public void SetFailed(string error)
        {
            ProcessedAt = DateTime.UtcNow;
            Metadata["error"] = error;
            Status = EventStatus.Failed;
        }
    }

    /// <summary>
    /// Types of events.
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Blockchain event.
        /// </summary>
        Blockchain,

        /// <summary>
        /// Storage event.
        /// </summary>
        Storage,

        /// <summary>
        /// Schedule event.
        /// </summary>
        Schedule,

        /// <summary>
        /// External event.
        /// </summary>
        External,

        /// <summary>
        /// System event.
        /// </summary>
        System,

        /// <summary>
        /// User event.
        /// </summary>
        User
    }

    /// <summary>
    /// Status of an event.
    /// </summary>
    public enum EventStatus
    {
        /// <summary>
        /// The event is pending processing.
        /// </summary>
        Pending,

        /// <summary>
        /// The event is being processed.
        /// </summary>
        Processing,

        /// <summary>
        /// The event has been processed.
        /// </summary>
        Processed,

        /// <summary>
        /// The event processing failed.
        /// </summary>
        Failed
    }
}
