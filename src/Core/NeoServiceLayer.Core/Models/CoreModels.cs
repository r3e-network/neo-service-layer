using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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
    
    /// <summary>
    /// Gets or sets the from address (convenience property).
    /// </summary>
    public string From { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the to address (convenience property).
    /// </summary>
    public string To { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the transaction value (convenience property).
    /// </summary>
    public decimal Value { get; set; }
    
    /// <summary>
    /// Gets or sets the transaction data (convenience property).
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Represents a fairness analysis result.
/// </summary>
public class FairnessAnalysisResult
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fairness score (0-1).
    /// </summary>
    public double FairnessScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    /// Gets or sets the estimated MEV.
    /// </summary>
    public decimal EstimatedMEV { get; set; }

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the detected risks.
    /// </summary>
    public List<string> DetectedRisks { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the protection fee.
    /// </summary>
    public decimal ProtectionFee { get; set; }

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
/// Represents an uncertainty assessment result.
/// </summary>
public class UncertaintyResult
{
    /// <summary>
    /// Gets or sets the prediction intervals.
    /// </summary>
    public Dictionary<string, (double Lower, double Upper)> PredictionIntervals { get; set; } = new();

    /// <summary>
    /// Gets or sets the epistemic uncertainty.
    /// </summary>
    public double EpistemicUncertainty { get; set; }

    /// <summary>
    /// Gets or sets the aleatoric uncertainty.
    /// </summary>
    public double AleatoricUncertainty { get; set; }

    /// <summary>
    /// Gets or sets the total uncertainty.
    /// </summary>
    public double TotalUncertainty { get; set; }

    /// <summary>
    /// Gets or sets the confidence bounds.
    /// </summary>
    public Dictionary<string, double> ConfidenceBounds { get; set; } = new();
}

/// <summary>
/// Represents validation result for prediction accuracy.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets the mean absolute error.
    /// </summary>
    public double MeanAbsoluteError { get; set; }

    /// <summary>
    /// Gets or sets the root mean square error.
    /// </summary>
    public double RootMeanSquareError { get; set; }

    /// <summary>
    /// Gets or sets the mean absolute percentage error.
    /// </summary>
    public double MeanAbsolutePercentageError { get; set; }

    /// <summary>
    /// Gets or sets the R2 score.
    /// </summary>
    public double R2Score { get; set; }

    /// <summary>
    /// Gets or sets the prediction intervals.
    /// </summary>
    public Dictionary<string, (double Lower, double Upper)> PredictionIntervals { get; set; } = new();

    /// <summary>
    /// Gets or sets the outlier detection results.
    /// </summary>
    public List<string> OutlierDetection { get; set; } = new();
}

/// <summary>
/// Represents backtest result.
/// </summary>
public class BacktestResult
{
    /// <summary>
    /// Gets or sets the total trades.
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    /// Gets or sets the win rate.
    /// </summary>
    public double WinRate { get; set; }

    /// <summary>
    /// Gets or sets the Sharpe ratio.
    /// </summary>
    public double SharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the maximum drawdown.
    /// </summary>
    public double MaxDrawdown { get; set; }

    /// <summary>
    /// Gets or sets the profit factor.
    /// </summary>
    public double ProfitFactor { get; set; }

    /// <summary>
    /// Gets or sets the monthly returns.
    /// </summary>
    public List<double> MonthlyReturns { get; set; } = new();
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

/// <summary>
/// Represents market forecast request.
/// </summary>
public class MarketForecastRequest
{
    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    [Required]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time horizon.
    /// </summary>
    public ForecastTimeHorizon TimeHorizon { get; set; }

    /// <summary>
    /// Gets or sets the current price.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Gets or sets the market data.
    /// </summary>
    public Dictionary<string, object> MarketData { get; set; } = new();

    /// <summary>
    /// Gets or sets the technical indicators.
    /// </summary>
    public Dictionary<string, double> TechnicalIndicators { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk parameters.
    /// </summary>
    public Dictionary<string, double> RiskParameters { get; set; } = new();
}

/// <summary>
/// Represents forecast time horizon.
/// </summary>
public enum ForecastTimeHorizon
{
    /// <summary>
    /// Short term forecast (hours).
    /// </summary>
    ShortTerm,

    /// <summary>
    /// Medium term forecast (days).
    /// </summary>
    MediumTerm,

    /// <summary>
    /// Long term forecast (weeks/months).
    /// </summary>
    LongTerm
}

/// <summary>
/// Represents market forecast result.
/// </summary>
public class MarketForecast
{
    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the predicted prices.
    /// </summary>
    public List<PriceForecast> PredictedPrices { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall trend.
    /// </summary>
    public MarketTrend OverallTrend { get; set; }

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Gets or sets the price targets.
    /// </summary>
    public Dictionary<string, decimal> PriceTargets { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the support levels.
    /// </summary>
    public List<decimal> SupportLevels { get; set; } = new();

    /// <summary>
    /// Gets or sets the resistance levels.
    /// </summary>
    public List<decimal> ResistanceLevels { get; set; } = new();

    /// <summary>
    /// Gets or sets the market indicators.
    /// </summary>
    public Dictionary<string, double> MarketIndicators { get; set; } = new();

    /// <summary>
    /// Gets or sets the time horizon.
    /// </summary>
    public ForecastTimeHorizon TimeHorizon { get; set; }

    /// <summary>
    /// Gets or sets the forecast metrics.
    /// </summary>
    public Dictionary<string, double> ForecastMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the volatility metrics.
    /// </summary>
    public VolatilityMetrics? VolatilityMetrics { get; set; }

    /// <summary>
    /// Gets or sets the trading recommendations.
    /// </summary>
    public List<string> TradingRecommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence intervals.
    /// </summary>
    public Dictionary<string, ConfidenceInterval> ConfidenceIntervals { get; set; } = new();

    /// <summary>
    /// Gets or sets the forecast metrics.
    /// </summary>
    public ForecastMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets when the forecast was generated.
    /// </summary>
    public DateTime ForecastedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents market trend.
/// </summary>
public enum MarketTrend
{
    /// <summary>
    /// Bullish trend.
    /// </summary>
    Bullish,

    /// <summary>
    /// Bearish trend.
    /// </summary>
    Bearish,

    /// <summary>
    /// Neutral trend.
    /// </summary>
    Neutral,

    /// <summary>
    /// Volatile market.
    /// </summary>
    Volatile
}

/// <summary>
/// Represents volatility metrics.
/// </summary>
public class VolatilityMetrics
{
    /// <summary>
    /// Gets or sets the Value at Risk.
    /// </summary>
    public double VaR { get; set; }

    /// <summary>
    /// Gets or sets the expected shortfall.
    /// </summary>
    public double ExpectedShortfall { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation.
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Gets or sets the beta.
    /// </summary>
    public double Beta { get; set; }
}

/// <summary>
/// Represents price forecast.
/// </summary>
public class PriceForecast
{
    /// <summary>
    /// Gets or sets the forecast date.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the predicted price.
    /// </summary>
    public decimal PredictedPrice { get; set; }

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the prediction interval.
    /// </summary>
    public ConfidenceInterval Interval { get; set; } = new();
}

/// <summary>
/// Represents confidence interval.
/// </summary>
public class ConfidenceInterval
{
    /// <summary>
    /// Gets or sets the lower bound.
    /// </summary>
    public decimal LowerBound { get; set; }

    /// <summary>
    /// Gets or sets the upper bound.
    /// </summary>
    public decimal UpperBound { get; set; }

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    public double ConfidenceLevel { get; set; }
}

/// <summary>
/// Represents forecast metrics.
/// </summary>
public class ForecastMetrics
{
    /// <summary>
    /// Gets or sets the mean absolute error.
    /// </summary>
    public double MeanAbsoluteError { get; set; }

    /// <summary>
    /// Gets or sets the root mean square error.
    /// </summary>
    public double RootMeanSquareError { get; set; }

    /// <summary>
    /// Gets or sets the mean absolute percentage error.
    /// </summary>
    public double MeanAbsolutePercentageError { get; set; }

    /// <summary>
    /// Gets or sets the R-squared value.
    /// </summary>
    public double RSquared { get; set; }
}

#endregion

#region Voting Types

/// <summary>
/// Voting strategy types for Neo blockchain governance.
/// </summary>
public enum VotingStrategyType
{
    /// <summary>
    /// Manual candidate selection.
    /// </summary>
    Manual,

    /// <summary>
    /// Automatic candidate selection.
    /// </summary>
    Automatic,

    /// <summary>
    /// Profit-optimized selection.
    /// </summary>
    ProfitOptimized,

    /// <summary>
    /// Stability-focused selection.
    /// </summary>
    StabilityFocused,

    /// <summary>
    /// Conditional selection based on preferences.
    /// </summary>
    Conditional,

    /// <summary>
    /// Custom selection with user-defined criteria.
    /// </summary>
    Custom
}

#endregion
