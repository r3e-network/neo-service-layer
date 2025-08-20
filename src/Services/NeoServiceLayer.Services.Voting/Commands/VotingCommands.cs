using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.Voting.Domain.Aggregates;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting.Commands
{
    // Proposal management commands
    public class CreateProposalCommand : CommandBase<Guid>
    {
        public string Title { get; }
        public string Description { get; }
        public ProposalType Type { get; }
        public VotingRules Rules { get; }
        public List<VoteOption> Options { get; }
        public Guid CreatedBy { get; }
        public DateTime VotingStartsAt { get; }
        public DateTime VotingEndsAt { get; }

        public CreateProposalCommand(string title, string description, ProposalType type, VotingRules rules, 
            List<VoteOption> options, Guid createdBy, DateTime votingStartsAt, DateTime votingEndsAt, 
            string initiatedBy, Guid? correlationId = null) : base(initiatedBy, correlationId)
        {
            Title = title;
            Description = description;
            Type = type;
            Rules = rules;
            Options = options;
            CreatedBy = createdBy;
            VotingStartsAt = votingStartsAt;
            VotingEndsAt = votingEndsAt;
        }
    }

    public class StartVotingCommand : CommandBase
    {
        public Guid ProposalId { get; }

        public StartVotingCommand(Guid proposalId, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
        }
    }

    public class EndVotingCommand : CommandBase<VotingResult>
    {
        public Guid ProposalId { get; }

        public EndVotingCommand(Guid proposalId, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
        }
    }

    public class CancelProposalCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public string Reason { get; }

        public CancelProposalCommand(Guid proposalId, string reason, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            Reason = reason;
        }
    }

    public class ExtendVotingPeriodCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public DateTime NewEndTime { get; }

        public ExtendVotingPeriodCommand(Guid proposalId, DateTime newEndTime, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            NewEndTime = newEndTime;
        }
    }

    // Voting commands
    public class CastVoteCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public Guid OptionId { get; }
        public decimal Weight { get; }

        public CastVoteCommand(Guid proposalId, Guid voterId, Guid optionId, decimal weight, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            VoterId = voterId;
            OptionId = optionId;
            Weight = weight;
        }
    }

    public class ChangeVoteCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public Guid NewOptionId { get; }
        public decimal Weight { get; }

        public ChangeVoteCommand(Guid proposalId, Guid voterId, Guid newOptionId, decimal weight, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            VoterId = voterId;
            NewOptionId = newOptionId;
            Weight = weight;
        }
    }

    public class WithdrawVoteCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }

        public WithdrawVoteCommand(Guid proposalId, Guid voterId, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            VoterId = voterId;
        }
    }

    public class DelegateVoteCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid DelegatorId { get; }
        public Guid DelegateId { get; }
        public decimal Weight { get; }

        public DelegateVoteCommand(Guid proposalId, Guid delegatorId, Guid delegateId, decimal weight, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            DelegatorId = delegatorId;
            DelegateId = delegateId;
            Weight = weight;
        }
    }

    public class RevokeDelegationCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid DelegatorId { get; }
        public Guid DelegateId { get; }

        public RevokeDelegationCommand(Guid proposalId, Guid delegatorId, Guid delegateId, string initiatedBy, Guid? correlationId = null) 
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            DelegatorId = delegatorId;
            DelegateId = delegateId;
        }
    }

    // Batch voting commands
    public class CastMultipleVotesCommand : CommandBase<int>
    {
        public Guid ProposalId { get; }
        public List<VoteInput> Votes { get; }

        public CastMultipleVotesCommand(Guid proposalId, List<VoteInput> votes, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            Votes = votes;
        }
    }

    public class VoteInput
    {
        public Guid VoterId { get; }
        public Guid OptionId { get; }
        public decimal Weight { get; }

        public VoteInput(Guid voterId, Guid optionId, decimal weight = 1.0m)
        {
            VoterId = voterId;
            OptionId = optionId;
            Weight = weight;
        }
    }

    // Administrative commands
    public class InvalidateVoteCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public string Reason { get; }

        public InvalidateVoteCommand(Guid proposalId, Guid voterId, string reason, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            VoterId = voterId;
            Reason = reason;
        }
    }

    public class RecalculateResultsCommand : CommandBase<VotingResult>
    {
        public Guid ProposalId { get; }

        public RecalculateResultsCommand(Guid proposalId, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
        }
    }

    public class AuditProposalCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid AuditorId { get; }
        public string AuditType { get; }
        public Dictionary<string, object> AuditData { get; }

        public AuditProposalCommand(Guid proposalId, Guid auditorId, string auditType, Dictionary<string, object> auditData, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            AuditorId = auditorId;
            AuditType = auditType;
            AuditData = auditData;
        }
    }

    // Notification commands
    public class SendVotingReminderCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public List<Guid> RecipientIds { get; }

        public SendVotingReminderCommand(Guid proposalId, List<Guid> recipientIds, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            RecipientIds = recipientIds;
        }
    }

    public class AddProposalCommentCommand : CommandBase<Guid>
    {
        public Guid ProposalId { get; }
        public Guid AuthorId { get; }
        public string Comment { get; }

        public AddProposalCommentCommand(Guid proposalId, Guid authorId, string comment, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            AuthorId = authorId;
            Comment = comment;
        }
    }

    // Query support commands
    public class RecordProposalViewCommand : CommandBase
    {
        public Guid ProposalId { get; }
        public Guid ViewerId { get; }

        public RecordProposalViewCommand(Guid proposalId, Guid viewerId, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            ViewerId = viewerId;
        }
    }

    public class VerifyVoteCommand : CommandBase<VoteVerificationResult>
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }

        public VerifyVoteCommand(Guid proposalId, Guid voterId, string initiatedBy, Guid? correlationId = null)
            : base(initiatedBy, correlationId)
        {
            ProposalId = proposalId;
            VoterId = voterId;
        }
    }

    // Command results
    public record VoteVerificationResult(
        bool IsValid,
        Guid? OptionId,
        decimal? Weight,
        DateTime? CastAt,
        string? VerificationHash);
}
