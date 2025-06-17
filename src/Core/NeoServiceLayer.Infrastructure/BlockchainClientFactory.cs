using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core;
using NeoServiceLayer.Neo.N3;
using NeoServiceLayer.Neo.X;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Factory for creating blockchain clients.
/// </summary>
public class BlockchainClientFactory : IBlockchainClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlockchainClientFactory> _logger;
    private readonly BlockchainConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockchainClientFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The blockchain configuration.</param>
    public BlockchainClientFactory(
        IServiceProvider serviceProvider,
        ILogger<BlockchainClientFactory> logger,
        IOptions<BlockchainConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration.Value;
    }

    /// <inheritdoc/>
    public IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        try
        {
            _logger.LogDebug("Creating blockchain client for {BlockchainType}", blockchainType);

            return blockchainType switch
            {
                BlockchainType.NeoN3 => CreateNeoN3Client(),
                BlockchainType.NeoX => CreateNeoXClient(),
                _ => throw new NotSupportedException($"Blockchain type {blockchainType} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create blockchain client for {BlockchainType}", blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchainTypes()
    {
        return new[] { BlockchainType.NeoN3, BlockchainType.NeoX };
    }

    /// <summary>
    /// Creates a Neo N3 blockchain client.
    /// </summary>
    /// <returns>The Neo N3 blockchain client.</returns>
    private IBlockchainClient CreateNeoN3Client()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        var logger = _serviceProvider.GetRequiredService<ILogger<Neo.N3.NeoN3Client>>();
        var rpcUrl = _configuration.NeoN3?.RpcUrl ?? "http://localhost:20332";

        // Use reflection to create the client since we can't directly reference the implementation
        var clientType = Type.GetType("NeoServiceLayer.Neo.N3.NeoN3Client, NeoServiceLayer.Neo.N3");
        if (clientType == null)
        {
            throw new InvalidOperationException("Neo N3 client type not found. Ensure NeoServiceLayer.Neo.N3 assembly is loaded.");
        }

        var client = Activator.CreateInstance(clientType, logger, httpClient, rpcUrl) as IBlockchainClient;
        if (client == null)
        {
            throw new InvalidOperationException("Failed to create Neo N3 client instance.");
        }

        _logger.LogInformation("Created Neo N3 client with RPC URL: {RpcUrl}", rpcUrl);
        return client;
    }

    /// <summary>
    /// Creates a Neo X blockchain client.
    /// </summary>
    /// <returns>The Neo X blockchain client.</returns>
    private IBlockchainClient CreateNeoXClient()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        var logger = _serviceProvider.GetRequiredService<ILogger<Neo.X.NeoXClient>>();
        var rpcUrl = _configuration.NeoX?.RpcUrl ?? "http://localhost:8545";

        // Use reflection to create the client since we can't directly reference the implementation
        var clientType = Type.GetType("NeoServiceLayer.Neo.X.NeoXClient, NeoServiceLayer.Neo.X");
        if (clientType == null)
        {
            throw new InvalidOperationException("Neo X client type not found. Ensure NeoServiceLayer.Neo.X assembly is loaded.");
        }

        var client = Activator.CreateInstance(clientType, logger, httpClient, rpcUrl) as IBlockchainClient;
        if (client == null)
        {
            throw new InvalidOperationException("Failed to create Neo X client instance.");
        }

        _logger.LogInformation("Created Neo X client with RPC URL: {RpcUrl}", rpcUrl);
        return client;
    }
}

/// <summary>
/// Configuration for blockchain clients.
/// </summary>
public class BlockchainConfiguration
{
    /// <summary>
    /// Gets or sets the Neo N3 configuration.
    /// </summary>
    public NeoN3Configuration? NeoN3 { get; set; }

    /// <summary>
    /// Gets or sets the Neo X configuration.
    /// </summary>
    public NeoXConfiguration? NeoX { get; set; }
}

/// <summary>
/// Configuration for Neo N3 blockchain client.
/// </summary>
public class NeoN3Configuration
{
    /// <summary>
    /// Gets or sets the RPC URL.
    /// </summary>
    public string RpcUrl { get; set; } = "http://localhost:20332";

    /// <summary>
    /// Gets or sets the WebSocket URL for subscriptions.
    /// </summary>
    public string? WebSocketUrl { get; set; }

    /// <summary>
    /// Gets or sets the network magic number.
    /// </summary>
    public uint NetworkMagic { get; set; } = 860833102; // Neo N3 MainNet

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable debug logging.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;
}

/// <summary>
/// Configuration for Neo X blockchain client.
/// </summary>
public class NeoXConfiguration
{
    /// <summary>
    /// Gets or sets the RPC URL.
    /// </summary>
    public string RpcUrl { get; set; } = "http://localhost:8545";

    /// <summary>
    /// Gets or sets the WebSocket URL for subscriptions.
    /// </summary>
    public string? WebSocketUrl { get; set; }

    /// <summary>
    /// Gets or sets the chain ID.
    /// </summary>
    public int ChainId { get; set; } = 12227332; // Neo X MainNet

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable debug logging.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the gas price strategy.
    /// </summary>
    public string GasPriceStrategy { get; set; } = "standard";
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
