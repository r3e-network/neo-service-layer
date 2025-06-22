using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Core.Models;

#region Pattern Recognition Models

/// <summary>
/// Represents a fraud detection request.
/// </summary>
public class FraudDetectionRequest
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction data to analyze.
    /// </summary>
    [Required]
    public Dictionary<string, object> TransactionData { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the detection sensitivity level.
    /// </summary>
    public DetectionSensitivity Sensitivity { get; set; } = DetectionSensitivity.Standard;

    /// <summary>
    /// Gets or sets the detection threshold.
    /// </summary>
    public double Threshold { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets whether to include historical analysis.
    /// </summary>
    public bool IncludeHistoricalAnalysis { get; set; } = true;
}

/// <summary>
/// Represents a fraud detection result.
/// </summary>
public class FraudDetectionResult
{
    /// <summary>
    /// Gets or sets the detection ID.
    /// </summary>
    public string DetectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fraud risk score (0-1).
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets whether fraud was detected.
    /// </summary>
    public bool IsFraudulent { get; set; }

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the detected fraud patterns.
    /// </summary>
    public List<FraudPattern> DetectedPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public Dictionary<string, double> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets when the detection was performed.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional analysis details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Represents an anomaly detection request.
/// </summary>
public class AnomalyDetectionRequest
{
    /// <summary>
    /// Gets or sets the data to analyze for anomalies.
    /// </summary>
    [Required]
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the detection parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the anomaly threshold.
    /// </summary>
    public double Threshold { get; set; } = 0.95;

    /// <summary>
    /// Gets or sets the analysis window size.
    /// </summary>
    public int WindowSize { get; set; } = 100;
}

/// <summary>
/// Represents an anomaly detection result.
/// </summary>
public class AnomalyDetectionResult
{
    /// <summary>
    /// Gets or sets the detection ID.
    /// </summary>
    public string DetectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the anomaly score.
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Gets or sets whether an anomaly was detected.
    /// </summary>
    public bool IsAnomalous { get; set; }

    /// <summary>
    /// Gets or sets the detected anomalies.
    /// </summary>
    public List<Anomaly> DetectedAnomalies { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets when the detection was performed.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional analysis details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Represents a classification request.
/// </summary>
public class ClassificationRequest
{
    /// <summary>
    /// Gets or sets the data to classify.
    /// </summary>
    [Required]
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the classification model ID.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the classification parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include confidence scores.
    /// </summary>
    public bool IncludeConfidenceScores { get; set; } = true;
}

/// <summary>
/// Represents a classification result.
/// </summary>
public class ClassificationResult
{
    /// <summary>
    /// Gets or sets the classification ID.
    /// </summary>
    public string ClassificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the predicted class.
    /// </summary>
    public string PredictedClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the class probabilities.
    /// </summary>
    public Dictionary<string, double> ClassProbabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets when the classification was performed.
    /// </summary>
    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional classification details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Represents a fraud pattern.
/// </summary>
public class FraudPattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string PatternId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern severity.
    /// </summary>
    public PatternSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the pattern confidence.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the pattern attributes.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Represents an anomaly.
/// </summary>
public class Anomaly
{
    /// <summary>
    /// Gets or sets the anomaly ID.
    /// </summary>
    public string AnomalyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the anomaly type.
    /// </summary>
    public AnomalyType Type { get; set; }

    /// <summary>
    /// Gets or sets the anomaly score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the anomaly description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected data points.
    /// </summary>
    public List<string> AffectedDataPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets when the anomaly was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents detection sensitivity levels.
/// </summary>
public enum DetectionSensitivity
{
    /// <summary>
    /// Low sensitivity - fewer false positives.
    /// </summary>
    Low,

    /// <summary>
    /// Standard sensitivity.
    /// </summary>
    Standard,

    /// <summary>
    /// High sensitivity - more thorough detection.
    /// </summary>
    High,

    /// <summary>
    /// Maximum sensitivity - highest detection rate.
    /// </summary>
    Maximum
}

/// <summary>
/// Represents pattern severity levels.
/// </summary>
public enum PatternSeverity
{
    /// <summary>
    /// Low severity pattern.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity pattern.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity pattern.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity pattern.
    /// </summary>
    Critical
}

/// <summary>
/// Represents anomaly types.
/// </summary>
public enum AnomalyType
{
    /// <summary>
    /// Statistical anomaly.
    /// </summary>
    Statistical,

    /// <summary>
    /// Behavioral anomaly.
    /// </summary>
    Behavioral,

    /// <summary>
    /// Temporal anomaly.
    /// </summary>
    Temporal,

    /// <summary>
    /// Contextual anomaly.
    /// </summary>
    Contextual,

    /// <summary>
    /// Collective anomaly.
    /// </summary>
    Collective
}

#endregion
