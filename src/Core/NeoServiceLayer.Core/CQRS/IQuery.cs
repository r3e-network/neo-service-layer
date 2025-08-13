using System;

namespace NeoServiceLayer.Core.CQRS
{
    /// <summary>
    /// Base interface for all queries in the CQRS pattern
    /// Queries represent intent to read system state without modification
    /// </summary>
    /// <typeparam name="TResult">Type of result returned by the query</typeparam>
    public interface IQuery&lt;out TResult&gt;
    {
        /// <summary>
        /// Unique identifier for this query instance
        /// </summary>
        Guid QueryId { get; }
        
        /// <summary>
        /// UTC timestamp when the query was created
        /// </summary>
        DateTime CreatedAt { get; }
        
        /// <summary>
        /// User or system that initiated this query
        /// </summary>
        string InitiatedBy { get; }
        
        /// <summary>
        /// Correlation ID for tracking related operations
        /// </summary>
        Guid CorrelationId { get; }
        
        /// <summary>
        /// Maximum time to wait for query results (in seconds)
        /// </summary>
        int? TimeoutSeconds { get; }
        
        /// <summary>
        /// Whether to use cached results if available
        /// </summary>
        bool AllowCached { get; }
    }
}