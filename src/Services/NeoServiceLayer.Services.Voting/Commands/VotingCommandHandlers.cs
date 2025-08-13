using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.EventSourcing;
using NeoServiceLayer.Services.Voting.Domain.Aggregates;
using NeoServiceLayer.Services.Voting.Domain.ValueObjects;
using NeoServiceLayer.Services.Voting.Services;

namespace NeoServiceLayer.Services.Voting.Commands
{
    public class VotingCommandHandlers :
        ICommandHandler<CreateProposalCommand, Guid>,
        ICommandHandler<StartVotingCommand>,
        ICommandHandler<EndVotingCommand, VotingResult>,
        ICommandHandler<CancelProposalCommand>,
        ICommandHandler<ExtendVotingPeriodCommand>,
        ICommandHandler<CastVoteCommand>,
        ICommandHandler<ChangeVoteCommand>,
        ICommandHandler<WithdrawVoteCommand>,
        ICommandHandler<DelegateVoteCommand>,
        ICommandHandler<RevokeDelegationCommand>,
        ICommandHandler<CastMultipleVotesCommand, int>,
        ICommandHandler<InvalidateVoteCommand>,
        ICommandHandler<RecalculateResultsCommand, VotingResult>,
        ICommandHandler<AuditProposalCommand>,
        ICommandHandler<SendVotingReminderCommand>,
        ICommandHandler<AddProposalCommentCommand, Guid>,
        ICommandHandler<RecordProposalViewCommand>,
        ICommandHandler<VerifyVoteCommand, VoteVerificationResult>
    {
        private readonly IEventStore _eventStore;
        private readonly IVotingService _votingService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<VotingCommandHandlers> _logger;

        public VotingCommandHandlers(
            IEventStore eventStore,
            IVotingService votingService,
            INotificationService notificationService,
            ILogger<VotingCommandHandlers> logger)
        {
            _eventStore = eventStore;
            _votingService = votingService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Guid> HandleAsync(CreateProposalCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating proposal: {Title}", command.Title);

            // Validate voter eligibility
            if (!await _votingService.IsEligibleToCreateProposalAsync(command.CreatedBy))
            {
                throw new UnauthorizedAccessException("User is not eligible to create proposals");
            }

            // Create the proposal
            var proposal = Proposal.Create(
                command.Title,
                command.Description,
                command.Type,
                command.Rules,
                command.Options,
                command.CreatedBy,
                command.VotingStartsAt,
                command.VotingEndsAt);

            // Save to event store
            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);

            // Send notifications
            await _notificationService.NotifyProposalCreatedAsync(proposal.Id, command.Title);

            _logger.LogInformation("Proposal {ProposalId} created successfully", proposal.Id);
            return proposal.Id;
        }

        public async Task HandleAsync(StartVotingCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting voting for proposal {ProposalId}", command.ProposalId);

            var proposal = await LoadProposalAsync(command.ProposalId);
            proposal.StartVoting();

            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);

            await _notificationService.NotifyVotingStartedAsync(proposal.Id);
        }

        public async Task<VotingResult> HandleAsync(EndVotingCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Ending voting for proposal {ProposalId}", command.ProposalId);

            var proposal = await LoadProposalAsync(command.ProposalId);
            proposal.EndVoting();

            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);

            await _notificationService.NotifyVotingEndedAsync(proposal.Id, proposal.Result!);

            return proposal.Result!;
        }

        public async Task HandleAsync(CancelProposalCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cancelling proposal {ProposalId}", command.ProposalId);

            var proposal = await LoadProposalAsync(command.ProposalId);
            proposal.CancelProposal(command.Reason);

            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);

            await _notificationService.NotifyProposalCancelledAsync(proposal.Id, command.Reason);
        }

        public async Task HandleAsync(ExtendVotingPeriodCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Extending voting period for proposal {ProposalId} to {NewEndTime}",
                command.ProposalId, command.NewEndTime);

            var proposal = await LoadProposalAsync(command.ProposalId);
            proposal.ExtendVotingPeriod(command.NewEndTime);

            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);

            await _notificationService.NotifyVotingPeriodExtendedAsync(proposal.Id, command.NewEndTime);
        }

        public async Task HandleAsync(CastVoteCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Casting vote for proposal {ProposalId} by voter {VoterId}",
                command.ProposalId, command.VoterId);

            // Validate voter eligibility
            if (!await _votingService.IsEligibleToVoteAsync(command.VoterId, command.ProposalId))
            {
                throw new UnauthorizedAccessException("User is not eligible to vote on this proposal");
            }

            // Get voter weight if weighted voting
            var weight = await _votingService.GetVoterWeightAsync(command.VoterId, command.ProposalId);
            if (weight <= 0)
            {
                weight = command.Weight;
            }

            var proposal = await LoadProposalAsync(command.ProposalId);
            proposal.CastVote(command.VoterId, command.OptionId, weight);

            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);

            _logger.LogInformation("Vote cast successfully for proposal {ProposalId}", command.ProposalId);
        }

        public async Task HandleAsync(ChangeVoteCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Changing vote for proposal {ProposalId} by voter {VoterId}",
                command.ProposalId, command.VoterId);

            var proposal = await LoadProposalAsync(command.ProposalId);

            // Get current vote to change
            if (!proposal.Votes.ContainsKey(command.VoterId))
            {
                throw new InvalidOperationException("No existing vote found to change");
            }

            proposal.CastVote(command.VoterId, command.NewOptionId, command.Weight);

            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);
        }

        public async Task HandleAsync(WithdrawVoteCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Withdrawing vote for proposal {ProposalId} by voter {VoterId}",
                command.ProposalId, command.VoterId);

            var proposal = await LoadProposalAsync(command.ProposalId);
            proposal.WithdrawVote(command.VoterId);

            await _eventStore.SaveEventsAsync(
                proposal.Id,
                proposal.GetUncommittedEvents(),
                proposal.Version,
                cancellationToken);
        }

        public async Task HandleAsync(DelegateVoteCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Delegating vote for proposal {ProposalId} from {DelegatorId} to {DelegateId}",
                command.ProposalId, command.DelegatorId, command.DelegateId);

            // Validate delegation is allowed
            var proposal = await LoadProposalAsync(command.ProposalId);
            if (!proposal.Rules.AllowDelegation)
            {
                throw new InvalidOperationException("Vote delegation is not allowed for this proposal");
            }

            // Record delegation in service
            await _votingService.RecordDelegationAsync(
                command.ProposalId,
                command.DelegatorId,
                command.DelegateId,
                command.Weight);

            _logger.LogInformation("Vote delegation recorded successfully");
        }

        public async Task HandleAsync(RevokeDelegationCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Revoking delegation for proposal {ProposalId} from {DelegatorId}",
                command.ProposalId, command.DelegatorId);

            await _votingService.RevokeDelegationAsync(
                command.ProposalId,
                command.DelegatorId,
                command.DelegateId);

            _logger.LogInformation("Vote delegation revoked successfully");
        }

        public async Task<int> HandleAsync(CastMultipleVotesCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Casting {Count} votes for proposal {ProposalId}",
                command.Votes.Count, command.ProposalId);

            var proposal = await LoadProposalAsync(command.ProposalId);
            var successCount = 0;

            foreach (var vote in command.Votes)
            {
                try
                {
                    if (await _votingService.IsEligibleToVoteAsync(vote.VoterId, command.ProposalId))
                    {
                        var weight = await _votingService.GetVoterWeightAsync(vote.VoterId, command.ProposalId);
                        if (weight <= 0) weight = vote.Weight;

                        proposal.CastVote(vote.VoterId, vote.OptionId, weight);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cast vote for voter {VoterId}", vote.VoterId);
                }
            }

            if (successCount > 0)
            {
                await _eventStore.SaveEventsAsync(
                    proposal.Id,
                    proposal.GetUncommittedEvents(),
                    proposal.Version,
                    cancellationToken);
            }

            _logger.LogInformation("Successfully cast {SuccessCount} out of {TotalCount} votes",
                successCount, command.Votes.Count);
            return successCount;
        }

        public async Task HandleAsync(InvalidateVoteCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Invalidating vote for proposal {ProposalId} by voter {VoterId}",
                command.ProposalId, command.VoterId);

            var proposal = await LoadProposalAsync(command.ProposalId);

            // Remove the vote if it exists
            if (proposal.Votes.ContainsKey(command.VoterId))
            {
                proposal.WithdrawVote(command.VoterId);

                await _eventStore.SaveEventsAsync(
                    proposal.Id,
                    proposal.GetUncommittedEvents(),
                    proposal.Version,
                    cancellationToken);
            }

            // Record invalidation reason
            await _votingService.RecordVoteInvalidationAsync(
                command.ProposalId,
                command.VoterId,
                command.Reason);
        }

        public async Task<VotingResult> HandleAsync(RecalculateResultsCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Recalculating results for proposal {ProposalId}", command.ProposalId);

            var proposal = await LoadProposalAsync(command.ProposalId);

            // Force recalculation by ending voting again if already ended
            if (proposal.Status == ProposalStatus.Completed)
            {
                // This will recalculate and update the result
                proposal.EndVoting();

                await _eventStore.SaveEventsAsync(
                    proposal.Id,
                    proposal.GetUncommittedEvents(),
                    proposal.Version,
                    cancellationToken);
            }

            return proposal.Result!;
        }

        public async Task HandleAsync(AuditProposalCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Auditing proposal {ProposalId} by {AuditorId}",
                command.ProposalId, command.AuditorId);

            await _votingService.RecordAuditAsync(
                command.ProposalId,
                command.AuditorId,
                command.AuditType,
                command.AuditData);
        }

        public async Task HandleAsync(SendVotingReminderCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending voting reminder for proposal {ProposalId} to {Count} recipients",
                command.ProposalId, command.RecipientIds.Count);

            await _notificationService.SendVotingReminderAsync(command.ProposalId, command.RecipientIds);
        }

        public async Task<Guid> HandleAsync(AddProposalCommentCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Adding comment to proposal {ProposalId} by {AuthorId}",
                command.ProposalId, command.AuthorId);

            var commentId = await _votingService.AddCommentAsync(
                command.ProposalId,
                command.AuthorId,
                command.Comment);

            return commentId;
        }

        public async Task HandleAsync(RecordProposalViewCommand command, CancellationToken cancellationToken = default)
        {
            await _votingService.RecordViewAsync(command.ProposalId, command.ViewerId);
        }

        public async Task<VoteVerificationResult> HandleAsync(VerifyVoteCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Verifying vote for proposal {ProposalId} by voter {VoterId}",
                command.ProposalId, command.VoterId);

            var proposal = await LoadProposalAsync(command.ProposalId);

            if (proposal.Votes.TryGetValue(command.VoterId, out var vote))
            {
                var verificationHash = _votingService.GenerateVoteVerificationHash(
                    command.ProposalId,
                    command.VoterId,
                    vote.OptionId,
                    vote.CastAt);

                return new VoteVerificationResult(
                    true,
                    vote.OptionId,
                    vote.Weight,
                    vote.CastAt,
                    verificationHash);
            }

            return new VoteVerificationResult(false, null, null, null, null);
        }

        private async Task<Proposal> LoadProposalAsync(Guid proposalId)
        {
            var events = await _eventStore.GetEventsAsync(proposalId);
            if (!events.Any())
            {
                throw new InvalidOperationException($"Proposal {proposalId} not found");
            }

            var proposal = new Proposal();
            proposal.LoadFromHistory(events);
            return proposal;
        }
    }
}
