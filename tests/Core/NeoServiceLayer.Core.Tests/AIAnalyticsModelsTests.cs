using FluentAssertions;
using NeoServiceLayer.Core;
using Xunit;

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
        request.TextData.Should().BeEmpty();
        request.Language.Should().Be("en");
        request.Keywords.Should().BeEmpty();
        request.IncludeEmotions.Should().BeFalse();
    }

    [Fact]
    public void SentimentAnalysisRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new SentimentAnalysisRequest();
        var textData = new[] { "Hello world", "Test text" };
        var keywords = new[] { "hello", "test" };

        // Act
        request.TextData = textData;
        request.Language = "fr";
        request.Keywords = keywords;
        request.IncludeEmotions = true;

        // Assert
        request.TextData.Should().BeEquivalentTo(textData);
        request.Language.Should().Be("fr");
        request.Keywords.Should().BeEquivalentTo(keywords);
        request.IncludeEmotions.Should().BeTrue();
    }

    #endregion

    #region SentimentResult Tests

    [Fact]
    public void SentimentResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new SentimentResult();

        // Assert
        result.AnalysisId.Should().NotBeEmpty();
        Guid.TryParse(result.AnalysisId, out _).Should().BeTrue();
        result.OverallSentiment.Should().Be(0);
        result.Confidence.Should().Be(0);
        result.AnalysisTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.SampleSize.Should().Be(0);
        result.KeywordSentiments.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SentimentResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new SentimentResult();
        var analysisId = "test-analysis-123";
        var analysisTime = DateTime.UtcNow.AddMinutes(-5);
        var keywordSentiments = new Dictionary<string, double> { ["positive"] = 0.8, ["negative"] = 0.2 };

        // Act
        result.AnalysisId = analysisId;
        result.OverallSentiment = 0.75;
        result.Confidence = 0.9;
        result.AnalysisTime = analysisTime;
        result.SampleSize = 100;
        result.KeywordSentiments = keywordSentiments;

        // Assert
        result.AnalysisId.Should().Be(analysisId);
        result.OverallSentiment.Should().Be(0.75);
        result.Confidence.Should().Be(0.9);
        result.AnalysisTime.Should().Be(analysisTime);
        result.SampleSize.Should().Be(100);
        result.KeywordSentiments.Should().BeEquivalentTo(keywordSentiments);
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
        registration.Description.Should().BeEmpty();
        registration.ModelFormat.Should().Be("onnx");
        registration.ModelData.Should().BeEmpty();
        registration.ModelHash.Should().BeEmpty();
        registration.InputSchema.Should().BeEmpty();
        registration.OutputSchema.Should().BeEmpty();
        registration.Owner.Should().BeEmpty();
    }

    [Fact]
    public void ModelRegistration_Properties_ShouldBeSettable()
    {
        // Arrange
        var registration = new ModelRegistration();
        var modelData = new byte[] { 1, 2, 3, 4, 5 };
        var inputSchema = new[] { "input1", "input2" };
        var outputSchema = new[] { "output1" };

        // Act
        registration.Name = "Test Model";
        registration.Description = "Test Description";
        registration.ModelFormat = "tensorflow";
        registration.ModelData = modelData;
        registration.ModelHash = "abc123";
        registration.InputSchema = inputSchema;
        registration.OutputSchema = outputSchema;
        registration.Owner = "test-owner";

        // Assert
        registration.Name.Should().Be("Test Model");
        registration.Description.Should().Be("Test Description");
        registration.ModelFormat.Should().Be("tensorflow");
        registration.ModelData.Should().BeEquivalentTo(modelData);
        registration.ModelHash.Should().Be("abc123");
        registration.InputSchema.Should().BeEquivalentTo(inputSchema);
        registration.OutputSchema.Should().BeEquivalentTo(outputSchema);
        registration.Owner.Should().Be("test-owner");
    }

    #endregion

    #region FraudDetectionRequest Tests

    [Fact]
    public void FraudDetectionRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new FraudDetectionRequest();

        // Assert
        request.TransactionId.Should().BeEmpty();
        request.FromAddress.Should().BeEmpty();
        request.ToAddress.Should().BeEmpty();
        request.Amount.Should().Be(0);
        request.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.Features.Should().NotBeNull().And.BeEmpty();
        request.ModelId.Should().BeEmpty();
        request.Threshold.Should().Be(0.8);
    }

    [Fact]
    public void FraudDetectionRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new FraudDetectionRequest();
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var features = new Dictionary<string, object> { ["velocity"] = 5, ["amount_ratio"] = 0.3 };

        // Act
        request.TransactionId = "tx-123";
        request.FromAddress = "from-addr";
        request.ToAddress = "to-addr";
        request.Amount = 100.5m;
        request.Timestamp = timestamp;
        request.Features = features;
        request.ModelId = "fraud-model-1";
        request.Threshold = 0.95;

        // Assert
        request.TransactionId.Should().Be("tx-123");
        request.FromAddress.Should().Be("from-addr");
        request.ToAddress.Should().Be("to-addr");
        request.Amount.Should().Be(100.5m);
        request.Timestamp.Should().Be(timestamp);
        request.Features.Should().BeEquivalentTo(features);
        request.ModelId.Should().Be("fraud-model-1");
        request.Threshold.Should().Be(0.95);
    }

    #endregion

    #region FraudDetectionResult Tests

    [Fact]
    public void FraudDetectionResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new FraudDetectionResult();

        // Assert
        result.TransactionId.Should().BeEmpty();
        result.IsFraud.Should().BeFalse();
        result.FraudScore.Should().Be(0);
        result.RiskFactors.Should().BeEmpty();
        result.Confidence.Should().Be(0);
        result.DetectionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ModelId.Should().BeEmpty();
        result.Proof.Should().BeEmpty();
    }

    [Fact]
    public void FraudDetectionResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new FraudDetectionResult();
        var detectionTime = DateTime.UtcNow.AddMinutes(-2);
        var riskFactors = new[] { "high_velocity", "unusual_amount" };

        // Act
        result.TransactionId = "tx-456";
        result.IsFraud = true;
        result.FraudScore = 0.92;
        result.RiskFactors = riskFactors;
        result.Confidence = 0.88;
        result.DetectionTime = detectionTime;
        result.ModelId = "fraud-model-2";
        result.Proof = "proof-data";

        // Assert
        result.TransactionId.Should().Be("tx-456");
        result.IsFraud.Should().BeTrue();
        result.FraudScore.Should().Be(0.92);
        result.RiskFactors.Should().BeEquivalentTo(riskFactors);
        result.Confidence.Should().Be(0.88);
        result.DetectionTime.Should().Be(detectionTime);
        result.ModelId.Should().Be("fraud-model-2");
        result.Proof.Should().Be("proof-data");
    }

    #endregion

    #region AnomalyDetectionRequest Tests

    [Fact]
    public void AnomalyDetectionRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new AnomalyDetectionRequest();

        // Assert
        request.Data.Should().BeEmpty();
        request.FeatureNames.Should().BeEmpty();
        request.Threshold.Should().Be(0.95);
        request.ReturnScores.Should().BeTrue();
        request.ModelId.Should().BeEmpty();
    }

    [Fact]
    public void AnomalyDetectionRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new AnomalyDetectionRequest();
        var data = new double[][] { [1.0, 2.0], [3.0, 4.0] };
        var featureNames = new[] { "feature1", "feature2" };

        // Act
        request.Data = data;
        request.FeatureNames = featureNames;
        request.Threshold = 0.85;
        request.ReturnScores = false;
        request.ModelId = "anomaly-model-1";

        // Assert
        request.Data.Should().BeEquivalentTo(data);
        request.FeatureNames.Should().BeEquivalentTo(featureNames);
        request.Threshold.Should().Be(0.85);
        request.ReturnScores.Should().BeFalse();
        request.ModelId.Should().Be("anomaly-model-1");
    }

    #endregion

    #region AnomalyDetectionResult Tests

    [Fact]
    public void AnomalyDetectionResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new AnomalyDetectionResult();

        // Assert
        result.AnalysisId.Should().NotBeEmpty();
        Guid.TryParse(result.AnalysisId, out _).Should().BeTrue();
        result.IsAnomaly.Should().BeEmpty();
        result.AnomalyScores.Should().BeEmpty();
        result.AnomalyCount.Should().Be(0);
        result.DetectionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ModelId.Should().BeEmpty();
        result.Proof.Should().BeEmpty();
    }

    [Fact]
    public void AnomalyDetectionResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new AnomalyDetectionResult();
        var analysisId = "analysis-789";
        var isAnomaly = new[] { true, false, true };
        var anomalyScores = new[] { 0.95, 0.3, 0.88 };
        var detectionTime = DateTime.UtcNow.AddMinutes(-3);

        // Act
        result.AnalysisId = analysisId;
        result.IsAnomaly = isAnomaly;
        result.AnomalyScores = anomalyScores;
        result.AnomalyCount = 2;
        result.DetectionTime = detectionTime;
        result.ModelId = "anomaly-model-2";
        result.Proof = "anomaly-proof";

        // Assert
        result.AnalysisId.Should().Be(analysisId);
        result.IsAnomaly.Should().BeEquivalentTo(isAnomaly);
        result.AnomalyScores.Should().BeEquivalentTo(anomalyScores);
        result.AnomalyCount.Should().Be(2);
        result.DetectionTime.Should().Be(detectionTime);
        result.ModelId.Should().Be("anomaly-model-2");
        result.Proof.Should().Be("anomaly-proof");
    }

    #endregion

    #region ClassificationRequest Tests

    [Fact]
    public void ClassificationRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new ClassificationRequest();

        // Assert
        request.InputData.Should().BeEmpty();
        request.FeatureNames.Should().BeEmpty();
        request.ModelId.Should().BeEmpty();
        request.ReturnProbabilities.Should().BeTrue();
        request.ExpectedClasses.Should().BeEmpty();
    }

    [Fact]
    public void ClassificationRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new ClassificationRequest();
        var inputData = new object[] { 1.5, "text", true };
        var featureNames = new[] { "numeric", "text", "boolean" };
        var expectedClasses = new[] { "class1", "class2" };

        // Act
        request.InputData = inputData;
        request.FeatureNames = featureNames;
        request.ModelId = "classification-model-1";
        request.ReturnProbabilities = false;
        request.ExpectedClasses = expectedClasses;

        // Assert
        request.InputData.Should().BeEquivalentTo(inputData);
        request.FeatureNames.Should().BeEquivalentTo(featureNames);
        request.ModelId.Should().Be("classification-model-1");
        request.ReturnProbabilities.Should().BeFalse();
        request.ExpectedClasses.Should().BeEquivalentTo(expectedClasses);
    }

    #endregion

    #region ClassificationResult Tests

    [Fact]
    public void ClassificationResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new ClassificationResult();

        // Assert
        result.ClassificationId.Should().NotBeEmpty();
        Guid.TryParse(result.ClassificationId, out _).Should().BeTrue();
        result.PredictedClasses.Should().BeEmpty();
        result.Probabilities.Should().BeEmpty();
        result.Confidence.Should().Be(0);
        result.ClassificationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ModelId.Should().BeEmpty();
        result.Proof.Should().BeEmpty();
    }

    [Fact]
    public void ClassificationResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new ClassificationResult();
        var classificationId = "classification-101";
        var predictedClasses = new[] { "class1", "class2" };
        var probabilities = new[] { 0.8, 0.2 };
        var classificationTime = DateTime.UtcNow.AddMinutes(-1);

        // Act
        result.ClassificationId = classificationId;
        result.PredictedClasses = predictedClasses;
        result.Probabilities = probabilities;
        result.Confidence = 0.85;
        result.ClassificationTime = classificationTime;
        result.ModelId = "classification-model-2";
        result.Proof = "classification-proof";

        // Assert
        result.ClassificationId.Should().Be(classificationId);
        result.PredictedClasses.Should().BeEquivalentTo(predictedClasses);
        result.Probabilities.Should().BeEquivalentTo(probabilities);
        result.Confidence.Should().Be(0.85);
        result.ClassificationTime.Should().Be(classificationTime);
        result.ModelId.Should().Be("classification-model-2");
        result.Proof.Should().Be("classification-proof");
    }

    #endregion
}
