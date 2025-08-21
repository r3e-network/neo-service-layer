using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Messaging
{
    /// <summary>
    /// Comprehensive message queue service for Neo Service Layer
    /// Provides reliable, scalable message queuing with durability and ordering guarantees
    /// </summary>
    public interface IMessageQueueService
    {
        /// <summary>
        /// Publishes a message to a queue
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="queueName">Queue name</param>
        /// <param name="message">Message to publish</param>
        /// <param name="publishOptions">Publishing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Publish result</returns>
        Task<MessagePublishResult> PublishAsync<T>(
            string queueName,
            T message,
            MessagePublishOptions? publishOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes multiple messages to a queue
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="queueName">Queue name</param>
        /// <param name="messages">Messages to publish</param>
        /// <param name="publishOptions">Publishing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Batch publish result</returns>
        Task<MessageBatchPublishResult> PublishBatchAsync<T>(
            string queueName,
            IEnumerable<T> messages,
            MessagePublishOptions? publishOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message to a topic (fan-out)
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="topicName">Topic name</param>
        /// <param name="message">Message to publish</param>
        /// <param name="publishOptions">Publishing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Topic publish result</returns>
        Task<MessagePublishResult> PublishToTopicAsync<T>(
            string topicName,
            T message,
            MessagePublishOptions? publishOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to a queue for message consumption
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="queueName">Queue name</param>
        /// <param name="messageHandler">Message handler</param>
        /// <param name="subscriptionOptions">Subscription options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Subscription handle</returns>
        Task<IMessageSubscription> SubscribeAsync<T>(
            string queueName,
            Func<MessageContext<T>, CancellationToken, Task<MessageHandleResult>> messageHandler,
            MessageSubscriptionOptions? subscriptionOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to a topic for message consumption
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="topicName">Topic name</param>
        /// <param name="subscriptionName">Subscription name</param>
        /// <param name="messageHandler">Message handler</param>
        /// <param name="subscriptionOptions">Subscription options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Subscription handle</returns>
        Task<IMessageSubscription> SubscribeToTopicAsync<T>(
            string topicName,
            string subscriptionName,
            Func<MessageContext<T>, CancellationToken, Task<MessageHandleResult>> messageHandler,
            MessageSubscriptionOptions? subscriptionOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a message queue
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="queueOptions">Queue configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Queue creation result</returns>
        Task<QueueCreationResult> CreateQueueAsync(
            string queueName,
            QueueOptions? queueOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a message topic
        /// </summary>
        /// <param name="topicName">Topic name</param>
        /// <param name="topicOptions">Topic configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Topic creation result</returns>
        Task<TopicCreationResult> CreateTopicAsync(
            string topicName,
            TopicOptions? topicOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a message queue
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="deleteOptions">Deletion options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Queue deletion result</returns>
        Task<QueueDeletionResult> DeleteQueueAsync(
            string queueName,
            QueueDeletionOptions? deleteOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a message topic
        /// </summary>
        /// <param name="topicName">Topic name</param>
        /// <param name="deleteOptions">Deletion options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Topic deletion result</returns>
        Task<TopicDeletionResult> DeleteTopicAsync(
            string topicName,
            TopicDeletionOptions? deleteOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a queue
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Queue information</returns>
        Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a topic
        /// </summary>
        /// <param name="topicName">Topic name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Topic information</returns>
        Task<TopicInfo> GetTopicInfoAsync(string topicName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all queues
        /// </summary>
        /// <param name="listOptions">Listing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of queue information</returns>
        Task<QueueListResult> ListQueuesAsync(
            QueueListOptions? listOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all topics
        /// </summary>
        /// <param name="listOptions">Listing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of topic information</returns>
        Task<TopicListResult> ListTopicsAsync(
            TopicListOptions? listOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Purges all messages from a queue
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Purge result</returns>
        Task<QueuePurgeResult> PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dead letter messages from a queue
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="maxMessages">Maximum messages to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dead letter messages</returns>
        Task<DeadLetterMessagesResult> GetDeadLetterMessagesAsync(
            string queueName,
            int maxMessages = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Requeues dead letter messages
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="messageIds">Message IDs to requeue (null for all)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Requeue result</returns>
        Task<MessageRequeueResult> RequeueDeadLetterMessagesAsync(
            string queueName,
            IEnumerable<string>? messageIds = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets message queue service statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service statistics</returns>
        Task<MessageQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets message queue service health
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service health information</returns>
        Task<MessageQueueHealth> GetHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a message subscription
    /// </summary>
    public interface IMessageSubscription : IDisposable
    {
        /// <summary>
        /// Subscription identifier
        /// </summary>
        string SubscriptionId { get; }

        /// <summary>
        /// Queue or topic name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Subscription type
        /// </summary>
        SubscriptionType Type { get; }

        /// <summary>
        /// Whether the subscription is active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Number of messages processed
        /// </summary>
        long ProcessedMessageCount { get; }

        /// <summary>
        /// Number of failed messages
        /// </summary>
        long FailedMessageCount { get; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        DateTime LastActivityAt { get; }

        /// <summary>
        /// Starts message processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Start result</returns>
        Task<SubscriptionControlResult> StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops message processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stop result</returns>
        Task<SubscriptionControlResult> StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses message processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Pause result</returns>
        Task<SubscriptionControlResult> PauseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes message processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Resume result</returns>
        Task<SubscriptionControlResult> ResumeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets subscription metrics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Subscription metrics</returns>
        Task<SubscriptionMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Message context containing message data and metadata
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    public class MessageContext<T>
    {
        /// <summary>
        /// Unique message identifier
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Message payload
        /// </summary>
        public T Payload { get; set; } = default!;

        /// <summary>
        /// Message headers
        /// </summary>
        public Dictionary<string, object> Headers { get; set; } = new();

        /// <summary>
        /// Queue or topic name
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Message timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Number of delivery attempts
        /// </summary>
        public int DeliveryAttempt { get; set; }

        /// <summary>
        /// Message correlation ID
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Reply-to queue/topic
        /// </summary>
        public string? ReplyTo { get; set; }

        /// <summary>
        /// Message expiration time
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Message priority
        /// </summary>
        public MessagePriority Priority { get; set; }

        /// <summary>
        /// Message content type
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Message encoding
        /// </summary>
        public string ContentEncoding { get; set; } = "utf-8";

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Acknowledges the message as successfully processed
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Acknowledgment result</returns>
        public Task<MessageAckResult> AcknowledgeAsync(CancellationToken cancellationToken = default)
        {
            return AcknowledgeHandler?.Invoke(cancellationToken) ?? Task.FromResult(new MessageAckResult { Success = false });
        }

        /// <summary>
        /// Rejects the message (will be requeued or sent to dead letter)
        /// </summary>
        /// <param name="requeue">Whether to requeue the message</param>
        /// <param name="reason">Rejection reason</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rejection result</returns>
        public Task<MessageNackResult> RejectAsync(
            bool requeue = true,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            return RejectHandler?.Invoke(requeue, reason, cancellationToken) ?? Task.FromResult(new MessageNackResult { Success = false });
        }

        /// <summary>
        /// Internal handler for acknowledgments
        /// </summary>
        internal Func<CancellationToken, Task<MessageAckResult>>? AcknowledgeHandler { get; set; }

        /// <summary>
        /// Internal handler for rejections
        /// </summary>
        internal Func<bool, string?, CancellationToken, Task<MessageNackResult>>? RejectHandler { get; set; }
    }

    /// <summary>
    /// Configuration options for publishing messages
    /// </summary>
    public class MessagePublishOptions
    {
        /// <summary>
        /// Message priority
        /// </summary>
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        /// <summary>
        /// Message time-to-live
        /// </summary>
        public TimeSpan? TimeToLive { get; set; }

        /// <summary>
        /// Message headers
        /// </summary>
        public Dictionary<string, object> Headers { get; set; } = new();

        /// <summary>
        /// Message correlation ID
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Reply-to queue/topic
        /// </summary>
        public string? ReplyTo { get; set; }

        /// <summary>
        /// Whether the message should be persistent
        /// </summary>
        public bool IsPersistent { get; set; } = true;

        /// <summary>
        /// Whether to compress the message
        /// </summary>
        public bool Compress { get; set; } = false;

        /// <summary>
        /// Whether to encrypt the message
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// Message routing key (for topic exchanges)
        /// </summary>
        public string? RoutingKey { get; set; }

        /// <summary>
        /// Serialization format
        /// </summary>
        public SerializationFormat SerializationFormat { get; set; } = SerializationFormat.Json;

        /// <summary>
        /// Publisher confirmation mode
        /// </summary>
        public PublisherConfirmMode ConfirmMode { get; set; } = PublisherConfirmMode.WaitForConfirm;

        /// <summary>
        /// Timeout for publisher confirmations
        /// </summary>
        public TimeSpan ConfirmTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Configuration options for message subscriptions
    /// </summary>
    public class MessageSubscriptionOptions
    {
        /// <summary>
        /// Maximum number of concurrent message handlers
        /// </summary>
        public int MaxConcurrency { get; set; } = 1;

        /// <summary>
        /// Maximum number of unacknowledged messages
        /// </summary>
        public int MaxUnacknowledgedMessages { get; set; } = 10;

        /// <summary>
        /// Message acknowledgment mode
        /// </summary>
        public AcknowledgmentMode AcknowledgmentMode { get; set; } = AcknowledgmentMode.Auto;

        /// <summary>
        /// Message prefetch count
        /// </summary>
        public int PrefetchCount { get; set; } = 1;

        /// <summary>
        /// Auto-start the subscription
        /// </summary>
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// Retry policy for failed messages
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; } = new();

        /// <summary>
        /// Dead letter queue configuration
        /// </summary>
        public DeadLetterConfiguration? DeadLetterConfig { get; set; }

        /// <summary>
        /// Message filter expression
        /// </summary>
        public string? MessageFilter { get; set; }

        /// <summary>
        /// Consumer group (for load balancing)
        /// </summary>
        public string? ConsumerGroup { get; set; }

        /// <summary>
        /// Whether to start from the beginning of the queue
        /// </summary>
        public bool StartFromBeginning { get; set; } = false;
    }

    /// <summary>
    /// Configuration options for queues
    /// </summary>
    public class QueueOptions
    {
        /// <summary>
        /// Whether the queue should be durable
        /// </summary>
        public bool IsDurable { get; set; } = true;

        /// <summary>
        /// Whether the queue is exclusive to one connection
        /// </summary>
        public bool IsExclusive { get; set; } = false;

        /// <summary>
        /// Whether to auto-delete the queue when not in use
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Maximum queue length
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Maximum queue size in bytes
        /// </summary>
        public long? MaxSizeBytes { get; set; }

        /// <summary>
        /// Message time-to-live for the queue
        /// </summary>
        public TimeSpan? MessageTtl { get; set; }

        /// <summary>
        /// Queue expiration time
        /// </summary>
        public TimeSpan? QueueExpiration { get; set; }

        /// <summary>
        /// Dead letter exchange
        /// </summary>
        public string? DeadLetterExchange { get; set; }

        /// <summary>
        /// Dead letter routing key
        /// </summary>
        public string? DeadLetterRoutingKey { get; set; }

        /// <summary>
        /// Maximum number of delivery attempts
        /// </summary>
        public int MaxDeliveryAttempts { get; set; } = 3;

        /// <summary>
        /// Queue priority
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Additional queue arguments
        /// </summary>
        public Dictionary<string, object> Arguments { get; set; } = new();
    }

    /// <summary>
    /// Configuration options for topics
    /// </summary>
    public class TopicOptions
    {
        /// <summary>
        /// Whether the topic should be durable
        /// </summary>
        public bool IsDurable { get; set; } = true;

        /// <summary>
        /// Topic type (fanout, direct, topic, headers)
        /// </summary>
        public TopicType Type { get; set; } = TopicType.Fanout;

        /// <summary>
        /// Whether to auto-delete the topic when not in use
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Topic expiration time
        /// </summary>
        public TimeSpan? TopicExpiration { get; set; }

        /// <summary>
        /// Message retention period
        /// </summary>
        public TimeSpan? MessageRetention { get; set; }

        /// <summary>
        /// Maximum number of subscribers
        /// </summary>
        public int? MaxSubscribers { get; set; }

        /// <summary>
        /// Additional topic arguments
        /// </summary>
        public Dictionary<string, object> Arguments { get; set; } = new();
    }

    /// <summary>
    /// Retry policy configuration
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Initial retry delay
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum retry delay
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Backoff multiplier
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Whether to use exponential backoff
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Whether to add jitter to retry delays
        /// </summary>
        public bool UseJitter { get; set; } = true;
    }

    /// <summary>
    /// Dead letter configuration
    /// </summary>
    public class DeadLetterConfiguration
    {
        /// <summary>
        /// Dead letter queue name
        /// </summary>
        public string QueueName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to create the dead letter queue automatically
        /// </summary>
        public bool AutoCreate { get; set; } = true;

        /// <summary>
        /// Maximum number of messages in dead letter queue
        /// </summary>
        public int? MaxMessages { get; set; }

        /// <summary>
        /// Dead letter message retention period
        /// </summary>
        public TimeSpan? RetentionPeriod { get; set; }
    }

    /// <summary>
    /// Various enums for message queue configuration
    /// </summary>
    public enum SubscriptionType
    {
        Queue,
        Topic
    }

    public enum MessagePriority
    {
        Lowest = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Highest = 4
    }

    public enum AcknowledgmentMode
    {
        Auto,
        Manual
    }

    public enum SerializationFormat
    {
        Json,
        MessagePack,
        ProtocolBuffers,
        Avro,
        Binary
    }

    public enum PublisherConfirmMode
    {
        None,
        WaitForConfirm,
        Async
    }

    public enum TopicType
    {
        Fanout,
        Direct,
        Topic,
        Headers
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    /// <summary>
    /// Result classes for various message queue operations
    /// </summary>
    public class MessagePublishResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public MessageMetrics Metrics { get; set; } = new();
    }

    public class MessageBatchPublishResult
    {
        public bool Success { get; set; }
        public int TotalMessages { get; set; }
        public int SuccessfulMessages { get; set; }
        public int FailedMessages { get; set; }
        public List<string> MessageIds { get; set; } = new();
        public List<string> FailedMessageErrors { get; set; } = new();
        public DateTime PublishedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public MessageMetrics Metrics { get; set; } = new();
    }

    public class MessageHandleResult
    {
        public bool Success { get; set; }
        public MessageHandleAction Action { get; set; } = MessageHandleAction.Acknowledge;
        public string? ErrorMessage { get; set; }
        public bool ShouldRetry { get; set; } = true;
        public TimeSpan? RetryDelay { get; set; }
    }

    public class MessageAckResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public DateTime AcknowledgedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class MessageNackResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public DateTime RejectedAt { get; set; }
        public bool WasRequeued { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SubscriptionControlResult
    {
        public bool Success { get; set; }
        public string SubscriptionId { get; set; } = string.Empty;
        public SubscriptionState NewState { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class QueueCreationResult
    {
        public bool Success { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TopicCreationResult
    {
        public bool Success { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class QueueDeletionResult
    {
        public bool Success { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
        public int PurgedMessageCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TopicDeletionResult
    {
        public bool Success { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class QueuePurgeResult
    {
        public bool Success { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public int PurgedMessageCount { get; set; }
        public DateTime PurgedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DeadLetterMessagesResult
    {
        public bool Success { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public List<DeadLetterMessage> Messages { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class MessageRequeueResult
    {
        public bool Success { get; set; }
        public string QueueName { get; set; } = string.Empty;
        public int RequeuedMessageCount { get; set; }
        public DateTime RequeuedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class QueueListResult
    {
        public bool Success { get; set; }
        public List<QueueInfo> Queues { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class TopicListResult
    {
        public bool Success { get; set; }
        public List<TopicInfo> Topics { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Information classes
    /// </summary>
    public class QueueInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDurable { get; set; }
        public bool IsExclusive { get; set; }
        public bool AutoDelete { get; set; }
        public long MessageCount { get; set; }
        public int ConsumerCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public QueueState State { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new();
    }

    public class TopicInfo
    {
        public string Name { get; set; } = string.Empty;
        public TopicType Type { get; set; }
        public bool IsDurable { get; set; }
        public bool AutoDelete { get; set; }
        public int SubscriberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public TopicState State { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new();
    }

    public class DeadLetterMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string OriginalQueue { get; set; } = string.Empty;
        public object Payload { get; set; } = null!;
        public Dictionary<string, object> Headers { get; set; } = new();
        public DateTime DeadLetteredAt { get; set; }
        public int DeliveryAttempts { get; set; }
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Statistics and metrics classes
    /// </summary>
    public class MessageQueueStatistics
    {
        public long TotalMessagesSent { get; set; }
        public long TotalMessagesReceived { get; set; }
        public long TotalMessagesProcessed { get; set; }
        public long TotalMessagesFailed { get; set; }
        public long TotalQueuesCreated { get; set; }
        public long TotalTopicsCreated { get; set; }
        public int ActiveQueues { get; set; }
        public int ActiveTopics { get; set; }
        public int ActiveSubscriptions { get; set; }
        public TimeSpan AverageMessageLatency { get; set; }
        public DateTime CollectedAt { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    public class MessageQueueHealth
    {
        public HealthStatus Status { get; set; }
        public bool IsConnected { get; set; }
        public string BrokerVersion { get; set; } = string.Empty;
        public int ConnectedNodes { get; set; }
        public int TotalNodes { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double DiskUsagePercent { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public List<string> Issues { get; set; } = new();
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class SubscriptionMetrics
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public long MessagesProcessed { get; set; }
        public long MessagesAcknowledged { get; set; }
        public long MessagesRejected { get; set; }
        public long ErrorCount { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public DateTime LastMessageAt { get; set; }
        public SubscriptionState CurrentState { get; set; }
    }

    public class MessageMetrics
    {
        public TimeSpan PublishLatency { get; set; }
        public TimeSpan ProcessingLatency { get; set; }
        public long MessageSizeBytes { get; set; }
        public bool WasCompressed { get; set; }
        public bool WasEncrypted { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Additional enums and options
    /// </summary>
    public enum MessageHandleAction
    {
        Acknowledge,
        Reject,
        Retry
    }

    public enum SubscriptionState
    {
        Created,
        Starting,
        Active,
        Paused,
        Stopping,
        Stopped,
        Error
    }

    public enum QueueState
    {
        Active,
        Idle,
        Full,
        Error
    }

    public enum TopicState
    {
        Active,
        Idle,
        Error
    }

    public class QueueListOptions
    {
        public string? NameFilter { get; set; }
        public QueueState? StateFilter { get; set; }
        public int MaxResults { get; set; } = 100;
        public bool IncludeStatistics { get; set; } = false;
    }

    public class TopicListOptions
    {
        public string? NameFilter { get; set; }
        public TopicType? TypeFilter { get; set; }
        public TopicState? StateFilter { get; set; }
        public int MaxResults { get; set; } = 100;
        public bool IncludeStatistics { get; set; } = false;
    }

    public class QueueDeletionOptions
    {
        public bool PurgeMessages { get; set; } = true;
        public bool IfUnused { get; set; } = false;
        public bool IfEmpty { get; set; } = false;
    }

    public class TopicDeletionOptions
    {
        public bool IfUnused { get; set; } = false;
        public bool DisconnectSubscribers { get; set; } = true;
    }
}