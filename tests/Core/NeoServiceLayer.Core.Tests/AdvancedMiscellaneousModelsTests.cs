using FluentAssertions;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.Core.Tests;

/// <summary>
/// Tests for advanced Miscellaneous model classes to verify property behavior and default values.
/// </summary>
public class AdvancedMiscellaneousModelsTests
{
    #region FairnessAnalysisResult Tests

    [Fact]
    public void FairnessAnalysisResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new FairnessAnalysisResult();

        // Assert
        result.AnalysisId.Should().NotBeEmpty();
        Guid.TryParse(result.AnalysisId, out _).Should().BeTrue();
        result.TransactionId.Should().BeEmpty();
        result.TransactionHash.Should().BeEmpty();
        result.FairnessScore.Should().Be(0);
        result.RiskLevel.Should().Be("Low");
        result.EstimatedMEV.Should().Be(0);
        result.RiskFactors.Should().BeEmpty();
        result.DetectedRisks.Should().BeEmpty();
        result.Recommendations.Should().BeEmpty();
        result.ProtectionFee.Should().Be(0);
        result.AnalyzedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Details.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void FairnessAnalysisResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new FairnessAnalysisResult();
        var riskFactors = new[] { "high_value", "frequent_trader" };
        var detectedRisks = new[] { "sandwich_attack", "front_running" };
        var recommendations = new[] { "use_private_pool", "increase_slippage" };
        var details = new Dictionary<string, object> { ["confidence"] = 0.95 };
        var analyzedAt = DateTime.UtcNow.AddMinutes(-2);

        // Act
        result.AnalysisId = "analysis-123";
        result.TransactionId = "tx-456";
        result.TransactionHash = "0xhash789";
        result.FairnessScore = 0.85;
        result.RiskLevel = "Medium";
        result.EstimatedMEV = 0.05m;
        result.RiskFactors = riskFactors;
        result.DetectedRisks = detectedRisks;
        result.Recommendations = recommendations;
        result.ProtectionFee = 0.001m;
        result.AnalyzedAt = analyzedAt;
        result.Details = details;

        // Assert
        result.AnalysisId.Should().Be("analysis-123");
        result.TransactionId.Should().Be("tx-456");
        result.TransactionHash.Should().Be("0xhash789");
        result.FairnessScore.Should().Be(0.85);
        result.RiskLevel.Should().Be("Medium");
        result.EstimatedMEV.Should().Be(0.05m);
        result.RiskFactors.Should().BeEquivalentTo(riskFactors);
        result.DetectedRisks.Should().BeEquivalentTo(detectedRisks);
        result.Recommendations.Should().BeEquivalentTo(recommendations);
        result.ProtectionFee.Should().Be(0.001m);
        result.AnalyzedAt.Should().Be(analyzedAt);
        result.Details.Should().BeEquivalentTo(details);
    }

    #endregion

    #region TransactionAnalysisRequest Tests

    [Fact]
    public void TransactionAnalysisRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new TransactionAnalysisRequest();

        // Assert
        request.TransactionId.Should().BeEmpty();
        request.From.Should().BeEmpty();
        request.To.Should().BeEmpty();
        request.Value.Should().Be(0);
        request.TransactionData.Should().BeEmpty();
        request.Context.Should().NotBeNull().And.BeEmpty();
        request.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TransactionAnalysisRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new TransactionAnalysisRequest();
        var context = new Dictionary<string, object> { ["network"] = "mainnet" };
        var requestedAt = DateTime.UtcNow.AddMinutes(-1);

        // Act
        request.TransactionId = "tx-analysis-123";
        request.From = "0xfrom456";
        request.To = "0xto789";
        request.Value = 10.5m;
        request.TransactionData = "0xdata123";
        request.Context = context;
        request.RequestedAt = requestedAt;

        // Assert
        request.TransactionId.Should().Be("tx-analysis-123");
        request.From.Should().Be("0xfrom456");
        request.To.Should().Be("0xto789");
        request.Value.Should().Be(10.5m);
        request.TransactionData.Should().Be("0xdata123");
        request.Context.Should().BeEquivalentTo(context);
        request.RequestedAt.Should().Be(requestedAt);
    }

    #endregion

    #region RiskAssessmentRequest Tests

    [Fact]
    public void RiskAssessmentRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new RiskAssessmentRequest();

        // Assert
        request.TransactionId.Should().BeEmpty();
        request.FromAddress.Should().BeEmpty();
        request.ToAddress.Should().BeEmpty();
        request.Amount.Should().Be(0);
        request.AssetType.Should().BeEmpty();
        request.TransactionData.Should().NotBeNull().And.BeEmpty();
        request.RiskFactors.Should().BeEmpty();
        request.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.ModelId.Should().Be("default-risk-model");
    }

    [Fact]
    public void RiskAssessmentRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new RiskAssessmentRequest();
        var transactionData = new Dictionary<string, object> { ["gasPrice"] = "20000000000" };
        var riskFactors = new[] { "new_address", "large_amount" };
        var timestamp = DateTime.UtcNow.AddMinutes(-3);

        // Act
        request.TransactionId = "risk-tx-123";
        request.FromAddress = "0xriskfrom";
        request.ToAddress = "0xriskto";
        request.Amount = 1000m;
        request.AssetType = "Token";
        request.TransactionData = transactionData;
        request.RiskFactors = riskFactors;
        request.Timestamp = timestamp;
        request.ModelId = "advanced-risk-model";

        // Assert
        request.TransactionId.Should().Be("risk-tx-123");
        request.FromAddress.Should().Be("0xriskfrom");
        request.ToAddress.Should().Be("0xriskto");
        request.Amount.Should().Be(1000m);
        request.AssetType.Should().Be("Token");
        request.TransactionData.Should().BeEquivalentTo(transactionData);
        request.RiskFactors.Should().BeEquivalentTo(riskFactors);
        request.Timestamp.Should().Be(timestamp);
        request.ModelId.Should().Be("advanced-risk-model");
    }

    #endregion

    #region RiskAssessmentResult Tests

    [Fact]
    public void RiskAssessmentResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new RiskAssessmentResult();

        // Assert
        result.AssessmentId.Should().BeEmpty();
        result.TransactionId.Should().BeEmpty();
        result.RiskScore.Should().Be(0);
        result.RiskLevel.Should().Be(RiskLevel.Minimal);
        result.RiskFactors.Should().BeEmpty();
        result.AssessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void RiskAssessmentResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new RiskAssessmentResult();
        var riskFactors = new[] { "suspicious_pattern", "blacklisted_address" };
        var metadata = new Dictionary<string, object> { ["model_version"] = "v2.1" };
        var assessedAt = DateTime.UtcNow.AddMinutes(-1);

        // Act
        result.AssessmentId = "assessment-456";
        result.TransactionId = "tx-risk-789";
        result.RiskScore = 0.75;
        result.RiskLevel = RiskLevel.High;
        result.RiskFactors = riskFactors;
        result.AssessedAt = assessedAt;
        result.Success = true;
        result.ErrorMessage = "No errors";
        result.Metadata = metadata;

        // Assert
        result.AssessmentId.Should().Be("assessment-456");
        result.TransactionId.Should().Be("tx-risk-789");
        result.RiskScore.Should().Be(0.75);
        result.RiskLevel.Should().Be(RiskLevel.High);
        result.RiskFactors.Should().BeEquivalentTo(riskFactors);
        result.AssessedAt.Should().Be(assessedAt);
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().Be("No errors");
        result.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region NotificationPreferences Tests

    [Fact]
    public void NotificationPreferences_ShouldInitializeWithDefaults()
    {
        // Act
        var preferences = new NotificationPreferences();

        // Assert
        preferences.UserId.Should().BeEmpty();
        preferences.EmailEnabled.Should().BeTrue();
        preferences.SmsEnabled.Should().BeFalse();
        preferences.PushEnabled.Should().BeTrue();
        preferences.WebhookEnabled.Should().BeFalse();
        preferences.NotificationTypes.Should().BeEmpty();
        preferences.Settings.Should().NotBeNull().And.BeEmpty();
        preferences.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        preferences.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void NotificationPreferences_Properties_ShouldBeSettable()
    {
        // Arrange
        var preferences = new NotificationPreferences();
        var notificationTypes = new[] { "transaction", "security_alert" };
        var settings = new Dictionary<string, object> { ["quiet_hours"] = "22:00-08:00" };
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow.AddHours(-1);

        // Act
        preferences.UserId = "user-123";
        preferences.EmailEnabled = false;
        preferences.SmsEnabled = true;
        preferences.PushEnabled = false;
        preferences.WebhookEnabled = true;
        preferences.NotificationTypes = notificationTypes;
        preferences.Settings = settings;
        preferences.CreatedAt = createdAt;
        preferences.UpdatedAt = updatedAt;

        // Assert
        preferences.UserId.Should().Be("user-123");
        preferences.EmailEnabled.Should().BeFalse();
        preferences.SmsEnabled.Should().BeTrue();
        preferences.PushEnabled.Should().BeFalse();
        preferences.WebhookEnabled.Should().BeTrue();
        preferences.NotificationTypes.Should().BeEquivalentTo(notificationTypes);
        preferences.Settings.Should().BeEquivalentTo(settings);
        preferences.CreatedAt.Should().Be(createdAt);
        preferences.UpdatedAt.Should().Be(updatedAt);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void AssetType_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<AssetType>().Should().BeEquivalentTo([
            AssetType.Native,
            AssetType.Token,
            AssetType.Stablecoin,
            AssetType.Wrapped,
            AssetType.Synthetic,
            AssetType.NFT
        ]);
    }

    [Fact]
    public void RiskLevel_ShouldHaveCorrectValues()
    {
        // Assert
        Enum.GetValues<RiskLevel>().Should().BeEquivalentTo([
            RiskLevel.Minimal,
            RiskLevel.Low,
            RiskLevel.Medium,
            RiskLevel.High,
            RiskLevel.Critical
        ]);
    }

    #endregion
}
