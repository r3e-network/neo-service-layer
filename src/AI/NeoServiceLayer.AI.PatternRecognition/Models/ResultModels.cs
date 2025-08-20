using System;
using System.Collections.Generic;

namespace NeoServiceLayer.AI.PatternRecognition.Models
{
    /// <summary>
    /// Represents a behavior profile.
    /// </summary>
    public class BehaviorProfile
    {
        public string TypicalTimePattern { get; set; } = "normal";
        public string UserId { get; set; } = string.Empty;
        public ActivityLevel ActivityLevel { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated => UpdatedAt;  // Alias for UpdatedAt
        public Dictionary<string, object> Metadata { get; set; } = new();
        public double TransactionFrequency { get; set; }
        public bool UnusualTimePatterns { get; set; }
        public double AverageTransactionAmount { get; set; }
        public bool SuspiciousAddressInteractions { get; set; }
        public string EntityType { get; set; } = "regular";
        public double RiskTolerance { get; set; } = 0.5;
        public Dictionary<string, object> TransactionPatterns { get; set; } = new();
        public Dictionary<string, double> BehaviorMetrics { get; set; } = new();
    }

    /// <summary>
    /// Represents a fraud detection result.
    /// </summary>
    public class FraudDetectionResult
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsFraud { get; set; }
        public bool IsFraudulent { get; set; } // Alias for IsFraud
        public double RiskScore { get; set; }
        /// <summary>
        /// Gets or sets the fraud score (alias for RiskScore).
        /// </summary>
        public double FraudScore 
        { 
            get => RiskScore;
            set => RiskScore = value;
        }
        public double Confidence { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string DetectionId { get; set; } = Guid.NewGuid().ToString();
        public List<string> Flags { get; set; } = new();
        public List<string> DetectedPatterns { get; set; } = new();
        public List<FraudPattern> Patterns { get; set; } = new();
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public Dictionary<string, object> AnalysisDetails { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the risk level.
        /// </summary>
        public string RiskLevel { get; set; } = "Low";
        
        /// <summary>
        /// Gets or sets the risk factors list.
        /// </summary>
        public List<string> RiskFactors { get; set; } = new();
        
        /// <summary>
        /// Gets or sets whether the operation was successful.
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the error message if not successful.
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the model ID used for detection.
        /// </summary>
        public string ModelId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the detection time.
        /// </summary>
        public DateTime DetectionTime 
        { 
            get => DetectedAt;
            set => DetectedAt = value;
        }
        
        /// <summary>
        /// Gets or sets the cryptographic proof of detection.
        /// </summary>
        public string Proof { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents risk assessment result.
    /// </summary>
    public class RiskAssessmentResult
    {
        public string AssessmentId { get; set; } = string.Empty;
        public RiskLevel RiskLevel { get; set; }
        public double RiskScore { get; set; }
        public double OverallRiskScore { get; set; }
        public double Confidence { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public Dictionary<string, double>? RiskBreakdown { get; set; }
        public Dictionary<string, bool>? MitigatingFactors { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents classification result.
    /// </summary>
    public class ClassificationResult
    {
        public string Category { get; set; } = string.Empty;
        public string PredictedClass { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; } = TimeSpan.Zero;
        public double Confidence { get; set; }
        public Dictionary<string, double> Probabilities { get; set; } = new();
        public Dictionary<string, double> ClassProbabilities { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Dictionary<string, object> Details { get; set; } = new();
        public List<Classification> Classifications { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets when the classification was performed (alias for Timestamp).
        /// </summary>
        public DateTime ClassifiedAt 
        { 
            get => Timestamp;
            set => Timestamp = value;
        }
        
        public string ClassificationId { get; set; } = Guid.NewGuid().ToString();
        public string? ModelId { get; set; }
        public Dictionary<string, object>? InputData { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents an anomaly.
    /// </summary>
    public class Anomaly
    {
        public string Id { get; set; } = string.Empty;
        public AnomalyType Type { get; set; } = AnomalyType.StatisticalOutlier;
        public string Description { get; set; } = string.Empty;
        public double Severity { get; set; }
        public double Confidence { get; set; }
        public double Score => Confidence; // Alias for Confidence
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public double[] DataPoints { get; set; } = Array.Empty<double>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
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
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets whether the analysis was successful.
        /// </summary>
        public bool Success { get; set; }

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
        public bool IsNewProfile { get; set; }
        
        /// <summary>
        /// Gets or sets whether this is a new user profile (alias).
        /// </summary>
        public bool IsNewUserProfile 
        { 
            get => IsNewProfile;
            set => IsNewProfile = value;
        }

        /// <summary>
        /// Gets or sets the behavior patterns detected.
        /// </summary>
        public List<DetectedPattern> BehaviorPatterns { get; set; } = new();

        /// <summary>
        /// Gets or sets the risk factors identified.
        /// </summary>
        public List<string> RiskFactors { get; set; } = new();

        /// <summary>
        /// Gets or sets the deviation from the normal profile.
        /// </summary>
        public double ProfileDeviation { get; set; }
        
        /// <summary>
        /// Gets or sets the deviation from profile (alias).
        /// </summary>
        public double DeviationFromProfile 
        { 
            get => ProfileDeviation;
            set => ProfileDeviation = value;
        }

        /// <summary>
        /// Gets or sets the recommendations based on the analysis.
        /// </summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// Gets or sets the alert level based on the analysis.
        /// </summary>
        public AlertLevel AlertLevel { get; set; }
        
        /// <summary>
        /// Gets or sets when the analysis was performed (alias).
        /// </summary>
        public DateTime AnalyzedAt 
        { 
            get => AnalysisTime;
            set => AnalysisTime = value;
        }
    }

    /// <summary>
    /// Represents the result of an anomaly detection analysis.
    /// </summary>
    public class AnomalyDetectionResult
    {
        /// <summary>
        /// Gets or sets the unique identifier for this detection result.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the detection ID (alias for Id).
        /// </summary>
        public string DetectionId
        {
            get => Id;
            set => Id = value;
        }
        
        /// <summary>
        /// Gets or sets the analysis ID (alias for Id).
        /// </summary>
        public string AnalysisId
        {
            get => Id;
            set => Id = value;
        }
        
        /// <summary>
        /// Gets or sets the model ID that detected the anomaly.
        /// </summary>
        public string ModelId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the anomaly score (0.0 to 1.0).
        /// </summary>
        public double AnomalyScore { get; set; }
        
        /// <summary>
        /// Gets or sets the confidence of the detection.
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// Gets or sets when the anomaly was detected.
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the description of the anomaly.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the severity level.
        /// </summary>
        public string Severity { get; set; } = "Medium";
        
        /// <summary>
        /// Gets or sets additional data about the anomaly.
        /// </summary>
        public Dictionary<string, object> AnomalyData { get; set; } = new();
        
        /// <summary>
        /// Gets or sets whether this anomaly has been resolved.
        /// </summary>
        public bool IsResolved { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        /// <summary>
        /// Gets or sets whether anomalies were detected.
        /// </summary>
        public bool HasAnomalies { get; set; }
        
        /// <summary>
        /// Gets or sets the list of anomalies.
        /// </summary>
        public List<Anomaly> Anomalies { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the anomaly ID.
        /// </summary>
        public string AnomalyId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the recommended actions.
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the analysis details.
        /// </summary>
        public Dictionary<string, object> AnalysisDetails { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the risk level.
        /// </summary>
        public RiskLevel RiskLevel { get; set; }
        
        /// <summary>
        /// Gets or sets the risk factors.
        /// </summary>
        public List<string> RiskFactors { get; set; } = new();
        
        /// <summary>
        /// Gets or sets whether the operation was successful.
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the error message if not successful.
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets whether anomalies were detected (alias for HasAnomalies).
        /// </summary>
        public bool IsAnomalous 
        { 
            get => HasAnomalies;
            set => HasAnomalies = value;
        }
        
        /// <summary>
        /// Gets or sets the detected anomalies (alias for Anomalies).
        /// </summary>
        public List<Anomaly> DetectedAnomalies 
        { 
            get => Anomalies;
            set => Anomalies = value;
        }
        
        /// <summary>
        /// Gets or sets the details about the anomaly detection.
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the anomaly scores for each data point.
        /// </summary>
        public List<double> AnomalyScores { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the anomaly count.
        /// </summary>
        public int AnomalyCount { get; set; }
        
        /// <summary>
        /// Gets or sets the detection time (alias for DetectedAt).
        /// </summary>
        public DateTime DetectionTime 
        { 
            get => DetectedAt;
            set => DetectedAt = value;
        }
        
        /// <summary>
        /// Gets or sets the cryptographic proof of detection.
        /// </summary>
        public string Proof { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents a classification.
    /// </summary>
    public class Classification
    {
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public double Score { get; set; }
        public double Probability { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    
    /// <summary>
    /// Detection sensitivity enumeration.
    /// </summary>
    public enum DetectionSensitivity
    {
        Low,
        Standard,
        High,
        Maximum
    }
}