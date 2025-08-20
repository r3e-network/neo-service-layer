using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using System.Threading;

namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Mock implementation of IBlockchainClient for testing purposes.
/// </summary>
public class MockBlockchainClient : NeoServiceLayer.Infrastructure.IBlockchainClient
{
    private readonly ILogger<MockBlockchainClient> _logger;
    private readonly BlockchainType _blockchainType;
    private readonly Dictionary<string, NeoServiceLayer.Infrastructure.Block> _blocksByHash = new();
    private readonly Dictionary<long, NeoServiceLayer.Infrastructure.Block> _blocksByHeight = new();
    private readonly Dictionary<string, NeoServiceLayer.Infrastructure.Transaction> _transactions = new();
    private readonly Dictionary<string, List<Func<NeoServiceLayer.Infrastructure.Block, Task>>> _blockSubscriptions = new();
    private readonly Dictionary<string, List<Func<NeoServiceLayer.Infrastructure.Transaction, Task>>> _transactionSubscriptions = new();
    private readonly Dictionary<string, List<Func<NeoServiceLayer.Infrastructure.ContractEvent, Task>>> _eventSubscriptions = new();
    private long _currentHeight = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockBlockchainClient"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    public MockBlockchainClient(ILogger<MockBlockchainClient> logger, BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        _logger = logger;
        _blockchainType = blockchainType;
        InitializeMockData();
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => _blockchainType;

    /// <inheritdoc/>
    public async Task<long> GetBlockHeightAsync()
    {
        await Task.Delay(10); // Simulate network delay
        return _currentHeight;
    }

    /// <inheritdoc/>
    public async Task<NeoServiceLayer.Infrastructure.Block> GetBlockAsync(long height)
    {
        await Task.Delay(10); // Simulate network delay

        if (_blocksByHeight.TryGetValue(height, out var block))
        {
            return block;
        }

        // Generate a mock block for the requested height
        block = GenerateMockBlock(height);
        _blocksByHeight[height] = block;
        _blocksByHash[block.Hash] = block;
        return block;
    }

    /// <inheritdoc/>
    public async Task<NeoServiceLayer.Infrastructure.Block> GetBlockAsync(string hash)
    {
        await Task.Delay(10); // Simulate network delay

        if (_blocksByHash.TryGetValue(hash, out var block))
        {
            return block;
        }

        throw new InvalidOperationException($"Block with hash {hash} not found");
    }

    /// <inheritdoc/>
    public async Task<string> GetBlockHashAsync(long height)
    {
        await Task.Delay(10); // Simulate network delay

        if (_blocksByHeight.TryGetValue(height, out var block))
        {
            return block.Hash;
        }

        // Generate a mock hash if block doesn't exist
        return GenerateHash();
    }

    /// <inheritdoc/>
    public async Task<NeoServiceLayer.Infrastructure.Transaction> GetTransactionAsync(string hash)
    {
        await Task.Delay(10); // Simulate network delay

        if (_transactions.TryGetValue(hash, out var tx))
        {
            return tx;
        }

        // Generate a mock transaction
        var transaction = GenerateMockTransaction(hash);
        _transactions[hash] = transaction;
        return transaction;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NeoServiceLayer.Infrastructure.ContractEvent>> GetBlockEventsAsync(long blockHeight)
    {
        await Task.Delay(10); // Simulate network delay
        
        var events = new List<NeoServiceLayer.Infrastructure.ContractEvent>();
        
        // Generate some mock events for the block
        var random = new Random((int)blockHeight);
        var eventCount = random.Next(0, 5);
        
        for (int i = 0; i < eventCount; i++)
        {
            events.Add(new NeoServiceLayer.Infrastructure.ContractEvent
            {
                ContractHash = $"0x{GenerateHash().Substring(0, 40)}",
                EventName = $"Event_{i}",
                BlockIndex = (uint)blockHeight,
                TransactionHash = GenerateHash(),
                Parameters = new Dictionary<string, object>
                {
                    ["param1"] = random.Next(100),
                    ["param2"] = $"value_{i}"
                },
                Timestamp = DateTime.UtcNow.AddSeconds(-random.Next(3600))
            });
        }
        
        _logger.LogDebug("Retrieved {EventCount} events from block {BlockHeight}", events.Count, blockHeight);
        return events;
    }

    /// <inheritdoc/>
    public async Task<string> SendTransactionAsync(NeoServiceLayer.Infrastructure.Transaction transaction)
    {
        await Task.Delay(50); // Simulate network delay

        var hash = transaction.Hash ?? GenerateHash();
        transaction.Hash = hash;
        _transactions[hash] = transaction;

        _logger.LogDebug("Sent transaction {Hash}", hash);

        // Notify transaction subscribers
        foreach (var callbacks in _transactionSubscriptions.Values)
        {
            foreach (var callback in callbacks)
            {
                await callback(transaction);
            }
        }

        return hash;
    }

    /// <inheritdoc/>
    public async Task<decimal> GetBalanceAsync(string address, string assetId = "")
    {
        await Task.Delay(10); // Simulate network delay

        // Return a mock balance based on the address
        var seed = address.GetHashCode();
        var random = new Random(seed);
        var balance = (decimal)(random.NextDouble() * 1000000);

        _logger.LogDebug("Getting balance for {Address}, asset: {AssetId}, balance: {Balance}", address, assetId, balance);

        return balance;
    }

    /// <inheritdoc/>
    public async Task<decimal> GetGasPriceAsync()
    {
        await Task.Delay(10); // Simulate network delay
        return 0.00001m;
    }

    /// <inheritdoc/>
    public async Task<decimal> EstimateGasAsync(NeoServiceLayer.Infrastructure.Transaction transaction)
    {
        await Task.Delay(10); // Simulate network delay
        
        // Return a mock gas estimate based on transaction data length
        var baseGas = 21000m;
        var dataGas = (transaction.Data?.Length ?? 0) * 16m;
        
        return baseGas + dataGas;
    }

    /// <inheritdoc/>
    public async Task<string> SubscribeToBlocksAsync(Func<NeoServiceLayer.Infrastructure.Block, Task> callback)
    {
        await Task.Delay(10);
        var subscriptionId = Guid.NewGuid().ToString();

        if (!_blockSubscriptions.TryGetValue(subscriptionId, out var callbacks))
        {
            callbacks = new List<Func<NeoServiceLayer.Infrastructure.Block, Task>>();
            _blockSubscriptions[subscriptionId] = callbacks;
        }

        callbacks.Add(callback);
        return subscriptionId;
    }

    /// <inheritdoc/>
    public async Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId)
    {
        await Task.Delay(10);
        return _blockSubscriptions.Remove(subscriptionId);
    }

    /// <inheritdoc/>
    public async Task<string> SubscribeToTransactionsAsync(Func<NeoServiceLayer.Infrastructure.Transaction, Task> callback)
    {
        await Task.Delay(10);
        var subscriptionId = Guid.NewGuid().ToString();

        if (!_transactionSubscriptions.TryGetValue(subscriptionId, out var callbacks))
        {
            callbacks = new List<Func<NeoServiceLayer.Infrastructure.Transaction, Task>>();
            _transactionSubscriptions[subscriptionId] = callbacks;
        }

        callbacks.Add(callback);
        return subscriptionId;
    }

    /// <inheritdoc/>
    public async Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId)
    {
        await Task.Delay(10);
        return _transactionSubscriptions.Remove(subscriptionId);
    }

    /// <inheritdoc/>
    public async Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<NeoServiceLayer.Infrastructure.ContractEvent, Task> callback)
    {
        await Task.Delay(10);
        var subscriptionId = $"{contractAddress}:{eventName}:{Guid.NewGuid()}";

        if (!_eventSubscriptions.TryGetValue(subscriptionId, out var callbacks))
        {
            callbacks = new List<Func<NeoServiceLayer.Infrastructure.ContractEvent, Task>>();
            _eventSubscriptions[subscriptionId] = callbacks;
        }

        callbacks.Add(callback);
        return subscriptionId;
    }

    /// <inheritdoc/>
    public async Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        await Task.Delay(10);
        return _eventSubscriptions.Remove(subscriptionId);
    }

    /// <inheritdoc/>
    public async Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        await Task.Delay(20); // Simulate network delay

        _logger.LogDebug("Calling contract {Address} method {Method}", contractAddress, method);

        // Return mock response based on method
        return method switch
        {
            "balanceOf" => "1000000",
            "totalSupply" => "1000000000",
            "name" => "MockToken",
            "symbol" => "MCK",
            "decimals" => "18",
            _ => "0x" + GenerateHash()
        };
    }

    /// <inheritdoc/>
    public async Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        await Task.Delay(50); // Simulate network delay

        var txHash = GenerateHash();
        var transaction = new NeoServiceLayer.Infrastructure.Transaction
        {
            Hash = txHash,
            From = "0x" + GenerateHash(40),
            To = contractAddress,
            Value = 0,
            Data = $"{method}({string.Join(",", args)})",
            Timestamp = DateTime.UtcNow,
            BlockHeight = _currentHeight
        };

        _transactions[txHash] = transaction;

        _logger.LogDebug("Invoked contract {Address} method {Method}, tx: {Hash}", contractAddress, method, txHash);

        return txHash;
    }

    /// <inheritdoc/>
    public Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters)
    {
        return CallContractMethodAsync(contractAddress, method, parameters);
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters)
    {
        return InvokeContractMethodAsync(contractAddress, method, parameters);
    }

    #region Helper Methods

    private void InitializeMockData()
    {
        // Add some initial blocks
        for (long i = _currentHeight - 10; i <= _currentHeight; i++)
        {
            var block = GenerateMockBlock(i);
            _blocksByHeight[i] = block;
            _blocksByHash[block.Hash] = block;
        }
    }

    private NeoServiceLayer.Infrastructure.Block GenerateMockBlock(long height)
    {
        var transactions = new List<NeoServiceLayer.Infrastructure.Transaction>();
        var txCount = Random.Shared.Next(1, 10);

        for (int i = 0; i < txCount; i++)
        {
            var tx = GenerateMockTransaction();
            tx.BlockHeight = height;
            transactions.Add(tx);
            _transactions[tx.Hash] = tx;
        }

        return new NeoServiceLayer.Infrastructure.Block
        {
            Hash = "0x" + GenerateHash(),
            Height = height,
            Timestamp = DateTime.UtcNow.AddMinutes(-((_currentHeight - height) * 15)),
            PreviousHash = height > 0 ? "0x" + GenerateHash() : "0x0",
            Transactions = transactions
        };
    }

    private NeoServiceLayer.Infrastructure.Transaction GenerateMockTransaction(string? hash = null)
    {
        return new NeoServiceLayer.Infrastructure.Transaction
        {
            Hash = hash ?? "0x" + GenerateHash(),
            From = "0x" + GenerateHash(40),
            To = "0x" + GenerateHash(40),
            Value = (decimal)Random.Shared.NextDouble() * 100,
            Data = "0x" + GenerateHash(64),
            Timestamp = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(0, 60)),
            BlockHeight = _currentHeight,
            BlockHash = "0x" + GenerateHash()
        };
    }

    private string GenerateHash(int length = 64)
    {
        const string chars = "0123456789abcdef";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    #endregion
}