using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Blockchain
{
    /// <summary>
    /// Interface for a blockchain event listener that listens for blockchain events.
    /// </summary>
    public interface IBlockchainEventListener : IDisposable
    {
        /// <summary>
        /// Initializes the blockchain event listener.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Starts the blockchain event listener.
        /// </summary>
        /// <returns>True if the listener was started successfully, false otherwise.</returns>
        Task<bool> StartAsync();

        /// <summary>
        /// Stops the blockchain event listener.
        /// </summary>
        /// <returns>True if the listener was stopped successfully, false otherwise.</returns>
        Task<bool> StopAsync();

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
        /// Gets the blockchain service.
        /// </summary>
        /// <returns>The blockchain service.</returns>
        IBlockchainService GetBlockchainService();

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
        /// Gets the polling interval in milliseconds.
        /// </summary>
        /// <returns>The polling interval in milliseconds.</returns>
        int GetPollingIntervalMs();

        /// <summary>
        /// Sets the polling interval in milliseconds.
        /// </summary>
        /// <param name="intervalMs">The polling interval in milliseconds.</param>
        void SetPollingIntervalMs(int intervalMs);

        /// <summary>
        /// Gets the maximum number of blocks to process in a single poll.
        /// </summary>
        /// <returns>The maximum number of blocks to process in a single poll.</returns>
        int GetMaxBlocksPerPoll();

        /// <summary>
        /// Sets the maximum number of blocks to process in a single poll.
        /// </summary>
        /// <param name="maxBlocks">The maximum number of blocks to process in a single poll.</param>
        void SetMaxBlocksPerPoll(int maxBlocks);

        /// <summary>
        /// Gets the number of confirmations required before processing a block.
        /// </summary>
        /// <returns>The number of confirmations required before processing a block.</returns>
        int GetRequiredConfirmations();

        /// <summary>
        /// Sets the number of confirmations required before processing a block.
        /// </summary>
        /// <param name="confirmations">The number of confirmations required before processing a block.</param>
        void SetRequiredConfirmations(int confirmations);

        /// <summary>
        /// Gets a value indicating whether the listener is running.
        /// </summary>
        /// <returns>True if the listener is running, false otherwise.</returns>
        bool IsRunning();

        /// <summary>
        /// Gets the last processed block height.
        /// </summary>
        /// <returns>The last processed block height.</returns>
        Task<ulong> GetLastProcessedBlockHeightAsync();

        /// <summary>
        /// Sets the last processed block height.
        /// </summary>
        /// <param name="height">The last processed block height.</param>
        /// <returns>True if the height was set successfully, false otherwise.</returns>
        Task<bool> SetLastProcessedBlockHeightAsync(ulong height);

        /// <summary>
        /// Gets the number of events processed.
        /// </summary>
        /// <returns>The number of events processed.</returns>
        ulong GetEventsProcessedCount();

        /// <summary>
        /// Gets the number of blocks processed.
        /// </summary>
        /// <returns>The number of blocks processed.</returns>
        ulong GetBlocksProcessedCount();

        /// <summary>
        /// Gets the number of errors encountered.
        /// </summary>
        /// <returns>The number of errors encountered.</returns>
        ulong GetErrorCount();

        /// <summary>
        /// Gets the last error message.
        /// </summary>
        /// <returns>The last error message.</returns>
        string GetLastErrorMessage();

        /// <summary>
        /// Gets the last error timestamp.
        /// </summary>
        /// <returns>The last error timestamp.</returns>
        DateTime? GetLastErrorTimestamp();

        /// <summary>
        /// Gets the start timestamp.
        /// </summary>
        /// <returns>The start timestamp.</returns>
        DateTime? GetStartTimestamp();

        /// <summary>
        /// Gets the stop timestamp.
        /// </summary>
        /// <returns>The stop timestamp.</returns>
        DateTime? GetStopTimestamp();

        /// <summary>
        /// Gets the last poll timestamp.
        /// </summary>
        /// <returns>The last poll timestamp.</returns>
        DateTime? GetLastPollTimestamp();

        /// <summary>
        /// Gets the last poll duration in milliseconds.
        /// </summary>
        /// <returns>The last poll duration in milliseconds.</returns>
        long GetLastPollDurationMs();

        /// <summary>
        /// Gets the average poll duration in milliseconds.
        /// </summary>
        /// <returns>The average poll duration in milliseconds.</returns>
        double GetAveragePollDurationMs();

        /// <summary>
        /// Gets the total poll duration in milliseconds.
        /// </summary>
        /// <returns>The total poll duration in milliseconds.</returns>
        long GetTotalPollDurationMs();

        /// <summary>
        /// Gets the number of polls.
        /// </summary>
        /// <returns>The number of polls.</returns>
        ulong GetPollCount();
    }
}
