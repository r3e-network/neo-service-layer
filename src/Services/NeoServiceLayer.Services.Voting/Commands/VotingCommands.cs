using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.Voting.Domain.Aggregates;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;

namespace NeoServiceLayer.Services.Voting.Commands
{
    // Proposal management commands
    public record CreateProposalCommand(
        string Title,
        string Description,
        ProposalType Type,
        VotingRules Rules,
        List<VoteOption> Options,
        Guid CreatedBy,
        DateTime VotingStartsAt,
        DateTime VotingEndsAt) : ICommand<Guid>;

    public record StartVotingCommand(Guid ProposalId) : ICommand;

    public record EndVotingCommand(Guid ProposalId) : ICommand<VotingResult>;

    public record CancelProposalCommand(
        Guid ProposalId,
        string Reason) : ICommand;

    public record ExtendVotingPeriodCommand(
        Guid ProposalId,
        DateTime NewEndTime) : ICommand;

    // Voting commands
    public record CastVoteCommand(
        Guid ProposalId,
        Guid VoterId,
        Guid OptionId,
        decimal Weight = 1.0m) : ICommand;

    public record ChangeVoteCommand(
        Guid ProposalId,
        Guid VoterId,
        Guid NewOptionId,
        decimal Weight = 1.0m) : ICommand;

    public record WithdrawVoteCommand(
        Guid ProposalId,
        Guid VoterId) : ICommand;

    public record DelegateVoteCommand(
        Guid ProposalId,
        Guid DelegatorId,
        Guid DelegateId,
        decimal Weight = 1.0m) : ICommand;

    public record RevokeDelegationCommand(
        Guid ProposalId,
        Guid DelegatorId,
        Guid DelegateId) : ICommand;

    // Batch voting commands
    public record CastMultipleVotesCommand(
        Guid ProposalId,
        List<VoteInput> Votes) : ICommand<int>;

    public record VoteInput(
        Guid VoterId,
        Guid OptionId,
        decimal Weight = 1.0m);

    // Administrative commands
    public record InvalidateVoteCommand(
        Guid ProposalId,
        Guid VoterId,
        string Reason) : ICommand;

    public record RecalculateResultsCommand(Guid ProposalId) : ICommand<VotingResult>;

    public record AuditProposalCommand(
        Guid ProposalId,
        Guid AuditorId,
        string AuditType,
        Dictionary<string, object> AuditData) : ICommand;

    // Notification commands
    public record SendVotingReminderCommand(
        Guid ProposalId,
        List<Guid> RecipientIds) : ICommand;

    public record AddProposalCommentCommand(
        Guid ProposalId,
        Guid AuthorId,
        string Comment) : ICommand<Guid>;

    // Query support commands
    public record RecordProposalViewCommand(
        Guid ProposalId,
        Guid ViewerId) : ICommand;

    public record VerifyVoteCommand(
        Guid ProposalId,
        Guid VoterId) : ICommand<VoteVerificationResult>;

    // Command results
    public record VoteVerificationResult(
        bool IsValid,
        Guid? OptionId,
        decimal? Weight,
        DateTime? CastAt,
        string? VerificationHash);
}