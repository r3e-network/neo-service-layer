using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Events
{
    /// <summary>
    /// Interface for an event system that manages events and event triggers.
    /// </summary>
    public interface IEventSystem : IDisposable
    {
        /// <summary>
        /// Initializes the event system.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="event">The event to publish.</param>
        /// <returns>The published event.</returns>
        Task<EventInfo> PublishEventAsync(EventInfo @event);

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="source">The source of the event.</param>
        /// <param name="data">The data of the event.</param>
        /// <param name="userId">The ID of the user who triggered the event.</param>
        /// <returns>The published event.</returns>
        Task<EventInfo> PublishEventAsync(EventType type, string source, JsonDocument data, string userId = null);

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="source">The source of the event.</param>
        /// <param name="data">The data of the event as a JSON string.</param>
        /// <param name="userId">The ID of the user who triggered the event.</param>
        /// <returns>The published event.</returns>
        Task<EventInfo> PublishEventAsync(EventType type, string source, string data, string userId = null);

        /// <summary>
        /// Gets an event by ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to get.</param>
        /// <returns>The event, or null if not found.</returns>
        Task<EventInfo> GetEventAsync(string eventId);

        /// <summary>
        /// Lists all events.
        /// </summary>
        /// <param name="limit">The maximum number of events to return.</param>
        /// <param name="offset">The number of events to skip.</param>
        /// <returns>A list of events.</returns>
        Task<IReadOnlyList<EventInfo>> ListEventsAsync(int limit = 100, int offset = 0);

        /// <summary>
        /// Lists all events of a specific type.
        /// </summary>
        /// <param name="type">The type of events to list.</param>
        /// <param name="limit">The maximum number of events to return.</param>
        /// <param name="offset">The number of events to skip.</param>
        /// <returns>A list of events of the specified type.</returns>
        Task<IReadOnlyList<EventInfo>> ListEventsByTypeAsync(EventType type, int limit = 100, int offset = 0);

        /// <summary>
        /// Lists all events from a specific source.
        /// </summary>
        /// <param name="source">The source of events to list.</param>
        /// <param name="limit">The maximum number of events to return.</param>
        /// <param name="offset">The number of events to skip.</param>
        /// <returns>A list of events from the specified source.</returns>
        Task<IReadOnlyList<EventInfo>> ListEventsBySourceAsync(string source, int limit = 100, int offset = 0);

        /// <summary>
        /// Lists all events for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="limit">The maximum number of events to return.</param>
        /// <param name="offset">The number of events to skip.</param>
        /// <returns>A list of events for the specified user.</returns>
        Task<IReadOnlyList<EventInfo>> ListEventsByUserAsync(string userId, int limit = 100, int offset = 0);

        /// <summary>
        /// Processes an event.
        /// </summary>
        /// <param name="eventId">The ID of the event to process.</param>
        /// <returns>The number of triggers executed.</returns>
        Task<int> ProcessEventAsync(string eventId);

        /// <summary>
        /// Processes all pending events.
        /// </summary>
        /// <returns>The number of events processed.</returns>
        Task<int> ProcessPendingEventsAsync();

        /// <summary>
        /// Gets the event trigger manager.
        /// </summary>
        /// <returns>The event trigger manager.</returns>
        IEventTriggerManager GetTriggerManager();

        /// <summary>
        /// Registers an event handler.
        /// </summary>
        /// <param name="type">The type of events to handle.</param>
        /// <param name="handler">The event handler.</param>
        void RegisterEventHandler(EventType type, Func<EventInfo, Task> handler);

        /// <summary>
        /// Unregisters an event handler.
        /// </summary>
        /// <param name="type">The type of events to handle.</param>
        /// <param name="handler">The event handler.</param>
        void UnregisterEventHandler(EventType type, Func<EventInfo, Task> handler);
    }
}
