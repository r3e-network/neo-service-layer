// No alias needed - will use fully qualified names
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Mock blockchain client factory for testing purposes.
/// This class provides a test implementation that wraps the production factory.
/// </summary>
public class MockBlockchainClientFactory : NeoServiceLayer.Core.IBlockchainClientFactory
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
    public NeoServiceLayer.Core.IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        // Return a mock client that implements Core interface
        return new MockBlockchainClient(_loggerFactory.CreateLogger<MockBlockchainClient>(), blockchainType);
    }



    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchainTypes()
    {
        return new[] { BlockchainType.NeoN3, BlockchainType.NeoX };
    }


    /// <summary>
    /// Gets the mock client for backward compatibility in tests.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The blockchain client.</returns>
    public NeoServiceLayer.Core.IBlockchainClient GetMockClient(BlockchainType blockchainType)
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
