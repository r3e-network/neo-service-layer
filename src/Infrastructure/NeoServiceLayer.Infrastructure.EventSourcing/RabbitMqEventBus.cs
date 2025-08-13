using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NeoServiceLayer.Core.Events;
using System.Collections.Concurrent;
using Polly;
using Polly.Extensions.Http;

namespace NeoServiceLayer.Infrastructure.EventSourcing
{
    /// <summary>
    /// RabbitMQ implementation of the event bus
    /// </summary>
    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly ILogger&lt;RabbitMqEventBus&gt; _logger;
        private readonly EventBusConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ConcurrentDictionary&lt;Guid, EventSubscriptionInternal&gt; _subscriptions;
        private readonly AsyncPolicy _retryPolicy;
        private bool _disposed;

        public RabbitMqEventBus(
            ILogger&lt;RabbitMqEventBus&gt; logger,
            IOptions&lt;EventBusConfiguration&gt; configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            
            _subscriptions = new ConcurrentDictionary&lt;Guid, EventSubscriptionInternal&gt;();
            
            // Create connection and channel
            var factory = new ConnectionFactory
            {
                HostName = _configuration.HostName,
                Port = _configuration.Port,
                UserName = _configuration.UserName,
                Password = _configuration.Password,
                VirtualHost = _configuration.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };
            
            _connection = factory.CreateConnection($"NEO-EventBus-{Environment.MachineName}");
            _channel = _connection.CreateModel();
            
            // Setup retry policy
            _retryPolicy = Policy
                .Handle&lt;Exception&gt;()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =&gt; TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =&gt;
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} for event bus operation after {Delay}ms. Error: {Error}",
                            retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
                    });
            
            InitializeExchangesAndQueues();
        }

        public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            await _retryPolicy.ExecuteAsync(async () =&gt;
            {
                var eventMessage = new EventMessage
                {
                    EventId = domainEvent.EventId,
                    EventType = domainEvent.EventType,
                    AggregateId = domainEvent.AggregateId,
                    AggregateType = domainEvent.AggregateType,
                    AggregateVersion = domainEvent.AggregateVersion,
                    OccurredAt = domainEvent.OccurredAt,
                    CausationId = domainEvent.CausationId,
                    CorrelationId = domainEvent.CorrelationId,
                    InitiatedBy = domainEvent.InitiatedBy,
                    Data = JsonSerializer.Serialize(domainEvent, _jsonOptions),
                    Metadata = domainEvent.Metadata
                };

                var messageBody = JsonSerializer.SerializeToUtf8Bytes(eventMessage, _jsonOptions);
                var routingKey = GetRoutingKey(domainEvent.EventType);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = domainEvent.EventId.ToString();
                properties.Type = domainEvent.EventType;
                properties.Timestamp = new AmqpTimestamp(((DateTimeOffset)domainEvent.OccurredAt).ToUnixTimeSeconds());
                properties.CorrelationId = domainEvent.CorrelationId?.ToString();
                properties.Headers = new Dictionary&lt;string, object&gt;
                {
                    ["aggregate_id"] = domainEvent.AggregateId,
                    ["aggregate_type"] = domainEvent.AggregateType,
                    ["aggregate_version"] = domainEvent.AggregateVersion,
                    ["initiated_by"] = domainEvent.InitiatedBy
                };

                _channel.BasicPublish(
                    exchange: _configuration.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: messageBody);

                _logger.LogDebug(
                    "Published event {EventType} for aggregate {AggregateId} with routing key {RoutingKey}",
                    domainEvent.EventType, domainEvent.AggregateId, routingKey);

                await Task.CompletedTask; // For consistency with async pattern
            });
        }

        public async Task PublishBatchAsync(
            IEnumerable&lt;IDomainEvent&gt; domainEvents, 
            CancellationToken cancellationToken = default)
        {
            if (domainEvents == null)
                throw new ArgumentNullException(nameof(domainEvents));

            var batch = _channel.CreateBasicPublishBatch();
            var eventCount = 0;

            await _retryPolicy.ExecuteAsync(async () =&gt;
            {
                foreach (var domainEvent in domainEvents)
                {
                    var eventMessage = new EventMessage
                    {
                        EventId = domainEvent.EventId,
                        EventType = domainEvent.EventType,
                        AggregateId = domainEvent.AggregateId,
                        AggregateType = domainEvent.AggregateType,
                        AggregateVersion = domainEvent.AggregateVersion,
                        OccurredAt = domainEvent.OccurredAt,
                        CausationId = domainEvent.CausationId,
                        CorrelationId = domainEvent.CorrelationId,
                        InitiatedBy = domainEvent.InitiatedBy,
                        Data = JsonSerializer.Serialize(domainEvent, _jsonOptions),
                        Metadata = domainEvent.Metadata
                    };

                    var messageBody = JsonSerializer.SerializeToUtf8Bytes(eventMessage, _jsonOptions);
                    var routingKey = GetRoutingKey(domainEvent.EventType);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.MessageId = domainEvent.EventId.ToString();
                    properties.Type = domainEvent.EventType;
                    properties.Timestamp = new AmqpTimestamp(((DateTimeOffset)domainEvent.OccurredAt).ToUnixTimeSeconds());
                    properties.CorrelationId = domainEvent.CorrelationId?.ToString();

                    batch.Add(
                        _configuration.ExchangeName,
                        routingKey,
                        false,
                        properties,
                        messageBody);

                    eventCount++;
                }

                batch.Publish();

                _logger.LogDebug("Published batch of {EventCount} events", eventCount);

                await Task.CompletedTask; // For consistency with async pattern
            });
        }

        public IEventSubscription Subscribe&lt;TEvent&gt;(IEventHandler&lt;TEvent&gt; handler) 
            where TEvent : class, IDomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent).Name;
            return SubscribeInternal(eventType, async (message, cancellationToken) =&gt;
            {
                if (message.EventType == eventType)
                {
                    var domainEvent = JsonSerializer.Deserialize&lt;TEvent&gt;(message.Data, _jsonOptions);
                    if (domainEvent != null)
                    {
                        await handler.HandleAsync(domainEvent, cancellationToken);
                    }
                }
            });
        }

        public IEventSubscription Subscribe(string eventTypePattern, IEventHandler&lt;IDomainEvent&gt; handler)
        {
            if (string.IsNullOrWhiteSpace(eventTypePattern))
                throw new ArgumentException("Event type pattern cannot be null or empty", nameof(eventTypePattern));
            
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return SubscribeInternal(eventTypePattern, async (message, cancellationToken) =&gt;
            {
                // Simple pattern matching - in production, use more sophisticated matching
                if (IsPatternMatch(message.EventType, eventTypePattern))
                {
                    var domainEvent = CreateGenericDomainEvent(message);
                    await handler.HandleAsync(domainEvent, cancellationToken);
                }
            });
        }

        private IEventSubscription SubscribeInternal(
            string eventTypePattern, 
            Func&lt;EventMessage, CancellationToken, Task&gt; messageHandler)
        {
            var subscriptionId = Guid.NewGuid();
            var queueName = $"neo_events_{subscriptionId}";
            var routingKey = GetRoutingKey(eventTypePattern);

            // Declare queue
            _channel.QueueDeclare(
                queue: queueName,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: queueName,
                exchange: _configuration.ExchangeName,
                routingKey: routingKey);

            // Create consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =&gt;
            {
                try
                {
                    var messageBody = ea.Body.ToArray();
                    var eventMessage = JsonSerializer.Deserialize&lt;EventMessage&gt;(messageBody, _jsonOptions);
                    
                    if (eventMessage != null)
                    {
                        await messageHandler(eventMessage, CancellationToken.None);
                    }
                    
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event message");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            var consumerTag = _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            var subscription = new EventSubscriptionInternal(
                subscriptionId,
                eventTypePattern,
                queueName,
                consumerTag,
                () =&gt; CleanupSubscription(subscriptionId, queueName, consumerTag));

            _subscriptions.TryAdd(subscriptionId, subscription);

            _logger.LogInformation(
                "Created subscription {SubscriptionId} for pattern {Pattern} with queue {Queue}",
                subscriptionId, eventTypePattern, queueName);

            return subscription;
        }

        private void CleanupSubscription(Guid subscriptionId, string queueName, string consumerTag)
        {
            try
            {
                if (!_disposed && _channel.IsOpen)
                {
                    _channel.BasicCancel(consumerTag);
                    _channel.QueueDelete(queueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up subscription {SubscriptionId}", subscriptionId);
            }
            finally
            {
                _subscriptions.TryRemove(subscriptionId, out _);
            }
        }

        private void InitializeExchangesAndQueues()
        {
            // Declare main event exchange
            _channel.ExchangeDeclare(
                exchange: _configuration.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation(
                "Initialized RabbitMQ event bus with exchange {ExchangeName}",
                _configuration.ExchangeName);
        }

        private string GetRoutingKey(string eventType)
        {
            // Convert EventType to routing key format (e.g., KeyGeneratedEvent -&gt; key.generated.event)
            return eventType.Replace("Event", "").ToLowerInvariant();
        }

        private bool IsPatternMatch(string eventType, string pattern)
        {
            // Simple wildcard matching - in production, use more sophisticated pattern matching
            if (pattern == "*")
                return true;
            
            if (pattern.EndsWith("*"))
                return eventType.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
            
            return eventType.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private IDomainEvent CreateGenericDomainEvent(EventMessage message)
        {
            return new GenericDomainEventFromMessage(
                message.EventId,
                message.OccurredAt,
                message.AggregateId,
                message.AggregateType,
                message.AggregateVersion,
                message.EventType,
                message.CausationId,
                message.CorrelationId,
                message.InitiatedBy,
                message.Data,
                message.Metadata ?? new Dictionary&lt;string, object&gt;());
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Dispose all subscriptions
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            
            _subscriptions.Clear();

            _channel?.Dispose();
            _connection?.Dispose();

            _logger.LogInformation("RabbitMQ event bus disposed");
        }
    }

    /// <summary>
    /// Internal event subscription implementation
    /// </summary>
    internal class EventSubscriptionInternal : IEventSubscription
    {
        private readonly Action _cleanup;
        private bool _disposed;

        public EventSubscriptionInternal(
            Guid subscriptionId,
            string eventTypePattern,
            string queueName,
            string consumerTag,
            Action cleanup)
        {
            SubscriptionId = subscriptionId;
            EventTypePattern = eventTypePattern;
            QueueName = queueName;
            ConsumerTag = consumerTag;
            CreatedAt = DateTime.UtcNow;
            _cleanup = cleanup;
        }

        public Guid SubscriptionId { get; }
        public string EventTypePattern { get; }
        public string QueueName { get; }
        public string ConsumerTag { get; }
        public DateTime CreatedAt { get; }
        public bool IsActive =&gt; !_disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cleanup?.Invoke();
        }
    }

    /// <summary>
    /// Message format for RabbitMQ transport
    /// </summary>
    internal class EventMessage
    {
        public Guid EventId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string AggregateId { get; set; } = string.Empty;
        public string AggregateType { get; set; } = string.Empty;
        public long AggregateVersion { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid? CausationId { get; set; }
        public Guid? CorrelationId { get; set; }
        public string InitiatedBy { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public Dictionary&lt;string, object&gt;? Metadata { get; set; }
    }

    /// <summary>
    /// Generic domain event created from message bus
    /// </summary>
    internal class GenericDomainEventFromMessage : IDomainEvent
    {
        public GenericDomainEventFromMessage(
            Guid eventId,
            DateTime occurredAt,
            string aggregateId,
            string aggregateType,
            long aggregateVersion,
            string eventType,
            Guid? causationId,
            Guid? correlationId,
            string initiatedBy,
            string serializedData,
            IDictionary&lt;string, object&gt; metadata)
        {
            EventId = eventId;
            OccurredAt = occurredAt;
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            EventType = eventType;
            CausationId = causationId;
            CorrelationId = correlationId;
            InitiatedBy = initiatedBy;
            SerializedData = serializedData;
            Metadata = metadata;
        }

        public Guid EventId { get; }
        public DateTime OccurredAt { get; }
        public string AggregateId { get; }
        public string AggregateType { get; }
        public long AggregateVersion { get; }
        public string EventType { get; }
        public Guid? CausationId { get; }
        public Guid? CorrelationId { get; }
        public string InitiatedBy { get; }
        public string SerializedData { get; }
        public IDictionary&lt;string, object&gt; Metadata { get; }
    }
}