using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Voting.Models;
using VotingRulesDomain = NeoServiceLayer.Services.Voting.Domain.ValueObjects.VotingRules;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting.Strategies;

/// <summary>
/// Helper class for voting strategy operations.
/// </summary>
public class VotingStrategyHelper
{
    private readonly ILogger<VotingStrategyHelper> Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VotingStrategyHelper"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public VotingStrategyHelper(ILogger<VotingStrategyHelper> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets eligible candidates based on voting strategy.
    /// </summary>
    /// <param name="strategy">The voting strategy.</param>
    /// <param name="allCandidates">All available candidates.</param>
    /// <returns>Eligible candidates.</returns>
    public IEnumerable<Candidate> GetEligibleCandidates(VotingStrategy strategy, IEnumerable<Candidate> allCandidates)
    {
        return strategy.Type switch
        {
            VotingStrategyType.SimpleMajority => allCandidates.Where(c => c.IsActive),
            VotingStrategyType.Weighted => allCandidates.OrderByDescending(c => c.VotesReceived).Take(21),
            VotingStrategyType.SuperMajority => allCandidates.OrderByDescending(c => c.ExpectedReward),
            VotingStrategyType.Unanimous => allCandidates.Where(c => c.IsActive && c.UptimePercentage >= 98.0),
            VotingStrategyType.RankedChoice => GetConditionalCandidates(allCandidates, strategy),
            VotingStrategyType.Delegated => GetCustomCandidates(allCandidates, strategy),
            _ => allCandidates.Where(c => c.IsActive)
        };
    }

    /// <summary>
    /// Applies voting rules to filter and select candidates.
    /// </summary>
    /// <param name="candidates">The candidates to filter.</param>
    /// <param name="rules">The voting rules.</param>
    /// <returns>Selected candidates.</returns>
    public IEnumerable<Candidate> ApplyVotingRules(IEnumerable<Candidate> candidates, VotingRules rules)
    {
        var filteredCandidates = candidates.AsEnumerable();

        if (rules.OnlyActiveNodes)
        {
            filteredCandidates = filteredCandidates.Where(c => c.IsActive);
        }

        if (rules.OnlyConsensusNodes)
        {
            filteredCandidates = filteredCandidates.Where(c => c.IsConsensusNode);
        }

        filteredCandidates = filteredCandidates.Where(c => c.UptimePercentage >= rules.MinUptimePercentage);

        if (rules.VoteForBestProfit)
        {
            filteredCandidates = filteredCandidates.OrderByDescending(c => c.ExpectedReward);
        }
        else
        {
            // Default to stability-focused ordering
            filteredCandidates = filteredCandidates.OrderByDescending(c => c.UptimePercentage)
                .ThenByDescending(c => c.VotesReceived);
        }

        return filteredCandidates.Take(rules.MaxCandidates);
    }

    /// <summary>
    /// Gets candidates based on custom voting strategy.
    /// </summary>
    /// <param name="allCandidates">All available candidates.</param>
    /// <param name="strategy">The voting strategy.</param>
    /// <returns>Filtered candidates.</returns>
    private static IEnumerable<Candidate> GetCustomCandidates(IEnumerable<Candidate> allCandidates, VotingStrategy strategy)
    {
        // Apply custom filtering logic based on strategy parameters
        var candidates = allCandidates.AsEnumerable();

        // Apply custom filters from strategy parameters
        if (strategy.Parameters.TryGetValue("MinUptimePercentage", out var minUptimeObj) &&
            minUptimeObj is double minUptime)
        {
            candidates = candidates.Where(c => c.UptimePercentage >= minUptime);
        }

        if (strategy.Parameters.TryGetValue("MinPerformanceScore", out var minPerfObj) &&
            minPerfObj is double minPerf)
        {
            candidates = candidates.Where(c => c.Metrics.PerformanceScore >= minPerf);
        }

        if (strategy.Parameters.TryGetValue("RequireConsensus", out var requireConsensusObj) &&
            requireConsensusObj is bool requireConsensus && requireConsensus)
        {
            candidates = candidates.Where(c => c.IsConsensusNode);
        }

        // Apply max candidates limit
        if (strategy.Rules.MaxCandidates > 0)
        {
            candidates = candidates.Take(strategy.Rules.MaxCandidates);
        }

        return candidates;
    }

    /// <summary>
    /// Gets candidates for conditional voting strategy.
    /// </summary>
    /// <param name="allCandidates">All available candidates.</param>
    /// <param name="strategy">The voting strategy.</param>
    /// <returns>Conditional candidates.</returns>
    public IEnumerable<Candidate> GetConditionalCandidates(IEnumerable<Candidate> allCandidates, VotingStrategy strategy)
    {
        var result = new List<Candidate>();
        var candidateDict = allCandidates.ToDictionary(c => c.Address);

        // First, try preferred candidates
        foreach (var preferredAddress in strategy.PreferredCandidates)
        {
            if (candidateDict.TryGetValue(preferredAddress, out var candidate) && candidate.IsActive)
            {
                result.Add(candidate);
            }
        }

        // If we don't have enough, use fallback candidates
        if (result.Count < strategy.Rules.MaxCandidates)
        {
            var needed = strategy.Rules.MaxCandidates - result.Count;
            var usedAddresses = result.Select(c => c.Address).ToHashSet();

            foreach (var fallbackAddress in strategy.FallbackCandidates)
            {
                if (needed <= 0) break;

                if (candidateDict.TryGetValue(fallbackAddress, out var candidate) &&
                    candidate.IsActive &&
                    !usedAddresses.Contains(candidate.Address))
                {
                    result.Add(candidate);
                    needed--;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets balanced candidates for diversified voting.
    /// </summary>
    /// <param name="candidates">Available candidates.</param>
    /// <param name="maxCount">Maximum number of candidates.</param>
    /// <returns>Balanced candidates.</returns>
    public IEnumerable<Candidate> GetBalancedCandidates(IEnumerable<Candidate> candidates, int maxCount)
    {
        var activeCandidates = candidates.Where(c => c.IsActive).ToList();

        // Balance between top performers and diverse selection
        var topPerformers = activeCandidates.OrderByDescending(c => c.Metrics.PerformanceScore).Take(maxCount / 2);
        var diverseSelection = activeCandidates.OrderBy(c => c.VotesReceived).Take(maxCount - maxCount / 2);

        return topPerformers.Concat(diverseSelection).Distinct().Take(maxCount);
    }

    /// <summary>
    /// Calculates risk assessment for voting candidates.
    /// </summary>
    /// <param name="candidates">The candidates to assess.</param>
    /// <returns>Risk assessment.</returns>
    public VotingRiskAssessment CalculateRiskAssessment(Candidate[] candidates)
    {
        if (candidates.Length == 0)
        {
            return new VotingRiskAssessment { OverallRisk = 1.0 };
        }

        // Calculate concentration risk (voting for too few candidates)
        var concentrationRisk = candidates.Length < 10 ? 0.8 : 0.2;

        // Calculate performance risk (based on uptime and performance scores)
        var avgUptime = candidates.Average(c => c.UptimePercentage);
        var performanceRisk = avgUptime < 95 ? 0.7 : avgUptime < 98 ? 0.3 : 0.1;

        // Calculate reward risk (based on expected rewards variance)
        var rewardVariance = candidates.Length > 1
            ? candidates.Select(c => (double)c.ExpectedReward).Variance()
            : 0;
        var rewardRisk = rewardVariance > 100 ? 0.6 : 0.2;

        var overallRisk = (concentrationRisk + performanceRisk + rewardRisk) / 3;

        return new VotingRiskAssessment
        {
            OverallRisk = overallRisk,
            ConcentrationRisk = concentrationRisk,
            PerformanceRisk = performanceRisk,
            RewardRisk = rewardRisk,
            RiskFactors = GetRiskFactors(concentrationRisk, performanceRisk, rewardRisk),
            DetailedRisks = new Dictionary<string, double>
            {
                ["ConcentrationRisk"] = concentrationRisk,
                ["PerformanceRisk"] = performanceRisk,
                ["RewardRisk"] = rewardRisk,
                ["CandidateCount"] = candidates.Length,
                ["AverageUptime"] = avgUptime
            }
        };
    }

    /// <summary>
    /// Gets risk factors based on risk scores.
    /// </summary>
    /// <param name="concentrationRisk">Concentration risk score.</param>
    /// <param name="performanceRisk">Performance risk score.</param>
    /// <param name="rewardRisk">Reward risk score.</param>
    /// <returns>Risk factors.</returns>
    public string[] GetRiskFactors(double concentrationRisk, double performanceRisk, double rewardRisk)
    {
        var factors = new List<string>();

        if (concentrationRisk > 0.5)
            factors.Add("High concentration risk - voting for too few candidates");

        if (performanceRisk > 0.5)
            factors.Add("Performance risk - some candidates have low uptime");

        if (rewardRisk > 0.5)
            factors.Add("Reward risk - high variance in expected rewards");

        return factors.ToArray();
    }

    /// <summary>
    /// Generates recommendation reasoning text.
    /// </summary>
    /// <param name="strategyType">The strategy type.</param>
    /// <param name="candidateCount">Number of recommended candidates.</param>
    /// <returns>Reasoning text.</returns>
    public string GenerateRecommendationReasoning(VotingStrategyType strategyType, int candidateCount)
    {
        return strategyType switch
        {
            VotingStrategyType.SimpleMajority => $"Recommended {candidateCount} candidates using simple majority voting (>50% required).",
            VotingStrategyType.SuperMajority => $"Recommended {candidateCount} candidates using super majority voting (≥67% required).",
            VotingStrategyType.Unanimous => $"Recommended {candidateCount} candidates requiring unanimous consensus.",
            _ => $"Recommended {candidateCount} candidates based on specified criteria."
        };
    }

    /// <summary>
    /// Calculates confidence score for recommendations.
    /// </summary>
    /// <param name="candidates">Recommended candidates.</param>
    /// <param name="preferences">User preferences.</param>
    /// <returns>Confidence score (0-1).</returns>
    public double CalculateConfidenceScore(Candidate[] candidates, VotingPreferences preferences)
    {
        if (candidates.Length == 0) return 0;

        var avgUptime = candidates.Average(c => c.UptimePercentage);
        var activeRatio = candidates.Count(c => c.IsActive) / (double)candidates.Length;
        var consensusRatio = preferences.PreferConsensusNodes
            ? candidates.Count(c => c.IsConsensusNode) / (double)candidates.Length
            : 1.0;

        // Base confidence on uptime, activity, and consensus participation
        var confidence = (avgUptime / 100.0 * 0.4) + (activeRatio * 0.3) + (consensusRatio * 0.3);

        return Math.Min(1.0, confidence);
    }
}

/// <summary>
/// Extension methods for statistical calculations.
/// </summary>
public static class StatisticsExtensions
{
    /// <summary>
    /// Calculates the variance of a sequence of values.
    /// </summary>
    /// <param name="values">The values.</param>
    /// <returns>The variance.</returns>
    public static double Variance(this IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (valueList.Count <= 1) return 0;

        var mean = valueList.Average();
        var sumOfSquaredDifferences = valueList.Sum(v => Math.Pow(v - mean, 2));
        return sumOfSquaredDifferences / (valueList.Count - 1);
    }
}
