using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoServiceLayer.Services.Voting.Domain.ValueObjects
{
    public class VotingResult
    {
        public int TotalVoters { get; }
        public decimal TotalVoteWeight { get; }
        public Dictionary<Guid, decimal> VotesByOption { get; }
        public Guid? WinningOptionId { get; }
        public decimal WinningVoteCount { get; }
        public bool QuorumMet { get; }
        public decimal ParticipationRate { get; }

        public VotingResult(
            int totalVoters,
            decimal totalVoteWeight,
            Dictionary<Guid, decimal> votesByOption,
            Guid? winningOptionId,
            decimal winningVoteCount,
            bool quorumMet)
        {
            TotalVoters = totalVoters;
            TotalVoteWeight = totalVoteWeight;
            VotesByOption = new Dictionary<Guid, decimal>(votesByOption);
            WinningOptionId = winningOptionId;
            WinningVoteCount = winningVoteCount;
            QuorumMet = quorumMet;
            ParticipationRate = totalVoteWeight > 0 ? (decimal)totalVoters / totalVoteWeight * 100 : 0;
        }

        public decimal GetVotePercentage(Guid optionId)
        {
            if (TotalVoteWeight == 0)
                return 0;

            return VotesByOption.TryGetValue(optionId, out var votes) 
                ? (votes / TotalVoteWeight) * 100 
                : 0;
        }

        public List<(Guid OptionId, decimal VoteCount, decimal Percentage)> GetRankedResults()
        {
            return VotesByOption
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => (kvp.Key, kvp.Value, GetVotePercentage(kvp.Key)))
                .ToList();
        }

        public bool HasMajority(Guid optionId)
        {
            return GetVotePercentage(optionId) > 50;
        }

        public bool HasSuperMajority(Guid optionId, decimal threshold)
        {
            return GetVotePercentage(optionId) >= threshold;
        }

        public override string ToString()
        {
            var winner = WinningOptionId.HasValue 
                ? $"Winner: {WinningOptionId} with {WinningVoteCount:F2} votes ({GetVotePercentage(WinningOptionId.Value):F1}%)" 
                : "No winner";
            
            return $"Voting Result: {TotalVoters} voters, {TotalVoteWeight:F2} total weight, {winner}, Quorum: {(QuorumMet ? "Met" : "Not Met")}";
        }
    }
}