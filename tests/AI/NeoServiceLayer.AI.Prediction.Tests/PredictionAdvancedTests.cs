using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.AI.Prediction.Models;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using NeoServiceLayer.TestInfrastructure;
using Xunit;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.Prediction.Tests;

/// <summary>
/// Advanced comprehensive tests for PredictionService covering market forecasting,
/// sentiment analysis, ML model validation, and prediction accuracy scenarios.
/// </summary>
public class PredictionAdvancedTests : IDisposable
{
    private readonly IFixture _fixture;
    private readonly Mock<ILogger<PredictionService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IPersistentStorageProvider> _mockStorageProvider;
    private readonly IEnclaveManager _enclaveManager;
    private readonly PredictionService _service;

    public PredictionAdvancedTests()
    {
        _fixture = new Fixture();
        _mockLogger = new Mock<ILogger<PredictionService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockStorageProvider = new Mock<IPersistentStorageProvider>();

        SetupConfiguration();
        SetupStorageProvider();

        // Create real EnclaveManager with TestEnclaveWrapper
        var enclaveManagerLogger = new Mock<ILogger<EnclaveManager>>();
        var testEnclaveWrapper = new TestEnclaveWrapper();
        _enclaveManager = new EnclaveManager(enclaveManagerLogger.Object, testEnclaveWrapper);

        _service = new PredictionService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockStorageProvider.Object,
            _enclaveManager);

        InitializeServiceAsync().Wait();
    }

    private async Task InitializeServiceAsync()
    {
        await _service.InitializeAsync();
        _service.IsEnclaveInitialized.Should().BeTrue();
    }

    #region Market Forecasting Tests

    [Fact]
    public async Task ForecastMarketAsync_BullMarketConditions_PredictsPriceIncrease()
    {
        // Arrange
        var request = CreateMarketForecastRequest(
            symbol: "GAS",
            marketTrend: MarketTrend.Bullish,
            timeHorizon: ForecastTimeHorizon.ShortTerm,
            currentPrice: 4.50m);

        SetupMarketData("GAS", bullishTrend: true);

        // Act
        var forecast = await _service.ForecastMarketAsync(request, BlockchainType.NeoN3);

        // Assert
        forecast.Should().NotBeNull();
        forecast.Symbol.Should().Be("GAS");
        forecast.PredictedPrices.Should().NotBeEmpty();
        forecast.PredictedPrices.Should().HaveCountGreaterThan(5);
        forecast.OverallTrend.Should().Be(MarketTrend.Bullish);
        forecast.ConfidenceLevel.Should().BeGreaterThan(0.7);
        forecast.PriceTargets.Should().ContainKey("target_high");
        forecast.PriceTargets.Should().ContainKey("target_low");
        forecast.RiskFactors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ForecastMarketAsync_BearMarketConditions_PredictsPriceDecrease()
    {
        // Arrange
        var request = CreateMarketForecastRequest(
            symbol: "NEO",
            marketTrend: MarketTrend.Bearish,
            timeHorizon: ForecastTimeHorizon.MediumTerm,
            currentPrice: 12.80m);

        SetupMarketData("NEO", bullishTrend: false);

        // Act
        var forecast = await _service.ForecastMarketAsync(request, BlockchainType.NeoN3);

        // Assert
        forecast.OverallTrend.Should().Be(MarketTrend.Bearish);
        forecast.ConfidenceLevel.Should().BeGreaterThan(0.6);
        forecast.SupportLevels.Should().NotBeEmpty();
        forecast.ResistanceLevels.Should().NotBeEmpty();
        forecast.MarketIndicators.Should().ContainKey("RSI");
        forecast.MarketIndicators.Should().ContainKey("MACD");
    }

    [Theory]
    [InlineData(ForecastTimeHorizon.ShortTerm, 24, 0.85)]
    [InlineData(ForecastTimeHorizon.MediumTerm, 168, 0.75)]
    [InlineData(ForecastTimeHorizon.LongTerm, 720, 0.65)]
    public async Task ForecastMarketAsync_VariousTimeHorizons_AppropriatePredictionCounts(
        ForecastTimeHorizon timeHorizon, int expectedHours, double minConfidence)
    {
        // Arrange
        var request = CreateMarketForecastRequest(
            symbol: "ETH",
            marketTrend: MarketTrend.Neutral,
            timeHorizon: timeHorizon,
            currentPrice: 2500m);

        SetupMarketData("ETH", bullishTrend: true);

        // Act
        var forecast = await _service.ForecastMarketAsync(request, BlockchainType.NeoX);

        // Assert
        forecast.PredictedPrices.Should().HaveCount(expectedHours);
        forecast.ConfidenceLevel.Should().BeGreaterOrEqualTo(minConfidence);
        forecast.TimeHorizon.Should().Be(timeHorizon);
        forecast.ForecastMetrics.Should().ContainKey("accuracy_score");
        forecast.ForecastMetrics.Should().ContainKey("volatility_index");
    }

    [Fact]
    public async Task ForecastMarketAsync_HighVolatilityMarket_IncludesVolatilityWarnings()
    {
        // Arrange
        var request = CreateMarketForecastRequest(
            symbol: "VOLATILE_TOKEN",
            marketTrend: MarketTrend.Volatile,
            timeHorizon: ForecastTimeHorizon.ShortTerm,
            currentPrice: 1.50m);

        SetupVolatileMarketData("VOLATILE_TOKEN");

        // Act
        var forecast = await _service.ForecastMarketAsync(request, BlockchainType.NeoX);

        // Assert
        forecast.OverallTrend.Should().Be(MarketTrend.Volatile);
        forecast.RiskFactors.Should().Contain(r => r.Contains("high volatility"));
        forecast.VolatilityMetrics.Should().NotBeNull();
        forecast.VolatilityMetrics!.VaR.Should().BeGreaterThan(0.05); // Value at Risk > 5%
        forecast.VolatilityMetrics.ExpectedShortfall.Should().BeGreaterThan(forecast.VolatilityMetrics.VaR);
        forecast.TradingRecommendations.Should().Contain(r => r.Contains("risk management"));
    }

    #endregion

    #region Sentiment Analysis Tests

    [Fact]
    public async Task AnalyzeSentimentAsync_PositiveNewsData_ReturnsPositiveSentiment()
    {
        // Arrange
        var request = CreateSentimentAnalysisRequest(
            symbol: "NEO",
            newsData: GeneratePositiveNewsData(),
            socialMediaData: GeneratePositiveSocialData(),
            includeMarketSentiment: true);

        // Act
        var result = await _service.AnalyzeSentimentAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Label.Should().Be(CoreModels.SentimentLabel.Positive);
        result.SentimentScore.Should().BeGreaterThan(0.3); // Positive sentiment score
        result.Confidence.Should().BeGreaterThan(0.5);
        result.DetailedSentiment.Should().NotBeEmpty();
        result.DetailedSentiment.Should().ContainKey("positive");
        result.DetailedSentiment["positive"].Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_NegativeMarketConditions_ReturnsNegativeSentiment()
    {
        // Arrange
        var request = CreateSentimentAnalysisRequest(
            symbol: "BTC",
            newsData: GenerateNegativeNewsData(),
            socialMediaData: GenerateNegativeSocialData(),
            includeMarketSentiment: true);

        // Act
        var result = await _service.AnalyzeSentimentAsync(request, BlockchainType.NeoX);

        // Assert
        result.Label.Should().Be(CoreModels.SentimentLabel.Negative);
        result.SentimentScore.Should().BeLessThan(-0.3); // Negative sentiment score
        result.DetailedSentiment.Should().NotBeEmpty();
        result.DetailedSentiment.Should().ContainKey("negative");
        result.DetailedSentiment["negative"].Should().BeGreaterThan(0.5);
        result.Confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_MixedSentimentSources_ReturnsBalancedAnalysis()
    {
        // Arrange
        var request = CreateSentimentAnalysisRequest(
            symbol: "ETH",
            newsData: GenerateMixedNewsData(),
            socialMediaData: GenerateMixedSocialData(),
            includeMarketSentiment: true);

        // Act
        var result = await _service.AnalyzeSentimentAsync(request, BlockchainType.NeoX);

        // Assert
        result.Label.Should().Be(CoreModels.SentimentLabel.Neutral);
        result.SentimentScore.Should().BeInRange(-0.2, 0.2); // Neutral sentiment score range
        result.DetailedSentiment.Should().NotBeEmpty();
        result.DetailedSentiment.Should().ContainKey("neutral");
        result.DetailedSentiment["neutral"].Should().BeGreaterThan(0.4);
        result.Confidence.Should().BeGreaterThan(0.3);
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_TrendingHashtags_IdentifiesViralContent()
    {
        // Arrange
        var request = CreateSentimentAnalysisRequest(
            symbol: "DOGE",
            newsData: new List<string>(),
            socialMediaData: GenerateTrendingHashtagData(),
            includeMarketSentiment: false);

        // Act
        var result = await _service.AnalyzeSentimentAsync(request, BlockchainType.NeoX);

        // Assert
        result.SentimentScore.Should().NotBe(0); // Should have some sentiment
        result.Confidence.Should().BeGreaterThan(0.5);
        result.DetailedSentiment.Should().NotBeEmpty();
        // Hashtags should influence sentiment positively
        result.DetailedSentiment.Should().ContainKey("hashtag_influence");
        result.DetailedSentiment["hashtag_influence"].Should().BeGreaterThan(0);
    }

    #endregion

    #region Prediction Model Management Tests

    [Fact]
    public async Task CreateModelAsync_TimeSeriesModel_CreatesSuccessfully()
    {
        // Arrange
        var definition = CreatePredictionModelDefinition(
            modelType: "time_series",
            algorithm: "LSTM",
            features: new[] { "price", "volume", "momentum", "sentiment" });

        // Act
        var modelId = await _service.CreateModelAsync(definition, BlockchainType.NeoN3);

        // Assert
        modelId.Should().NotBeNullOrEmpty();
        modelId.Should().StartWith("pred_model_");

        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.Is<string>(key => key.Contains("prediction_model") && key.Contains(modelId)),
            It.IsAny<byte[]>(),
            It.IsAny<StorageOptions>()), Times.Once);
    }

    [Fact]
    public async Task GetModelsAsync_WithVariousModelTypes_ReturnsAllModels()
    {
        // Arrange
        SetupExistingPredictionModels();

        // Act
        var models = await _service.GetModelsAsync(BlockchainType.NeoN3);

        // Assert
        models.Should().NotBeEmpty();
        models.Should().HaveCountGreaterThan(3);
        models.Should().Contain(m => m.ModelType == "time_series");
        models.Should().Contain(m => m.ModelType == "sentiment_analysis");
        models.Should().Contain(m => m.ModelType == "market_forecast");
        models.Should().AllSatisfy(m =>
        {
            m.ModelId.Should().NotBeNullOrEmpty();
            m.Accuracy.Should().BeInRange(0.0, 1.0);
            m.TrainingDataSize.Should().BeGreaterThan(0);
            m.LastUpdated.Should().BeBefore(DateTime.UtcNow);
        });
    }

    [Fact]
    public async Task RetrainModelAsync_WithNewData_UpdatesModelPerformance()
    {
        // Arrange
        var modelId = "existing_model_123";
        var updatedDefinition = CreatePredictionModelDefinition(
            modelType: "enhanced_time_series",
            algorithm: "Transformer",
            features: new[] { "price", "volume", "sentiment", "technical_indicators", "market_cap" });

        SetupExistingPredictionModel(modelId);

        // Act
        var result = await _service.RetrainModelAsync(modelId, updatedDefinition, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.Is<string>(key => key.Contains(modelId)),
            It.IsAny<byte[]>(),
            It.IsAny<StorageOptions>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetPredictionHistoryAsync_WithActiveModel_ReturnsHistoricalPredictions()
    {
        // Arrange
        var modelId = "history_model_456";
        SetupPredictionHistory(modelId);

        // Act
        var history = await _service.GetPredictionHistoryAsync(modelId, BlockchainType.NeoN3);

        // Assert
        history.Should().NotBeEmpty();
        history.Should().HaveCountGreaterThan(10);
        // Verify history is properly ordered by prediction timestamp
        var orderedHistory = history.OrderByDescending(h => h.PredictedAt).ToList();
        history.Should().BeEquivalentTo(orderedHistory);
        history.Should().AllSatisfy(h =>
        {
            h.ModelId.Should().Be(modelId);
            h.Accuracy.Should().BeInRange(0.0, 1.0);
            h.PredictedValue.Should().BeGreaterThan(0);
            h.ActualValue.Should().BeGreaterThan(0);
        });
    }

    #endregion

    #region ML Model Validation Tests

    [Fact]
    public async Task ValidatePredictionAccuracy_TrainedTimeSeriesModel_MeetsAccuracyTargets()
    {
        // Arrange
        var modelId = "accuracy_test_model";
        var testData = GenerateTimeSeriesTestData(1000);
        SetupTrainedPredictionModel(modelId, "time_series");

        // Act
        var validationResult = await _service.ValidatePredictionAccuracyAsync(modelId, testData, BlockchainType.NeoN3);

        // Assert
        validationResult.Should().NotBeNull();
        validationResult.MeanAbsoluteError.Should().BeLessThan(0.05);
        validationResult.RootMeanSquareError.Should().BeLessThan(0.08);
        validationResult.MeanAbsolutePercentageError.Should().BeLessThan(0.10);
        validationResult.R2Score.Should().BeGreaterThan(0.80);
        validationResult.PredictionIntervals.Should().NotBeEmpty();
        validationResult.OutlierDetection.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BacktestPredictionModel_HistoricalData_ValidatesStrategy()
    {
        // Arrange
        var modelId = "backtest_model";
        var historicalData = GenerateHistoricalMarketData(365); // 1 year of data
        SetupTrainedPredictionModel(modelId, "market_forecast");

        // Act
        var backtestResult = await _service.BacktestPredictionModelAsync(
            modelId, historicalData, lookbackDays: 30, BlockchainType.NeoN3);

        // Assert
        backtestResult.Should().NotBeNull();
        backtestResult.TotalTrades.Should().BeGreaterThan(50);
        backtestResult.WinRate.Should().BeGreaterThan(0.55);
        backtestResult.SharpeRatio.Should().BeGreaterThan(1.0);
        backtestResult.MaxDrawdown.Should().BeLessThan(0.20);
        backtestResult.ProfitFactor.Should().BeGreaterThan(1.2);
        backtestResult.MonthlyReturns.Should().HaveCount(12);
    }

    [Fact]
    public async Task AssessPredictionUncertainty_ModelVariance_QuantifiesUncertainty()
    {
        // Arrange
        var modelId = "uncertainty_model";
        var predictionRequest = CreatePredictionRequest("NEO", timeHorizon: 24);
        SetupTrainedPredictionModel(modelId, "bayesian_neural_network");

        // Act
        var uncertaintyResult = await _service.AssessPredictionUncertaintyAsync(
            modelId, predictionRequest, confidenceLevel: 0.95, BlockchainType.NeoN3);

        // Assert
        uncertaintyResult.Should().NotBeNull();
        uncertaintyResult.PredictionIntervals.Should().NotBeEmpty();
        uncertaintyResult.EpistemicUncertainty.Should().BeGreaterThan(0);
        uncertaintyResult.AleatoricUncertainty.Should().BeGreaterThan(0);
        uncertaintyResult.TotalUncertainty.Should().BeGreaterThan(uncertaintyResult.EpistemicUncertainty);
        uncertaintyResult.ConfidenceBounds.Should().ContainKey("lower_bound");
        uncertaintyResult.ConfidenceBounds.Should().ContainKey("upper_bound");
    }

    #endregion

    #region Advanced Prediction Tests

    [Fact]
    public async Task PredictAsync_MultiModalData_IntegratesAllSources()
    {
        // Arrange
        var request = CreateAdvancedPredictionRequest(
            symbol: "GAS",
            includeTechnicalAnalysis: true,
            includeSentimentData: true,
            includeMarketMicrostructure: true,
            timeHorizon: 72);

        SetupMultiModalData("GAS");

        // Act
        var result = await _service.PredictAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Predictions.Should().NotBeEmpty();
        result.FeatureImportance.Should().ContainKey("technical_indicators");
        result.FeatureImportance.Should().ContainKey("sentiment_score");
        result.FeatureImportance.Should().ContainKey("market_microstructure");
        result.DataSources.Should().Contain("price_data");
        result.DataSources.Should().Contain("news_sentiment");
        result.DataSources.Should().Contain("social_sentiment");
        result.ModelEnsemble.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task PredictAsync_EnsembleMethods_CombinesMultipleModels()
    {
        // Arrange
        var request = CreatePredictionRequest("ETH", timeHorizon: 48);
        SetupModelEnsemble(new[] { "lstm_model", "transformer_model", "random_forest_model" });

        // Act
        var result = await _service.PredictAsync(request, BlockchainType.NeoX);

        // Assert
        result.ModelEnsemble.Should().HaveCount(3);
        result.EnsembleWeights.Should().HaveCount(3);
        result.EnsembleWeights.Values.Sum().Should().BeApproximately(1.0, 0.01);
        result.IndividualPredictions.Should().ContainKey("lstm_model");
        result.IndividualPredictions.Should().ContainKey("transformer_model");
        result.IndividualPredictions.Should().ContainKey("random_forest_model");
        result.EnsembleUncertainty.Should().BeLessThan(result.IndividualPredictions.Values.Max(p => p.Uncertainty));
    }

    #endregion

    #region Performance and Scalability Tests

    [Fact]
    public async Task ConcurrentPredictions_HighLoad_HandlesEfficiently()
    {
        // Arrange
        const int concurrentRequests = 50;
        var symbols = new[] { "NEO", "GAS", "ETH", "BTC", "ADA" };
        var requests = Enumerable.Range(0, concurrentRequests)
            .Select(i => CreatePredictionRequest(symbols[i % symbols.Length], timeHorizon: 24))
            .ToList();

        SetupStorageForHighLoad();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = requests.Select(req => _service.PredictAsync(req, BlockchainType.NeoN3));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(20000); // 20 seconds max

        // Verify quality of predictions
        results.Should().AllSatisfy(r =>
        {
            r.Predictions.Should().NotBeEmpty();
            r.Confidence.Should().BeGreaterThan(0.5);
            r.ProcessingTime.Should().BeLessThan(TimeSpan.FromSeconds(3));
        });
    }

    [Fact]
    public async Task LongTermForecast_ExtendedTimeHorizon_MaintainsAccuracy()
    {
        // Arrange
        var request = CreatePredictionRequest("NEO", timeHorizon: 8760); // 1 year (365 days * 24 hours)
        SetupLongTermData("NEO");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.PredictAsync(request, BlockchainType.NeoN3);
        stopwatch.Stop();

        // Assert
        result.PredictedValues.Should().HaveCount(8760);
        result.Confidence.Should().BeGreaterThan(0.3); // Lower confidence for long-term predictions
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(45000); // 45 seconds max

        // Verify degradation handling
        result.ConfidenceDegradation.Should().NotBeEmpty();
        result.ConfidenceDegradation.First().Should().BeGreaterThan(result.ConfidenceDegradation.Last());
    }

    #endregion

    #region Helper Methods

    private void SetupConfiguration()
    {
        _mockConfiguration.Setup(x => x.GetValue("ML.PredictionModelPath", It.IsAny<string>()))
                         .Returns("/tmp/prediction_models");
        _mockConfiguration.Setup(x => x.GetValue("ML.AccuracyThreshold", "0.80"))
                         .Returns("0.80");
        _mockConfiguration.Setup(x => x.GetValue("Market.DataProvider", "default"))
                         .Returns("binance");
    }

    private void SetupStorageProvider()
    {
        _mockStorageProvider.Setup(x => x.StoreAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>()))
                          .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.DeleteAsync(It.IsAny<string>()))
                          .ReturnsAsync(true);
    }

    private MarketForecastRequest CreateMarketForecastRequest(
        string symbol,
        MarketTrend marketTrend,
        ForecastTimeHorizon timeHorizon,
        decimal currentPrice)
    {
        return new MarketForecastRequest
        {
            Symbol = symbol,
            TimeHorizon = timeHorizon,
            CurrentPrice = currentPrice,
            MarketData = CreateMarketDataContext(symbol, marketTrend),
            TechnicalIndicators = new Dictionary<string, double>
            {
                ["RSI"] = 45.5,
                ["MACD"] = 0.12,
                ["BollingerBands"] = 0.8,
                ["VolumeProfile"] = 1.2
            },
            RiskParameters = new Dictionary<string, double>
            {
                ["max_drawdown"] = 0.15,
                ["volatility_threshold"] = 0.25
            }
        };
    }

    private CoreModels.SentimentAnalysisRequest CreateSentimentAnalysisRequest(
        string symbol,
        List<string> newsData,
        List<string> socialMediaData,
        bool includeMarketSentiment)
    {
        // Combine all text data into a single text for Core sentiment analysis
        var allText = string.Join("\n", newsData.Concat(socialMediaData));

        return new CoreModels.SentimentAnalysisRequest
        {
            Text = allText,
            Language = "en",
            IncludeDetailedAnalysis = includeMarketSentiment,
            Parameters = new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["news_data_count"] = newsData.Count,
                ["social_data_count"] = socialMediaData.Count,
                ["analysis_depth"] = "comprehensive",
                ["time_window_hours"] = 24,
                ["language_filters"] = new[] { "en", "zh", "jp" },
                ["source_weights"] = new Dictionary<string, double>
                {
                    ["news"] = 0.4,
                    ["twitter"] = 0.3,
                    ["reddit"] = 0.2,
                    ["telegram"] = 0.1
                }
            }
        };
    }

    private PredictionModelDefinition CreatePredictionModelDefinition(
        string modelType,
        string algorithm,
        string[] features)
    {
        return new PredictionModelDefinition
        {
            Name = $"{modelType} Model",
            Type = AIModelType.Prediction,
            Algorithm = algorithm,
            InputFeatures = features.ToList(),
            Version = "2.0",
            TrainingConfig = new Dictionary<string, object>
            {
                ["sequence_length"] = 60,
                ["prediction_horizon"] = 24,
                ["batch_size"] = 32,
                ["learning_rate"] = 0.001
            },
            Hyperparameters = new Dictionary<string, object>
            {
                ["hidden_units"] = 128,
                ["num_layers"] = 3,
                ["dropout_rate"] = 0.2,
                ["regularization"] = 0.01
            },
            PredictionType = PredictionType.TimeSeries,
            TargetVariable = "price"
        };
    }

    private CoreModels.PredictionRequest CreatePredictionRequest(string symbol, int timeHorizon)
    {
        return new CoreModels.PredictionRequest
        {
            ModelId = "default_prediction_model",
            InputData = new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["time_horizon"] = timeHorizon,
                ["price_history"] = GeneratePriceHistory(symbol, 100),
                ["volume_history"] = GenerateVolumeHistory(100),
                ["market_indicators"] = new { RSI = 45.5, MACD = 0.12 }
            },
            Parameters = new Dictionary<string, object>
            {
                ["confidence_level"] = 0.95
            }
        };
    }

    private CoreModels.PredictionRequest CreateAdvancedPredictionRequest(
        string symbol,
        bool includeTechnicalAnalysis,
        bool includeSentimentData,
        bool includeMarketMicrostructure,
        int timeHorizon)
    {
        var inputData = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["time_horizon"] = timeHorizon,
            ["price_history"] = GeneratePriceHistory(symbol, 200)
        };

        if (includeTechnicalAnalysis)
        {
            inputData["technical_indicators"] = new
            {
                RSI = 45.5,
                MACD = 0.12,
                BollingerBands = new { Upper = 15.2, Lower = 12.8 },
                MovingAverages = new { MA20 = 14.1, MA50 = 13.9 }
            };
        }

        if (includeSentimentData)
        {
            inputData["sentiment_data"] = new
            {
                OverallSentiment = 0.65,
                NewsSentiment = 0.72,
                SocialSentiment = 0.58
            };
        }

        if (includeMarketMicrostructure)
        {
            inputData["market_microstructure"] = new
            {
                BidAskSpread = 0.02,
                OrderBookDepth = 1000000,
                TradeSize = 5000
            };
        }

        return new CoreModels.PredictionRequest
        {
            ModelId = "advanced_ensemble_model",
            InputData = inputData,
            Parameters = new Dictionary<string, object>
            {
                ["confidence_level"] = 0.95
            }
        };
    }

    private List<string> GeneratePositiveNewsData()
    {
        return new List<string>
        {
            "Neo blockchain announces major partnership with tech giant",
            "GAS token utility expands to new DeFi protocols",
            "Positive regulatory developments boost Neo ecosystem",
            "Record transaction volume on Neo N3 network",
            "Neo Foundation launches new developer incentive program"
        };
    }

    private List<string> GenerateNegativeNewsData()
    {
        return new List<string>
        {
            "Market volatility affects cryptocurrency prices",
            "Regulatory concerns impact blockchain adoption",
            "Trading volumes decline across major exchanges",
            "Technical issues reported on network",
            "Bear market sentiment continues to persist"
        };
    }

    private List<string> GenerateMixedNewsData()
    {
        return new List<string>
        {
            "Neo announces partnership while market remains volatile",
            "Mixed signals from regulatory bodies on crypto policy",
            "Technical upgrade complete but adoption uncertain",
            "Trading volumes stable despite market concerns",
            "Developer activity increases while prices fluctuate"
        };
    }

    private List<string> GeneratePositiveSocialData()
    {
        return new List<string>
        {
            "Bullish on #NEO! Great fundamentals and strong community",
            "Just bought more $GAS, loving the utility expansion",
            "#NeoBlockchain has the best developer experience",
            "Smart contracts on Neo are incredibly efficient",
            "Neo's consensus mechanism is revolutionary"
        };
    }

    private List<string> GenerateNegativeSocialData()
    {
        return new List<string>
        {
            "Concerned about the market direction #crypto",
            "Selling pressure continues on most altcoins",
            "Bearish signals from technical analysis #NEO",
            "Market sentiment remains pessimistic",
            "Risk management is crucial in this environment"
        };
    }

    private List<string> GenerateMixedSocialData()
    {
        return new List<string>
        {
            "Mixed feelings about #NEO - good tech but tough market",
            "Holding long term despite short term volatility",
            "Great project but waiting for better entry point",
            "Technical progress vs market conditions conflict",
            "Cautiously optimistic about the future"
        };
    }

    private List<string> GenerateTrendingHashtagData()
    {
        return new List<string>
        {
            "#DOGE trending after celebrity endorsement",
            "#ToTheMoon viral across social platforms",
            "#DiamondHands community growing rapidly",
            "#HODL movement gains momentum",
            "#CryptoCommunity shows strong support"
        };
    }

    private List<object> GenerateTimeSeriesTestData(int count)
    {
        var random = new Random(42);
        var basePrice = 10.0;

        return Enumerable.Range(0, count)
            .Select(i =>
            {
                basePrice += (random.NextDouble() - 0.5) * 0.5;
                return new
                {
                    Timestamp = DateTime.UtcNow.AddHours(-count + i),
                    Price = Math.Max(0.1, basePrice),
                    Volume = random.Next(1000, 10000),
                    PredictedPrice = Math.Max(0.1, basePrice + (random.NextDouble() - 0.5) * 0.2)
                };
            })
            .Cast<object>()
            .ToList();
    }

    private List<object> GenerateHistoricalMarketData(int days)
    {
        var random = new Random(42);
        var basePrice = 15.0;

        return Enumerable.Range(0, days)
            .Select(i =>
            {
                basePrice *= (1 + (random.NextDouble() - 0.5) * 0.05);
                return new
                {
                    Date = DateTime.UtcNow.AddDays(-days + i),
                    Open = Math.Max(0.1, basePrice),
                    High = Math.Max(0.1, basePrice * (1 + random.NextDouble() * 0.05)),
                    Low = Math.Max(0.1, basePrice * (1 - random.NextDouble() * 0.05)),
                    Close = Math.Max(0.1, basePrice),
                    Volume = random.Next(10000, 100000)
                };
            })
            .Cast<object>()
            .ToList();
    }

    private decimal[] GeneratePriceHistory(string symbol, int count)
    {
        var random = new Random(symbol.GetHashCode());
        var basePrice = symbol == "NEO" ? 15.0m : symbol == "GAS" ? 4.5m : 2500.0m;

        return Enumerable.Range(0, count)
            .Select(i => Math.Max(0.01m, basePrice + (decimal)(random.NextDouble() - 0.5) * basePrice * 0.1m))
            .ToArray();
    }

    private int[] GenerateVolumeHistory(int count)
    {
        var random = new Random(42);
        return Enumerable.Range(0, count)
            .Select(i => random.Next(1000, 50000))
            .ToArray();
    }

    private void SetupMarketData(string symbol, bool bullishTrend)
    {
        var marketData = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["trend"] = bullishTrend ? "bullish" : "bearish",
            ["volume"] = 1000000,
            ["volatility"] = 0.15
        };

        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(symbol))))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(marketData)));
    }

    private void SetupVolatileMarketData(string symbol)
    {
        var marketData = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["trend"] = "volatile",
            ["volume"] = 2000000,
            ["volatility"] = 0.45
        };

        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(symbol))))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(marketData)));
    }

    private void SetupExistingPredictionModels()
    {
        var models = new List<PredictionModel>
        {
            new PredictionModel
            {
                Id = "pred_model_1",
                Name = "TimeSeries Model",
                ModelType = "time_series",
                Accuracy = 0.85,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new PredictionModel
            {
                Id = "pred_model_2",
                Name = "Sentiment Model",
                ModelType = "sentiment_analysis",
                Accuracy = 0.78,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new PredictionModel
            {
                Id = "pred_model_3",
                Name = "Market Forecast Model",
                ModelType = "market_forecast",
                Accuracy = 0.82,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new PredictionModel
            {
                Id = "pred_model_4",
                Name = "Deep Learning Model",
                ModelType = "deep_learning",
                Accuracy = 0.88,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        // Note: ListAsync is not available on IPersistentStorageProvider - using alternative approach
        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.IsAny<string>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(models)));
    }

    private void SetupExistingPredictionModel(string modelId)
    {
        var model = new PredictionModel
        {
            Id = modelId,
            Name = "Existing Model",
            ModelType = "time_series",
            Accuracy = 0.80,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(modelId))))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(model)));
    }

    private void SetupTrainedPredictionModel(string modelId, string modelType)
    {
        var model = new PredictionModel
        {
            Id = modelId,
            Name = $"Trained {modelType} Model",
            ModelType = modelType,
            Accuracy = 0.85,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Version = "2.0"
        };

        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(modelId))))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(model)));
    }

    private void SetupPredictionHistory(string modelId)
    {
        var history = Enumerable.Range(0, 20)
            .Select(i => new CoreModels.PredictionResult
            {
                ModelId = modelId,
                PredictionId = $"pred_{i}",
                Confidence = 0.75 + (i % 3) * 0.05,
                PredictedAt = DateTime.UtcNow.AddHours(-i),
                ProcessingTimeMs = 50 + i * 5
            })
            .ToList();

        // Note: ListAsync is not available on IPersistentStorageProvider - using alternative approach
        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(modelId))))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(history)));
    }

    private void SetupMultiModalData(string symbol)
    {
        var multiModalData = new Dictionary<string, object>
        {
            ["price_data"] = GeneratePriceHistory(symbol, 200),
            ["technical_indicators"] = new Dictionary<string, double>
            {
                ["RSI"] = 45.5,
                ["MACD"] = 0.12,
                ["BollingerBands"] = 0.8
            },
            ["sentiment_score"] = 0.65,
            ["market_microstructure"] = new Dictionary<string, object>
            {
                ["bid_ask_spread"] = 0.02,
                ["order_book_depth"] = 1000000
            },
            ["news_sentiment"] = 0.72,
            ["social_sentiment"] = 0.58
        };

        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(symbol))))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(multiModalData)));
    }

    private void SetupModelEnsemble(string[] modelIds)
    {
        foreach (var modelId in modelIds)
        {
            var model = new PredictionModel
            {
                Id = modelId,
                Name = $"{modelId} Model",
                ModelType = modelId.Contains("lstm") ? "lstm" : modelId.Contains("transformer") ? "transformer" : "random_forest",
                Accuracy = 0.80 + modelIds.ToList().IndexOf(modelId) * 0.02,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(modelId))))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(model)));
        }
    }

    private void SetupStorageForHighLoad()
    {
        _mockStorageProvider.Setup(x => x.StoreAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>()))
            .ReturnsAsync(true);

        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.IsAny<string>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["model_available"] = true,
                ["processing_capacity"] = 100
            })));
    }

    private void SetupLongTermData(string symbol)
    {
        var longTermData = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["historical_data"] = GenerateHistoricalMarketData(365),
            ["long_term_trends"] = new Dictionary<string, object>
            {
                ["yearly_trend"] = "growth",
                ["seasonal_patterns"] = new[] { "Q1_weak", "Q2_strong", "Q3_neutral", "Q4_strong" }
            },
            ["volatility_history"] = Enumerable.Range(0, 365).Select(i => 0.10 + (i % 30) * 0.001).ToArray()
        };

        _mockStorageProvider.Setup(x => x.RetrieveAsync(It.Is<string>(key => key.Contains(symbol))))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(longTermData)));
    }

    private Dictionary<string, object> CreateMarketDataContext(string symbol, MarketTrend trend)
    {
        return new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["trend"] = trend.ToString(),
            ["price_history"] = GeneratePriceHistory(symbol, 100),
            ["volume_history"] = GenerateVolumeHistory(100),
            ["market_cap"] = symbol == "NEO" ? 1500000000 : symbol == "GAS" ? 450000000 : 400000000000,
            ["circulating_supply"] = symbol == "NEO" ? 100000000 : symbol == "GAS" ? 100000000 : 150000000
        };
    }

    public void Dispose()
    {
        _enclaveManager?.DisposeAsync().AsTask().Wait();
        GC.SuppressFinalize(this);
    }

    #endregion
}

// Supporting types are now defined in NeoServiceLayer.AI.Prediction.Models 
