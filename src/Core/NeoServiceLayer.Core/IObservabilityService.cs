using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Interface for observability service operations.
    /// </summary>
    public interface IObservabilityService : IService
    {
        /// <summary>
        /// Records a metric.
        /// </summary>
        Task RecordMetricAsync(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records an event.
        /// </summary>
        Task RecordEventAsync(string name, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Starts a trace span.
        /// </summary>
        Task<ITraceSpan> StartSpanAsync(string operationName);

        /// <summary>
        /// Gets current metrics.
        /// </summary>
        new Task<Dictionary<string, double>> GetMetricsAsync();

        /// <summary>
        /// Gets health status.
        /// </summary>
        Task<ObservabilityHealthStatus> GetHealthStatusAsync();

        /// <summary>
        /// Starts an activity for tracing.
        /// </summary>
        IActivity? StartActivity(string name);

        /// <summary>
        /// Completes an activity.
        /// </summary>
        void CompleteActivity(IActivity? activity, bool success, string? message = null);

        /// <summary>
        /// Sets the health status of a component.
        /// </summary>
        void SetHealthStatus(string component, bool isHealthy, string message);
    }

    /// <summary>
    /// Represents an activity for tracing.
    /// </summary>
    public interface IActivity : IDisposable
    {
        /// <summary>
        /// Gets the activity name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Sets a tag on the activity.
        /// </summary>
        void SetTag(string key, string value);

        /// <summary>
        /// Adds an event to the activity.
        /// </summary>
        void AddEvent(string name);
    }

    /// <summary>
    /// Represents a trace span.
    /// </summary>
    public interface ITraceSpan
    {
        /// <summary>
        /// Sets a tag on the span.
        /// </summary>
        void SetTag(string key, string value);

        /// <summary>
        /// Logs an event to the span.
        /// </summary>
        void Log(string message);

        /// <summary>
        /// Ends the span.
        /// </summary>
        void End();
    }

    /// <summary>
    /// Represents the health status of observability components.
    /// </summary>
    public class ObservabilityHealthStatus
    {
        /// <summary>
        /// Gets or sets whether the service is healthy.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the health status message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component health details.
        /// </summary>
        public Dictionary<string, bool> ComponentHealth { get; set; } = new();
    }
}