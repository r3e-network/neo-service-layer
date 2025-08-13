using System;

namespace NeoServiceLayer.Services.Voting.Domain.ValueObjects
{
    public class VoteOption
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int DisplayOrder { get; }

        public VoteOption(Guid id, string name, string description, int displayOrder = 0)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Option ID cannot be empty", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Option name is required", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Option description is required", nameof(description));

            Id = id;
            Name = name;
            Description = description;
            DisplayOrder = displayOrder;
        }

        public static VoteOption Create(string name, string description, int displayOrder = 0)
        {
            return new VoteOption(Guid.NewGuid(), name, description, displayOrder);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not VoteOption other)
                return false;

            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name}: {Description}";
        }
    }
}
