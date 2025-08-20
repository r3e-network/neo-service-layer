using System;
using System.Collections.Generic;

namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Core models for pattern recognition.
/// </summary>
public static class CoreModels
{
    /// <summary>
    /// Represents a detected anomaly.
    /// </summary>
    public class Anomaly
    {
        /// <summary>
        /// Gets or sets the anomaly ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the anomaly type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the anomaly description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity score (0-1).
        /// </summary>
        public double Severity { get; set; }

        /// <summary>
        /// Gets or sets the confidence score (0-1).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Gets or sets when the anomaly was detected.
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI model types.
    /// </summary>
    public enum AIModelType
    {
        PatternRecognition,
        AnomalyDetection,
        FraudDetection,
        BehaviorAnalysis
    }

    // AnomalyDetectionRequest is defined in RequestModels.cs
}
