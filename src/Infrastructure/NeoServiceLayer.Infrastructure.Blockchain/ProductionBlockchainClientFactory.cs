using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Production implementation of the blockchain client factory for Neo N3 and Neo X networks.
/// </summary>
public class ProductionBlockchainClientFactory : IBlockchainClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<BlockchainType, IBlockchainClient> _clients = new();
    private readonly object _clientsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductionBlockchainClientFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="httpClient">The HTTP client for network requests.</param>
    public ProductionBlockchainClientFactory(ILoggerFactory loggerFactory, IConfiguration configuration, HttpClient httpClient)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public IBlockchainClient CreateClient(BlockchainType blockchainType)
    {
        lock (_clientsLock)
        {
            if (_clients.TryGetValue(blockchainType, out var existingClient))
            {
                return existingClient;
            }

            var client = CreateClientInternal(blockchainType);
            _clients[blockchainType] = client;
            return client;
        }
    }

    /// <inheritdoc/>
    public Task<IBlockchainClient> CreateClientAsync(BlockchainType blockchainType)
    {
        return Task.FromResult(CreateClient(blockchainType));
    }

    /// <inheritdoc/>
    public bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return blockchainType switch
        {
            BlockchainType.NeoN3 => true,
            BlockchainType.NeoX => true,
            _ => false
        };
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
        return ValidateConnectionAsync(blockchainType, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateConnectionAsync(BlockchainType blockchainType, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(blockchainType);

            // Test the connection by getting the current block height
            var height = await client.GetBlockHeightAsync();

            var logger = _loggerFactory.CreateLogger<ProductionBlockchainClientFactory>();
            logger.LogInformation("Successfully validated connection to {BlockchainType}. Current height: {Height}",
                blockchainType, height);

            return height >= 0;
        }
        catch (Exception ex)
        {
            var logger = _loggerFactory.CreateLogger<ProductionBlockchainClientFactory>();
            logger.LogError(ex, "Failed to validate connection to {BlockchainType}", blockchainType);
            return false;
        }
    }

    /// <summary>
    /// Creates a blockchain client for the specified blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The blockchain client.</returns>
    private IBlockchainClient CreateClientInternal(BlockchainType blockchainType)
    {
        var logger = _loggerFactory.CreateLogger<NeoBlockchainClient>();
        var rpcEndpoint = GetRpcEndpoint(blockchainType);

        logger.LogInformation("Creating blockchain client for {BlockchainType} using endpoint {Endpoint}",
            blockchainType, rpcEndpoint);

        return new NeoBlockchainClient(logger, blockchainType, _httpClient, rpcEndpoint);
    }

    /// <summary>
    /// Gets the RPC endpoint for the specified blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The RPC endpoint URL.</returns>
    private string GetRpcEndpoint(BlockchainType blockchainType)
    {
        var configKey = blockchainType switch
        {
            BlockchainType.NeoN3 => "Blockchain:NeoN3:RpcEndpoint",
            BlockchainType.NeoX => "Blockchain:NeoX:RpcEndpoint",
            _ => throw new NotSupportedException($"Blockchain type {blockchainType} is not supported")
        };

        var endpoint = _configuration[configKey];

        if (string.IsNullOrEmpty(endpoint))
        {
            // Use default endpoints if not configured
            endpoint = blockchainType switch
            {
                BlockchainType.NeoN3 => GetDefaultNeoN3Endpoint(),
                BlockchainType.NeoX => GetDefaultNeoXEndpoint(),
                _ => throw new NotSupportedException($"No default endpoint available for {blockchainType}")
            };

            var logger = _loggerFactory.CreateLogger<ProductionBlockchainClientFactory>();
            logger.LogWarning("No RPC endpoint configured for {BlockchainType}, using default: {Endpoint}",
                blockchainType, endpoint);
        }

        return endpoint;
    }

    /// <summary>
    /// Gets the default Neo N3 RPC endpoint.
    /// </summary>
    /// <returns>The default endpoint URL.</returns>
    private string GetDefaultNeoN3Endpoint()
    {
        // Try multiple well-known Neo N3 endpoints in order of preference
        var defaultEndpoints = new[]
        {
            "https://mainnet1.neo.coz.io:443", // City of Zion mainnet
            "https://mainnet2.neo.coz.io:443", // City of Zion mainnet backup
            "https://neo3-mainnet.neoline.vip:443", // NeoLine mainnet
            "https://mainnet.neotube.io:443", // NeoTube mainnet
            "https://rpc1.n3.nspcc.ru:10332", // NSPCC mainnet
            "http://localhost:20332" // Local node fallback
        };

        // In production, you might want to test connectivity and choose the best endpoint
        return defaultEndpoints[0];
    }

    /// <summary>
    /// Gets the default Neo X RPC endpoint.
    /// </summary>
    /// <returns>The default endpoint URL.</returns>
    private string GetDefaultNeoXEndpoint()
    {
        // Neo X specific endpoints (when available)
        var defaultEndpoints = new[]
        {
            "https://neox-mainnet.neo.org:443", // Official Neo X mainnet (hypothetical)
            "https://neox.coz.io:443", // City of Zion Neo X (hypothetical)
            "http://localhost:30332" // Local Neo X node fallback
        };

        return defaultEndpoints[0];
    }

    /// <summary>
    /// Gets configuration for network-specific parameters.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Network configuration parameters.</returns>
    private NetworkConfiguration GetNetworkConfiguration(BlockchainType blockchainType)
    {
        var configSection = blockchainType switch
        {
            BlockchainType.NeoN3 => "Blockchain:NeoN3",
            BlockchainType.NeoX => "Blockchain:NeoX",
            _ => throw new NotSupportedException($"Blockchain type {blockchainType} is not supported")
        };

        return new NetworkConfiguration
        {
            NetworkMagic = _configuration.GetValue<uint>($"{configSection}:NetworkMagic", GetDefaultNetworkMagic(blockchainType)),
            AddressVersion = _configuration.GetValue<byte>($"{configSection}:AddressVersion", GetDefaultAddressVersion(blockchainType)),
            MaxTransactionsPerBlock = _configuration.GetValue<uint>($"{configSection}:MaxTransactionsPerBlock", 512),
            MillisecondsPerBlock = _configuration.GetValue<uint>($"{configSection}:MillisecondsPerBlock", 15000),
            StandbyCommittee = _configuration.GetSection($"{configSection}:StandbyCommittee").Get<string[]>() ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Gets the default network magic for the blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The network magic value.</returns>
    private uint GetDefaultNetworkMagic(BlockchainType blockchainType)
    {
        return blockchainType switch
        {
            BlockchainType.NeoN3 => 0x4E454F33, // "NEO3" in hex
            BlockchainType.NeoX => 0x4E454F58, // "NEOX" in hex
            _ => 0x00000000
        };
    }

    /// <summary>
    /// Gets the default address version for the blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The address version byte.</returns>
    private byte GetDefaultAddressVersion(BlockchainType blockchainType)
    {
        return blockchainType switch
        {
            BlockchainType.NeoN3 => 0x35, // Neo N3 address version
            BlockchainType.NeoX => 0x35, // Neo X address version (same as N3)
            _ => 0x17
        };
    }

    /// <summary>
    /// Disposes all created clients and resources.
    /// </summary>
    public void Dispose()
    {
        lock (_clientsLock)
        {
            foreach (var client in _clients.Values.OfType<IDisposable>())
            {
                client.Dispose();
            }
            _clients.Clear();
        }

        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Network configuration for blockchain clients.
/// </summary>
public class NetworkConfiguration
{
    public uint NetworkMagic { get; set; }
    public byte AddressVersion { get; set; }
    public uint MaxTransactionsPerBlock { get; set; }
    public uint MillisecondsPerBlock { get; set; }
    public string[] StandbyCommittee { get; set; } = Array.Empty<string>();
}


