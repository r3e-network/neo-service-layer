using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Service for listening to Neo N3 blockchain events.
    /// </summary>
    public class NeoN3EventListenerService : BackgroundService
    {
        private readonly ILogger<NeoN3EventListenerService> _logger;
        private readonly INeoN3BlockchainService _blockchainService;
        private readonly IEventService _eventService;
        private readonly Dictionary<string, uint> _lastProcessedBlocks;
        private readonly List<ContractEventSubscription> _subscriptions;
        private readonly TimeSpan _pollingInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeoN3EventListenerService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="blockchainService">The blockchain service.</param>
        /// <param name="eventService">The event service.</param>
        public NeoN3EventListenerService(
            ILogger<NeoN3EventListenerService> logger,
            INeoN3BlockchainService blockchainService,
            IEventService eventService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _lastProcessedBlocks = new Dictionary<string, uint>();
            _subscriptions = new List<ContractEventSubscription>();
            _pollingInterval = TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Adds a subscription to a contract event.
        /// </summary>
        /// <param name="scriptHash">The script hash of the contract.</param>
        /// <param name="eventName">The name of the event to subscribe to.</param>
        /// <param name="callbackUrl">The URL to call when the event is detected.</param>
        /// <param name="startBlock">The block to start listening from.</param>
        /// <returns>The subscription ID.</returns>
        public string AddSubscription(string scriptHash, string eventName, string callbackUrl, uint startBlock = 0)
        {
            var subscriptionId = Guid.NewGuid().ToString();

            _subscriptions.Add(new ContractEventSubscription
            {
                Id = subscriptionId,
                ScriptHash = scriptHash,
                EventName = eventName,
                CallbackUrl = callbackUrl,
                StartBlock = startBlock
            });

            if (!_lastProcessedBlocks.ContainsKey(scriptHash))
            {
                _lastProcessedBlocks[scriptHash] = startBlock;
            }
            else
            {
                _lastProcessedBlocks[scriptHash] = Math.Min(_lastProcessedBlocks[scriptHash], startBlock);
            }

            _logger.LogInformation("Added subscription {SubscriptionId} for contract {ScriptHash}, event {EventName}", subscriptionId, scriptHash, eventName);

            return subscriptionId;
        }

        /// <summary>
        /// Removes a subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>True if the subscription was removed, false otherwise.</returns>
        public bool RemoveSubscription(string subscriptionId)
        {
            var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found", subscriptionId);
                return false;
            }

            _subscriptions.Remove(subscription);

            _logger.LogInformation("Removed subscription {SubscriptionId} for contract {ScriptHash}, event {EventName}", subscriptionId, subscription.ScriptHash, subscription.EventName);

            return true;
        }

        /// <inheritdoc/>
        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Neo N3 Event Listener Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollEventsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling events");
                }

                await System.Threading.Tasks.Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("Neo N3 Event Listener Service is stopping");
        }

        private async System.Threading.Tasks.Task PollEventsAsync()
        {
            // Group subscriptions by script hash
            var contractGroups = _subscriptions.GroupBy(s => s.ScriptHash);

            foreach (var group in contractGroups)
            {
                var scriptHash = group.Key;
                var lastProcessedBlock = _lastProcessedBlocks[scriptHash];

                try
                {
                    // Get current blockchain height
                    var heightStr = await _blockchainService.GetBlockchainHeightAsync();
                    var currentHeight = uint.Parse(heightStr);

                    // Skip if no new blocks
                    if (lastProcessedBlock >= currentHeight)
                    {
                        continue;
                    }

                    // Get events for this contract
                    var events = await _blockchainService.GetContractEventsAsync(scriptHash, (int)lastProcessedBlock, 100);

                    if (events.Length > 0)
                    {
                        _logger.LogInformation("Found {Count} events for contract {ScriptHash}", events.Length, scriptHash);

                        // Process events
                        foreach (var evt in events)
                        {
                            // Find matching subscriptions
                            var matchingSubscriptions = group.Where(s =>
                                s.EventName == evt.EventName &&
                                s.StartBlock <= evt.BlockIndex).ToList();

                            foreach (var subscription in matchingSubscriptions)
                            {
                                // Create blockchain event
                                var blockchainEvent = new Core.Models.Event
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Type = Core.Models.EventType.BlockchainEvent,
                                    Source = "Neo N3 Blockchain: " + evt.EventName,
                                    Data = new Dictionary<string, object>
                                    {
                                        ["TxHash"] = evt.TxHash,
                                        ["BlockIndex"] = evt.BlockIndex,
                                        ["ScriptHash"] = scriptHash,
                                        ["State"] = evt.State
                                    },
                                    OccurredAt = DateTime.UtcNow
                                };

                                // Process event
                                await _eventService.ProcessEventAsync(blockchainEvent, subscription.CallbackUrl);
                            }

                            // Update last processed block
                            _lastProcessedBlocks[scriptHash] = Math.Max(_lastProcessedBlocks[scriptHash], evt.BlockIndex);
                        }
                    }
                    else
                    {
                        // If no events, update to current height
                        _lastProcessedBlocks[scriptHash] = currentHeight;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing events for contract {ScriptHash}", scriptHash);
                }
            }
        }

        /// <summary>
        /// Represents a subscription to a contract event.
        /// </summary>
        private class ContractEventSubscription
        {
            /// <summary>
            /// Gets or sets the subscription ID.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Gets or sets the script hash of the contract.
            /// </summary>
            public string ScriptHash { get; set; }

            /// <summary>
            /// Gets or sets the name of the event to subscribe to.
            /// </summary>
            public string EventName { get; set; }

            /// <summary>
            /// Gets or sets the URL to call when the event is detected.
            /// </summary>
            public string CallbackUrl { get; set; }

            /// <summary>
            /// Gets or sets the block to start listening from.
            /// </summary>
            public uint StartBlock { get; set; }
        }
    }
}
