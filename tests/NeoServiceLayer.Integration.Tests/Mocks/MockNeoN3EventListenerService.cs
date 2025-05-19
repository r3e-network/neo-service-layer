using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    /// <summary>
    /// Mock service for listening to Neo N3 blockchain events.
    /// </summary>
    public class MockNeoN3EventListenerService : NeoN3EventListenerService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockNeoN3EventListenerService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="blockchainService">The blockchain service.</param>
        /// <param name="eventService">The event service.</param>
        public MockNeoN3EventListenerService(
            ILogger<NeoN3EventListenerService> logger,
            INeoN3BlockchainService blockchainService,
            IEventService eventService)
            : base(logger, blockchainService, eventService)
        {
            // Store the blockchain service for direct access
            _blockchainService = blockchainService;
        }

        // Store the blockchain service for direct access
        private readonly INeoN3BlockchainService _blockchainService;

        /// <summary>
        /// Adds a subscription to a contract event.
        /// </summary>
        /// <param name="scriptHash">The script hash of the contract.</param>
        /// <param name="eventName">The name of the event to subscribe to.</param>
        /// <param name="callbackUrl">The URL to call when the event is detected.</param>
        /// <param name="startBlock">The block to start listening from.</param>
        /// <returns>The subscription ID.</returns>
        public new string AddSubscription(string scriptHash, string eventName, string callbackUrl, uint startBlock = 0)
        {
            // Log that we're adding a subscription in simulation mode
            var logger = this.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(this) as ILogger;
            logger?.LogInformation("Adding subscription in simulation mode for contract {ScriptHash}, event {EventName}", scriptHash, eventName);

            // Use the blockchain service to add the subscription if it's a MockNeoN3BlockchainService
            if (_blockchainService is MockNeoN3BlockchainService mockBlockchainService)
            {
                return mockBlockchainService.AddSubscription(scriptHash, eventName, callbackUrl, startBlock);
            }

            // Fallback to generating a subscription ID
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Removes a subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>True if the subscription was removed, false otherwise.</returns>
        public new bool RemoveSubscription(string subscriptionId)
        {
            // Log that we're removing a subscription in simulation mode
            var logger = this.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(this) as ILogger;
            logger?.LogInformation("Removing subscription in simulation mode: {SubscriptionId}", subscriptionId);

            // Use the blockchain service to remove the subscription if it's a MockNeoN3BlockchainService
            if (_blockchainService is MockNeoN3BlockchainService mockBlockchainService)
            {
                return mockBlockchainService.RemoveSubscription(subscriptionId);
            }

            // Fallback to returning true
            return true;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Log that we're running in simulation mode
            var logger = this.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(this) as ILogger;
            logger?.LogInformation("Mock Neo N3 Event Listener Service is running in simulation mode");

            // In simulation mode, we don't actually poll for events
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

    }
}
