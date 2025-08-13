using System;

namespace NeoServiceLayer.Core.CQRS
{
    /// <summary>
    /// Base implementation for queries
    /// </summary>
    /// <typeparam name="TResult">Type of result returned by the query</typeparam>
    public abstract class QueryBase&lt;TResult&gt; : IQuery&lt;TResult&gt;
    {
        protected QueryBase(
            string initiatedBy,
            Guid? correlationId = null,
            int? timeoutSeconds = null,
            bool allowCached = true)
    {
        if (string.IsNullOrWhiteSpace(initiatedBy))
            throw new ArgumentException("Initiated by cannot be null or empty", nameof(initiatedBy));

        QueryId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        InitiatedBy = initiatedBy;
        CorrelationId = correlationId ?? Guid.NewGuid();
        TimeoutSeconds = timeoutSeconds;
        AllowCached = allowCached;
    }

    public Guid QueryId { get; }
    public DateTime CreatedAt { get; }
    public string InitiatedBy { get; }
    public Guid CorrelationId { get; }
    public int? TimeoutSeconds { get; }
    public bool AllowCached { get; }

    public override string ToString()
    {
        return $"{GetType().Name} - QueryId: {QueryId}, InitiatedBy: {InitiatedBy}, CreatedAt: {CreatedAt:O}";
    }
}
}
