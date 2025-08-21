using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;
using NeoServiceLayer.Services.Voting.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NeoServiceLayer.Services.Voting;

public partial class VotingService
{
    private IVotingRepository? _votingRepository;
    private ISealedDataRepository? _sealedDataRepository;

    /// <summary>
    /// Initializes PostgreSQL storage for the voting service.
    /// </summary>
    private async Task InitializePostgreSQLStorageAsync()
    {
        try
        {
            // Get repositories from service provider
            var serviceProvider = _storageService as IServiceProvider;
            if (serviceProvider != null)
            {
                _votingRepository = serviceProvider.GetService<IVotingRepository>();
                _sealedDataRepository = serviceProvider.GetService<ISealedDataRepository>();
            }

            if (_votingRepository != null && _sealedDataRepository != null)
            {
                Logger.LogInformation("PostgreSQL storage initialized for VotingService");
                
                // Load persisted data from PostgreSQL
                await LoadPersistedDataFromPostgreSQLAsync();
            }
            else
            {
                Logger.LogWarning("PostgreSQL repositories not available for VotingService, falling back to regular storage");
                // Fall back to regular storage loading
                await LoadPersistedDataAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize PostgreSQL storage for VotingService, falling back to regular storage");
            await LoadPersistedDataAsync();
        }
    }

    /// <summary>
    /// Loads persisted data from PostgreSQL storage.
    /// </summary>
    private async Task LoadPersistedDataFromPostgreSQLAsync()
    {
        if (_votingRepository == null || _sealedDataRepository == null)
        {
            await LoadPersistedDataAsync();
            return;
        }

        try
        {
            Logger.LogInformation("Loading voting data from PostgreSQL storage");

            // Load voting proposals and results
            await LoadVotingProposalsFromPostgreSQLAsync();

            // Load candidates
            await LoadCandidatesFromPostgreSQLAsync();

            Logger.LogInformation("Loaded persisted voting data from PostgreSQL: {StrategiesCount} strategies, {ResultsCount} results, {CandidatesCount} candidates",
                _votingStrategies.Count, _votingResults.Count, _candidates.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load voting data from PostgreSQL, falling back to regular storage");
            await LoadPersistedDataAsync();
        }
    }

    /// <summary>
    /// Loads voting proposals and results from PostgreSQL.
    /// </summary>
    private async Task LoadVotingProposalsFromPostgreSQLAsync()
    {
        if (_votingRepository == null) return;

        try
        {
            var proposals = await _votingRepository.GetAllAsync();
            var votes = await _votingRepository.GetVotesByProposalAsync(Guid.Empty); // Get all votes

            foreach (var proposal in proposals.Where(p => p.IsActive))
            {
                // Create voting strategy from proposal
                var strategy = new VotingStrategy
                {
                    Id = proposal.Id.ToString(),
                    Name = proposal.Title,
                    Description = proposal.Description,
                    CandidateSelectionCriteria = new List<string> { "Neo Council Candidate" },
                    VotingWeights = new Dictionary<string, decimal>(),
                    IsAutomatic = false,
                    IsActive = proposal.IsActive,
                    CreatedAt = proposal.CreatedAt,
                    UpdatedAt = proposal.UpdatedAt
                };

                // Parse metadata for additional strategy information
                if (!string.IsNullOrEmpty(proposal.Metadata))
                {
                    try
                    {
                        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(proposal.Metadata) ?? new Dictionary<string, object>();
                        
                        if (metadata.TryGetValue("candidate_criteria", out var criteria) && criteria is JsonElement criteriaElement)
                        {
                            if (criteriaElement.ValueKind == JsonValueKind.Array)
                            {
                                strategy.CandidateSelectionCriteria = criteriaElement.EnumerateArray()
                                    .Select(c => c.GetString() ?? string.Empty)
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .ToList();
                            }
                        }

                        if (metadata.TryGetValue("voting_weights", out var weights) && weights is JsonElement weightsElement)
                        {
                            if (weightsElement.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var weightProperty in weightsElement.EnumerateObject())
                                {
                                    if (weightProperty.Value.TryGetDecimal(out var weightValue))
                                    {
                                        strategy.VotingWeights[weightProperty.Name] = weightValue;
                                    }
                                }
                            }
                        }

                        if (metadata.TryGetValue("is_automatic", out var isAuto) && isAuto is JsonElement isAutoElement)
                        {
                            if (isAutoElement.ValueKind == JsonValueKind.True || isAutoElement.ValueKind == JsonValueKind.False)
                            {
                                strategy.IsAutomatic = isAutoElement.GetBoolean();
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Logger.LogWarning(ex, "Failed to parse proposal metadata for proposal {ProposalId}", proposal.Id);
                    }
                }

                lock (_strategiesLock)
                {
                    _votingStrategies[strategy.Id] = strategy;
                }

                // Create voting result from proposal and votes
                var proposalVotes = votes.Where(v => v.ProposalId == proposal.Id);
                var result = new VotingResult
                {
                    StrategyId = strategy.Id,
                    ProposalId = proposal.Id.ToString(),
                    VotesCast = proposalVotes.Count(),
                    TotalWeight = proposalVotes.Sum(v => v.Weight),
                    Results = new Dictionary<string, decimal>(),
                    Timestamp = proposal.UpdatedAt,
                    IsCompleted = proposal.Status == "Completed",
                    ExecutionDetails = new VotingExecutionDetails
                    {
                        StartTime = proposal.CreatedAt,
                        EndTime = proposal.ExpiresAt,
                        TotalCandidatesEvaluated = 0,
                        SuccessfulVotes = proposalVotes.Count(v => v.IsValid),
                        FailedVotes = proposalVotes.Count(v => !v.IsValid),
                        ErrorMessages = new List<string>()
                    }
                };

                // Calculate vote results
                foreach (var voteGroup in proposalVotes.GroupBy(v => v.CandidateId))
                {
                    var candidateId = voteGroup.Key?.ToString() ?? "Unknown";
                    var totalWeight = voteGroup.Sum(v => v.Weight);
                    result.Results[candidateId] = totalWeight;
                }

                lock (_resultsLock)
                {
                    _votingResults[result.StrategyId] = result;
                }
            }

            Logger.LogInformation("Loaded {ProposalCount} voting proposals from PostgreSQL", proposals.Count());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load voting proposals from PostgreSQL");
            throw;
        }
    }

    /// <summary>
    /// Loads candidates from PostgreSQL sealed data.
    /// </summary>
    private async Task LoadCandidatesFromPostgreSQLAsync()
    {
        if (_sealedDataRepository == null) return;

        try
        {
            // Load candidates from sealed data storage
            var candidateKeys = await _sealedDataRepository.GetKeysByServiceAsync("VotingService");
            var candidateEntries = candidateKeys.Where(k => k.StartsWith("candidates:"));

            foreach (var key in candidateEntries)
            {
                try
                {
                    var sealedData = await _sealedDataRepository.GetByKeyAsync(key, "VotingService");
                    if (sealedData != null)
                    {
                        // In a real implementation, this would be unsealed by SGX
                        // For now, we'll assume the data is stored as JSON in metadata
                        if (!string.IsNullOrEmpty(sealedData.Metadata))
                        {
                            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(sealedData.Metadata) ?? new Dictionary<string, object>();
                            
                            if (metadata.TryGetValue("candidate_data", out var candidateDataObj) && candidateDataObj is JsonElement candidateElement)
                            {
                                var candidate = JsonSerializer.Deserialize<Candidate>(candidateElement.GetRawText());
                                if (candidate != null)
                                {
                                    lock (_candidatesLock)
                                    {
                                        _candidates[candidate.Address] = candidate;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to load candidate from key {Key}", key);
                }
            }

            Logger.LogInformation("Loaded {CandidateCount} candidates from PostgreSQL sealed storage", _candidates.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load candidates from PostgreSQL sealed storage");
            throw;
        }
    }

    /// <summary>
    /// Persists voting proposal to PostgreSQL.
    /// </summary>
    private async Task PersistVotingProposalAsync(VotingStrategy strategy, VotingResult? result = null)
    {
        if (_votingRepository == null) return;

        try
        {
            var proposalId = Guid.TryParse(strategy.Id, out var id) ? id : Guid.NewGuid();

            // Check if proposal already exists
            var existingProposal = await _votingRepository.GetByIdAsync(proposalId);

            var metadata = new Dictionary<string, object>
            {
                ["candidate_criteria"] = strategy.CandidateSelectionCriteria,
                ["voting_weights"] = strategy.VotingWeights,
                ["is_automatic"] = strategy.IsAutomatic
            };

            if (existingProposal == null)
            {
                // Create new proposal
                var proposal = new Infrastructure.Persistence.PostgreSQL.Entities.VotingEntities.VotingProposal
                {
                    Id = proposalId,
                    Title = strategy.Name,
                    Description = strategy.Description,
                    CreatedBy = Guid.Empty, // System-created
                    Status = strategy.IsActive ? "Active" : "Inactive",
                    Category = "Council Election",
                    QuorumRequired = 0.5m,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    IsActive = strategy.IsActive,
                    Metadata = JsonSerializer.Serialize(metadata),
                    CreatedAt = strategy.CreatedAt,
                    UpdatedAt = strategy.UpdatedAt
                };

                await _votingRepository.CreateAsync(proposal);
            }
            else
            {
                // Update existing proposal
                existingProposal.Title = strategy.Name;
                existingProposal.Description = strategy.Description;
                existingProposal.Status = strategy.IsActive ? "Active" : "Inactive";
                existingProposal.IsActive = strategy.IsActive;
                existingProposal.Metadata = JsonSerializer.Serialize(metadata);
                existingProposal.UpdatedAt = DateTime.UtcNow;

                await _votingRepository.UpdateAsync(existingProposal);
            }

            Logger.LogDebug("Persisted voting proposal {ProposalId} to PostgreSQL", proposalId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist voting proposal for strategy {StrategyId} to PostgreSQL", strategy.Id);
        }
    }

    /// <summary>
    /// Persists candidate to PostgreSQL sealed storage.
    /// </summary>
    private async Task PersistCandidateAsync(Candidate candidate)
    {
        if (_sealedDataRepository == null) return;

        try
        {
            var key = $"candidates:{candidate.Address}";
            var candidateData = JsonSerializer.Serialize(candidate);
            
            // In a real implementation, this would be sealed by SGX
            // For now, we'll store the candidate data in metadata
            var metadata = new Dictionary<string, object>
            {
                ["candidate_data"] = JsonSerializer.Deserialize<object>(candidateData) ?? new { }
            };

            var sealedData = new Infrastructure.Persistence.PostgreSQL.Entities.SgxEntities.SealedDataItem
            {
                Id = Guid.NewGuid(),
                Key = key,
                ServiceName = "VotingService",
                SealedData = System.Text.Encoding.UTF8.GetBytes(candidateData), // Would be sealed in real implementation
                SealingPolicy = Infrastructure.Persistence.PostgreSQL.Entities.SgxEntities.SealingPolicyType.MrSigner,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _sealedDataRepository.StoreAsync(
                key, 
                "VotingService", 
                sealedData.SealedData, 
                sealedData.SealingPolicy, 
                sealedData.ExpiresAt, 
                metadata
            );

            Logger.LogDebug("Persisted candidate {CandidateAddress} to PostgreSQL sealed storage", candidate.Address);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist candidate {CandidateAddress} to PostgreSQL sealed storage", candidate.Address);
        }
    }

    /// <summary>
    /// Persists all voting data to PostgreSQL.
    /// </summary>
    private async Task PersistAllDataToPostgreSQLAsync()
    {
        if (_votingRepository == null || _sealedDataRepository == null)
        {
            await PersistAllDataAsync();
            return;
        }

        try
        {
            // Persist voting strategies and results
            foreach (var strategy in _votingStrategies.Values)
            {
                _votingResults.TryGetValue(strategy.Id, out var result);
                await PersistVotingProposalAsync(strategy, result);
            }

            // Persist candidates
            foreach (var candidate in _candidates.Values)
            {
                await PersistCandidateAsync(candidate);
            }

            Logger.LogDebug("Persisted all voting data to PostgreSQL");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist voting data to PostgreSQL, falling back to regular storage");
            await PersistAllDataAsync();
        }
    }

    /// <summary>
    /// Records a vote in PostgreSQL.
    /// </summary>
    public async Task<bool> RecordVoteAsync(Guid proposalId, Guid voterId, Guid? candidateId, decimal weight, bool isValid = true)
    {
        if (_votingRepository == null) return false;

        try
        {
            var vote = new Infrastructure.Persistence.PostgreSQL.Entities.VotingEntities.Vote
            {
                Id = Guid.NewGuid(),
                ProposalId = proposalId,
                VoterId = voterId,
                CandidateId = candidateId,
                Weight = weight,
                IsValid = isValid,
                VotedAt = DateTime.UtcNow,
                Metadata = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["voting_service"] = "VotingService",
                    ["timestamp"] = DateTime.UtcNow,
                    ["is_delegation"] = false
                })
            };

            await _votingRepository.CreateVoteAsync(vote);

            Logger.LogDebug("Recorded vote for proposal {ProposalId} by voter {VoterId} in PostgreSQL", proposalId, voterId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to record vote for proposal {ProposalId} by voter {VoterId} in PostgreSQL", proposalId, voterId);
            return false;
        }
    }

    /// <summary>
    /// Gets voting results from PostgreSQL.
    /// </summary>
    public async Task<VotingResult?> GetVotingResultAsync(Guid proposalId)
    {
        if (_votingRepository == null) return null;

        try
        {
            var proposal = await _votingRepository.GetByIdAsync(proposalId);
            if (proposal == null) return null;

            var votes = await _votingRepository.GetVotesByProposalAsync(proposalId);

            var result = new VotingResult
            {
                StrategyId = proposalId.ToString(),
                ProposalId = proposalId.ToString(),
                VotesCast = votes.Count(),
                TotalWeight = votes.Sum(v => v.Weight),
                Results = new Dictionary<string, decimal>(),
                Timestamp = proposal.UpdatedAt,
                IsCompleted = proposal.Status == "Completed",
                ExecutionDetails = new VotingExecutionDetails
                {
                    StartTime = proposal.CreatedAt,
                    EndTime = proposal.ExpiresAt,
                    TotalCandidatesEvaluated = votes.Select(v => v.CandidateId).Distinct().Count(),
                    SuccessfulVotes = votes.Count(v => v.IsValid),
                    FailedVotes = votes.Count(v => !v.IsValid),
                    ErrorMessages = new List<string>()
                }
            };

            // Calculate vote results by candidate
            foreach (var voteGroup in votes.GroupBy(v => v.CandidateId))
            {
                var candidateId = voteGroup.Key?.ToString() ?? "Unknown";
                var totalWeight = voteGroup.Sum(v => v.Weight);
                result.Results[candidateId] = totalWeight;
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get voting result for proposal {ProposalId} from PostgreSQL", proposalId);
            return null;
        }
    }
}