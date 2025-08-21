using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Domain
{
    /// <summary>
    /// Interface for publishing domain events
    /// </summary>
    public interface IDomainEventPublisher
    {
        /// <summary>
        /// Publishes a domain event
        /// </summary>
        /// <param name="domainEvent">The domain event to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes multiple domain events
        /// </summary>
        /// <param name="domainEvents">The domain events to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the operation</returns>
        Task PublishManyAsync(IDomainEvent[] domainEvents, CancellationToken cancellationToken = default);
    }
}