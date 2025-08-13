using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.Voting.Services
{
    public interface IVotingService
    {
        // Eligibility checks
        Task<bool> IsEligibleToVoteAsync(Guid voterId, Guid proposalId);
        Task<bool> IsEligibleToCreateProposalAsync(Guid userId);
        Task<decimal> GetVoterWeightAsync(Guid voterId, Guid proposalId);
        
        // Delegation management
        Task RecordDelegationAsync(Guid proposalId, Guid delegatorId, Guid delegateId, decimal weight);
        Task RevokeDelegationAsync(Guid proposalId, Guid delegatorId, Guid delegateId);
        Task<List<Guid>> GetDelegatedVotersAsync(Guid proposalId, Guid delegateId);
        
        // Vote verification
        string GenerateVoteVerificationHash(Guid proposalId, Guid voterId, Guid optionId, DateTime castAt);
        Task<bool> VerifyVoteAsync(Guid proposalId, Guid voterId, string verificationHash);
        
        // Audit and compliance
        Task RecordAuditAsync(Guid proposalId, Guid auditorId, string auditType, Dictionary<string, object> auditData);
        Task RecordVoteInvalidationAsync(Guid proposalId, Guid voterId, string reason);
        Task RecordViewAsync(Guid proposalId, Guid viewerId);
        
        // Comments and discussion
        Task<Guid> AddCommentAsync(Guid proposalId, Guid authorId, string comment);
        Task<List<CommentDto>> GetCommentsAsync(Guid proposalId);
        
        // Statistics and analytics
        Task<int> GetEligibleVoterCountAsync(Guid proposalId);
        Task<decimal> CalculateQuorumAsync(Guid proposalId);
        Task<Dictionary<DateTime, int>> GetVotingTrendAsync(Guid proposalId);
    }

    public interface INotificationService
    {
        Task NotifyProposalCreatedAsync(Guid proposalId, string title);
        Task NotifyVotingStartedAsync(Guid proposalId);
        Task NotifyVotingEndedAsync(Guid proposalId, Domain.ValueObjects.VotingResult result);
        Task NotifyProposalCancelledAsync(Guid proposalId, string reason);
        Task NotifyVotingPeriodExtendedAsync(Guid proposalId, DateTime newEndTime);
        Task SendVotingReminderAsync(Guid proposalId, List<Guid> recipientIds);
        Task NotifyVoteCastAsync(Guid proposalId, Guid voterId);
        Task NotifyDelegationAsync(Guid proposalId, Guid delegatorId, Guid delegateId);
    }

    public record CommentDto(
        Guid Id,
        Guid ProposalId,
        Guid AuthorId,
        string AuthorName,
        string Comment,
        DateTime CreatedAt,
        DateTime? EditedAt);
}