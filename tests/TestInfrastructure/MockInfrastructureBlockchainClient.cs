using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Mock implementation of Infrastructure.IBlockchainClient for testing purposes.
/// </summary>
public class MockInfrastructureBlockchainClient : MockBlockchainClient, NeoServiceLayer.Infrastructure.IBlockchainClient
{
    private readonly ILogger<MockInfrastructureBlockchainClient> _logger;
    private readonly Dictionary<string, Dictionary<string, decimal>> _balances = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MockInfrastructureBlockchainClient"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    public MockInfrastructureBlockchainClient(ILogger<MockInfrastructureBlockchainClient> logger, BlockchainType blockchainType = BlockchainType.NeoN3)
        : base(logger, blockchainType)
    {
        _logger = logger;
        InitializeMockBalances();
    }

    /// <summary>
    /// Gets a block hash by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block hash.</returns>
    public async Task<string> GetBlockHashAsync(long height)
    {
        await Task.Delay(10); // Simulate network delay
        var block = await GetBlockAsync(height);
        return block.Hash;
    }

    /// <summary>
    /// Gets the balance of an address for a specific asset.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="assetId">The asset ID.</param>
    /// <returns>The balance.</returns>
    public async Task<decimal> GetBalanceAsync(string address, string assetId)
    {
        await Task.Delay(10); // Simulate network delay

        if (_balances.TryGetValue(address, out var assets))
        {
            if (assets.TryGetValue(assetId, out var balance))
            {
                return balance;
            }
        }

        return 0m;
    }

    /// <summary>
    /// Gets the current gas price.
    /// </summary>
    /// <returns>The gas price.</returns>
    public async Task<decimal> GetGasPriceAsync()
    {
        await Task.Delay(10); // Simulate network delay

        // Return a mock gas price
        return BlockchainType switch
        {
            BlockchainType.NeoN3 => 0.01m, // 0.01 GAS
            BlockchainType.NeoX => 20m, // 20 Gwei
            _ => 1m
        };
    }

    /// <summary>
    /// Estimates gas for a transaction.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <returns>The estimated gas.</returns>
    public async Task<decimal> EstimateGasAsync(NeoServiceLayer.Infrastructure.Transaction transaction)
    {
        await Task.Delay(20); // Simulate network delay

        // Simple gas estimation based on data size
        var baseGas = 21000m;
        var dataGas = string.IsNullOrEmpty(transaction.Data) ? 0m : transaction.Data.Length * 68m;

        return baseGas + dataGas;
    }

    /// <summary>
    /// Calls a contract method (read-only, Infrastructure version).
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <returns>The method result.</returns>
    public async Task<string> CallContractAsync(string contractAddress, string method, params object[] parameters)
    {
        // Delegate to base implementation
        return await CallContractMethodAsync(contractAddress, method, parameters);
    }

    /// <summary>
    /// Invokes a contract method (state-changing, Infrastructure version).
    /// </summary>
    /// <param name="contractAddress">The contract address.</param>
    /// <param name="method">The method name.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <returns>The transaction hash.</returns>
    public async Task<string> InvokeContractAsync(string contractAddress, string method, params object[] parameters)
    {
        // Delegate to base implementation
        return await InvokeContractMethodAsync(contractAddress, method, parameters);
    }

    #region Helper Methods

    private void InitializeMockBalances()
    {
        // Add some test balances
        var testAddresses = new[]
        {
            "0x1234567890123456789012345678901234567890",
            "0xabcdefabcdefabcdefabcdefabcdefabcdefabcd",
            "NSiVJL6j5NJ9PEKnvYVkXxKdj5rqmJEbZD",
            "NTrezR3C4X8aMLVg7vozt5wguyNfFhwuFx"
        };

        var assetIds = new[]
        {
            "NEO",
            "GAS",
            "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5", // NEO token
            "0xd2a4cff31913016155e38e474a2c06d08be276cf"  // GAS token
        };

        foreach (var address in testAddresses)
        {
            _balances[address] = new Dictionary<string, decimal>();
            foreach (var assetId in assetIds)
            {
                _balances[address][assetId] = (decimal)(Random.Shared.NextDouble() * 10000);
            }
        }
    }

    #endregion
}
