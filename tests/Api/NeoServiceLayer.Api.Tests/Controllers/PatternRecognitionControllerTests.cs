﻿using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.AI.PatternRecognition;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.Api.Tests.Controllers;

/// <summary>
/// Unit tests for PatternRecognitionController.
/// </summary>
public class PatternRecognitionControllerTests
{
    private readonly Mock<AI.PatternRecognition.IPatternRecognitionService> _patternRecognitionServiceMock;
    private readonly Mock<ILogger<PatternRecognitionController>> _loggerMock;
    private readonly PatternRecognitionController _controller;

    public PatternRecognitionControllerTests()
    {
        _patternRecognitionServiceMock = new Mock<AI.PatternRecognition.IPatternRecognitionService>();
        _loggerMock = new Mock<ILogger<PatternRecognitionController>>();
        _controller = new PatternRecognitionController(_patternRecognitionServiceMock.Object, _loggerMock.Object);

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

    #region Fraud Detection Tests

    [Fact]
    public async Task DetectFraud_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new AI.PatternRecognition.Models.FraudDetectionRequest
        {
            TransactionId = "test-tx-123",
            TransactionData = new Dictionary<string, object> { { "amount", 1000 } },
            Amount = 1000,
            Parameters = new Dictionary<string, object> { { "Sensitivity", "Standard" } }
        };

        var expectedResult = new AI.PatternRecognition.Models.FraudDetectionResult
        {
            DetectionId = "fraud-123",
            TransactionId = "test-tx-123",
            FraudScore = 0.75,
            IsFraudulent = true,
            RiskLevel = AI.PatternRecognition.Models.RiskLevel.Medium,
            RiskFactors = new Dictionary<string, double> { { "confidence", 0.85 } },
            DetectedAt = DateTime.UtcNow
        };

        _patternRecognitionServiceMock
            .Setup(s => s.DetectFraudAsync(request, BlockchainType.NeoN3))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.DetectFraud(request, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AI.PatternRecognition.Models.FraudDetectionResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResult, response.Data);
        Assert.Equal("Fraud detection completed successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.DetectFraudAsync(request, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task DetectFraud_WithInvalidBlockchainType_ReturnsBadRequest()
    {
        // Arrange
        var request = new AI.PatternRecognition.Models.FraudDetectionRequest();

        // Act
        var result = await _controller.DetectFraud(request, "InvalidChain");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid blockchain type", response.Message);
    }

    [Fact]
    public async Task DetectFraud_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new AI.PatternRecognition.Models.FraudDetectionRequest();
        _patternRecognitionServiceMock
            .Setup(s => s.DetectFraudAsync(It.IsAny<AI.PatternRecognition.Models.FraudDetectionRequest>(), It.IsAny<BlockchainType>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _controller.DetectFraud(request, "NeoN3");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Service error", response.Message);
    }

    #endregion

    #region Pattern Analysis Tests

    [Fact]
    public async Task AnalyzePatterns_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new PatternAnalysisRequest
        {
            ModelId = "test-pattern-model",
            InputData = new Dictionary<string, object> { { "transactions", new[] { 1, 2, 3 } } },
            Parameters = new Dictionary<string, object> { { "MinimumConfidence", 0.7 }, { "AnalysisType", "General" } }
        };

        var expectedResult = new PatternAnalysisResult
        {
            AnalysisId = "analysis-123",
            DetectedPatterns = new List<DetectedPattern>
            {
                new DetectedPattern
                {
                    Id = "pattern-1",
                    Name = "Frequency Pattern",
                    Type = AI.PatternRecognition.Models.PatternRecognitionType.FraudDetection, // Use correct enum
                    Confidence = 0.85
                }
            },
            // OverallScore = 0.8, // Property doesn't exist
            Confidence = 0.85,
            AnalyzedAt = DateTime.UtcNow
        };

        _patternRecognitionServiceMock
            .Setup(s => s.AnalyzePatternsAsync(request, BlockchainType.NeoN3))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.AnalyzePatterns(request, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PatternAnalysisResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResult, response.Data);
        Assert.Equal("Pattern analysis completed successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.AnalyzePatternsAsync(request, BlockchainType.NeoN3), Times.Once);
    }

    #endregion

    #region Behavior Analysis Tests

    [Fact]
    public async Task AnalyzeBehavior_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new BehaviorAnalysisRequest
        {
            UserId = "user-123",
            // BehaviorData property doesn't exist
            AnalysisWindow = TimeSpan.FromDays(30),
            // CompareWithBaseline property doesn't exist
        };

        var expectedResult = new BehaviorAnalysisResult
        {
            AnalysisId = "behavior-123",
            Address = "user-123", // No UserId property, using Address instead
            RiskScore = 0.7, // No BehaviorScore property, using RiskScore instead
            RiskLevel = AI.PatternRecognition.Models.RiskLevel.Medium,
            // BehaviorPatterns = new List<BehaviorPattern>(), // Property doesn't exist
            // Anomalies = new List<BehaviorAnomaly>(), // Property doesn't exist
            AnalyzedAt = DateTime.UtcNow
        };

        _patternRecognitionServiceMock
            .Setup(s => s.AnalyzeBehaviorAsync(request, BlockchainType.NeoN3))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.AnalyzeBehavior(request, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BehaviorAnalysisResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResult, response.Data);
        Assert.Equal("Behavior analysis completed successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.AnalyzeBehaviorAsync(request, BlockchainType.NeoN3), Times.Once);
    }

    #endregion

    #region Risk Assessment Tests

    [Fact]
    public async Task AssessRisk_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new AI.PatternRecognition.Models.RiskAssessmentRequest
        {
            TransactionId = "tx-123",
            EntityId = "entity-123",
            EntityType = "User",
            TransactionData = new Dictionary<string, object> { { "transactionCount", 10 } },
            HistoricalData = new Dictionary<string, object> { { "previousTransactions", 100 } }
        };

        var expectedResult = new AI.PatternRecognition.Models.RiskAssessmentResult
        {
            AssessmentId = "risk-123",
            EntityId = "entity-123",
            RiskScore = 0.6,
            RiskLevel = AI.PatternRecognition.Models.RiskLevel.Medium,
            // RiskFactorScores, IdentifiedRisks, and Recommendations properties don't exist
            Confidence = 0.8,
            AssessedAt = DateTime.UtcNow
        };

        _patternRecognitionServiceMock
            .Setup(s => s.AssessRiskAsync(request, BlockchainType.NeoN3))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.AssessRisk(request, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AI.PatternRecognition.Models.RiskAssessmentResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResult, response.Data);
        Assert.Equal("Risk assessment completed successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.AssessRiskAsync(request, BlockchainType.NeoN3), Times.Once);
    }

    #endregion

    #region Model Management Tests

    [Fact]
    public async Task CreateModel_WithValidDefinition_ReturnsOkResult()
    {
        // Arrange
        var definition = new PatternModelDefinition
        {
            Name = "Test Model",
            Description = "Test pattern recognition model",
            ModelType = "Classification",
            Configuration = new Dictionary<string, object>()
        };

        var expectedModelId = "model-123";

        _patternRecognitionServiceMock
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

        _patternRecognitionServiceMock.Verify(s => s.CreateModelAsync(definition, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task GetModels_WithValidBlockchainType_ReturnsOkResult()
    {
        // Arrange
        var expectedModels = new List<PatternModel>
        {
            new PatternModel
            {
                ModelId = "model-1",
                Name = "Test Model 1",
                ModelType = "Classification", // Use string instead of enum
                PatternType = PatternRecognitionType.FraudDetection,
                IsActive = true, // Status property doesn't exist
                CreatedAt = DateTime.UtcNow
            },
            new PatternModel
            {
                ModelId = "model-2",
                Name = "Test Model 2",
                ModelType = "AnomalyDetection", // Use string instead of enum
                PatternType = PatternRecognitionType.AnomalyDetection,
                IsActive = true, // Status property doesn't exist
                CreatedAt = DateTime.UtcNow
            }
        };

        _patternRecognitionServiceMock
            .Setup(s => s.GetModelsAsync(BlockchainType.NeoN3))
            .ReturnsAsync(expectedModels);

        // Act
        var result = await _controller.GetModels("NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IEnumerable<PatternModel>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedModels, response.Data);
        Assert.Equal("Models retrieved successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.GetModelsAsync(BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task GetModel_WithValidModelId_ReturnsOkResult()
    {
        // Arrange
        var modelId = "model-123";
        var expectedModel = new PatternModel
        {
            ModelId = modelId,
            Name = "Test Model",
            ModelType = "Classification", // Use string instead of enum
            PatternType = PatternRecognitionType.FraudDetection,
            IsActive = true, // Status property doesn't exist
            CreatedAt = DateTime.UtcNow,
            // AccuracyMetrics = new Dictionary<string, double> { { "accuracy", 0.95 } } // Property doesn't exist
            Accuracy = 0.95
        };

        _patternRecognitionServiceMock
            .Setup(s => s.GetModelAsync(modelId, BlockchainType.NeoN3))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _controller.GetModel(modelId, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PatternModel>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedModel, response.Data);
        Assert.Equal("Model retrieved successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.GetModelAsync(modelId, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task UpdateModel_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var modelId = "model-123";
        var definition = new PatternModelDefinition
        {
            Name = "Updated Model",
            Description = "Updated description"
        };

        _patternRecognitionServiceMock
            .Setup(s => s.UpdateModelAsync(modelId, definition, BlockchainType.NeoN3))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateModel(modelId, definition, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        Assert.Equal("Model updated successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.UpdateModelAsync(modelId, definition, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task UpdateModel_WithNonExistentModel_ReturnsNotFound()
    {
        // Arrange
        var modelId = "non-existent-model";
        var definition = new PatternModelDefinition();

        _patternRecognitionServiceMock
            .Setup(s => s.UpdateModelAsync(modelId, definition, BlockchainType.NeoN3))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateModel(modelId, definition, "NeoN3");

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

        _patternRecognitionServiceMock
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

        _patternRecognitionServiceMock.Verify(s => s.DeleteModelAsync(modelId, BlockchainType.NeoN3), Times.Once);
    }

    #endregion

    #region History and Profile Tests

    [Fact]
    public async Task GetFraudDetectionHistory_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var userId = "user-123";
        var expectedHistory = new List<AI.PatternRecognition.Models.FraudDetectionResult>
        {
            new AI.PatternRecognition.Models.FraudDetectionResult
            {
                DetectionId = "fraud-1",
                FraudScore = 0.8,
                IsFraudulent = true,
                DetectedAt = DateTime.UtcNow.AddDays(-1)
            },
            new AI.PatternRecognition.Models.FraudDetectionResult
            {
                DetectionId = "fraud-2",
                FraudScore = 0.3,
                IsFraudulent = false,
                DetectedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        _patternRecognitionServiceMock
            .Setup(s => s.GetFraudDetectionHistoryAsync(userId, BlockchainType.NeoN3))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetFraudDetectionHistory("NeoN3", userId, 1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<AI.PatternRecognition.Models.FraudDetectionResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedHistory, response.Data);
        Assert.Equal("Fraud detection history retrieved successfully", response.Message);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(2, response.TotalItems);

        _patternRecognitionServiceMock.Verify(s => s.GetFraudDetectionHistoryAsync(userId, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task GetFraudDetectionHistory_WithInvalidPage_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetFraudDetectionHistory("NeoN3", null, 0, 20);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Page number must be greater than 0", response.Message);
    }

    [Fact]
    public async Task GetFraudDetectionHistory_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetFraudDetectionHistory("NeoN3", null, 1, 101);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Page size must be between 1 and 100", response.Message);
    }

    [Fact]
    public async Task GetBehaviorProfile_WithValidUserId_ReturnsOkResult()
    {
        // Arrange
        var userId = "user-123";
        var expectedProfile = new BehaviorProfile
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastUpdated = DateTime.UtcNow,
            // Baselines = new Dictionary<BehaviorType, BehaviorBaseline>(), // Property doesn't exist
            // LearnedPatterns = new List<BehaviorPattern>(), // Property doesn't exist
            BehaviorMetrics = new Dictionary<string, double> { { "loginFrequency", 0.5 } }
        };

        _patternRecognitionServiceMock
            .Setup(s => s.GetBehaviorProfileAsync(userId, BlockchainType.NeoN3))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetBehaviorProfile(userId, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BehaviorProfile>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedProfile, response.Data);
        Assert.Equal("Behavior profile retrieved successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.GetBehaviorProfileAsync(userId, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task UpdateBehaviorProfile_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var userId = "user-123";
        var profile = new BehaviorProfile
        {
            UserId = userId,
            BehaviorMetrics = new Dictionary<string, double> { { "newFactor", 0.7 } }
        };

        _patternRecognitionServiceMock
            .Setup(s => s.UpdateBehaviorProfileAsync(userId, profile, BlockchainType.NeoN3))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateBehaviorProfile(userId, profile, "NeoN3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        Assert.Equal("Behavior profile updated successfully", response.Message);

        _patternRecognitionServiceMock.Verify(s => s.UpdateBehaviorProfileAsync(userId, profile, BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task UpdateBehaviorProfile_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "non-existent-user";
        var profile = new BehaviorProfile();

        _patternRecognitionServiceMock
            .Setup(s => s.UpdateBehaviorProfileAsync(userId, profile, BlockchainType.NeoN3))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateBehaviorProfile(userId, profile, "NeoN3");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains($"User {userId} not found", response.Message);
    }

    #endregion
}
