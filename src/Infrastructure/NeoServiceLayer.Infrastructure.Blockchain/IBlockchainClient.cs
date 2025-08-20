using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using NeoServiceLayer.Core;
using Block = NeoServiceLayer.Infrastructure.Block;
using Transaction = NeoServiceLayer.Infrastructure.Transaction;
using ContractEvent = NeoServiceLayer.Infrastructure.ContractEvent;


namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Interface for blockchain clients.
/// </summary>
public interface IBlockchainClient
{
    /// <summary>
    /// Gets the blockchain type.
    /// </summary>
    BlockchainType BlockchainType { get; }

    /// <summary>
    /// Gets the current block height.
    /// </summary>
    /// <returns>The current block height.</returns>
    Task<long> GetBlockHeightAsync();

    /// <summary>
    /// Gets a block by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block.</returns>
    Task<Block> GetBlockAsync(long height);

    /// <summary>
    /// Gets a block by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <returns>The block.</returns>
    Task<Block> GetBlockAsync(string hash);

    /// <summary>
    /// Gets a block hash by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block hash.</returns>
    Task<string> GetBlockHashAsync(long height);

    /// <summary>
    /// Gets a transaction by hash.
    /// </summary>
    /// <param name="hash">The transaction hash.</param>
    /// <returns>The transaction.</returns>
    Task<Transaction> GetTransactionAsync(string hash);

    /// <summary>
    /// Sends a transaction.
    /// </summary>
    /// <param name="transaction">The transaction to send.</param>
    /// <returns>The transaction hash.</returns>
    Task<string> SendTransactionAsync(Transaction transaction);

    /// <summary>
    /// Gets the balance of an address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="assetId">The asset ID.</param>
    /// <returns>The balance.</returns>
    Task<decimal> GetBalanceAsync(string address, string assetId);

    /// <summary>
    /// Gets the gas price.
    /// </summary>
    /// <returns>The gas price.</returns>
    Task<decimal> GetGasPriceAsync();

    /// <summary>
    /// Estimates the gas for a transaction.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <returns>The estimated gas.</returns>
    Task<decimal> EstimateGasAsync(Transaction transaction);

    /// <summary>
    /// Gets events from a specific block.
    /// </summary>
    /// <param name="blockHeight">The block height.</param>
    /// <returns>The list of events in the block.</returns>
    Task<IEnumerable<ContractEvent>> GetBlockEventsAsync(long blockHeight);

    /// <summary>
    /// Subscribes to new blocks.
    /// </summary>
    /// <param name="callback">The callback to invoke when a new block is received.</param>
    /// <returns>The subscription ID.</returns>
    Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback);

    /// <summary>
    /// Unsubscribes from new blocks.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>True if the subscription was cancelled, false otherwise.</returns>
    Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId);

    /// <summary>
    /// Subscribes to new transactions.
    /// </summary>
    /// <param name="callback">The callback to invoke when a new transaction is received.</param>
    /// <returns>The subscription ID.</returns>
    Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback);

    /// <summary>
    /// Unsubscribes from new transactions.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>True if the subscription was cancelled, false otherwise.</returns>
    Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId);

    /// <summary>
    /// Subscribes to smart contract events.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="callback">The callback to invoke when an event is received.</param>
    /// <returns>The subscription ID.</returns>
    Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback);

    /// <summary>
    /// Unsubscribes from smart contract events.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>True if the subscription was cancelled, false otherwise.</returns>
    Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId);

    /// <summary>
    /// Calls a smart contract method.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="args">The method arguments.</param>
    /// <returns>The method result.</returns>
    Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args);

    /// <summary>
    /// Invokes a smart contract method.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="args">The method arguments.</param>
    /// <returns>The transaction hash.</returns>
    Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args);

    /// <summary>
    /// Calls a contract method.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <returns>The result of the contract call.</returns>
    Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters);

    /// <summary>
    /// Invokes a contract method.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <returns>The transaction hash.</returns>
    Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters);
}
