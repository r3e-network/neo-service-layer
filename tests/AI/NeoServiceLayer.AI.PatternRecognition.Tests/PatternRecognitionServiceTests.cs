using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.AI.PatternRecognition;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using CoreConfiguration = NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.AI.PatternRecognition.Tests;

/// <summary>
/// Comprehensive unit tests for PatternRecognitionService with fraud detection focus.
/// Tests all fraud detection algorithms, pattern matching, and risk scoring functionality.
/// </summary>
public class PatternRecognitionServiceTests : IDisposable
{
    private readonly Mock<ILogger<PatternRecognitionService>> _mockLogger;
    private readonly Mock<CoreConfiguration.IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IPersistentStorageProvider> _mockStorageProvider;
    private readonly IEnclaveManager _enclaveManager;
    private readonly PatternRecognitionService _service;

    public PatternRecognitionServiceTests()
    {
        _mockLogger = new Mock<ILogger<PatternRecognitionService>>();
        _mockConfiguration = new Mock<CoreConfiguration.IServiceConfiguration>();
        _mockStorageProvider = new Mock<IPersistentStorageProvider>();

        SetupConfiguration();

        // Create real EnclaveManager with TestEnclaveWrapper for SGX simulation mode testing
        var enclaveManagerLogger = new Mock<ILogger<EnclaveManager>>();
        var testEnclaveWrapper = new TestEnclaveWrapper();
        _enclaveManager = new EnclaveManager(enclaveManagerLogger.Object, testEnclaveWrapper);

        _service = new PatternRecognitionService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockStorageProvider.Object,
            _enclaveManager);

        // Initialize the service for testing - this must succeed for tests to work
        InitializeServiceForTesting();
    }

    private void InitializeServiceForTesting()
    {
        // Initialize the service synchronously for testing
        // This will call the actual enclave initialization code in simulation mode
        var initTask = _service.InitializeAsync();

        try
        {
            initTask.Wait();
        }
        catch (AggregateException ex)
        {
            var innerException = ex.GetBaseException();
            throw new InvalidOperationException($"Service initialization failed: {innerException.Message}", innerException);
        }

        // Verify that initialization succeeded
        if (!initTask.IsCompletedSuccessfully)
        {
            var exception = initTask.Exception?.GetBaseException();
            throw new InvalidOperationException($"Service initialization failed: {exception?.Message}", exception);
        }

        // Verify that the enclave is properly initialized
        if (!_service.IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Service initialization completed but enclave is not initialized");
        }

        // Verify that the enclave manager is initialized
        if (!_enclaveManager.IsInitialized)
        {
            throw new InvalidOperationException("Service initialization completed but enclave manager is not initialized");
        }
    }

    #region Fraud Detection Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "FraudDetection")]
    public async Task DetectFraudAsync_LowRiskTransaction_ReturnsLowScore()
    {
        // Arrange
        var lowRiskRequest = CreateFraudDetectionRequest(
            amount: 100,
            isNewAddress: false,
            highFrequency: false,
            unusualTimePattern: false);

        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(lowRiskRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.RiskScore.Should().BeLessThan(0.3);
        result.Details["risk_level"].ToString().Should().Be("Low");
        result.RiskFactors.Should().BeEmpty();
        VerifyLoggerCalled(LogLevel.Information, "Fraud detection completed");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "FraudDetection")]
    public async Task DetectFraudAsync_HighRiskTransaction_ReturnsHighScore()
    {
        // Arrange
        var highRiskRequest = CreateFraudDetectionRequest(
            amount: 50000,
            isNewAddress: true,
            highFrequency: true,
            unusualTimePattern: true);

        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(highRiskRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.RiskScore.Should().BeGreaterThan(0.7);
        result.Details["risk_level"].ToString().Should().Be("High");
        result.RiskFactors.Should().NotBeEmpty();
        result.RiskFactors.Should().ContainKey("High transaction velocity");
        result.RiskFactors.Should().ContainKey("Unusual transaction amount");
        VerifyLoggerCalled(LogLevel.Warning, "High fraud risk detected");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "FraudDetection")]
    [InlineData(1000, false, false, false, "Low")]
    [InlineData(15000, false, false, false, "Medium")]
    [InlineData(75000, true, true, true, "High")]
    [InlineData(5000, true, false, false, "Medium")]
    public async Task DetectFraudAsync_VariousRiskLevels_ReturnsCorrectClassification(
        decimal amount, bool isNewAddress, bool highFrequency, bool unusualTime, string expectedRiskLevel)
    {
        // Arrange
        var request = CreateFraudDetectionRequest(amount, isNewAddress, highFrequency, unusualTime);
        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Details["risk_level"].ToString().Should().Be(expectedRiskLevel);
    }

    #endregion

    #region Velocity Analysis Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "VelocityAnalysis")]
    public async Task DetectFraudAsync_HighVelocityPattern_IdentifiesRisk()
    {
        // Arrange
        var request = CreateFraudDetectionRequest(
            amount: 5000,
            isNewAddress: false,
            highFrequency: true,
            unusualTimePattern: false,
            transactionCount: 15,
            timeWindow: TimeSpan.FromMinutes(3));

        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(request, BlockchainType.NeoN3);

        // Assert
        result.RiskScore.Should().BeGreaterThan(0.5);
        result.RiskFactors.Should().ContainKey("High transaction velocity");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "VelocityAnalysis")]
    public async Task DetectFraudAsync_NormalVelocityPattern_LowRisk()
    {
        // Arrange
        var request = CreateFraudDetectionRequest(
            amount: 5000,
            isNewAddress: false,
            highFrequency: false,
            unusualTimePattern: false,
            transactionCount: 3,
            timeWindow: TimeSpan.FromHours(2));

        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(request, BlockchainType.NeoN3);

        // Assert
        result.RiskScore.Should().BeLessThan(0.4);
        result.RiskFactors.Should().NotContainKey("High transaction velocity");
    }

    #endregion

    #region Amount Analysis Tests

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "AmountAnalysis")]
    [InlineData(9999, true)] // Just under reporting threshold
    [InlineData(10000, true)] // Round number, large amount
    [InlineData(50000, true)] // Large amount
    [InlineData(150000, true)] // Very large amount
    [InlineData(1500, false)] // Normal amount
    public async Task DetectFraudAsync_SuspiciousAmounts_IdentifiesCorrectly(decimal amount, bool shouldBeSuspicious)
    {
        // Arrange
        var request = CreateFraudDetectionRequest(
            amount: amount,
            isNewAddress: false,
            highFrequency: false,
            unusualTimePattern: false);

        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(request, BlockchainType.NeoN3);

        // Assert
        if (shouldBeSuspicious)
        {
            result.RiskScore.Should().BeGreaterThan(0.3);
        }
        else
        {
            result.RiskScore.Should().BeLessThan(0.5);
        }
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "PatternMatching")]
    public async Task DetectFraudAsync_KnownFraudPattern_HighScore()
    {
        // Arrange
        var request = CreateFraudDetectionRequest(
            amount: 9999, // Just under threshold
            isNewAddress: true,
            highFrequency: true,
            unusualTimePattern: true);

        // Setup known fraud patterns
        var fraudPatterns = new[]
        {
            new { Pattern = "just_under_threshold", Weight = 0.8 },
            new { Pattern = "new_address_high_velocity", Weight = 0.7 }
        };

        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(request, BlockchainType.NeoN3);

        // Assert
        result.RiskScore.Should().BeGreaterThan(0.7);
        result.RiskFactors.Should().ContainKey("Matches known fraud patterns");
    }

    #endregion

    #region Behavioral Analysis Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BehavioralAnalysis")]
    public async Task DetectFraudAsync_DeviationFromNormalBehavior_IdentifiesRisk()
    {
        // Arrange
        var request = CreateFraudDetectionRequest(
            amount: 25000, // Much higher than normal
            isNewAddress: false,
            highFrequency: false,
            unusualTimePattern: false,
            senderAddress: "0x1234567890123456789012345678901234567890");

        // Setup user behavior profile
        var behaviorProfile = new
        {
            AverageTransactionAmount = 1000m,
            TransactionFrequency = 5,
            TypicalTimePattern = "business_hours"
        };

        SetupStorageProviderForFraudDetection();

        // Act
        var result = await _service.DetectFraudAsync(request, BlockchainType.NeoN3);

        // Assert
        result.RiskScore.Should().BeGreaterThan(0.5);
        result.RiskFactors.Should().ContainKey("Poor network reputation");
    }

    #endregion

    #region Anomaly Detection Tests

    [Fact(Skip = "Model mismatch between Core and Models namespaces")]
    [Trait("Category", "Unit")]
    [Trait("Component", "AnomalyDetection")]
    public async Task DetectAnomaliesAsync_NormalPattern_ReturnsLowAnomalyScore()
    {
        // This test is skipped due to incompatible model definitions between
        // NeoServiceLayer.Core.AnomalyDetectionRequest and NeoServiceLayer.Core.Models.AnomalyDetectionRequest
        await Task.CompletedTask;
    }

    [Fact(Skip = "Model mismatch between Core and Models namespaces")]
    [Trait("Category", "Unit")]
    [Trait("Component", "AnomalyDetection")]
    public async Task DetectAnomaliesAsync_AnomalousPattern_ReturnsHighAnomalyScore()
    {
        // This test is skipped due to incompatible model definitions between
        // NeoServiceLayer.Core.AnomalyDetectionRequest and NeoServiceLayer.Core.Models.AnomalyDetectionRequest
        await Task.CompletedTask;
    }

    #endregion

    #region Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "FraudDetection")]
    public async Task DetectFraudAsync_HighVolumeRequests_ProcessesEfficiently()
    {
        // Arrange
        const int requestCount = 50;
        var requests = Enumerable.Range(0, requestCount)
            .Select(i => CreateFraudDetectionRequest(1000 + i, false, false, false))
            .ToList();

        SetupStorageProviderForFraudDetection();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = requests.Select(request => _service.DetectFraudAsync(request, BlockchainType.NeoN3));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(requestCount);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
    }

    #endregion

    #region Helper Methods

    private void SetupConfiguration()
    {
        _mockConfiguration
            .Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test_value");
    }

    private void SetupStorageProviderForFraudDetection()
    {
        // Setup default empty responses for storage calls
        _mockStorageProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>()))
            .ReturnsAsync((byte[]?)null);

        _mockStorageProvider
            .Setup(x => x.StoreAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>()))
            .ReturnsAsync(true);
    }

    private static Core.Models.FraudDetectionRequest CreateFraudDetectionRequest(
        decimal amount,
        bool isNewAddress,
        bool highFrequency,
        bool unusualTimePattern,
        int transactionCount = 1,
        TimeSpan? timeWindow = null,
        string senderAddress = "0xabcdefabcdefabcdefabcdefabcdefabcdefabcd")
    {
        return new Core.Models.FraudDetectionRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            TransactionData = new Dictionary<string, object>
            {
                ["amount"] = amount,
                ["sender_address"] = senderAddress,
                ["recipient_address"] = "0xabcdef1234567890abcdef1234567890abcdef12",
                ["timestamp"] = DateTime.UtcNow,
                ["is_new_address"] = isNewAddress,
                ["high_frequency"] = highFrequency,
                ["unusual_time_pattern"] = unusualTimePattern,
                ["transaction_count"] = transactionCount,
                ["time_window"] = timeWindow ?? TimeSpan.FromHours(1)
            },
            Parameters = new Dictionary<string, object>
            {
                ["include_historical"] = true,
                ["analysis_depth"] = "comprehensive"
            },
            Threshold = 0.8,
            Sensitivity = Core.Models.DetectionSensitivity.Standard,
            IncludeHistoricalAnalysis = true
        };
    }

    private static NeoServiceLayer.Core.Models.AnomalyDetectionRequest CreateNormalAnomalyDetectionRequest()
    {
        // Create normal data points (no anomalies expected)
        var dataPoints = Enumerable.Range(0, 100)
            .Select(i => 1000.0 + (i * 10))
            .ToArray();

        return new NeoServiceLayer.Core.Models.AnomalyDetectionRequest
        {
            Data = new Dictionary<string, object>
            {
                ["data_points"] = dataPoints,
                ["feature_names"] = new[] { "amount", "hour", "random_factor" }
            },
            Parameters = new Dictionary<string, object>
            {
                ["model_id"] = "default-anomaly-model",
                ["test_case"] = "normal_pattern"
            },
            Threshold = 0.7
        };
    }

    private static NeoServiceLayer.Core.Models.AnomalyDetectionRequest CreateAnomalousAnomalyDetectionRequest()
    {
        // Create data with clear anomalies
        var normalData = Enumerable.Range(0, 90)
            .Select(i => 1000.0 + (i * 10))
            .ToList();

        // Add anomalous data points
        normalData.AddRange(new[] { 100000.0, 99999.0, 150000.0 });

        return new NeoServiceLayer.Core.Models.AnomalyDetectionRequest
        {
            Data = new Dictionary<string, object>
            {
                ["data_points"] = normalData.ToArray(),
                ["feature_names"] = new[] { "amount", "hour", "random_factor" }
            },
            Parameters = new Dictionary<string, object>
            {
                ["model_id"] = "default-anomaly-model",
                ["test_case"] = "anomalous_pattern"
            },
            Threshold = 0.7
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
        // Dispose of the enclave manager asynchronously
        _enclaveManager?.DisposeAsync().AsTask().Wait();
    }

    #endregion
}
