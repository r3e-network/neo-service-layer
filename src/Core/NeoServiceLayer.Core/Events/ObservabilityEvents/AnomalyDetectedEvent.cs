using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Events.ObservabilityEvents
{
    /// <summary>
    /// Event raised when an anomaly is detected in system metrics or behavior
    /// </summary>
    public class AnomalyDetectedEvent : DomainEventBase
    {
        public AnomalyDetectedEvent(
            string aggregateId,
            long aggregateVersion,
            string initiatedBy,
            string anomalyType,
            string serviceName,
            string metricName,
            double currentValue,
            double expectedValue,
            double deviationScore,
            string detectionAlgorithm,
            Dictionary&lt;string, object&gt; additionalContext,
            Guid? causationId = null,
            Guid? correlationId = null)
            : base(aggregateId, "ObservabilityService", aggregateVersion, initiatedBy, causationId, correlationId)
        {
            AnomalyType = anomalyType ?? throw new ArgumentNullException(nameof(anomalyType));
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            MetricName = metricName ?? throw new ArgumentNullException(nameof(metricName));
            CurrentValue = currentValue;
            ExpectedValue = expectedValue;
            DeviationScore = deviationScore;
            DetectionAlgorithm = detectionAlgorithm ?? throw new ArgumentNullException(nameof(detectionAlgorithm));
            AdditionalContext = additionalContext ?? new Dictionary&lt;string, object&gt;();

            AddMetadata("severity", GetSeverityLevel(deviationScore));
            AddMetadata("impact_category", GetImpactCategory(anomalyType));
            AddMetadata("requires_investigation", deviationScore &gt; 2.0);
        }

        /// <summary>
        /// Type of anomaly detected
        /// </summary>
        public string AnomalyType { get; }

        /// <summary>
        /// Name of the service where anomaly was detected
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        /// Name of the metric showing anomalous behavior
        /// </summary>
        public string MetricName { get; }

        /// <summary>
        /// Current metric value
        /// </summary>
        public double CurrentValue { get; }

        /// <summary>
        /// Expected metric value based on historical patterns
        /// </summary>
        public double ExpectedValue { get; }

        /// <summary>
        /// Statistical deviation score (standard deviations from normal)
        /// </summary>
        public double DeviationScore { get; }

        /// <summary>
        /// Algorithm used to detect the anomaly
        /// </summary>
        public string DetectionAlgorithm { get; }

        /// <summary>
        /// Additional context information
        /// </summary>
        public Dictionary&lt;string, object&gt; AdditionalContext { get; }

        private static string GetSeverityLevel(double deviationScore)
        {
            return Math.Abs(deviationScore) switch
            {
                &gt;= 4.0 =&gt; "critical",
                &gt;= 3.0 =&gt; "high",
                &gt;= 2.0 =&gt; "medium",
                &gt;= 1.0 =&gt; "low",
                _ =&gt; "info"
            };
        }

        private static string GetImpactCategory(string anomalyType)
        {
            return anomalyType.ToLowerInvariant() switch
            {
                var t when t.Contains("response_time") => "performance",
                var t when t.Contains("error_rate") => "reliability",
                var t when t.Contains("throughput") => "capacity",
                var t when t.Contains("memory") => "resource",
                var t when t.Contains("cpu") => "resource",
                _ => "operational"
            };
        }
    }
}