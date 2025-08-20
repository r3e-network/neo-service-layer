using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Blockchain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Mock blockchain client factory for testing purposes.
/// This class provides a test implementation that wraps the production factory.
/// </summary>
public class MockBlockchainClientFactory : IBlockchainClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockBlockchainClientFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public MockBlockchainClientFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockBlockchainClientFactory"/> class with custom configuration.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">Custom configuration for testing.</param>
    /// <param name="httpClient">Custom HTTP client for testing.</param>
    public MockBlockchainClientFactory(ILoggerFactory loggerFactory, IConfiguration configuration, HttpClient httpClient)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public NeoServiceLayer.Infrastructure.IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        // Return a mock client that implements Infrastructure interface
        return new MockInfrastructureBlockchainClient(_loggerFactory.CreateLogger<MockInfrastructureBlockchainClient>(), blockchainType);
    }

    /// <inheritdoc/>
    public Task<NeoServiceLayer.Infrastructure.IBlockchainClient> CreateClientAsync(BlockchainType blockchainType)
    {
        return Task.FromResult(CreateClient(blockchainType));
    }

    /// <inheritdoc/>
    public bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return blockchainType == BlockchainType.NeoN3 || blockchainType == BlockchainType.NeoX;
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchains()
    {
        return new[] { BlockchainType.NeoN3, BlockchainType.NeoX };
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchainTypes()
    {
        return GetSupportedBlockchains();
    }

    /// <inheritdoc/>
    public Task<bool> ValidateConnectionAsync(BlockchainType blockchainType)
    {
        return Task.FromResult(SupportsBlockchain(blockchainType));
    }

    /// <inheritdoc/>
    public Task<bool> ValidateConnectionAsync(BlockchainType blockchainType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SupportsBlockchain(blockchainType));
    }

    /// <summary>
    /// Gets the mock client for backward compatibility in tests.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The blockchain client.</returns>
    public NeoServiceLayer.Infrastructure.IBlockchainClient GetMockClient(BlockchainType blockchainType)
    {
        // Return as Core interface for backward compatibility
        return new MockBlockchainClient(_loggerFactory.CreateLogger<MockBlockchainClient>(), blockchainType);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose in mock implementation
    }
}
