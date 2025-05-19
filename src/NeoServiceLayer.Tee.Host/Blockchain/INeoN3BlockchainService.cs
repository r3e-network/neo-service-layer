using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain
{
    /// <summary>
    /// Interface for Neo N3 blockchain service.
    /// </summary>
    public interface INeoN3BlockchainService
    {
        /// <summary>
        /// Initializes the blockchain service.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Gets the current blockchain height.
        /// </summary>
        /// <returns>The current blockchain height.</returns>
        Task<ulong> GetBlockchainHeightAsync();

        /// <summary>
        /// Gets a block by height.
        /// </summary>
        /// <param name="height">The block height.</param>
        /// <returns>The block.</returns>
        Task<BlockchainBlock> GetBlockByHeightAsync(ulong height);

        /// <summary>
        /// Gets a block by hash.
        /// </summary>
        /// <param name="hash">The block hash.</param>
        /// <returns>The block.</returns>
        Task<BlockchainBlock> GetBlockByHashAsync(string hash);

        /// <summary>
        /// Gets a transaction by hash.
        /// </summary>
        /// <param name="hash">The transaction hash.</param>
        /// <returns>The transaction.</returns>
        Task<BlockchainTransaction> GetTransactionAsync(string hash);

        /// <summary>
        /// Gets the balance of an address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="assetId">The asset ID (optional, defaults to GAS).</param>
        /// <returns>The balance.</returns>
        Task<decimal> GetBalanceAsync(string address, string assetId = null);

        /// <summary>
        /// Invokes a contract method.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="operation">The operation to invoke.</param>
        /// <param name="args">The arguments for the operation.</param>
        /// <returns>The transaction hash.</returns>
        Task<string> InvokeContractMethodAsync(string contractHash, string operation, object[] args);

        /// <summary>
        /// Subscribes to contract events.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="eventName">The event name (optional).</param>
        /// <param name="callback">The callback to invoke when an event is received.</param>
        /// <param name="fromBlock">The block height to start from (optional).</param>
        /// <returns>The subscription ID.</returns>
        Task<string> SubscribeToContractEventsAsync(string contractHash, string eventName, Func<BlockchainEvent, Task> callback, ulong fromBlock = 0);

        /// <summary>
        /// Unsubscribes from contract events.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>True if unsubscription was successful, false otherwise.</returns>
        Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId);

        /// <summary>
        /// Gets contract events by name.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="fromBlock">The block height to start from.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>The events.</returns>
        Task<List<BlockchainEvent>> GetContractEventsByNameAsync(string contractHash, string eventName, ulong fromBlock, int count);

        /// <summary>
        /// Sends a callback transaction to a Neo N3 smart contract with JavaScript execution results.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="method">The callback method name.</param>
        /// <param name="functionId">The JavaScript function ID that was executed.</param>
        /// <param name="result">The result of the JavaScript execution.</param>
        /// <returns>The transaction hash.</returns>
        Task<string> SendCallbackTransactionAsync(string contractHash, string method, string functionId, string result);

        /// <summary>
        /// Checks if the service is connected to the blockchain.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        Task<bool> IsConnectedAsync();
    }
}
