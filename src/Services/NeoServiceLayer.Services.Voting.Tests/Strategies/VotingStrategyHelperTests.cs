using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Voting.Strategies;
using Xunit;

namespace NeoServiceLayer.Services.Voting.Tests.Strategies;

/// <summary>
/// Unit tests for the VotingStrategyHelper class.
/// </summary>
public class VotingStrategyHelperTests
{
    private readonly Mock<ILogger<VotingStrategyHelper>> _mockLogger;
    private readonly VotingStrategyHelper _strategyHelper;

    public VotingStrategyHelperTests()
    {
        _mockLogger = new Mock<ILogger<VotingStrategyHelper>>();
        _strategyHelper = new VotingStrategyHelper(_mockLogger.Object);
    }

    [Fact]
    public void GetEligibleCandidates_OnlyActiveStrategy_ReturnsActiveCandidates()
    {
        // Arrange
        var candidates = new List<CandidateInfo>
        {
            new CandidateInfo { PublicKey = "key1", Name = "Candidate 1", IsActive = true, VotesReceived = 1000, Rank = 1 },
            new CandidateInfo { PublicKey = "key2", Name = "Candidate 2", IsActive = false, VotesReceived = 900, Rank = 2 },
            new CandidateInfo { PublicKey = "key3", Name = "Candidate 3", IsActive = true, VotesReceived = 800, Rank = 3 },
            new CandidateInfo { PublicKey = "key4", Name = "Candidate 4", IsActive = true, VotesReceived = 700, Rank = 4 }
        };

        var strategy = new VotingStrategy
        {
            StrategyType = VotingStrategyType.OnlyActive,
            Rules = new VotingRules
            {
                MaxCandidates = 21
            }
        };

        // Act
        var result = _strategyHelper.GetEligibleCandidates(strategy, candidates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3); // Only active candidates
        result.All(c => c.IsActive).Should().BeTrue();
    }

    [Fact]
    public void GetEligibleCandidates_Top21Strategy_ReturnsTop21()
    {
        // Arrange
        var candidates = new List<CandidateInfo>();
        for (int i = 1; i <= 30; i++)
        {
            candidates.Add(new CandidateInfo
            {
                PublicKey = $"key{i}",
                Name = $"Candidate {i}",
                IsActive = true,
                VotesReceived = 1000 - i,
                Rank = i
            });
        }

        var strategy = new VotingStrategy
        {
            StrategyType = VotingStrategyType.Top21,
            Rules = new VotingRules
            {
                MaxCandidates = 21
            }
        };

        // Act
        var result = _strategyHelper.GetEligibleCandidates(strategy, candidates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(21);
        result.Should().BeInDescendingOrder(c => c.VotesReceived);
        result.First().VotesReceived.Should().Be(999); // Highest vote count
        result.Last().VotesReceived.Should().Be(979); // 21st highest vote count
    }

    [Fact]
    public void ApplyVotingRules_OnlyActiveNodes_ReturnsActiveCandidates()
    {
        // Arrange
        var candidates = new List<CandidateInfo>
        {
            new CandidateInfo { PublicKey = "key1", Name = "Candidate 1", IsActive = true, VotesReceived = 1000, UptimePercentage = 99.0 },
            new CandidateInfo { PublicKey = "key2", Name = "Candidate 2", IsActive = false, VotesReceived = 900, UptimePercentage = 98.0 },
            new CandidateInfo { PublicKey = "key3", Name = "Candidate 3", IsActive = true, VotesReceived = 800, UptimePercentage = 97.0 }
        };

        var rules = new VotingRules
        {
            OnlyActiveNodes = true,
            OnlyConsensusNodes = false, // Set to false so it doesn't filter out candidates
            MaxCandidates = 21,
            MinUptimePercentage = 95.0
        };

        // Act
        var result = _strategyHelper.ApplyVotingRules(candidates, rules);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.IsActive).Should().BeTrue();
        result.All(c => c.UptimePercentage >= 95.0).Should().BeTrue();
    }

    [Fact]
    public void GetConditionalCandidates_ValidCandidates_ReturnsConditionalCandidates()
    {
        // Arrange
        var candidates = new List<CandidateInfo>
        {
            new CandidateInfo { Address = "addr1", PublicKey = "key1", Name = "Candidate 1", IsActive = true, VotesReceived = 1000 },
            new CandidateInfo { Address = "addr2", PublicKey = "key2", Name = "Candidate 2", IsActive = false, VotesReceived = 900 },
            new CandidateInfo { Address = "addr3", PublicKey = "key3", Name = "Candidate 3", IsActive = true, VotesReceived = 800 }
        };

        var strategy = new VotingStrategy
        {
            StrategyType = VotingStrategyType.ConditionalVoting,
            PreferredCandidates = new[] { "addr1", "addr3" },
            FallbackCandidates = new[] { "addr2" },
            Rules = new VotingRules
            {
                MaxCandidates = 21
            }
        };

        // Act
        var result = _strategyHelper.GetConditionalCandidates(candidates, strategy);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only active preferred candidates
        result.Select(c => c.Address).Should().Contain(new[] { "addr1", "addr3" });
    }

    [Fact]
    public void GetBalancedCandidates_ValidCandidates_ReturnsBalancedSelection()
    {
        // Arrange
        var candidates = new List<CandidateInfo>();
        for (int i = 1; i <= 20; i++)
        {
            candidates.Add(new CandidateInfo
            {
                PublicKey = $"key{i}",
                Name = $"Candidate {i}",
                IsActive = true,
                VotesReceived = 1000 - i,
                Metrics = new CandidateMetrics
                {
                    PerformanceScore = 100.0 - i
                }
            });
        }

        // Act
        var result = _strategyHelper.GetBalancedCandidates(candidates, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
        result.All(c => c.IsActive).Should().BeTrue();
    }

    [Fact]
    public void CalculateRiskAssessment_ValidCandidates_ReturnsRiskAssessment()
    {
        // Arrange
        var candidates = new CandidateInfo[]
        {
            new CandidateInfo { PublicKey = "key1", Name = "Candidate 1", IsActive = true, VotesReceived = 1000, UptimePercentage = 99.0, ExpectedReward = 100 },
            new CandidateInfo { PublicKey = "key2", Name = "Candidate 2", IsActive = true, VotesReceived = 500, UptimePercentage = 97.0, ExpectedReward = 80 },
            new CandidateInfo { PublicKey = "key3", Name = "Candidate 3", IsActive = true, VotesReceived = 100, UptimePercentage = 95.0, ExpectedReward = 60 }
        };

        // Act
        var result = _strategyHelper.CalculateRiskAssessment(candidates);

        // Assert
        result.Should().NotBeNull();
        result.OverallRisk.Should().BeInRange(0, 1);
        result.DetailedRisks.Should().ContainKey("ConcentrationRisk");
        result.DetailedRisks.Should().ContainKey("PerformanceRisk");
        result.DetailedRisks.Should().ContainKey("RewardRisk");
        result.DetailedRisks.Should().ContainKey("CandidateCount");
        result.DetailedRisks.Should().ContainKey("AverageUptime");
    }

    [Fact]
    public void GenerateRecommendationReasoning_OnlyActiveStrategy_ReturnsCorrectReasoning()
    {
        // Act
        var result = _strategyHelper.GenerateRecommendationReasoning(VotingStrategyType.OnlyActive, 15);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("15");
        result.Should().Contain("active");
    }

    [Fact]
    public void CalculateConfidenceScore_ValidCandidates_ReturnsScore()
    {
        // Arrange
        var candidates = new CandidateInfo[]
        {
            new CandidateInfo { PublicKey = "key1", IsActive = true, UptimePercentage = 99.0, IsConsensusNode = true },
            new CandidateInfo { PublicKey = "key2", IsActive = true, UptimePercentage = 97.0, IsConsensusNode = true }
        };

        var preferences = new VotingPreferences
        {
            Rules = new VotingRules
            {
                OnlyConsensusNodes = true
            }
        };

        // Act
        var result = _strategyHelper.CalculateConfidenceScore(candidates, preferences);

        // Assert
        result.Should().BeInRange(0, 1);
    }
}
