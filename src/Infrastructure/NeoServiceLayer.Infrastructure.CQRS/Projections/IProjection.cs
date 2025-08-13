using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Events;

namespace NeoServiceLayer.Infrastructure.CQRS.Projections
{
    /// <summary>
    /// Interface for projections that build read models from events
    /// </summary>
    public interface IProjection
    {
        /// <summary>
        /// Name of the projection
        /// </summary>
        string ProjectionName { get; }

        /// <summary>
        /// Handles an event and updates the read model
        /// </summary>
        /// <param name="domainEvent">Event to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current position/checkpoint of the projection
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current position or 0 if not started</returns>
        Task<long> GetPositionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the current position/checkpoint of the projection
        /// </summary>
        /// <param name="position">Position to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SavePositionAsync(long position, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the projection to rebuild from the beginning
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ResetAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Base class for projections
    /// </summary>
    public abstract class ProjectionBase : IProjection
    {
        protected ProjectionBase(string projectionName)
        {
            if (string.IsNullOrWhiteSpace(projectionName))
                throw new ArgumentException("Projection name cannot be null or empty", nameof(projectionName));
            
            ProjectionName = projectionName;
        }

        public string ProjectionName { get; }

        public abstract Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

        public abstract Task<long> GetPositionAsync(CancellationToken cancellationToken = default);

        public abstract Task SavePositionAsync(long position, CancellationToken cancellationToken = default);

        public abstract Task ResetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if this projection handles a specific event type
        /// </summary>
        protected virtual bool CanHandle(IDomainEvent domainEvent)
        {
            return GetHandledEventTypes().Contains(domainEvent.GetType());
        }

        /// <summary>
        /// Gets the event types this projection handles
        /// </summary>
        protected abstract Type[] GetHandledEventTypes();
    }
}