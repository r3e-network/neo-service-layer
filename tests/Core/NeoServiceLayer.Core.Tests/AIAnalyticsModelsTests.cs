using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Core.Tests;

/// <summary>
/// Tests for AI Analytics model classes to verify property behavior and default values.
/// </summary>
public class AIAnalyticsModelsTests
{
    #region SentimentAnalysisRequest Tests

    [Fact]
    public void SentimentAnalysisRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new SentimentAnalysisRequest();

        // Assert
        request.Text.Should().BeEmpty();
        request.Language.Should().Be("en");
        request.IncludeDetailedAnalysis.Should().BeFalse();
        request.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void SentimentAnalysisRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new SentimentAnalysisRequest();
        var text = "Hello world test text";
        var parameters = new Dictionary<string, object> { ["keywords"] = new[] { "hello", "test" } };

        // Act
        request.Text = text;
        request.Language = "fr";
        request.Parameters = parameters;
        request.IncludeDetailedAnalysis = true;

        // Assert
        request.Text.Should().Be(text);
        request.Language.Should().Be("fr");
        request.Parameters.Should().BeEquivalentTo(parameters);
        request.IncludeDetailedAnalysis.Should().BeTrue();
    }

    #endregion

    #region SentimentResult Tests

    [Fact]
    public void SentimentResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new SentimentResult();

        // Assert
        result.AnalysisId.Should().BeEmpty();
        result.SentimentScore.Should().Be(0);
        result.Confidence.Should().Be(0);
        result.AnalyzedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.DetailedSentiment.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SentimentResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new SentimentResult();
        var analysisId = "test-analysis-123";
        var analyzedAt = DateTime.UtcNow.AddMinutes(-5);
        var detailedSentiment = new Dictionary<string, double> { ["positive"] = 0.8, ["negative"] = 0.2 };

        // Act
        result.AnalysisId = analysisId;
        result.SentimentScore = 0.75;
        result.Confidence = 0.9;
        result.AnalyzedAt = analyzedAt;
        result.DetailedSentiment = detailedSentiment;

        // Assert
        result.AnalysisId.Should().Be(analysisId);
        result.SentimentScore.Should().Be(0.75);
        result.Confidence.Should().Be(0.9);
        result.AnalyzedAt.Should().Be(analyzedAt);
        result.DetailedSentiment.Should().BeEquivalentTo(detailedSentiment);
    }

    #endregion

    #region ModelRegistration Tests

    [Fact]
    public void ModelRegistration_ShouldInitializeWithDefaults()
    {
        // Act
        var registration = new ModelRegistration();

        // Assert
        registration.Name.Should().BeEmpty();
        registration.Type.Should().BeEmpty();
        registration.Version.Should().Be("1.0.0");
        registration.ModelData.Should().BeEmpty();
        registration.Configuration.Should().NotBeNull().And.BeEmpty();
        registration.Description.Should().BeEmpty();
    }

    [Fact]
    public void ModelRegistration_Properties_ShouldBeSettable()
    {
        // Arrange
        var registration = new ModelRegistration();
        var modelData = new byte[] { 1, 2, 3, 4, 5 };
        var configuration = new Dictionary<string, object> { ["format"] = "tensorflow", ["version"] = "2.0" };

        // Act
        registration.Name = "Test Model";
        registration.Type = "neural_network";
        registration.Version = "2.0.1";
        registration.ModelData = modelData;
        registration.Configuration = configuration;
        registration.Description = "Test Description";

        // Assert
        registration.Name.Should().Be("Test Model");
        registration.Type.Should().Be("neural_network");
        registration.Version.Should().Be("2.0.1");
        registration.ModelData.Should().BeEquivalentTo(modelData);
        registration.Configuration.Should().BeEquivalentTo(configuration);
        registration.Description.Should().Be("Test Description");
    }

    #endregion

    #region FraudDetectionRequest Tests

    [Fact]
    public void FraudDetectionRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new NeoServiceLayer.Core.Models.FraudDetectionRequest();

        // Assert
        request.TransactionId.Should().BeEmpty();
        request.TransactionData.Should().NotBeNull().And.BeEmpty();
        request.Parameters.Should().NotBeNull().And.BeEmpty();
        request.Threshold.Should().Be(0.8);
        request.IncludeHistoricalAnalysis.Should().BeTrue();
    }

    [Fact]
    public void FraudDetectionRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new NeoServiceLayer.Core.Models.FraudDetectionRequest();
        var transactionData = new Dictionary<string, object> { ["amount"] = 100.5m, ["from"] = "from-addr" };
        var parameters = new Dictionary<string, object> { ["velocity"] = 5, ["amount_ratio"] = 0.3 };

        // Act
        request.TransactionId = "tx-123";
        request.TransactionData = transactionData;
        request.Parameters = parameters;
        request.Threshold = 0.95;
        request.IncludeHistoricalAnalysis = false;

        // Assert
        request.TransactionId.Should().Be("tx-123");
        request.TransactionData.Should().BeEquivalentTo(transactionData);
        request.Parameters.Should().BeEquivalentTo(parameters);
        request.Threshold.Should().Be(0.95);
        request.IncludeHistoricalAnalysis.Should().BeFalse();
    }

    #endregion

    #region FraudDetectionResult Tests

    [Fact]
    public void FraudDetectionResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new NeoServiceLayer.Core.Models.FraudDetectionResult();

        // Assert
        result.DetectionId.Should().BeEmpty();
        result.IsFraudulent.Should().BeFalse();
        result.RiskScore.Should().Be(0);
        result.RiskFactors.Should().NotBeNull().And.BeEmpty();
        result.Confidence.Should().Be(0);
        result.DetectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.DetectedPatterns.Should().NotBeNull().And.BeEmpty();
        result.Details.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void FraudDetectionResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new NeoServiceLayer.Core.Models.FraudDetectionResult();
        var detectionTime = DateTime.UtcNow.AddMinutes(-2);
        var riskFactors = new Dictionary<string, double> { ["high_velocity"] = 0.8, ["unusual_amount"] = 0.6 };

        // Act
        result.DetectionId = "detection-456";
        result.IsFraudulent = true;
        result.RiskScore = 0.92;
        result.RiskFactors = riskFactors;
        result.Confidence = 0.88;
        result.DetectedAt = detectionTime;

        // Assert
        result.DetectionId.Should().Be("detection-456");
        result.IsFraudulent.Should().BeTrue();
        result.RiskScore.Should().Be(0.92);
        result.RiskFactors.Should().BeEquivalentTo(riskFactors);
        result.Confidence.Should().Be(0.88);
        result.DetectedAt.Should().Be(detectionTime);
    }

    #endregion

    #region AnomalyDetectionRequest Tests

    [Fact]
    public void AnomalyDetectionRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new NeoServiceLayer.Core.Models.AnomalyDetectionRequest();

        // Assert
        request.Data.Should().NotBeNull().And.BeEmpty();
        request.Parameters.Should().NotBeNull().And.BeEmpty();
        request.Threshold.Should().Be(0.95);
        request.WindowSize.Should().Be(100);
    }

    [Fact]
    public void AnomalyDetectionRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new NeoServiceLayer.Core.Models.AnomalyDetectionRequest();
        var data = new Dictionary<string, object> { ["values"] = new double[] { 1.0, 2.0, 3.0 } };
        var parameters = new Dictionary<string, object> { ["algorithm"] = "isolation_forest" };

        // Act
        request.Data = data;
        request.Parameters = parameters;
        request.Threshold = 0.85;
        request.WindowSize = 50;

        // Assert
        request.Data.Should().BeEquivalentTo(data);
        request.Parameters.Should().BeEquivalentTo(parameters);
        request.Threshold.Should().Be(0.85);
        request.WindowSize.Should().Be(50);
}

#endregion

#region AnomalyDetectionResult Tests

[Fact]
public void AnomalyDetectionResult_ShouldInitializeWithDefaults()
{
    // Act
    var result = new NeoServiceLayer.Core.Models.AnomalyDetectionResult();

    // Assert
    result.DetectionId.Should().BeEmpty();
    result.AnomalyScore.Should().Be(0);
    result.IsAnomalous.Should().BeFalse();
    result.DetectedAnomalies.Should().NotBeNull().And.BeEmpty();
    result.Confidence.Should().Be(0);
    result.DetectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    result.Details.Should().NotBeNull().And.BeEmpty();
}

[Fact]
public void AnomalyDetectionResult_Properties_ShouldBeSettable()
{
    // Arrange
    var result = new NeoServiceLayer.Core.Models.AnomalyDetectionResult();
    var detectionId = "detection-789";
    var detectedAnomalies = new List<Anomaly>();
    var detectionTime = DateTime.UtcNow.AddMinutes(-3);

    // Act
    result.DetectionId = detectionId;
    result.AnomalyScore = 0.95;
    result.IsAnomalous = true;
    result.DetectedAnomalies = detectedAnomalies;
    result.Confidence = 0.88;
    result.DetectedAt = detectionTime;

    // Assert
    result.DetectionId.Should().Be(detectionId);
    result.AnomalyScore.Should().Be(0.95);
    result.IsAnomalous.Should().BeTrue();
    result.DetectedAnomalies.Should().BeEquivalentTo(detectedAnomalies);
    result.Confidence.Should().Be(0.88);
    result.DetectedAt.Should().Be(detectionTime);
}

#endregion

#region ClassificationRequest Tests

[Fact]
public void ClassificationRequest_ShouldInitializeWithDefaults()
{
    // Act
    var request = new NeoServiceLayer.Core.Models.ClassificationRequest();

    // Assert
    request.Data.Should().NotBeNull().And.BeEmpty();
    request.ModelId.Should().BeNull();
    request.Parameters.Should().NotBeNull().And.BeEmpty();
    request.IncludeConfidenceScores.Should().BeTrue();
}

[Fact]
public void ClassificationRequest_Properties_ShouldBeSettable()
{
    // Arrange
    var request = new NeoServiceLayer.Core.Models.ClassificationRequest();
    var data = new Dictionary<string, object> { ["features"] = new object[] { 1.5, "text", true } };
    var parameters = new Dictionary<string, object> { ["algorithm"] = "random_forest" };

    // Act
    request.Data = data;
    request.ModelId = "classification-model-1";
    request.Parameters = parameters;
    request.IncludeConfidenceScores = false;

    // Assert
    request.Data.Should().BeEquivalentTo(data);
    request.ModelId.Should().Be("classification-model-1");
    request.Parameters.Should().BeEquivalentTo(parameters);
    request.IncludeConfidenceScores.Should().BeFalse();
}

#endregion

#region ClassificationResult Tests

[Fact]
public void ClassificationResult_ShouldInitializeWithDefaults()
{
    // Act
    var result = new NeoServiceLayer.Core.Models.ClassificationResult();

    // Assert
    result.ClassificationId.Should().BeEmpty();
    result.PredictedClass.Should().BeEmpty();
    result.ClassProbabilities.Should().NotBeNull().And.BeEmpty();
    result.Confidence.Should().Be(0);
    result.ClassifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    result.Details.Should().NotBeNull().And.BeEmpty();
}

[Fact]
public void ClassificationResult_Properties_ShouldBeSettable()
{
    // Arrange
    var result = new NeoServiceLayer.Core.Models.ClassificationResult();
    var classificationId = "classification-101";
    var classProbabilities = new Dictionary<string, double> { ["class1"] = 0.8, ["class2"] = 0.2 };
    var classificationTime = DateTime.UtcNow.AddMinutes(-1);

    // Act
    result.ClassificationId = classificationId;
    result.PredictedClass = "class1";
    result.ClassProbabilities = classProbabilities;
    result.Confidence = 0.85;
    result.ClassifiedAt = classificationTime;

    // Assert
    result.ClassificationId.Should().Be(classificationId);
    result.PredictedClass.Should().Be("class1");
    result.ClassProbabilities.Should().BeEquivalentTo(classProbabilities);
    result.Confidence.Should().Be(0.85);
    result.ClassifiedAt.Should().Be(classificationTime);
}

#endregion
}
