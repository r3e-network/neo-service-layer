using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Infrastructure.EventSourcing
{
    /// <summary>
    /// Configuration for event store
    /// </summary>
    public class EventStoreConfiguration
    {
        public const string SectionName = "EventStore";

        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        [Range(1, 10000)]
        public int BatchSize { get; set; } = 100;

        [Range(1, 300)]
        public int CommandTimeout { get; set; } = 30;

        public bool EnableSnapshots { get; set; } = true;

        [Range(10, 10000)]
        public int SnapshotFrequency { get; set; } = 100;
    }

    /// <summary>
    /// Configuration for event bus
    /// </summary>
    public class EventBusConfiguration
    {
        public const string SectionName = "EventBus";

        [Required]
        public string HostName { get; set; } = "localhost";

        [Range(1, 65535)]
        public int Port { get; set; } = 5672;

        [Required]
        public string UserName { get; set; } = "guest";

        [Required]
        public string Password { get; set; } = "guest";

        public string VirtualHost { get; set; } = "/";

        [Required]
        public string ExchangeName { get; set; } = "neo_events";

        [Range(1, 60)]
        public int RetryCount { get; set; } = 3;

        [Range(100, 30000)]
        public int RetryDelayMs { get; set; } = 1000;

        public bool EnableDeadLetterQueue { get; set; } = true;

        public string DeadLetterExchange { get; set; } = "neo_events_dlx";

        [Range(1, 3600)]
        public int MessageTtlSeconds { get; set; } = 300;
    }

    /// <summary>
    /// Configuration for event processing
    /// </summary>
    public class EventProcessingConfiguration
    {
        public const string SectionName = "EventProcessing";

        [Range(1, 100)]
        public int MaxConcurrentHandlers { get; set; } = 10;

        [Range(1, 60)]
        public int HandlerTimeoutSeconds { get; set; } = 30;

        [Range(1, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        [Range(100, 10000)]
        public int RetryDelayMs { get; set; } = 1000;

        public bool EnableMetrics { get; set; } = true;

        public bool EnableTracing { get; set; } = true;

        [Range(1, 1000)]
        public int ProcessingBatchSize { get; set; } = 50;
    }
}