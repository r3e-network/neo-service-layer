using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Blockchain
{
    /// <summary>
    /// Interface for a blockchain service that interacts with a blockchain.
    /// </summary>
    public interface IBlockchainService : IDisposable
    {
        /// <summary>
        /// Initializes the blockchain service.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Gets the current blockchain height.
        /// </summary>
        /// <returns>The blockchain height.</returns>
        Task<ulong> GetBlockchainHeightAsync();

        /// <summary>
        /// Gets a block by its height.
        /// </summary>
        /// <param name="height">The block height.</param>
        /// <returns>The block, or null if not found.</returns>
        Task<BlockchainBlock> GetBlockByHeightAsync(ulong height);

        /// <summary>
        /// Gets a block by its hash.
        /// </summary>
        /// <param name="hash">The block hash.</param>
        /// <returns>The block, or null if not found.</returns>
        Task<BlockchainBlock> GetBlockByHashAsync(string hash);

        /// <summary>
        /// Gets a transaction by its hash.
        /// </summary>
        /// <param name="hash">The transaction hash.</param>
        /// <returns>The transaction, or null if not found.</returns>
        Task<BlockchainTransaction> GetTransactionAsync(string hash);

        /// <summary>
        /// Gets the balance of an address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="assetId">The asset ID, or null for the native asset.</param>
        /// <returns>The balance.</returns>
        Task<decimal> GetBalanceAsync(string address, string assetId = null);

        /// <summary>
        /// Gets events emitted by a smart contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="fromBlock">The block height to start from.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>An array of blockchain events.</returns>
        Task<BlockchainEvent[]> GetContractEventsAsync(string contractHash, ulong fromBlock = 0, int count = 100);

        /// <summary>
        /// Gets events emitted by a smart contract with a specific name.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="fromBlock">The block height to start from.</param>
        /// <param name="count">The maximum number of events to return.</param>
        /// <returns>An array of blockchain events.</returns>
        Task<BlockchainEvent[]> GetContractEventsByNameAsync(string contractHash, string eventName, ulong fromBlock = 0, int count = 100);

        /// <summary>
        /// Invokes a smart contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="operation">The operation to invoke.</param>
        /// <param name="args">The arguments to pass to the operation.</param>
        /// <returns>The transaction hash.</returns>
        Task<string> InvokeContractAsync(string contractHash, string operation, params object[] args);

        /// <summary>
        /// Test invokes a smart contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="operation">The operation to invoke.</param>
        /// <param name="args">The arguments to pass to the operation.</param>
        /// <returns>The result of the test invocation.</returns>
        Task<ContractInvocationResult> TestInvokeContractAsync(string contractHash, string operation, params object[] args);

        /// <summary>
        /// Subscribes to events emitted by a smart contract.
        /// </summary>
        /// <param name="contractHash">The contract hash.</param>
        /// <param name="eventName">The event name, or null for all events.</param>
        /// <param name="callback">The callback to invoke when an event is detected.</param>
        /// <param name="fromBlock">The block height to start from.</param>
        /// <returns>The subscription ID.</returns>
        Task<string> SubscribeToContractEventsAsync(string contractHash, string eventName, Func<BlockchainEvent, Task> callback, ulong fromBlock = 0);

        /// <summary>
        /// Unsubscribes from events.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <returns>True if the subscription was removed, false otherwise.</returns>
        Task<bool> UnsubscribeFromEventsAsync(string subscriptionId);

        /// <summary>
        /// Gets all active subscriptions.
        /// </summary>
        /// <returns>A list of active subscriptions.</returns>
        Task<IReadOnlyList<BlockchainSubscription>> GetActiveSubscriptionsAsync();

        /// <summary>
        /// Gets the blockchain type.
        /// </summary>
        /// <returns>The blockchain type.</returns>
        BlockchainType GetBlockchainType();

        /// <summary>
        /// Gets the blockchain network.
        /// </summary>
        /// <returns>The blockchain network.</returns>
        string GetBlockchainNetwork();

        /// <summary>
        /// Gets the blockchain RPC URL.
        /// </summary>
        /// <returns>The blockchain RPC URL.</returns>
        string GetBlockchainRpcUrl();

        /// <summary>
        /// Gets the blockchain version.
        /// </summary>
        /// <returns>The blockchain version.</returns>
        Task<string> GetBlockchainVersionAsync();

        /// <summary>
        /// Gets the blockchain peer count.
        /// </summary>
        /// <returns>The blockchain peer count.</returns>
        Task<int> GetBlockchainPeerCountAsync();

        /// <summary>
        /// Gets the blockchain sync state.
        /// </summary>
        /// <returns>The blockchain sync state.</returns>
        Task<BlockchainSyncState> GetBlockchainSyncStateAsync();

        /// <summary>
        /// Checks if the blockchain is connected.
        /// </summary>
        /// <returns>True if the blockchain is connected, false otherwise.</returns>
        Task<bool> IsConnectedAsync();

        /// <summary>
        /// Checks if the blockchain is synced.
        /// </summary>
        /// <returns>True if the blockchain is synced, false otherwise.</returns>
        Task<bool> IsSyncedAsync();
    }

    /// <summary>
    /// Types of blockchains.
    /// </summary>
    public enum BlockchainType
    {
        /// <summary>
        /// Neo N3 blockchain.
        /// </summary>
        NeoN3,

        /// <summary>
        /// Ethereum blockchain.
        /// </summary>
        Ethereum,

        /// <summary>
        /// Bitcoin blockchain.
        /// </summary>
        Bitcoin,

        /// <summary>
        /// Binance Smart Chain.
        /// </summary>
        BinanceSmartChain,

        /// <summary>
        /// Polygon blockchain.
        /// </summary>
        Polygon,

        /// <summary>
        /// Solana blockchain.
        /// </summary>
        Solana,

        /// <summary>
        /// Other blockchain.
        /// </summary>
        Other
    }

    /// <summary>
    /// Blockchain sync state.
    /// </summary>
    public enum BlockchainSyncState
    {
        /// <summary>
        /// The blockchain is not synced.
        /// </summary>
        NotSynced,

        /// <summary>
        /// The blockchain is syncing.
        /// </summary>
        Syncing,

        /// <summary>
        /// The blockchain is synced.
        /// </summary>
        Synced,

        /// <summary>
        /// The blockchain sync state is unknown.
        /// </summary>
        Unknown
    }
}
