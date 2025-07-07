using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// No alias needed - will use fully qualified names
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;

namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Test configuration and setup utilities.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Creates a test configuration with default values.
    /// </summary>
    public static IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            ["Blockchain:NeoN3:RpcUrl"] = "http://localhost:20332",
            ["Blockchain:NeoX:RpcUrl"] = "http://localhost:8545",
            ["KeyManagement:MaxKeyCount"] = "100",
            ["Oracle:MaxDataSources"] = "10",
            ["Storage:MaxStorageSizeBytes"] = "1000000",
            ["Voting:MaxActiveProposals"] = "5",
            ["AI:ModelCacheSize"] = "5",
            ["ZeroKnowledge:CircuitCacheSize"] = "10",
            ["Tee:EnclaveType"] = "Mock",
            ["Logging:LogLevel:Default"] = "Warning"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    /// <summary>
    /// Creates a test service collection with mock services.
    /// </summary>
    public static IServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Add configuration
        services.AddSingleton(CreateTestConfiguration());

        return services;
    }

    /// <summary>
    /// Creates test blockchain configuration.
    /// </summary>
    public static NeoServiceLayer.Infrastructure.Blockchain.BlockchainConfiguration CreateTestBlockchainConfig()
    {
        return new NeoServiceLayer.Infrastructure.Blockchain.BlockchainConfiguration
        {
            NeoN3 = new NeoServiceLayer.Infrastructure.Blockchain.NeoN3Configuration
            {
                RpcUrl = "http://localhost:20332",
                NetworkMagic = 860833102
            },
            NeoX = new NeoServiceLayer.Infrastructure.Blockchain.NeoXConfiguration
            {
                RpcUrl = "http://localhost:8545",
                ChainId = 12227332
            }
        };
    }
}
