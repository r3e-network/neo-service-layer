using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Blockchain;

namespace NeoServiceLayer.Web.Extensions;

/// <summary>
/// Wrapper to adapt between Core and Infrastructure IBlockchainClientFactory interfaces.
/// </summary>
internal class InfrastructureBlockchainClientFactoryWrapper : Infrastructure.IBlockchainClientFactory
{
    private readonly BlockchainClientFactory _factory;

    public InfrastructureBlockchainClientFactoryWrapper(BlockchainClientFactory factory)
    {
        _factory = factory;
    }

    public Infrastructure.IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        // The factory already returns Core.IBlockchainClient which the adapters implement
        // We need to cast it to Infrastructure.IBlockchainClient
        var coreClient = _factory.CreateClient(blockchainType);

        // Since our adapters implement Core.IBlockchainClient, we need to wrap them
        // to provide Infrastructure.IBlockchainClient
        return new InfrastructureBlockchainClientAdapter(coreClient);
    }

    public IEnumerable<BlockchainType> GetSupportedBlockchainTypes()
    {
        return _factory.GetSupportedBlockchainTypes();
    }
}

/// <summary>
/// Adapter to provide Infrastructure.IBlockchainClient interface from Core.IBlockchainClient.
/// </summary>
internal class InfrastructureBlockchainClientAdapter : Infrastructure.IBlockchainClient
{
    private readonly Core.IBlockchainClient _coreClient;

    public InfrastructureBlockchainClientAdapter(Core.IBlockchainClient coreClient)
    {
        _coreClient = coreClient;
    }

    public BlockchainType BlockchainType => _coreClient.BlockchainType;

    public Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters)
    {
        // Core interface uses CallContractMethodAsync
        return _coreClient.CallContractMethodAsync(contractAddress, method, parameters);
    }

    public Task<string> CallContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return _coreClient.CallContractMethodAsync(contractAddress, method, args);
    }

    public Task<decimal> EstimateGasAsync(Transaction transaction)
    {
        // Not available in Core interface, return default
        return Task.FromResult(0m);
    }

    public Task<decimal> GetBalanceAsync(string address, string assetId)
    {
        return _coreClient.GetBalanceAsync(address, assetId);
    }

    public Task<Block> GetBlockAsync(long height)
    {
        return _coreClient.GetBlockAsync(height);
    }

    public Task<Block> GetBlockAsync(string hash)
    {
        return _coreClient.GetBlockAsync(hash);
    }

    public Task<string> GetBlockHashAsync(long height)
    {
        // Not available in Core interface, use GetBlockAsync
        return _coreClient.GetBlockAsync(height).ContinueWith(t => t.Result.Hash);
    }

    public Task<long> GetBlockHeightAsync()
    {
        return _coreClient.GetBlockHeightAsync();
    }

    public Task<decimal> GetGasPriceAsync()
    {
        // Not available in Core interface, return default
        return Task.FromResult(0.00001m);
    }

    public Task<Transaction> GetTransactionAsync(string hash)
    {
        return _coreClient.GetTransactionAsync(hash);
    }

    public Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters)
    {
        // Core interface uses InvokeContractMethodAsync
        return _coreClient.InvokeContractMethodAsync(contractAddress, method, parameters);
    }

    public Task<string> InvokeContractMethodAsync(string contractAddress, string method, params object[] args)
    {
        return _coreClient.InvokeContractMethodAsync(contractAddress, method, args);
    }

    public Task<string> SendTransactionAsync(Transaction transaction)
    {
        return _coreClient.SendTransactionAsync(transaction);
    }

    public Task<string> SubscribeToBlocksAsync(Func<Block, Task> callback)
    {
        return _coreClient.SubscribeToBlocksAsync(callback);
    }

    public Task<string> SubscribeToContractEventsAsync(string contractAddress, string eventName, Func<ContractEvent, Task> callback)
    {
        return _coreClient.SubscribeToContractEventsAsync(contractAddress, eventName, callback);
    }

    public Task<string> SubscribeToTransactionsAsync(Func<Transaction, Task> callback)
    {
        return _coreClient.SubscribeToTransactionsAsync(callback);
    }

    public Task<bool> UnsubscribeFromBlocksAsync(string subscriptionId)
    {
        return _coreClient.UnsubscribeFromBlocksAsync(subscriptionId);
    }

    public Task<bool> UnsubscribeFromContractEventsAsync(string subscriptionId)
    {
        return _coreClient.UnsubscribeFromContractEventsAsync(subscriptionId);
    }

    public Task<bool> UnsubscribeFromTransactionsAsync(string subscriptionId)
    {
        return _coreClient.UnsubscribeFromTransactionsAsync(subscriptionId);
    }
}
