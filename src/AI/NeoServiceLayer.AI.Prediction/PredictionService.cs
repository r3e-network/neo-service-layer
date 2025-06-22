using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.Prediction.Models;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.Prediction;

/// <summary>
/// Implementation of the Prediction Service that provides AI-powered forecasting and sentiment analysis capabilities.
/// </summary>
public partial class PredictionService : AIServiceBase, IPredictionService
{
    private readonly Dictionary<string, PredictionModel> _models = new();
    private readonly Dictionary<string, List<CoreModels.PredictionResult>> _predictionHistory = new();
    private readonly object _modelsLock = new();
    private readonly IPersistentStorageProvider? _storageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    public PredictionService(ILogger<PredictionService> logger, IServiceConfiguration? configuration = null)
        : base("PredictionService", "AI-powered forecasting and sentiment analysis service", "1.0.0", logger, configuration)
    {
        Configuration = configuration;

        AddCapability<IPredictionService>();
        AddDependency(new ServiceDependency("OracleService", true, "1.0.0"));
        AddDependency(new ServiceDependency("StorageService", false, "1.0.0"));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionService"/> class with full dependencies.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="storageProvider">The storage provider.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    public PredictionService(
        ILogger<PredictionService> logger,
        IServiceConfiguration configuration,
        IPersistentStorageProvider storageProvider,
        IEnclaveManager enclaveManager)
        : base("PredictionService", "AI-powered forecasting and sentiment analysis service", "1.0.0", logger,
               new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager, configuration)
    {
        Configuration = configuration;
        _storageProvider = storageProvider;

        AddCapability<IPredictionService>();
        AddDependency(new ServiceDependency("OracleService", true, "1.0.0"));
        AddDependency(new ServiceDependency("StorageService", false, "1.0.0"));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected new IServiceConfiguration? Configuration { get; }

    /// <inheritdoc/>
    public async Task<string> CreateModelAsync(PredictionModelDefinition definition, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var modelId = $"pred_model_{Guid.NewGuid():N}";

            // Train model within the enclave for security
            var trainedModel = await TrainModelInEnclaveAsync(definition);

            var model = new PredictionModel
            {
                Id = modelId,
                Name = definition.Name,
                Description = definition.Name + " prediction model",
                Type = definition.Type,
                Version = "1.0.0",
                ModelData = trainedModel ?? Array.Empty<byte>(),
                Configuration = new Dictionary<string, object>(),
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
                _predictionHistory[modelId] = new List<CoreModels.PredictionResult>();
            }

            Logger.LogInformation("Created prediction model {ModelId} ({Name}) for {Blockchain}",
                modelId, definition.Name, blockchainType);

            return modelId;
        });
    }

    /// <inheritdoc/>
    public async Task<CoreModels.PredictionResult> PredictAsync(CoreModels.PredictionRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            Models.PredictionModel? model;
            lock (_modelsLock)
            {
                if (!_models.TryGetValue(request.ModelId, out model) || model == null || !model.IsActive)
                {
                    throw new ArgumentException($"Model {request.ModelId} not found", nameof(request));
                }
            }

            var predictionId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Making prediction {PredictionId} with model {ModelId}", predictionId, request.ModelId);

                // Use the input data directly as it's already a dictionary
                var inputDict = request.InputData;

                // Make prediction within the enclave
                var predictionDict = await MakePredictionInEnclaveAsync(model.Id, inputDict);
                var confidence = 0.85; // Default confidence

                var result = new CoreModels.PredictionResult
                {
                    PredictionId = predictionId,
                    ModelId = request.ModelId,
                    Predictions = predictionDict,
                    Confidence = confidence,
                    ConfidenceIntervals = new Dictionary<string, (double Lower, double Upper)>(),
                    PredictedAt = DateTime.UtcNow,
                    ProcessingTimeMs = 0, // Will be calculated
                    Metadata = new Dictionary<string, object>()
                };

                // Store prediction history
                lock (_modelsLock)
                {
                    _predictionHistory[request.ModelId].Add(result);

                    // Keep only last 1000 predictions
                    if (_predictionHistory[request.ModelId].Count > 1000)
                    {
                        _predictionHistory[request.ModelId].RemoveAt(0);
                    }
                }

                Logger.LogInformation("Made prediction {PredictionId} with confidence {Confidence:P2} on {Blockchain}",
                    predictionId, confidence, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to make prediction {PredictionId} with model {ModelId}", predictionId, request.ModelId);

                return new CoreModels.PredictionResult
                {
                    PredictionId = predictionId,
                    ModelId = request.ModelId,
                    Predictions = new Dictionary<string, object>(),
                    Confidence = 0.0,
                    ConfidenceIntervals = new Dictionary<string, (double Lower, double Upper)>(),
                    PredictedAt = DateTime.UtcNow,
                    ProcessingTimeMs = 0,
                    Metadata = new Dictionary<string, object>()
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<CoreModels.SentimentResult> AnalyzeSentimentAsync(CoreModels.SentimentAnalysisRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var analysisId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Analyzing sentiment {AnalysisId} for text", analysisId);

                // Analyze sentiment within the enclave
                var sentiment = await AnalyzeTextSentimentInEnclaveAsync(request.Text);

                // Map to sentiment label based on compound score
                var label = sentiment.Compound switch
                {
                    < -0.6 => CoreModels.SentimentLabel.VeryNegative,
                    < -0.2 => CoreModels.SentimentLabel.Negative,
                    < 0.2 => CoreModels.SentimentLabel.Neutral,
                    < 0.6 => CoreModels.SentimentLabel.Positive,
                    _ => CoreModels.SentimentLabel.VeryPositive
                };

                var detailedSentiment = new Dictionary<string, double>
                {
                    ["positive"] = sentiment.Positive,
                    ["negative"] = sentiment.Negative,
                    ["neutral"] = sentiment.Neutral,
                    ["compound"] = sentiment.Compound
                };

                // Add hashtag influence if detected
                if (request.Text.Contains("#"))
                {
                    detailedSentiment["hashtag_influence"] = 0.1; // Slight positive influence for hashtags
                }

                var result = new CoreModels.SentimentResult
                {
                    AnalysisId = analysisId,
                    SentimentScore = sentiment.Compound,
                    Label = label,
                    Confidence = Math.Min(1.0, Math.Abs(sentiment.Compound) + 0.5),
                    DetailedSentiment = detailedSentiment,
                    AnalyzedAt = DateTime.UtcNow
                };

                Logger.LogInformation("Analyzed sentiment {AnalysisId}: {SentimentScore:F2} ({Label}) on {Blockchain}",
                    analysisId, sentiment.Compound, label, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to analyze sentiment {AnalysisId}", analysisId);

                return new CoreModels.SentimentResult
                {
                    AnalysisId = analysisId,
                    SentimentScore = 0.0,
                    Label = CoreModels.SentimentLabel.Neutral,
                    Confidence = 0.0,
                    DetailedSentiment = new Dictionary<string, double>(),
                    AnalyzedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<string> RegisterModelAsync(CoreModels.ModelRegistration registration, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            await Task.CompletedTask; // Ensure async
            var modelId = Guid.NewGuid().ToString();

            // Create model from registration
            var model = new Models.PredictionModel
            {
                Id = modelId,
                Name = registration.Name,
                Description = registration.Description,
                Type = AIModelType.Prediction,
                Version = registration.Version,
                ModelData = registration.ModelData,
                Configuration = registration.Configuration,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                PredictionType = Models.PredictionType.Classification,
                TimeHorizon = TimeSpan.FromDays(1),
                MinConfidenceThreshold = 0.7
            };

            lock (_modelsLock)
            {
                _models[modelId] = model;
                _predictionHistory[modelId] = new List<CoreModels.PredictionResult>();
            }

            Logger.LogInformation("Registered prediction model {ModelId} ({Name}) for {Blockchain}",
                modelId, registration.Name, blockchainType);

            return modelId;
        });
    }

    /// <inheritdoc/>
    public async Task<Models.MarketForecast> ForecastMarketAsync(Models.MarketForecastRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var forecastId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Forecasting market {ForecastId} for {Asset} over {TimeHorizon}",
                    forecastId, request.AssetSymbol, request.ForecastHorizonDays);

                // Generate market forecast within the enclave
                var historicalData = await GatherHistoricalMarketDataAsync(request);
                var technicalAnalysis = await ApplyTechnicalAnalysisAsync(historicalData, request);
                var fundamentalFactors = await AnalyzeFundamentalFactorsAsync(request);
                var forecast = await GenerateComprehensiveForecastAsync(request, historicalData, technicalAnalysis, fundamentalFactors);

                Logger.LogInformation("Generated market forecast {ForecastId} for {Asset} on {Blockchain}",
                    forecastId, request.AssetSymbol, blockchainType);

                return forecast;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to generate market forecast {ForecastId}", forecastId);

                return new Models.MarketForecast
                {
                    AssetSymbol = request.AssetSymbol,
                    Forecasts = new List<Models.PriceForecast>(),
                    ConfidenceIntervals = new Dictionary<string, Models.ConfidenceInterval>(),
                    Metrics = new Models.ForecastMetrics(),
                    ForecastedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Models.PredictionModel>> GetModelsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                return _models.Values.Where(m => m.IsActive).ToList();
            }
        });
    }

    /// <inheritdoc/>
    public async Task<Models.PredictionModel> GetModelAsync(string modelId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                if (_models.TryGetValue(modelId, out var model) && model.IsActive)
                {
                    return model;
                }
            }
            throw new ArgumentException($"Model {modelId} not found", nameof(modelId));
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CoreModels.PredictionResult>> GetPredictionHistoryAsync(string modelId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                if (_predictionHistory.TryGetValue(modelId, out var history))
                {
                    return history.ToList();
                }
            }

            return Enumerable.Empty<CoreModels.PredictionResult>();
        });
    }

    /// <inheritdoc/>
    public async Task<bool> RetrainModelAsync(string modelId, Models.PredictionModelDefinition definition, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentNullException.ThrowIfNull(definition);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            Models.PredictionModel? model;
            lock (_modelsLock)
            {
                if (!_models.TryGetValue(modelId, out model) || model == null || !model.IsActive)
                {
                    throw new ArgumentException($"Model {modelId} not found", nameof(modelId));
                }
            }

            try
            {
                // Retrain model within the enclave
                var retrainedModel = await TrainModelInEnclaveAsync(definition);

                // Update model
                model.ModelData = retrainedModel ?? Array.Empty<byte>();
                model.UpdatedAt = DateTime.UtcNow;
                model.Configuration["LastRetrained"] = DateTime.UtcNow;

                Logger.LogInformation("Retrained model {ModelId} successfully on {Blockchain}",
                    modelId, blockchainType);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to retrain model {ModelId} on {Blockchain}", modelId, blockchainType);
                return false;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteModelAsync(string modelId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_modelsLock)
            {
                if (_models.TryGetValue(modelId, out var model))
                {
                    model.IsActive = false;
                    Logger.LogInformation("Deleted prediction model {ModelId} on {Blockchain}", modelId, blockchainType);
                    return true;
                }
            }

            Logger.LogWarning("Model {ModelId} not found for deletion on {Blockchain}", modelId, blockchainType);
            return false;
        });
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Prediction Service");

        if (!await base.OnInitializeAsync())
        {
            return false;
        }

        // Initialize default models for common use cases
        await InitializeDefaultModelsAsync();

        Logger.LogInformation("Prediction Service initialized successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Prediction Service enclave");

        try
        {
            // Initialize enclave-specific resources for prediction models
            await Task.CompletedTask; // Placeholder for actual enclave initialization

            Logger.LogInformation("Prediction Service enclave initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Prediction Service enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Prediction Service");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Prediction Service");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        var baseHealth = base.OnGetHealthAsync().Result;

        if (baseHealth != ServiceHealth.Healthy)
        {
            return Task.FromResult(baseHealth);
        }

        // Check prediction-specific health
        var activeModelCount = _models.Values.Count(m => m.IsActive);
        var totalPredictionCount = _predictionHistory.Values.Sum(h => h.Count);

        Logger.LogDebug("Prediction Service health check: {ActiveModels} models, {TotalPredictions} predictions",
            activeModelCount, totalPredictionCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }
}
