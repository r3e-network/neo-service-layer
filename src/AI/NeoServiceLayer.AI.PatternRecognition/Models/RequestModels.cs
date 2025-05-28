using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents a pattern analysis request.
/// </summary>
public class PatternAnalysisRequest
{
    /// <summary>
    /// Gets or sets the model ID to use for analysis.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input data for analysis.
    /// </summary>
    [Required]
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents an anomaly detection request.
/// </summary>
public class AnomalyDetectionRequest
{
    /// <summary>
    /// Gets or sets the model ID to use for detection.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data points to analyze.
    /// </summary>
    [Required]
    public double[] DataPoints { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Gets or sets the feature names.
    /// </summary>
    public string[] FeatureNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the detection parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a classification request.
/// </summary>
public class ClassificationRequest
{
    /// <summary>
    /// Gets or sets the model ID to use for classification.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input data for classification.
    /// </summary>
    [Required]
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Gets or sets the classification parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a risk assessment request.
/// </summary>
public class RiskAssessmentRequest
{
    /// <summary>
    /// Gets or sets the transaction ID to assess.
    /// </summary>
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity ID being assessed.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    [Required]
    public Dictionary<string, object> TransactionData { get; set; } = new();

    /// <summary>
    /// Gets or sets the historical data for context.
    /// </summary>
    public Dictionary<string, object> HistoricalData { get; set; } = new();

    /// <summary>
    /// Gets or sets the assessment parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the user context for the assessment.
    /// </summary>
    public Dictionary<string, object> UserContext { get; set; } = new();
}

/// <summary>
/// Represents a behavior analysis request.
/// </summary>
public class BehaviorAnalysisRequest
{
    /// <summary>
    /// Gets or sets the address to analyze.
    /// </summary>
    [Required]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction history.
    /// </summary>
    [Required]
    public List<Dictionary<string, object>> TransactionHistory { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis time range.
    /// </summary>
    public TimeRange TimeRange { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the baseline period.
    /// </summary>
    public TimeSpan BaselinePeriod { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis period.
    /// </summary>
    public TimeRange AnalysisPeriod { get; set; } = new();
}

/// <summary>
/// Represents a fraud detection request.
/// </summary>
public class FraudDetectionRequest
{
    /// <summary>
    /// Gets or sets the transaction ID to analyze.
    /// </summary>
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    [Required]
    public Dictionary<string, object> TransactionData { get; set; } = new();

    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string RecipientAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the detection parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction amount (alias for Amount).
    /// </summary>
    public decimal TransactionAmount
    {
        get => Amount;
        set => Amount = value;
    }

    /// <summary>
    /// Gets or sets the transaction time (alias for Timestamp).
    /// </summary>
    public DateTime TransactionTime
    {
        get => Timestamp;
        set => Timestamp = value;
    }

    /// <summary>
    /// Gets or sets the receiver address (alias for RecipientAddress).
    /// </summary>
    public string ReceiverAddress
    {
        get => RecipientAddress;
        set => RecipientAddress = value;
    }

    /// <summary>
    /// Gets or sets whether this is a high frequency transaction.
    /// </summary>
    public bool HighFrequency { get; set; }

    /// <summary>
    /// Gets or sets whether this transaction has unusual time patterns.
    /// </summary>
    public bool UnusualTimePattern { get; set; }

    /// <summary>
    /// Gets or sets whether this involves a new address.
    /// </summary>
    public bool IsNewAddress { get; set; }

    /// <summary>
    /// Gets or sets whether this has suspicious geolocation.
    /// </summary>
    public bool SuspiciousGeolocation { get; set; }

    /// <summary>
    /// Gets or sets the transaction count in the time window.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the time window for analysis.
    /// </summary>
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the detection threshold.
    /// </summary>
    public double Threshold { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets the features (alias for TransactionData).
    /// </summary>
    public Dictionary<string, object> Features
    {
        get => TransactionData;
        set => TransactionData = value;
    }

    /// <summary>
    /// Gets or sets the from address (alias for SenderAddress).
    /// </summary>
    public string FromAddress
    {
        get => SenderAddress;
        set => SenderAddress = value;
    }

    /// <summary>
    /// Gets or sets the to address (alias for RecipientAddress).
    /// </summary>
    public string ToAddress
    {
        get => RecipientAddress;
        set => RecipientAddress = value;
    }
}

/// <summary>
/// Represents a time range for analysis.
/// </summary>
public class TimeRange
{
    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow.AddDays(-30);

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTime EndTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the duration of the time range.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Checks if the time range is valid.
    /// </summary>
    public bool IsValid => StartTime < EndTime;
}
