using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Mock implementation of the blockchain client factory for testing.
/// </summary>
public class MockBlockchainClientFactory : IBlockchainClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<BlockchainType, MockBlockchainClient> _clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MockBlockchainClientFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public MockBlockchainClientFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;

        // Create mock clients for all blockchain types
        foreach (BlockchainType blockchainType in Enum.GetValues(typeof(BlockchainType)))
        {
            _clients[blockchainType] = new MockBlockchainClient(
                _loggerFactory.CreateLogger<MockBlockchainClient>(),
                blockchainType);
        }
    }

    /// <inheritdoc/>
    public IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        if (_clients.TryGetValue(blockchainType, out var client))
        {
            return client;
        }

        throw new ArgumentException($"Blockchain type {blockchainType} is not supported.", nameof(blockchainType));
    }

    /// <inheritdoc/>
    public IEnumerable<BlockchainType> GetSupportedBlockchainTypes()
    {
        return _clients.Keys;
    }

    /// <summary>
    /// Gets a mock blockchain client for the specified blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The mock blockchain client.</returns>
    public MockBlockchainClient GetMockClient(BlockchainType blockchainType)
    {
        if (_clients.TryGetValue(blockchainType, out var client))
        {
            return client;
        }

        throw new ArgumentException($"Blockchain type {blockchainType} is not supported.", nameof(blockchainType));
    }
}
