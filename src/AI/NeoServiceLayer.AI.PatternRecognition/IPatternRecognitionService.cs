using NeoServiceLayer.Core;
using NeoServiceLayer.AI.PatternRecognition.Models;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Interface for the Pattern Recognition Service that provides AI-powered fraud detection and anomaly analysis.
/// </summary>
public interface IPatternRecognitionService : Core.IPatternRecognitionService
{
    /// <summary>
    /// Detects fraud in transaction data.
    /// </summary>
    /// <param name="request">The fraud detection request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fraud detection result.</returns>
    Task<Models.FraudDetectionResult> DetectFraudAsync(Models.FraudDetectionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Analyzes patterns in the provided data.
    /// </summary>
    /// <param name="request">The pattern analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The pattern analysis result.</returns>
    Task<PatternAnalysisResult> AnalyzePatternsAsync(PatternAnalysisRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Analyzes user behavior.
    /// </summary>
    /// <param name="request">The behavior analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The behavior analysis result.</returns>
    Task<BehaviorAnalysisResult> AnalyzeBehaviorAsync(BehaviorAnalysisRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Assesses risk for a transaction or user.
    /// </summary>
    /// <param name="request">The risk assessment request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The risk assessment result.</returns>
    Task<Models.RiskAssessmentResult> AssessRiskAsync(Models.RiskAssessmentRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a new pattern model.
    /// </summary>
    /// <param name="definition">The model definition.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The model ID.</returns>
    Task<string> CreateModelAsync(PatternModelDefinition definition, BlockchainType blockchainType);

    /// <summary>
    /// Gets all available pattern models.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of models.</returns>
    Task<IEnumerable<PatternModel>> GetModelsAsync(BlockchainType blockchainType);

    /// <summary>
    /// Gets a specific pattern model by ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The model.</returns>
    Task<PatternModel> GetModelAsync(string modelId, BlockchainType blockchainType);

    /// <summary>
    /// Updates a pattern model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="definition">The updated model definition.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateModelAsync(string modelId, PatternModelDefinition definition, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a pattern model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteModelAsync(string modelId, BlockchainType blockchainType);

    /// <summary>
    /// Gets fraud detection history.
    /// </summary>
    /// <param name="userId">The user ID (optional).</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fraud detection history.</returns>
    Task<IEnumerable<Models.FraudDetectionResult>> GetFraudDetectionHistoryAsync(string? userId, BlockchainType blockchainType);

    /// <summary>
    /// Gets behavior profile for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The behavior profile.</returns>
    Task<BehaviorProfile> GetBehaviorProfileAsync(string userId, BlockchainType blockchainType);

    /// <summary>
    /// Updates behavior profile for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="profile">The updated behavior profile.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateBehaviorProfileAsync(string userId, BehaviorProfile profile, BlockchainType blockchainType);
}
