using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Interface for Voting Service operations.
/// </summary>
public interface IVotingService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Creates a new voting strategy.
    /// </summary>
    Task<string> CreateVotingStrategyAsync(VotingStrategyRequest request, BlockchainType blockchainType);
    
    /// <summary>
    /// Executes voting using a specific strategy.
    /// </summary>
    Task<bool> ExecuteVotingAsync(string strategyId, string voterAddress, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets the result of a voting execution.
    /// </summary>
    Task<VotingResult> GetVotingResultAsync(string executionId, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets available candidates for voting.
    /// </summary>
    Task<IEnumerable<CandidateInfo>> GetCandidatesAsync(BlockchainType blockchainType);
    
    /// <summary>
    /// Gets voting strategies for an owner.
    /// </summary>
    Task<IEnumerable<VotingStrategy>> GetVotingStrategiesAsync(string ownerAddress, BlockchainType blockchainType);
    
    /// <summary>
    /// Updates an existing voting strategy.
    /// </summary>
    Task<bool> UpdateVotingStrategyAsync(string strategyId, VotingStrategyUpdate update, BlockchainType blockchainType);
    
    /// <summary>
    /// Deletes a voting strategy.
    /// </summary>
    Task<bool> DeleteVotingStrategyAsync(string strategyId, BlockchainType blockchainType);
    
    /// <summary>
    /// Gets voting recommendations based on preferences.
    /// </summary>
    Task<VotingRecommendation> GetVotingRecommendationAsync(VotingPreferences preferences, BlockchainType blockchainType);
}