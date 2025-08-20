using System.Collections.Generic;
using System.Linq;
using NeoServiceLayer.Core.Aggregates;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Services.Voting.Domain.Events;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;
using NeoServiceLayer.ServiceFramework;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting.Domain.Aggregates
{
    public class Proposal : AggregateRoot
    {
        private readonly Dictionary<Guid, Vote> _votes = new();
        private readonly List<VoteOption> _options = new();
        private VotingResult? _result;

        public string Title { get; private set; }
        public string Description { get; private set; }
        public ProposalType Type { get; private set; }
        public VotingRules Rules { get; private set; }
        public ProposalStatus Status { get; private set; }
        public Guid CreatedBy { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime VotingStartsAt { get; private set; }
        public DateTime VotingEndsAt { get; private set; }
        public DateTime? ClosedAt { get; private set; }
        public string? CancellationReason { get; private set; }

        public IReadOnlyList<VoteOption> Options => _options.AsReadOnly();
        public IReadOnlyDictionary<Guid, Vote> Votes => _votes;
        public VotingResult? Result => _result;
        public int TotalVotes => _votes.Count;
        public bool IsVotingOpen => Status == ProposalStatus.Active &&
                                    DateTime.UtcNow >= VotingStartsAt &&
                                    DateTime.UtcNow <= VotingEndsAt;

        // For Event Sourcing reconstruction
        private Proposal() : base()
        {
            Title = string.Empty;
            Description = string.Empty;
        }

    private void Apply(object domainEvent)
    {
        // Apply domain event to aggregate
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
        
        // Handle specific event types
        switch (domainEvent)
        {
            case ProposalCreatedEvent created:
                Id = created.ProposalId.ToString();
                Title = created.Title;
                break;
            case VoteCastEvent vote:
                // Update vote counts
                break;
            default:
                // Log unknown event type
                break;
        }
    }

        // Factory method for creating new proposals
        public static Proposal Create(
            string title,
            string description,
            ProposalType type,
            VotingRules rules,
            List<VoteOption> options,
            Guid createdBy,
            DateTime votingStartsAt,
            DateTime votingEndsAt)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required", nameof(title));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required", nameof(description));

            if (options == null || options.Count < 2)
                throw new ArgumentException("At least 2 options are required", nameof(options));

            if (votingStartsAt >= votingEndsAt)
                throw new ArgumentException("Voting end time must be after start time");

            if (votingEndsAt <= DateTime.UtcNow)
                throw new ArgumentException("Voting end time must be in the future");

            var proposalId = Guid.NewGuid();
            var proposal = new Proposal();

            var @event = new ProposalCreatedEvent(
                proposalId,
                title,
                description,
                type,
                rules,
                options,
                createdBy,
                votingStartsAt,
                votingEndsAt,
                DateTime.UtcNow);

            proposal.RaiseEvent(@event);

            // Auto-start if voting period has begun
            if (votingStartsAt <= DateTime.UtcNow)
            {
                proposal.RaiseEvent(new VotingStartedEvent(proposalId, DateTime.UtcNow));
            }

            return proposal;
        }

        public void StartVoting()
        {
            if (Status != ProposalStatus.Draft)
                throw new InvalidOperationException($"Cannot start voting - proposal status is {Status}");

            if (DateTime.UtcNow < VotingStartsAt)
                throw new InvalidOperationException("Cannot start voting before scheduled start time");

            RaiseEvent(new VotingStartedEvent(Guid.Parse(Id), DateTime.UtcNow));
        }

        public void CastVote(Guid voterId, Guid optionId, decimal weight = 1.0m)
        {
            if (!IsVotingOpen)
                throw new InvalidOperationException("Voting is not currently open");

            var option = _options.FirstOrDefault(o => o.Id == optionId);
            if (option == null)
                throw new ArgumentException("Invalid option ID", nameof(optionId));

            // Check if user has already voted
            if (_votes.ContainsKey(voterId))
            {
                if (!Rules.AllowVoteChange)
                    throw new InvalidOperationException("Vote changes are not allowed for this proposal");

                var existingVote = _votes[voterId];
                RaiseEvent(new VoteChangedEvent(
                    Guid.Parse(Id),
                    voterId,
                    existingVote.OptionId,
                    optionId,
                    weight,
                    DateTime.UtcNow));
            }
            else
            {
                // Check if voter meets minimum weight requirement
                if (weight < Rules.MinimumVoteWeight)
                    throw new InvalidOperationException($"Vote weight must be at least {Rules.MinimumVoteWeight}");

                RaiseEvent(new VoteCastEvent(
                    Guid.Parse(Id),
                    voterId,
                    optionId,
                    weight,
                    DateTime.UtcNow));
            }
        }

        public void WithdrawVote(Guid voterId)
        {
            if (!IsVotingOpen)
                throw new InvalidOperationException("Voting is not currently open");

            if (!Rules.AllowVoteWithdrawal)
                throw new InvalidOperationException("Vote withdrawal is not allowed for this proposal");

            if (!_votes.ContainsKey(voterId))
                throw new InvalidOperationException("No vote found to withdraw");

            RaiseEvent(new VoteWithdrawnEvent(Guid.Parse(Id), voterId, DateTime.UtcNow));
        }

        public void EndVoting()
        {
            if (Status != ProposalStatus.Active)
                throw new InvalidOperationException($"Cannot end voting - proposal status is {Status}");

            // Calculate results
            var result = CalculateResult();

            RaiseEvent(new VotingEndedEvent(Guid.Parse(Id), result, DateTime.UtcNow));

            // Determine winner if applicable
            if (result.WinningOptionId.HasValue)
            {
                var winningOption = _options.First(o => o.Id == result.WinningOptionId.Value);
                RaiseEvent(new ProposalApprovedEvent(
                    Guid.Parse(Id),
                    result.WinningOptionId.Value,
                    winningOption.Name,
                    result.WinningVoteCount,
                    DateTime.UtcNow));
            }
            else
            {
                RaiseEvent(new ProposalRejectedEvent(
                    Guid.Parse(Id),
                    "No option reached required threshold",
                    DateTime.UtcNow));
            }
        }

        public void CancelProposal(string reason)
        {
            if (Status == ProposalStatus.Completed || Status == ProposalStatus.Cancelled)
                throw new InvalidOperationException($"Cannot cancel proposal - status is {Status}");

            RaiseEvent(new ProposalCancelledEvent(Guid.Parse(Id), reason, DateTime.UtcNow));
        }

        public void ExtendVotingPeriod(DateTime newEndTime)
        {
            if (Status != ProposalStatus.Active)
                throw new InvalidOperationException("Can only extend active proposals");

            if (newEndTime <= VotingEndsAt)
                throw new ArgumentException("New end time must be after current end time");

            if (newEndTime <= DateTime.UtcNow)
                throw new ArgumentException("New end time must be in the future");

            RaiseEvent(new VotingPeriodExtendedEvent(
                Guid.Parse(Id),
                VotingEndsAt,
                newEndTime,
                DateTime.UtcNow));
        }

        private VotingResult CalculateResult()
        {
            var voteCounts = new Dictionary<Guid, decimal>();
            foreach (var option in _options)
            {
                voteCounts[option.Id] = 0;
            }

            // Count votes with weights
            foreach (var vote in _votes.Values)
            {
                voteCounts[vote.OptionId] += vote.Weight;
            }

            // Calculate total votes
            var totalVoteWeight = voteCounts.Values.Sum();

            // Check if quorum is met
            var quorumMet = Rules.RequiredQuorum == 0 ||
                           (totalVoteWeight >= Rules.RequiredQuorum);

            // Find winning option based on voting type
            Guid? winningOptionId = null;
            decimal winningVoteCount = 0;

            if (quorumMet && totalVoteWeight > 0)
            {
                switch (Type)
                {
                    case ProposalType.SimpleMajority:
                        var topOption = voteCounts.OrderByDescending(kvp => kvp.Value).First();
                        var topVotePercentage = (topOption.Value / totalVoteWeight) * 100;
                        if (topVotePercentage > 50)
                        {
                            winningOptionId = topOption.Key;
                            winningVoteCount = topOption.Value;
                        }
                        break;

                    case ProposalType.SuperMajority:
                        var superOption = voteCounts.OrderByDescending(kvp => kvp.Value).First();
                        var superVotePercentage = (superOption.Value / totalVoteWeight) * 100;
                        if (superVotePercentage >= Rules.SuperMajorityThreshold)
                        {
                            winningOptionId = superOption.Key;
                            winningVoteCount = superOption.Value;
                        }
                        break;

                    case ProposalType.Plurality:
                        var pluralityWinner = voteCounts.OrderByDescending(kvp => kvp.Value).First();
                        winningOptionId = pluralityWinner.Key;
                        winningVoteCount = pluralityWinner.Value;
                        break;

                    case ProposalType.Unanimous:
                        var unanimousOption = voteCounts.FirstOrDefault(kvp => kvp.Value == totalVoteWeight);
                        if (unanimousOption.Key != Guid.Empty)
                        {
                            winningOptionId = unanimousOption.Key;
                            winningVoteCount = unanimousOption.Value;
                        }
                        break;
                }
            }

            return new VotingResult(
                TotalVotes,
                totalVoteWeight,
                voteCounts,
                winningOptionId,
                winningVoteCount,
                quorumMet);
        }

        // Event handlers
        private void When(object @event)
        {
            switch (@event)
            {
                case ProposalCreatedEvent e:
                    Id = e.ProposalId.ToString();
                    Title = e.Title;
                    Description = e.Description;
                    Type = e.Type;
                    Rules = e.Rules;
                    _options.AddRange(e.Options);
                    CreatedBy = e.CreatedBy;
                    VotingStartsAt = e.VotingStartsAt;
                    VotingEndsAt = e.VotingEndsAt;
                    CreatedAt = e.CreatedAt;
                    Status = ProposalStatus.Draft;
                    break;

                case VotingStartedEvent e:
                    Status = ProposalStatus.Active;
                    break;

                case VoteCastEvent e:
                    _votes[e.VoterId] = new Vote(e.VoterId, e.OptionId, e.Weight, e.CastAt);
                    break;

                case VoteChangedEvent e:
                    _votes[e.VoterId] = new Vote(e.VoterId, e.NewOptionId, e.Weight, e.ChangedAt);
                    break;

                case VoteWithdrawnEvent e:
                    _votes.Remove(e.VoterId);
                    break;

                case VotingEndedEvent e:
                    Status = ProposalStatus.Completed;
                    ClosedAt = e.EndedAt;
                    _result = e.Result;
                    break;

                case ProposalCancelledEvent e:
                    Status = ProposalStatus.Cancelled;
                    CancellationReason = e.Reason;
                    ClosedAt = e.CancelledAt;
                    break;

                case VotingPeriodExtendedEvent e:
                    VotingEndsAt = e.NewEndTime;
                    break;

                case ProposalApprovedEvent approved:
                case ProposalRejectedEvent rejected:
                    // These are informational events, no state change needed
                    break;
            }
        }

        protected override void RegisterEventHandlers()
        {
            // Register event handlers for domain events
            // This method is called during aggregate initialization
        }

        protected override void ValidateInvariants()
        {
            // Validate business rules and invariants
            if (string.IsNullOrWhiteSpace(Title))
                throw new InvalidOperationException("Proposal title cannot be empty");

            if (VotingStartsAt >= VotingEndsAt)
                throw new InvalidOperationException("Voting start time must be before end time");

            if (_options.Count < 2)
                throw new InvalidOperationException("Proposal must have at least 2 voting options");

            if (Type == ProposalType.SuperMajority && Rules.SuperMajorityThreshold <= 50)
                throw new InvalidOperationException("Super majority threshold must be greater than 50%");
        }

        /// <summary>
        /// Loads a proposal from event history.
        /// </summary>
        public static Proposal LoadFromHistory(IEnumerable<IDomainEvent> events)
        {
            var proposal = new Proposal();
            foreach (var domainEvent in events)
            {
                proposal.Apply(domainEvent);
            }
            proposal.MarkEventsAsCommitted();
            return proposal;
        }
    }

    public enum ProposalStatus
    {
        Draft,
        Active,
        Completed,
        Cancelled
    }

    public enum ProposalType
    {
        SimpleMajority,    // >50% required
        SuperMajority,     // Custom threshold (e.g., 66%, 75%)
        Plurality,         // Most votes wins
        Unanimous          // 100% agreement required
    }
}
