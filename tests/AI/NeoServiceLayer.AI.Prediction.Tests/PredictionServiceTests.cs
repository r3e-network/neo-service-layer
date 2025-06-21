using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using IConfigurationSection = Microsoft.Extensions.Configuration.IConfigurationSection;

namespace NeoServiceLayer.AI.Prediction.Tests;

/// <summary>
/// Comprehensive unit tests for PredictionService with high coverage.
/// Tests ML model training, inference, validation, and enclave operations.
/// </summary>
public class PredictionServiceTests : IDisposable
{
    private readonly Mock<ILogger<PredictionService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IServiceRegistry> _mockServiceRegistry;
    private readonly PredictionService _service;

    public PredictionServiceTests()
    {
        _mockLogger = new Mock<ILogger<PredictionService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockServiceRegistry = new Mock<IServiceRegistry>();

        // Setup configuration
        SetupConfiguration();

        _service = new PredictionService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockEnclaveManager.Object,
            _mockServiceRegistry.Object);
    }

    #region Service Lifecycle Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StartAsync_ValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _service.StartAsync(cancellationToken);

        // Assert
        _service.IsRunning.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Prediction Service started successfully");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StopAsync_RunningService_StopsSuccessfully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await _service.StartAsync(cancellationToken);

        // Act
        await _service.StopAsync(cancellationToken);

        // Assert
        _service.IsRunning.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Information, "Prediction Service stopped successfully");
    }

    #endregion

    #region Model Training Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelTraining")]
    public async Task TrainModelAsync_ValidTrainingData_ReturnsModelId()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        const string modelType = "linear_regression";
        const string algorithm = "gradient_descent";
        var parameters = new Dictionary<string, object>
        {
            ["learning_rate"] = 0.01,
            ["max_iterations"] = 1000
        };

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(trainingData));

        _mockEnclaveManager
            .Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TrainModelAsync(modelType, algorithm, parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("model_");
        VerifyLoggerCalled(LogLevel.Information, "Model training completed successfully");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelTraining")]
    public async Task TrainModelAsync_InvalidModelType_ThrowsArgumentException()
    {
        // Arrange
        const string invalidModelType = "invalid_model";
        const string algorithm = "gradient_descent";
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.TrainModelAsync(invalidModelType, algorithm, parameters));

        exception.ParamName.Should().Be("modelType");
        exception.Message.Should().Contain("Unsupported model type");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelTraining")]
    [InlineData("linear_regression")]
    [InlineData("logistic_regression")]
    [InlineData("neural_network")]
    [InlineData("random_forest")]
    public async Task TrainModelAsync_SupportedModelTypes_ProcessesSuccessfully(string modelType)
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        const string algorithm = "gradient_descent";
        var parameters = new Dictionary<string, object>();

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(trainingData));

        _mockEnclaveManager
            .Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TrainModelAsync(modelType, algorithm, parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(LogLevel.Information, "Model training completed successfully");
    }

    #endregion

    #region Model Inference Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelInference")]
    public async Task PredictAsync_ValidModelAndFeatures_ReturnsPrediction()
    {
        // Arrange
        const string modelId = "model_test_12345";
        var inputFeatures = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var mockModel = CreateMockPredictionModel();

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(modelId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(mockModel));

        // Act
        var result = await _service.PredictAsync(modelId, inputFeatures);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().BeGreaterThan(0);
        result.Confidence.Should().BeInRange(0, 1);
        result.ModelId.Should().Be(modelId);
        result.Features.Should().BeEquivalentTo(inputFeatures);
        VerifyLoggerCalled(LogLevel.Debug, "Prediction completed successfully");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelInference")]
    public async Task PredictAsync_NonExistentModel_ThrowsInvalidOperationException()
    {
        // Arrange
        const string nonExistentModelId = "model_nonexistent";
        var inputFeatures = new double[] { 1.0, 2.0, 3.0 };

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(nonExistentModelId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PredictAsync(nonExistentModelId, inputFeatures));

        exception.Message.Should().Contain("Model not found");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelInference")]
    public async Task PredictAsync_InvalidFeatureCount_ThrowsArgumentException()
    {
        // Arrange
        const string modelId = "model_test_12345";
        var invalidFeatures = new double[] { 1.0, 2.0 }; // Too few features
        var mockModel = CreateMockPredictionModel(expectedFeatureCount: 5);

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(modelId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(mockModel));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PredictAsync(modelId, invalidFeatures));

        exception.Message.Should().Contain("Expected 5 features, got 2");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelInference")]
    public async Task PredictAsync_NullOrEmptyFeatures_ThrowsArgumentException()
    {
        // Arrange
        const string modelId = "model_test_12345";

        // Act & Assert - Null features
        var nullException = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PredictAsync(modelId, null!));
        nullException.Message.Should().Contain("Input features cannot be null or empty");

        // Act & Assert - Empty features
        var emptyException = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PredictAsync(modelId, Array.Empty<double>()));
        emptyException.Message.Should().Contain("Input features cannot be null or empty");
    }

    #endregion

    #region Model Validation Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelValidation")]
    public async Task ValidateModelAsync_HighAccuracyModel_PassesValidation()
    {
        // Arrange
        const string modelId = "model_high_accuracy";
        var mockModel = CreateMockPredictionModel();
        var validationData = CreateValidTrainingData();

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(modelId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(mockModel));

        // Act
        var result = await _service.ValidateModelAsync(modelId, validationData);

        // Assert
        result.Should().NotBeNull();
        result.Accuracy.Should().BeGreaterThan(0.7); // Above 70% threshold
        result.IsValid.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Model validation completed");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ModelValidation")]
    public async Task ValidateModelAsync_LowAccuracyModel_FailsValidation()
    {
        // Arrange
        const string modelId = "model_low_accuracy";
        var mockModel = CreateMockPredictionModel(accuracy: 0.5); // Below threshold
        var validationData = CreateValidTrainingData();

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(modelId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(mockModel));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ValidateModelAsync(modelId, validationData));

        exception.Message.Should().Contain("Model accuracy");
        exception.Message.Should().Contain("below acceptable threshold");
    }

    #endregion

    #region Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "ModelInference")]
    public async Task PredictAsync_MultipleRequests_HandlesLoadEfficiently()
    {
        // Arrange
        const string modelId = "model_performance_test";
        var inputFeatures = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var mockModel = CreateMockPredictionModel();

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(modelId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(mockModel));

        const int requestCount = 100;
        var tasks = new List<Task<PredictionResult>>();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(_service.PredictAsync(modelId, inputFeatures));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(requestCount);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    #endregion

    #region Helper Methods

    private void SetupConfiguration()
    {
        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("test_value");

        _mockConfiguration
            .Setup(x => x.GetSection(It.IsAny<string>()))
            .Returns(configSection.Object);
    }

    private static object CreateValidTrainingData()
    {
        return new
        {
            Features = new[]
            {
                new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 },
                new double[] { 2.0, 3.0, 4.0, 5.0, 6.0 },
                new double[] { 3.0, 4.0, 5.0, 6.0, 7.0 },
                new double[] { 4.0, 5.0, 6.0, 7.0, 8.0 },
                new double[] { 5.0, 6.0, 7.0, 8.0, 9.0 }
            },
            Labels = new double[] { 10.0, 15.0, 20.0, 25.0, 30.0 }
        };
    }

    private static object CreateMockPredictionModel(int expectedFeatureCount = 5, double accuracy = 0.85)
    {
        return new
        {
            ModelType = "linear_regression",
            FeatureCount = expectedFeatureCount,
            Weights = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 },
            Bias = 1.0,
            Accuracy = accuracy,
            FeatureStats = new
            {
                Mean = new double[] { 3.0, 4.0, 5.0, 6.0, 7.0 },
                StandardDeviation = new double[] { 1.0, 1.0, 1.0, 1.0, 1.0 }
            },
            Layers = new object[0], // For neural networks
            Trees = new object[0]   // For random forests
        };
    }

    private void VerifyLoggerCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    #region Test Data Models

    public class PredictionResult
    {
        public double Value { get; set; }
        public double Confidence { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double[] Features { get; set; } = Array.Empty<double>();
    }

    public class ValidationResult
    {
        public double Accuracy { get; set; }
        public bool IsValid { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public DateTime ValidatedAt { get; set; }
    }

    #endregion

    #endregion
}
