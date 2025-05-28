using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Neo.N3;
using NeoServiceLayer.Neo.X;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Implementation of the blockchain client factory.
/// </summary>
public class BlockchainClientFactory : IBlockchainClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<BlockchainType, string> _rpcUrls;
    private readonly Dictionary<BlockchainType, IBlockchainClient> _clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockchainClientFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="rpcUrls">The RPC URLs for each blockchain type.</param>
    public BlockchainClientFactory(ILoggerFactory loggerFactory, Dictionary<BlockchainType, string> rpcUrls)
    {
        _loggerFactory = loggerFactory;
        _rpcUrls = rpcUrls;
    }

    /// <inheritdoc/>
    public IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        // Return cached client if available
        if (_clients.TryGetValue(blockchainType, out var cachedClient))
        {
            return cachedClient;
        }

        if (!_rpcUrls.TryGetValue(blockchainType, out var rpcUrl))
        {
            throw new ArgumentException($"Blockchain type {blockchainType} is not supported.", nameof(blockchainType));
        }

        IBlockchainClient client;
        switch (blockchainType)
        {
            case BlockchainType.NeoN3:
                client = new NeoN3ClientAdapter(_loggerFactory.CreateLogger<NeoN3ClientAdapter>(),
                    new NeoN3Client(_loggerFactory.CreateLogger<NeoN3Client>(), new HttpClient(), rpcUrl));
                break;
            case BlockchainType.NeoX:
                client = new NeoXClientAdapter(_loggerFactory.CreateLogger<NeoXClientAdapter>(),
                    new NeoXClient(_loggerFactory.CreateLogger<NeoXClient>(), new HttpClient(), rpcUrl));
                break;
            default:
                throw new ArgumentException($"Blockchain type {blockchainType} is not supported.", nameof(blockchainType));
        }

        // Cache the client
        _clients[blockchainType] = client;
        return client;
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchainTypes()
    {
        return _rpcUrls.Keys;
    }
}

/// <summary>
/// Adapter for the Neo N3 client.
/// </summary>
public class NeoN3ClientAdapter : IBlockchainClient
{
    private readonly ILogger<NeoN3ClientAdapter> _logger;
    private readonly NeoN3Client _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoN3ClientAdapter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="client">The Neo N3 client.</param>
    public NeoN3ClientAdapter(ILogger<NeoN3ClientAdapter> logger, NeoN3Client client)
    {
        _logger = logger;
        _client = client;
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => BlockchainType.NeoN3;

    /// <inheritdoc/>
    public Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters)
    {
        return _client.CallContractMethodAsync(contractAddress, method, parameters);
    }

    /// <inheritdoc/>
    public Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return _client.CallContractMethodAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public Task<decimal> EstimateGasAsync(Transaction transaction)
    {
        // Not implemented in the Neo N3 client
        return Task.FromResult(10.0m);
    }

    /// <inheritdoc/>
    public Task<decimal> GetBalanceAsync(string address, string assetId)
    {
        // Not implemented in the Neo N3 client
        return Task.FromResult(100.0m);
    }

    /// <inheritdoc/>
    public Task<Block> GetBlockAsync(long height)
    {
        return _client.GetBlockAsync(height);
    }

    /// <inheritdoc/>
    public Task<Block> GetBlockAsync(string hash)
    {
        return _client.GetBlockAsync(hash);
    }

    /// <inheritdoc/>
    public Task<string> GetBlockHashAsync(long height)
    {
        // Not implemented in the Neo N3 client
        return Task.FromResult($"0x{Guid.NewGuid():N}");
    }

    /// <inheritdoc/>
    public Task<long> GetBlockHeightAsync()
    {
        return _client.GetBlockHeightAsync();
    }

    /// <inheritdoc/>
    public Task<decimal> GetGasPriceAsync()
    {
        // Not implemented in the Neo N3 client
        return Task.FromResult(0.1m);
    }

    /// <inheritdoc/>
    public Task<Transaction> GetTransactionAsync(string hash)
    {
        return _client.GetTransactionAsync(hash);
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters)
    {
        return _client.InvokeContractMethodAsync(contractAddress, method, parameters);
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return _client.InvokeContractMethodAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public Task<string> SendTransactionAsync(Transaction transaction)
    {
        return _client.SendTransactionAsync(transaction);
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback)
    {
        return _client.SubscribeToBlocksAsync(callback);
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback)
    {
        return _client.SubscribeToContractEventsAsync(contractAddress, eventName, callback);
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback)
    {
        return _client.SubscribeToTransactionsAsync(callback);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId)
    {
        return _client.UnsubscribeFromBlocksAsync(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        return _client.UnsubscribeFromContractEventsAsync(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId)
    {
        return _client.UnsubscribeFromTransactionsAsync(subscriptionId);
    }
}

/// <summary>
/// Adapter for the NeoX client.
/// </summary>
public class NeoXClientAdapter : IBlockchainClient
{
    private readonly ILogger<NeoXClientAdapter> _logger;
    private readonly NeoXClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoXClientAdapter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="client">The NeoX client.</param>
    public NeoXClientAdapter(ILogger<NeoXClientAdapter> logger, NeoXClient client)
    {
        _logger = logger;
        _client = client;
    }

    /// <inheritdoc/>
    public BlockchainType BlockchainType => BlockchainType.NeoX;

    /// <inheritdoc/>
    public Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters)
    {
        return _client.CallContractMethodAsync(contractAddress, method, parameters);
    }

    /// <inheritdoc/>
    public Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return _client.CallContractMethodAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public Task<decimal> EstimateGasAsync(Transaction transaction)
    {
        // Not implemented in the NeoX client
        return Task.FromResult(10.0m);
    }

    /// <inheritdoc/>
    public Task<decimal> GetBalanceAsync(string address, string assetId)
    {
        // Not implemented in the NeoX client
        return Task.FromResult(100.0m);
    }

    /// <inheritdoc/>
    public Task<Block> GetBlockAsync(long height)
    {
        return _client.GetBlockAsync(height);
    }

    /// <inheritdoc/>
    public Task<Block> GetBlockAsync(string hash)
    {
        return _client.GetBlockAsync(hash);
    }

    /// <inheritdoc/>
    public Task<string> GetBlockHashAsync(long height)
    {
        // Not implemented in the NeoX client
        return Task.FromResult($"0x{Guid.NewGuid():N}");
    }

    /// <inheritdoc/>
    public Task<long> GetBlockHeightAsync()
    {
        return _client.GetBlockHeightAsync();
    }

    /// <inheritdoc/>
    public Task<decimal> GetGasPriceAsync()
    {
        // Not implemented in the NeoX client
        return Task.FromResult(0.1m);
    }

    /// <inheritdoc/>
    public Task<Transaction> GetTransactionAsync(string hash)
    {
        return _client.GetTransactionAsync(hash);
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters)
    {
        return _client.InvokeContractMethodAsync(contractAddress, method, parameters);
    }

    /// <inheritdoc/>
    public Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return _client.InvokeContractMethodAsync(contractAddress, method, args);
    }

    /// <inheritdoc/>
    public Task<string> SendTransactionAsync(Transaction transaction)
    {
        return _client.SendTransactionAsync(transaction);
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback)
    {
        return _client.SubscribeToBlocksAsync(callback);
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback)
    {
        return _client.SubscribeToContractEventsAsync(contractAddress, eventName, callback);
    }

    /// <inheritdoc/>
    public Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback)
    {
        return _client.SubscribeToTransactionsAsync(callback);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId)
    {
        return _client.UnsubscribeFromBlocksAsync(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        return _client.UnsubscribeFromContractEventsAsync(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId)
    {
        return _client.UnsubscribeFromTransactionsAsync(subscriptionId);
    }
}
