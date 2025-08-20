using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.AI.PatternRecognition.Models
{
    /// <summary>
    /// Types of patterns that can be analyzed.
    /// </summary>
    public enum PatternType
    {
        Unknown,
        Trend,
        Seasonal,
        Anomaly,
        Cyclic,
        Irregular,
        Fraud,
        Behavioral,
        Sequence,
        Network
    }

    /// <summary>
    /// Result of pattern analysis.
    /// </summary>
    public class PatternAnalysisResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public PatternType Type { get; set; }
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DetectedPattern[] Patterns { get; set; } = Array.Empty<DetectedPattern>();
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public string AnalysisId => Id; // Alias for Id
        public string? ModelId { get; set; }
        public Dictionary<string, object>? InputData { get; set; }
        public List<DetectedPattern> DetectedPatterns => Patterns?.ToList() ?? new List<DetectedPattern>();
        public double ConfidenceScore => Confidence;
        public int PatternsFound => Patterns?.Length ?? 0;
        public Dictionary<string, object> AnalysisMetrics { get; set; } = new();
        public Dictionary<string, object> TemporalAnalysis { get; set; } = new();
        public Dictionary<string, object> NetworkAnalysis { get; set; } = new();
        public Dictionary<string, object> ProcessingMetrics { get; set; } = new();
    }

    /// <summary>
    /// Request for pattern analysis.
    /// </summary>
    public class PatternAnalysisRequest
    {
        public double[] Data { get; set; } = Array.Empty<double>();
        public PatternType? PreferredType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string? Context { get; set; }
        public double MinConfidence { get; set; } = 0.5;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? ModelId { get; set; }
        public Dictionary<string, object> InputData { get; set; } = new();
    }

    /// <summary>
    /// Interface for metrics collection.
    /// </summary>
    public interface IMetricsCollector
    {
        void RecordMetric(string name, double value);
        void RecordEvent(string name, Dictionary<string, object> properties);
        Task RecordMetricAsync(string name, double value, Dictionary<string, object> tags);
    }

    /// <summary>
    /// Interface for pattern analyzers.
    /// </summary>
    public interface IPatternAnalyzer
    {
        PatternType SupportedType { get; }
        Task<PatternAnalysisResult> AnalyzeAsync(PatternAnalysisRequest request);
        Task<TrainingResult> TrainAsync(TrainingData data);
        double ConfidenceThreshold { get; }
    }

    /// <summary>
    /// Individual training sample.
    /// </summary>
    public class TrainingSample
    {
        public double[] Data { get; set; } = Array.Empty<double>();
        public string Label { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Training data for pattern recognition models.
    /// </summary>
    public class TrainingData
    {
        public List<TrainingSample> Samples { get; set; } = new();
        public List<string> Labels { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Result of training a pattern recognition model.
    /// </summary>
    public class TrainingResult
    {
        public bool Success { get; set; }
        public double Accuracy { get; set; }
        public int TrainingSamples { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Represents a detected pattern.
    /// </summary>
    public class DetectedPattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public PatternType Type { get; set; }
        public double Confidence { get; set; }
        public double MatchScore { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Baseline statistics for pattern analysis.
    /// </summary>
    public class BaselineStatistics
    {
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
        public double Median { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Q1 { get; set; }
        public double Q3 { get; set; }
        public int Count { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
