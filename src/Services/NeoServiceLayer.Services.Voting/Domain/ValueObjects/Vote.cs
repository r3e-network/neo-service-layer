using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting.Domain.ValueObjects
{
    public class Vote
    {
        public Guid VoterId { get; }
        public Guid OptionId { get; }
        public decimal Weight { get; }
        public DateTime CastAt { get; }

        public Vote(Guid voterId, Guid optionId, decimal weight, DateTime castAt)
        {
            if (voterId == Guid.Empty)
                throw new ArgumentException("Voter ID cannot be empty", nameof(voterId));

            if (optionId == Guid.Empty)
                throw new ArgumentException("Option ID cannot be empty", nameof(optionId));

            if (weight <= 0)
                throw new ArgumentException("Vote weight must be positive", nameof(weight));

            VoterId = voterId;
            OptionId = optionId;
            Weight = weight;
            CastAt = castAt;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Vote other)
                return false;

            return VoterId == other.VoterId &&
                   OptionId == other.OptionId &&
                   Weight == other.Weight;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VoterId, OptionId, Weight);
        }
    }
}
