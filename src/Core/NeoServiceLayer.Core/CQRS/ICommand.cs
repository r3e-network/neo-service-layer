using System;

namespace NeoServiceLayer.Core.CQRS
{
    /// <summary>
    /// Base interface for all commands in the CQRS pattern
    /// Commands represent intent to change system state
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Unique identifier for this command instance
        /// </summary>
        Guid CommandId { get; }
        
        /// <summary>
        /// UTC timestamp when the command was created
        /// </summary>
        DateTime CreatedAt { get; }
        
        /// <summary>
        /// User or system that initiated this command
        /// </summary>
        string InitiatedBy { get; }
        
        /// <summary>
        /// Correlation ID for tracking related operations
        /// </summary>
        Guid CorrelationId { get; }
        
        /// <summary>
        /// Expected version for optimistic concurrency control
        /// </summary>
        long? ExpectedVersion { get; }
    }

    /// <summary>
    /// Command that returns a result
    /// </summary>
    /// <typeparam name="TResult">Type of result returned</typeparam>
    public interface ICommand&lt;out TResult&gt; : ICommand
    {
    }
}