using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NeoServiceLayer.Tee.Shared.Events
{
    /// <summary>
    /// Information about an event trigger.
    /// </summary>
    public class EventTriggerInfo
    {
        /// <summary>
        /// Gets or sets the ID of the event trigger.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the event trigger.
        /// </summary>
        public EventTriggerType Type { get; set; }

        /// <summary>
        /// Gets or sets the condition for the event trigger.
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets the ID of the JavaScript function to execute.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who owns the event trigger.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript code to execute.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the input data as a JSON string.
        /// </summary>
        public string InputJson { get; set; }

        /// <summary>
        /// Gets or sets the gas limit for execution.
        /// </summary>
        public ulong GasLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event trigger is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the next execution time for scheduled triggers.
        /// </summary>
        public ulong NextExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds for scheduled triggers.
        /// </summary>
        public ulong IntervalSeconds { get; set; }

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
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last execution timestamp.
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the number of times the event trigger has been executed.
        /// </summary>
        public ulong ExecutionCount { get; set; }

        /// <summary>
        /// Gets or sets the last execution result.
        /// </summary>
        public string LastExecutionResult { get; set; }

        /// <summary>
        /// Gets or sets the last execution error.
        /// </summary>
        public string LastExecutionError { get; set; }

        /// <summary>
        /// Gets or sets the status of the event trigger.
        /// </summary>
        public EventTriggerStatus Status { get; set; } = EventTriggerStatus.Active;

        /// <summary>
        /// Gets or sets the metadata for the event trigger.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the EventTriggerInfo class.
        /// </summary>
        public EventTriggerInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the EventTriggerInfo class.
        /// </summary>
        /// <param name="id">The ID of the event trigger.</param>
        /// <param name="type">The type of the event trigger.</param>
        /// <param name="condition">The condition for the event trigger.</param>
        /// <param name="functionId">The ID of the JavaScript function to execute.</param>
        /// <param name="userId">The ID of the user who owns the event trigger.</param>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="inputJson">The input data as a JSON string.</param>
        /// <param name="gasLimit">The gas limit for execution.</param>
        public EventTriggerInfo(string id, EventTriggerType type, string condition, string functionId, string userId, string code, string inputJson, ulong gasLimit)
        {
            Id = id;
            Type = type;
            Condition = condition;
            FunctionId = functionId;
            UserId = userId;
            Code = code;
            InputJson = inputJson;
            GasLimit = gasLimit;
            Enabled = true;
            NextExecutionTime = 0;
            IntervalSeconds = 0;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ExecutionCount = 0;
            Status = EventTriggerStatus.Active;
        }
    }

    /// <summary>
    /// Types of event triggers.
    /// </summary>
    public enum EventTriggerType
    {
        /// <summary>
        /// Trigger on a schedule.
        /// </summary>
        Schedule,

        /// <summary>
        /// Trigger on a blockchain event.
        /// </summary>
        Blockchain,

        /// <summary>
        /// Trigger on a storage event.
        /// </summary>
        Storage,

        /// <summary>
        /// Trigger on an external event.
        /// </summary>
        External
    }

    /// <summary>
    /// Status of an event trigger.
    /// </summary>
    public enum EventTriggerStatus
    {
        /// <summary>
        /// The event trigger is active.
        /// </summary>
        Active,

        /// <summary>
        /// The event trigger is inactive.
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
