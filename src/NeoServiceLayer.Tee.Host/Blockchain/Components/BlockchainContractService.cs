using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain.Components
{
    /// <summary>
    /// Service for interacting with blockchain contracts.
    /// </summary>
    public class BlockchainContractService : BlockchainServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the BlockchainContractService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcUrl">The RPC URL.</param>
        /// <param name="network">The network name.</param>
        public BlockchainContractService(ILogger logger, string rpcUrl, string network = "mainnet")
            : base(logger, rpcUrl, network)
        {
        }

        /// <summary>
        /// Gets the balance of an address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="assetId">The asset ID.</param>
        /// <returns>The balance.</returns>
        public async Task<decimal> GetBalanceAsync(string address, string assetId = null)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Address cannot be null or empty", nameof(address));
            }

            try
            {
                // If no asset ID is provided, use GAS
                assetId = assetId ?? "0xd2a4cff31913016155e38e474a2c06d08be276cf";

                var response = await SendRpcRequestAsync("getnep17balances", new object[] { address });
                var balancesJson = response.GetProperty("result").GetProperty("balances");

                foreach (var balance in balancesJson.EnumerateArray())
                {
                    var asset = balance.GetProperty("assethash").GetString();
                    if (asset.Equals(assetId, StringComparison.OrdinalIgnoreCase))
                    {
                        var amount = balance.GetProperty("amount").GetString();
                        if (decimal.TryParse(amount, out var result))
                        {
                            return result;
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 balance for address {Address}", address);
                throw;
            }
        }

        /// <summary>
        /// Gets events for a contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="fromBlock">The starting block height.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>The events.</returns>
        public async Task<BlockchainEvent[]> GetContractEventsAsync(string contractHash, ulong fromBlock = 0, int count = 100)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(contractHash))
            {
                throw new ArgumentException("Contract hash cannot be null or empty", nameof(contractHash));
            }

            try
            {
                var response = await SendRpcRequestAsync("getapplicationlog", new object[] { contractHash, (int)fromBlock, count });
                var eventsJson = response.GetProperty("result").GetProperty("executions");

                var events = new List<BlockchainEvent>();

                foreach (var execution in eventsJson.EnumerateArray())
                {
                    var txHash = execution.GetProperty("txid").GetString();
                    var notifications = execution.GetProperty("notifications");

                    foreach (var notification in notifications.EnumerateArray())
                    {
                        var contract = notification.GetProperty("contract").GetString();
                        if (contract.Equals(contractHash, StringComparison.OrdinalIgnoreCase))
                        {
                            var eventName = notification.GetProperty("eventname").GetString();
                            var state = notification.GetProperty("state").GetProperty("value");

                            var stateValues = new List<object>();
                            foreach (var item in state.EnumerateArray())
                            {
                                stateValues.Add(item.GetProperty("value").GetString());
                            }

                            events.Add(new BlockchainEvent
                            {
                                TxHash = txHash,
                                BlockHeight = fromBlock,
                                ContractHash = contractHash,
                                EventName = eventName,
                                State = stateValues.ToArray()
                            });
                        }
                    }
                }

                _logger.LogDebug("Retrieved {EventCount} Neo N3 events for contract {ContractHash}", events.Count, contractHash);

                return events.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 events for contract {ContractHash}", contractHash);
                throw;
            }
        }

        /// <summary>
        /// Gets events for a contract by name.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="fromBlock">The starting block height.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>The events.</returns>
        public async Task<BlockchainEvent[]> GetContractEventsByNameAsync(string contractHash, string eventName, ulong fromBlock = 0, int count = 100)
        {
            CheckDisposed();
            CheckInitialized();

            if (string.IsNullOrEmpty(contractHash))
            {
                throw new ArgumentException("Contract hash cannot be null or empty", nameof(contractHash));
            }

            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
            }

            try
            {
                var allEvents = await GetContractEventsAsync(contractHash, fromBlock, count);
                return allEvents.Where(e => e.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Neo N3 events for contract {ContractHash} and event {EventName}", contractHash, eventName);
                throw;
            }
        }

        /// <summary>
        /// Builds parameters for invoking a contract function.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The parameters.</returns>
        protected object[] BuildInvokeFunctionParams(string contractHash, string operation, object[] args)
        {
            var parameters = new List<object> { contractHash, operation };

            // Format arguments
            var formattedArgs = new List<object>();
            if (args != null)
            {
                foreach (var arg in args)
                {
                    formattedArgs.Add(FormatArgument(arg));
                }
            }

            parameters.Add(formattedArgs);
            return parameters.ToArray();
        }

        /// <summary>
        /// Formats an argument for a contract invocation.
        /// </summary>
        /// <param name="arg">The argument to format.</param>
        /// <returns>The formatted argument.</returns>
        protected object FormatArgument(object arg)
        {
            if (arg == null)
            {
                return new { type = "Null" };
            }

            if (arg is string str)
            {
                return new { type = "String", value = str };
            }

            if (arg is int || arg is long || arg is uint || arg is ulong)
            {
                return new { type = "Integer", value = arg.ToString() };
            }

            if (arg is bool b)
            {
                return new { type = "Boolean", value = b };
            }

            if (arg is byte[] bytes)
            {
                return new { type = "ByteArray", value = Convert.ToBase64String(bytes) };
            }

            // Default to string representation
            return new { type = "String", value = arg.ToString() };
        }
    }
}
