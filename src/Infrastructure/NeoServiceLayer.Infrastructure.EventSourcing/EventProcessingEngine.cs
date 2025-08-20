using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core.Events;
using Polly;


namespace NeoServiceLayer.Infrastructure.EventSourcing
{
    /// <summary>
    /// Background service for processing events with retry logic and error handling
    /// </summary>
    public class EventProcessingEngine : BackgroundService
    {
        private readonly ILogger<EventProcessingEngine> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventProcessingConfiguration _configuration;
        private readonly ConcurrentQueue<PendingEventHandler> _eventQueue;
        private readonly SemaphoreSlim _processingLimiter;
        private readonly ConcurrentDictionary<Type, List<Type>> _handlerRegistry;

        public EventProcessingEngine(
            ILogger<EventProcessingEngine> logger,
            IServiceProvider serviceProvider,
            IOptions<EventProcessingConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

            _eventQueue = new ConcurrentQueue<PendingEventHandler>();
            _processingLimiter = new SemaphoreSlim(_configuration.MaxConcurrentHandlers, _configuration.MaxConcurrentHandlers);
            _handlerRegistry = new ConcurrentDictionary<Type, List<Type>>();

            RegisterHandlers();
        }

        public async Task EnqueueEventAsync(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            var eventType = domainEvent.GetType();
            if (_handlerRegistry.TryGetValue(eventType, out var handlerTypes))
            {
                foreach (var handlerType in handlerTypes)
                {
                    _eventQueue.Enqueue(new PendingEventHandler
                    {
                        Event = domainEvent,
                        HandlerType = handlerType,
                        EnqueuedAt = DateTime.UtcNow,
                        AttemptCount = 0
                    });
                }
            }

            await Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event processing engine started");

            var processingTasks = new List<Task>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Process events in batches
                    var batch = new List<PendingEventHandler>();
                    for (int i = 0; i < _configuration.ProcessingBatchSize && _eventQueue.TryDequeue(out var pendingHandler); i++)
                    {
                        batch.Add(pendingHandler);
                    }

                    if (batch.Any())
                    {
                        // Start processing tasks for the batch
                        var batchTasks = batch.Select(async pendingHandler =>
                        {
                            await _processingLimiter.WaitAsync(stoppingToken);
                            try
                            {
                                await ProcessEventHandlerAsync(pendingHandler, stoppingToken);
                            }
                            finally
                            {
                                _processingLimiter.Release();
                            }
                        });

                        processingTasks.AddRange(batchTasks);
                    }

                    // Clean up completed tasks
                    var completedTasks = processingTasks.Where(t => t.IsCompleted).ToList();
                    foreach (var completedTask in completedTasks)
                    {
                        processingTasks.Remove(completedTask);
                        if (completedTask.IsFaulted && completedTask.Exception != null)
                        {
                            _logger.LogError(completedTask.Exception, "Event processing task failed");
                        }
                    }

                    // Small delay if no events to process
                    if (!batch.Any())
                    {
                        await Task.Delay(100, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in event processing loop");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            // Wait for all processing tasks to complete
            if (processingTasks.Any())
            {
                _logger.LogInformation("Waiting for {TaskCount} event processing tasks to complete", processingTasks.Count);
                await Task.WhenAll(processingTasks);
            }

            _logger.LogInformation("Event processing engine stopped");
        }

        private async Task ProcessEventHandlerAsync(PendingEventHandler pendingHandler, CancellationToken cancellationToken)
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: _configuration.MaxRetryAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(_configuration.RetryDelayMs * Math.Pow(2, retryAttempt - 1)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} for event handler {HandlerType} processing event {EventType} after {Delay}ms. Error: {Error}",
                            retryCount, pendingHandler.HandlerType.Name, pendingHandler.Event.EventType, timespan.TotalMilliseconds, outcome?.Message);
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetService(pendingHandler.HandlerType);

                if (handler == null)
                {
                    _logger.LogError("Handler {HandlerType} not found in service container", pendingHandler.HandlerType.Name);
                    return;
                }

                // Use reflection to call HandleAsync method
                var handleMethod = handler.GetType().GetMethod("HandleAsync");
                if (handleMethod == null)
                {
                    _logger.LogError("HandleAsync method not found on handler {HandlerType}", pendingHandler.HandlerType.Name);
                    return;
                }

                var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.HandlerTimeoutSeconds));

                try
                {
                    pendingHandler.AttemptCount++;
                    pendingHandler.LastAttemptAt = DateTime.UtcNow;

                    var task = (Task?)handleMethod.Invoke(handler, new object[] { pendingHandler.Event, timeoutCts.Token });
                    if (task != null)
                    {
                        await task;
                    }

                    _logger.LogDebug(
                        "Successfully processed event {EventType} with handler {HandlerType} (attempt {AttemptCount})",
                        pendingHandler.Event.EventType, pendingHandler.HandlerType.Name, pendingHandler.AttemptCount);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Event handler {pendingHandler.HandlerType.Name} timed out after {_configuration.HandlerTimeoutSeconds} seconds");
                }
                finally
                {
                    timeoutCts.Dispose();
                }
            });
        }

        private void RegisterHandlers()
        {
            // In a real implementation, you would scan assemblies for IEventHandler implementations
            // For now, this is a placeholder that would be populated by the DI container setup
            _logger.LogInformation("Event handler registry initialized");
        }

        public void RegisterHandler<TEvent, THandler>()
            where TEvent : class, IDomainEvent
            where THandler : class, IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(THandler);

            _handlerRegistry.AddOrUpdate(
                eventType,
                new List<Type> { handlerType },
                (key, existing) =>
                {
                    if (!existing.Contains(handlerType))
                    {
                        existing.Add(handlerType);
                    }
                    return existing;
                });

            _logger.LogInformation(
                "Registered handler {HandlerType} for event {EventType}",
                handlerType.Name, eventType.Name);
        }

        public override void Dispose()
        {
            _processingLimiter?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Represents a pending event handler execution
    /// </summary>
    internal class PendingEventHandler
    {
        public IDomainEvent Event { get; set; } = null!;
        public Type HandlerType { get; set; } = null!;
        public DateTime EnqueuedAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public int AttemptCount { get; set; }
    }
}