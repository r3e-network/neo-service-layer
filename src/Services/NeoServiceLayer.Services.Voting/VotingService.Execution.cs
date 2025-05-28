using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Voting execution operations for the Voting Service.
/// </summary>
public partial class VotingService
{
    /// <inheritdoc/>
    public async Task<bool> ExecuteVotingAsync(string strategyId, string voterAddress, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(strategyId);
        ArgumentException.ThrowIfNullOrEmpty(voterAddress);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            VotingStrategy? strategy;
            lock (_strategiesLock)
            {
                if (!_votingStrategies.TryGetValue(strategyId, out strategy))
                {
                    Logger.LogWarning("Voting strategy {StrategyId} not found", strategyId);
                    return false;
                }
            }

            var executionId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            try
            {
                Logger.LogInformation("Executing voting strategy {StrategyId} for voter {VoterAddress}",
                    strategyId, voterAddress);

                // Get eligible candidates based on strategy
                var eligibleCandidates = await GetEligibleCandidatesAsync(strategy, blockchainType);

                // Apply voting rules and select final candidates
                var selectedCandidates = ApplyVotingRules(eligibleCandidates, strategy.Rules);

                // Simulate voting execution (in real implementation, this would interact with Neo N3 blockchain)
                await Task.Delay(1000); // Simulate blockchain interaction

                var votingResult = new VotingResult
                {
                    ExecutionId = executionId,
                    StrategyId = strategyId,
                    VoterAddress = voterAddress,
                    ExecutedAt = startTime,
                    Success = true,
                    SelectedCandidates = selectedCandidates.Select(c => c.Address).ToArray(),
                    TransactionHash = Guid.NewGuid().ToString(), // Simulate transaction hash
                    ExecutionDetails = new Dictionary<string, object>
                    {
                        ["TotalCandidatesEvaluated"] = eligibleCandidates.Count(),
                        ["CandidatesVotedFor"] = selectedCandidates.Count(),
                        ["ExecutionTimeMs"] = (DateTime.UtcNow - startTime).TotalMilliseconds,
                        ["StrategyType"] = strategy.StrategyType.ToString()
                    }
                };

                lock (_resultsLock)
                {
                    _votingResults[executionId] = votingResult;
                }

                // Update strategy execution info
                strategy.LastExecuted = DateTime.UtcNow;
                if (strategy.AutoExecute)
                {
                    strategy.NextExecution = DateTime.UtcNow.Add(strategy.ExecutionInterval);
                }

                // Persist to storage
                await PersistVotingResultsAsync();
                await PersistVotingStrategiesAsync();

                Logger.LogInformation("Successfully executed voting strategy {StrategyId}, voted for {CandidateCount} candidates",
                    strategyId, selectedCandidates.Count());

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing voting strategy {StrategyId}", strategyId);

                var errorResult = new VotingResult
                {
                    ExecutionId = executionId,
                    StrategyId = strategyId,
                    VoterAddress = voterAddress,
                    ExecutedAt = startTime,
                    Success = false,
                    ErrorMessage = ex.Message
                };

                lock (_resultsLock)
                {
                    _votingResults[executionId] = errorResult;
                }

                // Persist error result to storage
                await PersistVotingResultsAsync();

                return false;
            }
        });
    }

    /// <inheritdoc/>
    public Task<VotingResult> GetVotingResultAsync(string executionId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(executionId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        lock (_resultsLock)
        {
            if (_votingResults.TryGetValue(executionId, out var result))
            {
                return Task.FromResult(result);
            }
        }

        throw new ArgumentException($"Voting result {executionId} not found", nameof(executionId));
    }

    /// <summary>
    /// Gets eligible candidates based on voting strategy.
    /// </summary>
    /// <param name="strategy">The voting strategy.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Eligible candidates.</returns>
    private async Task<IEnumerable<CandidateInfo>> GetEligibleCandidatesAsync(VotingStrategy strategy, BlockchainType blockchainType)
    {
        var allCandidates = await GetCandidatesAsync(blockchainType);

        return strategy.StrategyType switch
        {
            VotingStrategyType.Manual => allCandidates.Where(c => c.IsActive),
            VotingStrategyType.Automatic => allCandidates.OrderByDescending(c => c.VotesReceived).Take(21),
            VotingStrategyType.Conditional => GetConditionalCandidates(allCandidates, strategy),
            VotingStrategyType.ProfitOptimized => allCandidates.OrderByDescending(c => c.ExpectedReward),
            VotingStrategyType.StabilityFocused => allCandidates.Where(c => c.IsActive && c.UptimePercentage >= 98.0),
            VotingStrategyType.Custom => GetCustomCandidates(allCandidates, strategy),
            _ => allCandidates.Where(c => c.IsActive)
        };
    }

    /// <summary>
    /// Applies voting rules to filter and select candidates.
    /// </summary>
    /// <param name="candidates">The candidates to filter.</param>
    /// <param name="rules">The voting rules.</param>
    /// <returns>Selected candidates.</returns>
    private IEnumerable<CandidateInfo> ApplyVotingRules(IEnumerable<CandidateInfo> candidates, VotingRules rules)
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
    /// Gets candidates for conditional voting strategy.
    /// </summary>
    /// <param name="allCandidates">All available candidates.</param>
    /// <param name="strategy">The voting strategy.</param>
    /// <returns>Conditional candidates.</returns>
    private IEnumerable<CandidateInfo> GetConditionalCandidates(IEnumerable<CandidateInfo> allCandidates, VotingStrategy strategy)
    {
        var result = new List<CandidateInfo>();
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

            // If still not enough, fill with top active candidates
            if (result.Count < strategy.Rules.MaxCandidates)
            {
                var remainingNeeded = strategy.Rules.MaxCandidates - result.Count;
                var usedSet = result.Select(c => c.Address).ToHashSet();
                var additionalCandidates = allCandidates
                    .Where(c => c.IsActive && !usedSet.Contains(c.Address))
                    .OrderByDescending(c => c.VotesReceived)
                    .Take(remainingNeeded);

                result.AddRange(additionalCandidates);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets candidates for custom voting strategy.
    /// </summary>
    /// <param name="allCandidates">All available candidates.</param>
    /// <param name="strategy">The voting strategy.</param>
    /// <returns>Custom candidates.</returns>
    private IEnumerable<CandidateInfo> GetCustomCandidates(IEnumerable<CandidateInfo> allCandidates, VotingStrategy strategy)
    {
        // Apply custom logic based on strategy parameters
        var candidates = allCandidates.Where(c => c.IsActive);

        // Apply custom filters from strategy parameters
#pragma warning disable IDE0055 // Fix formatting
        if (strategy.Parameters.TryGetValue("MinUptime", out var minUptimeObj) &&
            minUptimeObj is double minUptime)
        {
            candidates = candidates.Where(c => c.UptimePercentage >= minUptime);
        }

        if (strategy.Parameters.TryGetValue("MinRank", out var minRankObj) &&
            minRankObj is int minRank)
        {
            candidates = candidates.Where(c => c.Rank >= minRank);
        }
#pragma warning restore IDE0055 // Fix formatting

        return candidates.Take(strategy.Rules.MaxCandidates);
    }
}
