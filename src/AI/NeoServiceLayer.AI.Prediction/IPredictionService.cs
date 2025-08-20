using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using Models = NeoServiceLayer.AI.Prediction.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.AI.Prediction;

/// <summary>
/// Interface for the Prediction Service that provides AI-powered forecasting and sentiment analysis capabilities.
/// </summary>
public interface IPredictionService : Core.IPredictionService
{
    /// <summary>
    /// Creates a new prediction model.
    /// </summary>
    /// <param name="definition">The model definition.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The model ID.</returns>
    Task<string> CreateModelAsync(Models.PredictionModelDefinition definition, BlockchainType blockchainType);

    // Note: PredictAsync, AnalyzeSentimentAsync, and RegisterModelAsync are inherited from Core.IPredictionService

    /// <summary>
    /// Generates market forecast.
    /// </summary>
    /// <param name="request">The market forecast request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The market forecast result.</returns>
    Task<Models.MarketForecast> ForecastMarketAsync(Models.MarketForecastRequest request, BlockchainType blockchainType);

    // Note: RegisterModelAsync is inherited from Core.IPredictionService

    /// <summary>
    /// Gets all available models.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of models.</returns>
    Task<IEnumerable<Models.PredictionModel>> GetModelsAsync(BlockchainType blockchainType);

    /// <summary>
    /// Gets a specific model by ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The model.</returns>
    Task<Models.PredictionModel> GetModelAsync(string modelId, BlockchainType blockchainType);

    /// <summary>
    /// Gets prediction history for a model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The prediction history.</returns>
    Task<IEnumerable<Core.Models.PredictionResult>> GetPredictionHistoryAsync(string modelId, BlockchainType blockchainType);

    /// <summary>
    /// Retrains a model with new data.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="definition">The updated model definition.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if successful.</returns>
    Task<bool> RetrainModelAsync(string modelId, Models.PredictionModelDefinition definition, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteModelAsync(string modelId, BlockchainType blockchainType);
}

// Note: PredictionRequest, PredictionResult, SentimentAnalysisRequest, SentimentResult, and ModelRegistration
// are defined in NeoServiceLayer.Core.Models and should be used from there.
// Additional service-specific types are defined in Models/PredictionModels.cs
