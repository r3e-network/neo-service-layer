using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.AI.PatternRecognition;
using NeoServiceLayer.AI.PatternRecognition.Analyzers;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.AI.PatternRecognition.Tests
{
    public class PatternRecognitionServiceComprehensiveTests : IDisposable
    {
        private readonly Mock<ILogger<PatternRecognitionService>> _mockLogger;
        private readonly Mock<IEnclaveService> _mockEnclaveService;
        private readonly Mock<IPatternAnalyzer> _mockPatternAnalyzer;
        private readonly PatternRecognitionService _service;

        public PatternRecognitionServiceComprehensiveTests()
        {
            _mockLogger = new Mock<ILogger<PatternRecognitionService>>();
            _mockEnclaveService = new Mock<IEnclaveService>();
            _mockPatternAnalyzer = new Mock<IPatternAnalyzer>();
            
            _mockEnclaveService.Setup(x => x.IsEnclaveEnabled).Returns(true);
            _mockEnclaveService.Setup(x => x.SecureComputeAsync(It.IsAny<byte[]>()))
                .ReturnsAsync((byte[] input) => input.Reverse().ToArray());

            _service = new PatternRecognitionService(_mockLogger.Object, _mockEnclaveService.Object);
        }

        [Fact]
        public async Task AnalyzePatternAsync_WithValidData_ReturnsPatternResult()
        {
            // Arrange
            var data = new PatternData
            {
                DataPoints = new List<DataPoint>
                {
                    new DataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-5), Value = 10.0 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-4), Value = 12.0 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-3), Value = 15.0 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-2), Value = 18.0 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddMinutes(-1), Value = 22.0 },
                    new DataPoint { Timestamp = DateTime.UtcNow, Value = 27.0 }
                },
                PatternType = PatternType.Trend,
                AnalysisOptions = new AnalysisOptions
                {
                    MinConfidence = 0.7,
                    MaxPatterns = 5,
                    IncludeStatistics = true
                }
            };

            // Act
            var result = await _service.AnalyzePatternAsync(data);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.DetectedPatterns.Should().NotBeEmpty();
            result.DetectedPatterns.First().PatternType.Should().Be(PatternType.Trend);
            result.Statistics.Should().NotBeNull();
        }

        [Fact]
        public async Task AnalyzePatternAsync_WithNullData_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _service.AnalyzePatternAsync(null);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("data");
        }

        [Fact]
        public async Task DetectAnomaliesAsync_WithAnomalousData_DetectsAnomalies()
        {
            // Arrange
            var data = new AnomalyDetectionRequest
            {
                TimeSeries = new List<TimeSeriesPoint>
                {
                    new TimeSeriesPoint { Timestamp = DateTime.UtcNow.AddHours(-5), Value = 100 },
                    new TimeSeriesPoint { Timestamp = DateTime.UtcNow.AddHours(-4), Value = 102 },
                    new TimeSeriesPoint { Timestamp = DateTime.UtcNow.AddHours(-3), Value = 98 },
                    new TimeSeriesPoint { Timestamp = DateTime.UtcNow.AddHours(-2), Value = 500 }, // Anomaly
                    new TimeSeriesPoint { Timestamp = DateTime.UtcNow.AddHours(-1), Value = 101 },
                    new TimeSeriesPoint { Timestamp = DateTime.UtcNow, Value = 99 }
                },
                Sensitivity = 0.95,
                Method = AnomalyDetectionMethod.StatisticalOutlier
            };

            // Act
            var result = await _service.DetectAnomaliesAsync(data);

            // Assert
            result.Should().NotBeNull();
            result.AnomaliesDetected.Should().BeTrue();
            result.Anomalies.Should().NotBeEmpty();
            result.Anomalies.Should().Contain(a => a.Value == 500);
            result.Anomalies.First(a => a.Value == 500).AnomalyScore.Should().BeGreaterThan(0.9);
        }

        [Fact]
        public async Task ClassifyPatternAsync_WithKnownPattern_ClassifiesCorrectly()
        {
            // Arrange
            var request = new PatternClassificationRequest
            {
                Pattern = new Pattern
                {
                    PatternId = Guid.NewGuid().ToString(),
                    PatternType = PatternType.Seasonal,
                    Features = new Dictionary<string, double>
                    {
                        ["Periodicity"] = 24.0,
                        ["Amplitude"] = 10.0,
                        ["Phase"] = 0.5
                    }
                },
                ClassificationModel = "SeasonalClassifier",
                IncludeConfidenceBreakdown = true
            };

            // Act
            var result = await _service.ClassifyPatternAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Classification.Should().NotBeNullOrEmpty();
            result.Confidence.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(1);
            result.ConfidenceBreakdown.Should().NotBeNull();
            result.AlternativeClassifications.Should().NotBeEmpty();
        }

        [Fact]
        public async Task PredictNextValuesAsync_WithHistoricalData_PredictsFutureValues()
        {
            // Arrange
            var request = new PredictionRequest
            {
                HistoricalData = new List<DataPoint>
                {
                    new DataPoint { Timestamp = DateTime.UtcNow.AddDays(-6), Value = 100 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddDays(-5), Value = 110 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddDays(-4), Value = 120 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddDays(-3), Value = 130 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddDays(-2), Value = 140 },
                    new DataPoint { Timestamp = DateTime.UtcNow.AddDays(-1), Value = 150 },
                    new DataPoint { Timestamp = DateTime.UtcNow, Value = 160 }
                },
                PredictionHorizon = 3,
                Model = PredictionModel.LinearRegression,
                IncludeConfidenceIntervals = true
            };

            // Act
            var result = await _service.PredictNextValuesAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Predictions.Should().HaveCount(3);
            result.Predictions.Should().OnlyContain(p => p.Value > 160);
            result.Predictions.Should().OnlyContain(p => p.ConfidenceInterval != null);
            result.ModelAccuracy.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task AnalyzeBehaviorAsync_WithUserBehaviorData_IdentifiesPatterns()
        {
            // Arrange
            var request = new BehaviorAnalysisRequest
            {
                UserId = "user123",
                BehaviorEvents = new List<BehaviorEvent>
                {
                    new BehaviorEvent { EventType = "login", Timestamp = DateTime.UtcNow.AddHours(-24), Metadata = new Dictionary<string, object> { ["location"] = "office" } },
                    new BehaviorEvent { EventType = "transaction", Timestamp = DateTime.UtcNow.AddHours(-23), Metadata = new Dictionary<string, object> { ["amount"] = 100.0 } },
                    new BehaviorEvent { EventType = "logout", Timestamp = DateTime.UtcNow.AddHours(-20), Metadata = new Dictionary<string, object> { ["duration"] = 240 } },
                    new BehaviorEvent { EventType = "login", Timestamp = DateTime.UtcNow.AddHours(-8), Metadata = new Dictionary<string, object> { ["location"] = "home" } },
                    new BehaviorEvent { EventType = "transaction", Timestamp = DateTime.UtcNow.AddHours(-7), Metadata = new Dictionary<string, object> { ["amount"] = 5000.0 } },
                    new BehaviorEvent { EventType = "logout", Timestamp = DateTime.UtcNow.AddHours(-6), Metadata = new Dictionary<string, object> { ["duration"] = 60 } }
                },
                AnalysisType = BehaviorAnalysisType.AnomalyDetection,
                BaselineWindow = TimeSpan.FromDays(30)
            };

            // Act
            var result = await _service.AnalyzeBehaviorAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("user123");
            result.BehaviorPatterns.Should().NotBeEmpty();
            result.AnomalousEvents.Should().NotBeEmpty();
            result.RiskScore.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(1);
        }

        [Fact]
        public async Task CorrelatePatternAsync_WithMultipleDataStreams_FindsCorrelations()
        {
            // Arrange
            var request = new CorrelationAnalysisRequest
            {
                DataStreams = new Dictionary<string, List<DataPoint>>
                {
                    ["Temperature"] = new List<DataPoint>
                    {
                        new DataPoint { Timestamp = DateTime.UtcNow.AddHours(-3), Value = 20 },
                        new DataPoint { Timestamp = DateTime.UtcNow.AddHours(-2), Value = 22 },
                        new DataPoint { Timestamp = DateTime.UtcNow.AddHours(-1), Value = 25 },
                        new DataPoint { Timestamp = DateTime.UtcNow, Value = 28 }
                    },
                    ["EnergyConsumption"] = new List<DataPoint>
                    {
                        new DataPoint { Timestamp = DateTime.UtcNow.AddHours(-3), Value = 100 },
                        new DataPoint { Timestamp = DateTime.UtcNow.AddHours(-2), Value = 110 },
                        new DataPoint { Timestamp = DateTime.UtcNow.AddHours(-1), Value = 125 },
                        new DataPoint { Timestamp = DateTime.UtcNow, Value = 145 }
                    }
                },
                CorrelationMethod = CorrelationMethod.Pearson,
                MinCorrelation = 0.7
            };

            // Act
            var result = await _service.CorrelatePatternAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Correlations.Should().NotBeEmpty();
            result.Correlations.Should().Contain(c => 
                c.Stream1 == "Temperature" && 
                c.Stream2 == "EnergyConsumption" && 
                c.CorrelationCoefficient > 0.9);
        }

        [Fact]
        public async Task TrainPatternModelAsync_WithTrainingData_TrainsSuccessfully()
        {
            // Arrange
            var request = new ModelTrainingRequest
            {
                ModelName = "CustomPatternModel",
                TrainingData = new List<LabeledPattern>
                {
                    new LabeledPattern
                    {
                        Pattern = new Pattern { PatternType = PatternType.Cyclic, Features = new Dictionary<string, double> { ["frequency"] = 0.1 } },
                        Label = "DailyPattern"
                    },
                    new LabeledPattern
                    {
                        Pattern = new Pattern { PatternType = PatternType.Cyclic, Features = new Dictionary<string, double> { ["frequency"] = 0.0417 } },
                        Label = "WeeklyPattern"
                    }
                },
                ModelParameters = new Dictionary<string, object>
                {
                    ["algorithm"] = "RandomForest",
                    ["maxDepth"] = 10,
                    ["numTrees"] = 100
                },
                ValidationSplit = 0.2
            };

            // Act
            var result = await _service.TrainPatternModelAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.ModelId.Should().NotBeNullOrEmpty();
            result.TrainingMetrics.Should().NotBeNull();
            result.TrainingMetrics.Accuracy.Should().BeGreaterThan(0);
            result.TrainingMetrics.Precision.Should().BeGreaterThan(0);
            result.TrainingMetrics.Recall.Should().BeGreaterThan(0);
            result.IsDeployed.Should().BeFalse();
        }

        [Fact]
        public async Task GetPatternStatisticsAsync_WithPatternId_ReturnsStatistics()
        {
            // Arrange
            var patternId = Guid.NewGuid().ToString();
            await _service.RegisterPatternAsync(new Pattern
            {
                PatternId = patternId,
                PatternType = PatternType.Trend,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            });

            // Act
            var statistics = await _service.GetPatternStatisticsAsync(patternId);

            // Assert
            statistics.Should().NotBeNull();
            statistics.PatternId.Should().Be(patternId);
            statistics.OccurrenceCount.Should().BeGreaterOrEqualTo(0);
            statistics.FirstDetected.Should().BeBefore(DateTime.UtcNow);
            statistics.LastDetected.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task ComparePatternAsync_WithTwoSimilarPatterns_ReturnsHighSimilarity()
        {
            // Arrange
            var pattern1 = new Pattern
            {
                PatternType = PatternType.Seasonal,
                Features = new Dictionary<string, double>
                {
                    ["amplitude"] = 10.0,
                    ["frequency"] = 0.1,
                    ["phase"] = 0.0
                }
            };

            var pattern2 = new Pattern
            {
                PatternType = PatternType.Seasonal,
                Features = new Dictionary<string, double>
                {
                    ["amplitude"] = 10.5,
                    ["frequency"] = 0.1,
                    ["phase"] = 0.05
                }
            };

            // Act
            var similarity = await _service.ComparePatternAsync(pattern1, pattern2);

            // Assert
            similarity.Should().NotBeNull();
            similarity.SimilarityScore.Should().BeGreaterThan(0.9);
            similarity.MatchingFeatures.Should().NotBeEmpty();
            similarity.DifferingFeatures.Should().NotBeEmpty();
        }

        [Fact]
        public async Task OptimizePatternDetectionAsync_WithHistoricalPerformance_OptimizesParameters()
        {
            // Arrange
            var request = new OptimizationRequest
            {
                CurrentParameters = new Dictionary<string, object>
                {
                    ["sensitivity"] = 0.8,
                    ["windowSize"] = 100,
                    ["minPatternLength"] = 5
                },
                PerformanceHistory = new List<PerformanceMetric>
                {
                    new PerformanceMetric { Timestamp = DateTime.UtcNow.AddDays(-7), Accuracy = 0.75, FalsePositiveRate = 0.15 },
                    new PerformanceMetric { Timestamp = DateTime.UtcNow.AddDays(-3), Accuracy = 0.78, FalsePositiveRate = 0.12 },
                    new PerformanceMetric { Timestamp = DateTime.UtcNow.AddDays(-1), Accuracy = 0.80, FalsePositiveRate = 0.10 }
                },
                OptimizationGoal = OptimizationGoal.MaximizeAccuracy,
                Constraints = new Dictionary<string, object>
                {
                    ["maxFalsePositiveRate"] = 0.15,
                    ["minSensitivity"] = 0.7
                }
            };

            // Act
            var result = await _service.OptimizePatternDetectionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.OptimizedParameters.Should().NotBeEmpty();
            result.ExpectedImprovement.Should().BeGreaterThan(0);
            result.OptimizedParameters["sensitivity"].Should().BeOfType<double>();
            ((double)result.OptimizedParameters["sensitivity"]).Should().BeGreaterThanOrEqualTo(0.7);
        }

        [Fact]
        public async Task ExportPatternAsync_WithValidPattern_ExportsSuccessfully()
        {
            // Arrange
            var pattern = new Pattern
            {
                PatternId = Guid.NewGuid().ToString(),
                PatternType = PatternType.Complex,
                Features = new Dictionary<string, double>
                {
                    ["complexity"] = 0.95,
                    ["dimensions"] = 5
                },
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var exportedData = await _service.ExportPatternAsync(pattern, ExportFormat.Json);

            // Assert
            exportedData.Should().NotBeNull();
            exportedData.Format.Should().Be(ExportFormat.Json);
            exportedData.Data.Should().NotBeNullOrEmpty();
            exportedData.Metadata.Should().ContainKey("PatternId");
            exportedData.Metadata.Should().ContainKey("ExportedAt");
        }

        [Fact]
        public async Task ImportPatternAsync_WithValidData_ImportsSuccessfully()
        {
            // Arrange
            var importData = new PatternImportData
            {
                Format = ExportFormat.Json,
                Data = "{\"PatternId\":\"test-pattern\",\"PatternType\":\"Trend\",\"Features\":{\"slope\":0.5}}",
                Metadata = new Dictionary<string, object>
                {
                    ["Source"] = "ExternalSystem",
                    ["ImportedAt"] = DateTime.UtcNow
                }
            };

            // Act
            var importedPattern = await _service.ImportPatternAsync(importData);

            // Assert
            importedPattern.Should().NotBeNull();
            importedPattern.PatternId.Should().Be("test-pattern");
            importedPattern.PatternType.Should().Be(PatternType.Trend);
            importedPattern.Features.Should().ContainKey("slope");
        }

        public void Dispose()
        {
            _service?.Dispose();
        }
    }
}