using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
// Removed incorrect using statement


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
    /// Trend analysis patterns.
    /// </summary>
    TrendAnalysis,
    
    /// <summary>
    /// Sequence analysis patterns.
    /// </summary>
    SequenceAnalysis,

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
/// Represents activity levels for behavior analysis.
/// </summary>
public enum ActivityLevel
{
    /// <summary>
    /// Very low activity.
    /// </summary>
    VeryLow,

    /// <summary>
    /// Low activity.
    /// </summary>
    Low,

    /// <summary>
    /// Normal activity.
    /// </summary>
    Normal,

    /// <summary>
    /// High activity.
    /// </summary>
    High,

    /// <summary>
    /// Very high activity.
    /// </summary>
    VeryHigh
}

/// <summary>
/// Represents alert levels.
/// </summary>
public enum AlertLevel
{
    /// <summary>
    /// Low alert level.
    /// </summary>
    Low,

    /// <summary>
    /// Medium alert level.
    /// </summary>
    Medium,

    /// <summary>
    /// High alert level.
    /// </summary>
    High,

    /// <summary>
    /// Critical alert level.
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
public class PatternModel
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern type (base property).
    /// </summary>
    public PatternType Type { get; set; }

    /// <summary>
    /// Gets or sets when the model was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the model ID (alias for Id).
    /// </summary>
    public string ModelId 
    { 
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Gets or sets when the model was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public PatternRecognitionType PatternType { get; set; }

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

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
    public double Accuracy { get; set; }

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
    public List<string> InputFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the output targets for this model.
    /// </summary>
    public List<string> OutputTargets { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether the model is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents pattern model definition.
/// </summary>
public class PatternModelDefinition
{
    public Dictionary<string, object> TrainingParameters { get; set; } = new();
    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public PatternRecognitionType PatternType { get; set; }
    
    /// <summary>
    /// Gets or sets the Type as an alias for PatternType.
    /// </summary>
    public PatternRecognitionType Type 
    { 
        get => PatternType; 
        set => PatternType = value; 
    }

    /// <summary>
    /// Gets or sets whether the model is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

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
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration parameters.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the algorithm used.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the input features.
    /// </summary>
    public List<string> InputFeatures { get; set; } = new();
    
    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the training data (compatibility property).
    /// </summary>
    public Dictionary<string, object> TrainingData { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the parameters (compatibility property).
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
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

// DetectedPattern moved to PatternTypes.cs to avoid duplication

/// <summary>
/// Represents the period type for time patterns.
/// </summary>
public enum TimePeriodType
{
    /// <summary>
    /// Hourly patterns.
    /// </summary>
    Hourly = 0,
    
    /// <summary>
    /// Daily patterns.
    /// </summary>
    Daily = 1,
    
    /// <summary>
    /// Weekly patterns.
    /// </summary>
    Weekly = 2,
    
    /// <summary>
    /// Monthly patterns.
    /// </summary>
    Monthly = 3
}

/// <summary>
/// Represents a time-based pattern in behavior analysis.
/// </summary>
public class TimePattern
{
    /// <summary>
    /// Gets or sets the unique identifier for this time pattern.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the name of the pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of time period.
    /// </summary>
    public TimePeriodType PeriodType { get; set; }
    
    /// <summary>
    /// Gets or sets the activity distribution over the period.
    /// </summary>
    public Dictionary<string, double> ActivityDistribution { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the peak activity times.
    /// </summary>
    public List<TimeSpan> PeakActivityTimes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the confidence score for this pattern.
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Gets or sets when this pattern was identified.
    /// </summary>
    public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets additional metadata about the pattern.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a detected anomaly in data analysis.
/// </summary>
public class DetectedAnomaly
{
    /// <summary>
    /// Gets or sets the unique identifier for this anomaly.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of anomaly detected.
    /// </summary>
    public AnomalyType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the index in the data where the anomaly was detected.
    /// </summary>
    public int DataIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the data point index (alias for DataIndex).
    /// </summary>
    public int DataPointIndex
    {
        get => DataIndex;
        set => DataIndex = value;
    }
    
    /// <summary>
    /// Gets or sets the anomaly type (alias for Type).
    /// </summary>
    public AnomalyType AnomalyType
    {
        get => Type;
        set => Type = value;
    }
    
    /// <summary>
    /// Gets or sets the value that was detected as anomalous.
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// Gets or sets the expected value based on the model.
    /// </summary>
    public double ExpectedValue { get; set; }
    
    /// <summary>
    /// Gets or sets the deviation from the expected value.
    /// </summary>
    public double Deviation { get; set; }
    
    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Gets or sets the severity of the anomaly.
    /// </summary>
    public string Severity { get; set; } = "Medium";
    
    /// <summary>
    /// Gets or sets when the anomaly was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets additional metadata about the anomaly.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the description of the anomaly.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a mixing service pattern used in fraud detection.
/// </summary>
public class MixingServicePattern
{
    /// <summary>
    /// Gets or sets the unique identifier for this pattern.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the name of the mixing service pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of mixing service.
    /// </summary>
    public string ServiceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the minimum transaction amount for this pattern.
    /// </summary>
    public decimal MinTransactionAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum transaction amount for this pattern.
    /// </summary>
    public decimal MaxTransactionAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the typical delay between input and output transactions.
    /// </summary>
    public TimeSpan TypicalDelay { get; set; }
    
    /// <summary>
    /// Gets or sets the number of output addresses typically used.
    /// </summary>
    public int TypicalOutputAddresses { get; set; }
    
    /// <summary>
    /// Gets or sets the known mixing service addresses.
    /// </summary>
    public List<string> KnownAddresses { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the confidence threshold for matching this pattern.
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.8;
    
    /// <summary>
    /// Gets or sets the pattern indicators.
    /// </summary>
    public List<string> Indicators { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the typical amounts for this mixing service pattern.
    /// </summary>
    public List<decimal> TypicalAmounts { get; set; } = new();
    
    /// <summary>
    /// Gets or sets when this pattern was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets when this pattern was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets additional metadata about the pattern.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
