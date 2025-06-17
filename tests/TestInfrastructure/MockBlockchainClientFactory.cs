using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using System.Net.Http;

namespace NeoServiceLayer.Tests.Infrastructure;

/// <summary>
/// Mock blockchain client factory for testing purposes.
/// This class provides a test implementation that wraps the production factory.
/// </summary>
public class MockBlockchainClientFactory : IBlockchainClientFactory
{
    private readonly ProductionBlockchainClientFactory _productionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockBlockchainClientFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public MockBlockchainClientFactory(ILoggerFactory loggerFactory)
    {
        // Create a default configuration and HTTP client for testing
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Blockchain:NeoN3:RpcEndpoint"] = "http://localhost:20332",
                ["Blockchain:NeoX:RpcEndpoint"] = "http://localhost:30332",
                ["Blockchain:NeoN3:NetworkMagic"] = "1951352142",
                ["Blockchain:NeoX:NetworkMagic"] = "1313235324"
            })
            .Build();
        
        var httpClient = new HttpClient();
        
        _productionFactory = new ProductionBlockchainClientFactory(loggerFactory, configuration, httpClient);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockBlockchainClientFactory"/> class with custom configuration.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">Custom configuration for testing.</param>
    /// <param name="httpClient">Custom HTTP client for testing.</param>
    public MockBlockchainClientFactory(ILoggerFactory loggerFactory, IConfiguration configuration, HttpClient httpClient)
    {
        _productionFactory = new ProductionBlockchainClientFactory(loggerFactory, configuration, httpClient);
    }

    /// <inheritdoc/>
    public IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        return _productionFactory.CreateClient(blockchainType);
    }

    /// <inheritdoc/>
    public Task<IBlockchainClient> CreateClientAsync(BlockchainType blockchainType)
    {
        return _productionFactory.CreateClientAsync(blockchainType);
    }

    /// <inheritdoc/>
    public bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return _productionFactory.SupportsBlockchain(blockchainType);
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchains()
    {
        return _productionFactory.GetSupportedBlockchains();
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchainTypes()
    {
        return GetSupportedBlockchains();
    }

    /// <inheritdoc/>
    public Task<bool> ValidateConnectionAsync(BlockchainType blockchainType)
    {
        return _productionFactory.ValidateConnectionAsync(blockchainType);
    }

    /// <inheritdoc/>
    public Task<bool> ValidateConnectionAsync(BlockchainType blockchainType, CancellationToken cancellationToken = default)
    {
        return _productionFactory.ValidateConnectionAsync(blockchainType, cancellationToken);
    }

    /// <summary>
    /// Gets the mock client for backward compatibility in tests.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The blockchain client.</returns>
    public IBlockchainClient GetMockClient(BlockchainType blockchainType)
    {
        return CreateClient(blockchainType);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _productionFactory?.Dispose();
    }
} 