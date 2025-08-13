using System;

namespace NeoServiceLayer.Core.CQRS
{
    /// <summary>
    /// Base implementation for commands
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        protected CommandBase(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
        {
            if (string.IsNullOrWhiteSpace(initiatedBy))
                throw new ArgumentException("Initiated by cannot be null or empty", nameof(initiatedBy));

            CommandId = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            InitiatedBy = initiatedBy;
            CorrelationId = correlationId ?? Guid.NewGuid();
            ExpectedVersion = expectedVersion;
        }

        public Guid CommandId { get; }
        public DateTime CreatedAt { get; }
        public string InitiatedBy { get; }
        public Guid CorrelationId { get; }
        public long? ExpectedVersion { get; }

        public override string ToString()
        {
            return $"{GetType().Name} - CommandId: {CommandId}, InitiatedBy: {InitiatedBy}, CreatedAt: {CreatedAt:O}";
        }
    }

    /// <summary>
    /// Base implementation for commands that return results
    /// </summary>
    /// <typeparam name="TResult">Type of result returned</typeparam>
    public abstract class CommandBase&lt;TResult&gt; : CommandBase, ICommand&lt;TResult&gt;
    {
        protected CommandBase(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
    {
    }
}
}
