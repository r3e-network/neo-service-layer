using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents pattern recognition type.
/// </summary>
public enum PatternRecognitionType
{
    /// <summary>
    /// Fraud detection.
    /// </summary>
    FraudDetection,

    /// <summary>
    /// Anomaly detection.
    /// </summary>
    AnomalyDetection,

    /// <summary>
    /// Behavioral analysis.
    /// </summary>
    BehavioralAnalysis,

    /// <summary>
    /// Network analysis.
    /// </summary>
    NetworkAnalysis,

    /// <summary>
    /// Temporal pattern analysis.
    /// </summary>
    TemporalPattern,

    /// <summary>
    /// Statistical pattern analysis.
    /// </summary>
    StatisticalPattern,

    /// <summary>
    /// Sequence pattern analysis.
    /// </summary>
    SequencePattern,

    /// <summary>
    /// Clustering analysis.
    /// </summary>
    ClusteringAnalysis,

    /// <summary>
    /// Behavior analysis.
    /// </summary>
    BehaviorAnalysis,

    /// <summary>
    /// Classification analysis.
    /// </summary>
    Classification,

    /// <summary>
    /// Clustering analysis.
    /// </summary>
    Clustering,

    /// <summary>
    /// Regression analysis.
    /// </summary>
    Regression
}

/// <summary>
/// Represents risk levels.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Minimal risk.
    /// </summary>
    Minimal,

    /// <summary>
    /// Low risk.
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk.
    /// </summary>
    Medium,

    /// <summary>
    /// High risk.
    /// </summary>
    High,

    /// <summary>
    /// Critical risk.
    /// </summary>
    Critical
}

/// <summary>
/// Represents anomaly types.
/// </summary>
public enum AnomalyType
{
    /// <summary>
    /// Statistical outlier.
    /// </summary>
    StatisticalOutlier,

    /// <summary>
    /// Temporal anomaly.
    /// </summary>
    TemporalAnomaly,

    /// <summary>
    /// Behavioral anomaly.
    /// </summary>
    BehavioralAnomaly,

    /// <summary>
    /// Network anomaly.
    /// </summary>
    NetworkAnomaly,

    /// <summary>
    /// Pattern anomaly.
    /// </summary>
    PatternAnomaly,

    /// <summary>
    /// High value anomaly.
    /// </summary>
    HighValue,

    /// <summary>
    /// Low value anomaly.
    /// </summary>
    LowValue,

    /// <summary>
    /// General outlier.
    /// </summary>
    Outlier,

    /// <summary>
    /// Statistical anomaly.
    /// </summary>
    Statistical
}

/// <summary>
/// Represents a pattern recognition model.
/// </summary>
public class PatternModel : AIModel
{
    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public PatternRecognitionType PatternType { get; set; }

    /// <summary>
    /// Gets or sets the detection threshold.
    /// </summary>
    public double DetectionThreshold { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets the pattern templates.
    /// </summary>
    public List<PatternTemplate> Templates { get; set; } = new();

    /// <summary>
    /// Gets or sets the feature extractors.
    /// </summary>
    public List<string> FeatureExtractors { get; set; } = new();

    /// <summary>
    /// Gets or sets the model accuracy.
    /// </summary>
    public new double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets when the model was last trained.
    /// </summary>
    public DateTime LastTrained { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the trained model data.
    /// </summary>
    public byte[] TrainedModel { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the training data.
    /// </summary>
    public Dictionary<string, object> TrainingData { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the algorithm used by this model.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input features for this model.
    /// </summary>
    public new List<string> InputFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the output targets for this model.
    /// </summary>
    public List<string> OutputTargets { get; set; } = new();
}

/// <summary>
/// Represents pattern model definition.
/// </summary>
public class PatternModelDefinition : AIModelDefinition
{
    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public PatternRecognitionType PatternType { get; set; }

    /// <summary>
    /// Gets or sets the detection algorithms.
    /// </summary>
    public List<string> DetectionAlgorithms { get; set; } = new();

    /// <summary>
    /// Gets or sets the feature extraction methods.
    /// </summary>
    public List<string> FeatureExtractionMethods { get; set; } = new();

    /// <summary>
    /// Gets or sets the anomaly detection configuration.
    /// </summary>
    public AnomalyDetectionConfig AnomalyConfig { get; set; } = new();

    /// <summary>
    /// Gets or sets the output targets for this model.
    /// </summary>
    public List<string> OutputTargets { get; set; } = new();

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public new string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Represents pattern template.
/// </summary>
public class PatternTemplate
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public PatternRecognitionType Type { get; set; }

    /// <summary>
    /// Gets or sets the template parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents anomaly detection configuration.
/// </summary>
public class AnomalyDetectionConfig
{
    /// <summary>
    /// Gets or sets the contamination rate (expected proportion of outliers).
    /// </summary>
    public double Contamination { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the number of estimators for ensemble methods.
    /// </summary>
    public int NumberOfEstimators { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of samples to use.
    /// </summary>
    public int MaxSamples { get; set; } = 256;

    /// <summary>
    /// Gets or sets the random state for reproducibility.
    /// </summary>
    public int? RandomState { get; set; }

    /// <summary>
    /// Gets or sets the threshold for anomaly detection.
    /// </summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets whether to use feature scaling.
    /// </summary>
    public bool UseFeatureScaling { get; set; } = true;

    /// <summary>
    /// Gets or sets the feature scaling method.
    /// </summary>
    public string FeatureScalingMethod { get; set; } = "StandardScaler";

    /// <summary>
    /// Gets or sets additional configuration parameters.
    /// </summary>
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
}
