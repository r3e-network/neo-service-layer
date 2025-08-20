using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.Voting.Domain.Aggregates;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting.Queries
{
    // Proposal queries
    public class GetProposalByIdQuery : QueryBase<ProposalDto?>
    {
        public Guid ProposalId { get; }
        
        public GetProposalByIdQuery(Guid proposalId) : base("System")
        {
            ProposalId = proposalId;
        }
    }

    public class GetActiveProposalsQuery : QueryBase<PagedResult<ProposalSummaryDto>>
    {
        public int PageNumber { get; }
        public int PageSize { get; }
        
        public GetActiveProposalsQuery(int pageNumber = 1, int pageSize = 20) : base("System")
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class GetProposalsByStatusQuery : QueryBase<PagedResult<ProposalSummaryDto>>
    {
        public ProposalStatus Status { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        
        public GetProposalsByStatusQuery(ProposalStatus status, int pageNumber = 1, int pageSize = 20) : base("System")
        {
            Status = status;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class GetProposalsByCreatorQuery : QueryBase<PagedResult<ProposalSummaryDto>>
    {
        public Guid CreatorId { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        
        public GetProposalsByCreatorQuery(Guid creatorId, int pageNumber = 1, int pageSize = 20) : base("System")
        {
            CreatorId = creatorId;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class SearchProposalsQuery : QueryBase<PagedResult<ProposalSummaryDto>>
    {
        public string? SearchTerm { get; }
        public ProposalType? Type { get; }
        public ProposalStatus? Status { get; }
        public DateTime? StartDateFrom { get; }
        public DateTime? StartDateTo { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        
        public SearchProposalsQuery(string? searchTerm = null, ProposalType? type = null, ProposalStatus? status = null, DateTime? startDateFrom = null, DateTime? startDateTo = null, int pageNumber = 1, int pageSize = 20) : base("System")
        {
            SearchTerm = searchTerm;
            Type = type;
            Status = status;
            StartDateFrom = startDateFrom;
            StartDateTo = startDateTo;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    // Voting queries
    public class GetVoteByVoterQuery : QueryBase<VoteDto?>
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        
        public GetVoteByVoterQuery(Guid proposalId, Guid voterId) : base("System")
        {
            ProposalId = proposalId;
            VoterId = voterId;
        }
    }

    public class GetProposalVotesQuery : QueryBase<PagedResult<VoteDto>>
    {
        public Guid ProposalId { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        
        public GetProposalVotesQuery(Guid proposalId, int pageNumber = 1, int pageSize = 100) : base("System")
        {
            ProposalId = proposalId;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class GetVoterHistoryQuery : QueryBase<PagedResult<VoteHistoryDto>>
    {
        public Guid VoterId { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        
        public GetVoterHistoryQuery(Guid voterId, int pageNumber = 1, int pageSize = 20) : base("System")
        {
            VoterId = voterId;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class GetLiveResultsQuery : QueryBase<LiveResultsDto>
    {
        public Guid ProposalId { get; }
        
        public GetLiveResultsQuery(Guid proposalId) : base("System")
        {
            ProposalId = proposalId;
        }
    }

    public class GetFinalResultsQuery : QueryBase<FinalResultsDto?>
    {
        public Guid ProposalId { get; }
        
        public GetFinalResultsQuery(Guid proposalId) : base("System")
        {
            ProposalId = proposalId;
        }
    }

    // Statistics queries
    public class GetProposalStatisticsQuery : QueryBase<ProposalStatisticsDto>
    {
        public Guid ProposalId { get; }
        
        public GetProposalStatisticsQuery(Guid proposalId) : base("System")
        {
            ProposalId = proposalId;
        }
    }

    public class GetVotingStatisticsQuery : QueryBase<VotingStatisticsDto>
    {
        public DateTime? StartDate { get; }
        public DateTime? EndDate { get; }
        
        public GetVotingStatisticsQuery(DateTime? startDate = null, DateTime? endDate = null) : base("System")
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    public class GetParticipationRateQuery : QueryBase<ParticipationRateDto>
    {
        public Guid ProposalId { get; }
        public DateTime? AsOfDate { get; }
        
        public GetParticipationRateQuery(Guid proposalId, DateTime? asOfDate = null) : base("System")
        {
            ProposalId = proposalId;
            AsOfDate = asOfDate;
        }
    }

    public class GetVoterTurnoutQuery : QueryBase<VoterTurnoutDto>
    {
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        
        public GetVoterTurnoutQuery(DateTime startDate, DateTime endDate) : base("System")
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    // Delegation queries
    public class GetDelegationsQuery : QueryBase<List<DelegationDto>>
    {
        public Guid ProposalId { get; }
        
        public GetDelegationsQuery(Guid proposalId) : base("System")
        {
            ProposalId = proposalId;
        }
    }

    public class GetDelegatedVotesQuery : QueryBase<List<DelegatedVoteDto>>
    {
        public Guid ProposalId { get; }
        public Guid DelegateId { get; }
        
        public GetDelegatedVotesQuery(Guid proposalId, Guid delegateId) : base("System")
        {
            ProposalId = proposalId;
            DelegateId = delegateId;
        }
    }

    // Audit queries
    public class GetProposalAuditLogQuery : QueryBase<PagedResult<AuditLogDto>>
    {
        public Guid ProposalId { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        
        public GetProposalAuditLogQuery(Guid proposalId, int pageNumber = 1, int pageSize = 50) : base("System")
        {
            ProposalId = proposalId;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class GetVoteVerificationQuery : QueryBase<VoteVerificationDto>
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public string VerificationCode { get; }
        
        public GetVoteVerificationQuery(Guid proposalId, Guid voterId, string verificationCode) : base("System")
        {
            ProposalId = proposalId;
            VoterId = voterId;
            VerificationCode = verificationCode;
        }
    }

    // DTOs
    public record ProposalDto(
        Guid Id,
        string Title,
        string Description,
        ProposalType Type,
        VotingRules Rules,
        List<VoteOptionDto> Options,
        ProposalStatus Status,
        Guid CreatedBy,
        string CreatorName,
        DateTime CreatedAt,
        DateTime VotingStartsAt,
        DateTime VotingEndsAt,
        DateTime? ClosedAt,
        int TotalVotes,
        VotingResult? Result);

    public record ProposalSummaryDto(
        Guid Id,
        string Title,
        ProposalType Type,
        ProposalStatus Status,
        DateTime VotingStartsAt,
        DateTime VotingEndsAt,
        int TotalVotes,
        decimal ParticipationRate);

    public record VoteOptionDto(
        Guid Id,
        string Name,
        string Description,
        int DisplayOrder,
        int VoteCount,
        decimal VotePercentage);

    public record VoteDto(
        Guid ProposalId,
        Guid VoterId,
        string VoterName,
        Guid OptionId,
        string OptionName,
        decimal Weight,
        DateTime CastAt,
        bool IsVerified);

    public record VoteHistoryDto(
        Guid ProposalId,
        string ProposalTitle,
        Guid OptionId,
        string OptionName,
        DateTime VotedAt,
        ProposalStatus ProposalStatus,
        bool WasWinningVote);

    public record LiveResultsDto(
        Guid ProposalId,
        int TotalVotes,
        decimal TotalWeight,
        List<OptionResultDto> Options,
        Guid? CurrentLeader,
        decimal ParticipationRate,
        DateTime LastUpdated);

    public record FinalResultsDto(
        Guid ProposalId,
        VotingResult Result,
        Guid? WinningOptionId,
        string? WinningOptionName,
        decimal WinningVoteCount,
        decimal WinningPercentage,
        bool QuorumMet,
        DateTime FinalizedAt);

    public record OptionResultDto(
        Guid OptionId,
        string Name,
        int VoteCount,
        decimal VoteWeight,
        decimal Percentage,
        bool IsLeading);

    public record ProposalStatisticsDto(
        Guid ProposalId,
        int TotalEligibleVoters,
        int ActualVoters,
        decimal ParticipationRate,
        int UniqueVoters,
        int VoteChanges,
        int VoteWithdrawals,
        Dictionary<string, int> VotesByHour,
        Dictionary<Guid, int> VotesByOption);

    public record VotingStatisticsDto(
        int TotalProposals,
        int ActiveProposals,
        int CompletedProposals,
        int TotalVotesCast,
        int UniqueVoters,
        decimal AverageParticipationRate,
        Dictionary<ProposalType, int> ProposalsByType,
        Dictionary<DateTime, int> ProposalsByDay);

    public record ParticipationRateDto(
        Guid ProposalId,
        int EligibleVoters,
        int ActualVoters,
        decimal Rate,
        DateTime CalculatedAt);

    public record VoterTurnoutDto(
        int TotalEligibleVoters,
        int ActiveVoters,
        decimal TurnoutRate,
        Dictionary<DateTime, int> DailyTurnout,
        List<Guid> MostActiveVoters);

    public record DelegationDto(
        Guid DelegatorId,
        string DelegatorName,
        Guid DelegateId,
        string DelegateName,
        decimal Weight,
        DateTime DelegatedAt);

    public record DelegatedVoteDto(
        Guid DelegatorId,
        string DelegatorName,
        Guid OptionId,
        string OptionName,
        decimal Weight,
        DateTime CastAt);

    public record AuditLogDto(
        Guid Id,
        Guid ProposalId,
        Guid? UserId,
        string Action,
        Dictionary<string, object> Data,
        DateTime Timestamp);

    public record VoteVerificationDto(
        bool IsValid,
        Guid? ProposalId,
        Guid? VoterId,
        Guid? OptionId,
        DateTime? CastAt,
        string VerificationHash,
        string Message);

    public record PagedResult<T>(
        List<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages)
    {
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }
}
