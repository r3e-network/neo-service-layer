using System.Collections.Generic;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Services.Voting.Domain.Aggregates;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting.Domain.Events
{
    // Proposal lifecycle events
    public class ProposalCreatedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public string Title { get; }
        public string Description { get; }
        public ProposalType Type { get; }
        public VotingRules Rules { get; }
        public List<VoteOption> Options { get; }
        public Guid CreatedBy { get; }
        public DateTime VotingStartsAt { get; }
        public DateTime VotingEndsAt { get; }
        public DateTime CreatedAt { get; }

        public ProposalCreatedEvent(Guid proposalId, string title, string description, ProposalType type, VotingRules rules, List<VoteOption> options, Guid createdBy, DateTime votingStartsAt, DateTime votingEndsAt, DateTime createdAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, createdBy.ToString())
        {
            ProposalId = proposalId;
            Title = title;
            Description = description;
            Type = type;
            Rules = rules;
            Options = options;
            CreatedBy = createdBy;
            VotingStartsAt = votingStartsAt;
            VotingEndsAt = votingEndsAt;
            CreatedAt = createdAt;
        }
    }

    public class VotingStartedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public DateTime StartedAt { get; }

        public VotingStartedEvent(Guid proposalId, DateTime startedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, "System")
        {
            ProposalId = proposalId;
            StartedAt = startedAt;
        }
    }

    public class VotingEndedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public VotingResult Result { get; }
        public DateTime EndedAt { get; }

        public VotingEndedEvent(Guid proposalId, VotingResult result, DateTime endedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, "System")
        {
            ProposalId = proposalId;
            Result = result;
            EndedAt = endedAt;
        }
    }

    public class ProposalCancelledEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public string Reason { get; }
        public DateTime CancelledAt { get; }

        public ProposalCancelledEvent(Guid proposalId, string reason, DateTime cancelledAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, "System")
        {
            ProposalId = proposalId;
            Reason = reason;
            CancelledAt = cancelledAt;
        }
    }

    public class VotingPeriodExtendedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public DateTime OldEndTime { get; }
        public DateTime NewEndTime { get; }
        public DateTime ExtendedAt { get; }

        public VotingPeriodExtendedEvent(Guid proposalId, DateTime oldEndTime, DateTime newEndTime, DateTime extendedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, "System")
        {
            ProposalId = proposalId;
            OldEndTime = oldEndTime;
            NewEndTime = newEndTime;
            ExtendedAt = extendedAt;
        }
    }

    // Voting action events
    public class VoteCastEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public Guid OptionId { get; }
        public decimal Weight { get; }
        public DateTime CastAt { get; }

        public VoteCastEvent(Guid proposalId, Guid voterId, Guid optionId, decimal weight, DateTime castAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, voterId.ToString())
        {
            ProposalId = proposalId;
            VoterId = voterId;
            OptionId = optionId;
            Weight = weight;
            CastAt = castAt;
        }
    }

    public class VoteChangedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public Guid OldOptionId { get; }
        public Guid NewOptionId { get; }
        public decimal Weight { get; }
        public DateTime ChangedAt { get; }

        public VoteChangedEvent(Guid proposalId, Guid voterId, Guid oldOptionId, Guid newOptionId, decimal weight, DateTime changedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, voterId.ToString())
        {
            ProposalId = proposalId;
            VoterId = voterId;
            OldOptionId = oldOptionId;
            NewOptionId = newOptionId;
            Weight = weight;
            ChangedAt = changedAt;
        }
    }

    public class VoteWithdrawnEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public DateTime WithdrawnAt { get; }

        public VoteWithdrawnEvent(Guid proposalId, Guid voterId, DateTime withdrawnAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, voterId.ToString())
        {
            ProposalId = proposalId;
            VoterId = voterId;
            WithdrawnAt = withdrawnAt;
        }
    }

    public class VoteDelegatedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid DelegatorId { get; }
        public Guid DelegateId { get; }
        public decimal Weight { get; }
        public DateTime DelegatedAt { get; }

        public VoteDelegatedEvent(Guid proposalId, Guid delegatorId, Guid delegateId, decimal weight, DateTime delegatedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, delegatorId.ToString())
        {
            ProposalId = proposalId;
            DelegatorId = delegatorId;
            DelegateId = delegateId;
            Weight = weight;
            DelegatedAt = delegatedAt;
        }
    }

    public class DelegationRevokedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid DelegatorId { get; }
        public Guid DelegateId { get; }
        public DateTime RevokedAt { get; }

        public DelegationRevokedEvent(Guid proposalId, Guid delegatorId, Guid delegateId, DateTime revokedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, delegatorId.ToString())
        {
            ProposalId = proposalId;
            DelegatorId = delegatorId;
            DelegateId = delegateId;
            RevokedAt = revokedAt;
        }
    }

    // Result events
    public class ProposalApprovedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid WinningOptionId { get; }
        public string WinningOptionName { get; }
        public decimal WinningVoteCount { get; }
        public DateTime ApprovedAt { get; }

        public ProposalApprovedEvent(Guid proposalId, Guid winningOptionId, string winningOptionName, decimal winningVoteCount, DateTime approvedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, "System")
        {
            ProposalId = proposalId;
            WinningOptionId = winningOptionId;
            WinningOptionName = winningOptionName;
            WinningVoteCount = winningVoteCount;
            ApprovedAt = approvedAt;
        }
    }

    public class ProposalRejectedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public string Reason { get; }
        public DateTime RejectedAt { get; }

        public ProposalRejectedEvent(Guid proposalId, string reason, DateTime rejectedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, "System")
        {
            ProposalId = proposalId;
            Reason = reason;
            RejectedAt = rejectedAt;
        }
    }

    public class QuorumNotMetEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public decimal ActualParticipation { get; }
        public decimal RequiredQuorum { get; }
        public DateTime CheckedAt { get; }

        public QuorumNotMetEvent(Guid proposalId, decimal actualParticipation, decimal requiredQuorum, DateTime checkedAt)
            : base(proposalId.ToString(), nameof(QuorumNotMetEvent), 1, "System")
        {
            ProposalId = proposalId;
            ActualParticipation = actualParticipation;
            RequiredQuorum = requiredQuorum;
            CheckedAt = checkedAt;
        }
    }

    // Notification events
    public class VotingReminderSentEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public List<Guid> RecipientIds { get; }
        public DateTime SentAt { get; }

        public VotingReminderSentEvent(Guid proposalId, List<Guid> recipientIds, DateTime sentAt)
            : base(proposalId.ToString(), nameof(VotingReminderSentEvent), 1, "System")
        {
            ProposalId = proposalId;
            RecipientIds = recipientIds;
            SentAt = sentAt;
        }
    }

    public class ProposalCommentAddedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid CommentId { get; }
        public Guid AuthorId { get; }
        public string Comment { get; }
        public DateTime AddedAt { get; }

        public ProposalCommentAddedEvent(Guid proposalId, Guid commentId, Guid authorId, string comment, DateTime addedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, authorId.ToString())
        {
            ProposalId = proposalId;
            CommentId = commentId;
            AuthorId = authorId;
            Comment = comment;
            AddedAt = addedAt;
        }
    }

    // Audit events
    public class ProposalViewedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid ViewerId { get; }
        public DateTime ViewedAt { get; }

        public ProposalViewedEvent(Guid proposalId, Guid viewerId, DateTime viewedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, viewerId.ToString())
        {
            ProposalId = proposalId;
            ViewerId = viewerId;
            ViewedAt = viewedAt;
        }
    }

    public class VoteVerifiedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid VoterId { get; }
        public Guid VerificationId { get; }
        public bool IsValid { get; }
        public DateTime VerifiedAt { get; }

        public VoteVerifiedEvent(Guid proposalId, Guid voterId, Guid verificationId, bool isValid, DateTime verifiedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, "System")
        {
            ProposalId = proposalId;
            VoterId = voterId;
            VerificationId = verificationId;
            IsValid = isValid;
            VerifiedAt = verifiedAt;
        }
    }

    public class ProposalAuditedEvent : DomainEventBase
    {
        public Guid ProposalId { get; }
        public Guid AuditorId { get; }
        public string AuditType { get; }
        public Dictionary<string, object> AuditData { get; }
        public DateTime AuditedAt { get; }

        public ProposalAuditedEvent(Guid proposalId, Guid auditorId, string auditType, Dictionary<string, object> auditData, DateTime auditedAt)
            : base(proposalId.ToString(), nameof(Proposal), 1, auditorId.ToString())
        {
            ProposalId = proposalId;
            AuditorId = auditorId;
            AuditType = auditType;
            AuditData = auditData;
            AuditedAt = auditedAt;
        }
    }
}