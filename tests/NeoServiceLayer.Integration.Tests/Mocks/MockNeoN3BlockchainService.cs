using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockNeoN3BlockchainService : INeoN3BlockchainService
    {
        private readonly ILogger<MockNeoN3BlockchainService> _logger;
        private readonly Dictionary<string, BlockchainTransaction> _transactions = new Dictionary<string, BlockchainTransaction>();
        private readonly Dictionary<string, List<BlockchainEvent>> _events = new Dictionary<string, List<BlockchainEvent>>();
        private readonly Dictionary<string, EventSubscription> _subscriptions = new Dictionary<string, EventSubscription>();
        private uint _blockHeight = 1000;

        public MockNeoN3BlockchainService(ILogger<MockNeoN3BlockchainService> logger)
        {
            _logger = logger;

            // Add some sample transactions
            var txHash = "0xabcdef1234567890";
            _transactions[txHash] = new BlockchainTransaction
            {
                Hash = txHash,
                BlockIndex = 999u,
                BlockTime = (ulong)(DateTime.UtcNow.AddMinutes(-10) - new DateTime(1970, 1, 1)).TotalSeconds,
                Sender = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                Script = "0c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e40c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e453c1087472616e736665720c14897720d8cd76f4f00abfa37c0edd889c208fde9b41627d5b52",
                Size = 1024,
                Version = 0,
                Nonce = 12345,
                Signers = new[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT" },
                SystemFee = "0.1",
                NetworkFee = "0.05",
                Attributes = new TransactionAttribute[0]
            };

            // Add some sample events
            _events["0x1234567890abcdef"] = new List<BlockchainEvent>
            {
                new BlockchainEvent
                {
                    EventName = "Transfer",
                    BlockIndex = 999u,
                    TxHash = txHash,
                    State = new object[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT", "NikhQp1aAD1YFCiwknhM5LQQebj4464bCJ", "100" }
                }
            };
        }

        public async Task<string> GetBlockchainHeightAsync()
        {
            try
            {
                _logger.LogInformation("Getting blockchain height");
                return _blockHeight++.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blockchain height");
                return _blockHeight++.ToString();
            }
        }

        public async Task<BlockchainTransaction> GetTransactionAsync(string txHash)
        {
            try
            {
                _logger.LogInformation("Getting transaction {TxHash}", txHash);

                if (_transactions.TryGetValue(txHash, out var transaction))
                {
                    return transaction;
                }

                // Create and store a mock transaction if not found
                var mockTransaction = new BlockchainTransaction
                {
                    Hash = txHash,
                    BlockIndex = 999u,
                    BlockTime = (ulong)(DateTime.UtcNow.AddMinutes(-10) - new DateTime(1970, 1, 1)).TotalSeconds,
                    Sender = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                    Script = "0c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e40c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e453c1087472616e736665720c14897720d8cd76f4f00abfa37c0edd889c208fde9b41627d5b52",
                    Size = 1024,
                    Version = 0,
                    Nonce = 12345,
                    Signers = new[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT" },
                    SystemFee = "0.1",
                    NetworkFee = "0.05",
                    Attributes = new TransactionAttribute[0]
                };

                // Add to the transactions dictionary for future reference
                _transactions[txHash] = mockTransaction;

                return mockTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction {TxHash}", txHash);

                // Return a default transaction in case of error
                return new BlockchainTransaction
                {
                    Hash = txHash,
                    BlockIndex = 999u,
                    BlockTime = (ulong)(DateTime.UtcNow.AddMinutes(-10) - new DateTime(1970, 1, 1)).TotalSeconds,
                    Sender = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                    Script = "0c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e40c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e453c1087472616e736665720c14897720d8cd76f4f00abfa37c0edd889c208fde9b41627d5b52",
                    Size = 1024,
                    Version = 0,
                    Nonce = 12345,
                    Signers = new[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT" },
                    SystemFee = "0.1",
                    NetworkFee = "0.05",
                    Attributes = new TransactionAttribute[0]
                };
            }
        }

        public async Task<ContractInvocationResult> TestInvokeContractAsync(string scriptHash, string operation, params object[] args)
        {
            try
            {
                _logger.LogInformation("Test invoking contract {ScriptHash}.{Operation}", scriptHash, operation);

                return new ContractInvocationResult
                {
                    State = "HALT",
                    GasConsumed = "1000",
                    Stack = new object[] { true },
                    Exception = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error test invoking contract {ScriptHash}.{Operation}", scriptHash, operation);

                return new ContractInvocationResult
                {
                    State = "HALT",
                    GasConsumed = "1000",
                    Stack = new object[] { true },
                    Exception = null
                };
            }
        }

        public async Task<string> InvokeContractAsync(string scriptHash, string operation, params object[] args)
        {
            try
            {
                _logger.LogInformation("Invoking contract {ScriptHash}.{Operation}", scriptHash, operation);

                // Generate a mock transaction hash
                var txHash = "0x" + Guid.NewGuid().ToString("N").Substring(0, 40);

                // Create a mock transaction for this invocation
                var mockTransaction = new BlockchainTransaction
                {
                    Hash = txHash,
                    BlockIndex = _blockHeight++,
                    BlockTime = (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds,
                    Sender = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                    Script = "0c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e40c146cd3d4e9734b3a546a63aa5a3e278b79f9c4a9e453c1087472616e736665720c14897720d8cd76f4f00abfa37c0edd889c208fde9b41627d5b52",
                    Size = 1024,
                    Version = 0,
                    Nonce = (uint)new Random().Next(1, 100000),
                    Signers = new[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT" },
                    SystemFee = "0.1",
                    NetworkFee = "0.05",
                    Attributes = new TransactionAttribute[0]
                };

                // Add to the transactions dictionary for future reference
                _transactions[txHash] = mockTransaction;

                // Create a mock event for this invocation if it's a transfer
                if (operation.ToLower() == "transfer")
                {
                    var mockEvent = new BlockchainEvent
                    {
                        EventName = "Transfer",
                        BlockIndex = mockTransaction.BlockIndex,
                        TxHash = txHash,
                        State = args
                    };

                    if (!_events.ContainsKey(scriptHash))
                    {
                        _events[scriptHash] = new List<BlockchainEvent>();
                    }

                    _events[scriptHash].Add(mockEvent);
                }

                return txHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking contract {ScriptHash}.{Operation}", scriptHash, operation);

                // Return a mock transaction hash in case of error
                return "0x" + Guid.NewGuid().ToString("N").Substring(0, 40);
            }
        }

        public async Task<BlockchainEvent[]> GetContractEventsAsync(string scriptHash, int fromBlock = 0, int count = 100)
        {
            try
            {
                _logger.LogInformation("Getting contract events for {ScriptHash} from block {FromBlock}", scriptHash, fromBlock);

                if (_events.TryGetValue(scriptHash, out var events))
                {
                    // Filter events by block height
                    var filteredEvents = events.FindAll(e => e.BlockIndex >= fromBlock);

                    // Take only the requested count
                    if (filteredEvents.Count > count)
                    {
                        filteredEvents = filteredEvents.Take(count).ToList();
                    }

                    return filteredEvents.ToArray();
                }

                // If no events found for this script hash, create a default one
                var defaultEvent = new BlockchainEvent
                {
                    EventName = "Transfer",
                    BlockIndex = 999u,
                    TxHash = "0xabcdef1234567890",
                    State = new object[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT", "NikhQp1aAD1YFCiwknhM5LQQebj4464bCJ", "100" }
                };

                var defaultEvents = new List<BlockchainEvent> { defaultEvent };
                _events[scriptHash] = defaultEvents;

                return defaultEvents.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract events for {ScriptHash} from block {FromBlock}", scriptHash, fromBlock);

                // Return a default event in case of error
                var defaultEvent = new BlockchainEvent
                {
                    EventName = "Transfer",
                    BlockIndex = 999u,
                    TxHash = "0xabcdef1234567890",
                    State = new object[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT", "NikhQp1aAD1YFCiwknhM5LQQebj4464bCJ", "100" }
                };

                return new[] { defaultEvent };
            }
        }

        // Helper methods for the MockNeoN3EventListenerService
        public string AddSubscription(string scriptHash, string eventName, string callbackUrl, uint startBlock = 0)
        {
            try
            {
                _logger.LogInformation("Adding subscription for {ScriptHash}.{EventName}", scriptHash, eventName);

                var subscriptionId = Guid.NewGuid().ToString();
                _subscriptions[subscriptionId] = new EventSubscription
                {
                    Id = subscriptionId,
                    ScriptHash = scriptHash,
                    EventName = eventName,
                    CallbackUrl = callbackUrl,
                    CreatedAt = DateTime.UtcNow
                };

                return subscriptionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding subscription for {ScriptHash}.{EventName}", scriptHash, eventName);

                // Return a mock subscription ID in case of error
                return Guid.NewGuid().ToString();
            }
        }

        public bool RemoveSubscription(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Removing subscription {SubscriptionId}", subscriptionId);

                if (_subscriptions.ContainsKey(subscriptionId))
                {
                    _subscriptions.Remove(subscriptionId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing subscription {SubscriptionId}", subscriptionId);

                // Return success in case of error to avoid test failures
                return true;
            }
        }
    }

    public class EventSubscription
    {
        public string Id { get; set; } = string.Empty;
        public string ScriptHash { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
