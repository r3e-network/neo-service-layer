using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Events
{
    /// <summary>
    /// Interface for an event trigger manager that manages event triggers.
    /// </summary>
    public interface IEventTriggerManager : IDisposable
    {
        /// <summary>
        /// Initializes the event trigger manager.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Registers an event trigger.
        /// </summary>
        /// <param name="trigger">The event trigger to register.</param>
        /// <returns>True if registration was successful, false otherwise.</returns>
        Task<bool> RegisterTriggerAsync(EventTriggerInfo trigger);

        /// <summary>
        /// Unregisters an event trigger.
        /// </summary>
        /// <param name="triggerId">The ID of the event trigger to unregister.</param>
        /// <returns>True if unregistration was successful, false otherwise.</returns>
        Task<bool> UnregisterTriggerAsync(string triggerId);

        /// <summary>
        /// Gets an event trigger by ID.
        /// </summary>
        /// <param name="triggerId">The ID of the event trigger to get.</param>
        /// <returns>The event trigger, or null if not found.</returns>
        Task<EventTriggerInfo> GetTriggerAsync(string triggerId);

        /// <summary>
        /// Lists all event triggers.
        /// </summary>
        /// <returns>A list of all event triggers.</returns>
        Task<IReadOnlyList<EventTriggerInfo>> ListTriggersAsync();

        /// <summary>
        /// Lists all event triggers of a specific type.
        /// </summary>
        /// <param name="triggerType">The type of event triggers to list.</param>
        /// <returns>A list of event triggers of the specified type.</returns>
        Task<IReadOnlyList<EventTriggerInfo>> ListTriggersByTypeAsync(EventTriggerType triggerType);

        /// <summary>
        /// Enables an event trigger.
        /// </summary>
        /// <param name="triggerId">The ID of the event trigger to enable.</param>
        /// <returns>True if the event trigger was enabled successfully, false otherwise.</returns>
        Task<bool> EnableTriggerAsync(string triggerId);

        /// <summary>
        /// Disables an event trigger.
        /// </summary>
        /// <param name="triggerId">The ID of the event trigger to disable.</param>
        /// <returns>True if the event trigger was disabled successfully, false otherwise.</returns>
        Task<bool> DisableTriggerAsync(string triggerId);

        /// <summary>
        /// Processes scheduled triggers.
        /// </summary>
        /// <param name="currentTime">The current time in seconds since epoch.</param>
        /// <returns>The number of triggers processed.</returns>
        Task<int> ProcessScheduledTriggersAsync(ulong currentTime);

        /// <summary>
        /// Processes a blockchain event.
        /// </summary>
        /// <param name="eventData">The event data as a JSON string.</param>
        /// <returns>The number of triggers processed.</returns>
        Task<int> ProcessBlockchainEventAsync(string eventData);

        /// <summary>
        /// Processes a storage event.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="operation">The storage operation.</param>
        /// <returns>The number of triggers processed.</returns>
        Task<int> ProcessStorageEventAsync(string key, string operation);

        /// <summary>
        /// Processes an external event.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="eventData">The event data as a JSON string.</param>
        /// <returns>The number of triggers processed.</returns>
        Task<int> ProcessExternalEventAsync(string eventType, string eventData);

        /// <summary>
        /// Executes an event trigger.
        /// </summary>
        /// <param name="triggerId">The ID of the event trigger to execute.</param>
        /// <param name="eventData">The event data as a JSON string.</param>
        /// <returns>The result of the execution as a JSON string.</returns>
        Task<string> ExecuteTriggerAsync(string triggerId, string eventData);
    }
}
