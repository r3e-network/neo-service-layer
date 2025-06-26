using FluentAssertions;
using NeoServiceLayer.Core.Models;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Models;

/// <summary>
/// Tests for AIModelMetrics class to verify property behavior and metrics calculations.
/// </summary>
public class AIModelMetricsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var metrics = new AIModelMetrics();

        // Assert
        metrics.ModelId.Should().BeEmpty();
        metrics.Accuracy.Should().Be(0);
        metrics.Precision.Should().Be(0);
        metrics.Recall.Should().Be(0);
        metrics.F1Score.Should().Be(0);
        metrics.Loss.Should().Be(0);
        metrics.TrainingTimeMs.Should().Be(0);
        metrics.InferenceTimeMs.Should().Be(0);
        metrics.AdditionalMetrics.Should().NotBeNull().And.BeEmpty();
        metrics.EvaluatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        metrics.TestDataSize.Should().Be(0);
        metrics.ConfusionMatrix.Should().NotBeNull();
        metrics.ConfusionMatrix.GetLength(0).Should().Be(0);
        metrics.ConfusionMatrix.GetLength(1).Should().Be(0);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var metrics = new AIModelMetrics();
        var additionalMetrics = new Dictionary<string, double> { ["auc"] = 0.95, ["mse"] = 0.02 };
        var evaluatedTime = DateTime.UtcNow.AddHours(-2);
        var confusionMatrix = new double[2, 2] { { 85, 5 }, { 10, 100 } };

        // Act
        metrics.ModelId = "test-model-789";
        metrics.Accuracy = 0.92;
        metrics.Precision = 0.88;
        metrics.Recall = 0.91;
        metrics.F1Score = 0.895;
        metrics.Loss = 0.15;
        metrics.TrainingTimeMs = 120000;
        metrics.InferenceTimeMs = 50;
        metrics.AdditionalMetrics = additionalMetrics;
        metrics.EvaluatedAt = evaluatedTime;
        metrics.TestDataSize = 1000;
        metrics.ConfusionMatrix = confusionMatrix;

        // Assert
        metrics.ModelId.Should().Be("test-model-789");
        metrics.Accuracy.Should().Be(0.92);
        metrics.Precision.Should().Be(0.88);
        metrics.Recall.Should().Be(0.91);
        metrics.F1Score.Should().Be(0.895);
        metrics.Loss.Should().Be(0.15);
        metrics.TrainingTimeMs.Should().Be(120000);
        metrics.InferenceTimeMs.Should().Be(50);
        metrics.AdditionalMetrics.Should().BeEquivalentTo(additionalMetrics);
        metrics.EvaluatedAt.Should().Be(evaluatedTime);
        metrics.TestDataSize.Should().Be(1000);
        metrics.ConfusionMatrix.Should().BeEquivalentTo(confusionMatrix);
    }

    [Fact]
    public void AdditionalMetrics_ShouldBeModifiable()
    {
        // Arrange
        var metrics = new AIModelMetrics();

        // Act
        metrics.AdditionalMetrics["roc_auc"] = 0.89;
        metrics.AdditionalMetrics["pr_auc"] = 0.92;
        metrics.AdditionalMetrics["specificity"] = 0.85;

        // Assert
        metrics.AdditionalMetrics.Should().HaveCount(3);
        metrics.AdditionalMetrics.Should().ContainKey("roc_auc").WhoseValue.Should().Be(0.89);
        metrics.AdditionalMetrics.Should().ContainKey("pr_auc").WhoseValue.Should().Be(0.92);
        metrics.AdditionalMetrics.Should().ContainKey("specificity").WhoseValue.Should().Be(0.85);
    }

    [Fact]
    public void ConfusionMatrix_ShouldSupportDifferentDimensions()
    {
        // Arrange
        var metrics = new AIModelMetrics();
        var matrix3x3 = new double[3, 3]
        {
            { 50, 2, 1 },
            { 3, 45, 2 },
            { 1, 1, 48 }
        };

        // Act
        metrics.ConfusionMatrix = matrix3x3;

        // Assert
        metrics.ConfusionMatrix.GetLength(0).Should().Be(3);
        metrics.ConfusionMatrix.GetLength(1).Should().Be(3);
        metrics.ConfusionMatrix[0, 0].Should().Be(50);
        metrics.ConfusionMatrix[1, 1].Should().Be(45);
        metrics.ConfusionMatrix[2, 2].Should().Be(48);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(0.999)]
    public void MetricValues_ShouldAcceptValidRanges(double value)
    {
        // Arrange
        var metrics = new AIModelMetrics
        {
            // Act & Assert
            Accuracy = value,
            Precision = value,
            Recall = value,
            F1Score = value
        };

        metrics.Accuracy.Should().Be(value);
        metrics.Precision.Should().Be(value);
        metrics.Recall.Should().Be(value);
        metrics.F1Score.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(10000)]
    [InlineData(long.MaxValue)]
    public void TimeMetrics_ShouldAcceptValidValues(long timeValue)
    {
        // Arrange
        var metrics = new AIModelMetrics
        {
            // Act
            TrainingTimeMs = timeValue,
            InferenceTimeMs = timeValue
        };

        // Assert
        metrics.TrainingTimeMs.Should().Be(timeValue);
        metrics.InferenceTimeMs.Should().Be(timeValue);
    }
}
