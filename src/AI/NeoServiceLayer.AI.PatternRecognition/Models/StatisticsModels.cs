namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents classification statistics.
/// </summary>
public class ClassificationStatistics
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time range for these statistics.
    /// </summary>
    public TimeRange TimeRange { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of classifications.
    /// </summary>
    public int TotalClassifications { get; set; }

    /// <summary>
    /// Gets or sets the number of successful classifications.
    /// </summary>
    public int SuccessfulClassifications { get; set; }

    /// <summary>
    /// Gets or sets the average confidence score.
    /// </summary>
    public double AverageConfidence { get; set; }

    /// <summary>
    /// Gets or sets the classification distribution.
    /// </summary>
    public Dictionary<string, int> ClassificationDistribution { get; set; } = new();

    /// <summary>
    /// Gets or sets when these statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the success rate.
    /// </summary>
    public double SuccessRate => TotalClassifications > 0 ? (double)SuccessfulClassifications / TotalClassifications : 0.0;
}

/// <summary>
/// Represents risk assessment statistics.
/// </summary>
public class RiskAssessmentStatistics
{
    /// <summary>
    /// Gets or sets the time range for these statistics.
    /// </summary>
    public TimeRange TimeRange { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of assessments.
    /// </summary>
    public int TotalAssessments { get; set; }

    /// <summary>
    /// Gets or sets the number of successful assessments.
    /// </summary>
    public int SuccessfulAssessments { get; set; }

    /// <summary>
    /// Gets or sets the average risk score.
    /// </summary>
    public double AverageRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level distribution.
    /// </summary>
    public Dictionary<string, int> RiskLevelDistribution { get; set; } = new();

    /// <summary>
    /// Gets or sets when these statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the success rate.
    /// </summary>
    public double SuccessRate => TotalAssessments > 0 ? (double)SuccessfulAssessments / TotalAssessments : 0.0;
}

/// <summary>
/// Represents anomaly detection statistics.
/// </summary>
public class AnomalyDetectionStatistics
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time range for these statistics.
    /// </summary>
    public TimeRange TimeRange { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of detections.
    /// </summary>
    public int TotalDetections { get; set; }

    /// <summary>
    /// Gets or sets the number of successful detections.
    /// </summary>
    public int SuccessfulDetections { get; set; }

    /// <summary>
    /// Gets or sets the total number of anomalies detected.
    /// </summary>
    public int TotalAnomalies { get; set; }

    /// <summary>
    /// Gets or sets the average anomaly score.
    /// </summary>
    public double AverageAnomalyScore { get; set; }

    /// <summary>
    /// Gets or sets the anomaly type distribution.
    /// </summary>
    public Dictionary<string, int> AnomalyTypeDistribution { get; set; } = new();

    /// <summary>
    /// Gets or sets when these statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the success rate.
    /// </summary>
    public double SuccessRate => TotalDetections > 0 ? (double)SuccessfulDetections / TotalDetections : 0.0;

    /// <summary>
    /// Gets the anomaly rate.
    /// </summary>
    public double AnomalyRate => TotalDetections > 0 ? (double)TotalAnomalies / TotalDetections : 0.0;
}

/// <summary>
/// Represents pattern analysis statistics.
/// </summary>
public class PatternAnalysisStatistics
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time range for these statistics.
    /// </summary>
    public TimeRange TimeRange { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of analyses.
    /// </summary>
    public int TotalAnalyses { get; set; }

    /// <summary>
    /// Gets or sets the number of successful analyses.
    /// </summary>
    public int SuccessfulAnalyses { get; set; }

    /// <summary>
    /// Gets or sets the total number of patterns detected.
    /// </summary>
    public int TotalPatternsDetected { get; set; }

    /// <summary>
    /// Gets or sets the average confidence score.
    /// </summary>
    public double AverageConfidence { get; set; }

    /// <summary>
    /// Gets or sets the pattern type distribution.
    /// </summary>
    public Dictionary<string, int> PatternTypeDistribution { get; set; } = new();

    /// <summary>
    /// Gets or sets when these statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the success rate.
    /// </summary>
    public double SuccessRate => TotalAnalyses > 0 ? (double)SuccessfulAnalyses / TotalAnalyses : 0.0;

    /// <summary>
    /// Gets the average patterns per analysis.
    /// </summary>
    public double AveragePatternsPerAnalysis => TotalAnalyses > 0 ? (double)TotalPatternsDetected / TotalAnalyses : 0.0;
}

/// <summary>
/// Represents model performance metrics.
/// </summary>
public class ModelPerformanceMetrics
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accuracy score.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the precision score.
    /// </summary>
    public double Precision { get; set; }

    /// <summary>
    /// Gets or sets the recall score.
    /// </summary>
    public double Recall { get; set; }

    /// <summary>
    /// Gets or sets the F1 score.
    /// </summary>
    public double F1Score { get; set; }

    /// <summary>
    /// Gets or sets the area under the ROC curve.
    /// </summary>
    public double AucRoc { get; set; }

    /// <summary>
    /// Gets or sets the confusion matrix.
    /// </summary>
    public Dictionary<string, Dictionary<string, int>> ConfusionMatrix { get; set; } = new();

    /// <summary>
    /// Gets or sets the feature importance scores.
    /// </summary>
    public Dictionary<string, double> FeatureImportance { get; set; } = new();

    /// <summary>
    /// Gets or sets the training time in milliseconds.
    /// </summary>
    public long TrainingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the inference time in milliseconds.
    /// </summary>
    public long InferenceTimeMs { get; set; }

    /// <summary>
    /// Gets or sets when these metrics were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metrics.
    /// </summary>
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Represents model training status.
/// </summary>
public class ModelTrainingStatus
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the training status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the model was last trained.
    /// </summary>
    public DateTime LastTrained { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the training duration.
    /// </summary>
    public TimeSpan TrainingDuration { get; set; }

    /// <summary>
    /// Gets or sets the current accuracy.
    /// </summary>
    public double CurrentAccuracy { get; set; }

    /// <summary>
    /// Gets or sets whether the model is currently training.
    /// </summary>
    public bool IsTraining { get; set; }

    /// <summary>
    /// Gets or sets the training progress percentage.
    /// </summary>
    public double TrainingProgress { get; set; }
}

/// <summary>
/// Represents model validation result.
/// </summary>
public class ModelValidationResult
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the model is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation accuracy.
    /// </summary>
    public double ValidationAccuracy { get; set; }

    /// <summary>
    /// Gets or sets the validation date.
    /// </summary>
    public DateTime ValidationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the validation metrics.
    /// </summary>
    public Dictionary<string, double> ValidationMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();
}
