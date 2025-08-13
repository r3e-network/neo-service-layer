using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.Voting.Domain.Aggregates;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;

namespace NeoServiceLayer.Services.Voting.Queries
{
    // Proposal queries
    public record GetProposalByIdQuery(Guid ProposalId) : IQuery<ProposalDto?>;

    public record GetActiveProposalsQuery(
        int PageNumber = 1,
        int PageSize = 20) : IQuery<PagedResult<ProposalSummaryDto>>;

    public record GetProposalsByStatusQuery(
        ProposalStatus Status,
        int PageNumber = 1,
        int PageSize = 20) : IQuery<PagedResult<ProposalSummaryDto>>;

    public record GetProposalsByCreatorQuery(
        Guid CreatorId,
        int PageNumber = 1,
        int PageSize = 20) : IQuery<PagedResult<ProposalSummaryDto>>;

    public record SearchProposalsQuery(
        string? SearchTerm = null,
        ProposalType? Type = null,
        ProposalStatus? Status = null,
        DateTime? StartDateFrom = null,
        DateTime? StartDateTo = null,
        int PageNumber = 1,
        int PageSize = 20) : IQuery<PagedResult<ProposalSummaryDto>>;

    // Voting queries
    public record GetVoteByVoterQuery(
        Guid ProposalId,
        Guid VoterId) : IQuery<VoteDto?>;

    public record GetProposalVotesQuery(
        Guid ProposalId,
        int PageNumber = 1,
        int PageSize = 100) : IQuery<PagedResult<VoteDto>>;

    public record GetVoterHistoryQuery(
        Guid VoterId,
        int PageNumber = 1,
        int PageSize = 20) : IQuery<PagedResult<VoteHistoryDto>>;

    public record GetLiveResultsQuery(Guid ProposalId) : IQuery<LiveResultsDto>;

    public record GetFinalResultsQuery(Guid ProposalId) : IQuery<FinalResultsDto?>;

    // Statistics queries
    public record GetProposalStatisticsQuery(Guid ProposalId) : IQuery<ProposalStatisticsDto>;

    public record GetVotingStatisticsQuery(
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<VotingStatisticsDto>;

    public record GetParticipationRateQuery(
        Guid ProposalId,
        DateTime? AsOfDate = null) : IQuery<ParticipationRateDto>;

    public record GetVoterTurnoutQuery(
        DateTime StartDate,
        DateTime EndDate) : IQuery<VoterTurnoutDto>;

    // Delegation queries
    public record GetDelegationsQuery(Guid ProposalId) : IQuery<List<DelegationDto>>;

    public record GetDelegatedVotesQuery(
        Guid ProposalId,
        Guid DelegateId) : IQuery<List<DelegatedVoteDto>>;

    // Audit queries
    public record GetProposalAuditLogQuery(
        Guid ProposalId,
        int PageNumber = 1,
        int PageSize = 50) : IQuery<PagedResult<AuditLogDto>>;

    public record GetVoteVerificationQuery(
        Guid ProposalId,
        Guid VoterId,
        string VerificationCode) : IQuery<VoteVerificationDto>;

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
