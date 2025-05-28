using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Mock implementation of the blockchain client for testing.
/// </summary>
public class MockBlockchainClient : IBlockchainClient
{
    private readonly ILogger _logger;
    private readonly BlockchainType _blockchainType;
    private readonly Dictionary<string, Transaction> _transactions = new();
    private readonly Dictionary<long, Block> _blocks = new();
    private readonly Dictionary<string, decimal> _balances = new();
    private readonly Dictionary<string, Func<Block, Task>> _blockSubscriptions = new();
    private readonly Dictionary<string, Func<Transaction, Task>> _transactionSubscriptions = new();
    private readonly Dictionary<string, (string ContractAddress, string EventName, Func<ContractEvent, Task> Callback)> _contractEventSubscriptions = new();
    private long _blockHeight = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockBlockchainClient"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    public MockBlockchainClient(ILogger logger, BlockchainType blockchainType)
    {
        _logger = logger;
        _blockchainType = blockchainType;

        // Initialize some mock data
        for (long i = 1; i <= _blockHeight; i++)
        {
            _blocks[i] = new Block
            {
                Hash = $"0x{Guid.NewGuid():N}",
                Height = i,
                Timestamp = DateTime.UtcNow.AddSeconds(-(_blockHeight - i)),
                PreviousHash = i > 1 ? _blocks[i - 1]?.Hash ?? string.Empty : string.Empty,
                Transactions = new List<Transaction>()
            };
        }
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => _blockchainType;

    /// <inheritdoc/>
    public Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters)
    {
        _logger.LogInformation("Calling contract {ContractAddress}.{Method} on {BlockchainType}", contractAddress, method, _blockchainType);
        return Task.FromResult($"{{\"result\": \"success\", \"value\": 42, \"contract\": \"{contractAddress}\", \"method\": \"{method}\"}}");
    }

    /// <inheritdoc/>
    public Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return CallContractAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public Task<decimal> EstimateGasAsync(Transaction transaction)
    {
        _logger.LogInformation("Estimating gas for transaction {TransactionHash} on {BlockchainType}", transaction.Hash, _blockchainType);
        return Task.FromResult(10.0m);
    }

    /// <inheritdoc/>
    public Task<decimal> GetBalanceAsync(string address, string assetId)
    {
        _logger.LogInformation("Getting balance for address {Address} and asset {AssetId} on {BlockchainType}", address, assetId, _blockchainType);

        string key = $"{address}:{assetId}";
        if (!_balances.TryGetValue(key, out var balance))
        {
            balance = 100.0m; // Default balance
            _balances[key] = balance;
        }

        return Task.FromResult(balance);
    }

    /// <inheritdoc/>
    public Task<Block> GetBlockAsync(long height)
    {
        _logger.LogInformation("Getting block at height {Height} on {BlockchainType}", height, _blockchainType);

        if (_blocks.TryGetValue(height, out var block))
        {
            return Task.FromResult(block);
        }

        throw new ArgumentException($"Block at height {height} not found.");
    }

    /// <inheritdoc/>
    public Task<Block> GetBlockAsync(string hash)
    {
        _logger.LogInformation("Getting block with hash {Hash} on {BlockchainType}", hash, _blockchainType);

        foreach (var block in _blocks.Values)
        {
            if (block.Hash == hash)
            {
                return Task.FromResult(block);
            }
        }

        throw new ArgumentException($"Block with hash {hash} not found.");
    }

    /// <inheritdoc/>
    public Task<string> GetBlockHashAsync(long height)
    {
        _logger.LogInformation("Getting block hash at height {Height} on {BlockchainType}", height, _blockchainType);

        if (_blocks.TryGetValue(height, out var block))
        {
            return Task.FromResult(block.Hash);
        }

        throw new ArgumentException($"Block at height {height} not found.");
    }

    /// <inheritdoc/>
    public Task<long> GetBlockHeightAsync()
    {
        _logger.LogInformation("Getting block height on {BlockchainType}", _blockchainType);
        return Task.FromResult(_blockHeight);
    }

    /// <inheritdoc/>
    public Task<decimal> GetGasPriceAsync()
    {
        _logger.LogInformation("Getting gas price on {BlockchainType}", _blockchainType);
        return Task.FromResult(0.1m);
    }

    /// <inheritdoc/>
    public Task<Transaction> GetTransactionAsync(string hash)
    {
        _logger.LogInformation("Getting transaction {Hash} on {BlockchainType}", hash, _blockchainType);

        if (_transactions.TryGetValue(hash, out var transaction))
        {
            return Task.FromResult(transaction);
        }

        // Create a mock transaction if not found
        transaction = new Transaction
        {
            Hash = hash,
            Sender = $"{_blockchainType}Sender",
            Recipient = $"{_blockchainType}Recipient",
            Value = 10.0m,
            Data = $"{_blockchainType}Data",
            Timestamp = DateTime.UtcNow,
            BlockHash = $"0x{Guid.NewGuid():N}",
            BlockHeight = _blockHeight
        };

        _transactions[hash] = transaction;
        return Task.FromResult(transaction);
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters)
    {
        _logger.LogInformation("Invoking contract {ContractAddress}.{Method} on {BlockchainType}", contractAddress, method, _blockchainType);

        // Create a mock transaction
        string hash = $"0x{Guid.NewGuid():N}";
        var transaction = new Transaction
        {
            Hash = hash,
            Sender = $"{_blockchainType}Sender",
            Recipient = contractAddress,
            Value = 0.0m,
            Data = $"{method}({string.Join(",", parameters)})",
            Timestamp = DateTime.UtcNow,
            BlockHash = $"0x{Guid.NewGuid():N}",
            BlockHeight = _blockHeight
        };

        _transactions[hash] = transaction;
        return Task.FromResult(hash);
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return InvokeContractAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public Task<string> SendTransactionAsync(Transaction transaction)
    {
        _logger.LogInformation("Sending transaction {TransactionHash} on {BlockchainType}", transaction.Hash, _blockchainType);

        // Store the transaction
        _transactions[transaction.Hash] = transaction;

        // Add the transaction to the latest block
        if (_blocks.TryGetValue(_blockHeight, out var block))
        {
            block.Transactions.Add(transaction);
        }

        return Task.FromResult(transaction.Hash);
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback)
    {
        string subscriptionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Subscribing to blocks with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);
        _blockSubscriptions[subscriptionId] = callback;
        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId)
    {
        _logger.LogInformation("Unsubscribing from blocks with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);
        return Task.FromResult(_blockSubscriptions.Remove(subscriptionId));
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback)
    {
        string subscriptionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Subscribing to transactions with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);
        _transactionSubscriptions[subscriptionId] = callback;
        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId)
    {
        _logger.LogInformation("Unsubscribing from transactions with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);
        return Task.FromResult(_transactionSubscriptions.Remove(subscriptionId));
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback)
    {
        string subscriptionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Subscribing to contract events for contract {ContractAddress} and event {EventName} with subscription ID {SubscriptionId} on {BlockchainType}", contractAddress, eventName, subscriptionId, _blockchainType);
        _contractEventSubscriptions[subscriptionId] = (contractAddress, eventName, callback);
        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        _logger.LogInformation("Unsubscribing from contract events with subscription ID {SubscriptionId} on {BlockchainType}", subscriptionId, _blockchainType);
        return Task.FromResult(_contractEventSubscriptions.Remove(subscriptionId));
    }

    /// <summary>
    /// Adds a new block to the blockchain.
    /// </summary>
    /// <returns>The new block.</returns>
    public Block AddBlock()
    {
        _blockHeight++;
        var block = new Block
        {
            Hash = $"0x{Guid.NewGuid():N}",
            Height = _blockHeight,
            Timestamp = DateTime.UtcNow,
            PreviousHash = _blocks[_blockHeight - 1].Hash,
            Transactions = new List<Transaction>()
        };

        _blocks[_blockHeight] = block;
        _logger.LogInformation("Added new block at height {Height} on {BlockchainType}", _blockHeight, _blockchainType);

        // Notify subscribers
        foreach (var callback in _blockSubscriptions.Values)
        {
            _ = callback(block);
        }

        return block;
    }

    /// <summary>
    /// Sets the balance for an address and asset.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="balance">The balance.</param>
    public void SetBalance(string address, string assetId, decimal balance)
    {
        string key = $"{address}:{assetId}";
        _balances[key] = balance;
        _logger.LogInformation("Set balance for address {Address} and asset {AssetId} to {Balance} on {BlockchainType}", address, assetId, balance, _blockchainType);
    }

    /// <summary>
    /// Adds a transaction to the blockchain.
    /// </summary>
    /// <param name="transaction">The transaction to add.</param>
    /// <returns>The added transaction.</returns>
    public Transaction AddTransaction(Transaction transaction)
    {
        _transactions[transaction.Hash] = transaction;

        // Add the transaction to the latest block
        if (_blocks.TryGetValue(_blockHeight, out var block))
        {
            block.Transactions.Add(transaction);
        }

        _logger.LogInformation("Added transaction {Hash} on {BlockchainType}", transaction.Hash, _blockchainType);

        // Notify subscribers
        foreach (var callback in _transactionSubscriptions.Values)
        {
            _ = callback(transaction);
        }

        return transaction;
    }

    /// <summary>
    /// Emits a contract event.
    /// </summary>
    /// <param name="contractEvent">The contract event to emit.</param>
    public void EmitContractEvent(ContractEvent contractEvent)
    {
        _logger.LogInformation("Emitting contract event {EventName} for contract {ContractAddress} on {BlockchainType}", contractEvent.EventName, contractEvent.ContractAddress, _blockchainType);

        // Notify subscribers
        foreach (var (contractAddress, eventName, callback) in _contractEventSubscriptions.Values)
        {
            if (contractEvent.ContractAddress == contractAddress && contractEvent.EventName == eventName)
            {
                _ = callback(contractEvent);
            }
        }
    }
}
