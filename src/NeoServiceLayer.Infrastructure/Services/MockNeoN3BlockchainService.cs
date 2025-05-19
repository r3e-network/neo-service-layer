using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Mock implementation of the Neo N3 blockchain service for testing.
    /// </summary>
    public class MockNeoN3BlockchainService : INeoN3BlockchainService
    {
        private readonly ILogger<MockNeoN3BlockchainService> _logger;
        private readonly Random _random = new Random();

        /// <summary>
        /// Initializes a new instance of the MockNeoN3BlockchainService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MockNeoN3BlockchainService(ILogger<MockNeoN3BlockchainService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task<string> GetBlockchainHeightAsync()
        {
            // Return a random height between 1000 and 10000
            return Task.FromResult(_random.Next(1000, 10000).ToString());
        }

        /// <inheritdoc/>
        public Task<BlockchainEvent[]> GetContractEventsAsync(string scriptHash, int fromBlock, int count)
        {
            // Create some mock events
            var events = new List<BlockchainEvent>();
            var eventCount = _random.Next(0, 5);

            for (int i = 0; i < eventCount; i++)
            {
                events.Add(new BlockchainEvent
                {
                    TxHash = $"0x{Guid.NewGuid():N}",
                    BlockIndex = (uint)(fromBlock + i),
                    EventName = "Transfer",
                    State = new object[] { "from", "to", "1000" }
                });
            }

            return Task.FromResult(events.ToArray());
        }

        /// <inheritdoc/>
        public Task<string> InvokeContractAsync(string scriptHash, string operation, params object[] args)
        {
            // Return a mock transaction hash
            return Task.FromResult($"0x{Guid.NewGuid():N}");
        }

        /// <inheritdoc/>
        public Task<ContractInvocationResult> TestInvokeContractAsync(string scriptHash, string operation, params object[] args)
        {
            // Return a mock invocation result
            return Task.FromResult(new ContractInvocationResult
            {
                State = "HALT",
                GasConsumed = "1.0",
                Exception = null,
                Stack = new object[]
                {
                    new { Type = "Boolean", Value = "true" }
                }
            });
        }

        /// <inheritdoc/>
        public Task<BlockchainTransaction> GetTransactionAsync(string txHash)
        {
            // Return a mock transaction
            return Task.FromResult(new BlockchainTransaction
            {
                Hash = txHash,
                BlockIndex = (uint)_random.Next(1000, 10000),
                BlockTime = (ulong)(DateTime.UtcNow.AddMinutes(-_random.Next(1, 60)) - new DateTime(1970, 1, 1)).TotalSeconds,
                Sender = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                Size = 1024,
                Version = 0,
                Nonce = (uint)_random.Next(1000000),
                Signers = new[] { "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT" },
                Script = "0x0123456789abcdef",
                SystemFee = "0.1",
                NetworkFee = "0.05",
                Attributes = new TransactionAttribute[0]
            });
        }
    }
}
