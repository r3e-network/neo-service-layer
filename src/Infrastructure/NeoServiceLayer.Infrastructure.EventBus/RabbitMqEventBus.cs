using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NeoServiceLayer.Core.Events;
using Polly;
using Polly.Retry;

namespace NeoServiceLayer.Infrastructure.EventBus
{
    /// <summary>
    /// RabbitMQ implementation of the event bus for distributed event handling
    /// </summary>
    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly ILogger<RabbitMqEventBus> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEventHandlerRegistry _handlerRegistry;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly Dictionary<Type, List<IEventHandler>> _handlers;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly SemaphoreSlim _publishSemaphore;
        private bool _disposed;

        public RabbitMqEventBus(
            ILogger<RabbitMqEventBus> logger,
            IConfiguration configuration,
            IEventHandlerRegistry handlerRegistry)
        {
            _logger = logger;
            _configuration = configuration;
            _handlerRegistry = handlerRegistry;
            _handlers = new Dictionary<Type, List<IEventHandler>>();
            _publishSemaphore = new SemaphoreSlim(1, 1);

            // Configure RabbitMQ connection
            var factory = new ConnectionFactory
            {
                HostName = configuration["EventBus:RabbitMQ:Host"] ?? "localhost",
                Port = configuration.GetValue<int>("EventBus:RabbitMQ:Port", 5672),
                UserName = configuration["EventBus:RabbitMQ:Username"] ?? "guest",
                Password = configuration["EventBus:RabbitMQ:Password"] ?? "guest",
                VirtualHost = configuration["EventBus:RabbitMQ:VirtualHost"] ?? "/",
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _exchangeName = configuration["EventBus:RabbitMQ:Exchange"] ?? "neo_service_events";
            _queueName = configuration["EventBus:RabbitMQ:Queue"] ?? $"neo_service_{Environment.MachineName}";

            // Create connection and channel
            _connection = factory.CreateConnection("NeoServiceLayer");
            _channel = _connection.CreateModel();

            // Declare exchange and queue
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-message-ttl"] = 86400000, // 24 hours
                    ["x-max-length"] = 1000000 // Max 1M messages
                });

            // Configure retry policy
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "Retry {RetryCount} after {TimeSpan}s for event publishing",
                            retryCount, timeSpan.TotalSeconds);
                    });

            // Set up consumer
            SetupConsumer();

            _logger.LogInformation("RabbitMQ Event Bus initialized with exchange {Exchange} and queue {Queue}",
                _exchangeName, _queueName);
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            await _publishSemaphore.WaitAsync();
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    var eventType = @event.GetType();
                    var routingKey = GetRoutingKey(eventType);
                    var message = JsonConvert.SerializeObject(@event, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
                    var body = Encoding.UTF8.GetBytes(message);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.MessageId = @event.EventId.ToString();
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.CorrelationId = @event.CorrelationId?.ToString();
                    properties.Headers = new Dictionary<string, object>
                    {
                        ["event-type"] = eventType.FullName,
                        ["aggregate-id"] = @event.AggregateId?.ToString(),
                        ["user-id"] = @event.UserId?.ToString(),
                        ["version"] = @event.Version.ToString()
                    };

                    _channel.BasicPublish(
                        exchange: _exchangeName,
                        routingKey: routingKey,
                        mandatory: false,
                        basicProperties: properties,
                        body: body);

                    _logger.LogDebug("Published event {EventType} with ID {EventId} to {RoutingKey}",
                        eventType.Name, @event.EventId, routingKey);

                    await Task.CompletedTask;
                });
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : IDomainEvent
        {
            if (events == null || !events.Any())
                return;

            await _publishSemaphore.WaitAsync();
            try
            {
                var batch = _channel.CreateBasicPublishBatch();

                foreach (var @event in events)
                {
                    var eventType = @event.GetType();
                    var routingKey = GetRoutingKey(eventType);
                    var message = JsonConvert.SerializeObject(@event, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
                    var body = Encoding.UTF8.GetBytes(message);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.MessageId = @event.EventId.ToString();
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.CorrelationId = @event.CorrelationId?.ToString();

                    batch.Add(_exchangeName, routingKey, false, properties, body);
                }

                batch.Publish();

                _logger.LogDebug("Published batch of {EventCount} events", events.Count());
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        public void Subscribe<TEvent, THandler>()
            where TEvent : IDomainEvent
            where THandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(THandler);
            var routingKey = GetRoutingKey(eventType);

            // Bind queue to exchange with routing key
            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: routingKey);

            // Register handler
            _handlerRegistry.RegisterHandler<TEvent, THandler>();

            _logger.LogInformation("Subscribed {Handler} to handle {Event} with routing key {RoutingKey}",
                handlerType.Name, eventType.Name, routingKey);
        }

        public void Unsubscribe<TEvent, THandler>()
            where TEvent : IDomainEvent
            where THandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(THandler);
            var routingKey = GetRoutingKey(eventType);

            // Unbind queue from exchange
            _channel.QueueUnbind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: routingKey);

            // Unregister handler
            _handlerRegistry.UnregisterHandler<TEvent, THandler>();

            _logger.LogInformation("Unsubscribed {Handler} from {Event}",
                handlerType.Name, eventType.Name);
        }

        private void SetupConsumer()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.Received += async (sender, args) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(args.Body.ToArray());
                    var @event = JsonConvert.DeserializeObject<IDomainEvent>(message, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });

                    if (@event != null)
                    {
                        await ProcessEventAsync(@event, args);
                        _channel.BasicAck(args.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogWarning("Could not deserialize message: {Message}", message);
                        _channel.BasicNack(args.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    
                    // Requeue message if it hasn't been redelivered too many times
                    var redeliveryCount = GetRedeliveryCount(args.BasicProperties);
                    if (redeliveryCount < 3)
                    {
                        _channel.BasicNack(args.DeliveryTag, false, true);
                    }
                    else
                    {
                        // Send to dead letter queue after max retries
                        _channel.BasicNack(args.DeliveryTag, false, false);
                        await PublishToDeadLetterQueueAsync(args.Body.ToArray(), ex);
                    }
                }
            };

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        }

        private async Task ProcessEventAsync(IDomainEvent @event, BasicDeliverEventArgs args)
        {
            var eventType = @event.GetType();
            var handlers = _handlerRegistry.GetHandlers(eventType);

            if (!handlers.Any())
            {
                _logger.LogWarning("No handlers registered for event type {EventType}", eventType.Name);
                return;
            }

            var tasks = handlers.Select(handler => HandleEventAsync(handler, @event));
            await Task.WhenAll(tasks);

            _logger.LogDebug("Processed event {EventType} with {HandlerCount} handlers",
                eventType.Name, handlers.Count());
        }

        private async Task HandleEventAsync(IEventHandler handler, IDomainEvent @event)
        {
            try
            {
                var handleMethod = handler.GetType()
                    .GetMethod("HandleAsync", new[] { @event.GetType(), typeof(CancellationToken) });
                
                if (handleMethod != null)
                {
                    var task = (Task)handleMethod.Invoke(handler, new object[] { @event, CancellationToken.None });
                    await task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler {Handler} for event {Event}",
                    handler.GetType().Name, @event.GetType().Name);
                throw;
            }
        }

        private string GetRoutingKey(Type eventType)
        {
            // Generate routing key based on event type
            // Format: domain.aggregate.event
            var typeName = eventType.Name;
            var parts = typeName.Split('.');
            
            if (parts.Length >= 3)
            {
                return string.Join(".", parts.TakeLast(3)).ToLower();
            }
            
            return typeName.ToLower().Replace("event", "");
        }

        private int GetRedeliveryCount(IBasicProperties properties)
        {
            if (properties?.Headers != null && properties.Headers.ContainsKey("x-redelivery-count"))
            {
                return Convert.ToInt32(properties.Headers["x-redelivery-count"]);
            }
            return 0;
        }

        private async Task PublishToDeadLetterQueueAsync(byte[] message, Exception exception)
        {
            try
            {
                var dlqName = $"{_queueName}_dlq";
                
                _channel.QueueDeclare(
                    queue: dlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                        ["x-message-ttl"] = 604800000 // 7 days
                    });

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Headers = new Dictionary<string, object>
                {
                    ["original-queue"] = _queueName,
                    ["error-message"] = exception.Message,
                    ["error-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: dlqName,
                    basicProperties: properties,
                    body: message);

                _logger.LogWarning("Message sent to dead letter queue: {DLQ}", dlqName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to dead letter queue");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _publishSemaphore?.Dispose();

            _disposed = true;
        }
    }

    /// <summary>
    /// Registry for event handlers
    /// </summary>
    public interface IEventHandlerRegistry
    {
        void RegisterHandler<TEvent, THandler>()
            where TEvent : IDomainEvent
            where THandler : IEventHandler<TEvent>;
        
        void UnregisterHandler<TEvent, THandler>()
            where TEvent : IDomainEvent
            where THandler : IEventHandler<TEvent>;
        
        IEnumerable<IEventHandler> GetHandlers(Type eventType);
    }

    /// <summary>
    /// Implementation of event handler registry
    /// </summary>
    public class EventHandlerRegistry : IEventHandlerRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, List<Type>> _handlerTypes;
        private readonly object _lock = new object();

        public EventHandlerRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _handlerTypes = new Dictionary<Type, List<Type>>();
        }

        public void RegisterHandler<TEvent, THandler>()
            where TEvent : IDomainEvent
            where THandler : IEventHandler<TEvent>
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                var handlerType = typeof(THandler);

                if (!_handlerTypes.ContainsKey(eventType))
                {
                    _handlerTypes[eventType] = new List<Type>();
                }

                if (!_handlerTypes[eventType].Contains(handlerType))
                {
                    _handlerTypes[eventType].Add(handlerType);
                }
            }
        }

        public void UnregisterHandler<TEvent, THandler>()
            where TEvent : IDomainEvent
            where THandler : IEventHandler<TEvent>
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                var handlerType = typeof(THandler);

                if (_handlerTypes.ContainsKey(eventType))
                {
                    _handlerTypes[eventType].Remove(handlerType);
                }
            }
        }

        public IEnumerable<IEventHandler> GetHandlers(Type eventType)
        {
            lock (_lock)
            {
                if (!_handlerTypes.ContainsKey(eventType))
                {
                    return Enumerable.Empty<IEventHandler>();
                }

                var handlers = new List<IEventHandler>();
                foreach (var handlerType in _handlerTypes[eventType])
                {
                    var handler = _serviceProvider.GetService(handlerType) as IEventHandler;
                    if (handler != null)
                    {
                        handlers.Add(handler);
                    }
                }

                return handlers;
            }
        }
    }
}