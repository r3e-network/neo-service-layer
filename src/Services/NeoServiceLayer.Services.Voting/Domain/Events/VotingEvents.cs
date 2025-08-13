using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Services.Voting.Domain.Aggregates;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;

namespace NeoServiceLayer.Services.Voting.Domain.Events
{
    // Proposal lifecycle events
    public record ProposalCreatedEvent(
        Guid ProposalId,
        string Title,
        string Description,
        ProposalType Type,
        VotingRules Rules,
        List<VoteOption> Options,
        Guid CreatedBy,
        DateTime VotingStartsAt,
        DateTime VotingEndsAt,
        DateTime CreatedAt) : DomainEvent;

    public record VotingStartedEvent(
        Guid ProposalId,
        DateTime StartedAt) : DomainEvent;

    public record VotingEndedEvent(
        Guid ProposalId,
        VotingResult Result,
        DateTime EndedAt) : DomainEvent;

    public record ProposalCancelledEvent(
        Guid ProposalId,
        string Reason,
        DateTime CancelledAt) : DomainEvent;

    public record VotingPeriodExtendedEvent(
        Guid ProposalId,
        DateTime OldEndTime,
        DateTime NewEndTime,
        DateTime ExtendedAt) : DomainEvent;

    // Voting action events
    public record VoteCastEvent(
        Guid ProposalId,
        Guid VoterId,
        Guid OptionId,
        decimal Weight,
        DateTime CastAt) : DomainEvent;

    public record VoteChangedEvent(
        Guid ProposalId,
        Guid VoterId,
        Guid OldOptionId,
        Guid NewOptionId,
        decimal Weight,
        DateTime ChangedAt) : DomainEvent;

    public record VoteWithdrawnEvent(
        Guid ProposalId,
        Guid VoterId,
        DateTime WithdrawnAt) : DomainEvent;

    public record VoteDelegatedEvent(
        Guid ProposalId,
        Guid DelegatorId,
        Guid DelegateId,
        decimal Weight,
        DateTime DelegatedAt) : DomainEvent;

    public record DelegationRevokedEvent(
        Guid ProposalId,
        Guid DelegatorId,
        Guid DelegateId,
        DateTime RevokedAt) : DomainEvent;

    // Result events
    public record ProposalApprovedEvent(
        Guid ProposalId,
        Guid WinningOptionId,
        string WinningOptionName,
        decimal WinningVoteCount,
        DateTime ApprovedAt) : DomainEvent;

    public record ProposalRejectedEvent(
        Guid ProposalId,
        string Reason,
        DateTime RejectedAt) : DomainEvent;

    public record QuorumNotMetEvent(
        Guid ProposalId,
        decimal ActualParticipation,
        decimal RequiredQuorum,
        DateTime CheckedAt) : DomainEvent;

    // Notification events
    public record VotingReminderSentEvent(
        Guid ProposalId,
        List<Guid> RecipientIds,
        DateTime SentAt) : DomainEvent;

    public record ProposalCommentAddedEvent(
        Guid ProposalId,
        Guid CommentId,
        Guid AuthorId,
        string Comment,
        DateTime AddedAt) : DomainEvent;

    // Audit events
    public record ProposalViewedEvent(
        Guid ProposalId,
        Guid ViewerId,
        DateTime ViewedAt) : DomainEvent;

    public record VoteVerifiedEvent(
        Guid ProposalId,
        Guid VoterId,
        Guid VerificationId,
        bool IsValid,
        DateTime VerifiedAt) : DomainEvent;

    public record ProposalAuditedEvent(
        Guid ProposalId,
        Guid AuditorId,
        string AuditType,
        Dictionary<string, object> AuditData,
        DateTime AuditedAt) : DomainEvent;
}
