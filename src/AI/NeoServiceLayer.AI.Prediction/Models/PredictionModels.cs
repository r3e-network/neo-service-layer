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
/// Represents sentiment analysis request.
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
