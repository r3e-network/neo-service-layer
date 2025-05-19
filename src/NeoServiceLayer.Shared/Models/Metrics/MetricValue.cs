using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models.Metrics
{
    /// <summary>
    /// Represents a metric value.
    /// </summary>
    public class MetricValue
    {
        /// <summary>
        /// Gets or sets the name of the metric.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the metric.
        /// </summary>
        public MetricType Type { get; set; }

        /// <summary>
        /// Gets or sets the value of the metric.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the count of values for histogram and timer metrics.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the sum of values for histogram and timer metrics.
        /// </summary>
        public double Sum { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for histogram and timer metrics.
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for histogram and timer metrics.
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the metric.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the tags for the metric.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents the type of a metric.
    /// </summary>
    public enum MetricType
    {
        /// <summary>
        /// A counter metric that can only increase.
        /// </summary>
        Counter,

        /// <summary>
        /// A gauge metric that can increase or decrease.
        /// </summary>
        Gauge,

        /// <summary>
        /// A histogram metric that tracks the distribution of values.
        /// </summary>
        Histogram,

        /// <summary>
        /// A timer metric that tracks the duration of operations.
        /// </summary>
        Timer
    }
}
