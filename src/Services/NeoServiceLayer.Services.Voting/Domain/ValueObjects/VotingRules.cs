using System;

namespace NeoServiceLayer.Services.Voting.Domain.ValueObjects
{
    public class VotingRules
    {
        public bool AllowVoteChange { get; }
        public bool AllowVoteWithdrawal { get; }
        public bool RequireAuthentication { get; }
        public bool AnonymousVoting { get; }
        public decimal MinimumVoteWeight { get; }
        public decimal RequiredQuorum { get; }
        public decimal SuperMajorityThreshold { get; }
        public bool AllowDelegation { get; }
        public bool ShowResultsDuringVoting { get; }
        public int MaxOptionsPerVoter { get; }

        public VotingRules(
            bool allowVoteChange = true,
            bool allowVoteWithdrawal = false,
            bool requireAuthentication = true,
            bool anonymousVoting = false,
            decimal minimumVoteWeight = 1.0m,
            decimal requiredQuorum = 0,
            decimal superMajorityThreshold = 66.67m,
            bool allowDelegation = false,
            bool showResultsDuringVoting = true,
            int maxOptionsPerVoter = 1)
        {
            if (minimumVoteWeight <= 0)
                throw new ArgumentException("Minimum vote weight must be positive", nameof(minimumVoteWeight));
            
            if (requiredQuorum < 0 || requiredQuorum > 100)
                throw new ArgumentException("Required quorum must be between 0 and 100", nameof(requiredQuorum));
            
            if (superMajorityThreshold < 50 || superMajorityThreshold > 100)
                throw new ArgumentException("Super majority threshold must be between 50 and 100", nameof(superMajorityThreshold));
            
            if (maxOptionsPerVoter < 1)
                throw new ArgumentException("Max options per voter must be at least 1", nameof(maxOptionsPerVoter));

            AllowVoteChange = allowVoteChange;
            AllowVoteWithdrawal = allowVoteWithdrawal;
            RequireAuthentication = requireAuthentication;
            AnonymousVoting = anonymousVoting;
            MinimumVoteWeight = minimumVoteWeight;
            RequiredQuorum = requiredQuorum;
            SuperMajorityThreshold = superMajorityThreshold;
            AllowDelegation = allowDelegation;
            ShowResultsDuringVoting = showResultsDuringVoting;
            MaxOptionsPerVoter = maxOptionsPerVoter;
        }

        public static VotingRules Default()
        {
            return new VotingRules();
        }

        public static VotingRules SimpleMajority()
        {
            return new VotingRules(
                allowVoteChange: true,
                allowVoteWithdrawal: false,
                requireAuthentication: true,
                requiredQuorum: 0,
                superMajorityThreshold: 50.01m);
        }

        public static VotingRules SuperMajority(decimal threshold = 66.67m)
        {
            return new VotingRules(
                allowVoteChange: true,
                allowVoteWithdrawal: false,
                requireAuthentication: true,
                requiredQuorum: 50,
                superMajorityThreshold: threshold);
        }

        public static VotingRules Unanimous()
        {
            return new VotingRules(
                allowVoteChange: true,
                allowVoteWithdrawal: false,
                requireAuthentication: true,
                requiredQuorum: 100,
                superMajorityThreshold: 100);
        }

        public static VotingRules Anonymous()
        {
            return new VotingRules(
                allowVoteChange: false,
                allowVoteWithdrawal: false,
                requireAuthentication: false,
                anonymousVoting: true,
                showResultsDuringVoting: false);
        }

        public static VotingRules Weighted(decimal minimumWeight = 1.0m)
        {
            return new VotingRules(
                allowVoteChange: true,
                minimumVoteWeight: minimumWeight,
                allowDelegation: true);
        }

        public override string ToString()
        {
            return $"Voting Rules: " +
                   $"Change={AllowVoteChange}, " +
                   $"Withdrawal={AllowVoteWithdrawal}, " +
                   $"Anonymous={AnonymousVoting}, " +
                   $"Quorum={RequiredQuorum}%, " +
                   $"SuperMajority={SuperMajorityThreshold}%";
        }
    }
}