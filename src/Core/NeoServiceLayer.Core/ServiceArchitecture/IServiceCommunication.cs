using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.ServiceArchitecture
{
    /// <summary>
    /// Inter-service communication interface
    /// </summary>
    public interface IServiceCommunication
    {
        /// <summary>
        /// Sends a request to another service
        /// </summary>
        Task<TResponse> SendAsync<TRequest, TResponse>(
            string targetServiceId, 
            TRequest request,
            CommunicationOptions options = null,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Sends a one-way message to another service
        /// </summary>
        Task SendOneWayAsync<TMessage>(
            string targetServiceId,
            TMessage message,
            CommunicationOptions options = null,
            CancellationToken cancellationToken = default)
            where TMessage : class;

        /// <summary>
        /// Publishes an event to all interested services
        /// </summary>
        Task PublishEventAsync<TEvent>(
            TEvent eventData,
            EventPublishOptions options = null,
            CancellationToken cancellationToken = default)
            where TEvent : IServiceEvent;

        /// <summary>
        /// Subscribes to events from other services
        /// </summary>
        Task<IDisposable> SubscribeAsync<TEvent>(
            Func<TEvent, Task> handler,
            EventSubscriptionOptions options = null,
            CancellationToken cancellationToken = default)
            where TEvent : IServiceEvent;

        /// <summary>
        /// Broadcasts a message to multiple services
        /// </summary>
        Task<IEnumerable<TResponse>> BroadcastAsync<TRequest, TResponse>(
            IEnumerable<string> targetServiceIds,
            TRequest request,
            BroadcastOptions options = null,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Creates a service proxy for type-safe communication
        /// </summary>
        TService CreateProxy<TService>(string serviceId, ProxyOptions options = null)
            where TService : class;

        /// <summary>
        /// Registers a service handler
        /// </summary>
        Task RegisterHandlerAsync<TRequest, TResponse>(
            Func<TRequest, Task<TResponse>> handler,
            HandlerOptions options = null,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Creates a communication channel
        /// </summary>
        IServiceChannel CreateChannel(string targetServiceId, ChannelOptions options = null);
    }

    /// <summary>
    /// Service communication channel for bi-directional communication
    /// </summary>
    public interface IServiceChannel : IDisposable
    {
        string ChannelId { get; }
        string SourceServiceId { get; }
        string TargetServiceId { get; }
        ChannelState State { get; }
        
        Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
        
        Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class;
        
        Task<IAsyncEnumerable<TMessage>> ReceiveAsync<TMessage>(CancellationToken cancellationToken = default)
            where TMessage : class;
        
        Task OpenAsync(CancellationToken cancellationToken = default);
        Task CloseAsync(CancellationToken cancellationToken = default);
        
        event EventHandler<ChannelStateChangedEventArgs> StateChanged;
        event EventHandler<ChannelErrorEventArgs> Error;
    }

    /// <summary>
    /// Channel state
    /// </summary>
    public enum ChannelState
    {
        Created,
        Opening,
        Open,
        Closing,
        Closed,
        Faulted
    }

    /// <summary>
    /// Service event interface
    /// </summary>
    public interface IServiceEvent
    {
        string EventId { get; }
        string EventType { get; }
        DateTime Timestamp { get; }
        string SourceServiceId { get; }
        Dictionary<string, string> Metadata { get; }
    }

    /// <summary>
    /// Base service event implementation
    /// </summary>
    public abstract class ServiceEventBase : IServiceEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public abstract string EventType { get; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SourceServiceId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Communication options
    /// </summary>
    public class CommunicationOptions
    {
        public TimeSpan? Timeout { get; set; }
        public int? RetryCount { get; set; }
        public TimeSpan? RetryDelay { get; set; }
        public bool? UseCircuitBreaker { get; set; }
        public bool? EnableCompression { get; set; }
        public bool? EnableEncryption { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public CommunicationPattern Pattern { get; set; } = CommunicationPattern.RequestResponse;
        public SerializationFormat Format { get; set; } = SerializationFormat.Json;
        public QualityOfService Qos { get; set; } = QualityOfService.AtLeastOnce;
    }

    /// <summary>
    /// Communication patterns
    /// </summary>
    public enum CommunicationPattern
    {
        RequestResponse,
        FireAndForget,
        PublishSubscribe,
        Streaming,
        Callback
    }

    /// <summary>
    /// Serialization formats
    /// </summary>
    public enum SerializationFormat
    {
        Json,
        MessagePack,
        Protobuf,
        Binary,
        Xml
    }

    /// <summary>
    /// Quality of service levels
    /// </summary>
    public enum QualityOfService
    {
        AtMostOnce,
        AtLeastOnce,
        ExactlyOnce
    }

    /// <summary>
    /// Event publish options
    /// </summary>
    public class EventPublishOptions
    {
        public bool Persistent { get; set; } = true;
        public TimeSpan? Expiration { get; set; }
        public int? Priority { get; set; }
        public string RoutingKey { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    /// <summary>
    /// Event subscription options
    /// </summary>
    public class EventSubscriptionOptions
    {
        public string SubscriptionId { get; set; }
        public bool Durable { get; set; } = true;
        public string Filter { get; set; }
        public int? MaxConcurrency { get; set; }
        public TimeSpan? MessageTimeout { get; set; }
        public bool AutoAcknowledge { get; set; } = true;
        public RetryPolicy RetryPolicy { get; set; }
    }

    /// <summary>
    /// Retry policy
    /// </summary>
    public class RetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
        public double BackoffMultiplier { get; set; } = 2.0;
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Broadcast options
    /// </summary>
    public class BroadcastOptions : CommunicationOptions
    {
        public bool WaitForAllResponses { get; set; } = true;
        public bool ContinueOnError { get; set; } = true;
        public int? MinimumResponses { get; set; }
        public TimeSpan? AggregationTimeout { get; set; }
    }

    /// <summary>
    /// Proxy options
    /// </summary>
    public class ProxyOptions
    {
        public bool EnableCaching { get; set; } = true;
        public TimeSpan? CacheDuration { get; set; }
        public bool EnableMetrics { get; set; } = true;
        public bool EnableTracing { get; set; } = true;
        public IServiceInterceptor[] Interceptors { get; set; }
    }

    /// <summary>
    /// Service interceptor for cross-cutting concerns
    /// </summary>
    public interface IServiceInterceptor
    {
        Task<object> InterceptAsync(InterceptionContext context, Func<Task<object>> next);
    }

    /// <summary>
    /// Interception context
    /// </summary>
    public class InterceptionContext
    {
        public string ServiceId { get; set; }
        public string MethodName { get; set; }
        public object[] Arguments { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Handler options
    /// </summary>
    public class HandlerOptions
    {
        public string HandlerId { get; set; }
        public int? MaxConcurrency { get; set; }
        public TimeSpan? MessageTimeout { get; set; }
        public bool EnableMetrics { get; set; } = true;
        public bool EnableTracing { get; set; } = true;
    }

    /// <summary>
    /// Channel options
    /// </summary>
    public class ChannelOptions
    {
        public ChannelType Type { get; set; } = ChannelType.Bidirectional;
        public int? BufferSize { get; set; }
        public TimeSpan? KeepAliveInterval { get; set; }
        public bool EnableCompression { get; set; }
        public bool EnableEncryption { get; set; }
    }

    /// <summary>
    /// Channel types
    /// </summary>
    public enum ChannelType
    {
        Unidirectional,
        Bidirectional,
        Streaming,
        Multiplex
    }

    /// <summary>
    /// Channel state changed event args
    /// </summary>
    public class ChannelStateChangedEventArgs : EventArgs
    {
        public ChannelState OldState { get; set; }
        public ChannelState NewState { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Channel error event args
    /// </summary>
    public class ChannelErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public string Message { get; set; }
        public bool IsFatal { get; set; }
    }
}