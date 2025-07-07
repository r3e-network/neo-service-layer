using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for AI prediction services in production mode.
/// </summary>
[Tags("AI Services")]
public class AIController : BaseApiController
{
    private readonly IPredictionService _predictionService;
    private readonly IPatternRecognitionService? _patternRecognitionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIController"/> class.
    /// </summary>
    /// <param name="predictionService">The prediction service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="patternRecognitionService">The pattern recognition service.</param>
    public AIController(
        IPredictionService predictionService,
        ILogger<AIController> logger,
        IPatternRecognitionService? patternRecognitionService = null) : base(logger)
    {
        _predictionService = predictionService;
        _patternRecognitionService = patternRecognitionService;
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

            // Create prediction request for the service
            var predictionRequest = new Core.Models.PredictionRequest
            {
                ModelId = request.ModelType,
                InputData = request.HistoricalData.ToDictionary(
                    data => data.Timestamp.ToString("yyyy-MM-dd"),
                    data => (object)data.Value)
            };

            // Call the actual prediction service
            var serviceResult = await _predictionService.PredictAsync(
                predictionRequest,
                request.BlockchainType ?? BlockchainType.NeoN3);

            // Convert service result to API response model
            var result = new PredictionResult
            {
                PredictedValue = serviceResult.PredictedValue,
                Confidence = serviceResult.Confidence,
                Trend = serviceResult.Metadata?.GetValueOrDefault("trend")?.ToString() ?? "Unknown",
                ModelUsed = request.ModelType,
                ComputedInEnclave = request.UseEnclave,
                Timestamp = serviceResult.Timestamp,
                Factors = serviceResult.Metadata?.GetValueOrDefault("factors") as string[] ?? Array.Empty<string>()
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

            // Use pattern recognition service if available
            if (_patternRecognitionService != null)
            {
                var anomalyRequest = new Core.AnomalyDetectionRequest
                {
                    Data = request.DataPoints.Select(p => new double[] { p.Value }).ToArray(),
                    FeatureNames = new[] { "value" },
                    ModelId = request.PatternType ?? "general"
                };

                var patterns = await _patternRecognitionService.DetectAnomaliesAsync(
                    anomalyRequest,
                    request.BlockchainType ?? BlockchainType.NeoN3);

                var result = new PatternAnalysisResult
                {
                    DetectedPatterns = patterns.IsAnomaly.Select((isAnomaly, i) => isAnomaly ? $"Anomaly_{i}" : $"Normal_{i}").ToArray(),
                    Confidence = patterns.AnomalyScores.DefaultIfEmpty(0.0).Average(),
                    AnomaliesDetected = patterns.AnomalyCount,
                    TrendStrength = patterns.AnomalyScores.DefaultIfEmpty(0.0).Max(),
                    AnalyzedInEnclave = request.UseEnclave,
                    Timestamp = patterns.DetectionTime
                };

                Logger.LogInformation("Analyzed pattern using {AnalysisType} for user {UserId}",
                    request.PatternType, GetCurrentUserId());

                return Ok(CreateResponse(result, "Pattern analysis completed"));
            }
            else
            {
                // Fallback if pattern recognition service is not available
                Logger.LogWarning("Pattern recognition service not available, returning error");
                return StatusCode(503, CreateErrorResponse("Pattern recognition service is not available"));
            }
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
            // Get registered models from prediction service
            var registeredModels = new List<AIModelInfo>();

            // Get models for Neo N3
            var modelsN3 = await GetModelsForBlockchain(BlockchainType.NeoN3);
            registeredModels.AddRange(modelsN3);

            // Get models for Neo X
            var modelsX = await GetModelsForBlockchain(BlockchainType.NeoX);
            registeredModels.AddRange(modelsX);

            if (!registeredModels.Any())
            {
                Logger.LogWarning("No AI models found in prediction service");
                return Ok(CreateResponse(Array.Empty<AIModelInfo>(), "No models available"));
            }

            return Ok(CreateResponse(registeredModels, "Models retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetAvailableModels");
        }
    }

    private async Task<List<AIModelInfo>> GetModelsForBlockchain(BlockchainType blockchainType)
    {
        var models = new List<AIModelInfo>();

        try
        {
            // Check if we have the extended prediction service
            if (_predictionService is AI.Prediction.IPredictionService extendedService)
            {
                var serviceModels = await extendedService.GetModelsAsync(blockchainType);
                foreach (var model in serviceModels)
                {
                    models.Add(new AIModelInfo
                    {
                        Name = model.Name,
                        Type = model.ModelType,
                        Description = model.Description,
                        Version = model.Version,
                        SupportsEnclave = false, // Default value since not available in model
                        Accuracy = model.TrainingMetrics?.GetValueOrDefault("accuracy") as double? ?? 0.0,
                        LastTrained = model.LastUpdated
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get models for blockchain {Blockchain}", blockchainType);
        }

        return models;
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

    /// <summary>
    /// Gets or sets the blockchain type to use for prediction.
    /// </summary>
    public BlockchainType? BlockchainType { get; set; }
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

    /// <summary>
    /// Gets or sets the blockchain type to use for pattern analysis.
    /// </summary>
    public BlockchainType? BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the pattern type for analysis.
    /// </summary>
    public string? PatternType { get; set; }
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
