using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.AI.Prediction.Models;
using NeoServiceLayer.ServiceFramework;

namespace NeoServiceLayer.AI.Prediction;

/// <summary>
/// Helper methods for the Prediction Service.
/// </summary>
public partial class PredictionService
{
    /// <summary>
    /// Gets a model by ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <returns>The model.</returns>
    protected override ModelInfo? GetModel(string modelId)
    {
        lock (_modelsLock)
        {
            if (_models.TryGetValue(modelId, out var model) && model.IsActive)
            {
                // Convert PredictionModel to ModelInfo
                return new ModelInfo
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    Version = model.Version ?? "1.0.0",
                    Format = "custom",
                    InputSchema = new string[] { "default_input" },
                    OutputSchema = new string[] { "prediction" },
                    RegisteredAt = model.CreatedAt,
                    LastUsed = model.UpdatedAt,
                    InferenceCount = 0,
                    AverageInferenceTimeMs = 0.0,
                    Metadata = new Dictionary<string, object>()
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Initializes default models for common use cases.
    /// </summary>
    private async Task InitializeDefaultModelsAsync()
    {
        await Task.CompletedTask; // Ensure async
        var defaultModels = new[]
        {
            new Models.PredictionModelDefinition
            {
                Name = "CryptoPricePredictor",
                Type = AIModelType.Prediction,
                PredictionType = Models.PredictionType.Price,
                TargetVariable = "price",
                InputFeatures = new List<string> { "price", "volume", "market_cap", "sentiment" },
                OutputFeatures = new List<string> { "predicted_price", "price_change" }
            },
            new Models.PredictionModelDefinition
            {
                Name = "MarketTrendAnalyzer",
                Type = AIModelType.Prediction,
                PredictionType = Models.PredictionType.MarketTrend,
                TargetVariable = "trend",
                InputFeatures = new List<string> { "price_history", "volume_profile", "technical_indicators" },
                OutputFeatures = new List<string> { "trend_direction", "trend_strength" }
            },
            new Models.PredictionModelDefinition
            {
                Name = "VolatilityPredictor",
                Type = AIModelType.Prediction,
                PredictionType = Models.PredictionType.Volatility,
                TargetVariable = "volatility",
                InputFeatures = new List<string> { "price_returns", "volume", "market_events" },
                OutputFeatures = new List<string> { "volatility", "risk_level" }
            }
        };

        foreach (var definition in defaultModels)
        {
            try
            {
                // Create a simple default model
                var modelId = Guid.NewGuid().ToString();
                var model = new Models.PredictionModel
                {
                    Id = modelId,
                    Name = definition.Name,
                    Description = definition.Name + " prediction model",
                    Type = AIModelType.Prediction,
                    Version = "1.0.0",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    PredictionType = definition.PredictionType,
                    TimeHorizon = TimeSpan.FromDays(1),
                    MinConfidenceThreshold = 0.7
                };

                lock (_modelsLock)
                {
                    _models[modelId] = model;
                    _predictionHistory[modelId] = new List<PredictionResult>();
                }

                Logger.LogInformation("Initialized default model {ModelName} with ID {ModelId}", definition.Name, modelId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to initialize default model {ModelName}", definition.Name);
            }
        }
    }
}
