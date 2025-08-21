using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Default implementation of domain event publisher using dependency injection
    /// </summary>
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DomainEventPublisher> _logger;

        /// <summary>
        /// Initializes a new instance of DomainEventPublisher
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <param name="logger">The logger</param>
        public DomainEventPublisher(
            IServiceProvider serviceProvider, 
            ILogger<DomainEventPublisher> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent == null)
            {
                _logger.LogWarning("Attempted to publish null domain event");
                return;
            }

            var eventType = domainEvent.GetType();
            _logger.LogDebug("Publishing domain event {EventType} with ID {EventId}", 
                eventType.Name, domainEvent.EventId);

            try
            {
                await PublishToHandlersAsync(domainEvent, eventType, cancellationToken);
                _logger.LogDebug("Successfully published domain event {EventType} with ID {EventId}", 
                    eventType.Name, domainEvent.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish domain event {EventType} with ID {EventId}", 
                    eventType.Name, domainEvent.EventId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task PublishManyAsync(IDomainEvent[] domainEvents, CancellationToken cancellationToken = default)
        {
            if (domainEvents == null || domainEvents.Length == 0)
            {
                _logger.LogDebug("No domain events to publish");
                return;
            }

            _logger.LogDebug("Publishing {EventCount} domain events", domainEvents.Length);

            var tasks = domainEvents.Select(domainEvent => 
                PublishAsync(domainEvent, cancellationToken));

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogDebug("Successfully published all {EventCount} domain events", domainEvents.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish some domain events");
                throw;
            }
        }

        /// <summary>
        /// Publishes the domain event to all registered handlers
        /// </summary>
        /// <param name="domainEvent">The domain event</param>
        /// <param name="eventType">The event type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        private async Task PublishToHandlersAsync(
            IDomainEvent domainEvent, 
            Type eventType, 
            CancellationToken cancellationToken)
        {
            // Get all handlers for this event type
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType);

            var handlerTasks = new List<Task>();

            foreach (var handler in handlers)
            {
                var handlerTask = InvokeHandlerAsync(handler, domainEvent, cancellationToken);
                handlerTasks.Add(handlerTask);
            }

            if (handlerTasks.Count == 0)
            {
                _logger.LogDebug("No handlers found for domain event {EventType}", eventType.Name);
                return;
            }

            _logger.LogDebug("Found {HandlerCount} handlers for domain event {EventType}", 
                handlerTasks.Count, eventType.Name);

            // Execute all handlers concurrently
            await Task.WhenAll(handlerTasks);
        }

        /// <summary>
        /// Invokes a specific handler for the domain event
        /// </summary>
        /// <param name="handler">The handler instance</param>
        /// <param name="domainEvent">The domain event</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        private async Task InvokeHandlerAsync(
            object handler, 
            IDomainEvent domainEvent, 
            CancellationToken cancellationToken)
        {
            try
            {
                var handlerMethod = handler.GetType().GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                if (handlerMethod != null)
                {
                    var task = (Task?)handlerMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    if (task != null)
                    {
                        await task;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {HandlerType} failed to handle domain event {EventType}", 
                    handler.GetType().Name, domainEvent.GetType().Name);
                throw;
            }
        }
    }
}