/// <summary>
/// Interface for blockchain clients.
/// </summary>
    /// <summary>
    /// Gets the blockchain type.
    /// </summary>
    /// <summary>
    /// Gets the current block height.
    /// </summary>
    /// <returns>The current block height.</returns>
    /// <summary>
    /// Gets a block by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block.</returns>
    /// <summary>
    /// Gets a block by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <returns>The block.</returns>
    /// <summary>
    /// Gets a transaction by hash.
    /// </summary>
    /// <param name="hash">The transaction hash.</param>
    /// <returns>The transaction.</returns>
    /// <summary>
    /// Sends a transaction.
    /// </summary>
    /// <param name="transaction">The transaction to send.</param>
    /// <returns>The transaction hash.</returns>
    /// <summary>
    /// Subscribes to new blocks.
    /// </summary>
    /// <param name="callback">The callback to invoke when a new block is received.</param>
    /// <returns>The subscription ID.</returns>
    /// <summary>
    /// Unsubscribes from new blocks.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>True if the subscription was cancelled, false otherwise.</returns>
    /// <summary>
    /// Subscribes to new transactions.
    /// </summary>
    /// <param name="callback">The callback to invoke when a new transaction is received.</param>
    /// <returns>The subscription ID.</returns>
    /// <summary>
    /// Unsubscribes from new transactions.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>True if the subscription was cancelled, false otherwise.</returns>
    /// <summary>
    /// Subscribes to smart contract events.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="callback">The callback to invoke when an event is received.</param>
    /// <returns>The subscription ID.</returns>
    /// <summary>
    /// Unsubscribes from smart contract events.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>True if the subscription was cancelled, false otherwise.</returns>
    /// <summary>
    /// Calls a smart contract method.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="args">The method arguments.</param>
    /// <returns>The method result.</returns>
    /// <summary>
    /// Invokes a smart contract method.
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="args">The method arguments.</param>
    /// <returns>The transaction hash.</returns>
    /// <summary>
    /// Gets the balance of an address for a specific asset.
    /// </summary>
    /// <param name="address">The address to query.</param>
    /// <param name="assetId">The asset identifier (optional, defaults to native token).</param>
    /// <returns>The balance amount.</returns>
/// <summary>
/// Represents a block.
/// </summary>
    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    /// <summary>
    /// Gets or sets the block timestamp.
    /// </summary>
    /// <summary>
    /// Gets or sets the previous block hash.
    /// </summary>
    /// <summary>
    /// Gets or sets the transactions in the block.
    /// </summary>
/// <summary>
/// Represents a transaction.
/// </summary>
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction sender.
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction sender (alias for Sender).
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction recipient.
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction recipient (alias for Recipient).
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction value.
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    /// <summary>
    /// Gets or sets the block number (alias for BlockHeight).
    /// </summary>
    /// <summary>
    /// Gets or sets the number of confirmations.
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    /// <summary>
    /// Gets or sets the gas used.
    /// </summary>
/// <summary>
/// Represents a smart contract event.
/// </summary>
    /// <summary>
    /// Gets or sets the contract address.
    /// </summary>
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    /// <summary>
    /// Gets or sets the event parameters.
    /// </summary>
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
