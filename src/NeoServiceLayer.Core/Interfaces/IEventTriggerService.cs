using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;
using System.Text.Json;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for managing event triggers.
    /// </summary>
    public interface IEventTriggerService
    {
        /// <summary>
        /// Creates a new event trigger.
        /// </summary>
        /// <param name="trigger">The event trigger to create.</param>
        /// <returns>The created event trigger.</returns>
        Task<EventTrigger> CreateTriggerAsync(EventTrigger trigger);

        /// <summary>
        /// Gets an event trigger by ID.
        /// </summary>
        /// <param name="triggerId">The ID of the event trigger to get.</param>
        /// <returns>The event trigger.</returns>
        Task<EventTrigger> GetTriggerAsync(string triggerId);

        /// <summary>
        /// Updates an event trigger.
        /// </summary>
        /// <param name="trigger">The event trigger to update.</param>
        /// <returns>The updated event trigger.</returns>
        Task<EventTrigger> UpdateTriggerAsync(EventTrigger trigger);

        /// <summary>
        /// Deletes an event trigger.
        /// </summary>
        /// <param name="triggerId">The ID of the event trigger to delete.</param>
        /// <returns>True if the trigger was deleted, false otherwise.</returns>
        Task<bool> DeleteTriggerAsync(string triggerId);

        /// <summary>
        /// Lists all event triggers for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of event triggers.</returns>
        Task<(List<EventTrigger> Triggers, int TotalCount)> ListTriggersAsync(
            string userId, 
            EventTriggerStatus? status = null, 
            int page = 1, 
            int pageSize = 10);

        /// <summary>
        /// Processes an event and triggers any matching JavaScript functions.
        /// </summary>
        /// <param name="eventType">The type of event.</param>
        /// <param name="eventData">The event data.</param>
        /// <returns>A list of execution results.</returns>
        Task<List<JsonDocument>> ProcessEventAsync(string eventType, JsonDocument eventData);

        /// <summary>
        /// Gets all event triggers that match an event type.
        /// </summary>
        /// <param name="eventType">The type of event.</param>
        /// <returns>A list of matching event triggers.</returns>
        Task<List<EventTrigger>> GetTriggersByEventTypeAsync(string eventType);
    }
}
