using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Core.Models;

#region Core Service Models

/// <summary>
/// Represents a pending transaction in the system.
/// </summary>
public class PendingTransaction
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender address.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the gas price.
    /// </summary>
    public decimal GasPrice { get; set; }

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public long GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the transaction nonce.
    /// </summary>
    public long Nonce { get; set; }

    /// <summary>
    /// Gets or sets when the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the transaction priority.
    /// </summary>
    public TransactionPriority Priority { get; set; } = TransactionPriority.Normal;

    /// <summary>
    /// Gets or sets the fairness level.
    /// </summary>
    public FairnessLevel FairnessLevel { get; set; } = FairnessLevel.Standard;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a transaction ordering pool.
/// </summary>
public class OrderingPool
{
    /// <summary>
    /// Gets or sets the pool ID.
    /// </summary>
    public string PoolId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pool name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordering algorithm.
    /// </summary>
    public OrderingAlgorithm Algorithm { get; set; } = OrderingAlgorithm.FIFO;

    /// <summary>
    /// Gets or sets the pending transactions.
    /// </summary>
    public List<PendingTransaction> PendingTransactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum pool size.
    /// </summary>
    public int MaxSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets when the pool was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the pool is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the pool configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Represents the fairness level for transaction ordering.
/// </summary>
public enum FairnessLevel
{
    /// <summary>
    /// Basic fairness level.
    /// </summary>
    Basic,

    /// <summary>
    /// Standard fairness level.
    /// </summary>
    Standard,

    /// <summary>
    /// High fairness level.
    /// </summary>
    High,

    /// <summary>
    /// Maximum fairness level.
    /// </summary>
    Maximum
}

/// <summary>
/// Represents the ordering algorithm for transactions.
/// </summary>
public enum OrderingAlgorithm
{
    /// <summary>
    /// First In, First Out ordering.
    /// </summary>
    FIFO,

    /// <summary>
    /// Priority-based ordering.
    /// </summary>
    Priority,

    /// <summary>
    /// Gas price-based ordering.
    /// </summary>
    GasPrice,

    /// <summary>
    /// Fair ordering algorithm.
    /// </summary>
    Fair,

    /// <summary>
    /// Time-weighted fair ordering.
    /// </summary>
    TimeWeightedFair
}

/// <summary>
/// Represents transaction priority levels.
/// </summary>
public enum TransactionPriority
{
    /// <summary>
    /// Low priority transaction.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority transaction.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority transaction.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority transaction.
    /// </summary>
    Critical
}

#endregion

#region Fair Ordering Models

/// <summary>
/// Represents a fair ordering result.
/// </summary>
public class FairOrderingResult
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered transactions.
    /// </summary>
    public List<PendingTransaction> OrderedTransactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the fairness score.
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets whether the ordering was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets when the ordering was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a fair transaction request.
/// </summary>
public class FairTransactionRequest
{
    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    [Required]
    public PendingTransaction Transaction { get; set; } = new();

    /// <summary>
    /// Gets or sets the desired fairness level.
    /// </summary>
    public FairnessLevel FairnessLevel { get; set; } = FairnessLevel.Standard;

    /// <summary>
    /// Gets or sets the maximum wait time in milliseconds.
    /// </summary>
    public int MaxWaitTimeMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets whether to use MEV protection.
    /// </summary>
    public bool UseMevProtection { get; set; } = true;

    /// <summary>
    /// Gets or sets additional preferences.
    /// </summary>
    public Dictionary<string, object> Preferences { get; set; } = new();
}

/// <summary>
/// Represents a fairness analysis result.
/// </summary>
public class FairnessAnalysisResult
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fairness score (0-1).
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the MEV risk level.
    /// </summary>
    public MevRiskLevel MevRisk { get; set; }

    /// <summary>
    /// Gets or sets the recommended fairness level.
    /// </summary>
    public FairnessLevel RecommendedLevel { get; set; }

    /// <summary>
    /// Gets or sets the analysis details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a transaction analysis request.
/// </summary>
public class TransactionAnalysisRequest
{
    /// <summary>
    /// Gets or sets the transaction to analyze.
    /// </summary>
    [Required]
    public PendingTransaction Transaction { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis depth.
    /// </summary>
    public AnalysisDepth Depth { get; set; } = AnalysisDepth.Standard;

    /// <summary>
    /// Gets or sets whether to include MEV analysis.
    /// </summary>
    public bool IncludeMevAnalysis { get; set; } = true;

    /// <summary>
    /// Gets or sets additional analysis parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents MEV risk levels.
/// </summary>
public enum MevRiskLevel
{
    /// <summary>
    /// Low MEV risk.
    /// </summary>
    Low,

    /// <summary>
    /// Medium MEV risk.
    /// </summary>
    Medium,

    /// <summary>
    /// High MEV risk.
    /// </summary>
    High,

    /// <summary>
    /// Critical MEV risk.
    /// </summary>
    Critical
}

/// <summary>
/// Represents analysis depth levels.
/// </summary>
public enum AnalysisDepth
{
    /// <summary>
    /// Basic analysis.
    /// </summary>
    Basic,

    /// <summary>
    /// Standard analysis.
    /// </summary>
    Standard,

    /// <summary>
    /// Deep analysis.
    /// </summary>
    Deep,

    /// <summary>
    /// Comprehensive analysis.
    /// </summary>
    Comprehensive
}

#endregion

#region MEV Analysis Models

/// <summary>
/// Represents an MEV analysis request.
/// </summary>
public class MevAnalysisRequest
{
    /// <summary>
    /// Gets or sets the transaction to analyze.
    /// </summary>
    [Required]
    public PendingTransaction Transaction { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis timeframe in seconds.
    /// </summary>
    public int TimeframeSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether to include historical data.
    /// </summary>
    public bool IncludeHistoricalData { get; set; } = true;

    /// <summary>
    /// Gets or sets the analysis parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents an MEV analysis result.
/// </summary>
public class MevAnalysisResult
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MEV risk score (0-1).
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the MEV risk level.
    /// </summary>
    public MevRiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the potential MEV value.
    /// </summary>
    public decimal PotentialMevValue { get; set; }

    /// <summary>
    /// Gets or sets the recommended protection level.
    /// </summary>
    public MevProtectionLevel RecommendedProtection { get; set; }

    /// <summary>
    /// Gets or sets the analysis details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents MEV protection levels.
/// </summary>
public enum MevProtectionLevel
{
    /// <summary>
    /// No MEV protection.
    /// </summary>
    None,

    /// <summary>
    /// Basic MEV protection.
    /// </summary>
    Basic,

    /// <summary>
    /// Standard MEV protection.
    /// </summary>
    Standard,

    /// <summary>
    /// Advanced MEV protection.
    /// </summary>
    Advanced,

    /// <summary>
    /// Maximum MEV protection.
    /// </summary>
    Maximum
}

#endregion

#region Prediction Models

/// <summary>
/// Represents a prediction request.
/// </summary>
public class PredictionRequest
{
    /// <summary>
    /// Gets or sets the model ID to use for prediction.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input data for prediction.
    /// </summary>
    [Required]
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Gets or sets the prediction parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include confidence intervals.
    /// </summary>
    public bool IncludeConfidenceIntervals { get; set; } = true;

    /// <summary>
    /// Gets or sets the prediction horizon.
    /// </summary>
    public TimeSpan? PredictionHorizon { get; set; }

    /// <summary>
    /// Gets or sets the symbol for the prediction.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time horizon in hours.
    /// </summary>
    public int TimeHorizon { get; set; }

    /// <summary>
    /// Gets or sets the features for prediction.
    /// </summary>
    public Dictionary<string, object> Features { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.95;
}

/// <summary>
/// Represents a prediction result.
/// </summary>
public class PredictionResult
{
    /// <summary>
    /// Gets or sets the prediction ID.
    /// </summary>
    public string PredictionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID used.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prediction values.
    /// </summary>
    public Dictionary<string, object> Predictions { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the confidence intervals.
    /// </summary>
    public Dictionary<string, (double Lower, double Upper)> ConfidenceIntervals { get; set; } = new();

    /// <summary>
    /// Gets or sets when the prediction was made.
    /// </summary>
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the predicted values array.
    /// </summary>
    public List<double> PredictedValues { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence degradation over time.
    /// </summary>
    public List<double> ConfidenceDegradation { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the accuracy score.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the predicted value.
    /// </summary>
    public double PredictedValue { get; set; }

    /// <summary>
    /// Gets or sets the actual value.
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    /// Gets or sets the feature importance scores.
    /// </summary>
    public Dictionary<string, double> FeatureImportance { get; set; } = new();

    /// <summary>
    /// Gets or sets the data sources used.
    /// </summary>
    public List<string> DataSources { get; set; } = new();

    /// <summary>
    /// Gets or sets the model ensemble information.
    /// </summary>
    public List<string> ModelEnsemble { get; set; } = new();

    /// <summary>
    /// Gets or sets the ensemble weights.
    /// </summary>
    public Dictionary<string, double> EnsembleWeights { get; set; } = new();

    /// <summary>
    /// Gets or sets individual predictions from ensemble models.
    /// </summary>
    public Dictionary<string, dynamic> IndividualPredictions { get; set; } = new();

    /// <summary>
    /// Gets or sets the ensemble uncertainty.
    /// </summary>
    public double EnsembleUncertainty { get; set; }

    /// <summary>
    /// Gets or sets the processing time.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Represents a sentiment analysis request.
/// </summary>
public class SentimentAnalysisRequest
{
    /// <summary>
    /// Gets or sets the text to analyze.
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language of the text.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Gets or sets whether to include detailed analysis.
    /// </summary>
    public bool IncludeDetailedAnalysis { get; set; } = false;

    /// <summary>
    /// Gets or sets additional analysis parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents a sentiment analysis result.
/// </summary>
public class SentimentResult
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sentiment score (-1 to 1).
    /// </summary>
    public double SentimentScore { get; set; }

    /// <summary>
    /// Gets or sets the sentiment label.
    /// </summary>
    public SentimentLabel Label { get; set; }

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets detailed sentiment breakdown.
    /// </summary>
    public Dictionary<string, double> DetailedSentiment { get; set; } = new();

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents sentiment labels.
/// </summary>
public enum SentimentLabel
{
    /// <summary>
    /// Very negative sentiment.
    /// </summary>
    VeryNegative,

    /// <summary>
    /// Negative sentiment.
    /// </summary>
    Negative,

    /// <summary>
    /// Neutral sentiment.
    /// </summary>
    Neutral,

    /// <summary>
    /// Positive sentiment.
    /// </summary>
    Positive,

    /// <summary>
    /// Very positive sentiment.
    /// </summary>
    VeryPositive
}

/// <summary>
/// Represents a model registration request.
/// </summary>
public class ModelRegistration
{
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the model data.
    /// </summary>
    public byte[] ModelData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the model configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

#endregion 