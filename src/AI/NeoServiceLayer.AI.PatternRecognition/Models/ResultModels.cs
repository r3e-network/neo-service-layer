namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents pattern analysis result.
/// </summary>
public class PatternAnalysisResult
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID used.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input data that was analyzed.
    /// </summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Gets or sets the detected patterns.
    /// </summary>
    public List<DetectedPattern> DetectedPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the analysis was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the pattern type that was analyzed.
    /// </summary>
    public PatternRecognitionType PatternType { get; set; }

    /// <summary>
    /// Gets or sets the number of patterns found.
    /// </summary>
    public int PatternsFound { get; set; }

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the analysis metrics.
    /// </summary>
    public Dictionary<string, double> AnalysisMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the temporal analysis results.
    /// </summary>
    public Dictionary<string, object> TemporalAnalysis { get; set; } = new();

    /// <summary>
    /// Gets or sets the network analysis results.
    /// </summary>
    public Dictionary<string, object> NetworkAnalysis { get; set; } = new();

    /// <summary>
    /// Gets or sets the processing metrics.
    /// </summary>
    public Dictionary<string, double> ProcessingMetrics { get; set; } = new();
}

/// <summary>
/// Represents anomaly detection result.
/// </summary>
public class AnomalyDetectionResult
{
    /// <summary>
    /// Gets or sets the detection ID.
    /// </summary>
    public string DetectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the anomaly ID.
    /// </summary>
    public string AnomalyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detected anomalies.
    /// </summary>
    public List<DetectedAnomaly> Anomalies { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall anomaly score.
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Gets or sets when the detection was performed.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the detection was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the anomaly type.
    /// </summary>
    public AnomalyType AnomalyType { get; set; }
}

/// <summary>
/// Represents classification result.
/// </summary>
public class ClassificationResult
{
    /// <summary>
    /// Gets or sets the classification ID.
    /// </summary>
    public string ClassificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID used for classification.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input data that was classified.
    /// </summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Gets or sets the classification result.
    /// </summary>
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets when the classification was performed.
    /// </summary>
    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the classification was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents risk assessment result.
/// </summary>
public class RiskAssessmentResult
{
    /// <summary>
    /// Gets or sets the assessment ID.
    /// </summary>
    public string AssessmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity ID that was assessed.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall risk score.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the overall risk score (alias).
    /// </summary>
    public double OverallRiskScore
    {
        get => RiskScore;
        set => RiskScore = value;
    }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public Dictionary<string, double> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the assessment timestamp.
    /// </summary>
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the assessment was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk breakdown by category.
    /// </summary>
    public Dictionary<string, double> RiskBreakdown { get; set; } = new();

    /// <summary>
    /// Gets or sets the mitigating factors.
    /// </summary>
    public Dictionary<string, double> MitigatingFactors { get; set; } = new();
}

/// <summary>
/// Represents behavior analysis result.
/// </summary>
public class BehaviorAnalysisResult
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address that was analyzed.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the behavior profile.
    /// </summary>
    public BehaviorProfile BehaviorProfile { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk score.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the analysis was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the user ID associated with the behavior analysis.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall behavior score.
    /// </summary>
    public double BehaviorScore { get; set; }

    /// <summary>
    /// Gets or sets whether this is a new user profile.
    /// </summary>
    public bool IsNewUserProfile { get; set; }

    /// <summary>
    /// Gets or sets the behavior patterns detected.
    /// </summary>
    public List<string> BehaviorPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk factors identified.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the deviation from the normal profile.
    /// </summary>
    public double DeviationFromProfile { get; set; }

    /// <summary>
    /// Gets or sets the recommendations based on the analysis.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the alert level based on the analysis.
    /// </summary>
    public AlertLevel AlertLevel { get; set; }
}

/// <summary>
/// Represents fraud detection result.
/// </summary>
public class FraudDetectionResult
{
    /// <summary>
    /// Gets or sets the detection ID.
    /// </summary>
    public string DetectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fraud score.
    /// </summary>
    public double FraudScore { get; set; }

    /// <summary>
    /// Gets or sets whether the transaction is fraudulent.
    /// </summary>
    public bool IsFraudulent { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public Dictionary<string, double> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets when the detection was performed.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the detection was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

