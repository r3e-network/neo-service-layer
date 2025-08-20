using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.AI.Prediction.Models;
using NeoServiceLayer.Core;
using CoreModels = NeoServiceLayer.Core.Models;
using PredictionModels = NeoServiceLayer.AI.Prediction.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for AI prediction operations.
/// </summary>
[ApiVersion("1.0")]
[Tags("AI Prediction")]
public class PredictionController : BaseApiController
{
    private readonly AI.Prediction.IPredictionService _predictionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionController"/> class.
    /// </summary>
    /// <param name="predictionService">The prediction service.</param>
    /// <param name="logger">The logger.</param>
    public PredictionController(
        AI.Prediction.IPredictionService predictionService,
        ILogger<PredictionController> logger) : base(logger)
    {
        _predictionService = predictionService;
    }

    /// <summary>
    /// Makes a prediction using AI algorithms.
    /// </summary>
    /// <param name="request">The prediction request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The prediction result.</returns>
    /// <response code="200">Prediction completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("predict/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<CoreModels.PredictionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> Predict(
        [FromBody] CoreModels.PredictionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _predictionService.PredictAsync(request, blockchain);

            Logger.LogInformation("Prediction completed for user {UserId} on {BlockchainType}. Confidence: {Confidence}",
                GetCurrentUserId(), blockchainType, result.Confidence);

            return Ok(CreateResponse(result, "Prediction completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Predict");
        }
    }

    /// <summary>
    /// Analyzes sentiment using AI algorithms.
    /// </summary>
    /// <param name="request">The sentiment analysis request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The sentiment analysis result.</returns>
    /// <response code="200">Sentiment analysis completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("sentiment/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PredictionModels.SentimentResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AnalyzeSentiment(
        [FromBody] NeoServiceLayer.Core.Models.SentimentAnalysisRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _predictionService.AnalyzeSentimentAsync(request, blockchain);

            Logger.LogInformation("Sentiment analysis completed for user {UserId} on {BlockchainType}. Confidence: {Confidence}",
                GetCurrentUserId(), blockchainType, result.Confidence);

            return Ok(CreateResponse(result, "Sentiment analysis completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AnalyzeSentiment");
        }
    }

    /// <summary>
    /// Generates market forecast using AI algorithms.
    /// </summary>
    /// <param name="request">The market forecast request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The market forecast result.</returns>
    /// <response code="200">Market forecast completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("market-forecast/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PredictionModels.MarketForecast>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ForecastMarket(
        [FromBody] PredictionModels.MarketForecastRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _predictionService.ForecastMarketAsync(request, blockchain);

            Logger.LogInformation("Market forecast completed for user {UserId} on {BlockchainType}. Asset: {AssetSymbol}, Forecasts: {ForecastCount}",
                GetCurrentUserId(), blockchainType, request.AssetSymbol, result.Forecasts.Count);

            return Ok(CreateResponse(result, "Market forecast completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ForecastMarket");
        }
    }

    /// <summary>
    /// Creates a new AI prediction model.
    /// </summary>
    /// <param name="definition">The model definition.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created model ID.</returns>
    /// <response code="200">Model created successfully.</response>
    /// <response code="400">Invalid model definition.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("models/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateModel(
        [FromBody] PredictionModels.PredictionModelDefinition definition,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var modelId = await _predictionService.CreateModelAsync(definition, blockchain);

            Logger.LogInformation("Prediction model created by user {UserId} on {BlockchainType}. Model ID: {ModelId}",
                GetCurrentUserId(), blockchainType, modelId);

            return Ok(CreateResponse(modelId, "Model created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateModel");
        }
    }

    /// <summary>
    /// Registers a new prediction model.
    /// </summary>
    /// <param name="registration">The model registration.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The registration result.</returns>
    /// <response code="200">Model registered successfully.</response>
    /// <response code="400">Invalid model registration.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("models/register/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> RegisterModel(
        [FromBody] NeoServiceLayer.Core.Models.ModelRegistration registration,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var modelId = await _predictionService.RegisterModelAsync(registration, blockchain);

            Logger.LogInformation("Prediction model registered by user {UserId} on {BlockchainType}. Model ID: {ModelId}",
                GetCurrentUserId(), blockchainType, modelId);

            return Ok(CreateResponse(modelId, "Model registered successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RegisterModel");
        }
    }

    /// <summary>
    /// Gets all available prediction models.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The list of models.</returns>
    /// <response code="200">Models retrieved successfully.</response>
    /// <response code="400">Invalid blockchain type.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("models/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PredictionModels.PredictionModel>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetModels([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var models = await _predictionService.GetModelsAsync(blockchain);

            return Ok(CreateResponse(models, "Models retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetModels");
        }
    }

    /// <summary>
    /// Gets a specific prediction model by ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The model.</returns>
    /// <response code="200">Model retrieved successfully.</response>
    /// <response code="404">Model not found.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("models/{modelId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PredictionModels.PredictionModel>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetModel(
        [FromRoute] string modelId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var model = await _predictionService.GetModelAsync(modelId, blockchain);

            return Ok(CreateResponse(model, "Model retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetModel");
        }
    }

    /// <summary>
    /// Gets prediction history for a user.
    /// </summary>
    /// <param name="userId">The user ID (optional).</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <returns>The prediction history.</returns>
    /// <response code="200">History retrieved successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("history/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(PaginatedResponse<CoreModels.PredictionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetPredictionHistory(
        [FromRoute] string blockchainType,
        [FromQuery] string? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (page < 1)
            {
                return BadRequest(CreateErrorResponse("Page number must be greater than 0"));
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(CreateErrorResponse("Page size must be between 1 and 100"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var history = await _predictionService.GetPredictionHistoryAsync(userId, blockchain);

            var pagedHistory = history.Skip((page - 1) * pageSize).Take(pageSize);

            var response = new PaginatedResponse<CoreModels.PredictionResult>
            {
                Success = true,
                Data = pagedHistory,
                Message = "Prediction history retrieved successfully",
                Timestamp = DateTime.UtcNow,
                Page = page,
                PageSize = pageSize,
                TotalItems = history.Count(),
                TotalPages = (int)Math.Ceiling((double)history.Count() / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetPredictionHistory");
        }
    }

    /// <summary>
    /// Retrains a prediction model with new data.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="definition">The updated model definition.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The retraining result.</returns>
    /// <response code="200">Model retrained successfully.</response>
    /// <response code="404">Model not found.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("models/{modelId}/retrain/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> RetrainModel(
        [FromRoute] string modelId,
        [FromBody] PredictionModels.PredictionModelDefinition definition,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var retrained = await _predictionService.RetrainModelAsync(modelId, definition, blockchain);

            if (!retrained)
            {
                return NotFound(CreateErrorResponse($"Model {modelId} not found"));
            }

            Logger.LogInformation("Prediction model retrained by user {UserId} on {BlockchainType}. Model ID: {ModelId}",
                GetCurrentUserId(), blockchainType, modelId);

            return Ok(CreateResponse(retrained, "Model retrained successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RetrainModel");
        }
    }

    /// <summary>
    /// Deletes a prediction model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The deletion result.</returns>
    /// <response code="200">Model deleted successfully.</response>
    /// <response code="404">Model not found.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpDelete("models/{modelId}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> DeleteModel(
        [FromRoute] string modelId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var deleted = await _predictionService.DeleteModelAsync(modelId, blockchain);

            if (!deleted)
            {
                return NotFound(CreateErrorResponse($"Model {modelId} not found"));
            }

            Logger.LogWarning("Prediction model deleted by user {UserId} on {BlockchainType}. Model ID: {ModelId}",
                GetCurrentUserId(), blockchainType, modelId);

            return Ok(CreateResponse(deleted, "Model deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "DeleteModel");
        }
    }
}
