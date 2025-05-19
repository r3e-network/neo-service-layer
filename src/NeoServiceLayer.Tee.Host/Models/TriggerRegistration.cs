using System;

namespace NeoServiceLayer.Tee.Host.Models
{
    /// <summary>
    /// Represents a registration of a trigger for an event.
    /// </summary>
    public class TriggerRegistration
    {
        /// <summary>
        /// Gets or sets the unique identifier for the trigger.
        /// </summary>
        public string TriggerId { get; set; }

        /// <summary>
        /// Gets or sets the type of event this trigger responds to.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the function to execute when the trigger fires.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who owns this trigger.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the condition that must be met for the trigger to fire.
        /// If this is null or empty, the trigger will fire for all events of the specified type.
        /// For complex conditions, this can be a JavaScript expression prefixed with "js:".
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets the UNIX timestamp (in seconds) when this trigger was created.
        /// </summary>
        public long CreatedTimestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this trigger is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of times this trigger can fire.
        /// A value of 0 means no limit.
        /// </summary>
        public int MaxInvocations { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of times this trigger has been invoked.
        /// </summary>
        public int InvocationCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the cooldown period (in seconds) between invocations of this trigger.
        /// A value of 0 means no cooldown.
        /// </summary>
        public int CooldownSeconds { get; set; } = 0;

        /// <summary>
        /// Gets or sets the UNIX timestamp (in seconds) of the last invocation of this trigger.
        /// </summary>
        public long LastInvocationTimestamp { get; set; } = 0;

        /// <summary>
        /// Gets or sets the custom metadata for this trigger.
        /// </summary>
        public string Metadata { get; set; }
    }
} 