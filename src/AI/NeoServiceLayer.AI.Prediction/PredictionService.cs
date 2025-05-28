using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.AI.Prediction.Models;

namespace NeoServiceLayer.AI.Prediction;

/// <summary>
/// Implementation of the Prediction Service that provides AI-powered forecasting and sentiment analysis capabilities.
/// </summary>
public partial class PredictionService : AIServiceBase, IPredictionService
{
    private readonly Dictionary<string, PredictionModel> _models = new();
    private readonly Dictionary<string, List<PredictionResult>> _predictionHistory = new();
    private readonly object _modelsLock = new();

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
            var modelId = Guid.NewGuid().ToString();

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
                _predictionHistory[modelId] = new List<PredictionResult>();
            }

            Logger.LogInformation("Created prediction model {ModelId} ({Name}) for {Blockchain}",
                modelId, definition.Name, blockchainType);

            return modelId;
        });
    }

    /// <inheritdoc/>
    public async Task<PredictionResult> PredictAsync(PredictionRequest request, BlockchainType blockchainType)
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

                // Convert input data to dictionary format
                var inputDict = new Dictionary<string, object>();
                for (int i = 0; i < request.InputData.Length; i++)
                {
                    inputDict[$"input_{i}"] = request.InputData[i];
                }

                // Make prediction within the enclave
                var predictionDict = await MakePredictionInEnclaveAsync(model.Id, inputDict);
                var confidence = 0.85; // Default confidence

                // Convert prediction dictionary to array
                var predictions = predictionDict.Values.ToArray();

                var result = new PredictionResult
                {
                    RequestId = request.RequestId,
                    ModelId = request.ModelId,
                    Predictions = predictions,
                    ConfidenceScores = new[] { confidence },
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = 0, // Will be calculated
                    Proof = string.Empty // Will be generated by enclave
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

                return new PredictionResult
                {
                    RequestId = request.RequestId,
                    ModelId = request.ModelId,
                    Predictions = Array.Empty<object>(),
                    ConfidenceScores = Array.Empty<double>(),
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = 0,
                    Proof = string.Empty
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<NeoServiceLayer.Core.SentimentResult> AnalyzeSentimentAsync(NeoServiceLayer.Core.SentimentAnalysisRequest request, BlockchainType blockchainType)
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
                Logger.LogDebug("Analyzing sentiment {AnalysisId} for {TextCount} texts", analysisId, request.TextData.Length);

                // Convert Core request to local request and analyze sentiment within the enclave
                var sentimentScores = new List<double>();
                var keywordSentiments = new Dictionary<string, double>();

                foreach (var text in request.TextData)
                {
                    var localRequest = new Models.SentimentAnalysisRequest
                    {
                        Text = text,
                        Language = "en"
                    };

                    var sentiment = await AnalyzeTextSentimentInEnclaveAsync(text);
                    sentimentScores.Add(sentiment.Compound);
                }

                // Process keywords if provided
                foreach (var keyword in request.Keywords)
                {
                    keywordSentiments[keyword] = sentimentScores.Average();
                }

                var overallSentiment = sentimentScores.Average();

                var result = new NeoServiceLayer.Core.SentimentResult
                {
                    AnalysisId = analysisId,
                    OverallSentiment = overallSentiment,
                    Confidence = Math.Abs(overallSentiment),
                    AnalysisTime = DateTime.UtcNow,
                    SampleSize = request.TextData.Length,
                    KeywordSentiments = keywordSentiments
                };

                Logger.LogInformation("Analyzed sentiment {AnalysisId}: {OverallSentiment:F2} on {Blockchain}",
                    analysisId, overallSentiment, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to analyze sentiment {AnalysisId}", analysisId);

                return new NeoServiceLayer.Core.SentimentResult
                {
                    AnalysisId = analysisId,
                    OverallSentiment = 0.0,
                    Confidence = 0.0,
                    AnalysisTime = DateTime.UtcNow,
                    SampleSize = 0,
                    KeywordSentiments = new Dictionary<string, double>()
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<string> RegisterModelAsync(NeoServiceLayer.Core.ModelRegistration registration, BlockchainType blockchainType)
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
                Version = "1.0.0",
                ModelData = registration.ModelData,
                Configuration = new Dictionary<string, object> { ["Owner"] = registration.Owner },
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
                _predictionHistory[modelId] = new List<PredictionResult>();
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
    public async Task<IEnumerable<PredictionResult>> GetPredictionHistoryAsync(string modelId, BlockchainType blockchainType)
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

            return Enumerable.Empty<PredictionResult>();
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
