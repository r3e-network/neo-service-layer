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
/// Tests for advanced Miscellaneous model classes to verify property behavior and default values.
/// </summary>
public class AdvancedMiscellaneousModelsTests
{
    #region FairnessAnalysisResult Tests

    [Fact]
    public void FairnessAnalysisResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new NeoServiceLayer.Core.Models.FairnessAnalysisResult();

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
        var result = new NeoServiceLayer.Core.Models.FairnessAnalysisResult();
        var riskFactors = new List<string> { "high_value", "frequent_trader" };
        var detectedRisks = new List<string> { "sandwich_attack", "front_running" };
        var recommendations = new List<string> { "use_private_pool", "increase_slippage" };
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
        var request = new NeoServiceLayer.Core.Models.TransactionAnalysisRequest();

        // Assert
        request.Transaction.Should().NotBeNull();
        request.Depth.Should().Be(AnalysisDepth.Standard);
        request.IncludeMevAnalysis.Should().BeTrue();
        request.Parameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void TransactionAnalysisRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new NeoServiceLayer.Core.Models.TransactionAnalysisRequest();
        var parameters = new Dictionary<string, object> { ["network"] = "mainnet" };
        var transaction = new PendingTransaction();

        // Act
        request.Transaction = transaction;
        request.Depth = AnalysisDepth.Deep;
        request.IncludeMevAnalysis = false;
        request.Parameters = parameters;

        // Assert
        request.Transaction.Should().Be(transaction);
        request.Depth.Should().Be(AnalysisDepth.Deep);
        request.IncludeMevAnalysis.Should().BeFalse();
        request.Parameters.Should().BeEquivalentTo(parameters);
    }

    #endregion

    #region NotificationPreferences Tests

    [Fact]
    public void NotificationPreferences_ShouldInitializeWithDefaults()
    {
        // Act
        var preferences = new NotificationPreferences();

        // Assert
        preferences.PreferredChannels.Should().BeEquivalentTo(new[] { "Email" });
        preferences.EnableNotifications.Should().BeTrue();
        preferences.QuietHoursStart.Should().BeNull();
        preferences.QuietHoursEnd.Should().BeNull();
    }

    [Fact]
    public void NotificationPreferences_Properties_ShouldBeSettable()
    {
        // Arrange
        var preferences = new NotificationPreferences();
        var preferredChannels = new[] { "Email", "SMS", "Push" };
        var quietStart = TimeSpan.FromHours(22);
        var quietEnd = TimeSpan.FromHours(8);

        // Act
        preferences.PreferredChannels = preferredChannels;
        preferences.EnableNotifications = false;
        preferences.QuietHoursStart = quietStart;
        preferences.QuietHoursEnd = quietEnd;

        // Assert
        preferences.PreferredChannels.Should().BeEquivalentTo(preferredChannels);
        preferences.EnableNotifications.Should().BeFalse();
        preferences.QuietHoursStart.Should().Be(quietStart);
        preferences.QuietHoursEnd.Should().Be(quietEnd);
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
