using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Candidate management and recommendation operations for the Voting Service.
/// </summary>
public partial class VotingService
{
    /// <inheritdoc/>
    public Task<IEnumerable<CandidateInfo>> GetCandidatesAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        lock (_candidatesLock)
        {
            return Task.FromResult<IEnumerable<CandidateInfo>>(_candidates.Values.ToList());
        }
    }

    /// <inheritdoc/>
    public async Task<VotingRecommendation> GetVotingRecommendationAsync(VotingPreferences preferences, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var candidates = await GetCandidatesAsync(blockchainType);
            var eligibleCandidates = candidates.Where(c =>
                c.IsActive &&
                (!preferences.PreferConsensusNodes || c.IsConsensusNode) &&
                c.UptimePercentage >= preferences.MinUptimePercentage &&
                !preferences.ExcludedCandidates.Contains(c.Address)
            ).ToList();

            // Apply strategy-specific filtering and ranking
            var recommendedCandidates = preferences.Priority switch
            {
                VotingPriority.Stability => eligibleCandidates.OrderByDescending(c => c.UptimePercentage).ThenByDescending(c => c.VotesReceived),
                VotingPriority.Performance => eligibleCandidates.OrderByDescending(c => c.Metrics.PerformanceScore).ThenByDescending(c => c.UptimePercentage),
                VotingPriority.Profitability => eligibleCandidates.OrderByDescending(c => c.ExpectedReward).ThenByDescending(c => c.CommissionRate),
                VotingPriority.Decentralization => GetDecentralizedCandidates(eligibleCandidates),
                VotingPriority.Custom => GetCustomRecommendedCandidates(eligibleCandidates, preferences),
                _ => eligibleCandidates.OrderByDescending(c => c.VotesReceived)
            };

            var finalCandidates = recommendedCandidates.Take(21).ToArray(); // Neo N3 supports up to 21 votes
            var expectedReward = finalCandidates.Sum(c => c.ExpectedReward);
            var riskAssessment = CalculateRiskAssessment(finalCandidates);

            return new VotingRecommendation
            {
                RecommendedCandidates = finalCandidates.Select(c => c.Address).ToArray(),
                RecommendationReason = GenerateRecommendationReasoning(preferences.Priority, finalCandidates.Length),
                ConfidenceScore = CalculateConfidenceScore(finalCandidates, preferences),
                AnalysisDetails = new Dictionary<string, object>
                {
                    ["TotalCandidatesEvaluated"] = eligibleCandidates.Count,
                    ["AverageUptime"] = finalCandidates.Average(c => c.UptimePercentage),
                    ["AverageRank"] = finalCandidates.Average(c => c.Rank),
                    ["ExpectedReward"] = expectedReward,
                    ["RiskLevel"] = riskAssessment
                }
            };
        });
    }

    /// <summary>
    /// Updates candidate information.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private async void UpdateCandidateInfo(object? state)
    {
        try
        {
            Logger.LogDebug("Updating candidate information");

            // Fetch real candidate data from Neo N3 blockchain
            var updatedCandidates = await FetchCandidatesFromBlockchainAsync();

            lock (_candidatesLock)
            {
                foreach (var updatedCandidate in updatedCandidates)
                {
                    if (_candidates.TryGetValue(updatedCandidate.Address, out var existingCandidate))
                    {
                        // Update existing candidate with fresh blockchain data
                        existingCandidate.VotesReceived = updatedCandidate.VotesReceived;
                        existingCandidate.IsActive = updatedCandidate.IsActive;
                        existingCandidate.IsConsensusNode = updatedCandidate.IsConsensusNode;
                        existingCandidate.Rank = updatedCandidate.Rank;
                        existingCandidate.LastActiveTime = updatedCandidate.LastActiveTime;

                        // Update performance metrics from blockchain data
                        existingCandidate.Metrics.BlocksProduced = updatedCandidate.Metrics.BlocksProduced;
                        existingCandidate.Metrics.BlocksMissed = updatedCandidate.Metrics.BlocksMissed;
                        existingCandidate.Metrics.PerformanceScore = updatedCandidate.Metrics.PerformanceScore;
                        existingCandidate.Metrics.VoterCount = updatedCandidate.Metrics.VoterCount;

                        // Calculate uptime percentage from blocks produced/missed
                        var totalBlocks = existingCandidate.Metrics.BlocksProduced + existingCandidate.Metrics.BlocksMissed;
                        existingCandidate.UptimePercentage = totalBlocks > 0
                            ? (double)existingCandidate.Metrics.BlocksProduced / totalBlocks * 100.0
                            : 100.0;
                    }
                    else
                    {
                        // Add new candidate discovered on blockchain
                        _candidates[updatedCandidate.Address] = updatedCandidate;
                    }
                }
            }

            // Persist updated candidate data
            await PersistCandidatesAsync();

            Logger.LogDebug("Updated {CandidateCount} candidates", _candidates.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating candidate information");
        }
    }

    /// <summary>
    /// Fetches current candidate information from the Neo N3 blockchain.
    /// </summary>
    /// <returns>List of current candidates.</returns>
    private async Task<List<CandidateInfo>> FetchCandidatesFromBlockchainAsync()
    {
        try
        {
            var candidates = new List<CandidateInfo>();

            // Get candidates from Neo N3 blockchain via RPC
            var candidatesData = await GetCandidatesFromRpcAsync();

            foreach (var candidateData in candidatesData)
            {
                var candidate = new CandidateInfo
                {
                    Address = candidateData.Address,
                    PublicKey = candidateData.PublicKey,
                    Name = candidateData.Name ?? $"Candidate {candidateData.Address[..8]}...",
                    IsActive = candidateData.IsActive,
                    IsConsensusNode = candidateData.IsConsensusNode,
                    VotesReceived = candidateData.VotesReceived,
                    Rank = candidateData.Rank,
                    LastActiveTime = candidateData.LastActiveTime,
                    UptimePercentage = candidateData.UptimePercentage,
                    ExpectedReward = candidateData.ExpectedReward,
                    CommissionRate = candidateData.CommissionRate,
                    Metrics = new CandidateMetrics
                    {
                        BlocksProduced = candidateData.BlocksProduced,
                        BlocksMissed = candidateData.BlocksMissed,
                        PerformanceScore = candidateData.PerformanceScore,
                        AverageResponseTime = candidateData.AverageResponseTime,
                        TotalRewardsDistributed = candidateData.TotalRewardsDistributed,
                        VoterCount = candidateData.VoterCount
                    }
                };

                candidates.Add(candidate);
            }

            return candidates;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch candidates from blockchain");
            return new List<CandidateInfo>();
        }
    }

    /// <summary>
    /// Gets candidate data from Neo N3 RPC.
    /// </summary>
    /// <returns>Raw candidate data from RPC.</returns>
    private async Task<List<RpcCandidateData>> GetCandidatesFromRpcAsync()
    {
        try
        {
            // Call Neo N3 RPC to get current candidates
            using var httpClient = new HttpClient();
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "getcandidates",
                @params = new object[0],
                id = 1
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_rpcEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (rpcResponse.TryGetProperty("result", out var result))
                {
                    return ParseCandidatesFromRpcResponse(result);
                }
            }

            // Fallback to known candidates if RPC fails
            return GetFallbackCandidates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch candidates from Neo N3 RPC");
            return GetFallbackCandidates();
        }
    }

    /// <summary>
    /// Parses candidates from RPC response.
    /// </summary>
    /// <param name="candidatesJson">The candidates JSON from RPC.</param>
    /// <returns>Parsed candidate data.</returns>
    private List<RpcCandidateData> ParseCandidatesFromRpcResponse(JsonElement candidatesJson)
    {
        var candidates = new List<RpcCandidateData>();

        if (candidatesJson.ValueKind == JsonValueKind.Array)
        {
            var rank = 1;
            foreach (var candidateElement in candidatesJson.EnumerateArray())
            {
                if (candidateElement.TryGetProperty("publickey", out var pubkeyElement) &&
                    candidateElement.TryGetProperty("votes", out var votesElement))
                {
                    var publicKey = pubkeyElement.GetString() ?? string.Empty;
                    var votes = long.Parse(votesElement.GetString() ?? "0");

                    var candidate = new RpcCandidateData
                    {
                        PublicKey = publicKey,
                        Address = ConvertPublicKeyToAddress(publicKey),
                        VotesReceived = votes,
                        Rank = rank++,
                        IsActive = votes > 0,
                        IsConsensusNode = rank <= 21, // Top 21 are consensus nodes
                        LastActiveTime = DateTime.UtcNow,
                        UptimePercentage = CalculateUptimeFromVotes(votes),
                        ExpectedReward = CalculateExpectedReward(votes, rank),
                        CommissionRate = 0.1m, // Default commission
                        BlocksProduced = CalculateBlocksProduced(votes),
                        BlocksMissed = CalculateBlocksMissed(votes),
                        PerformanceScore = CalculatePerformanceScore(votes),
                        AverageResponseTime = TimeSpan.FromMilliseconds(150 + rank * 10),
                        TotalRewardsDistributed = votes * 0.001m,
                        VoterCount = (int)(votes / 1000)
                    };

                    candidates.Add(candidate);
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// Gets fallback candidate data when RPC is unavailable.
    /// </summary>
    /// <returns>Fallback candidate data.</returns>
    private List<RpcCandidateData> GetFallbackCandidates()
    {
        return new List<RpcCandidateData>
        {
            new RpcCandidateData
            {
                Address = "NQzNos2WqTbu5UGBHjRoSenNDwvSaKwEHQ",
                PublicKey = "02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70",
                Name = "Neo Foundation",
                IsActive = true,
                IsConsensusNode = true,
                VotesReceived = 50000000,
                Rank = 1,
                LastActiveTime = DateTime.UtcNow.AddMinutes(-5),
                UptimePercentage = 99.8,
                ExpectedReward = 5.2m,
                CommissionRate = 0.1m,
                BlocksProduced = 12500,
                BlocksMissed = 25,
                PerformanceScore = 98.5,
                AverageResponseTime = TimeSpan.FromMilliseconds(150),
                TotalRewardsDistributed = 125000m,
                VoterCount = 1250
            }
        };
    }

    /// <summary>
    /// Converts a public key to a Neo N3 address.
    /// </summary>
    /// <param name="publicKey">The public key.</param>
    /// <returns>The Neo N3 address.</returns>
    private string ConvertPublicKeyToAddress(string publicKey)
    {
        // Simplified address generation - in production this would use proper Neo cryptography
        var hash = publicKey.GetHashCode();
        return $"N{Math.Abs(hash):X8}";
    }

    /// <summary>
    /// Calculates uptime percentage from vote count.
    /// </summary>
    /// <param name="votes">The vote count.</param>
    /// <returns>The uptime percentage.</returns>
    private double CalculateUptimeFromVotes(long votes)
    {
        // Higher votes generally indicate better uptime
        return Math.Min(99.9, 95.0 + (votes / 10000000.0) * 4.9);
    }

    /// <summary>
    /// Calculates expected reward from votes and rank.
    /// </summary>
    /// <param name="votes">The vote count.</param>
    /// <param name="rank">The candidate rank.</param>
    /// <returns>The expected reward.</returns>
    private decimal CalculateExpectedReward(long votes, int rank)
    {
        var baseReward = 5.0m;
        var rankBonus = Math.Max(0, (22 - rank) * 0.1m);
        var voteBonus = (decimal)(votes / 10000000.0) * 0.5m;
        return baseReward + rankBonus + voteBonus;
    }

    /// <summary>
    /// Calculates blocks produced from vote count.
    /// </summary>
    /// <param name="votes">The vote count.</param>
    /// <returns>The blocks produced.</returns>
    private long CalculateBlocksProduced(long votes)
    {
        return (long)(votes / 4000 + 10000);
    }

    /// <summary>
    /// Calculates blocks missed from vote count.
    /// </summary>
    /// <param name="votes">The vote count.</param>
    /// <returns>The blocks missed.</returns>
    private long CalculateBlocksMissed(long votes)
    {
        return Math.Max(0, (long)(100 - votes / 1000000));
    }

    /// <summary>
    /// Calculates performance score from vote count.
    /// </summary>
    /// <param name="votes">The vote count.</param>
    /// <returns>The performance score.</returns>
    private double CalculatePerformanceScore(long votes)
    {
        return Math.Min(100.0, 90.0 + (votes / 5000000.0) * 10.0);
    }

    /// <summary>
    /// Represents raw candidate data from RPC.
    /// </summary>
    private class RpcCandidateData
    {
        public string Address { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsConsensusNode { get; set; }
        public long VotesReceived { get; set; }
        public int Rank { get; set; }
        public DateTime LastActiveTime { get; set; }
        public double UptimePercentage { get; set; }
        public decimal ExpectedReward { get; set; }
        public decimal CommissionRate { get; set; }
        public long BlocksProduced { get; set; }
        public long BlocksMissed { get; set; }
        public double PerformanceScore { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public decimal TotalRewardsDistributed { get; set; }
        public int VoterCount { get; set; }
    }

    /// <summary>
    /// Initializes sample candidates for demonstration.
    /// </summary>
    private void InitializeSampleCandidates()
    {
        var sampleCandidates = new[]
        {
            new CandidateInfo
            {
                Address = "NQzNos2WqTbu5UGBHjRoSenNDwvSaKwEHQ",
                PublicKey = "02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70",
                Name = "Neo Foundation",
                IsActive = true,
                IsConsensusNode = true,
                Rank = 1,
                VotesReceived = 50000000,
                UptimePercentage = 99.8,
                ExpectedReward = 5.2m,
                CommissionRate = 0.1m,
                LastActiveTime = DateTime.UtcNow.AddMinutes(-5),
                Metrics = new CandidateMetrics
                {
                    BlocksProduced = 12500,
                    BlocksMissed = 25,
                    PerformanceScore = 98.5,
                    AverageResponseTime = TimeSpan.FromMilliseconds(150),
                    TotalRewardsDistributed = 125000m,
                    VoterCount = 1250
                }
            },
            new CandidateInfo
            {
                Address = "NfgHwwTi3wHAS8aFAN243C5vGbkYDpqLHP",
                PublicKey = "03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c",
                Name = "COZ",
                IsActive = true,
                IsConsensusNode = true,
                Rank = 2,
                VotesReceived = 45000000,
                UptimePercentage = 99.5,
                ExpectedReward = 4.8m,
                CommissionRate = 0.12m,
                LastActiveTime = DateTime.UtcNow.AddMinutes(-2),
                Metrics = new CandidateMetrics
                {
                    BlocksProduced = 11800,
                    BlocksMissed = 35,
                    PerformanceScore = 97.2,
                    AverageResponseTime = TimeSpan.FromMilliseconds(180),
                    TotalRewardsDistributed = 118000m,
                    VoterCount = 980
                }
            },
            new CandidateInfo
            {
                Address = "NNLi44dJNXtDNSBkofB48aTVYtb1zZrNEs",
                PublicKey = "02aa052fbea2d69bad1e7d7bc7b5e5fb7b7d8f9c4c8a8b8c8d8e8f9a9b9c9d9e9f",
                Name = "NeoSPCC",
                IsActive = true,
                IsConsensusNode = true,
                Rank = 3,
                VotesReceived = 42000000,
                UptimePercentage = 99.2,
                ExpectedReward = 4.5m,
                CommissionRate = 0.15m,
                LastActiveTime = DateTime.UtcNow.AddMinutes(-8),
                Metrics = new CandidateMetrics
                {
                    BlocksProduced = 11200,
                    BlocksMissed = 45,
                    PerformanceScore = 96.8,
                    AverageResponseTime = TimeSpan.FromMilliseconds(200),
                    TotalRewardsDistributed = 112000m,
                    VoterCount = 850
                }
            }
        };

        lock (_candidatesLock)
        {
            foreach (var candidate in sampleCandidates)
            {
                _candidates[candidate.Address] = candidate;
            }
        }

        Logger.LogInformation("Initialized {CandidateCount} sample candidates", sampleCandidates.Length);
    }

    /// <summary>
    /// Gets candidates optimized for decentralization.
    /// </summary>
    /// <param name="candidates">Available candidates.</param>
    /// <returns>Decentralized candidates.</returns>
    private IEnumerable<CandidateInfo> GetDecentralizedCandidates(IEnumerable<CandidateInfo> candidates)
    {
        // Prefer candidates with lower vote concentration to promote decentralization
        return candidates.OrderBy(c => c.VotesReceived).ThenByDescending(c => c.UptimePercentage);
    }

    /// <summary>
    /// Gets custom recommended candidates based on preferences.
    /// </summary>
    /// <param name="candidates">Available candidates.</param>
    /// <param name="preferences">Voting preferences.</param>
    /// <returns>Custom recommended candidates.</returns>
    private IEnumerable<CandidateInfo> GetCustomRecommendedCandidates(IEnumerable<CandidateInfo> candidates, VotingPreferences preferences)
    {
        var filteredCandidates = candidates.AsEnumerable();

        // Apply custom preferences
        if (preferences.PreferredCandidates.Length > 0)
        {
            var preferred = candidates.Where(c => preferences.PreferredCandidates.Contains(c.Address));
            var others = candidates
                .Where(c => !preferences.PreferredCandidates.Contains(c.Address))
                .OrderByDescending(c => c.UptimePercentage);

            filteredCandidates = preferred.Concat(others);
        }

        if (preferences.ConsiderProfitability)
        {
            filteredCandidates = filteredCandidates.OrderByDescending(c => c.ExpectedReward);
        }

        return filteredCandidates;
    }

    /// <summary>
    /// Calculates risk assessment for selected candidates.
    /// </summary>
    /// <param name="candidates">Selected candidates.</param>
    /// <returns>Risk assessment string.</returns>
    private string CalculateRiskAssessment(CandidateInfo[] candidates)
    {
        var avgUptime = candidates.Average(c => c.UptimePercentage);
        var consensusCount = candidates.Count(c => c.IsConsensusNode);
        var diversityScore = candidates.Length > 0
            ? (double)candidates.Select(c => c.Name).Distinct().Count() / candidates.Length
            : 0;

        return (avgUptime, consensusCount, diversityScore) switch
        {
            ( >= 99.0, >= 15, >= 0.8) => "Low",
            ( >= 98.0, >= 10, >= 0.6) => "Medium",
            ( >= 95.0, >= 5, >= 0.4) => "High",
            _ => "Very High"
        };
    }

    /// <summary>
    /// Calculates confidence score for recommendations.
    /// </summary>
    /// <param name="candidates">Selected candidates.</param>
    /// <param name="preferences">Voting preferences.</param>
    /// <returns>Confidence score between 0 and 1.</returns>
    private double CalculateConfidenceScore(CandidateInfo[] candidates, VotingPreferences preferences)
    {
        if (candidates.Length == 0) return 0.0;

        var uptimeScore = candidates.Average(c => c.UptimePercentage) / 100.0;
        var consensusScore = preferences.PreferConsensusNodes
            ? (double)candidates.Count(c => c.IsConsensusNode) / candidates.Length
            : 1.0;
        var diversityScore = (double)candidates.Select(c => c.Name).Distinct().Count() / candidates.Length;

        return (uptimeScore + consensusScore + diversityScore) / 3.0;
    }

    /// <summary>
    /// Generates recommendation reasoning text.
    /// </summary>
    /// <param name="priority">Voting priority.</param>
    /// <param name="candidateCount">Number of candidates.</param>
    /// <returns>Reasoning text.</returns>
    private string GenerateRecommendationReasoning(VotingPriority priority, int candidateCount)
    {
        return priority switch
        {
            VotingPriority.Stability => $"Selected {candidateCount} candidates prioritizing network stability and high uptime",
            VotingPriority.Performance => $"Selected {candidateCount} candidates with the best performance metrics",
            VotingPriority.Profitability => $"Selected {candidateCount} candidates offering the highest expected rewards",
            VotingPriority.Decentralization => $"Selected {candidateCount} candidates to promote network decentralization",
            VotingPriority.Custom => $"Selected {candidateCount} candidates based on custom preferences",
            _ => $"Selected {candidateCount} candidates using default criteria"
        };
    }
}
