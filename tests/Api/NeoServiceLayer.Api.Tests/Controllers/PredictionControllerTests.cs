using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.AI.Prediction.Models;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Core;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Security.Claims;


namespace NeoServiceLayer.Api.Tests.Controllers;

/// <summary>
/// Unit tests for PredictionController.
/// </summary>
public class PredictionControllerTests
{
    private readonly Mock<AI.Prediction.IPredictionService> _predictionServiceMock;
    private readonly Mock<ILogger<PredictionController>> _loggerMock;
    private readonly PredictionController _controller;

    public PredictionControllerTests()
    {
        _predictionServiceMock = new Mock<AI.Prediction.IPredictionService>();
        _loggerMock = new Mock<ILogger<PredictionController>>();
        _controller = new PredictionController(_predictionServiceMock.Object, _loggerMock.Object);

        // Setup controller context with authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    #region Prediction Tests

    [Fact]
    public async Task Predict_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new Core.Models.PredictionRequest
        {
            ModelId = "model-123",
            InputData = new Dictionary<string, object> { { "price", 100 }, { "volume", 1000 } },
            Parameters = new Dictionary<string, object>()
        };

        var expectedResult = new Core.Models.PredictionResult
        {
            PredictionId = "prediction-123",
            ModelId = "model-123",
            PredictedValue = 105.5,
            Confidence = 0.85,
            PredictedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>()
        };

        _predictionServiceMock
            .Setup(s => s.PredictAsync(request, BlockchainType.NeoN3))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Predict(request, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<Core.Models.PredictionResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResult, response.Data);
        Assert.Equal("Prediction completed successfully", response.Message);

        _predictionServiceMock.Verify(s => s.PredictAsync(request, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task Predict_WithInvalidBlockchainType_ReturnsBadRequest()
    {
        // Arrange
        var request = new Core.Models.PredictionRequest();

        // Act
        var result = await _controller.Predict(request, "InvalidChain");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid blockchain type", response.Message);
    }

    [Fact]
    public async Task Predict_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new Core.Models.PredictionRequest();
        _predictionServiceMock
            .Setup(s => s.PredictAsync(It.IsAny<Core.Models.PredictionRequest>(), It.IsAny<BlockchainType>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _controller.Predict(request, "NeoN3");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Service error", response.Message);
    }

    #endregion

    #region Sentiment Analysis Tests

    [Fact]
    public async Task AnalyzeSentiment_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new Core.Models.SentimentAnalysisRequest
        {
            Text = "This is a great investment opportunity!",
            Language = "en",
            IncludeDetailedAnalysis = true,
            Parameters = new Dictionary<string, object> { { "source", "social_media" } }
        };

        var expectedResult = new Core.Models.SentimentResult
        {
            AnalysisId = "sentiment-123",
            SentimentScore = 0.75, // Positive sentiment
            Label = Core.Models.SentimentLabel.Positive,
            Confidence = 0.90,
            DetailedSentiment = new Dictionary<string, double> { { "joy", 0.8 }, { "trust", 0.7 } },
            // KeyPhrases = new List<string> { "great investment", "opportunity" }, // Property doesn't exist
            AnalyzedAt = DateTime.UtcNow
        };

        _predictionServiceMock
            .Setup(s => s.AnalyzeSentimentAsync(request, BlockchainType.NeoN3))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.AnalyzeSentiment(request, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<Core.Models.SentimentResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResult, response.Data);
        Assert.Equal("Sentiment analysis completed successfully", response.Message);

        _predictionServiceMock.Verify(s => s.AnalyzeSentimentAsync(request, BlockchainType.NeoN3), Times.Once);
    }

    #endregion

    #region Market Forecast Tests

    [Fact]
    public async Task ForecastMarket_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new MarketForecastRequest
        {
            AssetSymbol = "NEO",
            ForecastHorizonDays = 7,
            HistoricalPeriodDays = 30,
            AdditionalFeatures = new List<string> { "volume", "sentiment" },
            ConfidenceLevel = 0.95
        };

        var expectedResult = new MarketForecast
        {
            AssetSymbol = "NEO",
            Forecasts = new List<PriceForecast>
            {
                new PriceForecast
                {
                    Date = DateTime.UtcNow.AddDays(1),
                    PredictedPrice = 15.50m,
                    Confidence = 0.85,
                    Interval = new ConfidenceInterval
                    {
                        LowerBound = 14.50m,
                        UpperBound = 16.50m,
                        ConfidenceLevel = 0.95
                    }
                }
            },
            ConfidenceIntervals = new Dictionary<string, ConfidenceInterval>(),
            Metrics = new ForecastMetrics
            {
                MeanAbsoluteError = 0.15,
                RootMeanSquareError = 0.20,
                MeanAbsolutePercentageError = 0.05,
                RSquared = 0.85
            },
            ForecastedAt = DateTime.UtcNow
        };

        _predictionServiceMock
            .Setup(s => s.ForecastMarketAsync(request, BlockchainType.NeoN3))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ForecastMarket(request, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<MarketForecast>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResult, response.Data);
        Assert.Equal("Market forecast completed successfully", response.Message);

        _predictionServiceMock.Verify(s => s.ForecastMarketAsync(request, BlockchainType.NeoN3), Times.Once);
    }

    #endregion

    #region Model Management Tests

    [Fact]
    public async Task CreateModel_WithValidDefinition_ReturnsOkResult()
    {
        // Arrange
        var definition = new PredictionModelDefinition
        {
            Name = "Price Prediction Model",
            Description = "Model for predicting asset prices",
            PredictionType = PredictionType.Price,
            TargetVariable = "price",
            TimeSeriesConfig = new TimeSeriesConfig
            {
                WindowSize = 30,
                ForecastHorizon = 7,
                UseTrend = true,
                UseSeasonality = true
            },
            ValidationStrategy = ValidationStrategy.TimeSeriesSplit
        };

        var expectedModelId = "model-123";

        _predictionServiceMock
            .Setup(s => s.CreateModelAsync(definition, BlockchainType.NeoN3))
            .ReturnsAsync(expectedModelId);

        // Act
        var result = await _controller.CreateModel(definition, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedModelId, response.Data);
        Assert.Equal("Model created successfully", response.Message);

        _predictionServiceMock.Verify(s => s.CreateModelAsync(definition, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task RegisterModel_WithValidRegistration_ReturnsOkResult()
    {
        // Arrange
        var registration = new Core.Models.ModelRegistration
        {
            Name = "Registered Model",
            Type = "TimeSeries",
            Version = "1.0",
            ModelData = new byte[] { 1, 2, 3, 4, 5 },
            Configuration = new Dictionary<string, object> { { "version", "1.0" } },
            Description = "Test registered model"
        };

        var expectedModelId = "registered-model-123";

        _predictionServiceMock
            .Setup(s => s.RegisterModelAsync(registration, BlockchainType.NeoN3))
            .ReturnsAsync(expectedModelId);

        // Act
        var result = await _controller.RegisterModel(registration, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedModelId, response.Data);
        Assert.Equal("Model registered successfully", response.Message);

        _predictionServiceMock.Verify(s => s.RegisterModelAsync(registration, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task GetModels_WithValidBlockchainType_ReturnsOkResult()
    {
        // Arrange
        var expectedModels = new List<PredictionModel>
        {
            new PredictionModel
            {
                ModelId = "model-1",
                Name = "Price Model",
                PredictionType = PredictionType.Price,
                TimeHorizon = TimeSpan.FromDays(7),
                MinConfidenceThreshold = 0.7,
                FeatureImportance = new Dictionary<string, double> { { "volume", 0.8 } }
            },
            new PredictionModel
            {
                ModelId = "model-2",
                Name = "Sentiment Model",
                PredictionType = PredictionType.Sentiment,
                TimeHorizon = TimeSpan.FromHours(1),
                MinConfidenceThreshold = 0.6,
                FeatureImportance = new Dictionary<string, double> { { "text_features", 0.9 } }
            }
        };

        _predictionServiceMock
            .Setup(s => s.GetModelsAsync(BlockchainType.NeoN3))
            .ReturnsAsync(expectedModels);

        // Act
        var result = await _controller.GetModels("NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IEnumerable<PredictionModel>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedModels, response.Data);
        Assert.Equal("Models retrieved successfully", response.Message);

        _predictionServiceMock.Verify(s => s.GetModelsAsync(BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task GetModel_WithValidModelId_ReturnsOkResult()
    {
        // Arrange
        var modelId = "model-123";
        var expectedModel = new PredictionModel
        {
            ModelId = modelId,
            Name = "Test Prediction Model",
            PredictionType = PredictionType.Price,
            TimeHorizon = TimeSpan.FromDays(7),
            MinConfidenceThreshold = 0.75,
            FeatureImportance = new Dictionary<string, double>
            {
                { "price_history", 0.6 },
                { "volume", 0.3 },
                { "sentiment", 0.1 }
            }
        };

        _predictionServiceMock
            .Setup(s => s.GetModelAsync(modelId, BlockchainType.NeoN3))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _controller.GetModel(modelId, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredictionModel>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedModel, response.Data);
        Assert.Equal("Model retrieved successfully", response.Message);

        _predictionServiceMock.Verify(s => s.GetModelAsync(modelId, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task GetPredictionHistory_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var modelId = "model-123";
        var expectedHistory = new List<Core.Models.PredictionResult>
        {
            new Core.Models.PredictionResult
            {
                PredictionId = "pred-1",
                ModelId = modelId,
                PredictedValue = 15.5,
                Confidence = 0.85,
                PredictedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Core.Models.PredictionResult
            {
                PredictionId = "pred-2",
                ModelId = modelId,
                PredictedValue = 16.0,
                Confidence = 0.80,
                PredictedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _predictionServiceMock
            .Setup(s => s.GetPredictionHistoryAsync(modelId, BlockchainType.NeoN3))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetPredictionHistory("NeoN3", modelId, 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<Core.Models.PredictionResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedHistory, response.Data);
        Assert.Equal("Prediction history retrieved successfully", response.Message);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(2, response.TotalItems);

        _predictionServiceMock.Verify(s => s.GetPredictionHistoryAsync(modelId, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task GetPredictionHistory_WithInvalidPage_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPredictionHistory("NeoN3", "model-123", 0, 20);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Page number must be greater than 0", response.Message);
    }

    [Fact]
    public async Task GetPredictionHistory_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPredictionHistory("NeoN3", "model-123", 1, 101);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Page size must be between 1 and 100", response.Message);
    }

    [Fact]
    public async Task RetrainModel_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var modelId = "model-123";
        var definition = new PredictionModelDefinition
        {
            Name = "Retrained Model",
            Description = "Updated model with new data"
        };

        _predictionServiceMock
            .Setup(s => s.RetrainModelAsync(modelId, definition, BlockchainType.NeoN3))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RetrainModel(modelId, definition, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        Assert.Equal("Model retrained successfully", response.Message);

        _predictionServiceMock.Verify(s => s.RetrainModelAsync(modelId, definition, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task RetrainModel_WithNonExistentModel_ReturnsNotFound()
    {
        // Arrange
        var modelId = "non-existent-model";
        var definition = new PredictionModelDefinition();

        _predictionServiceMock
            .Setup(s => s.RetrainModelAsync(modelId, definition, BlockchainType.NeoN3))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RetrainModel(modelId, definition, "NeoN3");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains($"Model {modelId} not found", response.Message);
    }

    [Fact]
    public async Task DeleteModel_WithValidModelId_ReturnsOkResult()
    {
        // Arrange
        var modelId = "model-123";

        _predictionServiceMock
            .Setup(s => s.DeleteModelAsync(modelId, BlockchainType.NeoN3))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteModel(modelId, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        Assert.Equal("Model deleted successfully", response.Message);

        _predictionServiceMock.Verify(s => s.DeleteModelAsync(modelId, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task DeleteModel_WithNonExistentModel_ReturnsNotFound()
    {
        // Arrange
        var modelId = "non-existent-model";

        _predictionServiceMock
            .Setup(s => s.DeleteModelAsync(modelId, BlockchainType.NeoN3))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteModel(modelId, "NeoN3");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains($"Model {modelId} not found", response.Message);
    }

    #endregion
}
