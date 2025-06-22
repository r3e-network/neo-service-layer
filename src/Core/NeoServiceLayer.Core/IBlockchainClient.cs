namespace NeoServiceLayer.Core;

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
}

/// <summary>
/// Represents a block.
/// </summary>
public class Block
{
    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long Height { get; set; }

    /// <summary>
    /// Gets or sets the block timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the previous block hash.
    /// </summary>
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transactions in the block.
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new List<Transaction>();
}

/// <summary>
/// Represents a transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction sender.
    /// </summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction recipient.
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long BlockHeight { get; set; }
}

/// <summary>
/// Represents a smart contract event.
/// </summary>
public class ContractEvent
{
    /// <summary>
    /// Gets or sets the contract address.
    /// </summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
