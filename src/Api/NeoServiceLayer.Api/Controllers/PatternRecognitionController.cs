using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.AI.PatternRecognition;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Core;
using PatternModels = NeoServiceLayer.AI.PatternRecognition.Models;
using System.ComponentModel.DataAnnotations;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for AI pattern recognition operations.
/// </summary>
[ApiVersion("1.0")]
[Tags("AI Pattern Recognition")]
public class PatternRecognitionController : BaseApiController
{
    private readonly AI.PatternRecognition.IPatternRecognitionService _patternRecognitionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternRecognitionController"/> class.
    /// </summary>
    /// <param name="patternRecognitionService">The pattern recognition service.</param>
    /// <param name="logger">The logger.</param>
    public PatternRecognitionController(
        AI.PatternRecognition.IPatternRecognitionService patternRecognitionService,
        ILogger<PatternRecognitionController> logger) : base(logger)
    {
        _patternRecognitionService = patternRecognitionService;
    }

    /// <summary>
    /// Detects fraud in transaction data using AI pattern recognition.
    /// </summary>
    /// <param name="request">The fraud detection request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The fraud detection result.</returns>
    /// <response code="200">Fraud detection completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("fraud-detection/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PatternModels.FraudDetectionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> DetectFraud(
        [FromBody] PatternModels.FraudDetectionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _patternRecognitionService.DetectFraudAsync(request, blockchain);

            Logger.LogInformation("Fraud detection completed for user {UserId} on {BlockchainType}. Fraud Score: {FraudScore}",
                GetCurrentUserId(), blockchainType, result.FraudScore);

            return Ok(CreateResponse(result, "Fraud detection completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "DetectFraud");
        }
    }

    /// <summary>
    /// Analyzes patterns in the provided data using AI algorithms.
    /// </summary>
    /// <param name="request">The pattern analysis request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The pattern analysis result.</returns>
    /// <response code="200">Pattern analysis completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("pattern-analysis/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PatternAnalysisResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AnalyzePatterns(
        [FromBody] PatternAnalysisRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _patternRecognitionService.AnalyzePatternsAsync(request, blockchain);

            Logger.LogInformation("Pattern analysis completed for user {UserId} on {BlockchainType}. Patterns found: {PatternCount}",
                GetCurrentUserId(), blockchainType, result.DetectedPatterns.Count);

            return Ok(CreateResponse(result, "Pattern analysis completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AnalyzePatterns");
        }
    }

    /// <summary>
    /// Analyzes user behavior patterns using AI algorithms.
    /// </summary>
    /// <param name="request">The behavior analysis request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The behavior analysis result.</returns>
    /// <response code="200">Behavior analysis completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("behavior-analysis/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<BehaviorAnalysisResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AnalyzeBehavior(
        [FromBody] BehaviorAnalysisRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _patternRecognitionService.AnalyzeBehaviorAsync(request, blockchain);

            Logger.LogInformation("Behavior analysis completed for user {UserId} on {BlockchainType}. Risk Level: {RiskLevel}",
                GetCurrentUserId(), blockchainType, result.RiskLevel);

            return Ok(CreateResponse(result, "Behavior analysis completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AnalyzeBehavior");
        }
    }

    /// <summary>
    /// Assesses risk for a transaction or user using AI algorithms.
    /// </summary>
    /// <param name="request">The risk assessment request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The risk assessment result.</returns>
    /// <response code="200">Risk assessment completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("risk-assessment/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PatternModels.RiskAssessmentResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AssessRisk(
        [FromBody] PatternModels.RiskAssessmentRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _patternRecognitionService.AssessRiskAsync(request, blockchain);

            Logger.LogInformation("Risk assessment completed for user {UserId} on {BlockchainType}. Risk Score: {RiskScore}",
                GetCurrentUserId(), blockchainType, result.RiskScore);

            return Ok(CreateResponse(result, "Risk assessment completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AssessRisk");
        }
    }

    /// <summary>
    /// Creates a new AI pattern recognition model.
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
        [FromBody] PatternModelDefinition definition,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var modelId = await _patternRecognitionService.CreateModelAsync(definition, blockchain);

            Logger.LogInformation("Pattern recognition model created by user {UserId} on {BlockchainType}. Model ID: {ModelId}",
                GetCurrentUserId(), blockchainType, modelId);

            return Ok(CreateResponse(modelId, "Model created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateModel");
        }
    }

    /// <summary>
    /// Gets all available pattern recognition models.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The list of models.</returns>
    /// <response code="200">Models retrieved successfully.</response>
    /// <response code="400">Invalid blockchain type.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("models/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PatternModel>>), 200)]
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
            var models = await _patternRecognitionService.GetModelsAsync(blockchain);

            return Ok(CreateResponse(models, "Models retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetModels");
        }
    }

    /// <summary>
    /// Gets a specific pattern recognition model by ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The model.</returns>
    /// <response code="200">Model retrieved successfully.</response>
    /// <response code="404">Model not found.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("models/{modelId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PatternModel>), 200)]
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
            var model = await _patternRecognitionService.GetModelAsync(modelId, blockchain);

            return Ok(CreateResponse(model, "Model retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetModel");
        }
    }

    /// <summary>
    /// Updates a pattern recognition model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="definition">The updated model definition.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The update result.</returns>
    /// <response code="200">Model updated successfully.</response>
    /// <response code="404">Model not found.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPut("models/{modelId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> UpdateModel(
        [FromRoute] string modelId,
        [FromBody] PatternModelDefinition definition,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var updated = await _patternRecognitionService.UpdateModelAsync(modelId, definition, blockchain);

            if (!updated)
            {
                return NotFound(CreateErrorResponse($"Model {modelId} not found"));
            }

            Logger.LogInformation("Pattern recognition model updated by user {UserId} on {BlockchainType}. Model ID: {ModelId}",
                GetCurrentUserId(), blockchainType, modelId);

            return Ok(CreateResponse(updated, "Model updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UpdateModel");
        }
    }

    /// <summary>
    /// Deletes a pattern recognition model.
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
            var deleted = await _patternRecognitionService.DeleteModelAsync(modelId, blockchain);

            if (!deleted)
            {
                return NotFound(CreateErrorResponse($"Model {modelId} not found"));
            }

            Logger.LogWarning("Pattern recognition model deleted by user {UserId} on {BlockchainType}. Model ID: {ModelId}",
                GetCurrentUserId(), blockchainType, modelId);

            return Ok(CreateResponse(deleted, "Model deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "DeleteModel");
        }
    }

    /// <summary>
    /// Gets fraud detection history for a user.
    /// </summary>
    /// <param name="userId">The user ID (optional).</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <returns>The fraud detection history.</returns>
    /// <response code="200">History retrieved successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("fraud-detection/history/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(PaginatedResponse<PatternModels.FraudDetectionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetFraudDetectionHistory(
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
            var history = await _patternRecognitionService.GetFraudDetectionHistoryAsync(userId, blockchain);

            var pagedHistory = history.Skip((page - 1) * pageSize).Take(pageSize);

            var response = new PaginatedResponse<PatternModels.FraudDetectionResult>
            {
                Success = true,
                Data = pagedHistory,
                Message = "Fraud detection history retrieved successfully",
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
            return HandleException(ex, "GetFraudDetectionHistory");
        }
    }

    /// <summary>
    /// Gets behavior profile for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The behavior profile.</returns>
    /// <response code="200">Behavior profile retrieved successfully.</response>
    /// <response code="404">User not found.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("behavior-profile/{userId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<BehaviorProfile>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetBehaviorProfile(
        [FromRoute] string userId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var profile = await _patternRecognitionService.GetBehaviorProfileAsync(userId, blockchain);

            return Ok(CreateResponse(profile, "Behavior profile retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetBehaviorProfile");
        }
    }

    /// <summary>
    /// Updates behavior profile for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="profile">The updated behavior profile.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The update result.</returns>
    /// <response code="200">Behavior profile updated successfully.</response>
    /// <response code="404">User not found.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPut("behavior-profile/{userId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> UpdateBehaviorProfile(
        [FromRoute] string userId,
        [FromBody] BehaviorProfile profile,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var updated = await _patternRecognitionService.UpdateBehaviorProfileAsync(userId, profile, blockchain);

            if (!updated)
            {
                return NotFound(CreateErrorResponse($"User {userId} not found"));
            }

            Logger.LogInformation("Behavior profile updated for user {UserId} by {CurrentUserId} on {BlockchainType}",
                userId, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(updated, "Behavior profile updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UpdateBehaviorProfile");
        }
    }
} 