using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Events
{
    /// <summary>
    /// Interface for handling domain events
    /// </summary>
    /// <typeparam name="TEvent">Type of event to handle</typeparam>
    public interface IEventHandler&lt;in TEvent&gt; where TEvent : IDomainEvent
    {
        /// <summary>
        /// Handles the specified domain event
        /// </summary>
        /// <param name="domainEvent">The domain event to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for event handler with failure handling
/// </summary>
/// <typeparam name="TEvent">Type of event to handle</typeparam>
public interface IEventHandlerWithRetry&lt;in TEvent & gt; : IEventHandler & lt; TEvent & gt; where TEvent : IDomainEvent
    {
        /// <summary>
        /// Handles event processing failure
        /// </summary>
        /// <param name="domainEvent">The event that failed to process</param>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="attemptNumber">Current attempt number (starting from 1)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True to retry, false to give up</returns>
        Task&lt; bool&gt; HandleFailureAsync(
            TEvent domainEvent,
            Exception exception,
            int attemptNumber,
            CancellationToken cancellationToken = default);
    }
}
