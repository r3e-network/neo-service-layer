using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models.Events;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Provides a robust event system for enclave communication.
    /// </summary>
    public class EnclaveEventSystem : IEnclaveEventSystem, IDisposable
    {
        private readonly ILogger<EnclaveEventSystem> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly EnclaveEventOptions _options;
        private readonly ConcurrentDictionary<string, List<Func<EnclaveEvent, Task>>> _eventHandlers;
        private readonly ConcurrentDictionary<string, EnclaveEventSubscription> _subscriptions;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _eventProcessingTask;
        private readonly BlockingCollection<EnclaveEvent> _eventQueue;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveEventSystem"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for communication.</param>
        /// <param name="options">The options for the event system.</param>
        public EnclaveEventSystem(
            ILogger<EnclaveEventSystem> logger,
            IOcclumInterface occlumInterface,
            EnclaveEventOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _options = options ?? new EnclaveEventOptions();
            _eventHandlers = new ConcurrentDictionary<string, List<Func<EnclaveEvent, Task>>>();
            _subscriptions = new ConcurrentDictionary<string, EnclaveEventSubscription>();
            _cancellationTokenSource = new CancellationTokenSource();
            _eventQueue = new BlockingCollection<EnclaveEvent>(_options.MaxQueueSize);
            _disposed = false;

            // Start the event processing task
            _eventProcessingTask = Task.Run(() => ProcessEventsAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Publishes an event to the enclave.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventData">The data for the event.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task PublishAsync(string eventType, object eventData)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
            }

            _logger.LogDebug("Publishing event of type {EventType}", eventType);

            try
            {
                // Create the event
                var enclaveEvent = new EnclaveEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = eventType,
                    Data = eventData != null ? JsonSerializer.Serialize(eventData) : null,
                    Timestamp = DateTime.UtcNow
                };

                // Add the event to the queue
                if (!_eventQueue.TryAdd(enclaveEvent, _options.EnqueueTimeoutMs))
                {
                    _logger.LogWarning("Failed to add event of type {EventType} to the queue: queue is full or timed out", eventType);
                    throw new EnclaveEventException($"Failed to add event of type {eventType} to the queue: queue is full or timed out");
                }

                _logger.LogDebug("Event of type {EventType} added to the queue", eventType);
            }
            catch (Exception ex) when (!(ex is EnclaveEventException))
            {
                _logger.LogError(ex, "Failed to publish event of type {EventType}", eventType);
                throw new EnclaveEventException($"Failed to publish event of type {eventType}", ex);
            }
        }

        /// <summary>
        /// Subscribes to events of a specific type.
        /// </summary>
        /// <param name="eventType">The type of events to subscribe to.</param>
        /// <param name="handler">The handler for the events.</param>
        /// <returns>A subscription that can be used to unsubscribe.</returns>
        public IEnclaveEventSubscription Subscribe(string eventType, Func<EnclaveEvent, Task> handler)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _logger.LogDebug("Subscribing to events of type {EventType}", eventType);

            try
            {
                // Get or create the list of handlers for this event type
                var handlers = _eventHandlers.GetOrAdd(eventType, _ => new List<Func<EnclaveEvent, Task>>());

                // Add the handler to the list
                lock (handlers)
                {
                    handlers.Add(handler);
                }

                // Create a subscription
                var subscription = new EnclaveEventSubscription(this, eventType, handler);
                _subscriptions[subscription.Id] = subscription;

                _logger.LogDebug("Subscribed to events of type {EventType}", eventType);
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to events of type {EventType}", eventType);
                throw new EnclaveEventException($"Failed to subscribe to events of type {eventType}", ex);
            }
        }

        /// <summary>
        /// Unsubscribes from events.
        /// </summary>
        /// <param name="subscription">The subscription to unsubscribe.</param>
        /// <returns>True if the subscription was removed, false otherwise.</returns>
        public bool Unsubscribe(IEnclaveEventSubscription subscription)
        {
            CheckDisposed();

            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            _logger.LogDebug("Unsubscribing from events with subscription {SubscriptionId}", subscription.Id);

            try
            {
                // Remove the subscription
                if (!_subscriptions.TryRemove(subscription.Id, out var removedSubscription))
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found", subscription.Id);
                    return false;
                }

                // Remove the handler from the list
                if (_eventHandlers.TryGetValue(removedSubscription.EventType, out var handlers))
                {
                    lock (handlers)
                    {
                        handlers.Remove(removedSubscription.Handler);
                    }
                }

                _logger.LogDebug("Unsubscribed from events with subscription {SubscriptionId}", subscription.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from events with subscription {SubscriptionId}", subscription.Id);
                throw new EnclaveEventException($"Failed to unsubscribe from events with subscription {subscription.Id}", ex);
            }
        }

        /// <summary>
        /// Gets all subscriptions.
        /// </summary>
        /// <returns>A list of all subscriptions.</returns>
        public IReadOnlyList<IEnclaveEventSubscription> GetSubscriptions()
        {
            CheckDisposed();

            return new List<IEnclaveEventSubscription>(_subscriptions.Values);
        }

        /// <summary>
        /// Gets all subscriptions for a specific event type.
        /// </summary>
        /// <param name="eventType">The type of events to get subscriptions for.</param>
        /// <returns>A list of subscriptions for the specified event type.</returns>
        public IReadOnlyList<IEnclaveEventSubscription> GetSubscriptions(string eventType)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
            }

            return _subscriptions.Values
                .Where(s => s.EventType == eventType)
                .ToList();
        }

        /// <summary>
        /// Disposes the event system.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the event system.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop the event processing task
                    _cancellationTokenSource.Cancel();
                    try
                    {
                        _eventProcessingTask.Wait();
                    }
                    catch (AggregateException)
                    {
                        // Ignore task cancellation exceptions
                    }

                    // Dispose resources
                    _cancellationTokenSource.Dispose();
                    _eventQueue.Dispose();
                }

                _disposed = true;
            }
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event processing task started");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    EnclaveEvent enclaveEvent = null;

                    try
                    {
                        // Get the next event from the queue
                        enclaveEvent = _eventQueue.Take(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Task was canceled, exit the loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error taking event from the queue");
                        continue;
                    }

                    if (enclaveEvent == null)
                    {
                        continue;
                    }

                    try
                    {
                        // Process the event
                        await ProcessEventAsync(enclaveEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event of type {EventType}", enclaveEvent.Type);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event processing task");
            }

            _logger.LogInformation("Event processing task stopped");
        }

        private async Task ProcessEventAsync(EnclaveEvent enclaveEvent)
        {
            _logger.LogDebug("Processing event of type {EventType}", enclaveEvent.Type);

            // Get the handlers for this event type
            if (!_eventHandlers.TryGetValue(enclaveEvent.Type, out var handlers))
            {
                _logger.LogDebug("No handlers found for event of type {EventType}", enclaveEvent.Type);
                return;
            }

            // Create a copy of the handlers to avoid issues with concurrent modifications
            List<Func<EnclaveEvent, Task>> handlersCopy;
            lock (handlers)
            {
                handlersCopy = new List<Func<EnclaveEvent, Task>>(handlers);
            }

            // Invoke the handlers
            var tasks = new List<Task>();
            foreach (var handler in handlersCopy)
            {
                try
                {
                    tasks.Add(handler(enclaveEvent));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking handler for event of type {EventType}", enclaveEvent.Type);
                }
            }

            // Wait for all handlers to complete
            await Task.WhenAll(tasks);

            _logger.LogDebug("Event of type {EventType} processed", enclaveEvent.Type);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EnclaveEventSystem));
            }
        }
    }
}
