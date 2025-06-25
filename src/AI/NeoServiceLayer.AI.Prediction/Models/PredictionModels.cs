using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.Prediction.Models;

/// <summary>
/// Represents a prediction model.
/// </summary>
public class PredictionModel : AIModel
{
    /// <summary>
    /// Gets or sets the prediction type.
    /// </summary>
    public PredictionType PredictionType { get; set; }

    /// <summary>
    /// Gets or sets the time horizon for predictions.
    /// </summary>
    public TimeSpan TimeHorizon { get; set; }

    /// <summary>
    /// Gets or sets the minimum confidence threshold.
    /// </summary>
    public double MinConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the feature importance scores.
    /// </summary>
    public Dictionary<string, double> FeatureImportance { get; set; } = new();

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the training data size.
    /// </summary>
    public int TrainingDataSize { get; set; }

    /// <summary>
    /// Gets or sets when the model was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the definition of a prediction model.
/// </summary>
public class PredictionModelDefinition : AIModelDefinition
{
    /// <summary>
    /// Gets or sets the prediction type.
    /// </summary>
    public PredictionType PredictionType { get; set; }

    /// <summary>
    /// Gets or sets the target variable.
    /// </summary>
    [Required]
    public string TargetVariable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time series configuration.
    /// </summary>
    public TimeSeriesConfig? TimeSeriesConfig { get; set; }

    /// <summary>
    /// Gets or sets the validation strategy.
    /// </summary>
    public ValidationStrategy ValidationStrategy { get; set; } = ValidationStrategy.CrossValidation;

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the features.
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the hyper parameters.
    /// </summary>
    public Dictionary<string, object> HyperParameters { get; set; } = new();
}

/// <summary>
/// Represents the type of prediction.
/// </summary>
public enum PredictionType
{
    /// <summary>
    /// Price prediction.
    /// </summary>
    Price,

    /// <summary>
    /// Market trend prediction.
    /// </summary>
    MarketTrend,

    /// <summary>
    /// Volatility prediction.
    /// </summary>
    Volatility,

    /// <summary>
    /// Volume prediction.
    /// </summary>
    Volume,

    /// <summary>
    /// Risk assessment.
    /// </summary>
    Risk,

    /// <summary>
    /// Sentiment analysis.
    /// </summary>
    Sentiment,

    /// <summary>
    /// Time series forecasting.
    /// </summary>
    TimeSeries,

    /// <summary>
    /// Classification prediction.
    /// </summary>
    Classification,

    /// <summary>
    /// Regression prediction.
    /// </summary>
    Regression
}

/// <summary>
/// Represents time series configuration.
/// </summary>
public class TimeSeriesConfig
{
    /// <summary>
    /// Gets or sets the window size for time series analysis.
    /// </summary>
    public int WindowSize { get; set; } = 30;

    /// <summary>
    /// Gets or sets the forecast horizon.
    /// </summary>
    public int ForecastHorizon { get; set; } = 1;

    /// <summary>
    /// Gets or sets the seasonality period.
    /// </summary>
    public int? SeasonalityPeriod { get; set; }

    /// <summary>
    /// Gets or sets whether to use trend analysis.
    /// </summary>
    public bool UseTrend { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use seasonal decomposition.
    /// </summary>
    public bool UseSeasonality { get; set; } = true;
}

/// <summary>
/// Represents validation strategy.
/// </summary>
public enum ValidationStrategy
{
    /// <summary>
    /// Cross-validation.
    /// </summary>
    CrossValidation,

    /// <summary>
    /// Time series split.
    /// </summary>
    TimeSeriesSplit,

    /// <summary>
    /// Hold-out validation.
    /// </summary>
    HoldOut,

    /// <summary>
    /// Bootstrap validation.
    /// </summary>
    Bootstrap
}

/// <summary>
/// Represents sentiment analysis request (service-specific).
/// </summary>
public class SentimentAnalysisRequest
{
    /// <summary>
    /// Gets or sets the text to analyze.
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of the text.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language of the text.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Gets or sets additional context.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the news data.
    /// </summary>
    public List<string> NewsData { get; set; } = new();

    /// <summary>
    /// Gets or sets the social media data.
    /// </summary>
    public List<string> SocialMediaData { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include market sentiment.
    /// </summary>
    public bool IncludeMarketSentiment { get; set; }

    /// <summary>
    /// Gets or sets the analysis depth.
    /// </summary>
    public SentimentAnalysisDepth AnalysisDepth { get; set; }

    /// <summary>
    /// Gets or sets the time window.
    /// </summary>
    public TimeSpan TimeWindow { get; set; }

    /// <summary>
    /// Gets or sets the language filters.
    /// </summary>
    public string[] LanguageFilters { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the source weights.
    /// </summary>
    public Dictionary<string, double> SourceWeights { get; set; } = new();
}

/// <summary>
/// Represents sentiment analysis depth.
/// </summary>
public enum SentimentAnalysisDepth
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
    /// Comprehensive analysis.
    /// </summary>
    Comprehensive
}

/// <summary>
/// Represents sentiment analysis result.
/// </summary>
public class SentimentResult
{
    /// <summary>
    /// Gets or sets the sentiment score.
    /// </summary>
    public SentimentScore Sentiment { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets detected emotions.
    /// </summary>
    public Dictionary<string, double> Emotions { get; set; } = new();

    /// <summary>
    /// Gets or sets key phrases.
    /// </summary>
    public List<string> KeyPhrases { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis timestamp.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the overall sentiment.
    /// </summary>
    public SentimentType OverallSentiment { get; set; }

    /// <summary>
    /// Gets or sets the sentiment score value.
    /// </summary>
    public double SentimentScore { get; set; }

    /// <summary>
    /// Gets or sets the news sentiment.
    /// </summary>
    public double NewsSentiment { get; set; }

    /// <summary>
    /// Gets or sets the social media sentiment.
    /// </summary>
    public double SocialMediaSentiment { get; set; }

    /// <summary>
    /// Gets or sets the market sentiment.
    /// </summary>
    public double MarketSentiment { get; set; }

    /// <summary>
    /// Gets or sets the sentiment trends.
    /// </summary>
    public List<SentimentTrend> SentimentTrends { get; set; } = new();

    /// <summary>
    /// Gets or sets the keywords.
    /// </summary>
    public List<KeywordSentiment> Keywords { get; set; } = new();

    /// <summary>
    /// Gets or sets the emotional analysis.
    /// </summary>
    public EmotionalAnalysis? EmotionalAnalysis { get; set; }

    /// <summary>
    /// Gets or sets the influencer sentiment.
    /// </summary>
    public List<InfluencerSentiment> InfluencerSentiment { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk indicators.
    /// </summary>
    public List<string> RiskIndicators { get; set; } = new();

    /// <summary>
    /// Gets or sets the source reliability.
    /// </summary>
    public Dictionary<string, double> SourceReliability { get; set; } = new();

    /// <summary>
    /// Gets or sets the conflicting signals.
    /// </summary>
    public List<string> ConflictingSignals { get; set; } = new();

    /// <summary>
    /// Gets or sets the trending topics.
    /// </summary>
    public List<string> TrendingTopics { get; set; } = new();

    /// <summary>
    /// Gets or sets the viral content.
    /// </summary>
    public List<string> ViralContent { get; set; } = new();

    /// <summary>
    /// Gets or sets the influence metrics.
    /// </summary>
    public Dictionary<string, double> InfluenceMetrics { get; set; } = new();
}

/// <summary>
/// Represents sentiment trend.
/// </summary>
public class SentimentTrend
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the sentiment value.
    /// </summary>
    public double SentimentValue { get; set; }

    /// <summary>
    /// Gets or sets the volume.
    /// </summary>
    public int Volume { get; set; }
}

/// <summary>
/// Represents keyword sentiment.
/// </summary>
public class KeywordSentiment
{
    /// <summary>
    /// Gets or sets the keyword.
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sentiment.
    /// </summary>
    public SentimentType Sentiment { get; set; }

    /// <summary>
    /// Gets or sets the frequency.
    /// </summary>
    public int Frequency { get; set; }
}

/// <summary>
/// Represents emotional analysis.
/// </summary>
public class EmotionalAnalysis
{
    /// <summary>
    /// Gets or sets the fear level.
    /// </summary>
    public double Fear { get; set; }

    /// <summary>
    /// Gets or sets the greed level.
    /// </summary>
    public double Greed { get; set; }

    /// <summary>
    /// Gets or sets the optimism level.
    /// </summary>
    public double Optimism { get; set; }

    /// <summary>
    /// Gets or sets the pessimism level.
    /// </summary>
    public double Pessimism { get; set; }
}

/// <summary>
/// Represents influencer sentiment.
/// </summary>
public class InfluencerSentiment
{
    /// <summary>
    /// Gets or sets the influencer name.
    /// </summary>
    public string InfluencerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sentiment.
    /// </summary>
    public SentimentType Sentiment { get; set; }

    /// <summary>
    /// Gets or sets the reach.
    /// </summary>
    public int Reach { get; set; }

    /// <summary>
    /// Gets or sets the influence score.
    /// </summary>
    public double InfluenceScore { get; set; }
}

/// <summary>
/// Represents sentiment score.
/// </summary>
public class SentimentScore
{
    /// <summary>
    /// Gets or sets the overall sentiment.
    /// </summary>
    public SentimentType Overall { get; set; }

    /// <summary>
    /// Gets or sets the positive score.
    /// </summary>
    public double Positive { get; set; }

    /// <summary>
    /// Gets or sets the negative score.
    /// </summary>
    public double Negative { get; set; }

    /// <summary>
    /// Gets or sets the neutral score.
    /// </summary>
    public double Neutral { get; set; }

    /// <summary>
    /// Gets or sets the compound score.
    /// </summary>
    public double Compound { get; set; }
}

/// <summary>
/// Represents sentiment type.
/// </summary>
public enum SentimentType
{
    /// <summary>
    /// Positive sentiment.
    /// </summary>
    Positive,

    /// <summary>
    /// Negative sentiment.
    /// </summary>
    Negative,

    /// <summary>
    /// Neutral sentiment.
    /// </summary>
    Neutral,

    /// <summary>
    /// Mixed sentiment.
    /// </summary>
    Mixed
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
/// Represents market forecast request.
/// </summary>
public class MarketForecastRequest
{
    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    [Required]
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the forecast horizon in days.
    /// </summary>
    public int ForecastHorizonDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the historical data period in days.
    /// </summary>
    public int HistoricalPeriodDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets additional features to include.
    /// </summary>
    public List<string> AdditionalFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence level.
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
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
/// Represents market forecast result.
/// </summary>
public class MarketForecast
{
    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the forecasted prices.
    /// </summary>
    public List<PriceForecast> Forecasts { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence intervals.
    /// </summary>
    public Dictionary<string, ConfidenceInterval> ConfidenceIntervals { get; set; } = new();

    /// <summary>
    /// Gets or sets the forecast accuracy metrics.
    /// </summary>
    public ForecastMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the forecast timestamp.
    /// </summary>
    public DateTime ForecastedAt { get; set; } = DateTime.UtcNow;

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

/// <summary>
/// Represents model registration request.
/// </summary>
public class ModelRegistration
{
    /// <summary>
    /// Gets or sets the model definition.
    /// </summary>
    [Required]
    public PredictionModelDefinition ModelDefinition { get; set; } = new();

    /// <summary>
    /// Gets or sets the training data.
    /// </summary>
    public byte[] TrainingData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the validation data.
    /// </summary>
    public byte[] ValidationData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the registration metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents prediction analysis result.
/// </summary>
public class PredictionAnalysisResult
{
    /// <summary>
    /// Gets or sets the analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the analysis results.
    /// </summary>
    public Dictionary<string, object> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the analysis timestamp.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

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
/// Represents uncertainty assessment result.
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
