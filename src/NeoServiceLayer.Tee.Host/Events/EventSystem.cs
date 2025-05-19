using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Shared.Events;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// System for managing events and event triggers.
    /// </summary>
    public class EventSystem : IEventSystem
    {
        private readonly ILogger<EventSystem> _logger;
        private readonly IStorageManager _storageManager;
        private readonly IEventTriggerManager _triggerManager;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, EventInfo> _events;
        private readonly ConcurrentDictionary<EventType, List<Func<EventInfo, Task>>> _eventHandlers;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the EventSystem class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="storageManager">The storage manager.</param>
        /// <param name="triggerManager">The event trigger manager.</param>
        public EventSystem(
            ILogger<EventSystem> logger,
            IStorageManager storageManager,
            IEventTriggerManager triggerManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _triggerManager = triggerManager ?? throw new ArgumentNullException(nameof(triggerManager));
            _semaphore = new SemaphoreSlim(1, 1);
            _events = new ConcurrentDictionary<string, EventInfo>();
            _eventHandlers = new ConcurrentDictionary<EventType, List<Func<EventInfo, Task>>>();
            _initialized = false;
            _disposed = false;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_initialized)
                    {
                        return true;
                    }

                    // Initialize storage provider for events
                    var storageProvider = _storageManager.GetProvider("events");
                    if (storageProvider == null)
                    {
                        // Create a new storage provider for events
                        storageProvider = await _storageManager.CreateProviderAsync(
                            "events",
                            StorageProviderType.File,
                            new FileStorageOptions { StorageDirectory = "events" });

                        if (storageProvider == null)
                        {
                            _logger.LogError("Failed to create storage provider for events");
                            return false;
                        }
                    }

                    // Initialize trigger manager
                    if (!await _triggerManager.InitializeAsync())
                    {
                        _logger.LogError("Failed to initialize event trigger manager");
                        return false;
                    }

                    // Load events from storage
                    if (!await LoadEventsAsync())
                    {
                        _logger.LogError("Failed to load events from storage");
                        return false;
                    }

                    _initialized = true;
                    _logger.LogInformation("Event system initialized with {EventCount} events", _events.Count);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing event system");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<EventInfo> PublishEventAsync(EventInfo @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Save event to storage
                if (!await SaveEventAsync(@event))
                {
                    throw new InvalidOperationException("Failed to save event to storage");
                }

                // Add event to in-memory map
                _events[@event.Id] = @event;

                // Process event asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessEventAsync(@event.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event {EventId}", @event.Id);
                    }
                });

                _logger.LogInformation("Published event {EventId} of type {EventType}", @event.Id, @event.Type);
                return @event;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event of type {EventType}", @event.Type);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<EventInfo> PublishEventAsync(EventType type, string source, JsonDocument data, string userId = null)
        {
            var @event = new EventInfo(type, source, data, userId);
            return await PublishEventAsync(@event);
        }

        /// <inheritdoc/>
        public async Task<EventInfo> PublishEventAsync(EventType type, string source, string data, string userId = null)
        {
            var @event = new EventInfo(type, source, data, userId);
            return await PublishEventAsync(@event);
        }

        /// <inheritdoc/>
        public async Task<EventInfo> GetEventAsync(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            }

            CheckDisposed();
            CheckInitialized();

            if (_events.TryGetValue(eventId, out var @event))
            {
                return @event;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<EventInfo>> ListEventsAsync(int limit = 100, int offset = 0)
        {
            CheckDisposed();
            CheckInitialized();

            return _events.Values
                .OrderByDescending(e => e.Timestamp)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<EventInfo>> ListEventsByTypeAsync(EventType type, int limit = 100, int offset = 0)
        {
            CheckDisposed();
            CheckInitialized();

            return _events.Values
                .Where(e => e.Type == type)
                .OrderByDescending(e => e.Timestamp)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<EventInfo>> ListEventsBySourceAsync(string source, int limit = 100, int offset = 0)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Source cannot be null or empty", nameof(source));
            }

            CheckDisposed();
            CheckInitialized();

            return _events.Values
                .Where(e => e.Source == source)
                .OrderByDescending(e => e.Timestamp)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<EventInfo>> ListEventsByUserAsync(string userId, int limit = 100, int offset = 0)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            CheckDisposed();
            CheckInitialized();

            return _events.Values
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Timestamp)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<int> ProcessEventAsync(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                // Get event
                var @event = await GetEventAsync(eventId);
                if (@event == null)
                {
                    throw new ArgumentException($"Event {eventId} not found");
                }

                // Skip already processed events
                if (@event.Status == EventStatus.Processed || @event.Status == EventStatus.Failed)
                {
                    return 0;
                }

                // Update event status
                @event.Status = EventStatus.Processing;
                await SaveEventAsync(@event);

                int triggersExecuted = 0;

                try
                {
                    // Process event based on type
                    switch (@event.Type)
                    {
                        case EventType.Blockchain:
                            triggersExecuted = await _triggerManager.ProcessBlockchainEventAsync(@event.GetDataAsString());
                            break;

                        case EventType.Storage:
                            // Extract key and operation from event data
                            var storageEvent = JsonSerializer.Deserialize<Dictionary<string, string>>(@event.GetDataAsString());
                            if (storageEvent.TryGetValue("key", out var key) && storageEvent.TryGetValue("operation", out var operation))
                            {
                                triggersExecuted = await _triggerManager.ProcessStorageEventAsync(key, operation);
                            }
                            break;

                        case EventType.Schedule:
                            // Extract current time from event data
                            var scheduleEvent = JsonSerializer.Deserialize<Dictionary<string, ulong>>(@event.GetDataAsString());
                            if (scheduleEvent.TryGetValue("current_time", out var currentTime))
                            {
                                triggersExecuted = await _triggerManager.ProcessScheduledTriggersAsync(currentTime);
                            }
                            break;

                        case EventType.External:
                            // Extract event type from event data
                            var externalEvent = JsonSerializer.Deserialize<Dictionary<string, string>>(@event.GetDataAsString());
                            if (externalEvent.TryGetValue("event_type", out var eventType))
                            {
                                triggersExecuted = await _triggerManager.ProcessExternalEventAsync(eventType, @event.GetDataAsString());
                            }
                            break;

                        case EventType.System:
                        case EventType.User:
                            // Process using event handlers
                            await ProcessEventHandlersAsync(@event);
                            break;
                    }

                    // Update event status
                    @event.SetProcessed(triggersExecuted);
                    await SaveEventAsync(@event);

                    _logger.LogInformation("Processed event {EventId}, executed {TriggersExecuted} triggers", eventId, triggersExecuted);
                    return triggersExecuted;
                }
                catch (Exception ex)
                {
                    // Update event status
                    @event.SetFailed(ex.Message);
                    await SaveEventAsync(@event);

                    _logger.LogError(ex, "Error processing event {EventId}", eventId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventId}", eventId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ProcessPendingEventsAsync()
        {
            CheckDisposed();
            CheckInitialized();

            try
            {
                int processedCount = 0;

                // Get pending events
                var pendingEvents = _events.Values
                    .Where(e => e.Status == EventStatus.Pending)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                foreach (var @event in pendingEvents)
                {
                    try
                    {
                        await ProcessEventAsync(@event.Id);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing pending event {EventId}", @event.Id);
                    }
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending events");
                return 0;
            }
        }

        /// <inheritdoc/>
        public IEventTriggerManager GetTriggerManager()
        {
            CheckDisposed();
            CheckInitialized();

            return _triggerManager;
        }

        /// <inheritdoc/>
        public void RegisterEventHandler(EventType type, Func<EventInfo, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            CheckDisposed();

            _eventHandlers.AddOrUpdate(
                type,
                new List<Func<EventInfo, Task>> { handler },
                (_, handlers) =>
                {
                    handlers.Add(handler);
                    return handlers;
                });
        }

        /// <inheritdoc/>
        public void UnregisterEventHandler(EventType type, Func<EventInfo, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            CheckDisposed();

            if (_eventHandlers.TryGetValue(type, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Processes event handlers for an event.
        /// </summary>
        /// <param name="event">The event to process.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessEventHandlersAsync(EventInfo @event)
        {
            if (_eventHandlers.TryGetValue(@event.Type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(@event);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing event handler for event {EventId}", @event.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Saves an event to storage.
        /// </summary>
        /// <param name="event">The event to save.</param>
        /// <returns>True if the event was saved successfully, false otherwise.</returns>
        private async Task<bool> SaveEventAsync(EventInfo @event)
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("events");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for events not found");
                    return false;
                }

                var json = JsonSerializer.Serialize(@event);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                return await storageProvider.WriteAsync(@event.Id, bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving event {EventId} to storage", @event.Id);
                return false;
            }
        }

        /// <summary>
        /// Loads events from storage.
        /// </summary>
        /// <returns>True if events were loaded successfully, false otherwise.</returns>
        private async Task<bool> LoadEventsAsync()
        {
            try
            {
                var storageProvider = _storageManager.GetProvider("events");
                if (storageProvider == null)
                {
                    _logger.LogError("Storage provider for events not found");
                    return false;
                }

                var keys = await storageProvider.GetAllKeysAsync();
                foreach (var key in keys)
                {
                    try
                    {
                        var bytes = await storageProvider.ReadAsync(key);
                        if (bytes == null || bytes.Length == 0)
                        {
                            continue;
                        }

                        var json = System.Text.Encoding.UTF8.GetString(bytes);
                        var @event = JsonSerializer.Deserialize<EventInfo>(json);

                        // Add event to in-memory map
                        _events[@event.Id] = @event;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading event {EventId} from storage", key);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events from storage");
                return false;
            }
        }

        /// <summary>
        /// Checks if the system is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Event system is not initialized");
            }
        }

        /// <summary>
        /// Checks if the system is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventSystem));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the system.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the system.
        /// </summary>
        ~EventSystem()
        {
            Dispose(false);
        }
    }
}
