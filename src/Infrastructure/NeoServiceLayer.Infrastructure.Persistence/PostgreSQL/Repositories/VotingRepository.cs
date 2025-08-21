using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.VotingEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories
{
    /// <summary>
    /// Voting repository interface.
    /// </summary>
    public interface IVotingRepository
    {
        Task<VotingProposal?> GetProposalByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<VotingProposal>> GetActiveProposalsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<VotingProposal>> GetProposalsByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default);
        Task<VotingProposal> CreateProposalAsync(VotingProposal proposal, CancellationToken cancellationToken = default);
        Task<VotingProposal> UpdateProposalAsync(VotingProposal proposal, CancellationToken cancellationToken = default);
        Task<Vote> CastVoteAsync(Vote vote, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vote>> GetVotesForProposalAsync(Guid proposalId, CancellationToken cancellationToken = default);
        Task<VotingResult?> GetResultAsync(Guid proposalId, CancellationToken cancellationToken = default);
        Task<VotingResult> SaveResultAsync(VotingResult result, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// PostgreSQL implementation of voting repository.
    /// </summary>
    public class VotingRepository : IVotingRepository
    {
        private readonly NeoServiceLayerDbContext _context;
        private readonly ILogger<VotingRepository> _logger;

        public VotingRepository(
            NeoServiceLayerDbContext context,
            ILogger<VotingRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VotingProposal?> GetProposalByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.VotingProposals
                    .Include(p => p.Options)
                    .Include(p => p.Votes)
                    .Include(p => p.Result)
                    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get voting proposal by ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<VotingProposal>> GetActiveProposalsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                return await _context.VotingProposals
                    .Include(p => p.Options)
                    .Include(p => p.Votes)
                    .Include(p => p.Result)
                    .Where(p => p.IsActive && p.StartsAt <= now && p.EndsAt > now)
                    .OrderBy(p => p.EndsAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active voting proposals");
                throw;
            }
        }

        public async Task<IEnumerable<VotingProposal>> GetProposalsByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.VotingProposals
                    .Include(p => p.Options)
                    .Include(p => p.Votes)
                    .Include(p => p.Result)
                    .Where(p => p.CreatedBy == creatorId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get voting proposals by creator {CreatorId}", creatorId);
                throw;
            }
        }

        public async Task<VotingProposal> CreateProposalAsync(VotingProposal proposal, CancellationToken cancellationToken = default)
        {
            try
            {
                proposal.Id = Guid.NewGuid();
                proposal.CreatedAt = DateTime.UtcNow;

                _context.VotingProposals.Add(proposal);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created voting proposal {Title} with ID {Id}", proposal.Title, proposal.Id);
                return proposal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create voting proposal {Title}", proposal.Title);
                throw;
            }
        }

        public async Task<VotingProposal> UpdateProposalAsync(VotingProposal proposal, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _context.VotingProposals
                    .FirstOrDefaultAsync(p => p.Id == proposal.Id, cancellationToken);

                if (existing == null)
                {
                    throw new InvalidOperationException($"Voting proposal with ID {proposal.Id} not found");
                }

                // Update properties
                existing.Title = proposal.Title;
                existing.Description = proposal.Description;
                existing.Status = proposal.Status;
                existing.StartsAt = proposal.StartsAt;
                existing.EndsAt = proposal.EndsAt;
                existing.MinimumParticipation = proposal.MinimumParticipation;
                existing.RequiredMajority = proposal.RequiredMajority;
                existing.IsActive = proposal.IsActive;
                existing.Metadata = proposal.Metadata;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated voting proposal {Id}", proposal.Id);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update voting proposal {Id}", proposal.Id);
                throw;
            }
        }

        public async Task<Vote> CastVoteAsync(Vote vote, CancellationToken cancellationToken = default)
        {
            try
            {
                vote.Id = Guid.NewGuid();
                vote.CastAt = DateTime.UtcNow;

                _context.Votes.Add(vote);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cast vote for proposal {ProposalId} by voter {VoterId}", 
                    vote.ProposalId, vote.VoterId ?? vote.VoterIdentifier);
                return vote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cast vote for proposal {ProposalId}", vote.ProposalId);
                throw;
            }
        }

        public async Task<IEnumerable<Vote>> GetVotesForProposalAsync(Guid proposalId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Votes
                    .Include(v => v.Option)
                    .Where(v => v.ProposalId == proposalId && v.IsValid)
                    .OrderBy(v => v.CastAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get votes for proposal {ProposalId}", proposalId);
                throw;
            }
        }

        public async Task<VotingResult?> GetResultAsync(Guid proposalId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.VotingResults
                    .Include(r => r.WinningOption)
                    .FirstOrDefaultAsync(r => r.ProposalId == proposalId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get voting result for proposal {ProposalId}", proposalId);
                throw;
            }
        }

        public async Task<VotingResult> SaveResultAsync(VotingResult result, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _context.VotingResults
                    .FirstOrDefaultAsync(r => r.ProposalId == result.ProposalId, cancellationToken);

                if (existing != null)
                {
                    // Update existing result
                    existing.TotalVotes = result.TotalVotes;
                    existing.TotalWeight = result.TotalWeight;
                    existing.ParticipationRate = result.ParticipationRate;
                    existing.WinningOptionId = result.WinningOptionId;
                    existing.CalculatedAt = DateTime.UtcNow;
                    existing.IsFinal = result.IsFinal;
                    existing.Metadata = result.Metadata;
                }
                else
                {
                    // Create new result
                    result.Id = Guid.NewGuid();
                    result.CalculatedAt = DateTime.UtcNow;
                    _context.VotingResults.Add(result);
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Saved voting result for proposal {ProposalId}", result.ProposalId);
                return existing ?? result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save voting result for proposal {ProposalId}", result.ProposalId);
                throw;
            }
        }
    }
}