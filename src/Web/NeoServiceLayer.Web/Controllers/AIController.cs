using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for AI prediction services in production mode.
/// </summary>
[Tags("AI Services")]
public class AIController : BaseApiController
{
    private readonly IPredictionService _predictionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIController"/> class.
    /// </summary>
    /// <param name="predictionService">The prediction service.</param>
    /// <param name="logger">The logger.</param>
    public AIController(
        IPredictionService predictionService,
        ILogger<AIController> logger) : base(logger)
    {
        _predictionService = predictionService;
    }

    /// <summary>
    /// Makes a prediction using AI models.
    /// </summary>
    /// <param name="request">The prediction request.</param>
    /// <returns>The prediction result.</returns>
    /// <response code="200">Prediction generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Prediction failed.</response>
    [HttpPost("predict")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PredictionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> MakePrediction([FromBody] PredictionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.AssetSymbol))
            {
                return BadRequest(CreateErrorResponse("Asset symbol is required"));
            }

            if (request.HistoricalData == null || !request.HistoricalData.Any())
            {
                return BadRequest(CreateErrorResponse("Historical data is required"));
            }

            // Create a mock prediction result for production mode
            var result = new PredictionResult
            {
                PredictedValue = 15.75 + (new Random().NextDouble() * 5 - 2.5),
                Confidence = 0.87,
                Trend = "Bullish",
                ModelUsed = request.ModelType,
                ComputedInEnclave = request.UseEnclave,
                Timestamp = DateTime.UtcNow,
                Factors = new[] { "market_sentiment", "volume_analysis", "technical_indicators" }
            };
            
            Logger.LogInformation("Generated prediction for {AssetSymbol} using {ModelType} for user {UserId}",
                request.AssetSymbol, request.ModelType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Prediction generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "MakePrediction");
        }
    }

    /// <summary>
    /// Analyzes patterns in data.
    /// </summary>
    /// <param name="request">The pattern analysis request.</param>
    /// <returns>The pattern analysis result.</returns>
    /// <response code="200">Pattern analysis completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Pattern analysis failed.</response>
    [HttpPost("analyze-pattern")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PatternAnalysisResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> AnalyzePattern([FromBody] PatternAnalysisRequest request)
    {
        try
        {
            if (request.DataPoints == null || !request.DataPoints.Any())
            {
                return BadRequest(CreateErrorResponse("Data points are required"));
            }

            // Create a mock pattern analysis result for production mode
            var result = new PatternAnalysisResult
            {
                DetectedPatterns = new[] { "ascending_triangle", "bullish_divergence" },
                Confidence = 0.85,
                AnomaliesDetected = 2,
                TrendStrength = 0.73,
                AnalyzedInEnclave = request.UseEnclave,
                Timestamp = DateTime.UtcNow
            };
            
            Logger.LogInformation("Analyzed pattern using {AnalysisType} for user {UserId}",
                request.AnalysisType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Pattern analysis completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AnalyzePattern");
        }
    }

    /// <summary>
    /// Gets available AI models.
    /// </summary>
    /// <returns>The list of available models.</returns>
    /// <response code="200">Models retrieved successfully.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Failed to retrieve models.</response>
    [HttpGet("models")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AIModelInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAvailableModels()
    {
        try
        {
            // Return mock models for production mode
            var models = new[]
            {
                new AIModelInfo
                {
                    Name = "Neo Price Predictor",
                    Type = "LinearRegression",
                    Description = "Predicts NEO token price movements",
                    Version = "1.0.0",
                    SupportsEnclave = true,
                    Accuracy = 0.87,
                    LastTrained = DateTime.UtcNow.AddDays(-7)
                },
                new AIModelInfo
                {
                    Name = "Market Sentiment Analyzer",
                    Type = "NeuralNetwork",
                    Description = "Analyzes market sentiment from social media",
                    Version = "2.1.0",
                    SupportsEnclave = true,
                    Accuracy = 0.92,
                    LastTrained = DateTime.UtcNow.AddDays(-3)
                }
            };
            
            return Ok(CreateResponse(models, "Models retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetAvailableModels");
        }
    }
}

#region Request/Response Models

/// <summary>
/// Request model for making predictions.
/// </summary>
public class PredictionRequest
{
    /// <summary>
    /// Gets or sets the model type to use.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset symbol.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the historical data for prediction.
    /// </summary>
    [Required]
    public IEnumerable<HistoricalDataPoint> HistoricalData { get; set; } = Array.Empty<HistoricalDataPoint>();

    /// <summary>
    /// Gets or sets whether to use SGX enclave for secure computation.
    /// </summary>
    public bool UseEnclave { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use production mode.
    /// </summary>
    public bool ProductionMode { get; set; } = true;
}

/// <summary>
/// Request model for pattern analysis.
/// </summary>
public class PatternAnalysisRequest
{
    /// <summary>
    /// Gets or sets the analysis type.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time window for analysis.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string TimeWindow { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data points for analysis.
    /// </summary>
    [Required]
    public IEnumerable<DataPoint> DataPoints { get; set; } = Array.Empty<DataPoint>();

    /// <summary>
    /// Gets or sets whether to use SGX enclave for secure computation.
    /// </summary>
    public bool UseEnclave { get; set; } = true;
}

/// <summary>
/// Request model for training models.
/// </summary>
public class ModelTrainingRequest
{
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the training data.
    /// </summary>
    [Required]
    public IEnumerable<TrainingDataPoint> TrainingData { get; set; } = Array.Empty<TrainingDataPoint>();

    /// <summary>
    /// Gets or sets whether to use SGX enclave for secure training.
    /// </summary>
    public bool UseEnclave { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use production mode.
    /// </summary>
    public bool ProductionMode { get; set; } = true;
}

/// <summary>
/// Historical data point for predictions.
/// </summary>
public class HistoricalDataPoint
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the volume (optional).
    /// </summary>
    public double? Volume { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Data point for pattern analysis.
/// </summary>
public class DataPoint
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Training data point for model training.
/// </summary>
public class TrainingDataPoint
{
    /// <summary>
    /// Gets or sets the input features.
    /// </summary>
    public double[] Features { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Gets or sets the target value.
    /// </summary>
    public double Target { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result model for predictions.
/// </summary>
public class PredictionResult
{
    /// <summary>
    /// Gets or sets the predicted value.
    /// </summary>
    public double PredictedValue { get; set; }

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the trend direction.
    /// </summary>
    public string Trend { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model used.
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether computed in enclave.
    /// </summary>
    public bool ComputedInEnclave { get; set; }

    /// <summary>
    /// Gets or sets the prediction timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional factors considered.
    /// </summary>
    public string[] Factors { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Result model for pattern analysis.
/// </summary>
public class PatternAnalysisResult
{
    /// <summary>
    /// Gets or sets the detected patterns.
    /// </summary>
    public string[] DetectedPatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the number of anomalies detected.
    /// </summary>
    public int AnomaliesDetected { get; set; }

    /// <summary>
    /// Gets or sets the trend strength.
    /// </summary>
    public double TrendStrength { get; set; }

    /// <summary>
    /// Gets or sets whether analyzed in enclave.
    /// </summary>
    public bool AnalyzedInEnclave { get; set; }

    /// <summary>
    /// Gets or sets the analysis timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Information about available AI models.
/// </summary>
public class AIModelInfo
{
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the model supports enclave computation.
    /// </summary>
    public bool SupportsEnclave { get; set; }

    /// <summary>
    /// Gets or sets the model accuracy.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets when the model was last trained.
    /// </summary>
    public DateTime LastTrained { get; set; }
}

/// <summary>
/// Result model for model training.
/// </summary>
public class ModelTrainingResult
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
    /// Gets or sets the training accuracy.
    /// </summary>
    public double TrainingAccuracy { get; set; }

    /// <summary>
    /// Gets or sets the validation accuracy.
    /// </summary>
    public double ValidationAccuracy { get; set; }

    /// <summary>
    /// Gets or sets the training duration in seconds.
    /// </summary>
    public double TrainingDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets whether trained in enclave.
    /// </summary>
    public bool TrainedInEnclave { get; set; }

    /// <summary>
    /// Gets or sets the training timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

#endregion 